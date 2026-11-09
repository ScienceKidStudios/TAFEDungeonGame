using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TextCore.LowLevel;
using TMPro;
using Object = UnityEngine.Object;

namespace PuzzleMechanics.EditorTools
{
    /// <summary>
    /// Builds every dungeon model, material, font, prefab and both example scenes from code.
    ///
    /// Menu: Tools > Dungeon Kit > Generate Everything
    ///
    /// Running it again is safe: materials and prefabs are updated in place (so scenes that use
    /// them keep working) and the two example scenes are rebuilt from scratch.
    ///
    /// Every model is built from Unity primitives arranged as child objects. Moving parts
    /// (switch stones, plate slabs, door leaves, lever handles, chest lids) are always their own child.
    ///
    /// The code is split over several files:
    ///   DungeonKitGenerator.cs            menus, materials, fonts and shared helpers (this file)
    ///   DungeonKitGenerator.Mechanics.cs  doors, plates, switches, levers, gong, chute, runestones...
    ///   DungeonKitGenerator.Pickups.cs    boxes, crates, barrels, pots, blocks, balls, key
    ///   DungeonKitGenerator.Props.cs      torches, braziers, chest, furniture, decoration
    ///   DungeonKitGenerator.Level.cs      walls, floors, ceilings, pillars, stairs, player
    ///   DungeonKitGenerator.Scenes.cs     lighting and room helpers shared by both scenes
    ///   DungeonKitGenerator.Showcase.cs   the showcase dungeon
    ///   DungeonKitGenerator.TestRoom.cs   the small mechanics test room in Assets/Bryce
    /// </summary>
    public static partial class DungeonKitGenerator
    {
        private const string KitRoot = "Assets/Prefabs";
        private const string MechanicsDir = KitRoot + "/01_Puzzle_Mechanics";
        private const string PickupsDir = KitRoot + "/02_Pickups";
        private const string PropsDir = KitRoot + "/03_Props";
        private const string LevelDir = KitRoot + "/04_Level_Building";
        private const string PlayerDir = KitRoot + "/05_Player";
        private const string SourceDir = KitRoot + "/_Source";
        private const string MaterialDir = SourceDir + "/Materials";
        private const string LightingDir = SourceDir + "/Lighting";
        private const string FontDir = SourceDir + "/Fonts";

        private const string ShowcaseScenePath = KitRoot + "/Showcase_Dungeon.unity";
        private const string TestScenePath = "Assets/Bryce/Scenes/MechanicsTestScene.unity";

        // Open-licence fonts (SIL Open Font License, see the OFL.txt next to each font).
        private const string HeadingTtf = FontDir + "/Cinzel/Cinzel-Bold.ttf";
        private const string HeadingFontPath = FontDir + "/Cinzel/Cinzel-Bold SDF.asset";
        private const string HeadingPlaquePath = FontDir + "/Cinzel/Cinzel-Bold SDF Carved.mat";
        private const string BodyTtf = FontDir + "/AlegreyaSans/AlegreyaSans-Medium.ttf";
        private const string BodyFontPath = FontDir + "/AlegreyaSans/AlegreyaSans-Medium SDF.asset";
        private const string BodyPlaquePath = FontDir + "/AlegreyaSans/AlegreyaSans-Medium SDF Carved.mat";

        // ------------------------------------------------------------------
        // Menu items
        // ------------------------------------------------------------------

        [MenuItem("Tools/Dungeon Kit/Generate Everything (Prefabs + Scenes)", priority = 0)]
        public static void GenerateAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            GenerateAllNoPrompt();
            EditorSceneManager.OpenScene(ShowcaseScenePath);
        }

        [MenuItem("Tools/Dungeon Kit/Generate Prefabs Only", priority = 1)]
        public static void GeneratePrefabsOnly()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // Start from an empty scene so temporary objects never end up in someone else's scene.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildPrefabs();
            AssetDatabase.SaveAssets();
            Debug.Log($"Dungeon Kit: prefabs saved to {KitRoot}.");
        }

        [MenuItem("Tools/Dungeon Kit/Open Showcase Scene", priority = 20)]
        public static void OpenShowcase()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ShowcaseScenePath);
        }

        /// <summary>Used by the menu and by batch mode (-executeMethod).</summary>
        public static void GenerateAllNoPrompt()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrefabSet set = BuildPrefabs();
            BuildShowcaseScene(set);
            BuildTestRoomScene(set);
            AssetDatabase.SaveAssets();
            Debug.Log($"Dungeon Kit: prefabs saved to {KitRoot}. Scenes: {ShowcaseScenePath} and {TestScenePath}.");
        }

        // ------------------------------------------------------------------
        // Materials (dungeon palette)
        // ------------------------------------------------------------------

        private static Material stone, stoneDark, stoneLight, stoneFloor, mortar, plaque, wood, woodDark, woodLight,
            iron, gold, bronze, clay, bone, clothRed, clothBlue, bookGreen, paper, wax, coal, cobweb,
            flame, flameCore, ember, runeOff, magic;

        private static bool IsHdrp =>
            GraphicsSettings.currentRenderPipeline != null &&
            GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("HDRenderPipeline");

        private static void BuildMaterials()
        {
            EnsureFolder(MaterialDir);

            stone = GetOrCreateMaterial("M_Stone", new Color(0.42f, 0.4f, 0.37f), smoothness: 0.15f);
            stoneDark = GetOrCreateMaterial("M_Stone_Dark", new Color(0.25f, 0.24f, 0.23f), smoothness: 0.15f);
            stoneLight = GetOrCreateMaterial("M_Stone_Light", new Color(0.56f, 0.54f, 0.5f), smoothness: 0.15f);
            stoneFloor = GetOrCreateMaterial("M_Stone_Floor", new Color(0.34f, 0.33f, 0.31f), smoothness: 0.2f);
            mortar = GetOrCreateMaterial("M_Mortar", new Color(0.13f, 0.12f, 0.11f), smoothness: 0.05f);
            plaque = GetOrCreateMaterial("M_Stone_Plaque", new Color(0.2f, 0.19f, 0.18f), smoothness: 0.2f);

            wood = GetOrCreateMaterial("M_Wood", new Color(0.4f, 0.25f, 0.13f), smoothness: 0.25f);
            woodDark = GetOrCreateMaterial("M_Wood_Dark", new Color(0.22f, 0.13f, 0.07f), smoothness: 0.2f);
            woodLight = GetOrCreateMaterial("M_Wood_Light", new Color(0.55f, 0.37f, 0.2f), smoothness: 0.25f);

            iron = GetOrCreateMaterial("M_Iron", new Color(0.2f, 0.2f, 0.21f), smoothness: 0.4f, metallic: 0.35f);
            gold = GetOrCreateMaterial("M_Gold", new Color(0.95f, 0.7f, 0.25f), emission: 0.15f, smoothness: 0.55f, metallic: 0.5f);
            bronze = GetOrCreateMaterial("M_Bronze", new Color(0.62f, 0.42f, 0.2f), smoothness: 0.5f, metallic: 0.4f);

            clay = GetOrCreateMaterial("M_Clay", new Color(0.6f, 0.33f, 0.2f), smoothness: 0.2f);
            bone = GetOrCreateMaterial("M_Bone", new Color(0.82f, 0.78f, 0.66f), smoothness: 0.3f);
            clothRed = GetOrCreateMaterial("M_Cloth_Red", new Color(0.45f, 0.07f, 0.06f), smoothness: 0.05f);
            clothBlue = GetOrCreateMaterial("M_Cloth_Blue", new Color(0.1f, 0.15f, 0.38f), smoothness: 0.05f);
            bookGreen = GetOrCreateMaterial("M_Book_Green", new Color(0.12f, 0.3f, 0.15f), smoothness: 0.1f);
            paper = GetOrCreateMaterial("M_Paper", new Color(0.85f, 0.8f, 0.65f), smoothness: 0.1f);
            wax = GetOrCreateMaterial("M_Wax", new Color(0.9f, 0.86f, 0.74f), smoothness: 0.3f);
            coal = GetOrCreateMaterial("M_Coal", new Color(0.05f, 0.04f, 0.04f), smoothness: 0.1f);
            cobweb = GetOrCreateMaterial("M_Cobweb", new Color(0.8f, 0.8f, 0.78f), emission: 0.05f, smoothness: 0f);

            // Warm torchlight.
            flame = GetOrCreateMaterial("M_Flame", new Color(1f, 0.45f, 0.1f), emission: 2.2f);
            flameCore = GetOrCreateMaterial("M_Flame_Core", new Color(1f, 0.8f, 0.4f), emission: 3f);
            ember = GetOrCreateMaterial("M_Ember", new Color(0.8f, 0.25f, 0.05f), emission: 1.5f);

            // Magic: soft blue when off, bright green when on. StateVisual switches between its own colours,
            // so this material is only the "off" look you see in the editor.
            StateVisual defaults = new GameObject().AddComponent<StateVisual>();
            runeOff = GetOrCreateMaterial("M_Rune", defaults.offColor, emission: defaults.offGlow * 2f, smoothness: 0.4f);
            Object.DestroyImmediate(defaults.gameObject);
            magic = GetOrCreateMaterial("M_Magic_Barrier", new Color(0.35f, 0.7f, 1f), emission: 3f);
        }

        private static Material GetOrCreateMaterial(string name, Color color, float emission = 0f, float smoothness = 0.3f,
            float metallic = 0f)
        {
            string path = $"{MaterialDir}/{name}.mat";
            Shader shader = FindLitShader();

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }

            SetColor(mat, "_BaseColor", color);
            SetColor(mat, "_Color", color);
            SetFloat(mat, "_Smoothness", smoothness);
            SetFloat(mat, "_Glossiness", smoothness);
            SetFloat(mat, "_Metallic", metallic);

            Color glow = color * emission;
            if (mat.HasProperty("_EmissiveColor"))
            {
                // HDRP: set the HDR colour directly and make it ignore exposure so it reads as a glow.
                SetFloat(mat, "_UseEmissiveIntensity", 0f);
                SetColor(mat, "_EmissiveColorLDR", color);
                mat.SetColor("_EmissiveColor", glow);
                SetFloat(mat, "_EmissiveExposureWeight", 0f);
            }
            if (mat.HasProperty("_EmissionColor"))
            {
                // URP / Built-in: emission needs its keyword switched on.
                mat.SetColor("_EmissionColor", glow);
                if (emission > 0f)
                    mat.EnableKeyword("_EMISSION");
                else
                    mat.DisableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }

            ValidateHdrpMaterial(mat);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>Picks the Lit shader for the active render pipeline so nothing renders pink.</summary>
        private static Shader FindLitShader()
        {
            string pipeline = GraphicsSettings.currentRenderPipeline != null
                ? GraphicsSettings.currentRenderPipeline.GetType().Name
                : "";

            string[] candidates = pipeline.Contains("HDRenderPipeline")
                ? new[] { "HDRP/Lit", "Standard" }
                : pipeline.Contains("Universal")
                    ? new[] { "Universal Render Pipeline/Lit", "Standard" }
                    : new[] { "Standard" };

            foreach (string shaderName in candidates)
            {
                Shader shader = Shader.Find(shaderName);
                if (shader != null)
                    return shader;
            }
            return Shader.Find("Standard");
        }

        // HDRP materials made from code should be "validated" so their keywords match their settings.
        // Done through reflection so this file still compiles in projects without HDRP.
        private static void ValidateHdrpMaterial(Material mat)
        {
            Type hdMaterial = Type.GetType("UnityEngine.Rendering.HighDefinition.HDMaterial, Unity.RenderPipelines.HighDefinition.Runtime");
            MethodInfo validate = hdMaterial?.GetMethod("ValidateMaterial", BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(Material) }, null);
            if (validate == null)
                return;

            try { validate.Invoke(null, new object[] { mat }); }
            catch (Exception e) { Debug.LogWarning($"Could not validate HDRP material {mat.name}: {e.Message}"); }
        }

        private static void SetColor(Material mat, string property, Color value)
        {
            if (mat.HasProperty(property))
                mat.SetColor(property, value);
        }

        private static void SetFloat(Material mat, string property, float value)
        {
            if (mat.HasProperty(property))
                mat.SetFloat(property, value);
        }

        // ------------------------------------------------------------------
        // Fonts
        // ------------------------------------------------------------------

        private static TMP_FontAsset headingFont, bodyFont;
        private static Material headingCarved, bodyCarved;

        private const string FontCharacters =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "°×–—‘’“”•…→";

        private static void BuildFonts()
        {
            headingFont = GetOrCreateFontAsset(HeadingTtf, HeadingFontPath);
            bodyFont = GetOrCreateFontAsset(BodyTtf, BodyFontPath);
            headingCarved = GetOrCreateCarvedMaterial(headingFont, HeadingPlaquePath);
            bodyCarved = GetOrCreateCarvedMaterial(bodyFont, BodyPlaquePath);
        }

        /// <summary>
        /// Makes a TextMesh Pro font asset with a fixed (static) set of characters, so it never changes
        /// while the game runs and doesn't show up as a modified file in Git.
        /// </summary>
        private static TMP_FontAsset GetOrCreateFontAsset(string ttfPath, string assetPath)
        {
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null && existing.characterTable != null && existing.characterTable.Count > 50 &&
                existing.atlasTexture != null)
                return existing;

            Font source = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (source == null)
            {
                Debug.LogWarning($"Dungeon Kit: font {ttfPath} is missing, so TextMesh Pro's default font is used.");
                return TMP_Settings.defaultFontAsset;
            }

            if (existing != null)
                AssetDatabase.DeleteAsset(assetPath);

            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source, 64, 6, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, false);
            font.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(font, assetPath);
            font.atlasTextures[0].name = font.name + " Atlas";
            AssetDatabase.AddObjectToAsset(font.atlasTextures[0], font);
            font.material.name = font.name + " Material";
            AssetDatabase.AddObjectToAsset(font.material, font);

            if (!font.TryAddCharacters(FontCharacters, out string missing) && !string.IsNullOrEmpty(missing))
                Debug.Log($"Dungeon Kit: {font.name} has no glyphs for: {missing}");
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            EditorUtility.SetDirty(font.atlasTextures[0]);
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            return font;
        }

        /// <summary>A font material with a soft dark shadow under the letters, so text looks cut into the stone.</summary>
        private static Material GetOrCreateCarvedMaterial(TMP_FontAsset font, string path)
        {
            if (font == null || font.material == null)
                return null;

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(font.material);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = font.material.shader;
            mat.SetTexture("_MainTex", font.atlasTexture);
            mat.SetFloat("_TextureWidth", font.atlasWidth);
            mat.SetFloat("_TextureHeight", font.atlasHeight);
            mat.SetFloat("_GradientScale", font.atlasPadding + 1);
            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.75f));
            mat.SetFloat("_UnderlayOffsetX", 0.35f);
            mat.SetFloat("_UnderlayOffsetY", -0.35f);
            mat.SetFloat("_UnderlaySoftness", 0.25f);
            mat.SetFloat("_UnderlayDilate", 0.1f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------
        // Prefabs
        // ------------------------------------------------------------------

        private class PrefabSet
        {
            // 01 Puzzle mechanics
            public GameObject doorWooden, doorPortcullis, doorLocked, plate, plateHeavy, wallSwitch, pedestalButton,
                lever, gong, chute, runeCircle, gateAnd, gateOr, gateNot, timer, floorRunes, indicatorBrazier,
                indicatorTorch, barrier;
            // 02 Pickups
            public GameObject boxSmall, crateLarge, barrel, pot, blockHeavy, ballIron, key;
            // 03 Props
            public GameObject chest, torch, brazier, table, bench, bookshelf, skull, bones, cobwebProp, spikeTrap,
                banner, candles, rubble, sign;
            // 04 Level building
            public GameObject wall, wallDoorway, floor, ceiling, archway, pillar, pillarBroken, stairs, platform;
            // 05 Player
            public GameObject player;
        }

        private static PrefabSet BuildPrefabs()
        {
            BuildMaterials();
            BuildFonts();
            foreach (string dir in new[] { MechanicsDir, PickupsDir, PropsDir, LevelDir, PlayerDir })
                EnsureFolder(dir);

            PrefabSet p = new PrefabSet();

            // Pickups first: the dispenser prefab references the small box.
            p.boxSmall = SavePrefab(BuildBoxSmall(), PickupsDir, "Box_Wooden_Small");
            p.crateLarge = SavePrefab(BuildCrateLarge(), PickupsDir, "Crate_Large");
            p.barrel = SavePrefab(BuildBarrel(), PickupsDir, "Barrel_Wooden");
            p.pot = SavePrefab(BuildPot(), PickupsDir, "Pot_Clay");
            p.blockHeavy = SavePrefab(BuildBlockHeavy(), PickupsDir, "Block_Stone_Heavy");
            p.ballIron = SavePrefab(BuildBallIron(), PickupsDir, "Ball_Iron");
            p.key = SavePrefab(BuildKey(), PickupsDir, "Key_Gold");

            p.doorWooden = SavePrefab(BuildDoorWooden(), MechanicsDir, "Door_Wooden");
            p.doorPortcullis = SavePrefab(BuildDoorPortcullis(), MechanicsDir, "Door_Portcullis");
            p.doorLocked = SavePrefab(BuildDoorLocked(), MechanicsDir, "Door_Locked");
            p.plate = SavePrefab(BuildPressurePlate(), MechanicsDir, "PressurePlate_Stone");
            p.plateHeavy = SavePrefab(BuildPressurePlateHeavy(), MechanicsDir, "PressurePlate_Heavy");
            p.wallSwitch = SavePrefab(BuildWallSwitch(), MechanicsDir, "WallSwitch_Stone");
            p.pedestalButton = SavePrefab(BuildPedestalButton(), MechanicsDir, "Button_Pedestal");
            p.lever = SavePrefab(BuildLever(), MechanicsDir, "Lever_Wall");
            p.gong = SavePrefab(BuildGong(), MechanicsDir, "Target_Gong");
            p.chute = SavePrefab(BuildCeilingChute(p.boxSmall), MechanicsDir, "Dispenser_CeilingChute");
            p.runeCircle = SavePrefab(BuildRuneCircle(), MechanicsDir, "TriggerZone_RuneCircle");
            p.gateAnd = SavePrefab(BuildRunestone(GateType.And), MechanicsDir, "Runestone_AND");
            p.gateOr = SavePrefab(BuildRunestone(GateType.Or), MechanicsDir, "Runestone_OR");
            p.gateNot = SavePrefab(BuildRunestone(GateType.Not), MechanicsDir, "Runestone_NOT");
            p.timer = SavePrefab(BuildRunestoneTimer(), MechanicsDir, "Runestone_Timer");
            p.floorRunes = SavePrefab(BuildFloorRunes(), MechanicsDir, "Indicator_FloorRunes");
            p.indicatorBrazier = SavePrefab(BuildIndicatorBrazier(), MechanicsDir, "Indicator_Brazier");
            p.indicatorTorch = SavePrefab(BuildIndicatorTorch(), MechanicsDir, "Indicator_Torch");
            p.barrier = SavePrefab(BuildMagicBarrier(), MechanicsDir, "Barrier_Magic");

            p.chest = SavePrefab(BuildChest(), PropsDir, "Chest_Treasure");
            p.torch = SavePrefab(BuildWallTorch(), PropsDir, "Torch_Wall");
            p.brazier = SavePrefab(BuildStandingBrazier(), PropsDir, "Brazier_Standing");
            p.table = SavePrefab(BuildTable(), PropsDir, "Table_Wooden");
            p.bench = SavePrefab(BuildBench(), PropsDir, "Bench_Wooden");
            p.bookshelf = SavePrefab(BuildBookshelf(), PropsDir, "Bookshelf");
            p.skull = SavePrefab(BuildSkullProp(), PropsDir, "Skull");
            p.bones = SavePrefab(BuildBonesPile(), PropsDir, "Bones_Pile");
            p.cobwebProp = SavePrefab(BuildCobweb(), PropsDir, "Cobweb");
            p.spikeTrap = SavePrefab(BuildSpikeTrap(), PropsDir, "SpikeTrap");
            p.banner = SavePrefab(BuildBanner(), PropsDir, "Banner_Wall");
            p.candles = SavePrefab(BuildCandles(), PropsDir, "Candles");
            p.rubble = SavePrefab(BuildRubble(), PropsDir, "Rubble_Pile");
            p.sign = SavePrefab(BuildSign(), PropsDir, "Sign_StonePlaque");

            p.wall = SavePrefab(BuildWall(), LevelDir, "Wall_Stone_4x4");
            p.wallDoorway = SavePrefab(BuildWallDoorway(), LevelDir, "Wall_Stone_Doorway");
            p.floor = SavePrefab(BuildFloorTile(), LevelDir, "Floor_Stone_4x4");
            p.ceiling = SavePrefab(BuildCeilingTile(), LevelDir, "Ceiling_Stone_4x4");
            p.archway = SavePrefab(BuildArchway(), LevelDir, "Archway_Stone");
            p.pillar = SavePrefab(BuildPillar(), LevelDir, "Pillar_Stone");
            p.pillarBroken = SavePrefab(BuildPillarBroken(), LevelDir, "Pillar_Broken");
            p.stairs = SavePrefab(BuildStairs(), LevelDir, "Stairs_Stone");
            p.platform = SavePrefab(BuildPlatform(), LevelDir, "Platform_Stone");

            p.player = SavePrefab(BuildPlayer(), PlayerDir, "Player_FirstPerson");
            return p;
        }

        // ------------------------------------------------------------------
        // Model-building helpers
        // ------------------------------------------------------------------

        /// <summary>A primitive child. Visual only (no collider) unless 'collider' is true.</summary>
        private static GameObject Part(string name, PrimitiveType type, Transform parent, Vector3 localPosition,
            Vector3 localScale, Material mat, bool collider = false, Vector3? euler = null)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            go.transform.localScale = localScale;

            if (!collider)
                Object.DestroyImmediate(go.GetComponent<Collider>());

            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>A box-shaped part.</summary>
        private static GameObject Box(string name, Transform parent, Vector3 localPosition, Vector3 size, Material mat,
            bool collider = false, Vector3? euler = null)
        {
            return Part(name, PrimitiveType.Cube, parent, localPosition, size, mat, collider, euler);
        }

        private enum Axis { Y, X, Z }

        /// <summary>A cylinder by radius and height (Unity's cylinder is 1 wide and 2 tall), standing up or lying along X or Z.</summary>
        private static GameObject Cylinder(string name, Transform parent, Vector3 localPosition, float radius, float height,
            Material mat, Axis axis = Axis.Y, bool collider = false)
        {
            Vector3 euler = axis == Axis.Z ? new Vector3(90f, 0f, 0f) : axis == Axis.X ? new Vector3(0f, 0f, 90f) : Vector3.zero;
            return Part(name, PrimitiveType.Cylinder, parent, localPosition, new Vector3(radius * 2f, height * 0.5f, radius * 2f),
                mat, collider, euler);
        }

        private static GameObject Sphere(string name, Transform parent, Vector3 localPosition, Vector3 size, Material mat)
        {
            return Part(name, PrimitiveType.Sphere, parent, localPosition, size, mat);
        }

        /// <summary>A flat ring of box segments around the Y axis.</summary>
        private static void Ring(string name, Transform parent, Vector3 centre, float radius, int segments, float height,
            float thickness, Material mat, float widthFactor = 1.08f, bool collider = false)
        {
            float segmentLength = 2f * Mathf.PI * radius / segments * widthFactor;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * 360f / segments;
                float rad = angle * Mathf.Deg2Rad;
                Vector3 pos = centre + new Vector3(Mathf.Cos(rad) * radius, 0f, Mathf.Sin(rad) * radius);
                // Rotate so each segment's long side runs along the circle.
                Part(name, PrimitiveType.Cube, parent, pos, new Vector3(segmentLength, height, thickness), mat, collider,
                    new Vector3(0f, -(angle + 90f), 0f));
            }
        }

        /// <summary>The 12 edge bars of a cube (used for framed boxes and crates).</summary>
        private static void EdgeFrame(Transform parent, float offset, float length, float thickness, Material mat)
        {
            foreach (float a in new[] { -offset, offset })
            {
                foreach (float b in new[] { -offset, offset })
                {
                    Box("Edge", parent, new Vector3(0f, a, b), new Vector3(length, thickness, thickness), mat);
                    Box("Edge", parent, new Vector3(a, 0f, b), new Vector3(thickness, length, thickness), mat);
                    Box("Edge", parent, new Vector3(a, b, 0f), new Vector3(thickness, thickness, length), mat);
                }
            }
        }

        private static IEnumerable<Vector3> Corners(float offset)
        {
            foreach (float x in new[] { -offset, offset })
                foreach (float y in new[] { -offset, offset })
                    foreach (float z in new[] { -offset, offset })
                        yield return new Vector3(x, y, z);
        }

        private static GameObject Empty(string name, Transform parent, Vector3 localPosition, Vector3? euler = null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            return go;
        }

        private static BoxCollider AddBox(GameObject go, Vector3 centre, Vector3 size, bool trigger = false)
        {
            BoxCollider box = go.AddComponent<BoxCollider>();
            box.center = centre;
            box.size = size;
            box.isTrigger = trigger;
            return box;
        }

        /// <summary>Adds a StateVisual. With no renderers given, it recolours every renderer under 'host'.</summary>
        private static StateVisual AddStateVisual(GameObject host, params Renderer[] renderers)
        {
            StateVisual visual = host.AddComponent<StateVisual>();
            visual.renderers = renderers.Length > 0 ? renderers : host.GetComponentsInChildren<Renderer>();
            return visual;
        }

        /// <summary>A point light for a flame. HDRP uses real-world units (lumens), other pipelines use small numbers.</summary>
        private static Light AddFlameLight(GameObject go, Color color, float lumens, float range)
        {
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.shadows = LightShadows.None;
            light.lightmapBakeType = LightmapBakeType.Realtime;
            if (IsHdrp)
            {
                AddComponentByName(go, "UnityEngine.Rendering.HighDefinition.HDAdditionalLightData, Unity.RenderPipelines.HighDefinition.Runtime");
                light.intensity = lumens;
            }
            else
            {
                light.intensity = lumens / 600f;
            }
            return light;
        }

        /// <summary>
        /// A world-space TextMesh Pro label. Its readable side faces the object's +Z, the same way as
        /// wall-mounted pieces, so a label on a wall faces out of the wall.
        /// </summary>
        private static TextMeshPro CreateText(Transform parent, string name, string text, Vector3 localPosition,
            float letterHeight, Vector2 box, bool heading, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            TextMeshPro label = go.AddComponent<TextMeshPro>();
            TMP_FontAsset font = heading ? headingFont : bodyFont;
            if (font != null)
            {
                label.font = font;
                Material carved = heading ? headingCarved : bodyCarved;
                if (carved != null)
                    label.fontSharedMaterial = carved;
            }
            label.text = text;
            label.fontSize = letterHeight * 10f;   // TMP world text: font size 10 is about 1 m tall
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.color = color;
            label.rectTransform.sizeDelta = box;
            return label;
        }

        private static readonly Color CarvedGold = new Color(0.95f, 0.82f, 0.55f);
        private static readonly Color CarvedPale = new Color(0.88f, 0.84f, 0.76f);

        private static GameObject SavePrefab(GameObject temp, string folder, string name)
        {
            temp.name = name;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, $"{folder}/{name}.prefab");
            Object.DestroyImmediate(temp);
            return prefab;
        }

        private static void AddComponentByName(GameObject go, string assemblyQualifiedName)
        {
            Type type = Type.GetType(assemblyQualifiedName);
            if (type != null && go.GetComponent(type) == null)
                go.AddComponent(type);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
