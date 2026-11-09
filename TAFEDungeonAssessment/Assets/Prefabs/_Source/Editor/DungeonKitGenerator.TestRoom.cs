using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PuzzleMechanics.EditorTools
{
    public static partial class DungeonKitGenerator
    {
        /// <summary>
        /// A single 16 x 16 m dungeon room for quickly testing the mechanics, with two small cells behind doors.
        /// The full tour of every prefab is the showcase scene.
        /// </summary>
        private static void BuildTestRoomScene(PrefabSet set)
        {
            kit = set;
            EnsureFolder("Assets/Bryce/Scenes");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BeginLevel();
            Transform r = new GameObject("Test Room").transform;

            Vector3 woodenDoorSpot = new Vector3(2f, 0f, 8f);
            Vector3 portcullisSpot = new Vector3(-6f, 0f, 8f);
            RoomShell(r, -8f, 8f, -8f, 8f, new[] { woodenDoorSpot, portcullisSpot });
            RoomShell(r, -8f, 8f, 8f, 12f, new[] { woodenDoorSpot, portcullisSpot });
            WallPiece(r, new Vector3(-2f, 0f, 10f), false);   // splits the space behind the doors into two cells

            GameObject sign = Plaque(r, new Vector3(-2f, 2.3f, 7.85f), 3.4f, 1.6f, "Mechanics Test Room",
                "A quick room for testing the puzzle pieces.\nThe full tour is in Assets/Prefabs/Showcase_Dungeon.");
            sign.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            TorchAt(r, new Vector3(-7.83f, 2.4f, 0f), 90f);
            TorchAt(r, new Vector3(7.83f, 2.4f, 0f), -90f);
            TorchAt(r, new Vector3(0.4f, 2.4f, 7.83f), 180f);
            TorchAt(r, new Vector3(-6f, 2.4f, -7.83f), 0f);
            TorchAt(r, new Vector3(6f, 2.4f, -7.83f), 0f);
            Place(kit.brazier, r, new Vector3(0f, 0f, -2f));

            // Plate -> wooden door
            PuzzleDoor woodenDoor = Place<PuzzleDoor>(kit.doorWooden, r, woodenDoorSpot);
            PressurePlate plate = Place<PressurePlate>(kit.plate, r, new Vector3(2f, 0f, 4.5f));
            Runes(r, new Vector3(2f, 0f, 5.35f), new Vector3(2f, 0f, 7.6f), plate);
            Wire(woodenDoor, InputMode.All, plate);

            // Lever -> portcullis
            PuzzleDoor portcullis = Place<PuzzleDoor>(kit.doorPortcullis, r, portcullisSpot);
            Lever lever = Place<Lever>(kit.lever, r, new Vector3(-7.83f, 1.3f, 5f), 90f);
            RunePath(r, new PuzzleActivator[] { lever }, new Vector3(-7.6f, 0f, 5f), new Vector3(-6f, 0f, 5f), new Vector3(-6f, 0f, 7.6f));
            Wire(portcullis, InputMode.All, lever);

            // Wall switch -> ceiling chute
            WallButton button = Place<WallButton>(kit.wallSwitch, r, new Vector3(7.83f, 1.3f, -4f), -90f);
            button.mode = ButtonMode.Timed;
            button.timedDuration = 1f;
            Dirty(button);
            ObjectDispenser chute = Place<ObjectDispenser>(kit.chute, r, new Vector3(5f, 0f, -4f));
            Wire(chute, InputMode.Any, button);
            Runes(r, new Vector3(7.6f, 0f, -4f), new Vector3(5.7f, 0f, -4f), button);

            // Gong -> brazier, and gong AND plate -> torch
            ImpactTarget gong = Place<ImpactTarget>(kit.gong, r, new Vector3(-5f, 0f, -1f), 90f);
            Place(kit.ballIron, r, new Vector3(-1.5f, 0.18f, -5f));
            Place(kit.ballIron, r, new Vector3(-1f, 0.18f, -5.5f));
            Wire(Place<IndicatorLight>(kit.indicatorBrazier, r, new Vector3(-5f, 0f, -3.5f)), InputMode.Any, gong);
            LogicGate and = PlaceGate(kit.gateAnd, r, new Vector3(4f, 0f, 1.5f), -90f, plate, gong);
            Wire(Place<IndicatorLight>(kit.indicatorTorch, r, new Vector3(7.83f, 2.4f, 3f), -90f), InputMode.Any, and);

            // Pickups to play with
            Place(kit.boxSmall, r, new Vector3(0f, 0.3f, 2f));
            Place(kit.crateLarge, r, new Vector3(-2f, 0.45f, 4f), 15f);
            Place(kit.blockHeavy, r, new Vector3(4f, 0.55f, -6.5f));
            Place(kit.chest, r, new Vector3(-6.5f, 0f, -6.5f), 45f);

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(kit.player);
            player.transform.SetPositionAndRotation(new Vector3(0f, 0.05f, -6f), Quaternion.identity);

            SetupDungeonLighting(scene, "Assets/Bryce/Scenes/MechanicsTestScene Volume Profile.asset",
                "Assets/Bryce/Scenes/MechanicsTestScene Lighting Settings.lighting");
            MarkLevelStatic();
            EditorSceneManager.SaveScene(scene, TestScenePath);
        }
    }
}
