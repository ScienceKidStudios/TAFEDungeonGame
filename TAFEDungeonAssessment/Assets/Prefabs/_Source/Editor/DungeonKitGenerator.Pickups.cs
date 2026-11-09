using UnityEngine;

namespace PuzzleMechanics.EditorTools
{
    public static partial class DungeonKitGenerator
    {
        // Every pickup: Rigidbody + Carryable on the root, colliders that match the outer shape,
        // and detail pieces as children with no colliders. The root's origin is the centre of the object,
        // so to sit it on the floor set its Y position to half its height.
        //
        // Weight classes (see PlayerCarrier): light and medium can be lifted, heavy can only be dragged.

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

        /// <summary>0.6 m, 10 kg (light). Planked box with a wooden frame and iron corner caps.</summary>
        private static GameObject BuildBoxSmall()
        {
            const float size = 0.6f;
            const float edge = 0.07f;
            GameObject root = PickupRoot("Box_Wooden_Small", 10f, "Small Wooden Box (10 kg, light)");
            AddBox(root, Vector3.zero, Vector3.one * size);

            Box("Planks", root.transform, Vector3.zero, Vector3.one * (size - 0.02f), wood);
            EdgeFrame(root.transform, size * 0.5f - edge * 0.5f, size - edge, edge, woodDark);
            foreach (Vector3 corner in Corners(size * 0.5f - 0.035f))
                Box("Iron Corner", root.transform, corner, Vector3.one * 0.085f, iron);

            // A plank line across the middle of each side.
            float face = size * 0.5f - 0.005f;
            Box("Plank Line", root.transform, new Vector3(0f, 0f, face), new Vector3(size - 0.1f, 0.015f, 0.012f), woodDark);
            Box("Plank Line", root.transform, new Vector3(0f, 0f, -face), new Vector3(size - 0.1f, 0.015f, 0.012f), woodDark);
            Box("Plank Line", root.transform, new Vector3(face, 0f, 0f), new Vector3(0.012f, 0.015f, size - 0.1f), woodDark);
            Box("Plank Line", root.transform, new Vector3(-face, 0f, 0f), new Vector3(0.012f, 0.015f, size - 0.1f), woodDark);

            FinishPickup(root);
            return root;
        }

        /// <summary>0.9 m, 40 kg (medium). Liftable but slow. Thick frame with diagonal braces.</summary>
        private static GameObject BuildCrateLarge()
        {
            const float size = 0.9f;
            const float edge = 0.11f;
            GameObject root = PickupRoot("Crate_Large", 40f, "Large Crate (40 kg, medium)");
            AddBox(root, Vector3.zero, Vector3.one * size);

            Box("Planks", root.transform, Vector3.zero, Vector3.one * (size - 0.04f), woodLight);
            EdgeFrame(root.transform, size * 0.5f - edge * 0.5f, size - edge, edge, wood);

            float face = size * 0.5f - 0.015f;
            Vector3 brace = new Vector3(0.95f, 0.1f, 0.03f);
            Box("Brace", root.transform, new Vector3(0f, 0f, face), brace, wood, euler: new Vector3(0f, 0f, 45f));
            Box("Brace", root.transform, new Vector3(0f, 0f, -face), brace, wood, euler: new Vector3(0f, 0f, -45f));
            Box("Brace", root.transform, new Vector3(face, 0f, 0f), brace, wood, euler: new Vector3(0f, 90f, 45f));
            Box("Brace", root.transform, new Vector3(-face, 0f, 0f), brace, wood, euler: new Vector3(0f, 90f, -45f));
            foreach (Vector3 corner in Corners(size * 0.5f - 0.05f))
                Box("Iron Corner", root.transform, corner, Vector3.one * 0.12f, iron);

            FinishPickup(root);
            return root;
        }

        /// <summary>1 m tall, 45 kg (medium). Round collider, so it rolls when tipped over.</summary>
        private static GameObject BuildBarrel()
        {
            GameObject root = PickupRoot("Barrel_Wooden", 45f, "Barrel (45 kg, medium, rolls)");

            // A convex cylinder collider on a scaled child, so the barrel can roll on its side.
            GameObject colliderObject = Empty("Collider", root.transform, Vector3.zero);
            colliderObject.transform.localScale = new Vector3(0.76f, 0.5f, 0.76f);
            MeshCollider meshCollider = colliderObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = BuiltinMesh(PrimitiveType.Cylinder);
            meshCollider.convex = true;

            Cylinder("Staves", root.transform, Vector3.zero, 0.36f, 0.98f, wood);
            Cylinder("Bulge", root.transform, Vector3.zero, 0.38f, 0.56f, wood);
            Cylinder("Lid", root.transform, new Vector3(0f, 0.49f, 0f), 0.33f, 0.02f, woodDark);
            Cylinder("Base", root.transform, new Vector3(0f, -0.49f, 0f), 0.33f, 0.02f, woodDark);
            foreach (float y in new[] { -0.4f, 0.4f })
                Cylinder("Iron Hoop", root.transform, new Vector3(0f, y, 0f), 0.368f, 0.06f, iron);
            foreach (float y in new[] { -0.14f, 0.14f })
                Cylinder("Iron Hoop", root.transform, new Vector3(0f, y, 0f), 0.388f, 0.05f, iron);

            FinishPickup(root);
            return root;
        }

        /// <summary>About 0.7 m tall, 6 kg (light). Round body, so it can roll.</summary>
        private static GameObject BuildPot()
        {
            GameObject root = PickupRoot("Pot_Clay", 6f, "Clay Pot (6 kg, light)");
            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 0.03f, 0f);
            capsule.radius = 0.25f;
            capsule.height = 0.64f;

            Sphere("Body", root.transform, Vector3.zero, new Vector3(0.5f, 0.56f, 0.5f), clay);
            Cylinder("Foot", root.transform, new Vector3(0f, -0.27f, 0f), 0.15f, 0.04f, clay);
            Cylinder("Neck", root.transform, new Vector3(0f, 0.3f, 0f), 0.11f, 0.14f, clay);
            Cylinder("Rim", root.transform, new Vector3(0f, 0.38f, 0f), 0.15f, 0.04f, clay);
            Cylinder("Painted Band", root.transform, new Vector3(0f, 0.06f, 0f), 0.252f, 0.05f, woodDark);

            FinishPickup(root);
            return root;
        }

        /// <summary>1.1 m, 120 kg (heavy). Too heavy to lift: push it or drag it.</summary>
        private static GameObject BuildBlockHeavy()
        {
            const float size = 1.1f;
            GameObject root = PickupRoot("Block_Stone_Heavy", 120f, "Heavy Stone Block (120 kg, heavy)");
            AddBox(root, Vector3.zero, Vector3.one * size);

            Box("Core", root.transform, Vector3.zero, Vector3.one * 1.04f, stoneDark);
            foreach (Vector3 corner in Corners(0.41f))
                Box("Corner Stone", root.transform, corner, Vector3.one * 0.28f, stone);
            Box("Iron Band", root.transform, Vector3.zero, new Vector3(1.08f, 0.14f, 1.08f), iron);

            // A carved rune on each side so it reads as something special and heavy.
            float d = 0.525f;
            Vector3[] sides = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
            foreach (Vector3 dir in sides)
            {
                Vector3 euler = Quaternion.LookRotation(dir).eulerAngles;
                Box("Carved Rune", root.transform, dir * d + new Vector3(0f, 0.3f, 0f), new Vector3(0.06f, 0.26f, 0.02f), mortar, euler: euler);
                Box("Carved Rune", root.transform, dir * d + new Vector3(0f, -0.3f, 0f), new Vector3(0.2f, 0.06f, 0.02f), mortar, euler: euler);
            }

            FinishPickup(root);
            return root;
        }

        /// <summary>0.36 m ball, 6 kg (light). Best for throwing at the gong.</summary>
        private static GameObject BuildBallIron()
        {
            GameObject root = PickupRoot("Ball_Iron", 6f, "Iron Ball (6 kg, throw it!)");
            SphereCollider sphere = root.AddComponent<SphereCollider>();
            sphere.radius = 0.18f;

            Sphere("Ball", root.transform, Vector3.zero, Vector3.one * 0.36f, iron);
            Cylinder("Band", root.transform, Vector3.zero, 0.183f, 0.03f, stoneDark);
            Cylinder("Band", root.transform, Vector3.zero, 0.183f, 0.03f, stoneDark, Axis.Z);

            FinishPickup(root);
            return root;
        }

        /// <summary>A big gold key (1 kg). Carry it to a Door_Locked to open it.</summary>
        private static GameObject BuildKey()
        {
            GameObject root = PickupRoot("Key_Gold", 1f, "Gold Key");
            AddBox(root, new Vector3(0.02f, 0f, 0f), new Vector3(0.5f, 0.05f, 0.18f));

            Ring("Bow", root.transform, new Vector3(-0.15f, 0f, 0f), 0.07f, 10, 0.04f, 0.035f, gold);
            Cylinder("Shaft", root.transform, new Vector3(0.06f, 0f, 0f), 0.02f, 0.34f, gold, Axis.X);
            Box("Bit", root.transform, new Vector3(0.2f, 0f, 0.045f), new Vector3(0.05f, 0.04f, 0.07f), gold);
            Box("Bit", root.transform, new Vector3(0.13f, 0f, 0.035f), new Vector3(0.035f, 0.04f, 0.05f), gold);

            root.AddComponent<KeyItem>().keyName = "Gold Key";
            FinishPickup(root);
            return root;
        }

        /// <summary>The mesh Unity uses for a primitive shape (handy for convex mesh colliders).</summary>
        private static Mesh BuiltinMesh(PrimitiveType type)
        {
            GameObject temp = GameObject.CreatePrimitive(type);
            Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temp);
            return mesh;
        }
    }
}
