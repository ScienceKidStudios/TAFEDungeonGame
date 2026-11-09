using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PuzzleMechanics.EditorTools
{
    public static partial class DungeonKitGenerator
    {
        // Layout (4 m grid). A long hall runs along +X (z = -4 to 4). Rooms open off both sides of it,
        // 12 m wide and 12 m deep, each showing one mechanic or one group of props.
        // Rooms with a door have a small room behind it (4 m deep).
        // Every room is built in its own local space: the entrance is at z = 0 and the back wall at z = 12,
        // so things on the back wall face -Z (yaw 180) and floor props face the entrance with yaw 180.
        // The example puzzle room is at the far (east) end of the hall.

        private const float RoomWidth = 12f;
        private const int RoomsPerSide = 9;
        private const float BackWallZ = 11.83f;   // wall-mounted things on the back wall sit here

        private static Transform roomsRoot;

        private static void BuildShowcaseScene(PrefabSet set)
        {
            kit = set;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BeginLevel();
            roomsRoot = new GameObject("Rooms").transform;

            int n = 0;
            BuildPlateRoom(Room(n++, "1. Pressure Plate",
                "Put weight on the stone plate and the door opens.\nThe small box (10 kg) is heavy enough. So are you!",
                "PressurePlate_Stone, Box_Wooden_Small, Door_Wooden", true));
            BuildWeightRoom(Room(n++, "2. Light, Medium, Heavy",
                "This plate needs 100 kg and ignores players.\nThe stone block is too heavy to lift: push it or drag it on.",
                "PressurePlate_Heavy, Block_Stone_Heavy, Door_Portcullis", true));
            BuildSwitchRoom(Room(n++, "3. Wall Switches",
                "Look at a rune stone and press E.\nToggle stays on, Hold only works while you hold E, Timed turns off by itself.\nAny one switch opens the door.",
                "WallSwitch_Stone, Door_Wooden", true));
            BuildTimerRoom(Room(n++, "4. Timer Runestone",
                "Press the button. The timer waits 2 seconds, then opens the door.\nTurn the button off and the door closes 3 seconds later.",
                "Button_Pedestal, Runestone_Timer", true));
            BuildLeverRoom(Room(n++, "5. Wall Lever",
                "Pull the lever (E) to raise the portcullis.\nPull it again to drop it.",
                "Lever_Wall, Door_Portcullis", true));
            BuildGongRoom(Room(n++, "6. Gong Target",
                "Pick up an iron ball (hold Left Mouse) and throw it (Right Mouse) at the gong.\nA hard hit rings it and opens the door.",
                "Target_Gong, Ball_Iron", true));
            BuildLogicRoom(Room(n++, "7. Logic Runestones",
                "AND: both levers on. OR: either lever on. NOT: lever A off.\nThe AND stone opens the door. The braziers show OR and NOT.",
                "Runestone_AND, Runestone_OR, Runestone_NOT, Lever_Wall, Indicator_Brazier", true));
            BuildChuteRoom(Room(n++, "8. Ceiling Chute",
                "Press the button and a box drops from the chute.\nPut the box on the plate to open the door.",
                "Dispenser_CeilingChute, Button_Pedestal, PressurePlate_Stone", true));
            BuildIndicatorRoom(Room(n++, "9. Signal Lights",
                "Glowing floor runes, braziers and torches show what is switched on.\nPress the timed switch and follow the runes. This door stays open.",
                "Indicator_FloorRunes, Indicator_Brazier, Indicator_Torch", true));

            BuildCircleRoom(Room(n++, "10. Rune Circle",
                "Step into the rune circle to open the door.\nStep out and it closes again.",
                "TriggerZone_RuneCircle", true));
            BuildBarrierRoom(Room(n++, "11. Magic Barrier",
                "You can walk through the barrier, but boxes vanish in it.\nPull the lever to switch it off, then carry a box to the plate.",
                "Barrier_Magic, Dispenser_CeilingChute, Lever_Wall", true));
            BuildKeyRoom(Room(n++, "12. Key and Locked Door",
                "Pick up the gold key and carry it to the locked door.\nThe lock opens when the key gets close.",
                "Key_Gold, Door_Locked, Table_Wooden", true));
            BuildChestRoom(Room(n++, "13. Treasure Chest",
                "Press E to open the chest.\nA chest is an input too: opening this one opens the door.",
                "Chest_Treasure", true));
            BuildSpikeRoom(Room(n++, "14. Spike Trap",
                "Wait for the spikes to drop, then run across.\nTouch a raised spike and you are sent back. Reach the rune circle to open the door.",
                "SpikeTrap, TriggerZone_RuneCircle", true));
            BuildPickupsRoom(Room(n++, "15. Pickups",
                "Look at something and hold Left Mouse to pick it up. Right Mouse throws.\nLight and medium things can be carried. Heavy things can only be dragged.",
                "Everything in 02_Pickups", false));
            BuildFurnitureRoom(Room(n++, "16. Furniture and Treasure",
                "Tables, benches, shelves and chests to fill your rooms.",
                "Everything in 03_Props", false));
            BuildDecorationRoom(Room(n++, "17. Lights and Decoration",
                "Torches and braziers flicker and light the room.\nSkulls, bones, cobwebs, rubble and banners make it feel lived in (or died in).",
                "Torch_Wall, Brazier_Standing, Skull, Bones_Pile, Cobweb, Rubble_Pile, Banner_Wall, Sign_StonePlaque", false));
            BuildLevelPiecesRoom(Room(n++, "18. Level Building",
                "This whole dungeon is built from these pieces on a 4 m grid.\nHold Ctrl while moving a piece to snap it to the grid.",
                "Everything in 04_Level_Building", false));

            BuildHall();
            BuildExamplePuzzle();

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(kit.player);
            player.transform.SetPositionAndRotation(new Vector3(-3.5f, 0.05f, 0f), Quaternion.Euler(0f, 90f, 0f));

            SetupDungeonLighting(scene, LightingDir + "/Showcase_Dungeon Volume Profile.asset",
                LightingDir + "/Showcase_Dungeon Lighting Settings.lighting");
            MarkLevelStatic();
            EditorSceneManager.SaveScene(scene, ShowcaseScenePath);
        }

        // ---------------- Rooms ----------------

        /// <summary>
        /// Builds one room off the hall (walls, floor, ceiling, torches and the plaque) and returns its root.
        /// Rooms 0-8 are on the north side, 9-17 on the south side.
        /// </summary>
        private static Transform Room(int index, string title, string text, string prefabs, bool hasDoor)
        {
            bool north = index < RoomsPerSide;
            int column = index % RoomsPerSide;
            Transform room = new GameObject("Room " + title).transform;
            room.SetParent(roomsRoot, false);
            room.SetPositionAndRotation(new Vector3(column * RoomWidth, 0f, north ? 4f : -4f), Quaternion.Euler(0f, north ? 0f : 180f, 0f));

            RoomShell(room, -6f, 6f, 0f, 12f, hasDoor ? new[] { new Vector3(0f, 0f, 12f) } : null,
                new[] { new Vector3(-4f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(4f, 0f, 0f) });
            if (hasDoor)
                RoomShell(room, -6f, 6f, 12f, 16f, new[] { new Vector3(0f, 0f, 12f) });

            // Pillars where the room meets the hall.
            PillarAt(room.TransformPoint(new Vector3(-6f, 0f, 0f)));
            PillarAt(room.TransformPoint(new Vector3(6f, 0f, 0f)));

            // Torches either side of the door light the plaque and the door.
            TorchAt(room, new Vector3(-1.6f, 2.4f, BackWallZ), 180f);
            TorchAt(room, new Vector3(1.6f, 2.4f, BackWallZ), 180f);

            GameObject sign = Plaque(room, new Vector3(-4f, 2.25f, BackWallZ + 0.02f), 3.6f, 1.8f, title,
                text + "\n<size=80%><color=#C9B98F>Prefabs: " + prefabs + "</color></size>");
            sign.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            return room;
        }

        private static PuzzleDoor RoomDoor(Transform room, GameObject doorPrefab)
        {
            return Place<PuzzleDoor>(doorPrefab, room, new Vector3(0f, 0f, 12f));
        }

        private static void BuildPlateRoom(Transform r)
        {
            PuzzleDoor door = RoomDoor(r, kit.doorWooden);
            PressurePlate plate = Place<PressurePlate>(kit.plate, r, new Vector3(0f, 0f, 6f));
            Place(kit.boxSmall, r, new Vector3(-3f, 0.3f, 4f), 20f);
            Runes(r, new Vector3(0f, 0f, 6.85f), new Vector3(0f, 0f, 11.6f), plate);
            Wire(door, InputMode.All, plate);
        }

        private static void BuildWeightRoom(Transform r)
        {
            PuzzleDoor door = RoomDoor(r, kit.doorPortcullis);
            PressurePlate plate = Place<PressurePlate>(kit.plateHeavy, r, new Vector3(2.5f, 0f, 7.5f));
            Place(kit.blockHeavy, r, new Vector3(2.5f, 0.55f, 3.6f));
            Place(kit.boxSmall, r, new Vector3(-4.6f, 0.3f, 4f), 15f);
            Place(kit.crateLarge, r, new Vector3(-3f, 0.45f, 4f), -10f);
            Place(kit.barrel, r, new Vector3(-1.3f, 0.5f, 4f));
            FloorSign(r, "Light", "10 kg. Lift and throw.", new Vector3(-4.6f, 0f, 2.3f), 1.4f);
            FloorSign(r, "Medium", "40 kg. Lift (slowly).", new Vector3(-2.2f, 0f, 2.3f), 1.4f);
            FloorSign(r, "Heavy", "120 kg. Push or drag.", new Vector3(2.5f, 0f, 1.6f), 1.4f);
            RunePath(r, new PuzzleActivator[] { plate }, new Vector3(2.5f, 0f, 8.55f), new Vector3(2.5f, 0f, 10f),
                new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, 11.6f));
            Wire(door, InputMode.All, plate);
        }

        private static void BuildSwitchRoom(Transform r)
        {
            PuzzleDoor door = RoomDoor(r, kit.doorWooden);
            WallButton toggle = WallSwitchAt(r, new Vector3(2.6f, 1.3f, BackWallZ), 180f, ButtonMode.Toggle, 3f, "Toggle");
            WallButton hold = WallSwitchAt(r, new Vector3(3.9f, 1.3f, BackWallZ), 180f, ButtonMode.Momentary, 3f, "Hold");
            WallButton timed = WallSwitchAt(r, new Vector3(5.2f, 1.3f, BackWallZ), 180f, ButtonMode.Timed, 4f, "Timed");
            Runes(r, new Vector3(5.2f, 0f, 11.55f), new Vector3(1.35f, 0f, 11.55f), toggle, hold, timed);
            Wire(door, InputMode.Any, toggle, hold, timed);
        }

        private static WallButton WallSwitchAt(Transform r, Vector3 position, float yaw, ButtonMode mode, float seconds, string label)
        {
            WallButton button = Place<WallButton>(kit.wallSwitch, r, position, yaw);
            button.mode = mode;
            button.timedDuration = seconds;
            Dirty(button);
            WallLabel(r, label, position + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0.5f, 0f), yaw);
            return button;
        }

        private static void BuildTimerRoom(Transform r)
        {
            PuzzleDoor door = RoomDoor(r, kit.doorWooden);
            WallButton button = Place<WallButton>(kit.pedestalButton, r, new Vector3(-2f, 0f, 5f), 180f);
            TimerDelay timer = Place<TimerDelay>(kit.timer, r, new Vector3(1.5f, 0f, 8f), 180f);
            timer.input = button;
            timer.activateDelay = 2f;
            timer.deactivateDelay = 3f;
            Dirty(timer);
            RunePath(r, new PuzzleActivator[] { button }, new Vector3(-2f, 0f, 5.35f), new Vector3(-2f, 0f, 8f), new Vector3(1.05f, 0f, 8f));
            RunePath(r, new PuzzleActivator[] { timer }, new Vector3(1.5f, 0f, 8.35f), new Vector3(1.5f, 0f, 10f),
                new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, 11.6f));
            Wire(door, InputMode.All, timer);
        }

        private static void BuildLeverRoom(Transform r)
        {
            PuzzleDoor door = RoomDoor(r, kit.doorPortcullis);
            Lever lever = Place<Lever>(kit.lever, r, new Vector3(3.5f, 1.3f, BackWallZ), 180f);
            Runes(r, new Vector3(3.5f, 0f, 11.55f), new Vector3(1.35f, 0f, 11.55f), lever);
            Wire(door, InputMode.All, lever);
        }

        private static void BuildGongRoom(Transform r)
        {
            PuzzleDoor door = RoomDoor(r, kit.doorWooden);
            ImpactTarget gong = Place<ImpactTarget>(kit.gong, r, new Vector3(3f, 0f, 8f), 180f);
            Place(kit.ballIron, r, new Vector3(-1f, 0.18f, 3f));
            Place(kit.ballIron, r, new Vector3(0f, 0.18f, 3.4f));
            Place(kit.ballIron, r, new Vector3(1f, 0.18f, 3f));
            RunePath(r, new PuzzleActivator[] { gong }, new Vector3(3f, 0f, 8.45f), new Vector3(3f, 0f, 10f),
                new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, 11.6f));
            Wire(door, InputMode.All, gong);
        }

        private static void BuildLogicRoom(Transform r)
        {
            PuzzleDoor door = RoomDoor(r, kit.doorWooden);
            Lever leverA = Place<Lever>(kit.lever, r, new Vector3(-5.83f, 1.3f, 5f), 90f);
            Lever leverB = Place<Lever>(kit.lever, r, new Vector3(5.83f, 1.3f, 5f), -90f);
            WallLabel(r, "Lever A", new Vector3(-5.83f, 2.05f, 5f), 90f);
            WallLabel(r, "Lever B", new Vector3(5.83f, 2.05f, 5f), -90f);

            LogicGate and = PlaceGate(kit.gateAnd, r, new Vector3(0f, 0f, 8f), 180f, leverA, leverB);
            LogicGate not = PlaceGate(kit.gateNot, r, new Vector3(-3.5f, 0f, 8f), 180f, leverA);
            LogicGate or = PlaceGate(kit.gateOr, r, new Vector3(3.5f, 0f, 8f), 180f, leverA, leverB);
            Wire(Place<IndicatorLight>(kit.indicatorBrazier, r, new Vector3(-3.5f, 0f, 10.3f)), InputMode.Any, not);
            Wire(Place<IndicatorLight>(kit.indicatorBrazier, r, new Vector3(3.5f, 0f, 10.3f)), InputMode.Any, or);

            RunePath(r, new PuzzleActivator[] { leverA }, new Vector3(-5.6f, 0f, 5f), new Vector3(-0.3f, 0f, 5f), new Vector3(-0.3f, 0f, 7.65f));
            Runes(r, new Vector3(-3.5f, 0f, 5f), new Vector3(-3.5f, 0f, 7.65f), leverA);
            RunePath(r, new PuzzleActivator[] { leverB }, new Vector3(5.6f, 0f, 5f), new Vector3(0.3f, 0f, 5f), new Vector3(0.3f, 0f, 7.65f));
            Runes(r, new Vector3(3.5f, 0f, 5f), new Vector3(3.5f, 0f, 7.65f), leverB);
            Runes(r, new Vector3(0f, 0f, 8.35f), new Vector3(0f, 0f, 11.6f), and);
            Runes(r, new Vector3(-3.5f, 0f, 8.35f), new Vector3(-3.5f, 0f, 10f), not);
            Runes(r, new Vector3(3.5f, 0f, 8.35f), new Vector3(3.5f, 0f, 10f), or);
            Wire(door, InputMode.All, and);
        }

        private static void BuildChuteRoom(Transform r)
        {
            PuzzleDoor door = RoomDoor(r, kit.doorWooden);
            WallButton button = Place<WallButton>(kit.pedestalButton, r, new Vector3(-4.5f, 0f, 3f), 180f);
            button.mode = ButtonMode.Timed;
            button.timedDuration = 1f;
            Dirty(button);
            ObjectDispenser chute = Place<ObjectDispenser>(kit.chute, r, new Vector3(-2.5f, 0f, 8f));
            Wire(chute, InputMode.Any, button);
            PressurePlate plate = Place<PressurePlate>(kit.plate, r, new Vector3(2.5f, 0f, 8f));

            RunePath(r, new PuzzleActivator[] { button }, new Vector3(-4.5f, 0f, 3.35f), new Vector3(-4.5f, 0f, 8f), new Vector3(-3.2f, 0f, 8f));
            RunePath(r, new PuzzleActivator[] { plate }, new Vector3(2.5f, 0f, 8.85f), new Vector3(2.5f, 0f, 10f),
                new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, 11.6f));
            Wire(door, InputMode.All, plate);
        }

        private static void BuildIndicatorRoom(Transform r)
        {
            PuzzleDoor door = RoomDoor(r, kit.doorWooden);
            door.stayOpenOnceOpened = true;
            WallButton button = WallSwitchAt(r, new Vector3(4.2f, 1.3f, BackWallZ), 180f, ButtonMode.Timed, 4f, "Timed");

            RunePath(r, new PuzzleActivator[] { button }, new Vector3(4.2f, 0f, 11.55f), new Vector3(4.2f, 0f, 6f),
                new Vector3(-4f, 0f, 6f), new Vector3(-4f, 0f, 10f), new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, 11.6f));
            Wire(Place<IndicatorLight>(kit.indicatorBrazier, r, new Vector3(-2.3f, 0f, 11.1f)), InputMode.Any, button);
            Wire(Place<IndicatorLight>(kit.indicatorBrazier, r, new Vector3(2.3f, 0f, 11.1f)), InputMode.Any, button);
            Wire(Place<IndicatorLight>(kit.indicatorTorch, r, new Vector3(5.83f, 2.4f, 8f), -90f), InputMode.Any, button);
            Wire(Place<IndicatorLight>(kit.indicatorTorch, r, new Vector3(-5.83f, 2.4f, 8f), 90f), InputMode.Any, button);
            Wire(door, InputMode.All, button);
        }

        private static void BuildCircleRoom(Transform r)
        {
            PuzzleDoor door = RoomDoor(r, kit.doorWooden);
            TriggerZone circle = Place<TriggerZone>(kit.runeCircle, r, new Vector3(0f, 0f, 6f));
            Runes(r, new Vector3(0f, 0f, 7.2f), new Vector3(0f, 0f, 11.6f), circle);
            Wire(door, InputMode.All, circle);
        }

        private static void BuildBarrierRoom(Transform r)
        {
            PuzzleDoor door = RoomDoor(r, kit.doorWooden);

            // An inner wall across the room with the barrier in its doorway.
            WallPiece(r, new Vector3(-4f, 0f, 6f), true);
            WallPiece(r, new Vector3(0f, 0f, 6f), true, doorway: true);
            WallPiece(r, new Vector3(4f, 0f, 6f), true);
            Fizzler barrier = Place<Fizzler>(kit.barrier, r, new Vector3(0f, 0f, 6f));
            TorchAt(r, new Vector3(-1.6f, 2.4f, 5.83f), 180f);

            ObjectDispenser chute = Place<ObjectDispenser>(kit.chute, r, new Vector3(-3f, 0f, 3.5f));
            chute.dispenseOnStart = true;
            Dirty(chute);

            Lever lever = Place<Lever>(kit.lever, r, new Vector3(3.5f, 1.3f, 5.83f), 180f);
            WallLabel(r, "Barrier off", new Vector3(3.5f, 2.05f, 5.83f), 180f);
            Runes(r, new Vector3(3.5f, 0f, 5.55f), new Vector3(1.35f, 0f, 5.55f), lever);
            Wire(barrier, InputMode.All, lever);

            PressurePlate plate = Place<PressurePlate>(kit.plate, r, new Vector3(0f, 0f, 9f));
            Runes(r, new Vector3(0f, 0f, 9.85f), new Vector3(0f, 0f, 11.6f), plate);
            Wire(door, InputMode.All, plate);
        }

        private static void BuildKeyRoom(Transform r)
        {
            RoomDoor(r, kit.doorLocked);
            Place(kit.table, r, new Vector3(-3f, 0f, 5f));
            Place(kit.key, r, new Vector3(-3f, 0.83f, 5f), 30f);
            Place(kit.candles, r, new Vector3(-3.55f, 0.8f, 5.2f));
        }

        private static void BuildChestRoom(Transform r)
        {
            PuzzleDoor door = RoomDoor(r, kit.doorWooden);
            TreasureChest chest = Place<TreasureChest>(kit.chest, r, new Vector3(0f, 0f, 6f), 180f);
            Runes(r, new Vector3(0f, 0f, 6.45f), new Vector3(0f, 0f, 11.6f), chest);
            Wire(door, InputMode.All, chest);
        }

        private static void BuildSpikeRoom(Transform r)
        {
            PuzzleDoor door = RoomDoor(r, kit.doorWooden);
            GameObject respawn = Empty("Spike Respawn Point", r, new Vector3(0f, 0.05f, 2f));
            for (int i = 0; i < 6; i++)
            {
                SpikeTrap trap = Place<SpikeTrap>(kit.spikeTrap, r, new Vector3(-5f + i * 2f, 0f, 6f));
                trap.startDelay = i * 0.25f;
                trap.upTime = 1.2f;
                trap.downTime = 2.2f;
                trap.respawnPoint = respawn.transform;
                Dirty(trap);
            }
            TriggerZone circle = Place<TriggerZone>(kit.runeCircle, r, new Vector3(0f, 0f, 9.5f));
            Runes(r, new Vector3(0f, 0f, 10.7f), new Vector3(0f, 0f, 11.6f), circle);
            Wire(door, InputMode.All, circle);
        }

        private static void BuildPickupsRoom(Transform r)
        {
            (GameObject prefab, float y, string name, string text)[] items =
            {
                (kit.boxSmall, 0.3f, "Box_Wooden_Small", "Light, 10 kg"),
                (kit.pot, 0.29f, "Pot_Clay", "Light, 6 kg. Rolls"),
                (kit.crateLarge, 0.45f, "Crate_Large", "Medium, 40 kg"),
                (kit.barrel, 0.5f, "Barrel_Wooden", "Medium, 45 kg. Rolls"),
                (kit.blockHeavy, 0.55f, "Block_Stone_Heavy", "Heavy, 120 kg. Drag only"),
                (kit.ballIron, 0.18f, "Ball_Iron", "Light, 6 kg. For throwing"),
                (kit.key, 0.025f, "Key_Gold", "Opens Door_Locked"),
            };
            for (int i = 0; i < items.Length; i++)
            {
                float x = -4.8f + i * 1.6f;
                Place(items[i].prefab, r, new Vector3(x, items[i].y, 6.5f), 180f);
                FloorSign(r, items[i].name, items[i].text, new Vector3(x, 0f, 4.4f), 1.45f);
            }
            Place<PressurePlate>(kit.plate, r, new Vector3(0f, 0f, 9.5f));
            FloorSign(r, "Try the plate", "It needs 10 kg.", new Vector3(-2.2f, 0f, 9.5f), 1.6f);
        }

        private static void BuildFurnitureRoom(Transform r)
        {
            Place(kit.table, r, new Vector3(-4.2f, 0f, 7f));
            Place(kit.candles, r, new Vector3(-4.6f, 0.8f, 7.1f));
            Place(kit.bench, r, new Vector3(-4.2f, 0f, 5.9f));
            Place(kit.bench, r, new Vector3(-1.4f, 0f, 7f), 90f);
            Place(kit.chest, r, new Vector3(1.4f, 0f, 7f), 180f);
            Place(kit.bookshelf, r, new Vector3(4.3f, 0f, 7f), 180f);
            Place(kit.bookshelf, r, new Vector3(4.3f, 0f, 11.6f), 180f);
            Place(kit.barrel, r, new Vector3(5.2f, 0.5f, 9.6f));
            Place(kit.pot, r, new Vector3(-5.3f, 0.29f, 10.5f));
            Place(kit.crateLarge, r, new Vector3(-5.2f, 0.45f, 11.2f), 10f);

            FloorSign(r, "Table_Wooden", "With Candles on top.", new Vector3(-4.2f, 0f, 4.3f), 2.2f);
            FloorSign(r, "Bench_Wooden", "Sit down, adventurer.", new Vector3(-1.4f, 0f, 4.3f), 2.2f);
            FloorSign(r, "Chest_Treasure", "Press E to open it.", new Vector3(1.4f, 0f, 4.3f), 2.2f);
            FloorSign(r, "Bookshelf", "Full of dusty books.", new Vector3(4.3f, 0f, 4.3f), 2.2f);
        }

        private static void BuildDecorationRoom(Transform r)
        {
            Place(kit.brazier, r, new Vector3(-4f, 0f, 7f));
            Place(kit.skull, r, new Vector3(-1.6f, 0f, 7f), 200f);
            Place(kit.bones, r, new Vector3(0.6f, 0f, 7f), 160f);
            Place(kit.rubble, r, new Vector3(3.2f, 0f, 7f));
            Place(kit.candles, r, new Vector3(-1.6f, 0f, 9.5f));
            Place(kit.cobwebProp, r, new Vector3(5.8f, 3.95f, 11.8f), 180f);
            Place(kit.banner, r, new Vector3(5.83f, 3.5f, 8f), -90f);
            Place(kit.torch, r, new Vector3(5.83f, 2.4f, 4f), -90f);

            GameObject sign = Place(kit.sign, r, new Vector3(-5.83f, 1.9f, 7f), 90f);
            foreach (TMPro.TextMeshPro text in sign.GetComponentsInChildren<TMPro.TextMeshPro>())
            {
                text.text = text.name == "Title" ? "Sign_StonePlaque" : "Put me on a wall and change my words.\nClick the Title or Text child to edit.";
                Dirty(text);
            }

            FloorSign(r, "Brazier_Standing", "A big fire with light.", new Vector3(-4f, 0f, 4.4f), 1.9f);
            FloorSign(r, "Skull", "Also: Candles behind it.", new Vector3(-1.6f, 0f, 4.4f), 1.9f);
            FloorSign(r, "Bones_Pile", "Someone didn't make it.", new Vector3(0.6f, 0f, 4.4f), 1.9f);
            FloorSign(r, "Rubble_Pile", "Broken stone.", new Vector3(3.2f, 0f, 4.4f), 1.9f);
            WallLabel(r, "Torch_Wall", new Vector3(5.83f, 1.85f, 4f), -90f);
            WallLabel(r, "Banner_Wall", new Vector3(5.83f, 1.35f, 8f), -90f);
            WallLabel(r, "Cobweb (top corner)", new Vector3(5.83f, 3.2f, 10.6f), -90f, 0.11f, 1.6f);
        }

        private static void BuildLevelPiecesRoom(Transform r)
        {
            Place(kit.pillar, r, new Vector3(-4.5f, 0f, 9f));
            Place(kit.pillarBroken, r, new Vector3(-4.5f, 0f, 4.5f));
            Place(kit.archway, r, new Vector3(0f, 0f, 9f));
            Place(kit.platform, r, new Vector3(4f, 0f, 8f));
            Place(kit.stairs, r, new Vector3(4f, 0f, 4.4f));

            FloorSign(r, "Pillar_Stone", "Reaches the ceiling.", new Vector3(-2.6f, 0f, 9.6f), 1.6f);
            FloorSign(r, "Pillar_Broken", "With a fallen piece.", new Vector3(-2.6f, 0f, 3.2f), 1.6f);
            FloorSign(r, "Archway_Stone", "Walk through it.", new Vector3(0f, 0f, 6.6f), 1.6f);
            FloorSign(r, "Stairs + Platform", "Stairs_Stone climbs 1 m to Platform_Stone.", new Vector3(4f, 0f, 2.6f), 2.2f);
            FloorSign(r, "Walls, floors, ceilings",
                "Wall_Stone_4x4, Wall_Stone_Doorway,\nFloor_Stone_4x4, Ceiling_Stone_4x4", new Vector3(-0.4f, 0f, 2.6f), 2.6f);
        }

        // ---------------- Hall and entrance ----------------

        private static void BuildHall()
        {
            Transform world = new GameObject("Hall").transform;
            float endX = RoomsPerSide * RoomWidth - RoomWidth * 0.5f;   // east end of the hall

            for (float x = -4f; x < endX; x += 4f)
            {
                FloorSquare(world, new Vector3(x, 0f, -2f));
                FloorSquare(world, new Vector3(x, 0f, 2f));
            }
            WallPiece(world, new Vector3(-6f, 0f, -2f), false);
            WallPiece(world, new Vector3(-6f, 0f, 2f), false);

            // Torches on the pillars, alternating sides so the hall is evenly lit.
            for (int i = 1; i <= RoomsPerSide; i++)
            {
                float x = i * RoomWidth - RoomWidth * 0.5f;
                bool north = i % 2 == 0;
                Place(kit.torch, lightingRoot, new Vector3(x, 2.4f, north ? 3.63f : -3.63f), north ? 180f : 0f);
            }

            // The welcome plaque on the west wall, facing down the hall.
            GameObject welcome = Plaque(world, new Vector3(-5.83f, 2.2f, 0f), 4.6f, 2.1f, "The Dungeon Kit",
                "Walk down the hall. Each room shows a prefab you can use in your own level.\n" +
                "Read the plaque, then try it! The example puzzle is at the far end.\n" +
                "<size=85%><color=#C9B98F>WASD move - Space jump - Shift run - E use\n" +
                "Hold Left Mouse to carry - Right Mouse to throw - Esc frees the mouse</color></size>");
            welcome.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            Place(kit.torch, lightingRoot, new Vector3(-5.83f, 2.4f, 3.1f), 90f);
            Place(kit.torch, lightingRoot, new Vector3(-5.83f, 2.4f, -3.1f), 90f);
        }

        // ---------------- Example puzzle ----------------

        /// <summary>
        /// One puzzle that uses several mechanics together:
        ///   1. Press the button: a box drops from the chute. Carry it up the stairs to the plate on the platform.
        ///   2. Throw an iron ball at the gong.
        ///   3. Push the heavy stone block onto the 100 kg plate.
        /// All three feed an AND runestone, then a timer, then the portcullis. Behind it: treasure!
        /// </summary>
        private static void BuildExamplePuzzle()
        {
            // The room's local +Z points east, continuing the hall.
            float startX = RoomsPerSide * RoomWidth - RoomWidth * 0.5f;
            Transform r = new GameObject("Example Puzzle").transform;
            r.SetPositionAndRotation(new Vector3(startX, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));

            Vector3 portcullisSpot = new Vector3(2f, 0f, 20f);
            RoomShell(r, -12f, 12f, 0f, 20f, new[] { portcullisSpot }, new[] { new Vector3(-2f, 0f, 0f), new Vector3(2f, 0f, 0f) });
            RoomShell(r, -4f, 8f, 20f, 28f, new[] { portcullisSpot });

            // Plaque and lights
            GameObject sign = Plaque(r, new Vector3(-6f, 2.25f, 19.85f), 3.8f, 2.1f, "Example Puzzle",
                "1. Press the button for a box. Carry it up the stairs to the plate.\n" +
                "2. Throw an iron ball at the gong.\n" +
                "3. Push the stone block onto the 100 kg plate.\n" +
                "All three -> AND runestone -> timer -> portcullis. Treasure awaits!");
            sign.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            TorchAt(r, new Vector3(0.4f, 2.4f, 19.83f), 180f);
            TorchAt(r, new Vector3(3.6f, 2.4f, 19.83f), 180f);
            TorchAt(r, new Vector3(-11.83f, 2.4f, 6f), 90f);
            TorchAt(r, new Vector3(-11.83f, 2.4f, 14f), 90f);
            TorchAt(r, new Vector3(11.83f, 2.4f, 6f), -90f);
            TorchAt(r, new Vector3(11.83f, 2.4f, 14f), -90f);
            TorchAt(r, new Vector3(-6f, 2.4f, 0.17f), 0f);
            TorchAt(r, new Vector3(6f, 2.4f, 0.17f), 0f);
            PillarAt(r.TransformPoint(new Vector3(-4f, 0f, 0f)));
            PillarAt(r.TransformPoint(new Vector3(4f, 0f, 0f)));

            // Part 1: button -> chute, box -> plate on the platform
            WallButton button = Place<WallButton>(kit.pedestalButton, r, new Vector3(-9f, 0f, 3.5f), 180f);
            button.mode = ButtonMode.Timed;
            button.timedDuration = 1f;
            Dirty(button);
            ObjectDispenser chute = Place<ObjectDispenser>(kit.chute, r, new Vector3(-6f, 0f, 4.5f));
            Wire(chute, InputMode.Any, button);
            RunePath(r, new PuzzleActivator[] { button }, new Vector3(-9f, 0f, 3.85f), new Vector3(-9f, 0f, 4.5f), new Vector3(-6.7f, 0f, 4.5f));

            Place(kit.platform, r, new Vector3(-8f, 0f, 14f));
            Place(kit.stairs, r, new Vector3(-8f, 0f, 10.4f));
            PressurePlate ledgePlate = Place<PressurePlate>(kit.plate, r, new Vector3(-8.4f, 1f, 14.4f));
            Wire(Place<IndicatorLight>(kit.indicatorBrazier, r, new Vector3(-9.5f, 1f, 12.6f)), InputMode.Any, ledgePlate);

            // Part 2: gong
            ImpactTarget gong = Place<ImpactTarget>(kit.gong, r, new Vector3(8f, 0f, 9f), 180f);
            Place(kit.ballIron, r, new Vector3(5f, 0.18f, 3f));
            Place(kit.ballIron, r, new Vector3(6f, 0.18f, 3.4f));
            Place(kit.ballIron, r, new Vector3(7f, 0.18f, 3f));
            Wire(Place<IndicatorLight>(kit.indicatorBrazier, r, new Vector3(10.2f, 0f, 9f)), InputMode.Any, gong);

            // Part 3: heavy block -> heavy plate
            PressurePlate heavyPlate = Place<PressurePlate>(kit.plateHeavy, r, new Vector3(6f, 0f, 15f));
            Place(kit.blockHeavy, r, new Vector3(9.5f, 0.55f, 13f));
            Wire(Place<IndicatorLight>(kit.indicatorBrazier, r, new Vector3(6f, 0f, 17.2f)), InputMode.Any, heavyPlate);

            // Logic: AND -> timer -> portcullis
            LogicGate and = PlaceGate(kit.gateAnd, r, new Vector3(0f, 0f, 15f), 180f, ledgePlate, gong, heavyPlate);
            TimerDelay timer = Place<TimerDelay>(kit.timer, r, new Vector3(2f, 0f, 17.5f), 180f);
            timer.input = and;
            timer.activateDelay = 1f;
            timer.deactivateDelay = 0f;
            Dirty(timer);
            PuzzleDoor portcullis = Place<PuzzleDoor>(kit.doorPortcullis, r, portcullisSpot);
            portcullis.stayOpenOnceOpened = true;
            Wire(portcullis, InputMode.All, timer);

            RunePath(r, new PuzzleActivator[] { ledgePlate }, new Vector3(-5.9f, 0f, 15f), new Vector3(-0.45f, 0f, 15f));
            RunePath(r, new PuzzleActivator[] { gong }, new Vector3(8f, 0f, 9.45f), new Vector3(8f, 0f, 11f),
                new Vector3(0f, 0f, 11f), new Vector3(0f, 0f, 14.65f));
            RunePath(r, new PuzzleActivator[] { heavyPlate }, new Vector3(4.95f, 0f, 15f), new Vector3(0.45f, 0f, 15f));
            RunePath(r, new PuzzleActivator[] { and }, new Vector3(0f, 0f, 15.35f), new Vector3(0f, 0f, 17.5f), new Vector3(1.6f, 0f, 17.5f));
            Runes(r, new Vector3(2f, 0f, 17.85f), new Vector3(2f, 0f, 19.7f), timer);

            // Treasure room
            TreasureChest chest = Place<TreasureChest>(kit.chest, r, new Vector3(2f, 0f, 25.5f), 180f);
            Wire(Place<IndicatorLight>(kit.indicatorBrazier, r, new Vector3(-1.8f, 0f, 26.5f)), InputMode.Any, chest);
            Wire(Place<IndicatorLight>(kit.indicatorBrazier, r, new Vector3(5.8f, 0f, 26.5f)), InputMode.Any, chest);
            Place(kit.brazier, r, new Vector3(6.6f, 0f, 22.2f));
            Place(kit.bones, r, new Vector3(-2.5f, 0f, 22.5f), 40f);
            Place(kit.cobwebProp, r, new Vector3(7.8f, 3.95f, 27.8f), 180f);
            GameObject done = Plaque(r, new Vector3(2f, 2.4f, 27.85f), 3.2f, 1.3f, "Well Done!",
                "You solved the example puzzle.\nNow build your own!");
            done.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }
    }
}
