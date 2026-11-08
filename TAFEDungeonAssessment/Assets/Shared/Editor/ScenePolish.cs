using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace ProjectTools
{
    /// <summary>
    /// Project-wide polish pass for the HDRP project:
    ///  - Lighting: a per-scene post-processing Volume, tuned sun, light probes, a reflection probe,
    ///    accent lights on doors and a Lighting Settings asset stored next to each scene.
    ///  - Materials: switches materials that would render pink in HDRP to HDRP/Lit.
    ///  - Physics: fixes dynamic Rigidbodies that use concave Mesh Colliders (they fall through floors).
    ///  - Hierarchy: groups loose root objects into Lighting / Level Geometry / Physics Objects.
    ///
    /// Safe to run more than once. Menu: Tools > Project > Polish All Scenes
    /// </summary>
    public static class ScenePolish
    {
        public enum Style
        {
            Chamber,   // indoor Portal-style test chambers: clean, bright, neutral light
            Outdoor,   // open scenes: keep the sky, light post-processing
            Moody      // torch-lit scenes: darker, warm, fog and stronger bloom
        }

        private const string LightingGroup = "Lighting";
        private const string GeometryGroup = "Level Geometry";
        private const string PhysicsGroup = "Physics Objects";
        private const string AddedPrefix = "Polish ";   // objects this tool creates, so re-runs can replace them

        private static readonly StringBuilder Report = new StringBuilder();

        /// <summary>Which style each scene gets. Scenes not listed use Outdoor.</summary>
        public static Style StyleFor(string scenePath)
        {
            string p = scenePath.ToLowerInvariant();
            if (p.Contains("lavenda")) return Style.Moody;
            if (p.Contains("jamie") || p.Contains("joel") || p.Contains("thomas") || p.Contains("mechanicstestscene") ||
                p.Contains("_recovery"))
                return Style.Chamber;
            return Style.Outdoor;
        }

        [MenuItem("Tools/Project/Polish All Scenes")]
        public static void PolishAll()
        {
            Report.Clear();
            FixMaterials();

            foreach (string scenePath in ProjectSurvey.AllScenePaths())
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                Report.AppendLine($"=== {scenePath} ({StyleFor(scenePath)})");
                PolishScene(scene);
                EditorSceneManager.SaveScene(scene);
            }

            AssetDatabase.SaveAssets();
            string reportPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/PolishReport.txt"));
            File.WriteAllText(reportPath, Report.ToString());
            Debug.Log("[Polish] Done. Report: " + reportPath + "\n" + Report);
        }

        /// <summary>Polishes the open scene (used by scene generators so regenerated scenes keep their lighting).</summary>
        public static void PolishActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            PolishScene(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void PolishScene(Scene scene)
        {
            Style style = StyleFor(scene.path);
            FixPhysics(scene);
            TidyHierarchy(scene);
            Transform lighting = GetOrCreateRoot(scene, LightingGroup);
            RemovePreviouslyAdded(lighting);

            TuneLights(scene, style);
            CreateVolume(scene, style, lighting);
            if (style != Style.Outdoor)
            {
                Bounds bounds = StaticGeometryBounds(scene);
                MarkStaticForBaking(scene);
                CreateLightProbes(bounds, lighting);
                CreateReflectionProbe(bounds, lighting);
            }
            CreateDoorAccentLights(scene, style, lighting);
            CreateLightingSettings(scene, style);
        }

        // ------------------------------------------------------------------
        // Lighting
        // ------------------------------------------------------------------

        private static void TuneLights(Scene scene, Style style)
        {
            foreach (Light light in AllComponents<Light>(scene))
            {
                if (light.type == LightType.Directional)
                {
                    // A 1 lux sun is pitch black in HDRP (it uses real-world units).
                    if (light.intensity < 1000f)
                    {
                        Report.AppendLine($"  sun '{light.name}': intensity {light.intensity} -> 100000 lux");
                        light.intensity = 100000f;
                    }
                    if (style == Style.Chamber && light.intensity > 90000f)
                        light.intensity = 90000f;   // with the brighter ambient this stops white walls blowing out

                    light.shadows = LightShadows.Soft;
                    light.lightmapBakeType = LightmapBakeType.Mixed;   // realtime direct light + baked bounce light
                }
                else
                {
                    // Area lights set to Baked do nothing until lighting is baked, so make them Mixed.
                    if (light.lightmapBakeType == LightmapBakeType.Baked &&
                        (light.type == LightType.Rectangle || light.type == LightType.Disc))
                    {
                        light.lightmapBakeType = LightmapBakeType.Mixed;
                        Report.AppendLine($"  area light '{ProjectSurvey.HierarchyPath(light.transform)}': Baked -> Mixed (visible before baking)");
                    }

                    // A very bright point light that only reaches half a metre is almost certainly a typo.
                    if (light.type == LightType.Point && light.range < 1f && light.intensity > 1000f)
                    {
                        Report.AppendLine($"  point light '{ProjectSurvey.HierarchyPath(light.transform)}': range {light.range} -> 6");
                        light.range = 6f;
                    }

                    // Keep the number of realtime shadow-casting lights low for school PCs.
                    light.shadows = LightShadows.None;
                }
                EditorUtility.SetDirty(light);
            }
        }

        private static void CreateVolume(Scene scene, Style style, Transform parent)
        {
            string profilePath = $"{SceneFolder(scene)}/{scene.name} Volume Profile.asset";
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

            // Exposure: automatic, so dark and bright areas both stay readable.
            Exposure exposure = Add<Exposure>(profile);
            exposure.mode.Override(ExposureMode.AutomaticHistogram);
            exposure.compensation.Override(style == Style.Moody ? 1.2f : style == Style.Chamber ? 0f : 0f);
            exposure.adaptationSpeedDarkToLight.Override(2f);
            exposure.adaptationSpeedLightToDark.Override(1.5f);

            Tonemapping tonemapping = Add<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.ACES);

            Bloom bloom = Add<Bloom>(profile);
            bloom.intensity.Override(style == Style.Moody ? 0.35f : 0.2f);
            bloom.scatter.Override(0.65f);
            bloom.threshold.Override(1f);

            Vignette vignette = Add<Vignette>(profile);
            vignette.intensity.Override(style == Style.Moody ? 0.32f : 0.18f);
            vignette.smoothness.Override(0.45f);

            ScreenSpaceAmbientOcclusion ao = Add<ScreenSpaceAmbientOcclusion>(profile);
            ao.intensity.Override(style == Style.Outdoor ? 0.5f : 0.8f);
            ao.radius.Override(1.5f);

            // Motion blur is expensive and can feel smeary in first person.
            MotionBlur motionBlur = Add<MotionBlur>(profile);
            motionBlur.intensity.Override(0f);

            // Shorter shadow distance = sharper shadows and better performance.
            HDShadowSettings shadows = Add<HDShadowSettings>(profile);
            shadows.maxShadowDistance.Override(style == Style.Outdoor ? 150f : 80f);

            if (style == Style.Chamber)
            {
                // A soft, neutral grey sky gives clean white ambient light instead of the default blue tint.
                VisualEnvironment environment = Add<VisualEnvironment>(profile);
                environment.skyType.Override((int)SkyType.Gradient);
                environment.skyAmbientMode.Override(SkyAmbientMode.Dynamic);

                GradientSky sky = Add<GradientSky>(profile);
                sky.top.Override(new Color(0.72f, 0.78f, 0.86f));
                sky.middle.Override(new Color(0.86f, 0.87f, 0.88f));
                sky.bottom.Override(new Color(0.62f, 0.6f, 0.58f));
                sky.gradientDiffusion.Override(1.2f);
                sky.exposure.Override(12f);
            }
            else if (style == Style.Moody)
            {
                Fog fog = Add<Fog>(profile);
                fog.enabled.Override(true);
                fog.meanFreePath.Override(45f);
                fog.maximumHeight.Override(12f);
            }

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            GameObject volumeObject = new GameObject(AddedPrefix + "Post Processing Volume");
            volumeObject.transform.SetParent(parent, false);
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;   // layered over any volume the scene already had
            volume.sharedProfile = profile;

            if (style == Style.Chamber)
            {
                // Tells the light baker which sky to use for ambient light.
                StaticLightingSky staticSky = volumeObject.AddComponent<StaticLightingSky>();
                staticSky.profile = profile;
                staticSky.staticLightingSkyUniqueID = (int)SkyType.Gradient;
            }

            Report.AppendLine($"  volume: {profilePath}");
        }

        private static T Add<T>(VolumeProfile profile) where T : VolumeComponent
        {
            T component = profile.Add<T>();
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        private static void CreateLightProbes(Bounds bounds, Transform parent)
        {
            if (bounds.size == Vector3.zero)
                return;

            // A grid every 4 m, at knee and head height plus near the top of tall rooms.
            List<Vector3> positions = new List<Vector3>();
            float spacing = Mathf.Max(4f, Mathf.Sqrt(bounds.size.x * bounds.size.z / 600f));
            float floor = bounds.min.y;
            float[] heights = bounds.size.y > 6f
                ? new[] { floor + 0.5f, floor + 2.5f, bounds.max.y - 1f }
                : new[] { floor + 0.5f, floor + 2.5f };
            for (float x = bounds.min.x + spacing * 0.5f; x < bounds.max.x; x += spacing)
                for (float z = bounds.min.z + spacing * 0.5f; z < bounds.max.z; z += spacing)
                    foreach (float y in heights)
                        positions.Add(new Vector3(x, y, z));

            GameObject probes = new GameObject(AddedPrefix + "Light Probes");
            probes.transform.SetParent(parent, false);
            probes.AddComponent<LightProbeGroup>().probePositions = positions.ToArray();
            Report.AppendLine($"  light probes: {positions.Count} (spacing {spacing:0.#} m)");
        }

        private static void CreateReflectionProbe(Bounds bounds, Transform parent)
        {
            if (bounds.size == Vector3.zero)
                return;

            GameObject go = new GameObject(AddedPrefix + "Reflection Probe");
            go.transform.SetParent(parent, false);
            go.transform.position = bounds.center;
            ReflectionProbe probe = go.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked;
            HDAdditionalReflectionData data = go.GetComponent<HDAdditionalReflectionData>();
            if (data == null)
                data = go.AddComponent<HDAdditionalReflectionData>();
            data.mode = ProbeSettings.Mode.Baked;
            data.influenceVolume.shape = InfluenceShape.Box;
            data.influenceVolume.boxSize = Vector3.Min(bounds.size + Vector3.one, Vector3.one * 200f);
            Report.AppendLine($"  reflection probe: box {data.influenceVolume.boxSize}");
        }

        /// <summary>A soft spotlight in front of each door so players notice them.</summary>
        private static void CreateDoorAccentLights(Scene scene, Style style, Transform parent)
        {
            Vector3 playerPosition = AllComponents<Transform>(scene).FirstOrDefault(t => t.CompareTag("Player"))?.position ?? Vector3.zero;
            int count = 0;

            foreach (Transform door in AllComponents<Transform>(scene).Where(IsDoor))
            {
                // Light the side of the door that faces the player's start.
                float side = Vector3.Dot(playerPosition - door.position, door.forward) >= 0f ? 1f : -1f;
                GameObject go = new GameObject(AddedPrefix + "Door Accent Light");
                go.transform.SetParent(parent, false);
                go.transform.position = door.position + Vector3.up * 3.6f + door.forward * (side * 1.8f);
                go.transform.LookAt(door.position + Vector3.up * 1.2f);

                Light light = go.AddComponent<Light>();
                light.type = LightType.Spot;
                go.AddComponent<HDAdditionalLightData>();
                light.color = style == Style.Moody ? new Color(1f, 0.78f, 0.55f) : new Color(0.85f, 0.93f, 1f);
                light.intensity = style == Style.Outdoor ? 12000f : 6000f;   // lumens
                light.range = 8f;
                light.spotAngle = 55f;
                light.innerSpotAngle = 25f;
                light.shadows = LightShadows.None;
                light.lightmapBakeType = LightmapBakeType.Realtime;
                count++;
            }
            if (count > 0)
                Report.AppendLine($"  door accent lights: {count}");
        }

        /// <summary>A door is an object named "...Door" that is a prefab instance root or has a collider/child geometry.</summary>
        private static bool IsDoor(Transform t)
        {
            string n = t.name.ToLowerInvariant();
            if (!n.EndsWith("door") || n.StartsWith(AddedPrefix.ToLowerInvariant()))
                return false;
            return t.GetComponent("PuzzleDoor") != null || PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject);
        }

        private static void CreateLightingSettings(Scene scene, Style style)
        {
            string path = $"{SceneFolder(scene)}/{scene.name} Lighting Settings.lighting";
            LightingSettings settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
            if (settings == null)
            {
                settings = new LightingSettings { name = scene.name + " Lighting Settings" };
                AssetDatabase.CreateAsset(settings, path);
            }

            // Baked bounce light into light probes (no lightmap UVs needed), realtime direct light.
            settings.bakedGI = style != Style.Outdoor;
            settings.realtimeGI = false;
            settings.mixedBakeMode = MixedLightingMode.IndirectOnly;
            settings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            settings.lightmapResolution = 8f;
            settings.lightmapMaxSize = 1024;
            settings.directionalityMode = LightmapsMode.NonDirectional;
            settings.indirectSampleCount = 256;
            EditorUtility.SetDirty(settings);

            Lightmapping.lightingSettings = settings;
            Report.AppendLine($"  lighting settings: {path} (baked GI {(settings.bakedGI ? "on" : "off")})");
        }

        /// <summary>
        /// Static level geometry contributes to baked lighting and receives it from light probes,
        /// so ProBuilder meshes and models don't need lightmap UVs.
        /// </summary>
        private static void MarkStaticForBaking(Scene scene)
        {
            int count = 0;
            foreach (MeshRenderer renderer in AllComponents<MeshRenderer>(scene))
            {
                if (!IsStaticGeometry(renderer.gameObject))
                    continue;

                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(renderer.gameObject);
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,
                    flags | StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic);
                renderer.receiveGI = ReceiveGI.LightProbes;
                count++;
            }
            Report.AppendLine($"  static geometry marked for baking: {count}");
        }

        private static Bounds StaticGeometryBounds(Scene scene)
        {
            Bounds bounds = new Bounds();
            bool first = true;
            foreach (MeshRenderer r in AllComponents<MeshRenderer>(scene).Where(r => IsStaticGeometry(r.gameObject)))
            {
                // Ignore huge ground planes so probes are placed where the level actually is.
                if (r.bounds.size.x > 500f || r.bounds.size.z > 500f)
                    continue;
                if (first) { bounds = r.bounds; first = false; }
                else bounds.Encapsulate(r.bounds);
            }
            return bounds;
        }

        /// <summary>
        /// Geometry that never moves: no Rigidbody, Animator or gameplay script on it or its parents,
        /// and not part of the player.
        /// </summary>
        private static bool IsStaticGeometry(GameObject go)
        {
            for (Transform t = go.transform; t != null; t = t.parent)
            {
                if (t.CompareTag("Player") || t.GetComponent<Rigidbody>() || t.GetComponent<Animator>() ||
                    t.GetComponent<Animation>() || t.GetComponent<Camera>() || t.GetComponent<ParticleSystem>())
                    return false;
                if (t.GetComponents<MonoBehaviour>().Any(IsGameplayScript))
                    return false;
            }
            return true;
        }

        /// <summary>ProBuilder and render-pipeline helper components are fine on static geometry. Anything else might move it.</summary>
        private static bool IsGameplayScript(MonoBehaviour m)
        {
            if (m == null)
                return false;
            string ns = m.GetType().Namespace ?? "";
            return !(ns.StartsWith("UnityEngine.ProBuilder") || ns.StartsWith("UnityEngine.Rendering"));
        }

        // ------------------------------------------------------------------
        // Materials
        // ------------------------------------------------------------------

        /// <summary>Switches materials that are used in a scene or prefab but can't render in HDRP to HDRP/Lit.</summary>
        private static void FixMaterials()
        {
            Report.AppendLine("=== Materials");
            Shader lit = Shader.Find("HDRP/Lit");
            string[] roots = AssetDatabase.FindAssets("t:Scene t:Prefab", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
            IEnumerable<string> used = AssetDatabase.GetDependencies(roots, true)
                .Where(p => p.StartsWith("Assets/") && p.EndsWith(".mat"));

            foreach (string path in used)
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null || !NeedsHdrpShader(mat.shader))
                    continue;

                string oldShader = mat.shader != null ? mat.shader.name : "missing";
                Texture baseMap = FirstTexture(mat, "_BaseMap", "_MainTex", "_BaseColorMap") ?? AnyColourTexture(mat);
                Vector2 tiling = baseMap != null ? TextureScale(mat) : Vector2.one;
                Texture normal = FirstTexture(mat, "_BumpMap", "_NormalMap");
                Color color = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                float smoothness = mat.HasProperty("_Smoothness") ? mat.GetFloat("_Smoothness") : mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.4f;
                float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;

                mat.shader = lit;
                mat.SetColor("_BaseColor", color);
                mat.SetTexture("_BaseColorMap", baseMap);
                mat.SetTextureScale("_BaseColorMap", tiling);
                mat.SetFloat("_Smoothness", smoothness);
                mat.SetFloat("_Metallic", metallic);
                if (normal != null)
                {
                    mat.SetTexture("_NormalMap", normal);
                    mat.EnableKeyword("_NORMALMAP");
                }
                HDMaterial.ValidateMaterial(mat);
                EditorUtility.SetDirty(mat);
                Report.AppendLine($"  {path}: {oldShader} -> HDRP/Lit");
            }
        }

        private static bool NeedsHdrpShader(Shader shader)
        {
            if (shader == null || shader.name == "Hidden/InternalErrorShader")
                return true;

            string name = shader.name;
            string path = AssetDatabase.GetAssetPath(shader);
            if (name.StartsWith("Universal Render Pipeline/") || name == "Standard" || name == "Standard (Specular setup)" ||
                name.StartsWith("Legacy Shaders/") || path.Contains("/Built-in_RP/"))
                return true;

            // Shader Graphs made only for URP don't render in HDRP.
            if (path.EndsWith(".shadergraph"))
            {
                string text = File.ReadAllText(path);
                return text.Contains("UniversalTarget") && !text.Contains("HDTarget") && !text.Contains("HighDefinition");
            }
            return false;
        }

        private static Texture FirstTexture(Material mat, params string[] names)
        {
            foreach (string n in names)
            {
                if (mat.HasProperty(n) && mat.GetTexture(n) != null)
                    return mat.GetTexture(n);
            }
            return null;
        }

        private static Texture AnyColourTexture(Material mat)
        {
            foreach (string n in mat.GetTexturePropertyNames())
            {
                string lower = n.ToLowerInvariant();
                if (lower.Contains("normal") || lower.Contains("bump") || lower.Contains("mask") || lower.Contains("occlusion") ||
                    lower.Contains("emission") || lower.Contains("metallic") || lower.Contains("detail"))
                    continue;
                if (mat.GetTexture(n) != null)
                    return mat.GetTexture(n);
            }
            return null;
        }

        private static Vector2 TextureScale(Material mat)
        {
            foreach (string n in new[] { "_BaseMap", "_MainTex" })
            {
                if (mat.HasProperty(n))
                    return mat.GetTextureScale(n);
            }
            return Vector2.one;
        }

        // ------------------------------------------------------------------
        // Physics and hierarchy
        // ------------------------------------------------------------------

        private static void FixPhysics(Scene scene)
        {
            foreach (Rigidbody rb in AllComponents<Rigidbody>(scene).Where(r => !r.isKinematic))
            {
                // Unity can't simulate a moving body with a concave Mesh Collider, so the object falls through the floor.
                foreach (MeshCollider mc in rb.GetComponentsInChildren<MeshCollider>(true).Where(m => !m.convex))
                {
                    mc.convex = true;
                    EditorUtility.SetDirty(mc);
                    Report.AppendLine($"  physics '{ProjectSurvey.HierarchyPath(mc.transform)}': Mesh Collider set to Convex (was falling through floors)");
                }

                if (!rb.GetComponentsInChildren<Collider>(true).Any(c => !c.isTrigger))
                    Report.AppendLine($"  CHECK physics '{ProjectSurvey.HierarchyPath(rb.transform)}': Rigidbody has no solid collider");
            }
        }

        /// <summary>
        /// Scenes with lots of loose root objects get grouped. Only plain geometry and simple physics objects are moved
        /// (world positions are kept and nothing is renamed).
        /// </summary>
        private static void TidyHierarchy(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();

            // Lights and volumes always go in the Lighting group.
            foreach (GameObject root in roots.Where(r => r.name != LightingGroup &&
                         (r.GetComponent<Light>() || r.GetComponent<Volume>() || r.GetComponent<ReflectionProbe>() || r.GetComponent<LightProbeGroup>()) &&
                         r.transform.childCount == 0))
                root.transform.SetParent(GetOrCreateRoot(scene, LightingGroup), true);

            roots = scene.GetRootGameObjects();
            if (roots.Length < 25)
                return;

            int geometry = 0, physics = 0;
            foreach (GameObject root in roots)
            {
                if (root.name == LightingGroup || root.name == GeometryGroup || root.name == PhysicsGroup ||
                    PrefabUtility.IsOutermostPrefabInstanceRoot(root) || !IsPlainObject(root))
                    continue;

                bool dynamic = root.GetComponentInChildren<Rigidbody>(true) != null;
                root.transform.SetParent(GetOrCreateRoot(scene, dynamic ? PhysicsGroup : GeometryGroup), true);
                if (dynamic) physics++; else geometry++;
            }
            Report.AppendLine($"  hierarchy: grouped {geometry} objects into '{GeometryGroup}', {physics} into '{PhysicsGroup}'");
        }

        /// <summary>Only geometry, colliders, rigidbodies and ProBuilder data: nothing a script could depend on by hierarchy.</summary>
        private static bool IsPlainObject(GameObject root)
        {
            return root.GetComponentsInChildren<Component>(true).All(c =>
                c == null ? false :
                c is Transform || c is MeshFilter || c is MeshRenderer || c is Collider || c is Rigidbody ||
                (c.GetType().Namespace ?? "").StartsWith("UnityEngine.ProBuilder"));
        }

        private static Transform GetOrCreateRoot(Scene scene, string name)
        {
            GameObject existing = scene.GetRootGameObjects().FirstOrDefault(r => r.name == name);
            if (existing != null)
                return existing.transform;

            GameObject go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            return go.transform;
        }

        private static void RemovePreviouslyAdded(Transform lighting)
        {
            foreach (Transform child in lighting.Cast<Transform>().ToList().Where(c => c.name.StartsWith(AddedPrefix)))
                Object.DestroyImmediate(child.gameObject);
        }

        private static IEnumerable<T> AllComponents<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true));

        private static string SceneFolder(Scene scene) => Path.GetDirectoryName(scene.path).Replace('\\', '/');
    }
}
