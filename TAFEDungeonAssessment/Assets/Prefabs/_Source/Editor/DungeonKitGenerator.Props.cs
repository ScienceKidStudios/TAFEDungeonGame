using UnityEngine;
using TMPro;

namespace PuzzleMechanics.EditorTools
{
    public static partial class DungeonKitGenerator
    {
        private static readonly Color TorchColor = new Color(1f, 0.6f, 0.28f);

        // ---------------- Lights ----------------

        /// <summary>A burning torch in an iron wall bracket, with a flickering light. Faces +Z.</summary>
        private static GameObject BuildWallTorch()
        {
            GameObject root = new GameObject("Torch_Wall");
            TorchBracket(root.transform, out Vector3 flamePosition);
            Sphere("Burning Rag", root.transform, flamePosition + new Vector3(0f, -0.04f, 0f), new Vector3(0.09f, 0.07f, 0.09f), ember);
            Fire(root.transform, flamePosition, 0.8f, 900f, 9f);
            return root;
        }

        /// <summary>The iron bracket and wooden stick shared by both wall torches.</summary>
        private static void TorchBracket(Transform root, out Vector3 flamePosition)
        {
            Box("Wall Plate", root, new Vector3(0f, 0f, 0.015f), new Vector3(0.12f, 0.28f, 0.03f), iron);
            Box("Arm", root, new Vector3(0f, -0.04f, 0.1f), new Vector3(0.035f, 0.035f, 0.17f), iron, euler: new Vector3(-20f, 0f, 0f));
            Ring("Cup", root, new Vector3(0f, 0f, 0.19f), 0.05f, 8, 0.05f, 0.015f, iron);

            // The stick leans out from the wall a little.
            GameObject stick = Empty("Torch", root, new Vector3(0f, 0f, 0.19f), new Vector3(15f, 0f, 0f));
            Cylinder("Stick", stick.transform, new Vector3(0f, 0.03f, 0f), 0.03f, 0.42f, wood);
            Cylinder("Wrap", stick.transform, new Vector3(0f, 0.2f, 0f), 0.042f, 0.09f, clothRed);
            flamePosition = root.InverseTransformPoint(stick.transform.TransformPoint(new Vector3(0f, 0.27f, 0f)));
        }

        /// <summary>An orange flame made of glowing spheres, with a flickering point light.</summary>
        private static void Fire(Transform parent, Vector3 position, float size, float lumens, float range)
        {
            GameObject fire = Empty("Fire", parent, position);
            GameObject outer = Sphere("Flame", fire.transform, new Vector3(0f, 0.1f * size, 0f), new Vector3(0.18f, 0.3f, 0.18f) * size, flame);
            Sphere("Flame Core", fire.transform, new Vector3(0f, 0.07f * size, 0f), new Vector3(0.1f, 0.16f, 0.1f) * size, flameCore);
            Sphere("Flame Tip", fire.transform, new Vector3(0.015f, 0.26f * size, 0f), new Vector3(0.08f, 0.16f, 0.08f) * size, flame);

            GameObject lightObject = Empty("Torch Light", fire.transform, new Vector3(0f, 0.25f * size, 0.05f));
            AddFlameLight(lightObject, TorchColor, lumens, range);
            FlickeringLight flicker = lightObject.AddComponent<FlickeringLight>();
            flicker.flame = outer.transform;
        }

        /// <summary>A tall iron fire bowl on three legs, with a flickering light.</summary>
        private static GameObject BuildStandingBrazier()
        {
            GameObject root = new GameObject("Brazier_Standing");
            AddBox(root, new Vector3(0f, 0.6f, 0f), new Vector3(0.8f, 1.2f, 0.8f));

            for (int i = 0; i < 3; i++)
            {
                float angle = i * 120f;
                Vector3 pos = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0.45f, 0.22f);
                Box("Leg", root.transform, pos, new Vector3(0.05f, 0.95f, 0.05f), iron, euler: new Vector3(-14f, angle, 0f));
                Vector3 foot = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0.02f, 0.33f);
                Box("Foot", root.transform, foot, new Vector3(0.1f, 0.04f, 0.14f), iron, euler: new Vector3(0f, angle, 0f));
            }
            Ring("Leg Ring", root.transform, new Vector3(0f, 0.45f, 0f), 0.2f, 10, 0.03f, 0.03f, iron);
            Cylinder("Bowl", root.transform, new Vector3(0f, 0.95f, 0f), 0.36f, 0.16f, iron);
            Cylinder("Bowl Base", root.transform, new Vector3(0f, 0.86f, 0f), 0.24f, 0.06f, iron);
            Ring("Bowl Lip", root.transform, new Vector3(0f, 1.04f, 0f), 0.37f, 14, 0.04f, 0.04f, iron);
            Cylinder("Coals", root.transform, new Vector3(0f, 1.03f, 0f), 0.31f, 0.03f, coal);
            for (int i = 0; i < 5; i++)
            {
                Vector3 pos = Quaternion.Euler(0f, i * 72f, 0f) * new Vector3(0f, 1.06f, 0.16f);
                Sphere("Ember", root.transform, pos, new Vector3(0.1f, 0.06f, 0.1f), ember);
            }
            Fire(root.transform, new Vector3(0f, 1.05f, 0f), 2f, 1400f, 10f);
            return root;
        }

        /// <summary>A few wax candles on an iron plate. Decoration only (no light, so you can use lots).</summary>
        private static GameObject BuildCandles()
        {
            GameObject root = new GameObject("Candles");
            Cylinder("Plate", root.transform, new Vector3(0f, 0.01f, 0f), 0.16f, 0.02f, iron);
            Vector3[] spots = { new Vector3(-0.06f, 0f, 0.03f), new Vector3(0.06f, 0f, 0.04f), new Vector3(0f, 0f, -0.07f) };
            float[] heights = { 0.22f, 0.15f, 0.1f };
            for (int i = 0; i < spots.Length; i++)
            {
                float h = heights[i];
                Cylinder("Candle", root.transform, spots[i] + new Vector3(0f, 0.02f + h * 0.5f, 0f), 0.03f, h, wax);
                Sphere("Candle Flame", root.transform, spots[i] + new Vector3(0f, 0.045f + h, 0f), new Vector3(0.025f, 0.05f, 0.025f), flameCore);
            }
            return root;
        }

        // ---------------- Treasure chest ----------------

        /// <summary>A wooden chest with iron bands. Press E to open the lid. Its front (with the lock) faces +Z.</summary>
        private static GameObject BuildChest()
        {
            GameObject root = new GameObject("Chest_Treasure");
            AddBox(root, new Vector3(0f, 0.3f, 0f), new Vector3(1f, 0.6f, 0.62f));

            // A hollow box, so the treasure inside shows when the lid opens.
            Box("Bottom", root.transform, new Vector3(0f, 0.04f, 0f), new Vector3(1f, 0.08f, 0.6f), woodDark);
            Box("Front", root.transform, new Vector3(0f, 0.27f, 0.27f), new Vector3(1f, 0.46f, 0.06f), wood);
            Box("Back", root.transform, new Vector3(0f, 0.27f, -0.27f), new Vector3(1f, 0.46f, 0.06f), wood);
            Box("Side", root.transform, new Vector3(0.47f, 0.27f, 0f), new Vector3(0.06f, 0.46f, 0.48f), wood);
            Box("Side", root.transform, new Vector3(-0.47f, 0.27f, 0f), new Vector3(0.06f, 0.46f, 0.48f), wood);
            foreach (float x in new[] { -0.3f, 0.3f })
            {
                Box("Iron Band", root.transform, new Vector3(x, 0.27f, 0.302f), new Vector3(0.07f, 0.48f, 0.01f), iron);
                Box("Iron Band", root.transform, new Vector3(x, 0.27f, -0.302f), new Vector3(0.07f, 0.48f, 0.01f), iron);
            }
            Box("Lock Plate", root.transform, new Vector3(0f, 0.4f, 0.31f), new Vector3(0.14f, 0.16f, 0.02f), gold);
            Box("Keyhole", root.transform, new Vector3(0f, 0.38f, 0.321f), new Vector3(0.025f, 0.06f, 0.005f), coal);

            // Treasure
            Box("Gold Pile", root.transform, new Vector3(0f, 0.38f, 0f), new Vector3(0.86f, 0.12f, 0.46f), gold);
            for (int i = 0; i < 7; i++)
            {
                Vector3 pos = new Vector3(-0.32f + i * 0.11f, 0.45f + (i % 3) * 0.015f, (i % 2 == 0 ? 0.08f : -0.07f));
                Cylinder("Coin", root.transform, pos, 0.05f, 0.012f, gold);
            }
            Sphere("Gem", root.transform, new Vector3(0.18f, 0.47f, 0.05f), new Vector3(0.07f, 0.06f, 0.07f), clothRed);

            // The lid's pivot is its hinge along the back edge.
            GameObject lid = Empty("Lid (hinge)", root.transform, new Vector3(0f, 0.5f, -0.3f));
            Box("Lid", lid.transform, new Vector3(0f, 0.04f, 0.3f), new Vector3(1.02f, 0.08f, 0.62f), wood);
            Part("Lid Top", PrimitiveType.Cylinder, lid.transform, new Vector3(0f, 0.08f, 0.3f), new Vector3(0.24f, 0.51f, 0.6f), wood,
                euler: new Vector3(0f, 0f, 90f));
            foreach (float x in new[] { -0.3f, 0.3f })
                Part("Iron Band", PrimitiveType.Cylinder, lid.transform, new Vector3(x, 0.08f, 0.3f), new Vector3(0.27f, 0.036f, 0.63f), iron,
                    euler: new Vector3(0f, 0f, 90f));
            Box("Hasp", lid.transform, new Vector3(0f, 0f, 0.62f), new Vector3(0.1f, 0.12f, 0.02f), gold);

            TreasureChest chest = root.AddComponent<TreasureChest>();
            chest.lidPivot = lid.transform;
            return root;
        }

        // ---------------- Furniture ----------------

        private static GameObject BuildTable()
        {
            GameObject root = new GameObject("Table_Wooden");
            Box("Top", root.transform, new Vector3(0f, 0.76f, 0f), new Vector3(1.6f, 0.08f, 0.9f), wood, collider: true);
            Box("Top Edge", root.transform, new Vector3(0f, 0.7f, 0f), new Vector3(1.5f, 0.06f, 0.8f), woodDark);
            foreach (float x in new[] { -0.68f, 0.68f })
            {
                foreach (float z in new[] { -0.34f, 0.34f })
                    Box("Leg", root.transform, new Vector3(x, 0.36f, z), new Vector3(0.09f, 0.72f, 0.09f), woodDark, collider: true);
                Box("Stretcher", root.transform, new Vector3(x, 0.15f, 0f), new Vector3(0.05f, 0.06f, 0.68f), woodDark);
            }
            Box("Stretcher", root.transform, new Vector3(0f, 0.15f, 0f), new Vector3(1.36f, 0.06f, 0.05f), woodDark);
            return root;
        }

        private static GameObject BuildBench()
        {
            GameObject root = new GameObject("Bench_Wooden");
            Box("Seat", root.transform, new Vector3(0f, 0.44f, 0f), new Vector3(1.4f, 0.07f, 0.36f), wood, collider: true);
            foreach (float x in new[] { -0.55f, 0.55f })
                Box("Leg", root.transform, new Vector3(x, 0.2f, 0f), new Vector3(0.07f, 0.41f, 0.32f), woodDark, collider: true);
            Box("Stretcher", root.transform, new Vector3(0f, 0.14f, 0f), new Vector3(1.1f, 0.06f, 0.05f), woodDark);
            return root;
        }

        /// <summary>A tall shelf full of books. Its open front faces +Z.</summary>
        private static GameObject BuildBookshelf()
        {
            GameObject root = new GameObject("Bookshelf");
            AddBox(root, new Vector3(0f, 1f, 0f), new Vector3(1.3f, 2f, 0.42f));

            Box("Side", root.transform, new Vector3(-0.625f, 1f, 0f), new Vector3(0.05f, 2f, 0.42f), woodDark);
            Box("Side", root.transform, new Vector3(0.625f, 1f, 0f), new Vector3(0.05f, 2f, 0.42f), woodDark);
            Box("Back", root.transform, new Vector3(0f, 1f, -0.195f), new Vector3(1.2f, 2f, 0.03f), wood);
            Box("Top", root.transform, new Vector3(0f, 1.98f, 0f), new Vector3(1.32f, 0.05f, 0.44f), woodDark);

            Material[] covers = { clothRed, bookGreen, clothBlue, woodLight, paper };
            Random.State saved = Random.state;
            Random.InitState(7);   // the same "random" books every time
            float[] shelves = { 0.06f, 0.54f, 1.02f, 1.5f };
            foreach (float y in shelves)
            {
                Box("Shelf", root.transform, new Vector3(0f, y, 0f), new Vector3(1.2f, 0.04f, 0.4f), woodDark);
                float x = -0.57f;
                while (x < 0.5f)
                {
                    float w = Random.Range(0.04f, 0.08f);
                    float h = Random.Range(0.26f, 0.38f);
                    float lean = Random.value < 0.12f ? 12f : 0f;
                    Box("Book", root.transform, new Vector3(x + w * 0.5f, y + 0.02f + h * 0.5f, 0.02f), new Vector3(w, h, 0.26f),
                        covers[Random.Range(0, covers.Length)], euler: new Vector3(0f, 0f, lean));
                    x += w + 0.008f + (lean > 0f ? 0.05f : 0f);
                    if (Random.value < 0.08f)
                        x += 0.12f;   // the odd gap
                }
            }
            Random.state = saved;
            return root;
        }

        // ---------------- Decoration ----------------

        private static GameObject BuildSkullProp()
        {
            GameObject root = new GameObject("Skull");
            SkullModel(root.transform, Vector3.zero, 0f);
            return root;
        }

        private static void SkullModel(Transform parent, Vector3 position, float yaw)
        {
            GameObject skull = Empty("Skull Model", parent, position, new Vector3(0f, yaw, 0f));
            Sphere("Cranium", skull.transform, new Vector3(0f, 0.11f, -0.01f), new Vector3(0.18f, 0.17f, 0.21f), bone);
            Sphere("Face", skull.transform, new Vector3(0f, 0.07f, 0.06f), new Vector3(0.14f, 0.12f, 0.1f), bone);
            Box("Jaw", skull.transform, new Vector3(0f, 0.025f, 0.07f), new Vector3(0.1f, 0.04f, 0.07f), bone);
            foreach (float x in new[] { -0.035f, 0.035f })
                Sphere("Eye Socket", skull.transform, new Vector3(x, 0.09f, 0.1f), new Vector3(0.045f, 0.04f, 0.03f), coal);
            Box("Nose", skull.transform, new Vector3(0f, 0.058f, 0.11f), new Vector3(0.02f, 0.025f, 0.01f), coal);
            Box("Teeth", skull.transform, new Vector3(0f, 0.035f, 0.106f), new Vector3(0.07f, 0.015f, 0.008f), paper);
        }

        private static GameObject BuildBonesPile()
        {
            GameObject root = new GameObject("Bones_Pile");
            SkullModel(root.transform, new Vector3(0.05f, 0.04f, 0.05f), -25f);
            (Vector3 pos, Vector3 euler, float length)[] bones =
            {
                (new Vector3(-0.2f, 0.03f, -0.05f), new Vector3(0f, 30f, 90f), 0.45f),
                (new Vector3(0.15f, 0.03f, -0.18f), new Vector3(0f, -60f, 90f), 0.4f),
                (new Vector3(-0.05f, 0.07f, -0.12f), new Vector3(10f, 80f, 90f), 0.38f),
                (new Vector3(0.28f, 0.03f, 0.12f), new Vector3(0f, 10f, 90f), 0.3f),
                (new Vector3(-0.25f, 0.03f, 0.18f), new Vector3(0f, -20f, 90f), 0.28f),
                (new Vector3(0.02f, 0.03f, 0.25f), new Vector3(0f, 95f, 90f), 0.25f),
            };
            foreach (var (pos, euler, length) in bones)
            {
                GameObject bone = Empty("Bone", root.transform, pos, euler);
                Cylinder("Shaft", bone.transform, Vector3.zero, 0.022f, length, BoneMat());
                Sphere("Knuckle", bone.transform, new Vector3(0f, length * 0.5f, 0f), Vector3.one * 0.06f, BoneMat());
                Sphere("Knuckle", bone.transform, new Vector3(0f, -length * 0.5f, 0f), Vector3.one * 0.06f, BoneMat());
            }
            return root;
        }

        private static Material BoneMat() => bone;

        /// <summary>
        /// A corner cobweb. The origin is the corner it hangs from: threads spread out to the right (+X)
        /// and downwards (-Y), in a flat sheet facing +Z.
        /// </summary>
        private static GameObject BuildCobweb()
        {
            GameObject root = new GameObject("Cobweb");
            const float length = 0.9f;
            const int spokes = 6;
            for (int i = 0; i < spokes; i++)
            {
                float angle = -i * 90f / (spokes - 1);   // 0 = right, -90 = down
                Vector3 dir = Quaternion.Euler(0f, 0f, angle) * Vector3.right;
                Box("Thread", root.transform, dir * length * 0.5f, new Vector3(length, 0.006f, 0.006f), cobweb,
                    euler: new Vector3(0f, 0f, angle));
            }
            foreach (float r in new[] { 0.3f, 0.55f, 0.8f })
            {
                for (int i = 0; i < spokes - 1; i++)
                {
                    float a1 = -i * 90f / (spokes - 1) * Mathf.Deg2Rad;
                    float a2 = -(i + 1) * 90f / (spokes - 1) * Mathf.Deg2Rad;
                    // Each ring thread sags slightly towards the corner.
                    Vector3 p1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * r;
                    Vector3 p2 = new Vector3(Mathf.Cos(a2), Mathf.Sin(a2), 0f) * r;
                    Vector3 mid = (p1 + p2) * 0.5f * 0.95f;
                    float angle = Mathf.Atan2(p2.y - p1.y, p2.x - p1.x) * Mathf.Rad2Deg;
                    Box("Thread", root.transform, mid, new Vector3(Vector3.Distance(p1, p2), 0.005f, 0.005f), cobweb,
                        euler: new Vector3(0f, 0f, angle));
                }
            }
            return root;
        }

        /// <summary>A 2 x 2 m floor trap. Spikes pop up and down and send the player back to the start.</summary>
        private static GameObject BuildSpikeTrap()
        {
            GameObject root = new GameObject("SpikeTrap");
            AddBox(root, new Vector3(0f, 0.4f, 0f), new Vector3(1.8f, 0.8f, 1.8f), trigger: true);

            Box("Plate", root.transform, new Vector3(0f, 0.01f, 0f), new Vector3(2f, 0.02f, 2f), iron);
            Box("Plate Edge", root.transform, new Vector3(0f, 0.012f, 0.97f), new Vector3(2f, 0.024f, 0.06f), stoneDark);
            Box("Plate Edge", root.transform, new Vector3(0f, 0.012f, -0.97f), new Vector3(2f, 0.024f, 0.06f), stoneDark);
            Box("Plate Edge", root.transform, new Vector3(0.97f, 0.012f, 0f), new Vector3(0.06f, 0.024f, 2f), stoneDark);
            Box("Plate Edge", root.transform, new Vector3(-0.97f, 0.012f, 0f), new Vector3(0.06f, 0.024f, 2f), stoneDark);

            // While down, the spikes hide under the floor (inside the floor tile).
            GameObject spikes = Empty("Spikes", root.transform, new Vector3(0f, -0.5f, 0f));
            for (int x = 0; x < 4; x++)
            {
                for (int z = 0; z < 4; z++)
                {
                    Vector3 pos = new Vector3(-0.66f + x * 0.44f, 0f, -0.66f + z * 0.44f);
                    Box("Hole", root.transform, pos + new Vector3(0f, 0.021f, 0f), new Vector3(0.1f, 0.002f, 0.1f), coal);
                    Cylinder("Spike", spikes.transform, pos + new Vector3(0f, 0.2f, 0f), 0.025f, 0.4f, iron);
                    Box("Spike Tip", spikes.transform, pos + new Vector3(0f, 0.42f, 0f), new Vector3(0.05f, 0.05f, 0.05f), iron,
                        euler: new Vector3(45f, 0f, 45f));
                }
            }

            SpikeTrap trap = root.AddComponent<SpikeTrap>();
            trap.spikes = spikes.transform;
            trap.raiseHeight = 0.5f;
            return root;
        }

        /// <summary>A cloth banner hanging from an iron rod. Faces +Z, hang it on a wall.</summary>
        private static GameObject BuildBanner()
        {
            GameObject root = new GameObject("Banner_Wall");
            Cylinder("Rod", root.transform, new Vector3(0f, 0f, 0.06f), 0.025f, 1.1f, iron, Axis.X);
            foreach (float x in new[] { -0.57f, 0.57f })
                Sphere("Rod End", root.transform, new Vector3(x, 0f, 0.06f), Vector3.one * 0.06f, gold);
            Box("Cloth", root.transform, new Vector3(0f, -0.8f, 0.05f), new Vector3(0.9f, 1.55f, 0.02f), clothRed);
            Box("Tail", root.transform, new Vector3(-0.23f, -1.62f, 0.05f), new Vector3(0.32f, 0.32f, 0.02f), clothRed,
                euler: new Vector3(0f, 0f, 45f));
            Box("Tail", root.transform, new Vector3(0.23f, -1.62f, 0.05f), new Vector3(0.32f, 0.32f, 0.02f), clothRed,
                euler: new Vector3(0f, 0f, 45f));
            Box("Trim", root.transform, new Vector3(0f, -0.12f, 0.062f), new Vector3(0.9f, 0.05f, 0.005f), gold);
            Box("Emblem", root.transform, new Vector3(0f, -0.75f, 0.062f), new Vector3(0.36f, 0.36f, 0.005f), gold, euler: new Vector3(0f, 0f, 45f));
            Box("Emblem Inner", root.transform, new Vector3(0f, -0.75f, 0.066f), new Vector3(0.22f, 0.22f, 0.005f), clothRed, euler: new Vector3(0f, 0f, 45f));
            return root;
        }

        /// <summary>A heap of broken stone. The bigger chunks are solid.</summary>
        private static GameObject BuildRubble()
        {
            GameObject root = new GameObject("Rubble_Pile");
            (Vector3 pos, Vector3 size, Vector3 euler, bool solid)[] chunks =
            {
                (new Vector3(0f, 0.18f, 0f), new Vector3(0.6f, 0.4f, 0.5f), new Vector3(8f, 20f, 12f), true),
                (new Vector3(0.45f, 0.12f, 0.2f), new Vector3(0.4f, 0.26f, 0.35f), new Vector3(-10f, 50f, 6f), true),
                (new Vector3(-0.42f, 0.1f, 0.25f), new Vector3(0.34f, 0.22f, 0.3f), new Vector3(15f, -30f, -8f), false),
                (new Vector3(-0.2f, 0.08f, -0.4f), new Vector3(0.3f, 0.18f, 0.28f), new Vector3(0f, 70f, 20f), false),
                (new Vector3(0.3f, 0.06f, -0.38f), new Vector3(0.22f, 0.14f, 0.2f), new Vector3(30f, 10f, 0f), false),
                (new Vector3(0.65f, 0.05f, -0.1f), new Vector3(0.16f, 0.12f, 0.18f), new Vector3(0f, 40f, 30f), false),
                (new Vector3(0.05f, 0.42f, 0.05f), new Vector3(0.3f, 0.2f, 0.26f), new Vector3(-20f, 10f, 25f), false),
            };
            int i = 0;
            foreach (var (pos, size, euler, solid) in chunks)
                Box("Chunk", root.transform, pos, size, i++ % 2 == 0 ? stone : stoneDark, solid, euler);
            return root;
        }

        /// <summary>
        /// A carved stone sign. Its text faces +Z, so hang it on a wall like a torch.
        /// Change the words on the Title and Text children (TextMeshPro component).
        /// </summary>
        private static GameObject BuildSign()
        {
            GameObject root = new GameObject("Sign_StonePlaque");
            Plaque(root.transform, Vector3.zero, 1.6f, 0.9f, "Title", "Write your hint here.\nClick the Text child to change it.", true);
            return root;
        }

        /// <summary>
        /// Builds a stone plaque whose back sits on z = 0 and whose text faces +Z.
        /// With autoSize on, the text shrinks to fit (handy for the student prefab).
        /// </summary>
        private static GameObject Plaque(Transform parent, Vector3 position, float width, float height, string title, string body,
            bool autoSize = false)
        {
            GameObject plaqueObject = Empty("Plaque", parent, position);
            Box("Slab", plaqueObject.transform, new Vector3(0f, 0f, 0.04f), new Vector3(width, height, 0.08f), plaque);
            const float b = 0.06f;
            Box("Border", plaqueObject.transform, new Vector3(0f, height * 0.5f - b * 0.5f, 0.085f), new Vector3(width, b, 0.02f), stoneDark);
            Box("Border", plaqueObject.transform, new Vector3(0f, -height * 0.5f + b * 0.5f, 0.085f), new Vector3(width, b, 0.02f), stoneDark);
            Box("Border", plaqueObject.transform, new Vector3(width * 0.5f - b * 0.5f, 0f, 0.085f), new Vector3(b, height, 0.02f), stoneDark);
            Box("Border", plaqueObject.transform, new Vector3(-width * 0.5f + b * 0.5f, 0f, 0.085f), new Vector3(b, height, 0.02f), stoneDark);
            foreach (float x in new[] { -1f, 1f })
                foreach (float y in new[] { -1f, 1f })
                    Cylinder("Iron Bolt", plaqueObject.transform, new Vector3(x * (width * 0.5f - b * 0.5f), y * (height * 0.5f - b * 0.5f), 0.1f),
                        0.035f, 0.02f, iron, Axis.Z);

            float inner = width - 0.3f;
            float titleSize = Mathf.Min(0.3f, height * 0.3f);
            bool hasBody = !string.IsNullOrEmpty(body);
            float titleY = hasBody ? height * 0.5f - b - 0.06f - titleSize * 0.6f : 0f;
            TextMeshPro titleText = CreateText(plaqueObject.transform, "Title", title, new Vector3(0f, titleY, 0.088f), titleSize,
                new Vector2(inner, titleSize * 1.3f), true, CarvedGold);
            // Long titles shrink to fit on one line.
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 0.6f;
            titleText.fontSizeMax = titleSize * 10f;
            titleText.textWrappingMode = autoSize ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;

            if (hasBody)
            {
                float top = titleY - titleSize * 0.7f;
                float bottom = -height * 0.5f + b + 0.05f;
                TextMeshPro bodyText = CreateText(plaqueObject.transform, "Text", body, new Vector3(0f, (top + bottom) * 0.5f, 0.088f),
                    0.16f, new Vector2(inner, top - bottom), false, CarvedPale, TextAlignmentOptions.Top);
                bodyText.enableAutoSizing = true;
                bodyText.fontSizeMin = 0.8f;
                bodyText.fontSizeMax = 1.7f;
            }
            return plaqueObject;
        }
    }
}
