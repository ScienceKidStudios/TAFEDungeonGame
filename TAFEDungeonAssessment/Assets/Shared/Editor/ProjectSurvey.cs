using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ProjectTools
{
    /// <summary>
    /// Read-only project report: lists what is in every scene (lights, volumes, cameras,
    /// physics problems, missing scripts) and saves a preview image of each scene.
    /// Nothing in the project is changed.
    ///
    /// Menu: Tools > Project > Survey Scenes
    /// Batch mode: -executeMethod ProjectTools.ProjectSurvey.Run -surveyOut "C:/some/folder"
    /// </summary>
    public static class ProjectSurvey
    {
        [MenuItem("Tools/Project/Survey Scenes")]
        public static void RunFromMenu() => Survey(Path.Combine(Application.dataPath, "../Temp/Survey"));

        public static void Run()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-surveyOut");
            Survey(i >= 0 ? args[i + 1] : Path.Combine(Application.dataPath, "../Temp/Survey"));
        }

        private static void Survey(string outDir)
        {
            Directory.CreateDirectory(outDir);
            StringBuilder report = new StringBuilder();

            foreach (string scenePath in AllScenePaths())
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                report.AppendLine($"=== {scenePath}");
                DescribeScene(scene, report);
                string imageName = Path.GetFileNameWithoutExtension(scenePath).Replace(" ", "_").Replace("'", "");
                report.AppendLine("  preview: " + SavePreview(Path.Combine(outDir, imageName + ".png")));
            }

            File.WriteAllText(Path.Combine(outDir, "survey.txt"), report.ToString());
            Debug.Log("[Survey] Written to " + outDir);
        }

        public static IEnumerable<string> AllScenePaths() =>
            AssetDatabase.FindAssets("t:Scene", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p);

        private static void DescribeScene(Scene scene, StringBuilder report)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            List<GameObject> all = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToList();

            Bounds bounds = new Bounds();
            bool hasBounds = false;
            foreach (Renderer r in all.Select(g => g.GetComponent<Renderer>()).Where(r => r != null && !(r is ParticleSystemRenderer)))
            {
                if (!hasBounds) { bounds = r.bounds; hasBounds = true; }
                else bounds.Encapsulate(r.bounds);
            }

            report.AppendLine($"  roots={roots.Length} objects={all.Count} bounds centre={bounds.center} size={bounds.size}");
            report.AppendLine("  root names: " + string.Join(", ", roots.Take(40).Select(r => r.name)) + (roots.Length > 40 ? " ..." : ""));
            report.AppendLine($"  ambientMode={RenderSettings.ambientMode} skybox={(RenderSettings.skybox ? RenderSettings.skybox.name : "none")} fog={RenderSettings.fog}");

            foreach (Light l in all.Select(g => g.GetComponent<Light>()).Where(l => l != null))
                report.AppendLine($"  light '{HierarchyPath(l.transform)}' type={l.type} intensity={l.intensity} color={l.color} shadows={l.shadows} mode={l.lightmapBakeType} range={l.range} rot={l.transform.eulerAngles} enabled={l.enabled && l.gameObject.activeInHierarchy}");

            foreach (Volume v in all.Select(g => g.GetComponent<Volume>()).Where(v => v != null))
            {
                string comps = v.sharedProfile != null ? string.Join(",", v.sharedProfile.components.Select(c => c.GetType().Name)) : "no profile";
                report.AppendLine($"  volume '{HierarchyPath(v.transform)}' global={v.isGlobal} priority={v.priority} profile={(v.sharedProfile ? AssetDatabase.GetAssetPath(v.sharedProfile) : "none")} [{comps}]");
            }

            foreach (Camera c in all.Select(g => g.GetComponent<Camera>()).Where(c => c != null))
                report.AppendLine($"  camera '{HierarchyPath(c.transform)}' tag={c.tag} pos={c.transform.position}");

            foreach (ReflectionProbe p in all.Select(g => g.GetComponent<ReflectionProbe>()).Where(p => p != null))
                report.AppendLine($"  probe '{HierarchyPath(p.transform)}' mode={p.mode} size={p.size}");

            foreach (GameObject g in all.Where(g => g.name.ToLower().Contains("door") || g.name.ToLower().Contains("exit") || g.name.ToLower().Contains("button") || g.CompareTag("Player")))
                report.AppendLine($"  notable '{HierarchyPath(g.transform)}' tag={g.tag} pos={g.transform.position}");

            int missingScripts = 0;
            foreach (GameObject g in all)
            {
                int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(g);
                if (count > 0)
                {
                    missingScripts += count;
                    report.AppendLine($"  MISSING SCRIPT x{count} on '{HierarchyPath(g.transform)}'");
                }
                if (PrefabUtility.IsPrefabAssetMissing(g) && PrefabUtility.IsOutermostPrefabInstanceRoot(g))
                    report.AppendLine($"  MISSING PREFAB for '{HierarchyPath(g.transform)}'");
            }

            foreach (Rigidbody rb in all.Select(g => g.GetComponent<Rigidbody>()).Where(r => r != null))
            {
                Collider[] colliders = rb.GetComponentsInChildren<Collider>(true);
                bool solid = colliders.Any(c => !c.isTrigger);
                bool concaveMesh = !rb.isKinematic && colliders.OfType<MeshCollider>().Any(m => !m.convex);
                if (!solid || concaveMesh)
                    report.AppendLine($"  PHYSICS '{HierarchyPath(rb.transform)}' kinematic={rb.isKinematic} solidCollider={solid} concaveMeshOnDynamicBody={concaveMesh}");
            }

            foreach (Component c in all.SelectMany(g => g.GetComponents<Component>()).Where(c => c != null))
            {
                SerializedObject so = new SerializedObject(c);
                SerializedProperty prop = so.GetIterator();
                while (prop.NextVisible(true))
                {
                    if (prop.propertyType == SerializedPropertyType.ObjectReference &&
                        prop.objectReferenceValue == null && prop.objectReferenceInstanceIDValue != 0)
                        report.AppendLine($"  BROKEN REF '{HierarchyPath(c.transform)}' {c.GetType().Name}.{prop.propertyPath}");
                }
            }
        }

        private static string SavePreview(string file)
        {
            Camera cam = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                .OrderByDescending(c => c.CompareTag("MainCamera")).FirstOrDefault();
            GameObject temp = null;
            if (cam == null)
            {
                Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
                if (renderers.Length == 0)
                    return "no renderers";
                Bounds b = renderers[0].bounds;
                foreach (Renderer r in renderers) b.Encapsulate(r.bounds);

                temp = new GameObject("Survey Camera");
                cam = temp.AddComponent<Camera>();
                float distance = Mathf.Max(b.extents.magnitude, 5f) * 1.2f;
                cam.transform.position = b.center + new Vector3(-0.6f, 0.7f, -0.8f).normalized * distance;
                cam.transform.LookAt(b.center);
                cam.farClipPlane = distance * 4f;
            }

            RenderTexture rt = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = cam.targetTexture;
            cam.targetTexture = rt;
            try
            {
                // Several frames so auto exposure can settle.
                for (int i = 0; i < 40; i++)
                    cam.Render();

                RenderTexture.active = rt;
                Texture2D image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                image.Apply();
                File.WriteAllBytes(file, image.EncodeToPNG());
                Object.DestroyImmediate(image);
                return file + (temp != null ? " (overview camera)" : $" (camera '{cam.name}')");
            }
            catch (System.Exception e)
            {
                return "render failed: " + e.Message;
            }
            finally
            {
                RenderTexture.active = null;
                cam.targetTexture = previous;
                rt.Release();
                if (temp != null)
                    Object.DestroyImmediate(temp);
            }
        }

        public static string HierarchyPath(Transform t) => t.parent == null ? t.name : HierarchyPath(t.parent) + "/" + t.name;
    }
}
