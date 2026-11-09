Hi everyone,

I apologise for not being present on the day, so I left a small gift which should hopefully help with your levels!

Have fun with it, and I can't wait to see what you build.

Bryce

---

# The Dungeon Kit

Everything in this folder is ready to use. Drag it into your scene and it works.

## What's in each folder

| Folder | What's inside |
|---|---|
| `01_Puzzle_Mechanics` | Doors, pressure plates, switches, levers, a gong, a ceiling chute, runestones and glowing signal lights. |
| `02_Pickups` | Things you can pick up, carry, throw or push. |
| `03_Props` | Torches, braziers, a treasure chest, furniture, a spike trap and decoration. |
| `04_Level_Building` | Walls, floors, ceilings, arches, pillars, stairs and platforms. |
| `05_Player` | A ready-made player, if you want one. |
| `_Source` | The behind-the-scenes files. You don't need to open this one. |

## Using a prefab in 3 steps

1. **Find it.** Open `Assets/Prefabs` and pick a numbered folder.
2. **Drag it in.** Drag the prefab into the Scene view or the Hierarchy.
3. **Adjust it.** Click it and look at the Inspector. Change any setting you like. Hover over a setting's name to see a tip.

A few handy tips:

- Walls, floors and ceilings fit together on a 4 metre grid.
- Hold **Ctrl** while you move something to snap it to the grid.
- The floor is at height 0. Ceilings go at height 4.
- Wall pieces like torches, switches and levers face the blue arrow. Put their back against a wall.

## Linking a lever, plate or switch to a door

1. Drag a door (`Door_Wooden` or `Door_Portcullis`) into your scene. Put a `Wall_Stone_Doorway` in the same spot so the door sits in the wall.
2. Drag in a lever, pressure plate or switch.
3. Click the **door** in the Hierarchy.
4. In the Inspector, find the **Inputs** list.
5. Click the small **+** button under the list.
6. Drag the lever (or plate, or switch) from the Hierarchy into the new empty slot.
7. Press **Play** and try it!

Want more than one thing to open the door? Add more to the list. Then pick an **Input Mode**:

- **All** means every one must be on.
- **Any** means just one is enough.

A few more things to know:

- This works for anything with an Inputs list. That includes the ceiling chute, braziers, floor runes and the magic barrier.
- **Runestones** combine things. Put the levers in the runestone's Inputs list. Then put the runestone in the door's Inputs list.
- The **timer runestone** follows one thing. Drag it into the timer's **Input** slot.
- Runes glow **blue** when off and **green** when on.
- In the Scene view, a line joins each input to the thing it controls. It's red when off and green when on (while playing).

## How weight works

Every pickup has a weight in kilograms. You can see it on its **Rigidbody** part, under **Mass**.

| Weight | What you can do | Pickups |
|---|---|---|
| **Light** | Pick it up and throw it | `Box_Wooden_Small` (10 kg), `Pot_Clay` (6 kg), `Ball_Iron` (6 kg), `Key_Gold` (1 kg) |
| **Medium** | Pick it up, but it moves slowly. Throws are short | `Crate_Large` (40 kg), `Barrel_Wooden` (45 kg) |
| **Heavy** | Too heavy to lift. Push it by walking into it, or hold Left Mouse to drag it | `Block_Stone_Heavy` (120 kg) |

Pressure plates check the weight on top of them:

- `PressurePlate_Stone` needs 10 kg. Any pickup works, and so does the player (who counts as 70 kg).
- `PressurePlate_Heavy` needs 100 kg and ignores the player. Only the heavy stone block will do.
- To change this, click the plate and change **Required Mass**.
- Things you are holding don't count. Put them down on the plate.

## Every prefab in one line

### 01_Puzzle_Mechanics

- `Door_Wooden` - Heavy wooden double doors with iron bands. They swing open.
- `Door_Portcullis` - An iron gate that slides up into the ceiling.
- `Door_Locked` - A wooden door with a padlock. Bring `Key_Gold` close to open it.
- `PressurePlate_Stone` - A stone plate that sinks when something heavy enough sits on it.
- `PressurePlate_Heavy` - A big plate that needs 100 kg. Players are too light.
- `WallSwitch_Stone` - A carved rune stone in the wall. Press E. Choose Toggle, Momentary (hold) or Timed.
- `Button_Pedestal` - A stone pedestal with a rune button on top. Works like the wall switch.
- `Lever_Wall` - An iron lever with a wooden handle. Press E to pull it up or down.
- `Target_Gong` - A bronze gong. Throw something at it hard to ring it.
- `Dispenser_CeilingChute` - A stone chute under the ceiling. It drops a pickup when its inputs turn on.
- `TriggerZone_RuneCircle` - A glowing circle on the floor. It turns on while the player stands in it.
- `Runestone_AND` - On when all of its inputs are on.
- `Runestone_OR` - On when any one of its inputs is on.
- `Runestone_NOT` - On when its input is off. It flips things around.
- `Runestone_Timer` - Copies one input, but waits a few seconds first.
- `Indicator_FloorRunes` - A glowing line on the floor. Stretch it to draw a path from a lever to a door.
- `Indicator_Brazier` - A small brazier that bursts into a green magic flame when its inputs are on.
- `Indicator_Torch` - A wall torch that lights with a magic flame when its inputs are on.
- `Barrier_Magic` - A shimmering barrier. Players walk through it, but pickups vanish.

### 02_Pickups

- `Box_Wooden_Small` - A light wooden box (10 kg).
- `Crate_Large` - A big crate (40 kg).
- `Barrel_Wooden` - A barrel (45 kg). It rolls if you tip it over.
- `Pot_Clay` - A light clay pot (6 kg). It rolls too.
- `Block_Stone_Heavy` - A heavy stone block (120 kg). Push it or drag it.
- `Ball_Iron` - An iron ball (6 kg). Great for throwing at the gong.
- `Key_Gold` - A big gold key. It opens `Door_Locked`.

### 03_Props

- `Chest_Treasure` - A treasure chest. Press E to open it. It can open a door too, like a lever.
- `Torch_Wall` - A burning wall torch with flickering light.
- `Brazier_Standing` - A tall fire bowl with a bright flickering light.
- `Candles` - A few candles. No light, so you can use lots of them.
- `Table_Wooden` - A wooden table.
- `Bench_Wooden` - A wooden bench.
- `Bookshelf` - A tall shelf full of books.
- `Skull` - A skull.
- `Bones_Pile` - A skull and a pile of bones.
- `Cobweb` - A cobweb for a corner. Turn it to fit.
- `Rubble_Pile` - A heap of broken stone.
- `Banner_Wall` - A red cloth banner for a wall.
- `SpikeTrap` - Spikes that pop up and down. They send the player back to the start (or to a spot you choose).
- `Sign_StonePlaque` - A stone sign. Click its Title or Text and change the words.

### 04_Level_Building

- `Wall_Stone_4x4` - A stone wall, 4 m wide and 4 m tall.
- `Wall_Stone_Doorway` - The same wall with a gap for a door.
- `Floor_Stone_4x4` - A 4 x 4 m floor of flagstones.
- `Ceiling_Stone_4x4` - A 4 x 4 m ceiling with a wooden beam. Put it at height 4.
- `Archway_Stone` - A free-standing stone arch.
- `Pillar_Stone` - A round pillar that reaches the ceiling.
- `Pillar_Broken` - A snapped pillar with a fallen piece.
- `Stairs_Stone` - Four steps that climb 1 metre.
- `Platform_Stone` - A 4 x 4 m raised platform, 1 metre high. The stairs fit it.

### 05_Player

- `Player_FirstPerson` - A first-person player that can walk, jump, use things, carry and throw.

## Opening and playing the showcase

1. Open `Assets/Prefabs` and double-click `Showcase_Dungeon`.
2. Press **Play**.
3. Walk down the hall. Each room has a stone plaque that tells you what to try.

| Key | What it does |
|---|---|
| W A S D | Walk |
| Mouse | Look around |
| Space | Jump |
| Shift | Run |
| E | Use levers, switches and chests |
| Hold Left Mouse | Pick up, carry or drag |
| Right Mouse | Throw what you are holding |
| Esc | Free the mouse (click to grab it again) |

The example puzzle is at the far end of the hall. It uses lots of pieces together.

## Using your own player

- Set your player's **Tag** to `Player` (at the top of the Inspector). Plates, rune circles and spike traps look for it.
- To let your player use levers, switches and chests, add the **Player Interactor** component to it.
- To let your player carry things, add the **Player Carrier** component as well.

## If something looks broken

Click **Tools > Dungeon Kit > Generate Everything (Prefabs + Scenes)** at the top of Unity. It rebuilds every prefab and the showcase. Your own scenes keep working. Don't save your own work in the showcase scene, because it gets rebuilt.
