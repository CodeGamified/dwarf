# Dwarf

A **CodeGamified** game where players write real code to manage a dwarf fortress — digging, building, crafting, defending, and surviving. Every decision that Dwarf Fortress buries in menus and hotkeys becomes a function call. Your dwarves are only as smart as your scripts.

## The Premise

You've struck the earth. A wagon, seven dwarves, and a hillside. Everything after that is code.

Instead of navigating menus to designate dig zones, assign labors, build workshops, and organize militias, you write Python scripts that run on the fortress's "Overseer Terminal." The engine compiles your code, executes it tick-by-tick against the simulation, and the fortress lives or dies by your logic.

**What would it look like to programmatically solve Dwarf Fortress?**

```python
# Embark spring, year 1
site = world.scan_embark(min_soil=3, has_river=True, has_flux=True)
fortress.embark(site, dwarves=7)

# Dig the entrance
z = fortress.surface_z()
entrance = dig.channel(x=40, y=40, z=z, width=3, depth=5)
dig.stairs(entrance.bottom, direction="down", levels=10)

# First workshop level
level = dig.floor_plan(z=z-6, blueprint="workshops_basic")
build.workshop("Carpenter", pos=level.slot(0))
build.workshop("Mason", pos=level.slot(1))
build.workshop("Craftsdwarf", pos=level.slot(2))

# Assign labors by aptitude
for dwarf in fortress.dwarves():
    best = dwarf.highest_skill()
    dwarf.enable_labor(best)
    dwarf.disable_all_except(best, "Hauling")

# Brew. Always brew.
orders.repeat("Brew Drink", workshop="Still", frequency="always")
```

That's the gameplay. Not clicking — *thinking in systems.*

## What Players Code

| System | What You Automate | Example Call |
|---|---|---|
| **Excavation** | Dig designations, floor plans, multi-z blueprints | `dig.room(z=-3, w=10, h=10)` |
| **Construction** | Walls, floors, bridges, mechanisms, traps | `build.bridge(pos, width=3, retract=True)` |
| **Labor** | Dwarf skill assignment, work priorities, schedules | `dwarf.set_labors(["Mining", "Masonry"])` |
| **Workshops** | Production queues, material preferences, repeating orders | `orders.queue("Craft Rock Mug", count=30)` |
| **Stockpiles** | Storage zones, material filters, hauling routes | `stockpile.create(zone, accept=["Food", "Drink"])` |
| **Military** | Squads, training schedules, patrol routes, siege response | `military.create_squad("Axes", dwarves=pick_by_skill("Axe"))` |
| **Hydraulics** | Water channels, pumps, pressure systems, magma forges | `water.channel(src=river, dst=cistern, floodgate=True)` |
| **Agriculture** | Farm plots, crop rotation, seasonal planting | `farm.plant(plot, crop="Plump Helmet", season="all")` |
| **Trade** | Caravan requests, export queues, broker instructions | `trade.offer(items=crafts.value_above(50))` |
| **Mood & Social** | Taverns, temples, need fulfillment, tantrum prevention | `rooms.assign_tavern(zone, keeper=brewer)` |

## How It Works

```
┌─────────────────────────────────────────────────────────┐
│  DWARF  (Dwarf Fortress meets code)                     │
│                                                         │
│  ICompilerExtension ─── dig.*, build.*, dwarf.*,        │
│                          military.*, trade.*, water.*    │
│  IGameIOHandler ─────── terrain reads, dwarf states,    │
│                          stockpile queries, threat scans │
│  TerminalWindow ─────── Overseer Terminal, Dwarf Log,   │
│                          Fortress Map, Stock Report      │
│  SimulationTime ─────── season ticks, caravan schedule,  │
│                          siege timing, day/night cycle   │
├─────────────────────────────────────────────────────────┤
│  .engine/ (CodeGamified shared submodules)               │
│                                                         │
│  CodeGamified.Engine    compiler + VM + bytecode         │
│  CodeGamified.TUI       terminal rendering + animation   │
│  CodeGamified.Time      simulation clock + time warp     │
└─────────────────────────────────────────────────────────┘
```

## Terminal Panels

The Overseer Terminal is your only interface. Multiple panels, all text, all code-driven:

| Panel | Shows |
|---|---|
| **Fortress Map** | ASCII cross-section of your fortress, z-level by z-level. Dwarves are `☺`, workshops `π`, water `≈`, magma `▓`. |
| **Dwarf Roster** | Name, skill, current task, mood, hunger/thirst. Color-coded by status. |
| **Task Queue** | Running scripts, pending orders, failed commands with error output. |
| **Stock Report** | Material counts, food/drink reserves, trade goods value. Progress bars for critical thresholds. |
| **Threat Scanner** | Goblin sieges, forgotten beasts, werebeast sightings. Distance and ETA. |
| **Event Log** | Births, deaths, moods, artifacts, caravan arrivals. Scrollable history. |

## The Challenge Progression

### Year 1 — Survive
```python
# Don't starve. Don't dehydrate. Don't get caught outside.
dig.basic_shelter(z=surface-1)
farm.create(pos, size=(3,3), crop="Plump Helmet")
orders.repeat("Brew Drink", workshop="Still")
```

### Year 2 — Industrialize
```python
# Smelt, forge, produce. Migrants are coming.
smelter = build.workshop("Smelter", pos=forge_level.slot(0))
forge = build.workshop("Metalsmith", pos=forge_level.slot(1))
orders.queue("Smelt Magnetite", workshop=smelter, repeat=True)
orders.queue("Forge Battle Axe", workshop=forge, count=10)

for migrant in fortress.new_arrivals():
    assign_by_need(migrant)
```

### Year 3 — Fortify
```python
# The goblins are coming.
military.create_squad("Iron Guard", size=5, weapon="Battle Axe")
military.set_training(squad="Iron Guard", months=[1,2,3,4,5,6,7,8,9,10,11,12])
build.drawbridge(entrance.top, width=3, linked_to=lever)

def on_siege(event):
    military.station("Iron Guard", pos=entrance.top)
    mechanism.pull(lever)  # raise the bridge
    fortress.alert("BURROW")

events.register("siege", on_siege)
```

### Year 4 — Megaproject
```python
# Glass tower? Magma moat? Automated trap hallway?
# You wrote the code to get here. Now build something legendary.
for z in range(surface, surface + 20):
    build.floor(material="Green Glass", z=z, footprint=tower_plan)
    build.wall(material="Green Glass", z=z, perimeter=tower_plan)
```

## Why Dwarf Fortress

Dwarf Fortress is already a programming problem — players just solve it through menus instead of code. The systems are deeply interrelated: water pressure affects flooding, which affects mood, which affects tantrums, which affects military readiness, which affects siege survival. Every Dwarf Fortress player eventually thinks in algorithms. This game just makes the algorithms explicit.

**DF mechanics that are naturally programmable:**
- **Excavation** is array manipulation on a 3D grid
- **Labor assignment** is a constraint satisfaction problem
- **Production chains** are dependency graphs (iron ore → iron bars → iron armor)
- **Military response** is event-driven programming
- **Hydraulic engineering** is fluid simulation + state machines
- **Fortress design** is spatial optimization under constraints
- **Mood/need management** is priority queue scheduling

If you can code it, you can fortress it.

## Quick Start

```bash
# Fork codegamified.github.io, then in your Unity project:
git submodule add https://github.com/CodeGamified/.engine.git Assets/.engine

# Implement the Dwarf interfaces:
# 1. ICompilerExtension  → register dig.*, build.*, dwarf.*, military.* builtins
# 2. IGameIOHandler      → execute terrain queries, dwarf state reads, build commands
# 3. TerminalWindow      → render Fortress Map, Dwarf Roster, Stock Report panels
#
# Strike the earth.
```

## Repo Structure

```
codegamified.github.io/
├── .engine/                     ← shared engine submodule
│   ├── CodeGamified.Engine/       compiler, VM, bytecode executor
│   ├── CodeGamified.TUI/          terminal UI framework
│   └── CodeGamified.Time/         simulation time + time warp
├── dwarf/                       ← you are here
│   ├── Assets/
│   │   ├── Engine/                engine submodules (local ref)
│   │   └── Scenes/
│   ├── Packages/
│   └── ProjectSettings/
└── ...
```

## License

Open-source under CodeGamified. See the [engine README](Dwarf/Assets/Engine/README.md) for submodule details.
