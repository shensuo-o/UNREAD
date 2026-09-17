using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using MelenitasDev.SoundsGood.Domain;

namespace MelenitasDev.SoundsGood.Editor
{
    public class SoundsGoodBuildProcessor : IPreprocessBuildWithReport
    {
        // ----- Properties
        public int callbackOrder { get; } = 0;
    
        // ----- Public Methods
        public void OnPreprocessBuild (BuildReport report)
        {
            var runtimeData = SoundsGoodRuntimeData.Instance;
            if (runtimeData == null)
            {
                Debug.LogError("[Sounds Good] Runtime data asset not found before build. Audio references " +
                               "may be missing in the player. Open any Sounds Good window to regenerate it.");
                return;
            }

            // Editor-only reference resolution is stripped from the build, so resolve every
            // reference now and write it to disk. The build then reads valid serialized values.
            runtimeData.ResolveAndPersist();

            Debug.Log($"[Sounds Good] Build preprocess: references resolved and saved.\n" +
                      $"{runtimeData.SoundDataCollection}, {runtimeData.MusicDataCollection}, " +
                      $"{runtimeData.OutputDataCollection}, {runtimeData.SfxEnum}, " +
                      $"{runtimeData.TracksEnum}, {runtimeData.OutputsEnum}, " +
                      $"{runtimeData.MasterAudioMixer}");
        }
    }
}
