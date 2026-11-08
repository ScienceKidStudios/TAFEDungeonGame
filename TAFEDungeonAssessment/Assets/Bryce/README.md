# Puzzle Mechanics Templates

Drag-and-drop puzzle pieces for a physics puzzle game (think Portal). Buttons, plates, targets and levers are **inputs**. Doors, dispensers, lights and fizzlers are **outputs**. Connect inputs to outputs by dragging them into the output's **Inputs** list in the Inspector. You don't need to write any code.

Every piece comes as a ready-made **graybox model** (built from Unity primitives) with its script already attached.

---

## Quick start

1. In Unity, click **Tools > Puzzle Mechanics > Generate Everything (Prefabs + Test Scene)**.
   This creates:
   - `Assets/Bryce/Materials/`: the graybox materials
   - `Assets/Bryce/Prefabs/Mechanics/`: plates, buttons, targets, levers, doors, gates, dispensers, lights...
   - `Assets/Bryce/Prefabs/Pickups/`: cube, crate, heavy block, ball
   - `Assets/Bryce/Prefabs/Environment/`: floor tile, wall panels, ledge, steps
   - `Assets/Bryce/Prefabs/TestPlayer.prefab`
   - `Assets/Bryce/Scenes/MechanicsTestScene.unity`: a playable test chamber
2. The test scene opens automatically. Press **Play**.
3. To use a piece in your own scene, drag its prefab from `Assets/Bryce/Prefabs/` into the scene.

You can run the menu item again whenever you like. Prefabs and materials are updated in place, so scenes that use them keep working. The test scene is rebuilt from scratch, so don't save your own work in it.

(**Tools > Puzzle Mechanics > Generate Prefabs Only** rebuilds only the prefabs and materials.)

### Test scene controls

| Key | Action |
|---|---|
| WASD / Mouse | Move / look |
| Space / Shift | Jump / sprint |
| **E** | Use buttons and levers |
| **Hold Left Mouse** | Pick up (or drag) an object. Let go to drop it |
| **Right Mouse** (while holding) | Throw |
| Esc | Free the mouse (click to capture it again) |

A crosshair and the controls are shown on screen. When you look at something, a prompt tells you what you can do with it.

### The test chamber

You start in a long corridor. Along one side are **11 test stations**, one per mechanic. Each has a label, the mechanic, the objects you need and a door it opens. At the far end is the **Final Puzzle**, which chains a dispenser, a target, a fizzler, a lever, a plate on a ledge and a heavy block on a heavy plate into an AND gate, then a delay, then the final door.

| # | Station | What to do |
|---|---|---|
| 1 | Pressure Plate | Put a cube on the 15 kg plate (or stand on it) |
| 2 | Mass Rule | Push the 120 kg block onto the 100 kg plate. You can't lift it |
| 3 | Wall Buttons | Toggle, momentary (hold E) and timed buttons |
| 4 | Impact Targets | Throw a ball at the targets |
| 5 | Lever | Flip the lever |
| 6 | Logic Gates | Two levers into AND, plus NOT and OR lamps |
| 7 | Timer / Delay | Pedestal button into a delay, then the door |
| 8 | Object Dispenser | Pedestal button drops a cube. Put it on the plate |
| 9 | Indicator Lights | Follow the floor lights. The door stays open once opened |
| 10 | Trigger Zone | Stand in the square |
| 11 | Fizzler | Turn the field off with the lever, then carry a cube through |
| 12 | Final Puzzle | Everything together |

---

## How it works (the big idea)

```
  INPUTS (PuzzleActivator)            OUTPUTS (PuzzleReceiver)
  ------------------------            ------------------------
  Pressure Plate  ─┐                  ┌─> Door
  Wall / Pedestal ─┤                  ├─> Object Dispenser
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

### Colours

| Colour | Meaning |
|---|---|
| **Blue** (dim glow) | Inactive / locked |
| **Green** (bright glow) | Active / unlocked |

The coloured parts of each model (button caps, plate tops, bullseyes, lever grips, door frame strips, floor lights) switch between these. Change them on the model's **StateVisual** component (**Off Color / On Color**).

### UnityEvents (no-code hooks)

Every input has **On Activated** and **On Deactivated** events at the top of its component. Click **+**, drag in any object and choose a function, such as playing a sound or calling `PuzzleDoor.Open`. Doors also have **On Opened / On Closed**, and dispensers have **On Dispensed**.

| Component | Functions you can call from events |
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

Every pickup is a Rigidbody, and its **mass** is its weight in kg. The test player (`PlayerCarrier`) uses these limits:

| Mass | What the player can do |
|---|---|
| up to **60 kg** (`Max Lift Mass`) | lift, carry and throw it. Heavier objects follow your view more slowly and fly more slowly |
| up to **150 kg** (`Max Drag Mass`) | too heavy to lift, but you can drag it along the floor or push it by walking into it |
| over 150 kg | too heavy to move |

### Pickups (`Prefabs/Pickups/`)

| Prefab | Mass | Size | Looks like |
|---|---|---|---|
| `Ball` | 5 kg | 0.4 m | Light sphere with two dark bands. Best for throwing at targets |
| `Cube_Light` | 15 kg | 0.6 m | Light cube with dark bevelled edges and face discs |
| `Crate_Medium` | 45 kg | 0.9 m | Mid-gray crate with a thick frame and diagonal braces. Liftable but slow |
| `Block_Heavy` | 120 kg | 1.2 m | Big dark block with chunky corners, marked **HEAVY**. Can't be lifted, only pushed or dragged |

Each pickup has one collider on its root that matches its outer size, a Rigidbody and a **Carryable** component. The detail pieces are children without colliders.

To make your own pickup, add a **Rigidbody** (set its mass) and a **Carryable** component to an object with a collider.

---

## Mechanics (`Prefabs/Mechanics/`)

All settings have tooltips. Hover over a setting in the Inspector to see what it does. Moving parts are always their own child object.

### PressurePlate (input)
A round base with a raised ring. The plate inside it sinks when pressed. It's ON while the total mass on it is at least **Required Mass**.

| Setting | Default | Notes |
|---|---|---|
| Required Mass | 15 kg | Total weight needed |
| Count Players | on | A player tagged `Player` counts as weight |
| Player Mass | 70 kg | Weight of a player with no Rigidbody (CharacterController) |
| Ignore Held Objects | on | A cube being carried doesn't press the plate (like Portal) |
| Pad / Press Depth / Pad Speed | | The moving plate and how far it sinks |

The detection area is the **trigger BoxCollider on the root**, shown as a yellow wire box in the Scene view. **Debug > Current Mass** shows the live weight in Play mode.

### WallButton and PedestalButton (input)
- **WallButton**: a backplate with a round button that pushes in. Its front faces the blue (+Z) arrow, so put the back flat against a wall.
- **PedestalButton**: a waist-high pillar with a big button on top that pushes down.

Both use the `WallButton` script. The only difference is **Press Direction**.

| Mode | Behaviour |
|---|---|
| Toggle | Each press flips it on or off |
| Momentary | ON only while the key is held |
| Timed | ON for **Timed Duration** seconds after a press |

### ImpactTarget (input)
A wall-mounted bullseye made of stacked rings. The centre lights up when something with a Rigidbody hits it faster than **Min Impact Speed** (default 5 m/s).
- **Min Impact Mass**: ignore light objects (0 = anything counts).
- **Stay Activated**: stays on forever, or turns off after **Reset After** seconds.
- **Log Impacts**: prints each hit's speed to the Console, which helps when tuning.

The target's front faces +Z. Keep its collider on the root object.

### Lever (input)
A floor housing with a handle that swings between OFF (towards you) and ON (away from you). The grip and the lamp on the front change colour. **Start On** sets its starting position. **Off Angle / On Angle** set how far it swings.

### TriggerZone (input)
A floor square that turns ON while the player (and/or a Carryable object) is inside the box. **Stay Activated** makes it a one-time trigger.

### LogicGate_AND / _OR / _NOT (logic)
Small labelled boxes with input and output connectors. The top panel lights up when the gate is ON.
- **AND**: ON when all inputs are on
- **OR**: ON when any input is on
- **NOT**: ON when no inputs are on. Give it one input to flip it.

Drag inputs into the gate's **Inputs** list, then use the gate as an input for something else. Avoid loops, where a gate feeds back into itself.

### TimerDelay (logic)
A labelled box with a clock face. It follows one **Input**, with a delay.
- **Activate Delay**: the input must stay on this long before the timer turns on.
- **Deactivate Delay**: the timer stays on this long after the input turns off.

### PuzzleDoor (output)
A frame (two posts and a header) with two panels that slide apart. The light strips on the frame turn green when the door is unlocked.

| Setting | Default | Notes |
|---|---|---|
| Inputs / Input Mode | | All = AND, Any = OR |
| Panels | 2 halves | Each panel has its own **Open Offset** (left -1 m, right +1 m) |
| Open Duration | 1 s | Smooth (eased) open and close time |
| Stay Open Once Opened | off | Once opened, never closes again |
| Start Open | off | |

The door fills the 2.4 m x 3 m opening of `WallPanel_Doorway`. Place both at the same position and the panels slide into the wall.

### ObjectDispenser (output)
A tube hanging from an overhead gantry. Objects appear inside the tube and drop out of the bottom.
- **Object Prefab**: what to drop (default `Cube_Light`). **Override Mass** sets its weight. Objects up to about 0.8 m across fit through the tube.
- **Replace Existing**: pressing again removes the old object and drops a fresh one.
- **Respawn When Lost**: if the object is destroyed (for example by a fizzler), falls below **Lost Below Y**, or goes further than **Max Distance**, a new one is dropped automatically.
- **Dispense On Start**: drop one when the game begins.

### IndicatorLight_FloorStrip and IndicatorLight_Lamp (output)
Lights that glow green when their inputs are on (blue when off).
- **FloorStrip**: a light set into a floor channel. Stretch it with its **Z scale** and use several to draw a path from a button to its door.
- **Lamp**: a small post with a round lamp. Put a row of them beside a path.

**Invert** makes a light glow when its inputs are *off*.

### Fizzler (output)
An energy field between two emitters. The player can walk through it, but any **Carryable** object that touches it is destroyed. By default its inputs turn it **off** (untick **Inputs Turn Field Off** to reverse that). With no inputs, it's always on. It fits the opening of `WallPanel_Doorway`. Pair it with a dispenser that has **Respawn When Lost** ticked.

---

## Environment (`Prefabs/Environment/`)

Room pieces on a **4 m grid**. The floor top is at y = 0.

| Prefab | Size | Notes |
|---|---|---|
| `FloorTile_4x4` | 4 x 4 m | A slab with a raised surface, so the seams show the grid |
| `WallPanel_4x4` | 4 m wide, 4 m tall, 0.3 m thick | Skirting, top cap and seams. Rotate 90° for walls along Z |
| `WallPanel_Doorway` | same footprint | 2.4 m x 3 m opening. Fits `PuzzleDoor` and `Fizzler` |
| `Platform_Ledge` | 4 x 4 m, 1 m high | A raised area |
| `Steps` | 2 m wide, rises 1 m over 1.6 m | Four 25 cm steps, climbing towards +Z. Meets the ledge |

Snap tip: hold **Ctrl** while moving to snap. Set the snap size to 4 for room pieces.

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

3. **Picking things up (optional).** If you use your own carry script, set `carryable.IsHeld = true` while holding an object and back to `false` when you drop it. That stops plates counting held cubes. You can also just add the **PlayerCarrier** component (plus **PlayerHUD** for the crosshair and prompts) to your player.

The pickups use the project's **Moveable** layer, so the team's existing `PhysicsGrabber` can pick them up too.

---

## Making your own mechanics

- **New input**: inherit from `PuzzleActivator` and call `SetActiveState(true/false)`. It then works with every door, gate and light automatically.
- **New output**: inherit from `PuzzleReceiver` and implement `OnInputsChanged(bool satisfied)`.
- **New model**: add a `StateVisual` to the part that should change colour.

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
| `Editor` | `PuzzleMechanicsGenerator`: the menu item that builds all the models, materials, prefabs and the test scene |

## Notes

- The project uses **HDRP** and the **new Input System**. Materials are created with `HDRP/Lit`, so nothing renders pink. In other pipelines, URP/Lit or Standard is used automatically.
- The palette: light gray for room surfaces, mid gray for mechanics, dark gray for heavy objects, blue for inactive and green (glowing) for active.
- If the test scene's labels or lighting look wrong, run the menu item again to rebuild it.
