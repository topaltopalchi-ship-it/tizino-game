using UnityEditor;
using UnityEngine;

namespace Tiizino.EditorTools
{
    /// <summary>
    /// Assigns the approved, unmodified source artwork as the Android application icon.
    /// The source PNG must be added at Assets/_Tiizino/Art/tiizino-icon.png first.
    /// This tool deliberately does not crop, recolor, redraw, or otherwise edit the image.
    /// </summary>
    public static class TiizinoIconSetup
    {
        private const string SourcePath = "Assets/_Tiizino/Art/tiizino-icon.png";

        [MenuItem("Tools/Tiizino/Apply Official App Icon")]
        public static void ApplyOfficialIcon()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(SourcePath);
            if (icon == null)
            {
                EditorUtility.DisplayDialog(
                    "Tiizino icon not found",
                    "Add the exact approved original image at " + SourcePath + " first. No substitute will be generated.",
                    "OK");
                Debug.LogError("Tiizino official icon missing: " + SourcePath);
                return;
            }

            PlayerSettings.SetIcons(NamedBuildTarget.Android, new[] { icon }, IconKind.Application);
            EditorUtility.SetDirty(icon);
            AssetDatabase.SaveAssets();
            Debug.Log("Applied the approved Tiizino source image as the Android application icon without visual edits.");
            EditorUtility.DisplayDialog("Tiizino icon", "Official source image assigned to Android Player Settings.", "OK");
        }
    }
}
