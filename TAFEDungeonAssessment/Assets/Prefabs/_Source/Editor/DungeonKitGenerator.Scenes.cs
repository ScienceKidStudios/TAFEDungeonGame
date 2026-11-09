using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace PuzzleMechanics.EditorTools
{
    public static partial class DungeonKitGenerator
    {
        // Helpers shared by the showcase and the test room: placing prefabs, wiring them up,
        // building rooms from the level pieces, and the torch-lit lighting setup.

        private static PrefabSet kit;

        // ---------------- Placing and wiring ----------------

        private static GameObject Place(GameObject prefab, Transform parent, Vector3 localPosition, float yRotation = 0f)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            return go;
        }

        private static T Place<T>(GameObject prefab, Transform parent, Vector3 localPosition, float yRotation = 0f) where T : Component
        {
            return Place(prefab, parent, localPosition, yRotation).GetComponent<T>();
        }

        private static void Wire(PuzzleReceiver receiver, InputMode mode, params PuzzleActivator[] inputs)
        {
            receiver.inputs = new List<PuzzleActivator>(inputs);
            receiver.inputMode = mode;
            Dirty(receiver);
        }

        private static LogicGate PlaceGate(GameObject prefab, Transform parent, Vector3 localPosition, float yRotation,
            params PuzzleActivator[] inputs)
        {
            LogicGate gate = Place<LogicGate>(prefab, parent, localPosition, yRotation);
            gate.inputs = new List<PuzzleActivator>(inputs);
            Dirty(gate);
            return gate;
        }

        /// <summary>Saves changes made to a prefab instance in the scene.</summary>
        private static void Dirty(Component component)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }

        /// <summary>A line of glowing floor runes from 'from' to 'to' (floor height), lit by the given inputs.</summary>
        private static void Runes(Transform parent, Vector3 from, Vector3 to, params PuzzleActivator[] inputs)
        {
            Vector3 delta = to - from;
            delta.y = 0f;
            float length = delta.magnitude;
            if (length < 0.01f)
                return;

            IndicatorLight line = Place<IndicatorLight>(kit.floorRunes, parent, (from + to) * 0.5f);
            line.transform.localRotation = Quaternion.LookRotation(delta / length);
            // Slightly longer than the gap so corners overlap neatly.
            line.transform.localScale = new Vector3(1f, 1f, length + 0.1f);
            Wire(line, InputMode.Any, inputs);
        }

        private static void RunePath(Transform parent, PuzzleActivator[] inputs, params Vector3[] points)
        {
            for (int i = 0; i < points.Length - 1; i++)
                Runes(parent, points[i], points[i + 1], inputs);
        }

        /// <summary>Text fixed to a wall (or any surface). Its readable side faces the holder's +Z.</summary>
        private static void WallLabel(Transform parent, string text, Vector3 localPosition, float yRotation, float letterHeight = 0.13f,
            float width = 1.4f)
        {
            GameObject holder = Empty("Label: " + text, parent, localPosition, new Vector3(0f, yRotation, 0f));
            CreateText(holder.transform, "Text", text, new Vector3(0f, 0f, 0.02f), letterHeight,
                new Vector2(width, letterHeight * 2.6f), true, CarvedGold);
        }

        /// <summary>
        /// A low stone sign on a short post, tilted back like a museum label so you can see over it.
        /// It faces -Z (towards a viewer looking along +Z).
        /// </summary>
        private static void FloorSign(Transform parent, string title, string body, Vector3 localPosition, float width = 1.4f)
        {
            GameObject sign = Empty("Sign: " + title, parent, localPosition);
            Box("Post", sign.transform, new Vector3(0f, 0.25f, 0.08f), new Vector3(0.1f, 0.5f, 0.1f), woodDark);
            GameObject p = Plaque(sign.transform, new Vector3(0f, 0.62f, 0f), width, 0.56f, title, body);
            p.transform.localRotation = Quaternion.Euler(-50f, 180f, 0f);
        }

        // ---------------- Building rooms from the level pieces ----------------

        private static Transform levelRoot, lightingRoot;
        private static Dictionary<string, GameObject> builtPieces;

        private static void BeginLevel()
        {
            levelRoot = new GameObject("Level (walls, floors, ceilings)").transform;
            lightingRoot = new GameObject("Lighting").transform;
            builtPieces = new Dictionary<string, GameObject>();
        }

        private static string PieceKey(string kind, Vector3 worldPosition, float worldYaw)
        {
            int yaw = Mathf.RoundToInt(Mathf.Repeat(worldYaw, 180f)) % 180;
            return $"{kind} {Mathf.RoundToInt(worldPosition.x * 10f)} {Mathf.RoundToInt(worldPosition.y * 10f)} {Mathf.RoundToInt(worldPosition.z * 10f)} {yaw}";
        }

        /// <summary>
        /// A wall piece at a position in 'space' (a room's local space). Each spot only ever gets one wall,
        /// so rooms that share a wall don't build it twice. A doorway replaces a plain wall in the same spot.
        /// </summary>
        private static void WallPiece(Transform space, Vector3 localPosition, bool alongX, bool doorway = false)
        {
            Vector3 world = space.TransformPoint(localPosition);
            float yaw = space.eulerAngles.y + (alongX ? 0f : 90f);
            string key = PieceKey("wall", world, yaw);
            if (builtPieces.TryGetValue(key, out GameObject existing))
            {
                if (!doorway || existing.name.StartsWith("Wall_Stone_Doorway"))
                    return;
                Object.DestroyImmediate(existing);
            }

            GameObject wall = Place(doorway ? kit.wallDoorway : kit.wall, levelRoot, world, yaw);
            builtPieces[key] = wall;
        }

        /// <summary>A floor tile and a ceiling tile for one 4 x 4 m grid square.</summary>
        private static void FloorSquare(Transform space, Vector3 localCentre, bool ceiling = true)
        {
            Vector3 world = space.TransformPoint(localCentre);
            world.y = 0f;
            string key = PieceKey("floor", world, 0f);
            if (builtPieces.ContainsKey(key))
                return;

            // Tiles are turned to match the room, so the ceiling beams run across it the same way everywhere.
            builtPieces[key] = Place(kit.floor, levelRoot, world, space.eulerAngles.y);
            if (ceiling)
                Place(kit.ceiling, levelRoot, world + Vector3.up * WallHeight, space.eulerAngles.y);
        }

        private static void PillarAt(Vector3 world)
        {
            string key = PieceKey("pillar", world, 0f);
            if (!builtPieces.ContainsKey(key))
                builtPieces[key] = Place(kit.pillar, levelRoot, world);
        }

        /// <summary>A wall torch (with its light) at a position in 'space', facing along the given yaw.</summary>
        private static void TorchAt(Transform space, Vector3 localPosition, float yaw)
        {
            Place(kit.torch, lightingRoot, space.TransformPoint(localPosition), space.eulerAngles.y + yaw);
        }

        /// <summary>
        /// Floors, ceilings and walls for a rectangular room in 'space', on the 4 m grid.
        /// x0/x1 and z0/z1 are the room edges (multiples of 4 apart). Doorways are listed as wall centre positions.
        /// Walls listed in 'openings' are left out completely.
        /// </summary>
        private static void RoomShell(Transform space, float x0, float x1, float z0, float z1, Vector3[] doorways = null,
            Vector3[] openings = null)
        {
            doorways = doorways ?? new Vector3[0];
            openings = openings ?? new Vector3[0];
            for (float x = x0 + 2f; x < x1; x += 4f)
                for (float z = z0 + 2f; z < z1; z += 4f)
                    FloorSquare(space, new Vector3(x, 0f, z));

            void Edge(Vector3 centre, bool alongX)
            {
                if (openings.Any(o => (o - centre).sqrMagnitude < 0.01f))
                    return;
                WallPiece(space, centre, alongX, doorways.Any(d => (d - centre).sqrMagnitude < 0.01f));
            }

            for (float x = x0 + 2f; x < x1; x += 4f)
            {
                Edge(new Vector3(x, 0f, z0), true);
                Edge(new Vector3(x, 0f, z1), true);
            }
            for (float z = z0 + 2f; z < z1; z += 4f)
            {
                Edge(new Vector3(x0, 0f, z), false);
                Edge(new Vector3(x1, 0f, z), false);
            }
        }

        /// <summary>Marks the plain level geometry as static so Unity can draw it in big batches (faster on school PCs).</summary>
        private static void MarkLevelStatic()
        {
            foreach (Transform t in levelRoot.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic |
                                                                      StaticEditorFlags.OccludeeStatic);
        }

        // ---------------- Lighting ----------------

        /// <summary>
        /// Warm, torch-lit dungeon lighting that is still easy to read: a fixed exposure tuned for torches,
        /// a dim warm "ambient" fill from a gradient sky so dark corners are never pitch black,
        /// light fog, bloom on the flames and a soft vignette. No baking needed.
        /// </summary>
        private static void SetupDungeonLighting(Scene scene, string profilePath, string lightingSettingsPath)
        {
            EnsureFolder(System.IO.Path.GetDirectoryName(profilePath).Replace('\\', '/'));
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }
            foreach (VolumeComponent old in profile.components.ToList())
            {
                profile.Remove(old.GetType());
                Object.DestroyImmediate(old, true);
            }

            Exposure exposure = AddOverride<Exposure>(profile);
            exposure.mode.Override(ExposureMode.Fixed);
            exposure.fixedExposure.Override(DungeonExposure);

            Tonemapping tonemapping = AddOverride<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.ACES);

            Bloom bloom = AddOverride<Bloom>(profile);
            bloom.intensity.Override(0.3f);
            bloom.scatter.Override(0.6f);

            Vignette vignette = AddOverride<Vignette>(profile);
            vignette.intensity.Override(0.3f);
            vignette.smoothness.Override(0.5f);

            ScreenSpaceAmbientOcclusion ao = AddOverride<ScreenSpaceAmbientOcclusion>(profile);
            ao.intensity.Override(0.8f);
            ao.radius.Override(1f);

            MotionBlur motionBlur = AddOverride<MotionBlur>(profile);
            motionBlur.intensity.Override(0f);

            VisualEnvironment environment = AddOverride<VisualEnvironment>(profile);
            environment.skyType.Override((int)SkyType.Gradient);
            environment.skyAmbientMode.Override(SkyAmbientMode.Dynamic);

            // The "sky" is never seen indoors. It only provides a soft warm fill light everywhere.
            GradientSky sky = AddOverride<GradientSky>(profile);
            sky.top.Override(new Color(0.42f, 0.42f, 0.5f));
            sky.middle.Override(new Color(0.5f, 0.44f, 0.38f));
            sky.bottom.Override(new Color(0.4f, 0.32f, 0.24f));
            sky.gradientDiffusion.Override(1f);
            sky.exposure.Override(DungeonAmbient);

            Fog fog = AddOverride<Fog>(profile);
            fog.enabled.Override(true);
            fog.meanFreePath.Override(45f);
            fog.baseHeight.Override(0f);
            fog.maximumHeight.Override(8f);
            fog.enableVolumetricFog.Override(false);   // volumetric fog looks great but is slow on school PCs

            HDShadowSettings shadows = AddOverride<HDShadowSettings>(profile);
            shadows.maxShadowDistance.Override(40f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            GameObject volumeObject = new GameObject("Dungeon Lighting Volume");
            volumeObject.transform.SetParent(lightingRoot, false);
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = profile;
            StaticLightingSky staticSky = volumeObject.AddComponent<StaticLightingSky>();
            staticSky.profile = profile;
            staticSky.staticLightingSkyUniqueID = (int)SkyType.Gradient;

            // Realtime lights only, so nothing needs baking.
            LightingSettings settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(lightingSettingsPath);
            if (settings == null)
            {
                settings = new LightingSettings { name = System.IO.Path.GetFileNameWithoutExtension(lightingSettingsPath) };
                AssetDatabase.CreateAsset(settings, lightingSettingsPath);
            }
            settings.bakedGI = false;
            settings.realtimeGI = false;
            EditorUtility.SetDirty(settings);
            SceneManager.SetActiveScene(scene);
            Lightmapping.lightingSettings = settings;
        }

        // Tuned by eye: torchlit walls read clearly and the gaps between torches stay dim but visible.
        private const float DungeonExposure = 1.5f;
        private const float DungeonAmbient = 0.5f;

        private static T AddOverride<T>(VolumeProfile profile) where T : VolumeComponent
        {
            T component = profile.Add<T>();
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }
    }
}
