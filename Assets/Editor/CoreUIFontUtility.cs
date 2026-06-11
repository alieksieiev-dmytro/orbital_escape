#if UNITY_EDITOR
using System;
using OrbitalEscape.Visuals.Palettes;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace OrbitalEscape.Editor
{
    public static class CoreUIFontUtility
    {
        private const string TmpRoot = "Assets/Art/Fonts/CoreUI/TMP";
        private const string MaterialRoot = TmpRoot + "/Materials";
        private const string SelectedFontPath = TmpRoot + "/Rajdhani SDF.asset";
        private const string SelectedGlowMaterialPath = MaterialRoot + "/Rajdhani NeonGlow.mat";

        [MenuItem("Orbital Escape/UI/Apply Rajdhani To Current UI Prefabs")]
        public static void ApplyRajdhaniToCurrentUIPrefabs()
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SelectedFontPath);
            var glowMaterial = AssetDatabase.LoadAssetAtPath<Material>(SelectedGlowMaterialPath);

            if (fontAsset == null || glowMaterial == null)
            {
                throw new InvalidOperationException("Rajdhani TMP font assets are missing from " + TmpRoot);
            }

            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/Prefabs/UI" });
            foreach (var prefabGuid in prefabGuids)
            {
                var prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                var root = PrefabUtility.LoadPrefabContents(prefabPath);

                try
                {
                    foreach (var textComponent in root.GetComponentsInChildren<TMP_Text>(true))
                    {
                        bool useGlow = ShouldUseGlow(textComponent);
                        textComponent.font = fontAsset;
                        textComponent.fontSharedMaterial = useGlow ? glowMaterial : fontAsset.material;
                        SyncTextColorTarget(textComponent);
                        SyncGlowTarget(textComponent, useGlow);
                        textComponent.SetAllDirty();
                        EditorUtility.SetDirty(textComponent);
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Rajdhani applied to current UI prefabs.");
        }

        private static bool ShouldUseGlow(TMP_Text textComponent)
        {
            var objectName = textComponent.gameObject.name.ToLowerInvariant();
            var text = textComponent.text.ToLowerInvariant();

            return objectName.Contains("score")
                || objectName.Contains("record")
                || objectName.Contains("title")
                || text.Contains("game over");
        }

        private static void SyncTextColorTarget(TMP_Text textComponent)
        {
            var colorTarget = textComponent.GetComponent<PaletteTextColorTarget>();
            if (colorTarget == null)
            {
                colorTarget = textComponent.gameObject.AddComponent<PaletteTextColorTarget>();
            }

            EditorUtility.SetDirty(colorTarget);
        }

        private static void SyncGlowTarget(TMP_Text textComponent, bool useGlow)
        {
            var glowTarget = textComponent.GetComponent<PaletteTextGlowTarget>();

            if (useGlow)
            {
                if (glowTarget == null)
                {
                    glowTarget = textComponent.gameObject.AddComponent<PaletteTextGlowTarget>();
                }

                EditorUtility.SetDirty(glowTarget);
                return;
            }

            if (glowTarget != null)
            {
                UnityEngine.Object.DestroyImmediate(glowTarget);
            }
        }
    }
}
#endif
