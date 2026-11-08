# Puzzle Mechanics Templates

Drag-and-drop puzzle pieces for a physics puzzle game (think Portal). Buttons, plates, targets and levers are **inputs**. Doors, dispensers, lights and fizzlers are **outputs**. Connect inputs to outputs by dragging them into the output's **Inputs** list in the Inspector. You don't need to write any code.

---

## Quick start

1. In Unity, click **Tools > Puzzle Mechanics > Generate Prefabs and Test Scene**.
   This creates:
   - `Assets/Bryce/Materials/`: the materials
   - `Assets/Bryce/Prefabs/`: every puzzle prefab
   - `Assets/Bryce/Scenes/MechanicsTestScene.unity`: a playable test level
2. The test scene opens automatically. Press **Play**.
3. To use a mechanic in your own scene, drag its prefab from `Assets/Bryce/Prefabs/` into the scene.

You can run the menu item again whenever you like. Prefabs are updated in place, so scenes that use them keep working. The test scene is rebuilt from scratch, so don't save your own work in it.

(**Tools > Puzzle Mechanics > Generate Prefabs Only** rebuilds only the prefabs.)

### Test scene controls

| Key | Action |
|---|---|
| WASD / Mouse | Move / look |
| Space / Shift | Jump / sprint |
| **E** | Use buttons and levers |
| **Hold Left Mouse** | Pick up (or drag) a cube. Let go to drop it |
| **Right Mouse** (while holding) | Throw |
| Esc | Free the mouse (click to capture it again) |

The scene is a corridor of **test stations**, one per mechanic. Each station has a label and a door that the mechanic opens. At the far end is a **Final Puzzle** that chains several mechanics together.

---

## How it works (the big idea)

```
  INPUTS (PuzzleActivator)            OUTPUTS (PuzzleReceiver)
  ------------------------            ------------------------
  Pressure Plate  ─┐                  ┌─> Door
  Wall Button     ─┤                  ├─> Object Dispenser
  Impact Target   ─┼─> (Logic Gate) ──┼─> Indicator Light
  Lever           ─┤   (Timer Delay)  └─> Fizzler
  Trigger Zone    ─┘
```

- Every **input** is either ON or OFF.
- Every **output** has an **Inputs** list and an **Input Mode**:
  - **All**: every input must be ON (AND)
  - **Any**: one input ON is enough (OR)
- **Logic Gates** and **Timer Delays** are inputs *and* listen to other inputs, so you can chain them: `Plate -> Delay -> AND Gate -> Door`.

### Wiring two things together

1. Select the **output** (for example the door).
2. In its component, find **Inputs** and click **+**.
3. Drag the **input** (plate, button...) from the Hierarchy into the new slot.
4. Choose **Input Mode** (All / Any).

In the **Scene view**, lines are drawn from each input to the things it controls. They're red when the input is off and green when it's on (in Play mode).

### UnityEvents (no-code hooks)

Every input has **On Activated** and **On Deactivated** events at the top of its component. Click **+**, drag in any object and choose a function, such as playing a sound, enabling a light, or calling `PuzzleDoor.Open`. Doors also have **On Opened / On Closed**, and dispensers have **On Dispensed**.

Handy functions to call from events:

| Component | Functions |
|---|---|
| PuzzleDoor | `Open()`, `Close()`, `Toggle()` |
| ObjectDispenser | `Dispense()` |
| IndicatorLight | `SetLit(bool)` |
| Fizzler | `SetFieldActive(bool)` |
| WallButton | `Press()` |
| Lever | `Flip()` |
| ImpactTarget | `ResetTarget()` |

---

## The mass rule

Every cube is a Rigidbody, and its **mass** is its weight in kg. The test player (`PlayerCarrier`) uses these limits:

| Mass | What the player can do |
|---|---|
| up to **60 kg** (`Max Lift Mass`) | lift, carry and throw it |
| up to **150 kg** (`Max Drag Mass`) | drag it along the floor or push it by walking into it, but not lift it |
| over 150 kg | nothing. It's too heavy |

The included cubes:

| Prefab | Mass | Size |
|---|---|---|
| `Cube_Light` | 20 kg | 0.8 m |
| `Cube_Medium` | 100 kg | 1.0 m (drag only) |
| `Cube_Heavy` | 250 kg | 1.3 m (can't be moved) |

Thrown objects fly at `Throw Impulse ÷ mass`, so heavy things are thrown more slowly. Pressure plates add up the real Rigidbody masses.

To make your own carryable object, add a **Rigidbody** (set its mass) and a **Carryable** component.

---

## The prefabs

All settings have tooltips. Hover over a setting in the Inspector to see what it does.

### PressurePlate (input)
ON while the total mass resting on it is at least **Required Mass**.

| Setting | Default | Notes |
|---|---|---|
| Required Mass | 20 kg | Total weight needed |
| Count Players | on | A player tagged `Player` counts as weight |
| Player Mass | 70 kg | Weight of a player with no Rigidbody (CharacterController) |
| Ignore Held Objects | on | A cube being carried doesn't press the plate (like Portal) |
| Press Depth / Pad Speed | | How the pad moves |

The detection area is the **trigger BoxCollider on the root**, shown as a yellow wire box in the Scene view. The **Debug > Current Mass** field shows the live weight in Play mode.

### WallButton (input)
Press it with the interact key. The button's front faces its blue (+Z) axis, so place it flat against a wall.

| Mode | Behaviour |
|---|---|
| Toggle | Each press flips it on or off |
| Momentary | ON only while the key is held |
| Timed | ON for **Timed Duration** seconds after a press |

### ImpactTarget (input)
ON when something with a Rigidbody hits it faster than **Min Impact Speed** (default 5 m/s).
- **Min Impact Mass**: ignore light objects (0 = anything counts).
- **Stay Activated**: stays on forever, or turns off after **Reset After** seconds.
- **Log Impacts**: prints each hit's speed to the Console, which helps when tuning.

The target's front faces +Z. Its collider must stay on the root object.

### Lever (input)
Flips on and off with the interact key. **Start On** sets its starting position.

### TriggerZone (input, extra)
ON while the player (and/or a Carryable cube) is inside the box. **Stay Activated** makes it a one-time trigger.

### LogicGate_AND / _OR / _NOT (logic)
- **AND**: ON when all inputs are on
- **OR**: ON when any input is on
- **NOT**: ON when no inputs are on. Give it one input to flip it.

Drag inputs into the gate's **Inputs** list, then use the gate as an input for something else. Avoid loops, where a gate feeds back into itself.

### TimerDelay (logic)
Follows one **Input**, with a delay.
- **Activate Delay**: the input must stay on this long before the timer turns on.
- **Deactivate Delay**: the timer stays on this long after the input turns off.

### PuzzleDoor (output)
Slides open when its inputs are satisfied.

| Setting | Default | Notes |
|---|---|---|
| Inputs / Input Mode | | All = AND, Any = OR |
| Open Offset | (2.05, 0, 0) | Where the panel slides to (local space) |
| Open Duration | 1 s | Smooth (eased) open and close time |
| Stay Open Once Opened | off | Once opened, never closes again |
| Start Open | off | |

The doorway is 2 m wide and 2.5 m tall. The panel slides sideways into the wall, so place the door in a 2 m gap in a wall. Lights on the frame turn green when the door is open.

### ObjectDispenser (output)
Drops a new object each time its inputs become satisfied.
- **Object Prefab**: what to drop (default `Cube_Light`). **Override Mass** sets its weight.
- **Replace Existing**: pressing again removes the old object and drops a fresh one.
- **Respawn When Lost**: if the object is destroyed (for example by a fizzler), falls below **Lost Below Y**, or goes further than **Max Distance**, a new one is dropped automatically.
- **Dispense On Start**: drop one when the game begins.

### IndicatorLight_FloorStrip (output)
A strip on the floor that lights up (blue becomes orange) when its inputs are on. Use a few of them to draw a path from a button to the door it opens. Stretch a strip by changing its **Z scale**. **Invert** makes it light up when the inputs are *off*.

### Fizzler (output, extra)
An energy field the player can walk through, but any **Carryable** object that touches it is destroyed. By default its inputs turn it **off** (untick **Inputs Turn Field Off** to reverse that). With no inputs, it's always on. Pair it with a dispenser that has **Respawn When Lost** ticked.

### Cube_Light / Cube_Medium / Cube_Heavy
Rigidbody + **Carryable**. They use the project's `Moveable` layer, so the team's existing grab script can pick them up too.

### TestPlayer
A simple first-person player used by the test scene. You don't need it in your own scenes.

---

## Using your own player

The mechanics don't depend on any particular player script. Your player needs to do three things:

1. **Tag the player `Player`.** Pressure plates and trigger zones look for this tag.
2. **Interact with things.** Either:
   - add the **PlayerInteractor** component to your player (it only needs a camera), or
   - in your own script, raycast from the camera and call the `IInteractable` interface:

   ```csharp
   using PuzzleMechanics;

   // When the interact key is pressed:
   if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 3f))
   {
       IInteractable thing = hit.collider.GetComponentInParent<IInteractable>();
       if (thing != null)
           thing.Interact(gameObject);   // and call thing.EndInteract(gameObject) when the key is released
   }
   ```

   `EndInteract` is only needed for **Momentary** buttons. Everything else works with `Interact` alone.

3. **Picking things up (optional).** If you use your own carry script, set `carryable.IsHeld = true` while holding an object and back to `false` when you drop it. That stops plates counting held cubes. You can also just add the **PlayerCarrier** component to your player.

To make your own interactable object, write a script that implements `IInteractable` (see `WallButton.cs` or `Lever.cs`).

---

## Making your own mechanics

- **New input**: inherit from `PuzzleActivator` and call `SetActiveState(true/false)`. It then works with every door, gate and light automatically.
- **New output**: inherit from `PuzzleReceiver` and implement `OnInputsChanged(bool satisfied)`.

All scripts are in the `PuzzleMechanics` namespace, so add `using PuzzleMechanics;` at the top of your own scripts.

---

## Script overview

| Folder | Scripts |
|---|---|
| `Scripts/Core` | `PuzzleActivator` (input base), `PuzzleReceiver` (output base), `PuzzleLogic` (AND/OR helper + gizmos), `IInteractable`, `Carryable`, `StateVisual` (on/off colours) |
| `Scripts/Inputs` | `PressurePlate`, `WallButton`, `ImpactTarget`, `Lever`, `TriggerZone` |
| `Scripts/Logic` | `LogicGate`, `TimerDelay` |
| `Scripts/Outputs` | `PuzzleDoor`, `ObjectDispenser`, `IndicatorLight`, `Fizzler` |
| `Scripts/Player` | `TestPlayerController`, `PlayerCarrier`, `PlayerInteractor`, `PlayerHUD` |
| `Scripts/Editor` | `PuzzleMechanicsGenerator` (the menu item that builds the prefabs and test scene) |

## Notes

- The project uses **HDRP** and the **new Input System**. Materials are created with `HDRP/Lit` (URP/Lit or Standard is used automatically in other pipelines).
- On/off colours use a `StateVisual` component. Change **Off Color / On Color** on it to restyle any mechanic.
- If the test scene's labels or lighting look wrong, run the menu item again to rebuild it.
