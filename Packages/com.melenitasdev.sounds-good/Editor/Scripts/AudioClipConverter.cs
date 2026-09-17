/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using MelenitasDev.SoundsGood.Domain;
using UnityEditor;
using UnityEngine;

namespace MelenitasDev.SoundsGood.Editor
{
    /// <summary>
    /// Measures the silence at the start of every registered audio clip and rewrites it as a trimmed
    /// WAV. That silence is what makes a sound feel late: an MP3 always carries the encoder's padding
    /// plus the decoder's own delay, and Unity bakes both into the audio it decodes at import.
    ///
    /// A compressed clip can't be trimmed without re-encoding it (which puts the padding straight
    /// back), so trimming means going through WAV. Unity has already decoded the file, so the samples
    /// are read from the AudioClip itself and no third-party decoder is needed.
    /// </summary>
    internal static class AudioClipConverter
    {
        /// <summary> Default silence threshold. Well under anything audible, but above dither noise. </summary>
        internal const float DEFAULT_SILENCE_DB = -60f;

        /// <summary>
        /// Silence kept before the first audible sample, in milliseconds. Cutting exactly on the
        /// transient leaves the file starting mid-waveform, which clicks; a millisecond of the
        /// silence that was already there can't be heard, and is a fortieth of what Unity's default
        /// DSP buffer costs anyway.
        /// </summary>
        internal const float DEFAULT_KEEP_BEFORE_ATTACK_MS = 1f;

        /// <summary> Past this there is no more click to avoid, only silence being kept for nothing. </summary>
        internal const float MAX_KEEP_BEFORE_ATTACK_MS = 20f;

        private const int OUTPUT_BITS_PER_SAMPLE = 16;

        internal enum CollectionKind { Sfx, Track }

        /// <summary> One registered clip, with whatever the scan could measure about it. </summary>
        internal class ClipEntry
        {
            internal AudioClip Clip;
            internal string Tag;
            internal CollectionKind Kind;
            internal string AssetPath;
            internal string Extension;
            internal bool IsWav;
            internal int LeadingSilenceFrames;
            internal float LeadingSilenceMs;
            internal bool Analyzed;
            internal string Error;

            /// <summary> Trimming is the risky half, so Tracks opt out by default (see the window). </summary>
            internal bool Selected;
        }

        private struct Pcm
        {
            internal float[] Samples;
            internal int Frequency;
            internal int Channels;
        }

        internal static float DbToAmplitude (float db) => Mathf.Pow(10f, db / 20f);

        // ----- Scanning

        /// <summary>
        /// Every clip registered in the Sound and Music collections, measured. Duplicate clips
        /// (the same asset under two tags) are reported once, since converting is per file.
        /// </summary>
        internal static List<ClipEntry> Scan (float silenceDb)
        {
            var entries = new List<ClipEntry>();
            var seen = new HashSet<string>();

            Collect(AssetLocator.Instance.SoundDataCollection?.Sounds, CollectionKind.Sfx, entries, seen);
            Collect(AssetLocator.Instance.MusicDataCollection?.MusicTracks, CollectionKind.Track, entries, seen);

            try
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("Sounds Good", $"Analyzing {entries[i].Clip.name}…",
                        (i + 1) / (float)entries.Count);
                    Analyze(entries[i], silenceDb);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return entries;
        }

        private static void Collect (SoundData[] datas, CollectionKind kind, List<ClipEntry> entries,
            HashSet<string> seen)
        {
            if (datas == null) return;

            foreach (SoundData data in datas)
            {
                if (data.Clips == null) continue;

                foreach (AudioClip clip in data.Clips)
                {
                    if (clip == null) continue;

                    string path = AssetDatabase.GetAssetPath(clip);
                    if (string.IsNullOrEmpty(path) || !seen.Add(path)) continue;

                    string extension = Path.GetExtension(path).ToLowerInvariant();

                    entries.Add(new ClipEntry
                    {
                        Clip = clip,
                        Tag = data.Tag,
                        Kind = kind,
                        AssetPath = path,
                        Extension = extension,
                        IsWav = extension == ".wav",
                        Selected = kind == CollectionKind.Sfx
                    });
                }
            }
        }

        /// <summary>
        /// Measuring only needs to know where the audio starts, and the project's own compressed
        /// format is accurate enough for that — so the scan reads the clip as it is imported and
        /// never reimports anything. Only the conversion pays for a clean PCM decode.
        /// </summary>
        private static void Analyze (ClipEntry entry, float silenceDb)
        {
            entry.Analyzed = false;
            entry.Error = null;

            if (!TryReadPcm(entry.Clip, false, out Pcm pcm, out string error))
            {
                entry.Error = error;
                return;
            }

            entry.LeadingSilenceFrames = CountLeadingSilenceFrames(pcm, DbToAmplitude(silenceDb));
            entry.LeadingSilenceMs = pcm.Frequency > 0
                ? entry.LeadingSilenceFrames / (float)pcm.Frequency * 1000f
                : 0f;
            entry.Analyzed = true;
        }

        private static int CountLeadingSilenceFrames (Pcm pcm, float threshold)
        {
            int channels = Mathf.Max(1, pcm.Channels);
            int frames = pcm.Samples.Length / channels;

            for (int frame = 0; frame < frames; frame++)
            {
                for (int channel = 0; channel < channels; channel++)
                {
                    if (Mathf.Abs(pcm.Samples[frame * channels + channel]) > threshold) return frame;
                }
            }

            return frames;
        }

        // ----- Conversion

        /// <summary>
        /// Rewrites the entry's clip as a WAV, optionally trimming the silence at its start, and
        /// repoints the collections when the result is a new asset. <paramref name="supersededPath"/>
        /// comes back with the original file the clip no longer needs, or null if nothing was replaced.
        /// </summary>
        /// <param name="keepBeforeAttackMs">Milliseconds of the original silence left in front of the
        /// first audible sample. 0 cuts right on the transient, which can click.</param>
        internal static bool Convert (ClipEntry entry, bool trim, float silenceDb, float keepBeforeAttackMs,
            out string supersededPath, out string message)
        {
            supersededPath = null;

            if (!TryReadPcm(entry.Clip, true, out Pcm pcm, out string readError))
            {
                message = readError;
                return false;
            }

            int trimFrames = 0;
            if (trim)
            {
                int silence = CountLeadingSilenceFrames(pcm, DbToAmplitude(silenceDb));
                float keepMs = Mathf.Clamp(keepBeforeAttackMs, 0f, MAX_KEEP_BEFORE_ATTACK_MS);
                int guard = Mathf.RoundToInt(keepMs / 1000f * pcm.Frequency);
                trimFrames = Mathf.Max(0, silence - guard);

                // A clip that is silent end to end would be trimmed away entirely; leave it alone.
                if (trimFrames >= pcm.Samples.Length / Mathf.Max(1, pcm.Channels)) trimFrames = 0;
            }

            bool replacingInPlace = entry.IsWav;

            if (trimFrames == 0 && replacingInPlace)
            {
                message = "Already a WAV and nothing to trim.";
                return false;
            }

            float[] output = trimFrames > 0 ? Slice(pcm, trimFrames) : pcm.Samples;

            string targetPath = replacingInPlace
                ? entry.AssetPath
                : Path.ChangeExtension(entry.AssetPath, ".wav");

            // Converting "Foo.mp3" when a different "Foo.wav" already exists must not clobber it.
            if (!replacingInPlace && File.Exists(targetPath))
                targetPath = AssetDatabase.GenerateUniqueAssetPath(targetPath);

            SoundData owner = FindOwner(entry.Clip, out CompressionPreset preset, out bool forceToMono);

            File.WriteAllBytes(targetPath, EncodeWav(output, pcm.Frequency, pcm.Channels));
            AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);

            // Restore the Sounds Good preset before taking a reference: that reimports the asset,
            // which can invalidate any AudioClip loaded beforehand.
            if (owner != null)
            {
                AudioClip imported = AssetDatabase.LoadAssetAtPath<AudioClip>(targetPath);
                if (imported != null)
                    EditorHelper.ChangeAudioClipImportSettings(new[] { imported }, preset, forceToMono);
            }

            AudioClip written = AssetDatabase.LoadAssetAtPath<AudioClip>(targetPath);
            if (written == null)
            {
                message = $"Wrote '{targetPath}' but Unity didn't import it as an AudioClip.";
                return false;
            }

            // Rewriting a WAV in place keeps its GUID, so only a brand new asset needs repointing.
            if (!replacingInPlace)
            {
                Repoint(entry.Clip, written);
                supersededPath = entry.AssetPath;
            }

            entry.Clip = written;
            entry.AssetPath = targetPath;
            entry.Extension = ".wav";
            entry.IsWav = true;
            Analyze(entry, silenceDb);

            float trimmedMs = trimFrames / (float)pcm.Frequency * 1000f;
            message = trimFrames > 0
                ? $"Trimmed {trimmedMs:0.0} ms" + (replacingInPlace ? "." : " and converted to WAV.")
                : "Converted to WAV.";
            return true;
        }

        private static float[] Slice (Pcm pcm, int trimFrames)
        {
            int offset = trimFrames * pcm.Channels;
            var sliced = new float[pcm.Samples.Length - offset];
            System.Array.Copy(pcm.Samples, offset, sliced, 0, sliced.Length);
            return sliced;
        }

        /// <summary> Swaps every reference to the old clip in both collections for the new one. </summary>
        private static void Repoint (AudioClip from, AudioClip to)
        {
            var soundCollection = AssetLocator.Instance.SoundDataCollection;
            var musicCollection = AssetLocator.Instance.MusicDataCollection;

            bool changed = RepointIn(soundCollection?.Sounds, from, to);
            changed |= RepointIn(musicCollection?.MusicTracks, from, to);

            if (!changed) return;

            if (soundCollection != null) EditorUtility.SetDirty(soundCollection);
            if (musicCollection != null) EditorUtility.SetDirty(musicCollection);
            AssetDatabase.SaveAssets();
        }

        private static bool RepointIn (SoundData[] datas, AudioClip from, AudioClip to)
        {
            if (datas == null) return false;

            bool changed = false;

            foreach (SoundData data in datas)
            {
                AudioClip[] clips = data.Clips;
                if (clips == null) continue;

                for (int i = 0; i < clips.Length; i++)
                {
                    if (clips[i] != from) continue;
                    clips[i] = to;
                    changed = true;
                }

                data.Clips = clips;
            }

            return changed;
        }

        /// <summary> The SoundData holding this clip, so the new file keeps its compression preset. </summary>
        private static SoundData FindOwner (AudioClip clip, out CompressionPreset preset, out bool forceToMono)
        {
            preset = CompressionPreset.FrequentSound;
            forceToMono = false;

            SoundData found = FindOwnerIn(AssetLocator.Instance.SoundDataCollection?.Sounds, clip)
                              ?? FindOwnerIn(AssetLocator.Instance.MusicDataCollection?.MusicTracks, clip);

            if (found == null) return null;

            preset = found.CompressionPreset;
            forceToMono = found.ForceToMono;
            return found;
        }

        private static SoundData FindOwnerIn (SoundData[] datas, AudioClip clip)
        {
            if (datas == null) return null;

            foreach (SoundData data in datas)
            {
                if (data.Clips == null) continue;

                foreach (AudioClip candidate in data.Clips)
                {
                    if (candidate == clip) return data;
                }
            }

            return null;
        }

        // ----- Decoding

        /// <summary>
        /// The clip's samples. Reading it as imported costs nothing and is accurate enough to find
        /// where the audio starts, so that's the default. <paramref name="losslessDecode"/> asks for a
        /// temporary PCM reimport instead, which is what writing a file needs: going through the
        /// project's compressed format would decode the audio a second time and lose quality.
        /// The reimport is the fragile half, so it always falls back to reading the clip as it is.
        /// </summary>
        private static bool TryReadPcm (AudioClip clip, bool losslessDecode, out Pcm pcm, out string error)
        {
            pcm = default;
            error = null;

            if (clip.samples <= 0)
            {
                error = "Clip reports no samples.";
                return false;
            }

            string path = AssetDatabase.GetAssetPath(clip);
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;

            if (importer == null)
            {
                error = $"No AudioImporter for '{path}'.";
                return false;
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;

            // Force To Mono downmixes the clip Unity hands back, so reading it that way and writing
            // the result would permanently collapse a stereo source to one channel.
            bool readableAsIs = clip.loadType == AudioClipLoadType.DecompressOnLoad && !importer.forceToMono;
            bool alreadyLossless = settings.compressionFormat == AudioCompressionFormat.PCM;

            if (!losslessDecode && readableAsIs && TryGetData(clip, out pcm)) return true;

            if (losslessDecode && readableAsIs && alreadyLossless && TryGetData(clip, out pcm)) return true;

            if (TryReadWithTemporaryPcmImport(importer, path, out pcm)) return true;

            // The reimport didn't work out; the clip as imported is still better than nothing.
            if (readableAsIs && TryGetData(clip, out pcm)) return true;

            error = $"Unity couldn't decode this clip (load type: {clip.loadType}, " +
                    $"format: {settings.compressionFormat}, state: {clip.loadState}, " +
                    $"force to mono: {importer.forceToMono}).";
            return false;
        }

        private static bool TryReadWithTemporaryPcmImport (AudioImporter importer, string path, out Pcm pcm)
        {
            pcm = default;

            AudioImporterSampleSettings original = importer.defaultSampleSettings;
            bool originalForceToMono = importer.forceToMono;
            bool originalLoadInBackground = importer.loadInBackground;

            try
            {
                AudioImporterSampleSettings temporary = original;
                temporary.loadType = AudioClipLoadType.DecompressOnLoad;
                temporary.compressionFormat = AudioCompressionFormat.PCM;
                temporary.preloadAudioData = true;

                importer.defaultSampleSettings = temporary;
                importer.forceToMono = false;

                // Sounds Good imports every clip with Load In Background on, which makes loading
                // asynchronous — GetData right after the reimport would find nothing ready yet.
                importer.loadInBackground = false;
                importer.SaveAndReimport();

                AudioClip decoded = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                return decoded != null && TryGetData(decoded, out pcm);
            }
            finally
            {
                importer.defaultSampleSettings = original;
                importer.forceToMono = originalForceToMono;
                importer.loadInBackground = originalLoadInBackground;
                importer.SaveAndReimport();
            }
        }

        private static bool TryGetData (AudioClip clip, out Pcm pcm)
        {
            pcm = default;

            if (!EnsureLoaded(clip)) return false;

            var buffer = new float[clip.samples * clip.channels];
            if (!clip.GetData(buffer, 0)) return false;

            pcm = new Pcm { Samples = buffer, Frequency = clip.frequency, Channels = clip.channels };
            return true;
        }

        /// <summary>
        /// GetData only returns anything once the audio data is in memory. With Load In Background on
        /// that happens on another thread, so the load is kicked off and waited on before reading.
        /// </summary>
        private static bool EnsureLoaded (AudioClip clip)
        {
            if (clip.loadState == AudioDataLoadState.Loaded) return true;

            clip.LoadAudioData();

            const int POLL_MS = 10;
            const int TIMEOUT_MS = 5000;

            for (int waited = 0; waited < TIMEOUT_MS && clip.loadState == AudioDataLoadState.Loading; waited += POLL_MS)
            {
                Thread.Sleep(POLL_MS);
            }

            return clip.loadState == AudioDataLoadState.Loaded;
        }

        // ----- WAV writing

        /// <summary> A canonical 16-bit PCM RIFF/WAVE file. </summary>
        private static byte[] EncodeWav (float[] samples, int frequency, int channels)
        {
            int bytesPerSample = OUTPUT_BITS_PER_SAMPLE / 8;
            int dataBytes = samples.Length * bytesPerSample;

            using var stream = new MemoryStream(44 + dataBytes);
            using var writer = new BinaryWriter(stream, Encoding.ASCII);

            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataBytes);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);                                               // PCM header length
            writer.Write((short)1);                                         // format: PCM
            writer.Write((short)channels);
            writer.Write(frequency);
            writer.Write(frequency * channels * bytesPerSample);            // byte rate
            writer.Write((short)(channels * bytesPerSample));               // block align
            writer.Write((short)OUTPUT_BITS_PER_SAMPLE);

            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataBytes);

            foreach (float sample in samples)
            {
                writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
            }

            writer.Flush();
            return stream.ToArray();
        }
    }
}
