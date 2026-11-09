using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace ProjectTools
{
    /// <summary>
    /// Sets up TextMesh Pro for the whole project:
    ///  1. Imports the TMP Essential Resources (if they are missing).
    ///  2. Builds TMP font assets for the shared fonts in Assets/Shared/Fonts.
    ///  3. Converts any legacy UI Text / TextMesh components to TextMesh Pro.
    ///
    /// Menu: Tools > Project > Text > ...
    /// </summary>
    public static class TextSetup
    {
        public const string FontRoot = "Assets/Shared/Fonts";

        // The fonts used across the project (all SIL Open Font License, see the OFL.txt next to each).
        public const string BodyFont = FontRoot + "/TitilliumWeb/TitilliumWeb-Regular SDF.asset";
        public const string BodyBoldFont = FontRoot + "/TitilliumWeb/TitilliumWeb-Bold SDF.asset";
        public const string HeadingFont = FontRoot + "/Rajdhani/Rajdhani-Bold SDF.asset";
        public const string HeadingOutlineMaterial = FontRoot + "/Rajdhani/Rajdhani-Bold SDF Outline.mat";
        public const string BodyOutlineMaterial = FontRoot + "/TitilliumWeb/TitilliumWeb-Bold SDF Outline.mat";

        private const string Ascii =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~°±×÷–—‘’“”•…";

        [MenuItem("Tools/Project/Text/1. Import TMP Essentials")]
        public static void ImportEssentials()
        {
            if (AssetDatabase.FindAssets("t:TMP_Settings").Length > 0)
            {
                Debug.Log("[Text] TMP Essential Resources already imported.");
                return;
            }

            string package = Path.GetFullPath("Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");
            AssetDatabase.ImportPackage(package, false);
            AssetDatabase.Refresh();
            Debug.Log("[Text] Imported TMP Essential Resources.");
        }

        /// <summary>Batch mode version: package imports finish after the method returns, so exit when it completes.</summary>
        public static void ImportEssentialsBatch()
        {
            if (AssetDatabase.FindAssets("t:TMP_Settings").Length > 0)
            {
                EditorApplication.Exit(0);
                return;
            }
            AssetDatabase.importPackageCompleted += _ => { AssetDatabase.Refresh(); EditorApplication.Exit(0); };
            AssetDatabase.importPackageFailed += (_, error) => { Debug.LogError("[Text] TMP import failed: " + error); EditorApplication.Exit(1); };
            ImportEssentials();
        }

        [MenuItem("Tools/Project/Text/2. Build Font Assets")]
        public static void BuildFontAssets()
        {
            foreach (string ttf in AssetDatabase.FindAssets("t:Font", new[] { FontRoot }).Select(AssetDatabase.GUIDToAssetPath))
            {
                string assetPath = Path.ChangeExtension(ttf, null) + " SDF.asset";
                if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath) != null)
                    continue;

                Font font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
                // Dynamic: any character missing from the atlas is added when it is first used.
                TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                    AtlasPopulationMode.Dynamic, true);
                fontAsset.name = Path.GetFileNameWithoutExtension(assetPath);
                AssetDatabase.CreateAsset(fontAsset, assetPath);

                fontAsset.atlasTextures[0].name = fontAsset.name + " Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
                fontAsset.material.name = fontAsset.name + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

                fontAsset.TryAddCharacters(Ascii, out _);
                EditorUtility.SetDirty(fontAsset);
                Debug.Log("[Text] Created " + assetPath);
            }
            AssetDatabase.SaveAssets();

            // Outlined versions for world-space labels, so text stays readable against both bright walls and dark sky.
            CreateOutlineMaterial(HeadingFont, HeadingOutlineMaterial);
            CreateOutlineMaterial(BodyBoldFont, BodyOutlineMaterial);

            SetProjectDefaultFont();
            AssetDatabase.SaveAssets();
        }

        private static void CreateOutlineMaterial(string fontPath, string materialPath)
        {
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (fontAsset == null)
                return;

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (mat == null)
            {
                mat = new Material(fontAsset.material);
                AssetDatabase.CreateAsset(mat, materialPath);
            }
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.18f);
            mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.05f, 0.06f, 0.08f, 1f));
            mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.1f);
            mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
            EditorUtility.SetDirty(mat);
        }

        private static void SetProjectDefaultFont()
        {
            TMP_FontAsset body = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BodyFont);
            string settingsGuid = AssetDatabase.FindAssets("t:TMP_Settings").FirstOrDefault();
            if (body == null || settingsGuid == null)
                return;

            TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(AssetDatabase.GUIDToAssetPath(settingsGuid));
            SerializedObject so = new SerializedObject(settings);
            so.FindProperty("m_defaultFontAsset").objectReferenceValue = body;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Text] Default TMP font set to " + body.name);
        }

        // ------------------------------------------------------------------
        // Legacy text conversion
        // ------------------------------------------------------------------

        [MenuItem("Tools/Project/Text/3. Convert Legacy Text In All Scenes And Prefabs")]
        public static void ConvertAll()
        {
            int converted = 0;

            foreach (string prefabPath in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                int count = ConvertUnder(root);
                if (count > 0)
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                PrefabUtility.UnloadPrefabContents(root);
                converted += count;
            }

            foreach (string scenePath in ProjectSurvey.AllScenePaths())
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                int count = scene.GetRootGameObjects().Sum(ConvertUnder);
                if (count > 0)
                    EditorSceneManager.SaveScene(scene);
                converted += count;
            }

            Debug.Log($"[Text] Converted {converted} legacy text components to TextMesh Pro.");
        }

        /// <summary>Converts every legacy Text / TextMesh under 'root'. Prefab instances are left to their prefab asset.</summary>
        private static int ConvertUnder(GameObject root)
        {
            int count = 0;
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BodyFont);

            foreach (Text text in root.GetComponentsInChildren<Text>(true).Where(t => !PrefabUtility.IsPartOfPrefabInstance(t)))
            {
                GameObject go = text.gameObject;
                string value = text.text;
                float size = text.fontSize;
                Color color = text.color;
                TextAnchor anchor = text.alignment;
                bool wrap = text.horizontalOverflow == HorizontalWrapMode.Wrap;
                bool raycast = text.raycastTarget;
                bool bold = text.fontStyle == FontStyle.Bold || text.fontStyle == FontStyle.BoldAndItalic;
                bool autoSize = text.resizeTextForBestFit;
                int minSize = text.resizeTextMinSize, maxSize = text.resizeTextMaxSize;

                Object.DestroyImmediate(text, true);
                TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
                if (font != null) tmp.font = font;
                tmp.text = value;
                tmp.fontSize = size;
                tmp.color = color;
                tmp.alignment = ToTmpAlignment(anchor);
                tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
                tmp.raycastTarget = raycast;
                tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
                tmp.enableAutoSizing = autoSize;
                tmp.fontSizeMin = minSize;
                tmp.fontSizeMax = maxSize;
                count++;
            }

            foreach (TextMesh mesh in root.GetComponentsInChildren<TextMesh>(true).Where(t => !PrefabUtility.IsPartOfPrefabInstance(t)))
            {
                GameObject go = mesh.gameObject;
                string value = mesh.text;
                // TextMesh line height is roughly fontSize x characterSize / 10 world units; TMP uses fontSize / 10.
                float size = (mesh.fontSize == 0 ? 13 : mesh.fontSize) * mesh.characterSize;
                Color color = mesh.color;
                TextAnchor anchor = mesh.anchor;

                Object.DestroyImmediate(go.GetComponent<MeshRenderer>(), true);
                Object.DestroyImmediate(mesh, true);
                TextMeshPro tmp = go.AddComponent<TextMeshPro>();
                if (font != null) tmp.font = font;
                tmp.text = value;
                tmp.fontSize = size;
                tmp.color = color;
                tmp.alignment = ToTmpAlignment(anchor);
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                count++;
            }
            return count;
        }

        private static TextAlignmentOptions ToTmpAlignment(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
        }
    }
}
