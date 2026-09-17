/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MelenitasDev.SoundsGood.Editor
{
    /// <summary>
    /// Lists every registered clip with the silence measured at its start, and rewrites the ones you
    /// pick as trimmed WAVs. See <see cref="AudioClipConverter"/> for why that silence is there and
    /// why trimming has to go through WAV.
    /// </summary>
    public class AudioSilenceTrimmerWindow : EditorWindow
    {
        // ----- Serialized
        [SerializeField] private VisualTreeAsset tree;

        // ----- UI Query
        private SG_BoolField convertToWavField => rootVisualElement.Q<SG_BoolField>("ConvertToWavField");
        private SG_BoolField trimSilenceField => rootVisualElement.Q<SG_BoolField>("TrimSilenceField");
        private SG_FloatField silenceThresholdField => rootVisualElement.Q<SG_FloatField>("SilenceThresholdField");
        private SG_Slider keepBeforeAttackField => rootVisualElement.Q<SG_Slider>("KeepBeforeAttackField");
        private VisualElement rowsContainer => rootVisualElement.Q<VisualElement>("RowsContainer");
        private Label summaryLabel => rootVisualElement.Q<Label>("SummaryLabel");
        private Button scanButton => rootVisualElement.Q<Button>("ScanButton");
        private Button selectAllButton => rootVisualElement.Q<Button>("SelectAllButton");
        private Button selectNoneButton => rootVisualElement.Q<Button>("SelectNoneButton");
        private Button applyButton => rootVisualElement.Q<Button>("ApplyButton");

        // ----- Fields
        private readonly List<AudioClipConverter.ClipEntry> entries = new List<AudioClipConverter.ClipEntry>();

        /// <summary> Silence under this is normal head padding, not something worth flagging. </summary>
        private const float NOTABLE_SILENCE_MS = 5f;

        private static readonly Color TextColor = new Color(0.910f, 0.792f, 0.643f);
        private static readonly Color DimColor = new Color(0.769f, 0.627f, 0.463f);
        private static readonly Color WarnColor = new Color(0.973f, 0.612f, 0.204f);

        // ----- Menu
        [MenuItem("Tools/Sounds Good/Audio Silence Trimmer", false, 55)]
        public static void ShowWindow ()
        {
            var window = GetWindow<AudioSilenceTrimmerWindow>();
            window.titleContent = new GUIContent("Audio Silence Trimmer");
            window.minSize = new Vector2(520, 460);
        }

        // ----- Unity Callbacks
        private void CreateGUI ()
        {
            if (tree == null)
            {
                Debug.LogError("[SoundsGood] Audio Silence Trimmer window VisualTreeAsset is not assigned.");
                return;
            }

            tree.CloneTree(rootVisualElement);

            if (scanButton != null) scanButton.clicked += Scan;
            if (selectAllButton != null) selectAllButton.clicked += () => SetAllSelected(true);
            if (selectNoneButton != null) selectNoneButton.clicked += () => SetAllSelected(false);
            if (applyButton != null) applyButton.clicked += ApplyToSelected;

            RefreshRows();
        }

        // ----- Private Methods
        private float SilenceDb => silenceThresholdField != null
            ? silenceThresholdField.Value
            : AudioClipConverter.DEFAULT_SILENCE_DB;

        // The slider's own range keeps this within 0..MAX_KEEP_BEFORE_ATTACK_MS, so there's nothing
        // to clamp here.
        private float KeepBeforeAttackMs => keepBeforeAttackField != null
            ? keepBeforeAttackField.Value
            : AudioClipConverter.DEFAULT_KEEP_BEFORE_ATTACK_MS;

        private void Scan ()
        {
            entries.Clear();
            entries.AddRange(AudioClipConverter.Scan(SilenceDb));
            RefreshRows();
            LogUnreadable();
        }

        /// <summary> A clip that can't be read is worth a console entry: the row can only show a dash. </summary>
        private void LogUnreadable ()
        {
            List<AudioClipConverter.ClipEntry> failed = entries.Where(entry => !entry.Analyzed).ToList();
            if (failed.Count == 0) return;

            string detail = string.Join("\n", failed.Select(entry => $"  {entry.AssetPath} — {entry.Error}"));
            Debug.LogWarning($"[SoundsGood] Audio Silence Trimmer couldn't read {failed.Count} clip(s):\n{detail}");
        }

        private void SetAllSelected (bool selected)
        {
            foreach (AudioClipConverter.ClipEntry entry in entries) entry.Selected = selected;
            RefreshRows();
        }

        private void RefreshRows ()
        {
            if (rowsContainer == null) return;

            rowsContainer.Clear();

            foreach (AudioClipConverter.ClipEntry entry in entries) rowsContainer.Add(BuildRow(entry));

            RefreshSummary();
        }

        private VisualElement BuildRow (AudioClipConverter.ClipEntry entry)
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingTop = 2, paddingBottom = 2, paddingLeft = 4, paddingRight = 4
                }
            };

            var toggle = new Toggle { value = entry.Selected, style = { width = 22, marginLeft = 0 } };
            toggle.RegisterValueChangedCallback(evt =>
            {
                entry.Selected = evt.newValue;
                RefreshSummary();
            });
            row.Add(toggle);

            row.Add(MakeLabel(entry.Tag, 90, TextColor));

            var fileLabel = MakeLabel(Path.GetFileName(entry.AssetPath), 0, TextColor);
            fileLabel.style.flexGrow = 1;
            fileLabel.tooltip = entry.AssetPath;
            row.Add(fileLabel);

            row.Add(MakeLabel(entry.Extension.TrimStart('.').ToUpperInvariant(), 42,
                entry.IsWav ? DimColor : WarnColor));

            string silenceText;
            Color silenceColor;

            if (!entry.Analyzed)
            {
                silenceText = "—";
                silenceColor = DimColor;
                fileLabel.tooltip = entry.Error ?? entry.AssetPath;
            }
            else
            {
                silenceText = $"{entry.LeadingSilenceMs:0.0} ms";
                silenceColor = entry.LeadingSilenceMs >= NOTABLE_SILENCE_MS ? WarnColor : DimColor;
            }

            Label silenceLabel = MakeLabel(silenceText, 70, silenceColor);
            silenceLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            row.Add(silenceLabel);

            return row;
        }

        private static Label MakeLabel (string text, float width, Color color)
        {
            var label = new Label(text)
            {
                style =
                {
                    color = color,
                    fontSize = 11,
                    overflow = Overflow.Hidden,
                    textOverflow = TextOverflow.Ellipsis
                }
            };

            if (width > 0f)
            {
                label.style.width = width;
                label.style.flexShrink = 0;
            }

            return label;
        }

        private void RefreshSummary ()
        {
            if (summaryLabel == null) return;

            if (entries.Count == 0)
            {
                summaryLabel.text = "Press Scan to measure every registered clip.";
                return;
            }

            int selected = entries.Count(entry => entry.Selected);
            int notable = entries.Count(entry => entry.Analyzed && entry.LeadingSilenceMs >= NOTABLE_SILENCE_MS);
            int failed = entries.Count(entry => !entry.Analyzed);

            string text = $"{entries.Count} clips · {notable} with more than {NOTABLE_SILENCE_MS:0} ms " +
                          $"of leading silence · {selected} selected";

            if (failed > 0) text += $" · {failed} couldn't be read";

            summaryLabel.text = text;
        }

        private void ApplyToSelected ()
        {
            List<AudioClipConverter.ClipEntry> targets = entries.Where(entry => entry.Selected).ToList();

            if (targets.Count == 0)
            {
                EditorUtility.DisplayDialog("Audio Silence Trimmer", "No clips selected.", "Ok");
                return;
            }

            bool convert = convertToWavField == null || convertToWavField.Value;
            bool trim = trimSilenceField == null || trimSilenceField.Value;

            if (!convert && !trim)
            {
                EditorUtility.DisplayDialog("Audio Silence Trimmer",
                    "Both options are off, so there's nothing to do.", "Ok");
                return;
            }

            var superseded = new List<string>();
            int changed = 0;
            var failures = new List<string>();

            try
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    AudioClipConverter.ClipEntry entry = targets[i];

                    EditorUtility.DisplayProgressBar("Sounds Good", $"Processing {entry.Clip.name}…",
                        (i + 1) / (float)targets.Count);

                    // Converting is what makes trimming possible, so a non-WAV needs it enabled.
                    if (!entry.IsWav && !convert) continue;

                    if (!AudioClipConverter.Convert(entry, trim, SilenceDb, KeepBeforeAttackMs,
                            out string obsolete, out string message))
                    {
                        failures.Add($"{Path.GetFileName(entry.AssetPath)}: {message}");
                        continue;
                    }

                    if (!string.IsNullOrEmpty(obsolete)) superseded.Add(obsolete);
                    changed++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            RefreshRows();
            OfferToDeleteSuperseded(superseded);
            ReportResult(changed, failures);
        }

        /// <summary>
        /// The originals are still on disk after a conversion. Deleting them is offered rather than
        /// done, because the collections have been repointed but anything else in the project that
        /// references them (a plain AudioSource in a scene) has not.
        /// </summary>
        private static void OfferToDeleteSuperseded (List<string> superseded)
        {
            if (superseded.Count == 0) return;

            const int PREVIEW = 8;
            string list = string.Join("\n", superseded.Take(PREVIEW).Select(Path.GetFileName));
            if (superseded.Count > PREVIEW) list += $"\n…and {superseded.Count - PREVIEW} more";

            bool delete = EditorUtility.DisplayDialog(
                "Delete the original files?",
                $"{superseded.Count} file(s) have been replaced by a WAV, and the Sounds Good " +
                $"collections now point at the new ones.\n\n{list}\n\n" +
                "Anything else referencing them directly (an AudioSource in a scene, another asset) " +
                "will lose its reference if you delete them.",
                "Delete", "Keep them");

            if (!delete) return;

            foreach (string path in superseded) AssetDatabase.DeleteAsset(path);
            AssetDatabase.Refresh();
        }

        private static void ReportResult (int changed, List<string> failures)
        {
            if (failures.Count == 0)
            {
                EditorUtility.DisplayDialog("Audio Silence Trimmer",
                    changed == 0 ? "Nothing needed changing." : $"{changed} clip(s) updated.", "Ok");
                return;
            }

            EditorUtility.DisplayDialog("Audio Silence Trimmer",
                $"{changed} clip(s) updated.\n\n{failures.Count} couldn't be processed:\n" +
                string.Join("\n", failures.Take(8)),
                "Ok");
        }
    }
}
