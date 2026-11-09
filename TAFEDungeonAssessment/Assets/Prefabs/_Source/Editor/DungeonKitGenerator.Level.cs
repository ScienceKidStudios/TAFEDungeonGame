using System.Collections.Generic;
using UnityEngine;

namespace PuzzleMechanics.EditorTools
{
    public static partial class DungeonKitGenerator
    {
        // Level pieces snap together on a 4 m grid. The floor top is at y = 0, walls are 4 m tall
        // and 0.3 m thick, and Ceiling_Stone_4x4 goes at y = 4.

        private const float WallHeight = 4f;
        private const float WallThickness = 0.3f;

        // ---------------- Walls ----------------

        /// <summary>4 m wide (along X) and 4 m tall. Rough stone blocks on both faces.</summary>
        private static GameObject BuildWall()
        {
            GameObject root = new GameObject("Wall_Stone_4x4");
            AddBox(root, new Vector3(0f, 2f, 0f), new Vector3(4f, WallHeight, WallThickness));
            Box("Mortar", root.transform, new Vector3(0f, 2f, 0f), new Vector3(4f, WallHeight, WallThickness - 0.02f), mortar);
            StoneCourses(root.transform, null);
            return root;
        }

        /// <summary>The same as a wall, with a 2.4 m wide x 3 m tall opening in the middle for a door.</summary>
        private static GameObject BuildWallDoorway()
        {
            GameObject root = new GameObject("Wall_Stone_Doorway");
            AddBox(root, new Vector3(-1.6f, 2f, 0f), new Vector3(0.8f, WallHeight, WallThickness));
            AddBox(root, new Vector3(1.6f, 2f, 0f), new Vector3(0.8f, WallHeight, WallThickness));
            AddBox(root, new Vector3(0f, 3.5f, 0f), new Vector3(2.4f, 1f, WallThickness));

            float core = WallThickness - 0.02f;
            Box("Mortar", root.transform, new Vector3(-1.6f, 2f, 0f), new Vector3(0.8f, WallHeight, core), mortar);
            Box("Mortar", root.transform, new Vector3(1.6f, 2f, 0f), new Vector3(0.8f, WallHeight, core), mortar);
            Box("Mortar", root.transform, new Vector3(0f, 3.5f, 0f), new Vector3(2.4f, 1f, core), mortar);
            StoneCourses(root.transform, new Rect(-1.2f, 0f, 2.4f, 3f));

            Box("Lintel Stone", root.transform, new Vector3(0f, 3.17f, 0f), new Vector3(3f, 0.34f, WallThickness + 0.08f), stoneLight);
            foreach (float x in new[] { -1.3f, 1.3f })
                for (int i = 0; i < 4; i++)
                    Box("Jamb Stone", root.transform, new Vector3(x, 0.4f + i * 0.72f, 0f),
                        new Vector3(i % 2 == 0 ? 0.24f : 0.18f, 0.68f, WallThickness + 0.06f), stoneLight);
            return root;
        }

        /// <summary>
        /// Rows of stone blocks across the wall, staggered like brickwork, with a plinth at the bottom and a
        /// capping row at the top. Blocks are cut around 'opening' (x, y, width, height) if one is given.
        /// </summary>
        private static void StoneCourses(Transform wall, Rect? opening)
        {
            const float plinth = 0.3f;
            const float cap = 0.25f;
            const int courses = 6;
            const float gap = 0.04f;
            float courseHeight = (WallHeight - plinth - cap) / courses;
            float depth = WallThickness + 0.04f;

            AddWallRun(wall, "Plinth", -2f, 2f, plinth * 0.5f, plinth, WallThickness + 0.1f, stoneDark, opening);
            AddWallRun(wall, "Capping", -2f, 2f, WallHeight - cap * 0.5f, cap, WallThickness + 0.06f, stoneDark, opening);

            for (int row = 0; row < courses; row++)
            {
                float y = plinth + courseHeight * (row + 0.5f);
                float[] widths = row % 2 == 0
                    ? new[] { 4f / 3f, 4f / 3f, 4f / 3f }
                    : new[] { 2f / 3f, 4f / 3f, 4f / 3f, 2f / 3f };
                float x = -2f;
                for (int i = 0; i < widths.Length; i++)
                {
                    Material mat = (row * 3 + i) % 4 == 0 ? stoneLight : stone;
                    AddWallRun(wall, "Stone Block", x + gap * 0.5f, x + widths[i] - gap * 0.5f, y, courseHeight - gap, depth, mat, opening);
                    x += widths[i];
                }
            }
        }

        /// <summary>A block from x0 to x1, split into two if it crosses the opening.</summary>
        private static void AddWallRun(Transform wall, string name, float x0, float x1, float y, float height, float depth,
            Material mat, Rect? opening)
        {
            List<Vector2> pieces = new List<Vector2> { new Vector2(x0, x1) };
            if (opening.HasValue)
            {
                Rect o = opening.Value;
                bool overlapsY = y - height * 0.5f < o.yMax - 0.01f && y + height * 0.5f > o.yMin + 0.01f;
                if (overlapsY && x0 < o.xMax && x1 > o.xMin)
                {
                    pieces.Clear();
                    if (o.xMin - x0 > 0.05f) pieces.Add(new Vector2(x0, o.xMin));
                    if (x1 - o.xMax > 0.05f) pieces.Add(new Vector2(o.xMax, x1));
                }
            }
            foreach (Vector2 p in pieces)
                Box(name, wall, new Vector3((p.x + p.y) * 0.5f, y, 0f), new Vector3(p.y - p.x, height, depth), mat);
        }

        // ---------------- Floor and ceiling ----------------

        /// <summary>A 4 x 4 m tile of flagstones. Its top is at y = 0.</summary>
        private static GameObject BuildFloorTile()
        {
            GameObject root = new GameObject("Floor_Stone_4x4");
            AddBox(root, new Vector3(0f, -0.1f, 0f), new Vector3(4f, 0.2f, 4f));
            Box("Bedding", root.transform, new Vector3(0f, -0.11f, 0f), new Vector3(4f, 0.18f, 4f), mortar);

            float[][] rows =
            {
                new[] { 1.5f, 1.25f, 1.25f },
                new[] { 1.0f, 1.6f, 1.4f },
                new[] { 1.3f, 1.4f, 1.3f },
            };
            const float gap = 0.04f;
            float depth = 4f / rows.Length;
            for (int r = 0; r < rows.Length; r++)
            {
                float z = -2f + depth * (r + 0.5f);
                float x = -2f;
                for (int i = 0; i < rows[r].Length; i++)
                {
                    float w = rows[r][i];
                    Material mat = (r + i) % 3 == 0 ? stone : stoneFloor;
                    Box("Flagstone", root.transform, new Vector3(x + w * 0.5f, -0.02f, z), new Vector3(w - gap, 0.04f, depth - gap), mat);
                    x += w;
                }
            }
            return root;
        }

        /// <summary>A 4 x 4 m ceiling slab with a wooden beam. Place it at y = 4 (its underside is at the origin).</summary>
        private static GameObject BuildCeilingTile()
        {
            GameObject root = new GameObject("Ceiling_Stone_4x4");
            AddBox(root, new Vector3(0f, 0.15f, 0f), new Vector3(4f, 0.3f, 4f));
            Box("Slab", root.transform, new Vector3(0f, 0.15f, 0f), new Vector3(4f, 0.3f, 4f), stoneDark);
            foreach (float x in new[] { -1f, 1f })
                foreach (float z in new[] { -1f, 1f })
                    Box("Stone Panel", root.transform, new Vector3(x, -0.01f, z), new Vector3(1.9f, 0.02f, 1.9f), stone);
            Box("Beam", root.transform, new Vector3(0f, -0.11f, 0f), new Vector3(4f, 0.22f, 0.26f), woodDark);
            return root;
        }

        // ---------------- Pillars, arch, stairs, platform ----------------

        /// <summary>A round 4 m stone pillar with a square base and top. It reaches the ceiling.</summary>
        private static GameObject BuildPillar()
        {
            GameObject root = new GameObject("Pillar_Stone");
            AddBox(root, new Vector3(0f, 2f, 0f), new Vector3(0.9f, 4f, 0.9f));
            Box("Plinth", root.transform, new Vector3(0f, 0.15f, 0f), new Vector3(1f, 0.3f, 1f), stoneDark);
            Cylinder("Base Ring", root.transform, new Vector3(0f, 0.36f, 0f), 0.45f, 0.12f, stone);
            Cylinder("Shaft", root.transform, new Vector3(0f, 2f, 0f), 0.35f, 3.2f, stone);
            foreach (float y in new[] { 1.2f, 2.4f })
                Cylinder("Joint", root.transform, new Vector3(0f, y, 0f), 0.355f, 0.03f, mortar);
            Cylinder("Capital Ring", root.transform, new Vector3(0f, 3.64f, 0f), 0.45f, 0.12f, stone);
            Box("Capital", root.transform, new Vector3(0f, 3.85f, 0f), new Vector3(1f, 0.3f, 1f), stoneDark);
            return root;
        }

        /// <summary>A pillar that has snapped off, with a fallen piece beside it.</summary>
        private static GameObject BuildPillarBroken()
        {
            GameObject root = new GameObject("Pillar_Broken");
            AddBox(root, new Vector3(0f, 0.85f, 0f), new Vector3(0.9f, 1.7f, 0.9f));
            Box("Plinth", root.transform, new Vector3(0f, 0.15f, 0f), new Vector3(1f, 0.3f, 1f), stoneDark);
            Cylinder("Base Ring", root.transform, new Vector3(0f, 0.36f, 0f), 0.45f, 0.12f, stone);
            Cylinder("Shaft", root.transform, new Vector3(0f, 1.0f, 0f), 0.35f, 1.2f, stone);
            Box("Broken Top", root.transform, new Vector3(0.08f, 1.65f, 0.05f), new Vector3(0.4f, 0.3f, 0.4f), stone, euler: new Vector3(12f, 30f, 8f));
            Box("Broken Top", root.transform, new Vector3(-0.12f, 1.6f, -0.08f), new Vector3(0.3f, 0.22f, 0.32f), stone, euler: new Vector3(-10f, 15f, 20f));
            Box("Broken Top", root.transform, new Vector3(0.05f, 1.56f, -0.15f), new Vector3(0.36f, 0.18f, 0.2f), stoneLight, euler: new Vector3(5f, -40f, -12f));

            GameObject fallen = Empty("Fallen Piece", root.transform, new Vector3(1.05f, 0.34f, 0.45f), new Vector3(0f, 35f, 0f));
            AddBox(fallen, Vector3.zero, new Vector3(0.95f, 0.66f, 0.66f));
            Cylinder("Shaft", fallen.transform, Vector3.zero, 0.33f, 0.95f, stone, Axis.X);
            Box("Broken End", fallen.transform, new Vector3(-0.45f, 0.05f, 0f), new Vector3(0.18f, 0.4f, 0.4f), stone, euler: new Vector3(20f, 0f, 10f));
            Box("Chip", root.transform, new Vector3(-0.7f, 0.06f, 0.5f), new Vector3(0.22f, 0.12f, 0.18f), stoneLight, euler: new Vector3(0f, 25f, 10f));
            return root;
        }

        /// <summary>A free-standing stone arch, 4 m wide and 4 m tall. Walk through it along Z.</summary>
        private static GameObject BuildArchway()
        {
            GameObject root = new GameObject("Archway_Stone");
            const float springY = 2.2f;   // where the curve starts
            const float radius = 1.5f;    // middle of the arch stones
            foreach (float side in new[] { -1f, 1f })
            {
                Box("Pier", root.transform, new Vector3(1.5f * side, springY * 0.5f, 0f), new Vector3(0.6f, springY, 0.8f), stone, collider: true);
                Box("Pier Base", root.transform, new Vector3(1.5f * side, 0.15f, 0f), new Vector3(0.76f, 0.3f, 0.9f), stoneDark);
                Box("Pier Top", root.transform, new Vector3(1.5f * side, springY - 0.08f, 0f), new Vector3(0.72f, 0.16f, 0.88f), stoneDark);
                Box("Spandrel", root.transform, new Vector3(1.62f * side, 3.55f, 0f), new Vector3(0.36f, 0.9f, 0.8f), stone, collider: true);
            }

            const int stones = 9;
            for (int i = 0; i < stones; i++)
            {
                float angle = 180f * (i + 0.5f) / stones;   // 0 = right, 180 = left
                float rad = angle * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(Mathf.Cos(rad) * radius, springY + Mathf.Sin(rad) * radius, 0f);
                bool keystone = i == stones / 2;
                Vector3 size = keystone ? new Vector3(0.58f, 0.7f, 0.9f) : new Vector3(0.5f, 0.6f, 0.8f);
                Box(keystone ? "Keystone" : "Arch Stone", root.transform, pos, size, keystone ? stoneLight : stone,
                    collider: true, euler: new Vector3(0f, 0f, angle - 90f));
            }
            return root;
        }

        /// <summary>Four 25 cm steps, 2 m wide, climbing towards +Z up to 1 m (the height of Platform_Stone).</summary>
        private static GameObject BuildStairs()
        {
            const int count = 4;
            const float rise = 0.25f;
            const float run = 0.4f;
            GameObject root = new GameObject("Stairs_Stone");
            for (int i = 0; i < count; i++)
            {
                float height = (i + 1) * rise;
                Box("Step", root.transform, new Vector3(0f, height * 0.5f, run * (i + 0.5f)), new Vector3(2f, height, run),
                    i % 2 == 0 ? stone : stoneFloor, collider: true);
                Box("Step Edge", root.transform, new Vector3(0f, height - 0.02f, run * i + 0.03f), new Vector3(2.02f, 0.045f, 0.07f), stoneLight);
            }
            Box("Side", root.transform, new Vector3(-1.04f, 0.5f, 0.8f), new Vector3(0.08f, 1f, 1.6f), stoneDark);
            Box("Side", root.transform, new Vector3(1.04f, 0.5f, 0.8f), new Vector3(0.08f, 1f, 1.6f), stoneDark);
            return root;
        }

        /// <summary>A 4 x 4 m raised stone platform, 1 m high. Pair it with Stairs_Stone.</summary>
        private static GameObject BuildPlatform()
        {
            GameObject root = new GameObject("Platform_Stone");
            AddBox(root, new Vector3(0f, 0.5f, 0f), new Vector3(4f, 1f, 4f));
            Box("Core", root.transform, new Vector3(0f, 0.48f, 0f), new Vector3(3.96f, 0.96f, 3.96f), mortar);

            // Two rows of blocks around the sides, staggered.
            for (int row = 0; row < 2; row++)
            {
                float y = 0.24f + row * 0.48f;
                float shift = row == 0 ? 0f : 0.5f;
                for (int side = 0; side < 4; side++)
                {
                    Quaternion rot = Quaternion.Euler(0f, side * 90f, 0f);
                    for (int i = 0; i < 4; i++)
                    {
                        float x = -2f + shift + i;
                        float x0 = Mathf.Max(-2f, x - 0.5f + 0.02f);
                        float x1 = Mathf.Min(2f, x + 0.5f - 0.02f);
                        if (x1 - x0 < 0.1f)
                            continue;
                        Vector3 pos = rot * new Vector3((x0 + x1) * 0.5f, y, 1.99f);
                        Box("Side Block", root.transform, pos, new Vector3(x1 - x0, 0.44f, 0.06f), (i + row + side) % 3 == 0 ? stoneLight : stone,
                            euler: new Vector3(0f, side * 90f, 0f));
                    }
                }
            }
            foreach (float x in new[] { -1f, 1f })
                foreach (float z in new[] { -1f, 1f })
                    Box("Top Stone", root.transform, new Vector3(x, 0.98f, z), new Vector3(1.96f, 0.04f, 1.96f), (x * z > 0) ? stoneFloor : stone);
            return root;
        }

        // ---------------- Player ----------------

        private static GameObject BuildPlayer()
        {
            GameObject root = new GameObject("Player_FirstPerson");
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
            hud.font = bodyFont;
            return root;
        }
    }
}
