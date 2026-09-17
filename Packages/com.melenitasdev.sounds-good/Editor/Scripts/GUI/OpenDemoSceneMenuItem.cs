using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using MelenitasDev.SoundsGood.Domain;

namespace MelenitasDev.SoundsGood.Editor
{
    public static class OpenDemoSceneMenuItem
    {
        [MenuItem("Tools/Sounds Good/Open 2D Demo Scene (with code)", priority = 100)]
        public static void OpenDemo2D ()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var demoAsset = AssetLocator.Instance.ShowcaseScene;
            if (demoAsset == null)
            {
                EditorUtility.DisplayDialog("Sounds Good", "Demo scene is not imported.", "OK");
                return;
            }

            string scenePath = AssetDatabase.GetAssetPath(demoAsset);
            if (string.IsNullOrEmpty(scenePath) || !File.Exists(scenePath))
            {
                EditorUtility.DisplayDialog("Sounds Good",
                    $"Demo scene asset exists but could not find it at path:\n{scenePath}\n", "OK");
                return;
            }

            EditorSceneManager.OpenScene(scenePath);
        }

        // The 3D demo is a paid feature. In the Free edition this entry is replaced by one
        // that explains what the scene shows (see ProFeatureMenus).
//#SG_PRO_BEGIN
        [MenuItem("Tools/Sounds Good/Open 3D Demo Scene (with emitters)", priority = 101)]
        public static void OpenDemo3D ()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var demoAsset = AssetLocator.Instance.OcclusionScene;
            if (demoAsset == null)
            {
                EditorUtility.DisplayDialog("Sounds Good", "Demo scene is not imported.", "OK");
                return;
            }

            string scenePath = AssetDatabase.GetAssetPath(demoAsset);
            if (string.IsNullOrEmpty(scenePath) || !File.Exists(scenePath))
            {
                EditorUtility.DisplayDialog("Sounds Good",
                    $"Demo scene asset exists but could not find it at path:\n{scenePath}\n", "OK");
                return;
            }

            EditorSceneManager.OpenScene(scenePath);
        }
//#SG_PRO_END
    }
}