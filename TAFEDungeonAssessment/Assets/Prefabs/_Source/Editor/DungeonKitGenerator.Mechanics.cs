using System.Collections.Generic;
using UnityEngine;

namespace PuzzleMechanics.EditorTools
{
    public static partial class DungeonKitGenerator
    {
        // Conventions:
        //  - The root's origin is on the floor (or on the wall surface for wall-mounted pieces).
        //  - Wall-mounted pieces face +Z (the blue arrow), so put their back flat against a wall.
        //  - Doors fit the 2.4 m x 3 m opening of Wall_Stone_Doorway. Place both at the same position.

        // ---------------- Doors ----------------

        /// <summary>A wooden frame with two heavy plank doors and iron bands. The doors swing away from -Z.</summary>
        private static GameObject BuildDoorWooden()
        {
            GameObject root = new GameObject("Door_Wooden");
            PuzzleDoor door = root.AddComponent<PuzzleDoor>();
            DoorFrame(root.transform, out StateVisual runes);

            door.panels = new[]
            {
                new PuzzleDoor.SlidingPanel { panel = DoorLeaf(root.transform, -1f), openRotation = new Vector3(0f, -100f, 0f) },
                new PuzzleDoor.SlidingPanel { panel = DoorLeaf(root.transform, 1f), openRotation = new Vector3(0f, 100f, 0f) }
            };
            door.statusLights = runes;
            door.openDuration = 1.4f;
            return root;
        }

        /// <summary>Wooden posts and lintel around the opening, with a glowing rune on each face of the lintel.</summary>
        private static void DoorFrame(Transform root, out StateVisual runes)
        {
            Box("Post Left", root, new Vector3(-1.1f, 1.5f, 0f), new Vector3(0.2f, 3f, 0.36f), woodDark, collider: true);
            Box("Post Right", root, new Vector3(1.1f, 1.5f, 0f), new Vector3(0.2f, 3f, 0.36f), woodDark, collider: true);
            Box("Lintel", root, new Vector3(0f, 2.9f, 0f), new Vector3(2.4f, 0.2f, 0.36f), woodDark, collider: true);

            GameObject runeGroup = Empty("Status Runes", root, Vector3.zero);
            foreach (float z in new[] { -0.19f, 0.19f })
            {
                Box("Rune", runeGroup.transform, new Vector3(0f, 2.9f, z), new Vector3(0.12f, 0.12f, 0.02f), runeOff,
                    euler: new Vector3(0f, 0f, 45f));
                Box("Rune Dot", runeGroup.transform, new Vector3(-0.3f, 2.9f, z), new Vector3(0.06f, 0.06f, 0.02f), runeOff);
                Box("Rune Dot", runeGroup.transform, new Vector3(0.3f, 2.9f, z), new Vector3(0.06f, 0.06f, 0.02f), runeOff);
            }
            runes = AddStateVisual(runeGroup);
        }

        /// <summary>One door leaf on its hinge. side = -1 for the left leaf, +1 for the right leaf.</summary>
        private static Transform DoorLeaf(Transform door, float side)
        {
            // The leaf's pivot is the hinge, at the outer edge of the opening.
            GameObject hinge = Empty(side < 0 ? "Leaf Left (hinge)" : "Leaf Right (hinge)", door, new Vector3(1f * side, 0f, 0f));
            float inward = -side;   // direction from the hinge towards the middle of the door
            AddBox(hinge, new Vector3(0.5f * inward, 1.4f, 0f), new Vector3(1f, 2.8f, 0.1f));
            Rigidbody body = hinge.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            // Four vertical planks with thin gaps.
            for (int i = 0; i < 4; i++)
            {
                float x = (0.125f + i * 0.25f) * inward;
                Box("Plank", hinge.transform, new Vector3(x, 1.4f, 0f), new Vector3(0.24f, 2.8f, 0.1f), i % 2 == 0 ? wood : woodLight);
            }
            foreach (float y in new[] { 0.35f, 1.4f, 2.45f })
                Box("Iron Band", hinge.transform, new Vector3(0.5f * inward, y, 0f), new Vector3(1f, 0.09f, 0.13f), iron);
            Box("Hinge Strap", hinge.transform, new Vector3(0.05f * inward, 1.4f, 0f), new Vector3(0.1f, 2.6f, 0.12f), iron);

            // Ring handles on both faces, near the middle of the door.
            foreach (float z in new[] { -0.08f, 0.08f })
            {
                Cylinder("Handle Plate", hinge.transform, new Vector3(0.85f * inward, 1.15f, z), 0.06f, 0.02f, iron, Axis.Z);
                Cylinder("Ring Handle", hinge.transform, new Vector3(0.85f * inward, 1.03f, z * 1.25f), 0.07f, 0.02f, iron, Axis.Z);
            }
            return hinge.transform;
        }

        /// <summary>An iron grid that slides up into the ceiling.</summary>
        private static GameObject BuildDoorPortcullis()
        {
            GameObject root = new GameObject("Door_Portcullis");
            PuzzleDoor door = root.AddComponent<PuzzleDoor>();

            // Stone guide posts and lintel.
            Box("Guide Left", root.transform, new Vector3(-1.1f, 1.5f, 0f), new Vector3(0.2f, 3f, 0.36f), stoneDark, collider: true);
            Box("Guide Right", root.transform, new Vector3(1.1f, 1.5f, 0f), new Vector3(0.2f, 3f, 0.36f), stoneDark, collider: true);
            Box("Lintel", root.transform, new Vector3(0f, 2.9f, 0f), new Vector3(2.4f, 0.2f, 0.36f), stoneDark, collider: true);
            GameObject runeGroup = Empty("Status Runes", root.transform, Vector3.zero);
            foreach (float z in new[] { -0.19f, 0.19f })
            {
                foreach (float x in new[] { -1.1f, 1.1f })
                    Box("Rune", runeGroup.transform, new Vector3(x, 2.5f, z), new Vector3(0.1f, 0.1f, 0.02f), runeOff,
                        euler: new Vector3(0f, 0f, 45f));
                Box("Rune", runeGroup.transform, new Vector3(0f, 2.9f, z), new Vector3(0.14f, 0.14f, 0.02f), runeOff,
                    euler: new Vector3(0f, 0f, 45f));
            }

            // The moving grid.
            GameObject gate = Empty("Gate", root.transform, Vector3.zero);
            AddBox(gate, new Vector3(0f, 1.4f, 0f), new Vector3(2f, 2.8f, 0.1f));
            Rigidbody body = gate.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            for (int i = 0; i < 7; i++)
            {
                float x = -0.9f + i * 0.3f;
                Box("Bar", gate.transform, new Vector3(x, 1.45f, 0f), new Vector3(0.06f, 2.7f, 0.06f), iron);
                Box("Spike", gate.transform, new Vector3(x, 0.07f, 0f), new Vector3(0.07f, 0.07f, 0.07f), iron,
                    euler: new Vector3(0f, 0f, 45f));
            }
            foreach (float y in new[] { 0.45f, 1.15f, 1.85f, 2.55f })
                Box("Cross Bar", gate.transform, new Vector3(0f, y, 0f), new Vector3(2f, 0.07f, 0.09f), iron);

            door.panels = new[] { new PuzzleDoor.SlidingPanel { panel = gate.transform, openOffset = new Vector3(0f, 2.6f, 0f) } };
            door.statusLights = AddStateVisual(runeGroup);
            door.openDuration = 2f;
            return root;
        }

        /// <summary>A wooden door with a padlock. Bring the Key_Gold close and it opens.</summary>
        private static GameObject BuildDoorLocked()
        {
            GameObject root = BuildDoorWooden();
            root.name = "Door_Locked";
            PuzzleDoor door = root.GetComponent<PuzzleDoor>();
            door.stayOpenOnceOpened = true;

            // The padlock hangs across the middle of the door on both faces.
            GameObject padlock = Empty("Padlock", root.transform, Vector3.zero);
            foreach (float z in new[] { -0.1f, 0.1f })
            {
                float s = Mathf.Sign(z);
                Box("Hasp", padlock.transform, new Vector3(0f, 1.35f, z), new Vector3(0.36f, 0.06f, 0.03f), iron);
                Box("Lock Body", padlock.transform, new Vector3(0f, 1.2f, z + 0.03f * s), new Vector3(0.2f, 0.22f, 0.07f), gold);
                Box("Shackle", padlock.transform, new Vector3(0f, 1.34f, z + 0.03f * s), new Vector3(0.14f, 0.08f, 0.03f), iron);
                Box("Keyhole", padlock.transform, new Vector3(0f, 1.17f, z + 0.067f * s), new Vector3(0.03f, 0.07f, 0.01f), coal);
            }

            // The lock's trigger box covers both sides of the door, so the key works from either side.
            GameObject lockObject = Empty("Key Lock", root.transform, Vector3.zero);
            AddBox(lockObject, new Vector3(0f, 1.5f, 0f), new Vector3(2.6f, 3f, 3f), trigger: true);
            KeyLock keyLock = lockObject.AddComponent<KeyLock>();
            keyLock.keyName = "Gold Key";
            keyLock.hideWhenUnlocked = padlock;

            door.inputs = new List<PuzzleActivator> { keyLock };
            return root;
        }

        // ---------------- Pressure plates ----------------

        /// <summary>A square stone slab in a frame. The slab sinks when weighed down (10 kg by default).</summary>
        private static GameObject BuildPressurePlate()
        {
            return PressurePlateModel("PressurePlate_Stone", 1.6f, stoneDark, 10f, true, null);
        }

        /// <summary>A bigger plate with an iron frame that needs 100 kg. Players are too light.</summary>
        private static GameObject BuildPressurePlateHeavy()
        {
            return PressurePlateModel("PressurePlate_Heavy", 2f, iron, 100f, false, "100 kg");
        }

        private static GameObject PressurePlateModel(string name, float size, Material frameMat, float requiredMass,
            bool countPlayers, string carvedText)
        {
            GameObject root = new GameObject(name);

            // Trigger box that detects what is resting on the plate. The plate has no solid collider of its own:
            // it is only a few centimetres tall, so things simply rest on the floor underneath and can be
            // pushed on and off without catching on an edge.
            AddBox(root, new Vector3(0f, 0.5f, 0f), new Vector3(size - 0.1f, 1f, size - 0.1f), trigger: true);

            float half = size * 0.5f;
            const float border = 0.14f;
            const float height = 0.06f;
            Box("Frame", root.transform, new Vector3(0f, height * 0.5f, half - border * 0.5f), new Vector3(size, height, border), frameMat);
            Box("Frame", root.transform, new Vector3(0f, height * 0.5f, -half + border * 0.5f), new Vector3(size, height, border), frameMat);
            Box("Frame", root.transform, new Vector3(half - border * 0.5f, height * 0.5f, 0f), new Vector3(border, height, size - border * 2f), frameMat);
            Box("Frame", root.transform, new Vector3(-half + border * 0.5f, height * 0.5f, 0f), new Vector3(border, height, size - border * 2f), frameMat);

            // The moving slab, with a rune ring inlaid in it that glows when pressed.
            float inner = size - border * 2f - 0.03f;
            GameObject pad = Empty("Slab", root.transform, Vector3.zero);
            Box("Stone", pad.transform, new Vector3(0f, height * 0.5f, 0f), new Vector3(inner, height, inner), stone);
            GameObject runes = Empty("Runes", pad.transform, Vector3.zero);
            float r = inner * 0.32f;
            Box("Rune", runes.transform, new Vector3(0f, height + 0.003f, r), new Vector3(r * 2f, 0.008f, 0.05f), runeOff);
            Box("Rune", runes.transform, new Vector3(0f, height + 0.003f, -r), new Vector3(r * 2f, 0.008f, 0.05f), runeOff);
            Box("Rune", runes.transform, new Vector3(r, height + 0.003f, 0f), new Vector3(0.05f, 0.008f, r * 2f), runeOff);
            Box("Rune", runes.transform, new Vector3(-r, height + 0.003f, 0f), new Vector3(0.05f, 0.008f, r * 2f), runeOff);
            Box("Rune", runes.transform, new Vector3(0f, height + 0.003f, 0f), new Vector3(0.18f, 0.008f, 0.18f), runeOff,
                euler: new Vector3(0f, 45f, 0f));
            AddStateVisual(runes);

            if (carvedText != null)
            {
                foreach (float z in new[] { -1f, 1f })
                {
                    TMPro.TextMeshPro label = CreateText(pad.transform, "Carved Weight", carvedText,
                        new Vector3(0f, height + 0.006f, r * 0.55f * z), 0.16f, new Vector2(1f, 0.25f), true, CarvedGold);
                    label.transform.localRotation = Quaternion.Euler(90f, z > 0 ? 180f : 0f, 0f);
                }
            }

            PressurePlate plate = root.AddComponent<PressurePlate>();
            plate.pad = pad.transform;
            plate.requiredMass = requiredMass;
            plate.countPlayers = countPlayers;
            plate.pressDepth = 0.04f;
            return root;
        }

        // ---------------- Switches and levers ----------------

        /// <summary>A carved rune stone set into a wall. It pushes in when pressed. Faces +Z.</summary>
        private static GameObject BuildWallSwitch()
        {
            GameObject root = new GameObject("WallSwitch_Stone");
            AddBox(root, new Vector3(0f, 0f, 0.1f), new Vector3(0.56f, 0.56f, 0.2f));

            Box("Surround", root.transform, new Vector3(0f, 0f, 0.03f), new Vector3(0.56f, 0.56f, 0.06f), stoneDark);
            Box("Recess", root.transform, new Vector3(0f, 0f, 0.061f), new Vector3(0.4f, 0.4f, 0.004f), mortar);

            GameObject stoneBlock = Empty("Switch Stone", root.transform, new Vector3(0f, 0f, 0.12f));
            Box("Stone", stoneBlock.transform, Vector3.zero, new Vector3(0.34f, 0.34f, 0.12f), stoneLight);
            GameObject glyph = Empty("Rune", stoneBlock.transform, Vector3.zero);
            Box("Rune", glyph.transform, new Vector3(0f, 0f, 0.061f), new Vector3(0.04f, 0.22f, 0.006f), runeOff);
            Box("Rune", glyph.transform, new Vector3(0.05f, 0.05f, 0.061f), new Vector3(0.1f, 0.035f, 0.006f), runeOff,
                euler: new Vector3(0f, 0f, -35f));
            Box("Rune", glyph.transform, new Vector3(0.05f, -0.02f, 0.061f), new Vector3(0.1f, 0.035f, 0.006f), runeOff,
                euler: new Vector3(0f, 0f, -35f));
            AddStateVisual(glyph);

            WallButton button = root.AddComponent<WallButton>();
            button.buttonCap = stoneBlock.transform;
            button.pressDirection = Vector3.back;
            button.pressDepth = 0.07f;
            return root;
        }

        /// <summary>A waist-high stone pedestal with a rune button on top that pushes down.</summary>
        private static GameObject BuildPedestalButton()
        {
            GameObject root = new GameObject("Button_Pedestal");
            AddBox(root, new Vector3(0f, 0.53f, 0f), new Vector3(0.62f, 1.06f, 0.62f));

            Box("Foot", root.transform, new Vector3(0f, 0.06f, 0f), new Vector3(0.62f, 0.12f, 0.62f), stoneDark);
            Box("Column", root.transform, new Vector3(0f, 0.5f, 0f), new Vector3(0.4f, 0.76f, 0.4f), stone);
            Box("Top", root.transform, new Vector3(0f, 0.92f, 0f), new Vector3(0.58f, 0.08f, 0.58f), stoneDark);

            GameObject buttonStone = Empty("Button", root.transform, new Vector3(0f, 0.99f, 0f));
            Cylinder("Stone", buttonStone.transform, Vector3.zero, 0.18f, 0.07f, stoneLight);
            GameObject glyph = Empty("Rune", buttonStone.transform, Vector3.zero);
            Box("Rune", glyph.transform, new Vector3(0f, 0.037f, 0f), new Vector3(0.2f, 0.006f, 0.04f), runeOff,
                euler: new Vector3(0f, 45f, 0f));
            Box("Rune", glyph.transform, new Vector3(0f, 0.037f, 0f), new Vector3(0.2f, 0.006f, 0.04f), runeOff,
                euler: new Vector3(0f, -45f, 0f));
            AddStateVisual(glyph);

            WallButton button = root.AddComponent<WallButton>();
            button.buttonCap = buttonStone.transform;
            button.pressDirection = Vector3.down;
            button.pressDepth = 0.04f;
            return root;
        }

        /// <summary>An iron wall lever with a wooden handle. Up = off, down = on. Faces +Z.</summary>
        private static GameObject BuildLever()
        {
            GameObject root = new GameObject("Lever_Wall");
            AddBox(root, new Vector3(0f, 0f, 0.22f), new Vector3(0.4f, 1.1f, 0.44f));

            Box("Back Plate", root.transform, new Vector3(0f, 0f, 0.02f), new Vector3(0.32f, 0.7f, 0.04f), iron);
            foreach (float y in new[] { -0.3f, 0.3f })
                foreach (float x in new[] { -0.12f, 0.12f })
                    Cylinder("Bolt", root.transform, new Vector3(x, y, 0.045f), 0.02f, 0.015f, stoneDark, Axis.Z);
            Box("Bracket", root.transform, new Vector3(-0.07f, 0f, 0.08f), new Vector3(0.03f, 0.16f, 0.12f), iron);
            Box("Bracket", root.transform, new Vector3(0.07f, 0f, 0.08f), new Vector3(0.03f, 0.16f, 0.12f), iron);
            Cylinder("Axle", root.transform, new Vector3(0f, 0f, 0.1f), 0.025f, 0.18f, iron, Axis.X);

            GameObject gem = Box("Rune Gem", root.transform, new Vector3(0f, 0.24f, 0.05f), new Vector3(0.07f, 0.07f, 0.03f), runeOff,
                euler: new Vector3(0f, 0f, 45f));
            GameObject gem2 = Box("Rune Gem", root.transform, new Vector3(0f, -0.24f, 0.05f), new Vector3(0.07f, 0.07f, 0.03f), runeOff,
                euler: new Vector3(0f, 0f, 45f));

            Lever lever = root.AddComponent<Lever>();
            lever.offAngle = 25f;
            lever.onAngle = 155f;

            GameObject pivot = Empty("Handle Pivot", root.transform, new Vector3(0f, 0f, 0.1f), new Vector3(lever.offAngle, 0f, 0f));
            Cylinder("Iron Rod", pivot.transform, new Vector3(0f, 0.2f, 0f), 0.022f, 0.4f, iron);
            Cylinder("Wooden Handle", pivot.transform, new Vector3(0f, 0.47f, 0f), 0.045f, 0.2f, wood);
            Sphere("End Knob", pivot.transform, new Vector3(0f, 0.58f, 0f), Vector3.one * 0.08f, iron);

            AddStateVisual(gem, gem.GetComponent<Renderer>(), gem2.GetComponent<Renderer>());
            lever.handlePivot = pivot.transform;
            return root;
        }

        // ---------------- Gong ----------------

        /// <summary>A bronze gong hanging in a wooden frame. Throw something at it to ring it.</summary>
        private static GameObject BuildGong()
        {
            GameObject root = new GameObject("Target_Gong");
            // The hit collider must be on the same object as the ImpactTarget script.
            AddBox(root, new Vector3(0f, 1.45f, 0f), new Vector3(1.3f, 1.3f, 0.16f));

            foreach (float x in new[] { -0.95f, 0.95f })
            {
                Box("Post", root.transform, new Vector3(x, 1.25f, 0f), new Vector3(0.14f, 2.5f, 0.14f), woodDark, collider: true);
                Box("Foot", root.transform, new Vector3(x, 0.05f, 0f), new Vector3(0.22f, 0.1f, 0.7f), woodDark);
                Box("Brace", root.transform, new Vector3(x, 0.3f, 0.18f), new Vector3(0.08f, 0.5f, 0.08f), woodDark,
                    euler: new Vector3(-35f, 0f, 0f));
                Box("Brace", root.transform, new Vector3(x, 0.3f, -0.18f), new Vector3(0.08f, 0.5f, 0.08f), woodDark,
                    euler: new Vector3(35f, 0f, 0f));
            }
            Box("Cross Beam", root.transform, new Vector3(0f, 2.45f, 0f), new Vector3(2.2f, 0.16f, 0.16f), woodDark, collider: true);
            foreach (float x in new[] { -1.12f, 1.12f })
                Box("Beam End", root.transform, new Vector3(x, 2.45f, 0f), new Vector3(0.08f, 0.22f, 0.22f), iron);

            // The swinging part: chains and disc hang from a pivot under the beam.
            GameObject pivot = Empty("Gong Pivot", root.transform, new Vector3(0f, 2.37f, 0f));
            foreach (float x in new[] { -0.25f, 0.25f })
                Cylinder("Chain", pivot.transform, new Vector3(x, -0.15f, 0f), 0.015f, 0.32f, iron);
            Cylinder("Disc", pivot.transform, new Vector3(0f, -0.92f, 0f), 0.62f, 0.05f, bronze, Axis.Z);
            Cylinder("Rim", pivot.transform, new Vector3(0f, -0.92f, 0f), 0.64f, 0.035f, iron, Axis.Z);
            GameObject runeRing = Cylinder("Rune Ring", pivot.transform, new Vector3(0f, -0.92f, 0f), 0.42f, 0.058f, runeOff, Axis.Z);
            Cylinder("Inner Disc", pivot.transform, new Vector3(0f, -0.92f, 0f), 0.33f, 0.062f, bronze, Axis.Z);
            GameObject boss = Cylinder("Boss", pivot.transform, new Vector3(0f, -0.92f, 0f), 0.13f, 0.1f, runeOff, Axis.Z);
            AddStateVisual(pivot, runeRing.GetComponent<Renderer>(), boss.GetComponent<Renderer>());

            ImpactTarget target = root.AddComponent<ImpactTarget>();
            target.swingPart = pivot.transform;
            target.minImpactSpeed = 4f;
            return root;
        }

        // ---------------- Dispenser ----------------

        /// <summary>
        /// A stone hopper that hangs under a 4 m ceiling, with trapdoor flaps. Place the root on the floor:
        /// the chute is at the top and objects drop onto the marked floor stone below.
        /// </summary>
        private static GameObject BuildCeilingChute(GameObject boxPrefab)
        {
            GameObject root = new GameObject("Dispenser_CeilingChute");

            const float inner = 1.1f;
            const float wall = 0.15f;
            const float bottom = 3f;
            const float top = 4f;
            float centreY = (bottom + top) * 0.5f;
            float h = top - bottom;
            float offset = inner * 0.5f + wall * 0.5f;
            float outer = inner + wall * 2f;

            // Hopper walls have real colliders so falling objects are guided straight down.
            Box("Chute Wall", root.transform, new Vector3(0f, centreY, offset), new Vector3(outer, h, wall), stone, collider: true);
            Box("Chute Wall", root.transform, new Vector3(0f, centreY, -offset), new Vector3(outer, h, wall), stone, collider: true);
            Box("Chute Wall", root.transform, new Vector3(offset, centreY, 0f), new Vector3(wall, h, inner), stone, collider: true);
            Box("Chute Wall", root.transform, new Vector3(-offset, centreY, 0f), new Vector3(wall, h, inner), stone, collider: true);
            Box("Ceiling Frame", root.transform, new Vector3(0f, top - 0.06f, 0f), new Vector3(outer + 0.3f, 0.12f, outer + 0.3f), stoneDark);
            Box("Chute Top", root.transform, new Vector3(0f, top - 0.01f, 0f), new Vector3(inner, 0.02f, inner), mortar);
            foreach (float z in new[] { -1f, 1f })
                Box("Iron Band", root.transform, new Vector3(0f, bottom + 0.08f, (offset + 0.08f) * z), new Vector3(outer + 0.02f, 0.1f, 0.02f), iron);
            foreach (float x in new[] { -1f, 1f })
                Box("Iron Band", root.transform, new Vector3((offset + 0.08f) * x, bottom + 0.08f, 0f), new Vector3(0.02f, 0.1f, outer + 0.02f), iron);

            // Two wooden trapdoor flaps hinged on opposite edges. Each flap's pivot is its hinge.
            Transform[] flaps = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                GameObject hinge = Empty("Trapdoor Hinge", root.transform, new Vector3(0f, bottom - 0.02f, inner * 0.5f * side),
                    new Vector3(0f, side < 0 ? 0f : 180f, 0f));
                Box("Trapdoor", hinge.transform, new Vector3(0f, 0f, inner * 0.25f), new Vector3(inner, 0.05f, inner * 0.5f - 0.01f), wood);
                Box("Iron Strap", hinge.transform, new Vector3(0f, -0.03f, inner * 0.25f), new Vector3(0.08f, 0.012f, inner * 0.5f - 0.04f), iron);
                flaps[i] = hinge.transform;
            }

            // A cracked flagstone on the floor shows where things land.
            Box("Landing Stone", root.transform, new Vector3(0f, 0.004f, 0f), new Vector3(1.3f, 0.008f, 1.3f), stoneDark);
            Box("Landing Rune", root.transform, new Vector3(0f, 0.009f, 0f), new Vector3(0.4f, 0.004f, 0.4f), runeOff,
                euler: new Vector3(0f, 45f, 0f));

            GameObject spawn = Empty("Spawn Point", root.transform, new Vector3(0f, 3.5f, 0f));

            ObjectDispenser dispenser = root.AddComponent<ObjectDispenser>();
            dispenser.spawnPoint = spawn.transform;
            dispenser.objectPrefab = boxPrefab;
            dispenser.trapdoorFlaps = flaps;
            return root;
        }

        // ---------------- Rune circle (trigger zone) ----------------

        /// <summary>A glowing rune circle on the floor. It turns on while the player stands inside.</summary>
        private static GameObject BuildRuneCircle()
        {
            GameObject root = new GameObject("TriggerZone_RuneCircle");
            AddBox(root, new Vector3(0f, 1f, 0f), new Vector3(2.2f, 2f, 2.2f), trigger: true);

            Cylinder("Circle Stone", root.transform, new Vector3(0f, 0.004f, 0f), 1.2f, 0.008f, stoneDark);
            GameObject glow = Empty("Runes", root.transform, Vector3.zero);
            Ring("Outer Ring", glow.transform, new Vector3(0f, 0.012f, 0f), 1.1f, 28, 0.008f, 0.06f, runeOff);
            Ring("Inner Ring", glow.transform, new Vector3(0f, 0.012f, 0f), 0.75f, 20, 0.008f, 0.04f, runeOff);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f;
                Vector3 pos = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0.012f, 0.93f);
                Box("Rune Mark", glow.transform, pos, new Vector3(0.12f, 0.008f, 0.12f), runeOff, euler: new Vector3(0f, angle + 45f, 0f));
            }
            Box("Centre Rune", glow.transform, new Vector3(0f, 0.012f, 0f), new Vector3(0.3f, 0.008f, 0.3f), runeOff,
                euler: new Vector3(0f, 45f, 0f));
            AddStateVisual(glow);

            root.AddComponent<TriggerZone>();
            return root;
        }

        // ---------------- Runestones (logic) ----------------

        private static GameObject BuildRunestone(GateType type)
        {
            string label = type.ToString().ToUpperInvariant();
            GameObject root = new GameObject("Runestone_" + label);
            GameObject glyph = RunestoneBody(root, label);

            // A different rune shape for each kind, carved into the front (+Z) face.
            const float z = 0.175f;
            switch (type)
            {
                case GateType.And:
                    Box("Rune", glyph.transform, new Vector3(-0.07f, 0.85f, z), new Vector3(0.04f, 0.3f, 0.01f), runeOff);
                    Box("Rune", glyph.transform, new Vector3(0.07f, 0.85f, z), new Vector3(0.04f, 0.3f, 0.01f), runeOff);
                    Box("Rune", glyph.transform, new Vector3(0f, 0.85f, z), new Vector3(0.18f, 0.04f, 0.01f), runeOff);
                    break;
                case GateType.Or:
                    Box("Rune", glyph.transform, new Vector3(-0.05f, 0.85f, z), new Vector3(0.04f, 0.3f, 0.01f), runeOff,
                        euler: new Vector3(0f, 0f, -18f));
                    Box("Rune", glyph.transform, new Vector3(0.05f, 0.85f, z), new Vector3(0.04f, 0.3f, 0.01f), runeOff,
                        euler: new Vector3(0f, 0f, 18f));
                    break;
                default:
                    Box("Rune", glyph.transform, new Vector3(0f, 0.85f, z), new Vector3(0.04f, 0.3f, 0.01f), runeOff);
                    Box("Rune", glyph.transform, new Vector3(0f, 0.85f, z), new Vector3(0.04f, 0.26f, 0.01f), runeOff,
                        euler: new Vector3(0f, 0f, 50f));
                    break;
            }
            AddStateVisual(glyph);

            LogicGate gate = root.AddComponent<LogicGate>();
            gate.gateType = type;
            return root;
        }

        private static GameObject BuildRunestoneTimer()
        {
            GameObject root = new GameObject("Runestone_Timer");
            GameObject glyph = RunestoneBody(root, "TIMER");

            // An hourglass rune.
            const float z = 0.175f;
            Box("Rune", glyph.transform, new Vector3(0f, 0.98f, z), new Vector3(0.18f, 0.035f, 0.01f), runeOff);
            Box("Rune", glyph.transform, new Vector3(0f, 0.72f, z), new Vector3(0.18f, 0.035f, 0.01f), runeOff);
            Box("Rune", glyph.transform, new Vector3(0f, 0.85f, z), new Vector3(0.035f, 0.3f, 0.01f), runeOff,
                euler: new Vector3(0f, 0f, 30f));
            Box("Rune", glyph.transform, new Vector3(0f, 0.85f, z), new Vector3(0.035f, 0.3f, 0.01f), runeOff,
                euler: new Vector3(0f, 0f, -30f));
            AddStateVisual(glyph);

            root.AddComponent<TimerDelay>();
            return root;
        }

        /// <summary>The standing stone shared by all runestones. Returns the (empty) glyph group to fill.</summary>
        private static GameObject RunestoneBody(GameObject root, string label)
        {
            AddBox(root, new Vector3(0f, 0.65f, 0f), new Vector3(0.7f, 1.3f, 0.5f));
            Box("Base", root.transform, new Vector3(0f, 0.06f, 0f), new Vector3(0.8f, 0.12f, 0.6f), stoneDark);
            Box("Stone Lower", root.transform, new Vector3(0f, 0.42f, 0f), new Vector3(0.58f, 0.6f, 0.36f), stone);
            Box("Stone Upper", root.transform, new Vector3(0f, 0.92f, 0f), new Vector3(0.5f, 0.42f, 0.34f), stone);
            Box("Stone Top", root.transform, new Vector3(0f, 1.18f, 0f), new Vector3(0.38f, 0.14f, 0.3f), stone,
                euler: new Vector3(0f, 0f, 0f));
            Box("Chip", root.transform, new Vector3(0.2f, 1.1f, 0f), new Vector3(0.16f, 0.16f, 0.32f), stone,
                euler: new Vector3(0f, 0f, 45f));

            GameObject glyph = Empty("Rune", root.transform, Vector3.zero);
            Box("Crystal", glyph.transform, new Vector3(0f, 1.3f, 0f), new Vector3(0.1f, 0.1f, 0.1f), runeOff,
                euler: new Vector3(45f, 0f, 45f));

            CreateText(root.transform, "Carved Label", label, new Vector3(0f, 0.4f, 0.182f), 0.13f,
                new Vector2(0.56f, 0.2f), true, CarvedGold);
            return glyph;
        }

        // ---------------- Indicators ----------------

        /// <summary>A 1 m line of glowing runes set into the floor, along +Z. Stretch it with the Z scale.</summary>
        private static GameObject BuildFloorRunes()
        {
            GameObject root = new GameObject("Indicator_FloorRunes");
            Box("Groove", root.transform, new Vector3(0f, 0.004f, 0f), new Vector3(0.26f, 0.008f, 1f), mortar);
            GameObject line = Box("Rune Line", root.transform, new Vector3(0f, 0.01f, 0f), new Vector3(0.08f, 0.006f, 1f), runeOff);
            StateVisual visual = AddStateVisual(line);
            visual.onGlow = 3f;
            visual.offGlow = 0.4f;

            IndicatorLight light = root.AddComponent<IndicatorLight>();
            light.visual = visual;
            light.inputMode = InputMode.Any;
            return root;
        }

        /// <summary>A small stone brazier that bursts into a magic flame (and light) when its inputs are on.</summary>
        private static GameObject BuildIndicatorBrazier()
        {
            GameObject root = new GameObject("Indicator_Brazier");
            AddBox(root, new Vector3(0f, 0.5f, 0f), new Vector3(0.6f, 1f, 0.6f));

            Box("Foot", root.transform, new Vector3(0f, 0.05f, 0f), new Vector3(0.5f, 0.1f, 0.5f), stoneDark);
            Box("Column", root.transform, new Vector3(0f, 0.4f, 0f), new Vector3(0.26f, 0.6f, 0.26f), stone);
            Cylinder("Bowl", root.transform, new Vector3(0f, 0.8f, 0f), 0.3f, 0.2f, stoneDark);
            Cylinder("Bowl Lip", root.transform, new Vector3(0f, 0.91f, 0f), 0.33f, 0.04f, stone);
            GameObject runeBand = Cylinder("Rune Band", root.transform, new Vector3(0f, 0.8f, 0f), 0.305f, 0.05f, runeOff);
            Cylinder("Coals", root.transform, new Vector3(0f, 0.9f, 0f), 0.25f, 0.03f, coal);

            GameObject fire = MagicFlame(root.transform, new Vector3(0f, 0.95f, 0f), 1f, out Light glowLight);
            List<Renderer> glowing = new List<Renderer>(fire.GetComponentsInChildren<Renderer>()) { runeBand.GetComponent<Renderer>() };
            StateVisual visual = AddStateVisual(runeBand, glowing.ToArray());
            visual.onGlow = 3f;

            IndicatorLight light = root.AddComponent<IndicatorLight>();
            light.visual = visual;
            light.glowLight = glowLight;
            light.showWhenLit = fire;
            light.inputMode = InputMode.Any;
            return root;
        }

        /// <summary>An unlit wall torch that lights with a magic flame when its inputs are on. Faces +Z.</summary>
        private static GameObject BuildIndicatorTorch()
        {
            GameObject root = new GameObject("Indicator_Torch");
            TorchBracket(root.transform, out Vector3 flamePosition);
            Sphere("Charred Tip", root.transform, flamePosition + new Vector3(0f, -0.04f, 0f), new Vector3(0.09f, 0.06f, 0.09f), coal);
            GameObject fire = MagicFlame(root.transform, flamePosition, 0.7f, out Light glowLight);

            StateVisual visual = AddStateVisual(fire);
            IndicatorLight light = root.AddComponent<IndicatorLight>();
            light.visual = visual;
            light.glowLight = glowLight;
            light.showWhenLit = fire;
            light.inputMode = InputMode.Any;
            return root;
        }

        /// <summary>A green magic flame with its own light. Hidden until an indicator turns it on.</summary>
        private static GameObject MagicFlame(Transform parent, Vector3 position, float size, out Light light)
        {
            StateVisual defaults = new GameObject().AddComponent<StateVisual>();
            Color onColor = defaults.onColor;
            Object.DestroyImmediate(defaults.gameObject);

            GameObject fire = Empty("Magic Flame", parent, position);
            GameObject outer = Sphere("Flame", fire.transform, new Vector3(0f, 0.12f * size, 0f), new Vector3(0.2f, 0.32f, 0.2f) * size, runeOff);
            Sphere("Flame Tip", fire.transform, new Vector3(0f, 0.28f * size, 0f), new Vector3(0.1f, 0.18f, 0.1f) * size, runeOff);

            GameObject lightObject = Empty("Light", fire.transform, new Vector3(0f, 0.25f * size, 0f));
            light = AddFlameLight(lightObject, onColor, 500f * size, 6f);
            FlickeringLight flicker = lightObject.AddComponent<FlickeringLight>();
            flicker.flame = outer.transform;
            flicker.flickerAmount = 0.15f;

            // Unlit until the indicator's inputs turn on.
            fire.SetActive(false);
            light.enabled = false;
            return fire;
        }

        // ---------------- Magic barrier ----------------

        /// <summary>
        /// A shimmering magic barrier between two stone posts. Players walk through it, but pickups vanish.
        /// Fits the opening of Wall_Stone_Doorway.
        /// </summary>
        private static GameObject BuildMagicBarrier()
        {
            GameObject root = new GameObject("Barrier_Magic");
            AddBox(root, new Vector3(0f, 1.45f, 0f), new Vector3(1.9f, 2.9f, 0.4f), trigger: true);

            foreach (float side in new[] { -1f, 1f })
            {
                Box("Post", root.transform, new Vector3(1.05f * side, 1.5f, 0f), new Vector3(0.3f, 3f, 0.36f), stoneDark, collider: true);
                Box("Post Cap", root.transform, new Vector3(1.05f * side, 2.95f, 0f), new Vector3(0.38f, 0.1f, 0.42f), stone);
                for (int i = 0; i < 4; i++)
                    Box("Post Rune", root.transform, new Vector3(0.89f * side, 0.6f + i * 0.6f, 0f), new Vector3(0.02f, 0.22f, 0.12f), magic);
            }

            GameObject field = Empty("Field", root.transform, Vector3.zero);
            for (int i = 0; i < 9; i++)
                Box("Field Line", field.transform, new Vector3(0f, 0.25f + i * 0.31f, 0f), new Vector3(1.8f, 0.02f, 0.02f), magic);
            for (int i = 0; i < 5; i++)
                Box("Field Line", field.transform, new Vector3(-0.72f + i * 0.36f, 1.45f, 0f), new Vector3(0.012f, 2.8f, 0.012f), magic);

            Fizzler barrier = root.AddComponent<Fizzler>();
            barrier.fieldVisual = field;
            return root;
        }
    }
}
