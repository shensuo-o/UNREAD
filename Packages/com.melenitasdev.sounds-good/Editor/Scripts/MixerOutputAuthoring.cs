/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using MelenitasDev.SoundsGood.Domain;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace MelenitasDev.SoundsGood.Editor
{
    /// <summary>
    /// Creates, renames and deletes Audio Mixer groups — our Outputs — from code, exposing each
    /// group's volume under exactly the group's name. That's the whole manual routine a user would
    /// otherwise follow in the Audio Mixer window (add child group, rename, expose the volume fader,
    /// rename the exposed parameter to match), done in one call.
    ///
    /// Unity ships no public API for any of it, so this goes through the same internal one the Audio
    /// Mixer window itself uses (<c>UnityEditor.Audio.AudioMixerController</c>). Every lookup is
    /// resolved once and verified: if a future Unity version moves or renames something,
    /// <see cref="IsSupported"/> turns false and the Output Manager falls back to the manual guide
    /// instead of throwing.
    /// </summary>
    internal static class MixerOutputAuthoring
    {
        internal const string UNSUPPORTED_MESSAGE =
            "Sounds Good can't create outputs automatically on this Unity version. " +
            "Follow the manual steps below instead.";

        // ----- Reflected members (resolved once, on first use)
        private static Type controllerType;
        private static Type groupType;
        private static Type exposedParameterType;

        private static PropertyInfo masterGroupProperty;
        private static PropertyInfo exposedParametersProperty;
        private static MethodInfo createNewGroupMethod;
        private static MethodInfo addChildToParentMethod;
        private static MethodInfo addGroupToCurrentViewMethod;
        private static MethodInfo deleteGroupsMethod;
        private static MethodInfo getGuidForVolumeMethod;
        private static FieldInfo exposedGuidField;
        private static FieldInfo exposedNameField;

        private static bool resolved;
        private static bool supported;

        /// <summary> Whether this Unity version exposes everything needed to author outputs in code. </summary>
        internal static bool IsSupported
        {
            get
            {
                Resolve();
                return supported;
            }
        }

        // ----- Public operations

        /// <summary>
        /// Adds a group named <paramref name="name"/> under Master and exposes its volume under that
        /// same name, so <see cref="SoundsGoodManager"/> can drive it.
        /// </summary>
        internal static bool TryCreateOutput (string name, out string error)
        {
            if (!TryGetMixer(out AudioMixer mixer, out error)) return false;

            try
            {
                object master = masterGroupProperty.GetValue(mixer);
                if (master == null)
                {
                    error = "The master Audio Mixer has no Master group.";
                    return false;
                }

                object group = createNewGroupMethod.Invoke(mixer, new object[] { name, true });
                if (group == null)
                {
                    error = $"Unity refused to create a group named '{name}'.";
                    return false;
                }

                addChildToParentMethod.Invoke(mixer, new[] { group, master });
                // Without this the group exists but stays hidden in the Audio Mixer window's view.
                addGroupToCurrentViewMethod.Invoke(mixer, new[] { group });

                if (!TryExposeVolume(mixer, group, name, out error)) return false;

                Save(mixer);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        /// <summary> Deletes the group and drops the exposed parameter that pointed at its volume. </summary>
        internal static bool TryDeleteOutput (AudioMixerGroup output, out string error)
        {
            if (!TryGetMixer(out AudioMixer mixer, out error)) return false;

            if (output == null || !groupType.IsInstanceOfType(output))
            {
                error = "That output isn't a group of the master Audio Mixer.";
                return false;
            }

            try
            {
                object volumeGuid = getGuidForVolumeMethod.Invoke(output, null);
                if (volumeGuid != null) SetExposedParameters(mixer, volumeGuid, null);

                Array groups = Array.CreateInstance(groupType, 1);
                groups.SetValue(output, 0);
                deleteGroupsMethod.Invoke(mixer, new object[] { groups });

                Save(mixer);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        /// <summary>
        /// Renames the group and its exposed parameter together — they have to keep matching, since
        /// that name is what the volume lookup uses.
        /// </summary>
        internal static bool TryRenameOutput (AudioMixerGroup output, string newName, out string error)
        {
            if (!TryGetMixer(out AudioMixer mixer, out error)) return false;

            if (output == null || !groupType.IsInstanceOfType(output))
            {
                error = "That output isn't a group of the master Audio Mixer.";
                return false;
            }

            try
            {
                object volumeGuid = getGuidForVolumeMethod.Invoke(output, null);
                if (volumeGuid == null)
                {
                    error = "Couldn't read that group's volume parameter.";
                    return false;
                }

                output.name = newName;
                SetExposedParameters(mixer, volumeGuid, newName);

                EditorUtility.SetDirty(output);
                Save(mixer);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        // ----- Private Methods

        private static bool TryGetMixer (out AudioMixer mixer, out string error)
        {
            mixer = null;
            error = null;

            Resolve();
            if (!supported)
            {
                error = UNSUPPORTED_MESSAGE;
                return false;
            }

            mixer = AssetLocator.Instance.MasterAudioMixer;
            if (mixer == null)
            {
                error = "No master Audio Mixer found. Check the Data Root Path in Settings.";
                return false;
            }

            if (!controllerType.IsInstanceOfType(mixer))
            {
                error = "The master Audio Mixer isn't an editable mixer asset.";
                return false;
            }

            return true;
        }

        /// <summary> Exposes the group's volume under <paramref name="parameterName"/>. </summary>
        private static bool TryExposeVolume (AudioMixer mixer, object group, string parameterName, out string error)
        {
            error = null;

            object volumeGuid = getGuidForVolumeMethod.Invoke(group, null);
            if (volumeGuid == null)
            {
                error = "The group was created, but its volume parameter couldn't be read.";
                return false;
            }

            SetExposedParameters(mixer, volumeGuid, parameterName);
            return true;
        }

        /// <summary>
        /// Rewrites the mixer's exposed parameter list so the entry for <paramref name="volumeGuid"/>
        /// is named <paramref name="parameterName"/> — or removed entirely when it's null. Writing the
        /// list directly (instead of expose-then-rename) means the parameter is never left with
        /// Unity's default "MyExposedParam" name.
        /// </summary>
        private static void SetExposedParameters (AudioMixer mixer, object volumeGuid, string parameterName)
        {
            Array current = exposedParametersProperty.GetValue(mixer) as Array;
            var entries = new List<object>();

            int length = current?.Length ?? 0;
            for (int i = 0; i < length; i++)
            {
                object entry = current.GetValue(i);
                // Drop any existing entry for this volume: it's either being renamed or removed.
                if (volumeGuid.Equals(exposedGuidField.GetValue(entry))) continue;
                entries.Add(entry);
            }

            if (!string.IsNullOrEmpty(parameterName))
            {
                object added = Activator.CreateInstance(exposedParameterType);
                exposedGuidField.SetValue(added, volumeGuid);
                exposedNameField.SetValue(added, parameterName);
                entries.Add(added);
            }

            Array updated = Array.CreateInstance(exposedParameterType, entries.Count);
            for (int i = 0; i < entries.Count; i++) updated.SetValue(entries[i], i);

            exposedParametersProperty.SetValue(mixer, updated);
        }

        private static void Save (AudioMixer mixer)
        {
            EditorUtility.SetDirty(mixer);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // ----- Reflection setup

        private static void Resolve ()
        {
            if (resolved) return;
            resolved = true;
            supported = false;

            try
            {
                controllerType = FindType("UnityEditor.Audio.AudioMixerController");
                groupType = FindType("UnityEditor.Audio.AudioMixerGroupController");
                if (controllerType == null || groupType == null) return;

                const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

                masterGroupProperty = controllerType.GetProperty("masterGroup", flags);
                exposedParametersProperty = controllerType.GetProperty("exposedParameters", flags);
                createNewGroupMethod = controllerType.GetMethod("CreateNewGroup", flags,
                    null, new[] { typeof(string), typeof(bool) }, null);
                addChildToParentMethod = controllerType.GetMethod("AddChildToParent", flags,
                    null, new[] { groupType, groupType }, null);
                addGroupToCurrentViewMethod = controllerType.GetMethod("AddGroupToCurrentView", flags,
                    null, new[] { groupType }, null);
                deleteGroupsMethod = controllerType.GetMethod("DeleteGroups", flags,
                    null, new[] { groupType.MakeArrayType() }, null);
                getGuidForVolumeMethod = groupType.GetMethod("GetGUIDForVolume", flags,
                    null, Type.EmptyTypes, null);

                if (masterGroupProperty == null || exposedParametersProperty == null ||
                    createNewGroupMethod == null || addChildToParentMethod == null ||
                    addGroupToCurrentViewMethod == null || deleteGroupsMethod == null ||
                    getGuidForVolumeMethod == null)
                    return;

                exposedParameterType = exposedParametersProperty.PropertyType.GetElementType();
                if (exposedParameterType == null) return;

                exposedGuidField = exposedParameterType.GetField("guid", flags);
                exposedNameField = exposedParameterType.GetField("name", flags);
                if (exposedGuidField == null || exposedNameField == null) return;

                supported = true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Sounds Good] Automatic output creation is unavailable: {exception.Message}");
                supported = false;
            }
        }

        private static Type FindType (string fullName)
        {
            Type type = Type.GetType($"{fullName}, UnityEditor");
            if (type != null) return type;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullName);
                if (type != null) return type;
            }

            return null;
        }
    }
}
