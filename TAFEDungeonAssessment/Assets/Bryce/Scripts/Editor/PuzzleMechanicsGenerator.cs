using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PuzzleMechanics.EditorTools
{
    /// <summary>
    /// Builds every puzzle prefab and the test scene from code.
    ///
    /// Menu: Tools > Puzzle Mechanics > Generate Prefabs and Test Scene
    ///
    /// Running it again is safe: materials and prefabs are updated in place (so references to
    /// them keep working) and the test scene is rebuilt from scratch.
    /// </summary>
    public static class PuzzleMechanicsGenerator
    {
        private const string Root = "Assets/Bryce";
        private const string PrefabDir = Root + "/Prefabs";
        private const string MaterialDir = Root + "/Materials";
        private const string SceneDir = Root + "/Scenes";
        private const string ScenePath = SceneDir + "/MechanicsTestScene.unity";

        // Colours used for on/off feedback.
        private static readonly Color InputOff = new Color(0.85f, 0.18f, 0.12f);
        private static readonly Color InputOn = new Color(0.15f, 0.95f, 0.35f);
        private static readonly Color StripOff = new Color(0.08f, 0.25f, 0.65f);
        private static readonly Color StripOn = new Color(1f, 0.55f, 0.08f);

        // ------------------------------------------------------------------
        // Menu items
        // ------------------------------------------------------------------

        [MenuItem("Tools/Puzzle Mechanics/Generate Prefabs and Test Scene")]
        public static void GenerateAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // Start from an empty scene so temporary objects never end up in someone else's scene.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrefabSet prefabs = BuildPrefabs();
            BuildTestScene(prefabs);

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
        // Materials
        // ------------------------------------------------------------------

        private static Material floorMat, wallMat, metalMat, darkMetalMat, doorMat, stateMat,
            targetMat, accentMat, fizzlerMat, lightCubeMat, mediumCubeMat, heavyCubeMat;

        private static bool IsHdrp =>
            GraphicsSettings.currentRenderPipeline != null &&
            GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("HDRenderPipeline");

        private static void BuildMaterials()
        {
            EnsureFolder(MaterialDir);
            floorMat = GetOrCreateMaterial("M_Floor", new Color(0.55f, 0.56f, 0.58f));
            wallMat = GetOrCreateMaterial("M_Wall", new Color(0.82f, 0.83f, 0.85f));
            metalMat = GetOrCreateMaterial("M_Metal", new Color(0.35f, 0.37f, 0.4f), smoothness: 0.6f);
            darkMetalMat = GetOrCreateMaterial("M_DarkMetal", new Color(0.15f, 0.16f, 0.18f), smoothness: 0.5f);
            doorMat = GetOrCreateMaterial("M_DoorPanel", new Color(0.62f, 0.66f, 0.72f), smoothness: 0.55f);
            stateMat = GetOrCreateMaterial("M_StateLight", Color.white, smoothness: 0.6f);
            targetMat = GetOrCreateMaterial("M_TargetRing", new Color(0.92f, 0.92f, 0.92f));
            accentMat = GetOrCreateMaterial("M_Accent", StripOn, emission: 1.5f);
            fizzlerMat = GetOrCreateMaterial("M_Fizzler", new Color(0.4f, 0.85f, 1f), emission: 3f);
            lightCubeMat = GetOrCreateMaterial("M_Cube_Light", new Color(0.75f, 0.85f, 0.95f));
            mediumCubeMat = GetOrCreateMaterial("M_Cube_Medium", new Color(0.95f, 0.55f, 0.15f));
            heavyCubeMat = GetOrCreateMaterial("M_Cube_Heavy", new Color(0.45f, 0.08f, 0.08f));
        }

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
            public GameObject plate, button, target, lever, door, gateAnd, gateOr, gateNot, timer,
                dispenser, strip, fizzler, zone, cubeLight, cubeMedium, cubeHeavy, player;
        }

        private static PrefabSet BuildPrefabs()
        {
            BuildMaterials();
            EnsureFolder(PrefabDir);

            PrefabSet p = new PrefabSet();
            // Cubes first: the dispenser prefab references the light cube.
            p.cubeLight = SavePrefab(BuildCube("Cube_Light", 0.8f, 20f, lightCubeMat, "Light Cube"), "Cube_Light");
            p.cubeMedium = SavePrefab(BuildCube("Cube_Medium", 1.0f, 100f, mediumCubeMat, "Heavy Cube"), "Cube_Medium");
            p.cubeHeavy = SavePrefab(BuildCube("Cube_Heavy", 1.3f, 250f, heavyCubeMat, "Very Heavy Crate"), "Cube_Heavy");

            p.plate = SavePrefab(BuildPressurePlate(), "PressurePlate");
            p.button = SavePrefab(BuildWallButton(), "WallButton");
            p.target = SavePrefab(BuildTarget(), "ImpactTarget");
            p.lever = SavePrefab(BuildLever(), "Lever");
            p.zone = SavePrefab(BuildTriggerZone(), "TriggerZone");
            p.door = SavePrefab(BuildDoor(), "PuzzleDoor");
            p.gateAnd = SavePrefab(BuildLogicGate(GateType.And), "LogicGate_AND");
            p.gateOr = SavePrefab(BuildLogicGate(GateType.Or), "LogicGate_OR");
            p.gateNot = SavePrefab(BuildLogicGate(GateType.Not), "LogicGate_NOT");
            p.timer = SavePrefab(BuildTimer(), "TimerDelay");
            p.dispenser = SavePrefab(BuildDispenser(p.cubeLight), "ObjectDispenser");
            p.strip = SavePrefab(BuildIndicatorStrip(), "IndicatorLight_FloorStrip");
            p.fizzler = SavePrefab(BuildFizzler(), "Fizzler");
            p.player = SavePrefab(BuildPlayer(), "TestPlayer");
            return p;
        }

        private static GameObject BuildCube(string name, float size, float mass, Material mat, string displayName)
        {
            GameObject cube = Part(name, PrimitiveType.Cube, null, Vector3.zero, Vector3.one * size, mat);

            Rigidbody rb = cube.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            Carryable carryable = cube.AddComponent<Carryable>();
            carryable.displayName = displayName;

            // Use the project's "Moveable" layer if it exists, so the team's own grab script works on these cubes too.
            int moveable = LayerMask.NameToLayer("Moveable");
            if (moveable >= 0)
                cube.layer = moveable;

            return cube;
        }

        private static GameObject BuildPressurePlate()
        {
            GameObject root = new GameObject("PressurePlate");

            // Trigger box that detects what is resting on the plate.
            BoxCollider sensor = root.AddComponent<BoxCollider>();
            sensor.isTrigger = true;
            sensor.center = new Vector3(0f, 0.65f, 0f);
            sensor.size = new Vector3(1.7f, 1f, 1.7f);

            Part("Base", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.06f, 0f), new Vector3(2f, 0.12f, 2f), darkMetalMat);
            GameObject pad = Part("Pad", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.15f, 0f),
                new Vector3(1.6f, 0.06f, 1.6f), stateMat, keepCollider: false);
            AddStateVisual(pad, InputOff, InputOn);

            PressurePlate plate = root.AddComponent<PressurePlate>();
            plate.pad = pad.transform;
            return root;
        }

        private static GameObject BuildWallButton()
        {
            // Pivot is at the back of the plate, front faces +Z, so it sits flush on a wall.
            GameObject root = new GameObject("WallButton");
            Part("Backplate", PrimitiveType.Cube, root.transform, new Vector3(0f, 0f, 0.03f), new Vector3(0.45f, 0.45f, 0.06f), metalMat);
            GameObject cap = Part("Cap", PrimitiveType.Cube, root.transform, new Vector3(0f, 0f, 0.09f), new Vector3(0.28f, 0.28f, 0.06f), stateMat);
            AddStateVisual(cap, InputOff, InputOn);

            WallButton button = root.AddComponent<WallButton>();
            button.buttonCap = cap.transform;
            return root;
        }

        private static GameObject BuildTarget()
        {
            // Front faces +Z. The collider must be on the root so the script receives collisions.
            GameObject root = new GameObject("ImpactTarget");
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0f, 0.06f);
            box.size = new Vector3(1.2f, 1.2f, 0.12f);

            Vector3 faceForward = new Vector3(90f, 0f, 0f); // cylinders point up, rotate to face +Z
            Part("Ring", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0f, 0.05f), new Vector3(1.2f, 0.05f, 1.2f),
                targetMat, keepCollider: false, euler: faceForward);
            GameObject bullseye = Part("Bullseye", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0f, 0.1f),
                new Vector3(0.6f, 0.03f, 0.6f), stateMat, keepCollider: false, euler: faceForward);
            AddStateVisual(bullseye, InputOff, InputOn);

            root.AddComponent<ImpactTarget>();
            return root;
        }

        private static GameObject BuildLever()
        {
            GameObject root = new GameObject("Lever");
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.55f, 0f);
            box.size = new Vector3(0.6f, 1.1f, 0.9f);

            Part("Base", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.08f, 0f), new Vector3(0.5f, 0.16f, 0.7f), metalMat, keepCollider: false);

            Transform pivot = new GameObject("Handle Pivot").transform;
            pivot.SetParent(root.transform, false);
            pivot.localPosition = new Vector3(0f, 0.16f, 0f);
            Part("Handle", PrimitiveType.Cube, pivot, new Vector3(0f, 0.4f, 0f), new Vector3(0.08f, 0.8f, 0.08f), darkMetalMat, keepCollider: false);
            GameObject knob = Part("Knob", PrimitiveType.Sphere, pivot, new Vector3(0f, 0.85f, 0f), Vector3.one * 0.2f, stateMat, keepCollider: false);
            AddStateVisual(knob, InputOff, InputOn);

            Lever lever = root.AddComponent<Lever>();
            lever.handlePivot = pivot;
            return root;
        }

        private static GameObject BuildTriggerZone()
        {
            GameObject root = new GameObject("TriggerZone");
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0f, 1f, 0f);
            box.size = new Vector3(2f, 2f, 2f);

            GameObject marker = Part("Floor Marker", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.01f, 0f),
                new Vector3(2f, 0.02f, 2f), stateMat, keepCollider: false);
            AddStateVisual(marker, new Color(0.2f, 0.4f, 0.9f), InputOn);

            root.AddComponent<TriggerZone>();
            return root;
        }

        private static GameObject BuildDoor()
        {
            // A 2 m wide, 2.5 m tall opening. The panel slides sideways (+X) into the wall.
            GameObject root = new GameObject("PuzzleDoor");
            Part("Frame Left", PrimitiveType.Cube, root.transform, new Vector3(-1.15f, 1.5f, 0f), new Vector3(0.3f, 3f, 0.5f), metalMat);
            Part("Frame Right", PrimitiveType.Cube, root.transform, new Vector3(1.15f, 1.5f, 0f), new Vector3(0.3f, 3f, 0.5f), metalMat);
            Part("Frame Top", PrimitiveType.Cube, root.transform, new Vector3(0f, 2.75f, 0f), new Vector3(2f, 0.5f, 0.5f), metalMat);

            GameObject panel = Part("Panel", PrimitiveType.Cube, root.transform, new Vector3(0f, 1.25f, 0f), new Vector3(2f, 2.5f, 0.2f), doorMat);
            Rigidbody panelBody = panel.AddComponent<Rigidbody>();
            panelBody.isKinematic = true;
            panelBody.interpolation = RigidbodyInterpolation.Interpolate;

            Transform lights = new GameObject("Status Lights").transform;
            lights.SetParent(root.transform, false);
            Part("Front", PrimitiveType.Cube, lights, new Vector3(0f, 2.75f, -0.26f), new Vector3(1.2f, 0.12f, 0.02f), stateMat, keepCollider: false);
            Part("Back", PrimitiveType.Cube, lights, new Vector3(0f, 2.75f, 0.26f), new Vector3(1.2f, 0.12f, 0.02f), stateMat, keepCollider: false);
            StateVisual statusVisual = AddStateVisual(lights.gameObject, InputOff, InputOn);

            PuzzleDoor door = root.AddComponent<PuzzleDoor>();
            door.doorPanel = panel.transform;
            door.statusLights = statusVisual;
            return root;
        }

        private static GameObject BuildLogicGate(GateType type)
        {
            string label = type.ToString().ToUpperInvariant();
            GameObject root = new GameObject("LogicGate_" + label);
            GameObject body = Part("Body", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.25f, 0f), new Vector3(0.7f, 0.5f, 0.7f), stateMat);
            AddStateVisual(body, InputOff, InputOn);
            CreateLabel(root.transform, label, new Vector3(0f, 0.85f, 0f), 0.35f, 1.5f);

            LogicGate gate = root.AddComponent<LogicGate>();
            gate.gateType = type;
            return root;
        }

        private static GameObject BuildTimer()
        {
            GameObject root = new GameObject("TimerDelay");
            GameObject body = Part("Body", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.25f, 0f), new Vector3(0.7f, 0.5f, 0.7f), stateMat);
            AddStateVisual(body, InputOff, InputOn);
            CreateLabel(root.transform, "DELAY", new Vector3(0f, 0.85f, 0f), 0.35f, 1.8f);

            root.AddComponent<TimerDelay>();
            return root;
        }

        private static GameObject BuildDispenser(GameObject cubePrefab)
        {
            // A ceiling chute with four rails. Objects appear inside it and drop onto the pad below.
            GameObject root = new GameObject("ObjectDispenser");
            Part("Housing", PrimitiveType.Cube, root.transform, new Vector3(0f, 3.2f, 0f), new Vector3(1.6f, 0.5f, 1.6f), metalMat);
            Part("Accent Ring", PrimitiveType.Cube, root.transform, new Vector3(0f, 2.93f, 0f), new Vector3(1.62f, 0.06f, 1.62f), accentMat, keepCollider: false);
            foreach (float x in new[] { -0.65f, 0.65f })
            {
                foreach (float z in new[] { -0.65f, 0.65f })
                    Part("Rail", PrimitiveType.Cube, root.transform, new Vector3(x, 2.3f, z), new Vector3(0.1f, 1.4f, 0.1f), darkMetalMat);
            }
            Part("Drop Pad", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.4f, 0.04f, 1.4f), darkMetalMat);

            Transform spawn = new GameObject("Spawn Point").transform;
            spawn.SetParent(root.transform, false);
            spawn.localPosition = new Vector3(0f, 2.4f, 0f);

            ObjectDispenser dispenser = root.AddComponent<ObjectDispenser>();
            dispenser.spawnPoint = spawn;
            dispenser.objectPrefab = cubePrefab;
            return root;
        }

        private static GameObject BuildIndicatorStrip()
        {
            // 1 m long floor strip along +Z. Stretch it with the root's Z scale.
            GameObject root = new GameObject("IndicatorLight_FloorStrip");
            GameObject strip = Part("Strip", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.015f, 0f),
                new Vector3(0.18f, 0.03f, 1f), stateMat, keepCollider: false);
            StateVisual visual = AddStateVisual(strip, StripOff, StripOn);
            visual.onGlow = 2.5f;
            visual.offGlow = 0.6f;

            IndicatorLight light = root.AddComponent<IndicatorLight>();
            light.visual = visual;
            light.inputMode = InputMode.Any;
            return root;
        }

        private static GameObject BuildFizzler()
        {
            // Fits the same 2 m gap as the door.
            GameObject root = new GameObject("Fizzler");
            BoxCollider field = root.AddComponent<BoxCollider>();
            field.isTrigger = true;
            field.center = new Vector3(0f, 1.4f, 0f);
            field.size = new Vector3(2f, 2.8f, 0.4f);

            Part("Emitter Left", PrimitiveType.Cube, root.transform, new Vector3(-1.1f, 1.5f, 0f), new Vector3(0.2f, 3f, 0.3f), metalMat);
            Part("Emitter Right", PrimitiveType.Cube, root.transform, new Vector3(1.1f, 1.5f, 0f), new Vector3(0.2f, 3f, 0.3f), metalMat);

            Transform visual = new GameObject("Field").transform;
            visual.SetParent(root.transform, false);
            for (int i = 0; i < 9; i++)
            {
                Part("Field Line", PrimitiveType.Cube, visual, new Vector3(0f, 0.2f + i * 0.3f, 0f),
                    new Vector3(2f, 0.025f, 0.025f), fizzlerMat, keepCollider: false);
            }

            Fizzler fizzler = root.AddComponent<Fizzler>();
            fizzler.fieldVisual = visual.gameObject;
            return root;
        }

        private static GameObject BuildPlayer()
        {
            GameObject root = new GameObject("TestPlayer");
            root.tag = "Player";

            CharacterController controller = root.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.35f;

            GameObject cameraObject = new GameObject("PlayerCamera");
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
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
            return root;
        }

        // ------------------------------------------------------------------
        // Test scene
        // ------------------------------------------------------------------

        private const float StationSpacing = 14f;
        private static PrefabSet prefabs;

        private static void BuildTestScene(PrefabSet p)
        {
            prefabs = p;
            EnsureFolder(SceneDir);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateSun();

            Transform level = new GameObject("Level").transform;
            int stationCount = 0;

            BuildPlateStation(Station(level, stationCount++, "Pressure Plate",
                "Put 20 kg on the plate (a light cube, or stand on it).", out PuzzleDoor d1), d1);
            BuildMassStation(Station(level, stationCount++, "Mass Rule - Heavy Plate",
                "Needs 100 kg. The orange cube is too heavy to lift: drag it or push it.\nThe red crate (250 kg) can't be moved at all.", out PuzzleDoor d2), d2);
            BuildButtonStation(Station(level, stationCount++, "Wall Buttons",
                "Three button modes. Any button opens the door (OR).", out PuzzleDoor d3), d3);
            BuildTargetStation(Station(level, stationCount++, "Impact Targets",
                "Throw a cube (Right Mouse) hard at a target.", out PuzzleDoor d4), d4);
            BuildLeverStation(Station(level, stationCount++, "Lever",
                "Press E on the lever to toggle the door.", out PuzzleDoor d5), d5);
            BuildGateStation(Station(level, stationCount++, "Logic Gates",
                "Both levers -> AND -> door.  NOT lights when lever A is off.  OR lights when either is on.", out PuzzleDoor d6), d6);
            BuildTimerStation(Station(level, stationCount++, "Timer / Delay",
                "The door opens 2 s after the button turns on, and closes 3 s after it turns off.", out PuzzleDoor d7), d7);
            BuildDispenserStation(Station(level, stationCount++, "Object Dispenser",
                "The button drops a cube (replacing the old one). Put it on the plate.", out PuzzleDoor d8), d8);
            BuildIndicatorStation(Station(level, stationCount++, "Indicator Lights + Stay-Open Door",
                "Lights trace the signal. This door stays open once opened.", out PuzzleDoor d9), d9);
            BuildZoneStation(Station(level, stationCount++, "Trigger Zone",
                "Stand in the blue square.", out PuzzleDoor d10), d10);
            BuildFizzlerStation(Station(level, stationCount++, "Fizzler",
                "Cubes are destroyed by the field (you are not). The lever turns it off.", out PuzzleDoor d11), d11);

            float finalX = (stationCount - 1) * StationSpacing + 7f + 13f;
            BuildFinalPuzzle(level, finalX);

            // Floor and outer walls around everything.
            float minX = -7f;
            float maxX = finalX + 13f;
            Box(level, "Floor", new Vector3((minX + maxX) * 0.5f, -0.1f, 1.5f), new Vector3(maxX - minX + 1f, 0.2f, 31f), floorMat);
            Box(level, "Outer Wall Front", new Vector3((minX + maxX) * 0.5f, 1.75f, -13f), new Vector3(maxX - minX, 3.5f, 0.4f), wallMat);
            Box(level, "Outer Wall Left", new Vector3(minX, 1.75f, -9f), new Vector3(0.4f, 3.5f, 8f), wallMat);
            Box(level, "Outer Wall Right", new Vector3(maxX, 1.75f, -9f), new Vector3(0.4f, 3.5f, 8f), wallMat);

            CreateLabel(level, "PUZZLE MECHANICS TEST", new Vector3(0f, 2.6f, -12.7f), 0.5f, 10f, rotationY: 180f);

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.player);
            player.transform.position = new Vector3(0f, 0.05f, -9f);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void CreateSun()
        {
            GameObject sun = new GameObject("Sun");
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
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

        /// <summary>
        /// Builds the walls, door and labels shared by every station and returns the station root.
        /// Local layout: player enters from -Z, door is in the back wall at z = 6, a small room behind it.
        /// </summary>
        private static Transform Station(Transform level, int index, string title, string description, out PuzzleDoor door)
        {
            Transform root = new GameObject($"Station {index + 1:00} - {title}").transform;
            root.SetParent(level, false);
            root.localPosition = new Vector3(index * StationSpacing, 0f, 0f);

            BackWallWithDoorway(root, 6f, 7f);
            Box(root, "Divider", new Vector3(7f, 1.75f, 2.5f), new Vector3(0.4f, 3.5f, 15f), wallMat);
            if (index == 0)
                Box(root, "Divider Left", new Vector3(-7f, 1.75f, 2.5f), new Vector3(0.4f, 3.5f, 15f), wallMat);
            Box(root, "Room Back Wall", new Vector3(0f, 1.75f, 10f), new Vector3(14f, 3.5f, 0.4f), wallMat);

            door = Place<PuzzleDoor>(prefabs.door, root, new Vector3(0f, 0f, 6f));

            CreateLabel(root, title, new Vector3(0f, 4.7f, 5.7f), 0.55f, 13f);
            CreateLabel(root, description, new Vector3(0f, 4f, 5.7f), 0.25f, 13f);
            return root;
        }

        /// <summary>A wall along X at depth z, from -halfWidth to +halfWidth, with a 2 m doorway at x = 0.</summary>
        private static void BackWallWithDoorway(Transform parent, float z, float halfWidth)
        {
            float sideWidth = halfWidth - 1f;
            float sideCenter = 1f + sideWidth * 0.5f;
            Box(parent, "Back Wall Left", new Vector3(-sideCenter, 1.75f, z), new Vector3(sideWidth, 3.5f, 0.4f), wallMat);
            Box(parent, "Back Wall Right", new Vector3(sideCenter, 1.75f, z), new Vector3(sideWidth, 3.5f, 0.4f), wallMat);
            Box(parent, "Back Wall Above Door", new Vector3(0f, 3.25f, z), new Vector3(2f, 0.5f, 0.4f), wallMat);
        }

        private static void BuildPlateStation(Transform s, PuzzleDoor door)
        {
            PressurePlate plate = Place<PressurePlate>(prefabs.plate, s, new Vector3(0f, 0f, 1f));
            Place<Carryable>(prefabs.cubeLight, s, new Vector3(-3f, 0.45f, -1f));
            Strip(s, new Vector3(0f, 0f, 2f), new Vector3(0f, 0f, 5.6f), plate);
            Wire(door, InputMode.All, plate);
        }

        private static void BuildMassStation(Transform s, PuzzleDoor door)
        {
            PressurePlate plate = Place<PressurePlate>(prefabs.plate, s, new Vector3(1.5f, 0f, 2.5f));
            plate.requiredMass = 100f;
            Dirty(plate);
            CreateLabel(s, "100 kg plate", new Vector3(1.5f, 1.4f, 2.5f), 0.22f, 3f);

            Place<Carryable>(prefabs.cubeLight, s, new Vector3(-5f, 0.45f, 2f));
            CreateLabel(s, "20 kg - lift", new Vector3(-5f, 1.3f, 2f), 0.2f, 3f);
            Place<Carryable>(prefabs.cubeMedium, s, new Vector3(-2.5f, 0.55f, -1f));
            CreateLabel(s, "100 kg - drag/push only", new Vector3(-2.5f, 1.5f, -1f), 0.2f, 3f);
            Place<Carryable>(prefabs.cubeHeavy, s, new Vector3(4.8f, 0.7f, -1.5f));
            CreateLabel(s, "250 kg - too heavy", new Vector3(4.8f, 1.8f, -1.5f), 0.2f, 3f);

            StripPath(s, new PuzzleActivator[] { plate }, new Vector3(1.5f, 0f, 3.5f), new Vector3(1.5f, 0f, 4.6f),
                new Vector3(0f, 0f, 4.6f), new Vector3(0f, 0f, 5.6f));
            Wire(door, InputMode.All, plate);
        }

        private static void BuildButtonStation(Transform s, PuzzleDoor door)
        {
            WallButton toggle = ButtonOnPost(s, new Vector3(-3f, 0f, -1f), ButtonMode.Toggle, 3f, "Toggle");
            WallButton momentary = ButtonOnPost(s, new Vector3(0f, 0f, -1f), ButtonMode.Momentary, 3f, "Momentary\n(hold E)");
            WallButton timed = ButtonOnPost(s, new Vector3(3f, 0f, -1f), ButtonMode.Timed, 4f, "Timed (4 s)");

            StripPath(s, new PuzzleActivator[] { toggle }, new Vector3(-3f, 0f, -0.8f), new Vector3(-3f, 0f, 4.3f), new Vector3(-0.1f, 0f, 4.3f));
            StripPath(s, new PuzzleActivator[] { momentary }, new Vector3(0f, 0f, -0.8f), new Vector3(0f, 0f, 4.2f));
            StripPath(s, new PuzzleActivator[] { timed }, new Vector3(3f, 0f, -0.8f), new Vector3(3f, 0f, 4.3f), new Vector3(0.1f, 0f, 4.3f));
            Strip(s, new Vector3(0f, 0f, 4.3f), new Vector3(0f, 0f, 5.6f), toggle, momentary, timed);

            Wire(door, InputMode.Any, toggle, momentary, timed);
        }

        private static void BuildTargetStation(Transform s, PuzzleDoor door)
        {
            ImpactTarget latching = Place<ImpactTarget>(prefabs.target, s, new Vector3(-4f, 2.2f, 5.78f), 180f);
            CreateLabel(s, "Stays on", new Vector3(-4f, 1.25f, 5.7f), 0.22f, 3f);

            ImpactTarget resetting = Place<ImpactTarget>(prefabs.target, s, new Vector3(4f, 2.2f, 5.78f), 180f);
            resetting.stayActivated = false;
            resetting.resetAfter = 4f;
            Dirty(resetting);
            CreateLabel(s, "Resets after 4 s", new Vector3(4f, 1.25f, 5.7f), 0.22f, 3f);

            Place<Carryable>(prefabs.cubeLight, s, new Vector3(-1f, 0.45f, -2f));
            Place<Carryable>(prefabs.cubeLight, s, new Vector3(1f, 0.45f, -2f));

            Strip(s, new Vector3(-4f, 0f, 5.5f), new Vector3(-1.3f, 0f, 5.5f), latching);
            Strip(s, new Vector3(4f, 0f, 5.5f), new Vector3(1.3f, 0f, 5.5f), resetting);
            Wire(door, InputMode.Any, latching, resetting);
        }

        private static void BuildLeverStation(Transform s, PuzzleDoor door)
        {
            Lever lever = Place<Lever>(prefabs.lever, s, new Vector3(0f, 0f, 1f));
            Strip(s, new Vector3(0f, 0f, 1.5f), new Vector3(0f, 0f, 5.6f), lever);
            Wire(door, InputMode.All, lever);
        }

        private static void BuildGateStation(Transform s, PuzzleDoor door)
        {
            Lever leverA = Place<Lever>(prefabs.lever, s, new Vector3(-3f, 0f, -1f));
            CreateLabel(s, "Lever A", new Vector3(-3f, 1.6f, -1f), 0.22f, 2f);
            Lever leverB = Place<Lever>(prefabs.lever, s, new Vector3(3f, 0f, -1f));
            CreateLabel(s, "Lever B", new Vector3(3f, 1.6f, -1f), 0.22f, 2f);

            LogicGate andGate = PlaceGate(prefabs.gateAnd, s, new Vector3(0f, 0f, 3f), leverA, leverB);
            LogicGate notGate = PlaceGate(prefabs.gateNot, s, new Vector3(-5f, 0f, 3f), leverA);
            LogicGate orGate = PlaceGate(prefabs.gateOr, s, new Vector3(5f, 0f, 3f), leverA, leverB);

            Strip(s, new Vector3(-3f, 0f, -0.5f), new Vector3(-3f, 0f, 3f), leverA);
            Strip(s, new Vector3(-3f, 0f, 3f), new Vector3(-0.4f, 0f, 3f), leverA);
            Strip(s, new Vector3(-3f, 0f, 3f), new Vector3(-4.6f, 0f, 3f), leverA);
            Strip(s, new Vector3(3f, 0f, -0.5f), new Vector3(3f, 0f, 3f), leverB);
            Strip(s, new Vector3(3f, 0f, 3f), new Vector3(0.4f, 0f, 3f), leverB);
            Strip(s, new Vector3(3f, 0f, 3f), new Vector3(4.6f, 0f, 3f), leverB);

            Strip(s, new Vector3(0f, 0f, 3.4f), new Vector3(0f, 0f, 5.6f), andGate);
            Strip(s, new Vector3(-5f, 0f, 3.4f), new Vector3(-5f, 0f, 5.5f), notGate);
            Strip(s, new Vector3(5f, 0f, 3.4f), new Vector3(5f, 0f, 5.5f), orGate);

            Wire(door, InputMode.All, andGate);
        }

        private static void BuildTimerStation(Transform s, PuzzleDoor door)
        {
            WallButton button = ButtonOnPost(s, new Vector3(0f, 0f, -1f), ButtonMode.Toggle, 3f, "Toggle button");
            TimerDelay timer = Place<TimerDelay>(prefabs.timer, s, new Vector3(0f, 0f, 2.5f));
            timer.input = button;
            timer.activateDelay = 2f;
            timer.deactivateDelay = 3f;
            Dirty(timer);
            CreateLabel(s, "on after 2 s / off after 3 s", new Vector3(1.9f, 0.6f, 2.5f), 0.18f, 2.5f);

            Strip(s, new Vector3(0f, 0f, -0.8f), new Vector3(0f, 0f, 2.1f), button);
            Strip(s, new Vector3(0f, 0f, 2.9f), new Vector3(0f, 0f, 5.6f), timer);
            Wire(door, InputMode.All, timer);
        }

        private static void BuildDispenserStation(Transform s, PuzzleDoor door)
        {
            WallButton button = ButtonOnPost(s, new Vector3(-5.5f, 0f, -1f), ButtonMode.Timed, 1f, "Dispense");
            ObjectDispenser dispenser = Place<ObjectDispenser>(prefabs.dispenser, s, new Vector3(-3f, 0f, 1.5f));
            Wire(dispenser, InputMode.Any, button);

            PressurePlate plate = Place<PressurePlate>(prefabs.plate, s, new Vector3(3f, 0f, 1.5f));

            StripPath(s, new PuzzleActivator[] { button }, new Vector3(-5.5f, 0f, -0.8f), new Vector3(-5.5f, 0f, 1.5f), new Vector3(-3.8f, 0f, 1.5f));
            StripPath(s, new PuzzleActivator[] { plate }, new Vector3(3f, 0f, 2.5f), new Vector3(3f, 0f, 4.6f),
                new Vector3(0f, 0f, 4.6f), new Vector3(0f, 0f, 5.6f));
            Wire(door, InputMode.All, plate);
        }

        private static void BuildIndicatorStation(Transform s, PuzzleDoor door)
        {
            WallButton button = ButtonOnPost(s, new Vector3(-4.5f, 0f, -1f), ButtonMode.Timed, 3f, "Timed (3 s)");
            StripPath(s, new PuzzleActivator[] { button }, new Vector3(-4.5f, 0f, -0.8f), new Vector3(-4.5f, 0f, 3f),
                new Vector3(3f, 0f, 3f), new Vector3(3f, 0f, 4.5f), new Vector3(0f, 0f, 4.5f), new Vector3(0f, 0f, 5.6f));

            door.stayOpenOnceOpened = true;
            Wire(door, InputMode.All, button);
        }

        private static void BuildZoneStation(Transform s, PuzzleDoor door)
        {
            TriggerZone zone = Place<TriggerZone>(prefabs.zone, s, new Vector3(0f, 0f, 1.5f));
            Strip(s, new Vector3(0f, 0f, 2.5f), new Vector3(0f, 0f, 5.6f), zone);
            Wire(door, InputMode.All, zone);
        }

        private static void BuildFizzlerStation(Transform s, PuzzleDoor door)
        {
            // An inner wall with the fizzler in its doorway, between the cube and the plate.
            BackWallWithDoorway(s, 1.5f, 7f);
            Fizzler fizzler = Place<Fizzler>(prefabs.fizzler, s, new Vector3(0f, 0f, 1.5f));

            ObjectDispenser dispenser = Place<ObjectDispenser>(prefabs.dispenser, s, new Vector3(-4f, 0f, -2f));
            dispenser.dispenseOnStart = true;
            Dirty(dispenser);
            CreateLabel(s, "Auto-refilling dispenser", new Vector3(-4f, 3.8f, -2f), 0.2f, 3f);

            Lever lever = Place<Lever>(prefabs.lever, s, new Vector3(3f, 0f, -2f));
            CreateLabel(s, "Fizzler off", new Vector3(3f, 1.6f, -2f), 0.22f, 2f);
            Wire(fizzler, InputMode.All, lever);
            StripPath(s, new PuzzleActivator[] { lever }, new Vector3(3f, 0f, -1.5f), new Vector3(3f, 0f, 1f), new Vector3(1.2f, 0f, 1f));

            PressurePlate plate = Place<PressurePlate>(prefabs.plate, s, new Vector3(0f, 0f, 3.8f));
            Strip(s, new Vector3(0f, 0f, 4.8f), new Vector3(0f, 0f, 5.6f), plate);
            Wire(door, InputMode.All, plate);
        }

        /// <summary>
        /// The combined puzzle: dispenser + target + fizzler + lever + two plates -> AND gate -> delay -> final door.
        /// </summary>
        private static void BuildFinalPuzzle(Transform level, float centerX)
        {
            Transform s = new GameObject("Station 12 - Final Puzzle").transform;
            s.SetParent(level, false);
            s.localPosition = new Vector3(centerX, 0f, 0f);

            // Walls: 26 m wide room, back wall (with final door) at z = 12, inner wall with a fizzler at z = 4.
            BackWallWithDoorway(s, 12f, 13f);
            Box(s, "Divider Left (back part)", new Vector3(-13f, 1.75f, 13f), new Vector3(0.4f, 3.5f, 6f), wallMat);
            Box(s, "Divider Right", new Vector3(13f, 1.75f, 5.5f), new Vector3(0.4f, 3.5f, 21f), wallMat);
            Box(s, "Room Back Wall", new Vector3(0f, 1.75f, 16f), new Vector3(26f, 3.5f, 0.4f), wallMat);
            Box(s, "Inner Wall Left", new Vector3(-10f, 1.75f, 4f), new Vector3(6f, 3.5f, 0.4f), wallMat);
            Box(s, "Inner Wall Right", new Vector3(4f, 1.75f, 4f), new Vector3(18f, 3.5f, 0.4f), wallMat);
            Box(s, "Inner Wall Above Fizzler", new Vector3(-6f, 3.15f, 4f), new Vector3(2f, 0.7f, 0.4f), wallMat);

            CreateLabel(s, "FINAL PUZZLE", new Vector3(0f, 4.4f, 3.7f), 0.6f, 20f);
            CreateLabel(s,
                "1. Press the button to dispense a cube.   2. Throw it hard at the target.\n" +
                "3. Pull the lever to turn off the fizzler, then carry a cube to the 20 kg plate.\n" +
                "4. Drag the 100 kg cube onto the heavy plate.   All three -> AND -> 1 s delay -> door.",
                new Vector3(0f, 3.7f, 3.7f), 0.22f, 22f);

            // Front half
            WallButton button = ButtonOnPost(s, new Vector3(-11f, 0f, -2.5f), ButtonMode.Timed, 1f, "Dispense");
            ObjectDispenser dispenser = Place<ObjectDispenser>(prefabs.dispenser, s, new Vector3(-9f, 0f, 0f));
            Wire(dispenser, InputMode.Any, button);

            Lever lever = Place<Lever>(prefabs.lever, s, new Vector3(-3.5f, 0f, 0f));
            CreateLabel(s, "Fizzler off", new Vector3(-3.5f, 1.6f, 0f), 0.22f, 2f);
            Fizzler fizzler = Place<Fizzler>(prefabs.fizzler, s, new Vector3(-6f, 0f, 4f));
            Wire(fizzler, InputMode.All, lever);

            ImpactTarget target = Place<ImpactTarget>(prefabs.target, s, new Vector3(6f, 2.2f, 3.78f), 180f);

            // Back half
            PressurePlate lightPlate = Place<PressurePlate>(prefabs.plate, s, new Vector3(-6f, 0f, 8f));
            CreateLabel(s, "20 kg", new Vector3(-6f, 1.3f, 8f), 0.25f, 2f);
            PressurePlate heavyPlate = Place<PressurePlate>(prefabs.plate, s, new Vector3(6f, 0f, 8f));
            heavyPlate.requiredMass = 100f;
            Dirty(heavyPlate);
            CreateLabel(s, "100 kg", new Vector3(6f, 1.3f, 8f), 0.25f, 2f);
            Place<Carryable>(prefabs.cubeMedium, s, new Vector3(10.5f, 0.55f, 6.5f));

            LogicGate andGate = PlaceGate(prefabs.gateAnd, s, new Vector3(0f, 0f, 9f), lightPlate, heavyPlate, target);
            TimerDelay delay = Place<TimerDelay>(prefabs.timer, s, new Vector3(0f, 0f, 10.6f));
            delay.input = andGate;
            delay.activateDelay = 1f;
            Dirty(delay);

            PuzzleDoor door = Place<PuzzleDoor>(prefabs.door, s, new Vector3(0f, 0f, 12f));
            door.stayOpenOnceOpened = true;
            Wire(door, InputMode.All, delay);
            CreateLabel(s, "PUZZLE COMPLETE!", new Vector3(0f, 2.2f, 15.7f), 0.5f, 8f);

            // Signal lines
            StripPath(s, new PuzzleActivator[] { button }, new Vector3(-11f, 0f, -2.3f), new Vector3(-11f, 0f, 0f), new Vector3(-9.8f, 0f, 0f));
            StripPath(s, new PuzzleActivator[] { lever }, new Vector3(-3.5f, 0f, 0.5f), new Vector3(-3.5f, 0f, 3.5f), new Vector3(-4.9f, 0f, 3.5f));
            StripPath(s, new PuzzleActivator[] { lightPlate }, new Vector3(-5f, 0f, 8f), new Vector3(-0.4f, 0f, 8f), new Vector3(-0.4f, 0f, 8.6f));
            StripPath(s, new PuzzleActivator[] { heavyPlate }, new Vector3(5f, 0f, 8f), new Vector3(0.4f, 0f, 8f), new Vector3(0.4f, 0f, 8.6f));
            StripPath(s, new PuzzleActivator[] { target }, new Vector3(6f, 0f, 3.6f), new Vector3(2f, 0f, 3.6f),
                new Vector3(2f, 0f, 6.5f), new Vector3(0f, 0f, 6.5f), new Vector3(0f, 0f, 8.6f));
            Strip(s, new Vector3(0f, 0f, 9.4f), new Vector3(0f, 0f, 10.2f), andGate);
            Strip(s, new Vector3(0f, 0f, 11f), new Vector3(0f, 0f, 11.6f), delay);
        }

        // ------------------------------------------------------------------
        // Scene helpers
        // ------------------------------------------------------------------

        private static T Place<T>(GameObject prefab, Transform parent, Vector3 localPosition, float yRotation = 0f) where T : Component
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            return go.GetComponent<T>();
        }

        private static LogicGate PlaceGate(GameObject prefab, Transform parent, Vector3 localPosition, params PuzzleActivator[] inputs)
        {
            LogicGate gate = Place<LogicGate>(prefab, parent, localPosition);
            gate.inputs = new List<PuzzleActivator>(inputs);
            Dirty(gate);
            return gate;
        }

        private static WallButton ButtonOnPost(Transform parent, Vector3 floorPosition, ButtonMode mode, float timedDuration, string label)
        {
            Box(parent, "Button Post", floorPosition + new Vector3(0f, 0.6f, 0f), new Vector3(0.3f, 1.2f, 0.3f), metalMat);

            // Rotated 180 so the button's front faces the player coming from -Z.
            WallButton button = Place<WallButton>(prefabs.button, parent, floorPosition + new Vector3(0f, 1.05f, -0.15f), 180f);
            button.mode = mode;
            button.timedDuration = timedDuration;
            Dirty(button);

            CreateLabel(parent, label, floorPosition + new Vector3(0f, 1.75f, -0.2f), 0.2f, 2.4f);
            return button;
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

            // Slightly longer than the gap so corners overlap neatly.
            IndicatorLight strip = Place<IndicatorLight>(prefabs.strip, parent, (from + to) * 0.5f);
            strip.transform.localRotation = Quaternion.LookRotation(delta / length);
            strip.transform.localScale = new Vector3(1f, 1f, length + 0.18f);
            Wire(strip, InputMode.Any, inputs);
        }

        private static void StripPath(Transform parent, PuzzleActivator[] inputs, params Vector3[] points)
        {
            for (int i = 0; i < points.Length - 1; i++)
                Strip(parent, points[i], points[i + 1], inputs);
        }

        private static void Box(Transform parent, string name, Vector3 localPosition, Vector3 size, Material mat)
        {
            Part(name, PrimitiveType.Cube, parent, localPosition, size, mat);
        }

        /// <summary>Saves changes made to a prefab instance in the scene.</summary>
        private static void Dirty(Component component)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }

        // ------------------------------------------------------------------
        // General helpers
        // ------------------------------------------------------------------

        private static GameObject Part(string name, PrimitiveType type, Transform parent, Vector3 localPosition,
            Vector3 localScale, Material mat, bool keepCollider = true, Vector3? euler = null)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            go.transform.localScale = localScale;

            if (!keepCollider)
                Object.DestroyImmediate(go.GetComponent<Collider>());

            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private static StateVisual AddStateVisual(GameObject go, Color off, Color on)
        {
            StateVisual visual = go.AddComponent<StateVisual>();
            visual.renderers = go.GetComponentsInChildren<Renderer>();
            visual.offColor = off;
            visual.onColor = on;
            return visual;
        }

        /// <summary>
        /// A floating world-space text label. It reads correctly when viewed while looking along +Z
        /// (use rotationY = 180 for labels that face the other way).
        /// </summary>
        private static void CreateLabel(Transform parent, string text, Vector3 localPosition, float letterHeight, float width,
            float rotationY = 0f)
        {
            const float pixelsPerMetre = 200f;
            int lines = text.Split('\n').Length;

            GameObject canvasObject = new GameObject("Label: " + text.Split('\n')[0], typeof(RectTransform));
            canvasObject.transform.SetParent(parent, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.localPosition = localPosition;
            canvasRect.localRotation = Quaternion.Euler(0f, rotationY, 0f);
            canvasRect.localScale = Vector3.one / pixelsPerMetre;
            canvasRect.sizeDelta = new Vector2(width, letterHeight * 1.3f * lines) * pixelsPerMetre;

            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(canvasObject.transform, false);
            RectTransform textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text label = textObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = Mathf.RoundToInt(letterHeight * pixelsPerMetre);
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.color = new Color(0.1f, 0.1f, 0.12f);
            label.raycastTarget = false;
        }

        private static GameObject SavePrefab(GameObject temp, string name)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, $"{PrefabDir}/{name}.prefab");
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
