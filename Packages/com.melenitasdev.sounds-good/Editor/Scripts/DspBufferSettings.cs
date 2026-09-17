using UnityEditor;
using UnityEngine;

namespace MelenitasDev.SoundsGood.Editor
{
    /// <summary>
    /// Reads and writes Unity's DSP Buffer Size, which lives in the project's AudioManager asset
    /// (Project Settings > Audio) rather than in the Sounds Good settings. It's surfaced in the
    /// Settings window because it's the single biggest contributor to how long it takes a sound to
    /// be heard after Play() is called, so tuning audio latency stays in one place.
    /// </summary>
    internal static class DspBufferSettings
    {
        private const string AUDIO_MANAGER_PATH = "ProjectSettings/AudioManager.asset";

        /// <summary> Buffer sizes in samples, matching Unity's own DSP Buffer Size popup. </summary>
        internal static readonly int[] Sizes = { 0, 256, 512, 1024 };

        /// <summary> Labels for <see cref="Sizes"/>, in the same order. </summary>
        internal static readonly string[] Labels = { "Default", "Best latency", "Good latency", "Best performance" };

        /// <summary> The buffer size stored in the project, in samples (0 = platform default). </summary>
        internal static int GetBufferSize ()
        {
            SerializedObject audioManager = GetAudioManager();
            if (audioManager == null) return 0;

            SerializedProperty requested = audioManager.FindProperty("m_RequestedDSPBufferSize");
            return requested != null ? requested.intValue : 0;
        }

        /// <summary> Writes the buffer size to the project's AudioManager asset and saves it. </summary>
        internal static void SetBufferSize (int size)
        {
            SerializedObject audioManager = GetAudioManager();
            if (audioManager == null)
            {
                Debug.LogError($"[SoundsGood] Couldn't open '{AUDIO_MANAGER_PATH}' to change the DSP Buffer Size. " +
                               "Set it manually in Project Settings > Audio.");
                return;
            }

            SerializedProperty requested = audioManager.FindProperty("m_RequestedDSPBufferSize");
            if (requested != null) requested.intValue = size;

            // The engine mirrors the requested value into this one; keep them in sync so the asset
            // doesn't sit in a half-updated state until audio restarts.
            SerializedProperty actual = audioManager.FindProperty("m_DSPBufferSize");
            if (actual != null) actual.intValue = size;

            audioManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        /// <summary> Index into <see cref="Sizes"/> for a buffer size, defaulting to "Default". </summary>
        internal static int IndexOfSize (int size)
        {
            for (int i = 0; i < Sizes.Length; i++)
            {
                if (Sizes[i] == size) return i;
            }

            return 0;
        }

        /// <summary>
        /// A human-readable summary of what the chosen buffer size costs in latency. "Default" has no
        /// fixed size, so it reports whatever the platform actually negotiated.
        /// </summary>
        internal static string DescribeLatency (int size)
        {
            int sampleRate = AudioSettings.outputSampleRate;
            if (sampleRate <= 0) sampleRate = 48000;

            int effective = size;
            string prefix = string.Empty;

            if (effective == 0)
            {
                effective = AudioSettings.GetConfiguration().dspBufferSize;
                prefix = "Platform default: ";
            }

            if (effective <= 0) return "Platform default (size negotiated at runtime).";

            float blockMs = effective / (float)sampleRate * 1000f;

            // Unity keeps more than one block in flight, so the delay actually heard is a small
            // multiple of a single block. Two is the usual figure and enough to size up the change.
            return $"{prefix}{effective} samples @ {sampleRate} Hz = {blockMs:0.0} ms per block " +
                   $"(~{blockMs * 2f:0.0} ms of output latency).";
        }

        private static SerializedObject GetAudioManager ()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(AUDIO_MANAGER_PATH);
            if (assets == null || assets.Length == 0 || assets[0] == null) return null;

            return new SerializedObject(assets[0]);
        }
    }
}
