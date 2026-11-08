using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using TMPro;
using Object = UnityEngine.Object;

namespace PuzzleMechanics.EditorTools
{
    /// <summary>
    /// Builds every graybox model, material, prefab and the test scene from code.
    ///
    /// Menu: Tools > Puzzle Mechanics > Generate Everything
    ///
    /// Running it again is safe: materials and prefabs are updated in place (so references to
    /// them keep working) and the test scene is rebuilt from scratch.
    ///
    /// Every model is made of Unity primitives arranged as child objects. Moving parts
    /// (button caps, plates, door panels, lever handles) are always their own child object.
    /// </summary>
    public static class PuzzleMechanicsGenerator
    {
        private const string Root = "Assets/Bryce";
        private const string PrefabDir = Root + "/Prefabs";
        private const string MechanicsDir = PrefabDir + "/Mechanics";
        private const string PickupsDir = PrefabDir + "/Pickups";
        private const string EnvironmentDir = PrefabDir + "/Environment";
        private const string MaterialDir = Root + "/Materials";
        private const string SceneDir = Root + "/Scenes";
        private const string ScenePath = SceneDir + "/MechanicsTestScene.unity";

        // Shared project fonts (SIL Open Font License). If they are missing, TMP's default font is used.
        private const string BodyFontPath = "Assets/Shared/Fonts/TitilliumWeb/TitilliumWeb-Bold SDF.asset";
        private const string BodyOutlinePath = "Assets/Shared/Fonts/TitilliumWeb/TitilliumWeb-Bold SDF Outline.mat";
        private const string HeadingFontPath = "Assets/Shared/Fonts/Rajdhani/Rajdhani-Bold SDF.asset";
        private const string HeadingOutlinePath = "Assets/Shared/Fonts/Rajdhani/Rajdhani-Bold SDF Outline.mat";

        // ------------------------------------------------------------------
        // Menu items
        // ------------------------------------------------------------------

        [MenuItem("Tools/Puzzle Mechanics/Generate Everything (Prefabs + Test Scene)")]
        public static void GenerateAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // Start from an empty scene so temporary objects never end up in someone else's scene.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrefabSet prefabs = BuildPrefabs();
            BuildTestScene(prefabs);

            // Apply the project's shared lighting polish (post-processing, probes, door lights) if it is in the project.
            Type polish = Type.GetType("ProjectTools.ScenePolish, Assembly-CSharp-Editor");
            polish?.GetMethod("PolishActiveScene")?.Invoke(null, null);

            AssetDatabase.SaveAssets();
            Debug.Log($"Puzzle Mechanics: prefabs saved to {PrefabDir}, test scene saved to {ScenePath}. Press Play to test.");
        }

        [MenuItem("Tools/Puzzle Mechanics/Generate Prefabs Only")]
        public static void GeneratePrefabsOnly()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildPrefabs();
            AssetDatabase.SaveAssets();
            Debug.Log($"Puzzle Mechanics: prefabs saved to {PrefabDir}.");
        }

        // ------------------------------------------------------------------
        // Materials (graybox palette)
        // ------------------------------------------------------------------

        private static Material roomLight, roomTrim, floorTile, floorSeam, mechanic, mechanicDark, heavy,
            pickupLight, accentInactive, accentActive, fizzlerField;

        private static bool IsHdrp =>
            GraphicsSettings.currentRenderPipeline != null &&
            GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("HDRenderPipeline");

        private static void BuildMaterials()
        {
            EnsureFolder(MaterialDir);

            // Light gray: room surfaces
            roomLight = GetOrCreateMaterial("M_Room_Light", Gray(0.78f));
            roomTrim = GetOrCreateMaterial("M_Room_Trim", Gray(0.64f));
            floorTile = GetOrCreateMaterial("M_Floor_Tile", Gray(0.72f));
            floorSeam = GetOrCreateMaterial("M_Floor_Seam", Gray(0.52f));

            // Mid gray: mechanics
            mechanic = GetOrCreateMaterial("M_Mechanic", Gray(0.47f), smoothness: 0.5f);
            mechanicDark = GetOrCreateMaterial("M_Mechanic_Dark", Gray(0.3f), smoothness: 0.5f);
            pickupLight = GetOrCreateMaterial("M_Pickup_Light", Gray(0.86f), smoothness: 0.45f);

            // Dark gray: heavy objects
            heavy = GetOrCreateMaterial("M_Heavy", Gray(0.16f), smoothness: 0.3f);

            // Accents: blue = inactive, green = active (glows). StateVisual switches between these colours.
            StateVisual defaults = new GameObject().AddComponent<StateVisual>();
            accentInactive = GetOrCreateMaterial("M_Accent_Inactive", defaults.offColor, emission: 0.3f, smoothness: 0.6f);
            accentActive = GetOrCreateMaterial("M_Accent_Active", defaults.onColor, emission: 2f, smoothness: 0.6f);
            Object.DestroyImmediate(defaults.gameObject);

            fizzlerField = GetOrCreateMaterial("M_Fizzler_Field", new Color(0.45f, 0.8f, 1f), emission: 3f);
        }

        private static Color Gray(float value) => new Color(value, value, value);

        private static Material GetOrCreateMaterial(string name, Color color, float emission = 0f, float smoothness = 0.35f)
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

            ValidateHdrpMaterial(mat);

            SetColor(mat, "_BaseColor", color);
            SetColor(mat, "_Color", color);
            SetFloat(mat, "_Smoothness", smoothness);
            SetFloat(mat, "_Glossiness", smoothness);
            SetFloat(mat, "_Metallic", 0f);

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
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }

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
        // Prefabs
        // ------------------------------------------------------------------

        private class PrefabSet
        {
            // Mechanics
            public GameObject plate, wallButton, pedestalButton, target, lever, zone, door, gateAnd, gateOr, gateNot,
                timer, dispenser, strip, lamp, fizzler;
            // Pickups
            public GameObject cube, crate, heavyBlock, ball;
            // Environment
            public GameObject floorTile, wallPanel, wallDoorway, ledge, steps;
            public GameObject player;
        }

        private static PrefabSet BuildPrefabs()
        {
            BuildMaterials();
            EnsureFolder(MechanicsDir);
            EnsureFolder(PickupsDir);
            EnsureFolder(EnvironmentDir);

            PrefabSet p = new PrefabSet();

            // Pickups first: the dispenser prefab references the light cube.
            p.cube = SavePrefab(BuildLightCube(), PickupsDir, "Cube_Light");
            p.crate = SavePrefab(BuildCrate(), PickupsDir, "Crate_Medium");
            p.heavyBlock = SavePrefab(BuildHeavyBlock(), PickupsDir, "Block_Heavy");
            p.ball = SavePrefab(BuildBall(), PickupsDir, "Ball");

            p.plate = SavePrefab(BuildPressurePlate(), MechanicsDir, "PressurePlate");
            p.wallButton = SavePrefab(BuildWallButton(), MechanicsDir, "WallButton");
            p.pedestalButton = SavePrefab(BuildPedestalButton(), MechanicsDir, "PedestalButton");
            p.target = SavePrefab(BuildTarget(), MechanicsDir, "ImpactTarget");
            p.lever = SavePrefab(BuildLever(), MechanicsDir, "Lever");
            p.zone = SavePrefab(BuildTriggerZone(), MechanicsDir, "TriggerZone");
            p.door = SavePrefab(BuildDoor(), MechanicsDir, "PuzzleDoor");
            p.gateAnd = SavePrefab(BuildLogicGate(GateType.And), MechanicsDir, "LogicGate_AND");
            p.gateOr = SavePrefab(BuildLogicGate(GateType.Or), MechanicsDir, "LogicGate_OR");
            p.gateNot = SavePrefab(BuildLogicGate(GateType.Not), MechanicsDir, "LogicGate_NOT");
            p.timer = SavePrefab(BuildTimer(), MechanicsDir, "TimerDelay");
            p.dispenser = SavePrefab(BuildDispenser(p.cube), MechanicsDir, "ObjectDispenser");
            p.strip = SavePrefab(BuildIndicatorStrip(), MechanicsDir, "IndicatorLight_FloorStrip");
            p.lamp = SavePrefab(BuildIndicatorLamp(), MechanicsDir, "IndicatorLight_Lamp");
            p.fizzler = SavePrefab(BuildFizzler(), MechanicsDir, "Fizzler");

            p.floorTile = SavePrefab(BuildFloorTile(), EnvironmentDir, "FloorTile_4x4");
            p.wallPanel = SavePrefab(BuildWallPanel(), EnvironmentDir, "WallPanel_4x4");
            p.wallDoorway = SavePrefab(BuildWallDoorway(), EnvironmentDir, "WallPanel_Doorway");
            p.ledge = SavePrefab(BuildLedge(), EnvironmentDir, "Platform_Ledge");
            p.steps = SavePrefab(BuildSteps(), EnvironmentDir, "Steps");

            p.player = SavePrefab(BuildPlayer(), PrefabDir, "TestPlayer");
            return p;
        }

        // ---------------- Pickups ----------------
        // Each pickup: Rigidbody + Carryable + one collider on the root that matches the outer size.
        // All the detail pieces are children with no colliders.

        private static GameObject PickupRoot(string name, float mass, string displayName)
        {
            GameObject root = new GameObject(name);
            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            Carryable carryable = root.AddComponent<Carryable>();
            carryable.displayName = displayName;
            return root;
        }

        private static void FinishPickup(GameObject root)
        {
            // Use the project's "Moveable" layer if it exists, so the team's own grab script works on these too.
            int moveable = LayerMask.NameToLayer("Moveable");
            if (moveable < 0)
                return;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = moveable;
        }

        /// <summary>0.6 m, 15 kg. Light core with darker edge bars and corners for a bevelled look.</summary>
        private static GameObject BuildLightCube()
        {
            const float size = 0.6f;
            const float edge = 0.06f;
            float h = size * 0.5f - edge * 0.5f;   // centre offset of the edge bars
            float inner = size - edge;              // length of the edge bars

            GameObject root = PickupRoot("Cube_Light", 15f, "Light Cube (15 kg)");
            AddBox(root, Vector3.zero, Vector3.one * size);

            Part("Core", PrimitiveType.Cube, root.transform, Vector3.zero, Vector3.one * (size - 0.02f), pickupLight);
            EdgeFrame(root.transform, h, inner, edge, mechanic);
            foreach (Vector3 corner in Corners(h))
                Part("Corner", PrimitiveType.Cube, root.transform, corner, Vector3.one * (edge + 0.012f), mechanicDark);

            // A dark disc on each face, like a Portal cube.
            foreach (Vector3 dir in FaceDirections())
            {
                Vector3 euler = Quaternion.FromToRotation(Vector3.up, dir).eulerAngles;
                Part("Face Disc", PrimitiveType.Cylinder, root.transform, dir * (size * 0.5f - 0.004f),
                    new Vector3(0.26f, 0.006f, 0.26f), mechanicDark, euler: euler);
            }

            FinishPickup(root);
            return root;
        }

        /// <summary>0.9 m, 45 kg. Liftable but sluggish. Thick frame with diagonal braces.</summary>
        private static GameObject BuildCrate()
        {
            const float size = 0.9f;
            const float edge = 0.1f;
            float h = size * 0.5f - edge * 0.5f;
            float inner = size - edge;

            GameObject root = PickupRoot("Crate_Medium", 45f, "Medium Crate (45 kg)");
            AddBox(root, Vector3.zero, Vector3.one * size);

            Part("Core", PrimitiveType.Cube, root.transform, Vector3.zero, Vector3.one * (size - 0.04f), mechanic);
            EdgeFrame(root.transform, h, inner, edge, mechanicDark);

            // Diagonal brace on each side face.
            float face = size * 0.5f - 0.015f;
            Vector3 brace = new Vector3(1.0f, 0.09f, 0.03f);
            Part("Brace", PrimitiveType.Cube, root.transform, new Vector3(0f, 0f, face), brace, mechanicDark, euler: new Vector3(0f, 0f, 45f));
            Part("Brace", PrimitiveType.Cube, root.transform, new Vector3(0f, 0f, -face), brace, mechanicDark, euler: new Vector3(0f, 0f, -45f));
            Part("Brace", PrimitiveType.Cube, root.transform, new Vector3(face, 0f, 0f), brace, mechanicDark, euler: new Vector3(0f, 90f, 45f));
            Part("Brace", PrimitiveType.Cube, root.transform, new Vector3(-face, 0f, 0f), brace, mechanicDark, euler: new Vector3(0f, 90f, -45f));

            FinishPickup(root);
            return root;
        }

        /// <summary>1.2 m, 120 kg. Too heavy to lift (only pushed or dragged). Dark, chunky, marked "HEAVY".</summary>
        private static GameObject BuildHeavyBlock()
        {
            const float size = 1.2f;
            GameObject root = PickupRoot("Block_Heavy", 120f, "Heavy Block (120 kg)");
            AddBox(root, Vector3.zero, Vector3.one * size);

            Part("Core", PrimitiveType.Cube, root.transform, Vector3.zero, Vector3.one * 1.1f, heavy);
            foreach (Vector3 corner in Corners(0.45f))
                Part("Corner Block", PrimitiveType.Cube, root.transform, corner, Vector3.one * 0.3f, mechanicDark);
            Part("Band", PrimitiveType.Cube, root.transform, new Vector3(0f, -0.05f, 0f), new Vector3(1.16f, 0.14f, 1.16f), mechanicDark);

            // "HEAVY" marking on all four sides. Labels read correctly when viewed looking along their +Z.
            float d = 0.556f;
            (Vector3 pos, float rotY)[] sides =
            {
                (new Vector3(0f, 0f, -d), 0f), (new Vector3(0f, 0f, d), 180f),
                (new Vector3(d, 0f, 0f), -90f), (new Vector3(-d, 0f, 0f), 90f)
            };
            foreach ((Vector3 pos, float rotY) in sides)
            {
                CreateLabel(root.transform, "HEAVY", pos + new Vector3(0f, 0.22f, 0f), 0.24f, 1f, rotY, Color.white);
                CreateLabel(root.transform, "120 kg", pos + new Vector3(0f, -0.3f, 0f), 0.13f, 1f, rotY, new Color(0.8f, 0.8f, 0.8f));
            }

            FinishPickup(root);
            return root;
        }

        /// <summary>0.4 m ball, 5 kg. Easy to throw at targets. The bands make it easy to see it spin.</summary>
        private static GameObject BuildBall()
        {
            GameObject root = PickupRoot("Ball", 5f, "Ball (5 kg)");
            SphereCollider sphere = root.AddComponent<SphereCollider>();
            sphere.radius = 0.2f;

            Part("Shell", PrimitiveType.Sphere, root.transform, Vector3.zero, Vector3.one * 0.4f, pickupLight);
            Part("Band", PrimitiveType.Cylinder, root.transform, Vector3.zero, new Vector3(0.408f, 0.02f, 0.408f), mechanicDark);
            Part("Band", PrimitiveType.Cylinder, root.transform, Vector3.zero, new Vector3(0.408f, 0.02f, 0.408f), mechanicDark,
                euler: new Vector3(90f, 0f, 0f));

            FinishPickup(root);
            return root;
        }

        // ---------------- Mechanics ----------------

        private static GameObject BuildPressurePlate()
        {
            // A round base with a raised ring, and a separate plate inside that sinks when pressed.
            GameObject root = new GameObject("PressurePlate");

            // Trigger box that detects what is resting on the plate.
            AddBox(root, new Vector3(0f, 0.65f, 0f), new Vector3(2f, 1f, 2f), trigger: true);

            // Solid collision for standing on (its top matches the pressed plate height).
            GameObject collision = Empty("Collision", root.transform, Vector3.zero);
            AddBox(collision, new Vector3(0f, 0.075f, 0f), new Vector3(2.4f, 0.15f, 2.4f));

            Cylinder("Base", root.transform, new Vector3(0f, 0.05f, 0f), 1.2f, 0.1f, mechanicDark);
            Ring("Base Ring", root.transform, new Vector3(0f, 0.16f, 0f), 1.07f, 24, 0.12f, 0.16f, mechanic);
            GameObject plateTop = Cylinder("Plate", root.transform, new Vector3(0f, 0.17f, 0f), 0.95f, 0.06f, accentInactive);
            AddStateVisual(plateTop);

            PressurePlate plate = root.AddComponent<PressurePlate>();
            plate.pad = plateTop.transform;
            plate.requiredMass = 15f;
            plate.pressDepth = 0.05f;
            return root;
        }

        private static GameObject BuildWallButton()
        {
            // Pivot is on the wall surface, front faces +Z. The round cap pushes into the backplate.
            GameObject root = new GameObject("WallButton");
            AddBox(root, new Vector3(0f, 0f, 0.06f), new Vector3(0.36f, 0.36f, 0.12f));

            Part("Backplate", PrimitiveType.Cube, root.transform, new Vector3(0f, 0f, 0.02f), new Vector3(0.36f, 0.36f, 0.04f), mechanic);
            Cylinder("Bezel", root.transform, new Vector3(0f, 0f, 0.055f), 0.13f, 0.03f, mechanicDark, alongZ: true);
            GameObject cap = Cylinder("Button Cap", root.transform, new Vector3(0f, 0f, 0.085f), 0.1f, 0.05f, accentInactive, alongZ: true);
            AddStateVisual(cap);

            WallButton button = root.AddComponent<WallButton>();
            button.buttonCap = cap.transform;
            button.pressDirection = Vector3.back;
            return root;
        }

        private static GameObject BuildPedestalButton()
        {
            // A waist-high pillar with a big button on top that pushes down.
            GameObject root = new GameObject("PedestalButton");
            AddBox(root, new Vector3(0f, 0.525f, 0f), new Vector3(0.6f, 1.05f, 0.6f));

            Cylinder("Foot", root.transform, new Vector3(0f, 0.03f, 0f), 0.3f, 0.06f, mechanicDark);
            Cylinder("Pillar", root.transform, new Vector3(0f, 0.485f, 0f), 0.15f, 0.85f, mechanic);
            Cylinder("Head", root.transform, new Vector3(0f, 0.95f, 0f), 0.3f, 0.08f, mechanicDark);
            GameObject cap = Cylinder("Button Cap", root.transform, new Vector3(0f, 1.02f, 0f), 0.22f, 0.06f, accentInactive);
            AddStateVisual(cap);

            WallButton button = root.AddComponent<WallButton>();
            button.buttonCap = cap.transform;
            button.pressDirection = Vector3.down;
            return root;
        }

        private static GameObject BuildTarget()
        {
            // Wall-mounted bullseye, front faces +Z. The collider must be on the root to receive hits.
            GameObject root = new GameObject("ImpactTarget");
            AddBox(root, new Vector3(0f, 0f, 0.08f), new Vector3(1.5f, 1.5f, 0.16f));

            Part("Mount", PrimitiveType.Cube, root.transform, new Vector3(0f, 0f, 0.02f), new Vector3(1.5f, 1.5f, 0.04f), mechanicDark);
            // Stacked concentric rings, each one a little smaller and further forward.
            Cylinder("Ring 1", root.transform, new Vector3(0f, 0f, 0.06f), 0.7f, 0.04f, roomLight, alongZ: true);
            Cylinder("Ring 2", root.transform, new Vector3(0f, 0f, 0.08f), 0.55f, 0.04f, mechanicDark, alongZ: true);
            Cylinder("Ring 3", root.transform, new Vector3(0f, 0f, 0.10f), 0.4f, 0.04f, roomLight, alongZ: true);
            Cylinder("Ring 4", root.transform, new Vector3(0f, 0f, 0.12f), 0.26f, 0.04f, mechanicDark, alongZ: true);
            GameObject centre = Cylinder("Bullseye", root.transform, new Vector3(0f, 0f, 0.14f), 0.14f, 0.04f, accentInactive, alongZ: true);
            AddStateVisual(centre);

            root.AddComponent<ImpactTarget>();
            return root;
        }

        private static GameObject BuildLever()
        {
            // A floor housing with a slot, and a handle that swings forward/back on a pivot.
            GameObject root = new GameObject("Lever");
            AddBox(root, new Vector3(0f, 0.55f, 0f), new Vector3(0.6f, 1.1f, 0.8f));

            Part("Housing", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.15f, 0f), new Vector3(0.5f, 0.3f, 0.7f), mechanic);
            Part("Slot Rail", PrimitiveType.Cube, root.transform, new Vector3(-0.13f, 0.32f, 0f), new Vector3(0.12f, 0.04f, 0.62f), mechanicDark);
            Part("Slot Rail", PrimitiveType.Cube, root.transform, new Vector3(0.13f, 0.32f, 0f), new Vector3(0.12f, 0.04f, 0.62f), mechanicDark);
            GameObject lamp = Part("Status Lamp", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.18f, -0.355f),
                new Vector3(0.24f, 0.07f, 0.02f), accentInactive);

            GameObject pivot = Empty("Handle Pivot", root.transform, new Vector3(0f, 0.3f, 0f));
            Cylinder("Shaft", pivot.transform, new Vector3(0f, 0.33f, 0f), 0.03f, 0.66f, mechanicDark);
            GameObject grip = Part("Grip", PrimitiveType.Capsule, pivot.transform, new Vector3(0f, 0.72f, 0f),
                new Vector3(0.12f, 0.11f, 0.12f), accentInactive);
            AddStateVisual(grip, grip.GetComponent<Renderer>(), lamp.GetComponent<Renderer>());

            Lever lever = root.AddComponent<Lever>();
            lever.handlePivot = pivot.transform;
            return root;
        }

        private static GameObject BuildTriggerZone()
        {
            GameObject root = new GameObject("TriggerZone");
            AddBox(root, new Vector3(0f, 1f, 0f), new Vector3(2f, 2f, 2f), trigger: true);

            Part("Floor Pad", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.008f, 0f), new Vector3(1.9f, 0.016f, 1.9f), mechanicDark);
            GameObject border = Empty("Border", root.transform, Vector3.zero);
            Part("Edge", PrimitiveType.Cube, border.transform, new Vector3(0f, 0.015f, 0.95f), new Vector3(2f, 0.02f, 0.1f), accentInactive);
            Part("Edge", PrimitiveType.Cube, border.transform, new Vector3(0f, 0.015f, -0.95f), new Vector3(2f, 0.02f, 0.1f), accentInactive);
            Part("Edge", PrimitiveType.Cube, border.transform, new Vector3(0.95f, 0.015f, 0f), new Vector3(0.1f, 0.02f, 1.8f), accentInactive);
            Part("Edge", PrimitiveType.Cube, border.transform, new Vector3(-0.95f, 0.015f, 0f), new Vector3(0.1f, 0.02f, 1.8f), accentInactive);
            AddStateVisual(border);

            root.AddComponent<TriggerZone>();
            return root;
        }

        private static GameObject BuildDoor()
        {
            // Fits the 2.4 m x 3 m opening of WallPanel_Doorway. Two halves slide apart into the wall.
            GameObject root = new GameObject("PuzzleDoor");

            Part("Post Left", PrimitiveType.Cube, root.transform, new Vector3(-1.1f, 1.5f, 0f), new Vector3(0.2f, 3f, 0.4f), mechanic, collider: true);
            Part("Post Right", PrimitiveType.Cube, root.transform, new Vector3(1.1f, 1.5f, 0f), new Vector3(0.2f, 3f, 0.4f), mechanic, collider: true);
            Part("Header", PrimitiveType.Cube, root.transform, new Vector3(0f, 2.9f, 0f), new Vector3(2f, 0.2f, 0.4f), mechanic, collider: true);

            // Indicator strips on both faces of the frame: blue = locked, green = unlocked.
            GameObject lights = Empty("Status Lights", root.transform, Vector3.zero);
            foreach (float z in new[] { -0.205f, 0.205f })
            {
                Part("Post Strip", PrimitiveType.Cube, lights.transform, new Vector3(-1.1f, 1.4f, z), new Vector3(0.05f, 2.4f, 0.02f), accentInactive);
                Part("Post Strip", PrimitiveType.Cube, lights.transform, new Vector3(1.1f, 1.4f, z), new Vector3(0.05f, 2.4f, 0.02f), accentInactive);
                Part("Header Strip", PrimitiveType.Cube, lights.transform, new Vector3(0f, 2.9f, z), new Vector3(1.6f, 0.06f, 0.02f), accentInactive);
            }
            StateVisual statusVisual = AddStateVisual(lights);

            PuzzleDoor door = root.AddComponent<PuzzleDoor>();
            door.panels = new[]
            {
                new PuzzleDoor.SlidingPanel { panel = BuildDoorPanel(root.transform, -1f), openOffset = new Vector3(-1f, 0f, 0f) },
                new PuzzleDoor.SlidingPanel { panel = BuildDoorPanel(root.transform, 1f), openOffset = new Vector3(1f, 0f, 0f) }
            };
            door.statusLights = statusVisual;
            return root;
        }

        /// <summary>One door half. side = -1 for left, +1 for right.</summary>
        private static Transform BuildDoorPanel(Transform door, float side)
        {
            GameObject panel = Empty(side < 0 ? "Panel Left" : "Panel Right", door, new Vector3(0.5f * side, 1.4f, 0f));
            AddBox(panel, Vector3.zero, new Vector3(1f, 2.8f, 0.12f));
            Rigidbody body = panel.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            Part("Slab", PrimitiveType.Cube, panel.transform, Vector3.zero, new Vector3(1f, 2.8f, 0.12f), mechanic);
            Part("Meeting Edge", PrimitiveType.Cube, panel.transform, new Vector3(-0.47f * side, 0f, 0f), new Vector3(0.06f, 2.8f, 0.16f), mechanicDark);
            Part("Kick Plate", PrimitiveType.Cube, panel.transform, new Vector3(0f, -1.2f, 0f), new Vector3(0.94f, 0.3f, 0.14f), mechanicDark);
            Part("Window Band", PrimitiveType.Cube, panel.transform, new Vector3(0.05f * side, 0.55f, 0f), new Vector3(0.7f, 0.1f, 0.14f), mechanicDark);
            return panel.transform;
        }

        private static GameObject BuildLogicGate(GateType type)
        {
            string label = type.ToString().ToUpperInvariant();
            GameObject root = new GameObject("LogicGate_" + label);
            GameObject body = LogicBox(root);

            GameObject top = Part("Top Panel", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.465f, 0f), new Vector3(0.5f, 0.03f, 0.5f), accentInactive);
            AddStateVisual(top);
            CreateLabel(root.transform, label, new Vector3(0f, 0.85f, 0f), 0.3f, 1.2f);
            CreateLabel(root.transform, label, new Vector3(0f, 0.25f, -0.31f), 0.14f, 0.55f, color: Color.white);

            LogicGate gate = root.AddComponent<LogicGate>();
            gate.gateType = type;
            return root;
        }

        private static GameObject BuildTimer()
        {
            GameObject root = new GameObject("TimerDelay");
            LogicBox(root);

            GameObject face = Cylinder("Clock Face", root.transform, new Vector3(0f, 0.25f, -0.31f), 0.15f, 0.02f, accentInactive, alongZ: true);
            Part("Clock Hand", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.3f, -0.325f), new Vector3(0.02f, 0.11f, 0.01f), mechanicDark);
            Part("Clock Hand", PrimitiveType.Cube, root.transform, new Vector3(0.035f, 0.25f, -0.325f), new Vector3(0.08f, 0.02f, 0.01f), mechanicDark);
            Part("Top Panel", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.465f, 0f), new Vector3(0.5f, 0.03f, 0.5f), mechanicDark);
            AddStateVisual(face);
            CreateLabel(root.transform, "DELAY", new Vector3(0f, 0.85f, 0f), 0.3f, 1.6f);

            root.AddComponent<TimerDelay>();
            return root;
        }

        /// <summary>The small box shared by logic gates and timers: base, body and in/out connectors.</summary>
        private static GameObject LogicBox(GameObject root)
        {
            Part("Base", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.025f, 0f), new Vector3(0.8f, 0.05f, 0.8f), mechanicDark, collider: true);
            GameObject body = Part("Body", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.25f, 0f), new Vector3(0.6f, 0.4f, 0.6f), mechanic, collider: true);
            Part("Input", PrimitiveType.Cube, root.transform, new Vector3(-0.33f, 0.2f, 0.12f), new Vector3(0.08f, 0.1f, 0.1f), mechanicDark);
            Part("Input", PrimitiveType.Cube, root.transform, new Vector3(-0.33f, 0.2f, -0.12f), new Vector3(0.08f, 0.1f, 0.1f), mechanicDark);
            Part("Output", PrimitiveType.Cube, root.transform, new Vector3(0.33f, 0.2f, 0f), new Vector3(0.08f, 0.14f, 0.14f), mechanicDark);
            return body;
        }

        private static GameObject BuildDispenser(GameObject cubePrefab)
        {
            // A tube hanging from an overhead gantry. Objects appear inside the tube and drop out of the bottom.
            GameObject root = new GameObject("ObjectDispenser");

            Part("Gantry Post", PrimitiveType.Cube, root.transform, new Vector3(-1.2f, 2.3f, 0f), new Vector3(0.2f, 4.6f, 0.2f), mechanicDark, collider: true);
            Part("Gantry Post", PrimitiveType.Cube, root.transform, new Vector3(1.2f, 2.3f, 0f), new Vector3(0.2f, 4.6f, 0.2f), mechanicDark, collider: true);
            Part("Gantry Beam", PrimitiveType.Cube, root.transform, new Vector3(0f, 4.45f, 0f), new Vector3(2.6f, 0.3f, 0.4f), mechanicDark, collider: true);

            Cylinder("Housing", root.transform, new Vector3(0f, 4.05f, 0f), 0.75f, 0.5f, mechanic);
            Ring("Top Collar", root.transform, new Vector3(0f, 3.77f, 0f), 0.68f, 16, 0.08f, 0.1f, mechanicDark);
            // The tube walls are vertical slats with real colliders so falling objects are guided down.
            Ring("Tube Slat", root.transform, new Vector3(0f, 2.8f, 0f), 0.62f, 12, 2f, 0.05f, mechanic, widthFactor: 0.55f, collider: true);
            Ring("Bottom Lip", root.transform, new Vector3(0f, 1.82f, 0f), 0.68f, 16, 0.1f, 0.1f, accentInactive);

            Cylinder("Drop Pad", root.transform, new Vector3(0f, 0.01f, 0f), 0.75f, 0.02f, mechanicDark);

            GameObject spawn = Empty("Spawn Point", root.transform, new Vector3(0f, 3.1f, 0f));

            ObjectDispenser dispenser = root.AddComponent<ObjectDispenser>();
            dispenser.spawnPoint = spawn.transform;
            dispenser.objectPrefab = cubePrefab;
            return root;
        }

        private static GameObject BuildIndicatorStrip()
        {
            // 1 m long floor strip along +Z. Stretch it with the root's Z scale.
            GameObject root = new GameObject("IndicatorLight_FloorStrip");
            Part("Channel", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.006f, 0f), new Vector3(0.26f, 0.012f, 1f), mechanicDark);
            GameObject strip = Part("Light Strip", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.014f, 0f), new Vector3(0.14f, 0.012f, 1f), accentInactive);
            StateVisual visual = AddStateVisual(strip);
            visual.onGlow = 2.5f;
            visual.offGlow = 0.6f;

            IndicatorLight light = root.AddComponent<IndicatorLight>();
            light.visual = visual;
            light.inputMode = InputMode.Any;
            return root;
        }

        private static GameObject BuildIndicatorLamp()
        {
            // A small post with a round lamp. Place a row of these beside a path.
            GameObject root = new GameObject("IndicatorLight_Lamp");
            Cylinder("Base", root.transform, new Vector3(0f, 0.015f, 0f), 0.1f, 0.03f, mechanicDark);
            Cylinder("Post", root.transform, new Vector3(0f, 0.25f, 0f), 0.035f, 0.44f, mechanic);
            GameObject bulb = Part("Lamp", PrimitiveType.Sphere, root.transform, new Vector3(0f, 0.52f, 0f), Vector3.one * 0.16f, accentInactive);
            StateVisual visual = AddStateVisual(bulb);
            visual.onGlow = 3f;
            visual.offGlow = 0.6f;

            IndicatorLight light = root.AddComponent<IndicatorLight>();
            light.visual = visual;
            light.inputMode = InputMode.Any;
            return root;
        }

        private static GameObject BuildFizzler()
        {
            // Fits the 2.4 m opening of WallPanel_Doorway.
            GameObject root = new GameObject("Fizzler");
            AddBox(root, new Vector3(0f, 1.45f, 0f), new Vector3(2.2f, 2.9f, 0.4f), trigger: true);

            foreach (float side in new[] { -1f, 1f })
            {
                Part("Emitter", PrimitiveType.Cube, root.transform, new Vector3(1.1f * side, 1.5f, 0f), new Vector3(0.2f, 3f, 0.3f), mechanic, collider: true);
                Part("Emitter Cap", PrimitiveType.Cube, root.transform, new Vector3(1.1f * side, 2.95f, 0f), new Vector3(0.26f, 0.1f, 0.36f), mechanicDark);
                Part("Emitter Glow", PrimitiveType.Cube, root.transform, new Vector3(1.0f * side, 1.45f, 0f), new Vector3(0.02f, 2.7f, 0.1f), fizzlerField);
            }

            GameObject field = Empty("Field", root.transform, Vector3.zero);
            for (int i = 0; i < 10; i++)
            {
                Part("Field Line", PrimitiveType.Cube, field.transform, new Vector3(0f, 0.2f + i * 0.28f, 0f),
                    new Vector3(2f, 0.02f, 0.02f), fizzlerField);
            }

            Fizzler fizzler = root.AddComponent<Fizzler>();
            fizzler.fieldVisual = field;
            return root;
        }

        // ---------------- Environment ----------------
        // Built on a 4 m grid. Floor top is at y = 0. Walls are 4 m tall and 0.3 m thick.

        private static GameObject BuildFloorTile()
        {
            GameObject root = new GameObject("FloorTile_4x4");
            AddBox(root, new Vector3(0f, -0.1f, 0f), new Vector3(4f, 0.2f, 4f));
            Part("Slab", PrimitiveType.Cube, root.transform, new Vector3(0f, -0.11f, 0f), new Vector3(4f, 0.2f, 4f), floorSeam);
            Part("Surface", PrimitiveType.Cube, root.transform, new Vector3(0f, -0.005f, 0f), new Vector3(3.92f, 0.02f, 3.92f), floorTile);
            return root;
        }

        private static GameObject BuildWallPanel()
        {
            // 4 m wide along X, faces both +Z and -Z.
            GameObject root = new GameObject("WallPanel_4x4");
            AddBox(root, new Vector3(0f, 2f, 0f), new Vector3(4f, 4f, 0.3f));
            Part("Panel", PrimitiveType.Cube, root.transform, new Vector3(0f, 2f, 0f), new Vector3(4f, 4f, 0.3f), roomLight);
            WallTrim(root.transform, -2f, 2f);
            Part("Seam", PrimitiveType.Cube, root.transform, new Vector3(-1.97f, 2f, 0f), new Vector3(0.06f, 4f, 0.34f), roomTrim);
            Part("Seam", PrimitiveType.Cube, root.transform, new Vector3(1.97f, 2f, 0f), new Vector3(0.06f, 4f, 0.34f), roomTrim);
            Part("Reveal", PrimitiveType.Cube, root.transform, new Vector3(0f, 2f, 0f), new Vector3(4f, 0.04f, 0.32f), roomTrim);
            return root;
        }

        private static GameObject BuildWallDoorway()
        {
            // Same footprint as a wall panel, with a 2.4 m wide x 3 m tall opening in the middle.
            GameObject root = new GameObject("WallPanel_Doorway");
            Part("Side Left", PrimitiveType.Cube, root.transform, new Vector3(-1.6f, 2f, 0f), new Vector3(0.8f, 4f, 0.3f), roomLight, collider: true);
            Part("Side Right", PrimitiveType.Cube, root.transform, new Vector3(1.6f, 2f, 0f), new Vector3(0.8f, 4f, 0.3f), roomLight, collider: true);
            Part("Header", PrimitiveType.Cube, root.transform, new Vector3(0f, 3.5f, 0f), new Vector3(2.4f, 1f, 0.3f), roomLight, collider: true);
            WallTrim(root.transform, -2f, -1.2f);
            WallTrim(root.transform, 1.2f, 2f);
            Part("Top Cap", PrimitiveType.Cube, root.transform, new Vector3(0f, 3.95f, 0f), new Vector3(4f, 0.1f, 0.36f), roomTrim);
            Part("Seam", PrimitiveType.Cube, root.transform, new Vector3(-1.97f, 2f, 0f), new Vector3(0.06f, 4f, 0.34f), roomTrim);
            Part("Seam", PrimitiveType.Cube, root.transform, new Vector3(1.97f, 2f, 0f), new Vector3(0.06f, 4f, 0.34f), roomTrim);
            Part("Lintel Trim", PrimitiveType.Cube, root.transform, new Vector3(0f, 3.03f, 0f), new Vector3(2.4f, 0.06f, 0.34f), roomTrim);
            return root;
        }

        /// <summary>Skirting board and top cap between two X positions.</summary>
        private static void WallTrim(Transform wall, float fromX, float toX)
        {
            float width = toX - fromX;
            float centre = (fromX + toX) * 0.5f;
            Part("Skirting", PrimitiveType.Cube, wall, new Vector3(centre, 0.125f, 0f), new Vector3(width, 0.25f, 0.36f), roomTrim);
            Part("Top Cap", PrimitiveType.Cube, wall, new Vector3(centre, 3.95f, 0f), new Vector3(width, 0.1f, 0.36f), roomTrim);
        }

        private static GameObject BuildLedge()
        {
            // A 4 x 4 m platform, 1 m high. Pair it with Steps.
            GameObject root = new GameObject("Platform_Ledge");
            AddBox(root, new Vector3(0f, 0.5f, 0f), new Vector3(4f, 1f, 4f));
            Part("Body", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.48f, 0f), new Vector3(3.96f, 0.96f, 3.96f), roomTrim);
            Part("Top", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.98f, 0f), new Vector3(4.04f, 0.04f, 4.04f), roomLight);
            Part("Edge Lip", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.93f, -2.01f), new Vector3(4.04f, 0.06f, 0.04f), mechanicDark);
            Part("Edge Lip", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.93f, 2.01f), new Vector3(4.04f, 0.06f, 0.04f), mechanicDark);
            Part("Edge Lip", PrimitiveType.Cube, root.transform, new Vector3(-2.01f, 0.93f, 0f), new Vector3(0.04f, 0.06f, 4.04f), mechanicDark);
            Part("Edge Lip", PrimitiveType.Cube, root.transform, new Vector3(2.01f, 0.93f, 0f), new Vector3(0.04f, 0.06f, 4.04f), mechanicDark);
            return root;
        }

        private static GameObject BuildSteps()
        {
            // Four 0.25 m steps, 2 m wide, rising towards +Z up to 1 m (the ledge height).
            const int count = 4;
            const float rise = 0.25f;
            const float run = 0.4f;
            GameObject root = new GameObject("Steps");
            for (int i = 0; i < count; i++)
            {
                float height = (i + 1) * rise;
                Part("Step", PrimitiveType.Cube, root.transform, new Vector3(0f, height * 0.5f, run * (i + 0.5f)),
                    new Vector3(2f, height, run), roomTrim, collider: true);
                Part("Nosing", PrimitiveType.Cube, root.transform, new Vector3(0f, height - 0.015f, run * i + 0.025f),
                    new Vector3(2.02f, 0.035f, 0.06f), mechanicDark);
            }
            Part("Side", PrimitiveType.Cube, root.transform, new Vector3(-1.02f, 0.5f, 0.8f), new Vector3(0.04f, 1f, 1.6f), mechanicDark);
            Part("Side", PrimitiveType.Cube, root.transform, new Vector3(1.02f, 0.5f, 0.8f), new Vector3(0.04f, 1f, 1.6f), mechanicDark);
            return root;
        }

        // ---------------- Player ----------------

        private static GameObject BuildPlayer()
        {
            GameObject root = new GameObject("TestPlayer");
            root.tag = "Player";

            CharacterController controller = root.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.35f;

            GameObject cameraObject = Empty("PlayerCamera", root.transform, new Vector3(0f, 1.6f, 0f));
            Camera cam = cameraObject.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.fieldOfView = 70f;
            cameraObject.AddComponent<AudioListener>();
            if (IsHdrp)
                AddComponentByName(cameraObject, "UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData, Unity.RenderPipelines.HighDefinition.Runtime");

            TestPlayerController move = root.AddComponent<TestPlayerController>();
            move.playerCamera = cam;
            PlayerCarrier carrier = root.AddComponent<PlayerCarrier>();
            carrier.playerCamera = cam;
            PlayerInteractor interactor = root.AddComponent<PlayerInteractor>();
            interactor.playerCamera = cam;
            interactor.carrier = carrier;
            PlayerHUD hud = root.AddComponent<PlayerHUD>();
            hud.interactor = interactor;
            hud.carrier = carrier;
            hud.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BodyFontPath);
            return root;
        }

        // ------------------------------------------------------------------
        // Test scene
        // ------------------------------------------------------------------
        // Layout (4 m grid): a corridor runs along X (z = -12 to -4). Each station is a 12 m wide bay
        // (x = -6 to 6 locally, z = -4 to 8) with its door in the back wall at z = 8 and a small room
        // behind it (z = 8 to 12). The final puzzle is a 24 m wide room at the far end.

        private const float StationWidth = 12f;
        private static PrefabSet prefabs;

        private static void BuildTestScene(PrefabSet p)
        {
            prefabs = p;
            EnsureFolder(SceneDir);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateSun();

            Transform level = new GameObject("Level").transform;
            Transform stations = new GameObject("Stations").transform;
            int count = 0;

            BuildPlateStation(Station(stations, count++, "Pressure Plate",
                "Put 15 kg on the plate: a light cube, or just stand on it.", out PuzzleDoor d1), d1);
            BuildMassStation(Station(stations, count++, "Mass Rule",
                "The plate needs 100 kg and ignores players. You can't lift the heavy block: push it.", out PuzzleDoor d2), d2);
            BuildButtonStation(Station(stations, count++, "Wall Buttons",
                "Toggle, momentary (hold E) and timed. Any button opens the door (OR).", out PuzzleDoor d3), d3);
            BuildTargetStation(Station(stations, count++, "Impact Targets",
                "Pick up a ball and throw it (Right Mouse) at a target.", out PuzzleDoor d4), d4);
            BuildLeverStation(Station(stations, count++, "Lever",
                "Press E on the lever to toggle the door.", out PuzzleDoor d5), d5);
            BuildGateStation(Station(stations, count++, "Logic Gates",
                "Both levers -> AND -> door.  NOT lamp: lever A off.  OR lamp: either lever on.", out PuzzleDoor d6), d6);
            BuildTimerStation(Station(stations, count++, "Timer / Delay",
                "Opens 2 s after the pedestal button turns on, closes 3 s after it turns off.", out PuzzleDoor d7), d7);
            BuildDispenserStation(Station(stations, count++, "Object Dispenser",
                "The pedestal button drops a cube (replacing the old one). Put it on the plate.", out PuzzleDoor d8), d8);
            BuildIndicatorStation(Station(stations, count++, "Indicator Lights + Stay-Open Door",
                "Follow the lights from the button to the door. This door stays open once opened.", out PuzzleDoor d9), d9);
            BuildZoneStation(Station(stations, count++, "Trigger Zone",
                "Stand inside the square.", out PuzzleDoor d10), d10);
            BuildFizzlerStation(Station(stations, count++, "Fizzler",
                "Cubes are destroyed by the field (you are not). The lever switches it off.", out PuzzleDoor d11), d11);

            // The final room starts where the last station ends.
            float finalCentreX = (count - 1) * StationWidth + StationWidth * 0.5f + 12f;
            BuildFinalPuzzle(stations, finalCentreX);

            // Floor tiles, corridor wall and end walls, all on the 4 m grid.
            float minX = -StationWidth * 0.5f;
            float maxX = finalCentreX + 12f;
            Transform floor = new GameObject("Floor").transform;
            floor.SetParent(level, false);
            for (float x = minX + 2f; x < maxX; x += 4f)
            {
                for (float z = -10f; z <= 14f; z += 4f)
                    PlaceObject(prefabs.floorTile, floor, new Vector3(x, 0f, z));
            }

            Transform walls = new GameObject("Corridor Walls").transform;
            walls.SetParent(level, false);
            for (float x = minX + 2f; x < maxX; x += 4f)
                PlaceObject(prefabs.wallPanel, walls, new Vector3(x, 0f, -12f));
            foreach (float z in new[] { -10f, -6f })
            {
                PlaceObject(prefabs.wallPanel, walls, new Vector3(minX, 0f, z), 90f);
                PlaceObject(prefabs.wallPanel, walls, new Vector3(maxX, 0f, z), 90f);
            }

            CreateLabel(level, "PUZZLE MECHANICS TEST", new Vector3(0f, 3f, -11.8f), 0.5f, 10f, rotationY: 180f);
            CreateLabel(level, "Stations are along this corridor. Final puzzle at the far end.",
                new Vector3(0f, 2.4f, -11.8f), 0.2f, 10f, rotationY: 180f);

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.player);
            player.transform.position = new Vector3(0f, 0.05f, -8f);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void CreateSun()
        {
            GameObject sun = new GameObject("Sun");
            sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;

            if (IsHdrp)
            {
                AddComponentByName(sun, "UnityEngine.Rendering.HighDefinition.HDAdditionalLightData, Unity.RenderPipelines.HighDefinition.Runtime");
                light.intensity = 100000f; // HDRP uses real-world units (lux); this matches the project's other scenes
            }
            else
            {
                light.intensity = 1.2f;
            }
        }

        /// <summary>Builds the walls, door and labels shared by every station and returns the station root.</summary>
        private static Transform Station(Transform parent, int index, string title, string description, out PuzzleDoor door)
        {
            Transform root = new GameObject($"Station {index + 1:00} - {title}").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(index * StationWidth, 0f, 0f);

            Transform walls = new GameObject("Walls").transform;
            walls.SetParent(root, false);
            PlaceObject(prefabs.wallPanel, walls, new Vector3(-4f, 0f, 8f));
            PlaceObject(prefabs.wallDoorway, walls, new Vector3(0f, 0f, 8f));
            PlaceObject(prefabs.wallPanel, walls, new Vector3(4f, 0f, 8f));
            foreach (float x in new[] { -4f, 0f, 4f })
                PlaceObject(prefabs.wallPanel, walls, new Vector3(x, 0f, 12f));
            foreach (float z in new[] { -2f, 2f, 6f, 10f })
            {
                PlaceObject(prefabs.wallPanel, walls, new Vector3(6f, 0f, z), 90f);
                if (index == 0)
                    PlaceObject(prefabs.wallPanel, walls, new Vector3(-6f, 0f, z), 90f);
            }

            door = Place<PuzzleDoor>(prefabs.door, root, new Vector3(0f, 0f, 8f));

            CreateLabel(root, title, new Vector3(0f, 5.1f, 7.8f), 0.55f, 11.5f);
            CreateLabel(root, description, new Vector3(0f, 4.45f, 7.8f), 0.22f, 11.5f);
            return root;
        }

        private static void BuildPlateStation(Transform s, PuzzleDoor door)
        {
            PressurePlate plate = Place<PressurePlate>(prefabs.plate, s, new Vector3(0f, 0f, 3f));
            Place<Carryable>(prefabs.cube, s, new Vector3(-3f, 0.31f, 0f));
            Strip(s, new Vector3(0f, 0f, 4.3f), new Vector3(0f, 0f, 7.6f), plate);
            Wire(door, InputMode.All, plate);
        }

        private static void BuildMassStation(Transform s, PuzzleDoor door)
        {
            PressurePlate plate = Place<PressurePlate>(prefabs.plate, s, new Vector3(2f, 0f, 4f));
            plate.requiredMass = 100f;
            plate.countPlayers = false;
            Dirty(plate);
            CreateLabel(s, "100 kg plate", new Vector3(2f, 1.3f, 4f), 0.22f, 3f);

            Place<Carryable>(prefabs.ball, s, new Vector3(-4.8f, 0.21f, 0.5f));
            CreateLabel(s, "5 kg\nthrow", new Vector3(-4.8f, 0.9f, 0.5f), 0.16f, 1.5f);
            Place<Carryable>(prefabs.cube, s, new Vector3(-3.6f, 0.31f, 0.5f));
            CreateLabel(s, "15 kg\nlift", new Vector3(-3.6f, 1.05f, 0.5f), 0.16f, 1.5f);
            Place<Carryable>(prefabs.crate, s, new Vector3(-1.8f, 0.46f, 0.5f));
            CreateLabel(s, "45 kg\nlift (slow)", new Vector3(-1.8f, 1.3f, 0.5f), 0.16f, 1.8f);
            Place<Carryable>(prefabs.heavyBlock, s, new Vector3(2f, 0.61f, -0.5f));
            CreateLabel(s, "120 kg - can't lift, push it", new Vector3(2f, 1.6f, -0.5f), 0.18f, 3.5f);

            StripPath(s, new PuzzleActivator[] { plate }, new Vector3(2f, 0f, 5.3f), new Vector3(2f, 0f, 6.6f),
                new Vector3(0f, 0f, 6.6f), new Vector3(0f, 0f, 7.6f));
            Wire(door, InputMode.All, plate);
        }

        private static void BuildButtonStation(Transform s, PuzzleDoor door)
        {
            WallButton toggle = WallButtonOnWall(s, -4.5f, ButtonMode.Toggle, 3f, "Toggle");
            WallButton momentary = WallButtonOnWall(s, -3f, ButtonMode.Momentary, 3f, "Momentary");
            WallButton timed = WallButtonOnWall(s, 3.5f, ButtonMode.Timed, 4f, "Timed (4 s)");

            StripPath(s, new PuzzleActivator[] { toggle }, new Vector3(-4.5f, 0f, 7.6f), new Vector3(-4.5f, 0f, 6.9f), new Vector3(-1.4f, 0f, 6.9f), new Vector3(-1.4f, 0f, 7.4f));
            Strip(s, new Vector3(-3f, 0f, 7.6f), new Vector3(-1.4f, 0f, 7.6f), momentary);
            Strip(s, new Vector3(3.5f, 0f, 7.6f), new Vector3(1.4f, 0f, 7.6f), timed);
            Wire(door, InputMode.Any, toggle, momentary, timed);
        }

        private static void BuildTargetStation(Transform s, PuzzleDoor door)
        {
            ImpactTarget latching = Place<ImpactTarget>(prefabs.target, s, new Vector3(-3.5f, 2.2f, 7.85f), 180f);
            CreateLabel(s, "Stays on", new Vector3(-3.5f, 1.15f, 7.8f), 0.22f, 3f);

            ImpactTarget resetting = Place<ImpactTarget>(prefabs.target, s, new Vector3(3.5f, 2.2f, 7.85f), 180f);
            resetting.stayActivated = false;
            resetting.resetAfter = 4f;
            Dirty(resetting);
            CreateLabel(s, "Resets after 4 s", new Vector3(3.5f, 1.15f, 7.8f), 0.22f, 3f);

            Place<Carryable>(prefabs.ball, s, new Vector3(-1f, 0.21f, -1f));
            Place<Carryable>(prefabs.ball, s, new Vector3(0f, 0.21f, -1.5f));
            Place<Carryable>(prefabs.ball, s, new Vector3(1f, 0.21f, -1f));

            Strip(s, new Vector3(-3.5f, 0f, 7.6f), new Vector3(-1.4f, 0f, 7.6f), latching);
            Strip(s, new Vector3(3.5f, 0f, 7.6f), new Vector3(1.4f, 0f, 7.6f), resetting);
            Wire(door, InputMode.Any, latching, resetting);
        }

        private static void BuildLeverStation(Transform s, PuzzleDoor door)
        {
            Lever lever = Place<Lever>(prefabs.lever, s, new Vector3(0f, 0f, 3f));
            Strip(s, new Vector3(0f, 0f, 3.5f), new Vector3(0f, 0f, 7.6f), lever);
            Wire(door, InputMode.All, lever);
        }

        private static void BuildGateStation(Transform s, PuzzleDoor door)
        {
            Lever leverA = Place<Lever>(prefabs.lever, s, new Vector3(-3f, 0f, 0f));
            CreateLabel(s, "Lever A", new Vector3(-3f, 1.5f, 0f), 0.22f, 2f);
            Lever leverB = Place<Lever>(prefabs.lever, s, new Vector3(3f, 0f, 0f));
            CreateLabel(s, "Lever B", new Vector3(3f, 1.5f, 0f), 0.22f, 2f);

            LogicGate andGate = PlaceGate(prefabs.gateAnd, s, new Vector3(0f, 0f, 4.5f), leverA, leverB);
            LogicGate notGate = PlaceGate(prefabs.gateNot, s, new Vector3(-4.5f, 0f, 4.5f), leverA);
            LogicGate orGate = PlaceGate(prefabs.gateOr, s, new Vector3(4.5f, 0f, 4.5f), leverA, leverB);

            Strip(s, new Vector3(-3f, 0f, 0.5f), new Vector3(-3f, 0f, 4.5f), leverA);
            Strip(s, new Vector3(-3f, 0f, 4.5f), new Vector3(-0.45f, 0f, 4.5f), leverA);
            Strip(s, new Vector3(-3f, 0f, 4.5f), new Vector3(-4.05f, 0f, 4.5f), leverA);
            Strip(s, new Vector3(3f, 0f, 0.5f), new Vector3(3f, 0f, 4.5f), leverB);
            Strip(s, new Vector3(3f, 0f, 4.5f), new Vector3(0.45f, 0f, 4.5f), leverB);
            Strip(s, new Vector3(3f, 0f, 4.5f), new Vector3(4.05f, 0f, 4.5f), leverB);

            Strip(s, new Vector3(0f, 0f, 4.95f), new Vector3(0f, 0f, 7.6f), andGate);
            Strip(s, new Vector3(-4.5f, 0f, 4.95f), new Vector3(-4.5f, 0f, 6.8f), notGate);
            Strip(s, new Vector3(4.5f, 0f, 4.95f), new Vector3(4.5f, 0f, 6.8f), orGate);
            Lamp(s, new Vector3(-4.5f, 0f, 7.1f), notGate);
            Lamp(s, new Vector3(4.5f, 0f, 7.1f), orGate);

            Wire(door, InputMode.All, andGate);
        }

        private static void BuildTimerStation(Transform s, PuzzleDoor door)
        {
            WallButton button = Pedestal(s, new Vector3(0f, 0f, 0f), ButtonMode.Toggle, 3f, "Toggle");
            TimerDelay timer = Place<TimerDelay>(prefabs.timer, s, new Vector3(0f, 0f, 4f));
            timer.input = button;
            timer.activateDelay = 2f;
            timer.deactivateDelay = 3f;
            Dirty(timer);
            CreateLabel(s, "on after 2 s\noff after 3 s", new Vector3(1.6f, 0.6f, 4f), 0.16f, 2f);

            Strip(s, new Vector3(0f, 0f, 0.4f), new Vector3(0f, 0f, 3.55f), button);
            Strip(s, new Vector3(0f, 0f, 4.45f), new Vector3(0f, 0f, 7.6f), timer);
            Wire(door, InputMode.All, timer);
        }

        private static void BuildDispenserStation(Transform s, PuzzleDoor door)
        {
            WallButton button = Pedestal(s, new Vector3(-4.5f, 0f, -1f), ButtonMode.Timed, 1f, "Dispense");
            ObjectDispenser dispenser = Place<ObjectDispenser>(prefabs.dispenser, s, new Vector3(-2.5f, 0f, 2.5f));
            Wire(dispenser, InputMode.Any, button);

            PressurePlate plate = Place<PressurePlate>(prefabs.plate, s, new Vector3(3f, 0f, 2.5f));

            StripPath(s, new PuzzleActivator[] { button }, new Vector3(-4.5f, 0f, -0.6f), new Vector3(-4.5f, 0f, 1f),
                new Vector3(-2.5f, 0f, 1f), new Vector3(-2.5f, 0f, 1.75f));
            StripPath(s, new PuzzleActivator[] { plate }, new Vector3(3f, 0f, 3.75f), new Vector3(3f, 0f, 6.6f),
                new Vector3(0f, 0f, 6.6f), new Vector3(0f, 0f, 7.6f));
            Wire(door, InputMode.All, plate);
        }

        private static void BuildIndicatorStation(Transform s, PuzzleDoor door)
        {
            WallButton button = WallButtonOnWall(s, -4.5f, ButtonMode.Timed, 3f, "Timed (3 s)");
            StripPath(s, new PuzzleActivator[] { button }, new Vector3(-4.5f, 0f, 7.6f), new Vector3(-4.5f, 0f, 1f),
                new Vector3(4f, 0f, 1f), new Vector3(4f, 0f, 6.6f), new Vector3(0f, 0f, 6.6f), new Vector3(0f, 0f, 7.6f));

            // A row of lamps beside the path, the other kind of indicator light.
            foreach (float x in new[] { -2.5f, -0.5f, 1.5f })
                Lamp(s, new Vector3(x, 0f, 0.5f), button);

            door.stayOpenOnceOpened = true;
            Wire(door, InputMode.All, button);
        }

        private static void BuildZoneStation(Transform s, PuzzleDoor door)
        {
            TriggerZone zone = Place<TriggerZone>(prefabs.zone, s, new Vector3(0f, 0f, 3f));
            Strip(s, new Vector3(0f, 0f, 4.05f), new Vector3(0f, 0f, 7.6f), zone);
            Wire(door, InputMode.All, zone);
        }

        private static void BuildFizzlerStation(Transform s, PuzzleDoor door)
        {
            // An inner wall with the fizzler in its doorway, between the cube and the plate.
            Transform walls = s.Find("Walls");
            PlaceObject(prefabs.wallPanel, walls, new Vector3(-4f, 0f, 3f));
            PlaceObject(prefabs.wallDoorway, walls, new Vector3(0f, 0f, 3f));
            PlaceObject(prefabs.wallPanel, walls, new Vector3(4f, 0f, 3f));
            Fizzler fizzler = Place<Fizzler>(prefabs.fizzler, s, new Vector3(0f, 0f, 3f));

            ObjectDispenser dispenser = Place<ObjectDispenser>(prefabs.dispenser, s, new Vector3(-3.3f, 0f, -1f));
            dispenser.dispenseOnStart = true;
            Dirty(dispenser);
            CreateLabel(s, "Auto-refilling dispenser", new Vector3(-3.3f, 5f, -1f), 0.2f, 3f);

            Lever lever = Place<Lever>(prefabs.lever, s, new Vector3(3f, 0f, -1.5f));
            CreateLabel(s, "Fizzler off", new Vector3(3f, 1.5f, -1.5f), 0.22f, 2f);
            Wire(fizzler, InputMode.All, lever);
            StripPath(s, new PuzzleActivator[] { lever }, new Vector3(3f, 0f, -1f), new Vector3(3f, 0f, 2.6f), new Vector3(1.3f, 0f, 2.6f));

            PressurePlate plate = Place<PressurePlate>(prefabs.plate, s, new Vector3(0f, 0f, 5.5f));
            Strip(s, new Vector3(0f, 0f, 6.75f), new Vector3(0f, 0f, 7.6f), plate);
            Wire(door, InputMode.All, plate);
        }

        /// <summary>
        /// The combined puzzle: dispenser + ball/target + fizzler/lever + plate on a ledge + heavy block on a heavy plate
        /// -> AND gate -> delay -> final door.
        /// </summary>
        private static void BuildFinalPuzzle(Transform parent, float centreX)
        {
            Transform s = new GameObject("Station 12 - Final Puzzle").transform;
            s.SetParent(parent, false);
            s.localPosition = new Vector3(centreX, 0f, 0f);

            // Walls: 24 m wide room. Inner wall (with the fizzler) at z = 4, final door at z = 12.
            Transform walls = new GameObject("Walls").transform;
            walls.SetParent(s, false);
            float[] columns = { -10f, -6f, -2f, 2f, 6f, 10f };
            foreach (float x in columns)
            {
                PlaceObject(x == 2f ? prefabs.wallDoorway : prefabs.wallPanel, walls, new Vector3(x, 0f, 12f));
                PlaceObject(x == -6f ? prefabs.wallDoorway : prefabs.wallPanel, walls, new Vector3(x, 0f, 4f));
                PlaceObject(prefabs.wallPanel, walls, new Vector3(x, 0f, 16f));
            }
            foreach (float z in new[] { -2f, 2f, 6f, 10f, 14f })
                PlaceObject(prefabs.wallPanel, walls, new Vector3(12f, 0f, z), 90f);
            PlaceObject(prefabs.wallPanel, walls, new Vector3(-12f, 0f, 14f), 90f);

            CreateLabel(s, "FINAL PUZZLE", new Vector3(0f, 5.9f, 3.8f), 0.6f, 20f);
            CreateLabel(s,
                "1. Press the pedestal button to dispense a cube.   2. Throw a ball hard at the target.\n" +
                "3. Pull the lever to switch off the fizzler, then carry the cube up the steps to the plate.\n" +
                "4. Push the heavy block onto the 100 kg plate.   All three -> AND -> 1 s delay -> door.",
                new Vector3(0f, 4.7f, 3.8f), 0.22f, 22f);

            // Front half
            WallButton button = Pedestal(s, new Vector3(-10.5f, 0f, -2.5f), ButtonMode.Timed, 1f, "Dispense");
            ObjectDispenser dispenser = Place<ObjectDispenser>(prefabs.dispenser, s, new Vector3(-8.5f, 0f, 0f));
            Wire(dispenser, InputMode.Any, button);

            Lever lever = Place<Lever>(prefabs.lever, s, new Vector3(-3f, 0f, 0f));
            CreateLabel(s, "Fizzler off", new Vector3(-3f, 1.5f, 0f), 0.22f, 2f);
            Fizzler fizzler = Place<Fizzler>(prefabs.fizzler, s, new Vector3(-6f, 0f, 4f));
            Wire(fizzler, InputMode.All, lever);

            ImpactTarget target = Place<ImpactTarget>(prefabs.target, s, new Vector3(6f, 2.2f, 3.85f), 180f);
            Place<Carryable>(prefabs.ball, s, new Vector3(4f, 0.21f, -1f));
            Place<Carryable>(prefabs.ball, s, new Vector3(5f, 0.21f, -1.5f));

            // Back half: a ledge with steps, and a heavy plate for the heavy block.
            PlaceObject(prefabs.ledge, s, new Vector3(-7f, 0f, 9f));
            PlaceObject(prefabs.steps, s, new Vector3(-7f, 0f, 5.4f));
            PressurePlate ledgePlate = Place<PressurePlate>(prefabs.plate, s, new Vector3(-7f, 1f, 9.2f));
            CreateLabel(s, "15 kg", new Vector3(-7f, 2.3f, 9.2f), 0.25f, 2f);

            PressurePlate heavyPlate = Place<PressurePlate>(prefabs.plate, s, new Vector3(6f, 0f, 8f));
            heavyPlate.requiredMass = 100f;
            heavyPlate.countPlayers = false;
            Dirty(heavyPlate);
            CreateLabel(s, "100 kg", new Vector3(6f, 1.3f, 8f), 0.25f, 2f);
            Place<Carryable>(prefabs.heavyBlock, s, new Vector3(9.5f, 0.61f, 7f));

            LogicGate andGate = PlaceGate(prefabs.gateAnd, s, new Vector3(2f, 0f, 9f), ledgePlate, heavyPlate, target);
            TimerDelay delay = Place<TimerDelay>(prefabs.timer, s, new Vector3(2f, 0f, 10.5f));
            delay.input = andGate;
            delay.activateDelay = 1f;
            Dirty(delay);

            PuzzleDoor door = Place<PuzzleDoor>(prefabs.door, s, new Vector3(2f, 0f, 12f));
            door.stayOpenOnceOpened = true;
            Wire(door, InputMode.All, delay);
            CreateLabel(s, "PUZZLE COMPLETE!", new Vector3(2f, 2.2f, 15.8f), 0.5f, 8f);

            // Signal lines
            StripPath(s, new PuzzleActivator[] { button }, new Vector3(-10.5f, 0f, -2.1f), new Vector3(-10.5f, 0f, 0f), new Vector3(-9.3f, 0f, 0f));
            StripPath(s, new PuzzleActivator[] { lever }, new Vector3(-3f, 0f, 0.5f), new Vector3(-3f, 0f, 3.6f), new Vector3(-4.9f, 0f, 3.6f));
            StripPath(s, new PuzzleActivator[] { ledgePlate }, new Vector3(-4.95f, 0f, 9f), new Vector3(1.55f, 0f, 9f));
            StripPath(s, new PuzzleActivator[] { heavyPlate }, new Vector3(4.75f, 0f, 8f), new Vector3(3f, 0f, 8f), new Vector3(3f, 0f, 9f), new Vector3(2.45f, 0f, 9f));
            StripPath(s, new PuzzleActivator[] { target }, new Vector3(6f, 0f, 3.6f), new Vector3(6f, 0f, 6.3f),
                new Vector3(2f, 0f, 6.3f), new Vector3(2f, 0f, 8.55f));
            Strip(s, new Vector3(2f, 0f, 9.45f), new Vector3(2f, 0f, 10.05f), andGate);
            Strip(s, new Vector3(2f, 0f, 10.95f), new Vector3(2f, 0f, 11.6f), delay);
        }

        // ------------------------------------------------------------------
        // Scene helpers
        // ------------------------------------------------------------------

        private static T Place<T>(GameObject prefab, Transform parent, Vector3 localPosition, float yRotation = 0f) where T : Component
        {
            return PlaceObject(prefab, parent, localPosition, yRotation).GetComponent<T>();
        }

        private static GameObject PlaceObject(GameObject prefab, Transform parent, Vector3 localPosition, float yRotation = 0f)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            return go;
        }

        private static LogicGate PlaceGate(GameObject prefab, Transform parent, Vector3 localPosition, params PuzzleActivator[] inputs)
        {
            LogicGate gate = Place<LogicGate>(prefab, parent, localPosition);
            gate.inputs = new List<PuzzleActivator>(inputs);
            Dirty(gate);
            return gate;
        }

        /// <summary>A wall button on the station's back wall (front face at z = 7.85), facing the player.</summary>
        private static WallButton WallButtonOnWall(Transform parent, float x, ButtonMode mode, float timedDuration, string label)
        {
            WallButton button = Place<WallButton>(prefabs.wallButton, parent, new Vector3(x, 1.2f, 7.85f), 180f);
            button.mode = mode;
            button.timedDuration = timedDuration;
            Dirty(button);
            CreateLabel(parent, label, new Vector3(x, 1.65f, 7.8f), 0.18f, 1.6f);
            return button;
        }

        private static WallButton Pedestal(Transform parent, Vector3 position, ButtonMode mode, float timedDuration, string label)
        {
            WallButton button = Place<WallButton>(prefabs.pedestalButton, parent, position);
            button.mode = mode;
            button.timedDuration = timedDuration;
            Dirty(button);
            CreateLabel(parent, label, position + new Vector3(0f, 1.45f, 0f), 0.2f, 2.4f);
            return button;
        }

        private static void Lamp(Transform parent, Vector3 position, params PuzzleActivator[] inputs)
        {
            IndicatorLight lamp = Place<IndicatorLight>(prefabs.lamp, parent, position);
            Wire(lamp, InputMode.Any, inputs);
        }

        private static void Wire(PuzzleReceiver receiver, InputMode mode, params PuzzleActivator[] inputs)
        {
            receiver.inputs = new List<PuzzleActivator>(inputs);
            receiver.inputMode = mode;
            Dirty(receiver);
        }

        /// <summary>A floor indicator strip from 'from' to 'to' (floor height), lit by the given inputs.</summary>
        private static void Strip(Transform parent, Vector3 from, Vector3 to, params PuzzleActivator[] inputs)
        {
            Vector3 delta = to - from;
            delta.y = 0f;
            float length = delta.magnitude;
            if (length < 0.01f)
                return;

            IndicatorLight strip = Place<IndicatorLight>(prefabs.strip, parent, (from + to) * 0.5f);
            strip.transform.localRotation = Quaternion.LookRotation(delta / length);
            // Slightly longer than the gap so corners overlap neatly.
            strip.transform.localScale = new Vector3(1f, 1f, length + 0.14f);
            Wire(strip, InputMode.Any, inputs);
        }

        private static void StripPath(Transform parent, PuzzleActivator[] inputs, params Vector3[] points)
        {
            for (int i = 0; i < points.Length - 1; i++)
                Strip(parent, points[i], points[i + 1], inputs);
        }

        /// <summary>Saves changes made to a prefab instance in the scene.</summary>
        private static void Dirty(Component component)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }

        // ------------------------------------------------------------------
        // Model-building helpers
        // ------------------------------------------------------------------

        /// <summary>A primitive child. Visual-only (no collider) unless 'collider' is true.</summary>
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

        /// <summary>A cylinder by radius and height (Unity's cylinder is 1 wide and 2 tall). Upright, or facing +Z.</summary>
        private static GameObject Cylinder(string name, Transform parent, Vector3 localPosition, float radius, float height,
            Material mat, bool alongZ = false)
        {
            return Part(name, PrimitiveType.Cylinder, parent, localPosition, new Vector3(radius * 2f, height * 0.5f, radius * 2f),
                mat, euler: alongZ ? new Vector3(90f, 0f, 0f) : Vector3.zero);
        }

        /// <summary>A flat ring of box segments around the Y axis (used for the plate rim and the dispenser tube).</summary>
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

        /// <summary>The 12 edge bars of a cube (used for bevelled-looking pickups).</summary>
        private static void EdgeFrame(Transform parent, float offset, float length, float thickness, Material mat)
        {
            foreach (float a in new[] { -offset, offset })
            {
                foreach (float b in new[] { -offset, offset })
                {
                    Part("Edge", PrimitiveType.Cube, parent, new Vector3(0f, a, b), new Vector3(length, thickness, thickness), mat);
                    Part("Edge", PrimitiveType.Cube, parent, new Vector3(a, 0f, b), new Vector3(thickness, length, thickness), mat);
                    Part("Edge", PrimitiveType.Cube, parent, new Vector3(a, b, 0f), new Vector3(thickness, thickness, length), mat);
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

        private static Vector3[] FaceDirections() =>
            new[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

        private static GameObject Empty(string name, Transform parent, Vector3 localPosition)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
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

        /// <summary>
        /// A floating world-space TextMesh Pro label. It reads correctly when viewed while looking along its +Z
        /// (use rotationY = 180 for labels that face the other way). Big text uses the heading font.
        /// </summary>
        private static void CreateLabel(Transform parent, string text, Vector3 localPosition, float letterHeight, float width,
            float rotationY = 0f, Color? color = null)
        {
            int lines = text.Split('\n').Length;
            bool heading = letterHeight >= 0.3f;

            GameObject go = new GameObject("Label: " + text.Split('\n')[0]);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(0f, rotationY, 0f);

            TextMeshPro label = go.AddComponent<TextMeshPro>();
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(heading ? HeadingFontPath : BodyFontPath);
            if (font != null)
            {
                label.font = font;
                // White text with a dark outline stays readable against bright walls and dark sky alike.
                Material outline = AssetDatabase.LoadAssetAtPath<Material>(heading ? HeadingOutlinePath : BodyOutlinePath);
                if (outline != null)
                    label.fontSharedMaterial = outline;
            }
            label.text = text;
            label.fontSize = letterHeight * 10f;   // TMP world text: font size 10 is about 1 m tall
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.color = color ?? Color.white;
            label.rectTransform.sizeDelta = new Vector2(width, letterHeight * 1.4f * lines);
        }

        private static GameObject SavePrefab(GameObject temp, string folder, string name)
        {
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
