# Puzzle Mechanics (Dungeon Kit)

The puzzle mechanics now live in the shared **Dungeon Kit** at `Assets/Prefabs/`, restyled for a dungeon.
Start with `Assets/Prefabs/READ_ME_FIRST.md`.

| What | Where |
|---|---|
| Student prefabs | `Assets/Prefabs/01_Puzzle_Mechanics` ... `05_Player` |
| Scripts (namespace `PuzzleMechanics`) | `Assets/Prefabs/_Source/Scripts` |
| Generator (menus, models, scenes) | `Assets/Prefabs/_Source/Editor/DungeonKitGenerator*.cs` |
| Materials, fonts, lighting | `Assets/Prefabs/_Source/Materials`, `Fonts`, `Lighting` |
| Showcase (every prefab, plus an example puzzle) | `Assets/Prefabs/Showcase_Dungeon.unity` |
| Small test room | `Assets/Bryce/Scenes/MechanicsTestScene.unity` |

## Regenerating

**Tools > Dungeon Kit > Generate Everything (Prefabs + Scenes)** rebuilds every material, font, prefab and both scenes.
Prefabs are updated in place, so scenes that use them keep working. Both scenes are rebuilt from scratch.
**Generate Prefabs Only** skips the scenes.

## How it works

- **Inputs** inherit from `PuzzleActivator` and are either on or off: `PressurePlate`, `WallButton`, `Lever`, `ImpactTarget`,
  `TriggerZone`, `KeyLock`, `TreasureChest`, plus the logic pieces `LogicGate` and `TimerDelay`.
- **Outputs** inherit from `PuzzleReceiver` and have an **Inputs** list with **All** / **Any**: `PuzzleDoor`,
  `ObjectDispenser`, `IndicatorLight`, `Fizzler` (the magic barrier).
- `StateVisual` switches the glow between blue (off) and green (on) using a MaterialPropertyBlock.
- `Carryable` marks pickups. Weight is the Rigidbody mass. `PlayerCarrier` lifts up to 60 kg and drags up to 150 kg.
- `PuzzleDoor` panels can slide (`openOffset`, the portcullis) and/or swing (`openRotation`, the wooden doors).
- `FlickeringLight` flickers torch lights and flames. `SpikeTrap` sends the player back to a respawn point.

To make a new input, inherit from `PuzzleActivator` and call `SetActiveState(true/false)`.
To make a new output, inherit from `PuzzleReceiver` and implement `OnInputsChanged(bool satisfied)`.

## Rendering notes

- The project uses HDRP. Materials are made with `HDRP/Lit` (URP/Lit or Standard in other pipelines), so nothing renders pink.
- Lighting is fully realtime: torch point lights without shadows, a fixed exposure, a dim gradient-sky fill, light fog,
  bloom and vignette (`_Source/Lighting`). No baking is needed.
- Fonts: Cinzel (headings) and Alegreya Sans (text), both SIL Open Font License, with static TextMesh Pro atlases.
