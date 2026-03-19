// Copyright CodeGamified 2025-2026
// MIT License — Dwarf
using UnityEngine;
using CodeGamified.Engine;
using CodeGamified.Engine.Compiler;
using CodeGamified.Engine.Runtime;
using CodeGamified.Time;
using Dwarf.Game;

namespace Dwarf.Scripting
{
    /// <summary>
    /// ProgramBehaviour subclass — tick-based Overseer Terminal execution.
    ///
    /// EXECUTION MODEL (tick-based, deterministic):
    ///   - Each simulation tick (~20 ops/sec sim-time), the overseer script runs
    ///   - Memory (variables) persists across ticks
    ///   - PC resets to 0 each tick (on HALT)
    ///   - Results are IDENTICAL at 0.5x, 1x, 100x speed
    /// </summary>
    public class DwarfProgram : ProgramBehaviour
    {
        private DwarfMatchManager _match;
        private FortressGrid _fortress;
        private DwarfIOHandler _ioHandler;
        private DwarfCompilerExtension _compilerExt;

        public const float OPS_PER_SECOND = 20f;
        private float _opAccumulator;

        // ── Default starter code — survive year 1 ───────────────

        private const string DEFAULT_CODE = @"# DWARF — Write your Overseer scripts!
# Your script runs at 20 ops/sec (sim-time).
# When it finishes, it restarts from the top.
# Variables persist — track state across ticks.
#
# QUERIES — Read fortress state:
#   get_pop()          → total living dwarves
#   get_food()         → food stockpile
#   get_drink()        → drink stockpile
#   get_wood()         → wood stockpile
#   get_stone()        → stone stockpile
#   get_ore()          → ore stockpile
#   get_bars()         → metal bars
#   get_goods()        → trade goods value
#   get_grid_width()   → fortress grid width
#   get_grid_depth()   → fortress grid depth (z-levels)
#   get_cell(x, z)     → cell type at (x, z)
#   get_idle()         → idle dwarves count
#   get_threat()       → 0=none, 1=ambush, 2=siege
#   get_morale()       → dwarf happiness (0.0-1.0)
#   get_workshops()    → total workshop count
#   get_farms()        → farm plot count
#   get_military()     → military dwarves
#   get_season()       → 0=spring, 1=summer, 2=autumn, 3=winter
#   get_year()         → current year
#   get_score()        → fortress wealth score
#
# COMMANDS — Issue overseer orders:
#   dig(x, z)                → excavate rock (+1 stone, chance of ore)
#   build_stairs(x, z)       → place stairs (costs 1 stone)
#   build_farm(x, z)         → designate farm plot (free)
#   build_stockpile(x, z)    → designate stockpile (free)
#   build_wall(x, z)         → build wall (costs 1 stone)
#   build_workshop(x, z, t)  → place workshop (costs 2 stone)
#       types: 1=Carpenter, 2=Mason, 3=Smelter,
#              4=Forge, 5=Still, 6=Kitchen, 7=Craftsdwarf
#   brew()                   → 1 food → 2 drink (needs Still)
#   smelt()                  → 1 ore → 1 bar (needs Smelter)
#   forge()                  → 1 bar → 5 goods (needs Forge)
#   craft()                  → 1 stone → 3 goods (needs Craftsdwarf)
#   recruit()                → move idle dwarf to military
#   raise_bridge()           → seal entrance (blocks sieges)
#   lower_bridge()           → open entrance
#
# CELL TYPES: 0=solid, 1=open, 2=surface, 3=stairs,
#   4=workshop, 5=farm, 6=stockpile, 7=wall, 8=entrance
#
# This starter digs out a basic fortress:
mid = get_grid_width() / 2
# Dig down from the entrance
dig(mid, 2)
build_stairs(mid, 2)
# Dig rooms on level 2
dig(mid - 1, 2)
dig(mid + 1, 2)
dig(mid - 2, 2)
dig(mid + 2, 2)
# Build a farm and a still for food and drink
build_farm(mid - 2, 2)
build_farm(mid + 2, 2)
# Brew if we have a still and food
food = get_food()
drink = get_drink()
if drink < 10:
    brew()
# Check for threats and raise bridge
threat = get_threat()
if threat > 0:
    raise_bridge()
    recruit()
if threat == 0:
    lower_bridge()
";

        public string CurrentSourceCode => _sourceCode;
        public System.Action OnCodeChanged;

        public void Initialize(DwarfMatchManager match, FortressGrid fortress,
                               string initialCode = null, string programName = "OverseerAI")
        {
            _match = match;
            _fortress = fortress;
            _compilerExt = new DwarfCompilerExtension();

            _programName = programName;
            _sourceCode = initialCode ?? DEFAULT_CODE;
            _autoRun = true;

            LoadAndRun(_sourceCode);
        }

        protected override void Update()
        {
            if (_executor == null || _program == null || _isPaused) return;
            if (_match == null || !_match.MatchInProgress || _match.GameOver) return;

            float timeScale = SimulationTime.Instance?.timeScale ?? 1f;
            if (SimulationTime.Instance != null && SimulationTime.Instance.isPaused) return;

            float simDelta = UnityEngine.Time.deltaTime * timeScale;
            _opAccumulator += simDelta * OPS_PER_SECOND;

            int opsToRun = (int)_opAccumulator;
            _opAccumulator -= opsToRun;

            for (int i = 0; i < opsToRun; i++)
            {
                if (_executor.State.IsHalted)
                {
                    _executor.State.PC = 0;
                    _executor.State.IsHalted = false;
                }
                _executor.ExecuteOne();
            }

            if (opsToRun > 0)
                ProcessEvents();
        }

        protected override IGameIOHandler CreateIOHandler()
        {
            _ioHandler = new DwarfIOHandler(_match, _fortress);
            return _ioHandler;
        }

        protected override CompiledProgram CompileSource(string source, string name)
        {
            return PythonCompiler.Compile(source, name, _compilerExt);
        }

        protected override void ProcessEvents()
        {
            if (_executor?.State == null) return;
            while (_executor.State.OutputEvents.Count > 0)
                _executor.State.OutputEvents.Dequeue();
        }

        public void UploadCode(string newSource)
        {
            _sourceCode = newSource ?? DEFAULT_CODE;
            LoadAndRun(_sourceCode);
            Debug.Log($"[OverseerAI] Uploaded new code ({_program?.Instructions?.Length ?? 0} instructions)");
            OnCodeChanged?.Invoke();
        }

        public void ResetExecution()
        {
            if (_executor?.State == null) return;
            _executor.State.Reset();
            _opAccumulator = 0f;
        }
    }
}
