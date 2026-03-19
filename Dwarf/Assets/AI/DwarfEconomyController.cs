// Copyright CodeGamified 2025-2026
// MIT License — Dwarf
using UnityEngine;
using Dwarf.Game;
using Dwarf.Scripting;

namespace Dwarf.AI
{
    /// <summary>
    /// Economy difficulty tiers — controls how demanding the global economy is.
    /// </summary>
    public enum EconomyDifficulty
    {
        Peaceful = 0,   // no threats, generous production
        Moderate = 1,   // occasional ambush, normal economy
        Brutal   = 2,   // frequent sieges, harsh winters
        Insane   = 3,   // relentless assault, scarce resources
    }

    /// <summary>
    /// Economy controller — runs a second bytecode program that manages
    /// the fortress's automated production, resource consumption, and
    /// economic cycles. Same builtins as the player's overseer script.
    ///
    /// Each difficulty tier is a Python script that reads fortress state
    /// and issues standing orders (brew, smelt, craft, recruit for defense).
    /// The player's overseer script focuses on dig/build decisions while
    /// the economy script handles the repetitive production loops.
    /// </summary>
    public class DwarfEconomyController : MonoBehaviour
    {
        private DwarfMatchManager _match;
        private FortressGrid _fortress;
        private DwarfProgram _program;
        private EconomyDifficulty _difficulty;

        public EconomyDifficulty Difficulty => _difficulty;
        public DwarfProgram Program => _program;

        public void Initialize(DwarfMatchManager match, FortressGrid fortress,
                               EconomyDifficulty difficulty)
        {
            _match = match;
            _fortress = fortress;
            _program = gameObject.AddComponent<DwarfProgram>();
            SetDifficulty(difficulty);
        }

        public void SetDifficulty(EconomyDifficulty difficulty)
        {
            _difficulty = difficulty;
            string code = GetSampleCode(difficulty);
            _program.Initialize(_match, _fortress, code, $"Economy_{difficulty}");
            Debug.Log($"[Economy] Difficulty → {difficulty} (running bytecode)");
        }

        // =================================================================
        // ECONOMY SCRIPTS — each tier runs a standing-order loop that
        // manages production, brewing, smelting, forging, and defense.
        // Higher difficulty = more aggressive threats, fewer resources.
        // =================================================================

        public static string GetSampleCode(EconomyDifficulty difficulty)
        {
            switch (difficulty)
            {
                case EconomyDifficulty.Peaceful: return PEACEFUL;
                case EconomyDifficulty.Moderate: return MODERATE;
                case EconomyDifficulty.Brutal:   return BRUTAL;
                case EconomyDifficulty.Insane:   return INSANE;
                default:                         return MODERATE;
            }
        }

        // ── Peaceful: generous production, no military needed ───

        private const string PEACEFUL = @"# ECONOMY: Peaceful — generous production
# Auto-brew to keep drink topped up
drink = get_drink()
food = get_food()
if drink < 15:
    brew()
# Auto-smelt ore into bars
ore = get_ore()
if ore > 0:
    smelt()
# Auto-craft goods from stone surplus
stone = get_stone()
if stone > 10:
    craft()
# Forge if we have bars
bars = get_bars()
if bars > 2:
    forge()
";

        // ── Moderate: balanced economy with some recruitment ─────

        private const string MODERATE = @"# ECONOMY: Moderate — balanced production + defense
# Priority 1: Always brew (dwarves NEED booze)
drink = get_drink()
food = get_food()
pop = get_pop()
if drink < pop:
    brew()
    brew()
# Priority 2: Smelt ore
ore = get_ore()
if ore > 0:
    smelt()
# Priority 3: Forge weapons when bars available
bars = get_bars()
if bars > 1:
    forge()
# Priority 4: Craft surplus stone
stone = get_stone()
if stone > 8:
    craft()
# Priority 5: Recruit if threats possible and idle dwarves exist
threat = get_threat()
military = get_military()
idle = get_idle()
if threat > 0:
    raise_bridge()
    if idle > 0:
        recruit()
if threat == 0:
    lower_bridge()
# Keep at least 2 military
if military < 2:
    if idle > 2:
        recruit()
";

        // ── Brutal: aggressive smelting/forging, constant recruitment ──

        private const string BRUTAL = @"# ECONOMY: Brutal — heavy industry + garrison
# Brew aggressively — starvation is death
drink = get_drink()
pop = get_pop()
if drink < pop * 2:
    brew()
    brew()
    brew()
# Smelt everything
ore = get_ore()
if ore > 0:
    smelt()
    smelt()
# Forge all bars into equipment
bars = get_bars()
if bars > 0:
    forge()
    forge()
# Craft for trade value
stone = get_stone()
if stone > 5:
    craft()
    craft()
# Heavy recruitment — minimum 4 military
military = get_military()
idle = get_idle()
threat = get_threat()
if threat > 0:
    raise_bridge()
if threat == 0:
    lower_bridge()
if military < 4:
    if idle > 1:
        recruit()
        recruit()
";

        // ── Insane: constant war footing, minimal waste ─────────

        private const string INSANE = @"# ECONOMY: Insane — war economy
# Brew every tick if possible
drink = get_drink()
pop = get_pop()
brew()
brew()
brew()
# Smelt and forge non-stop
ore = get_ore()
smelt()
smelt()
bars = get_bars()
forge()
forge()
forge()
# Craft all spare stone
stone = get_stone()
craft()
craft()
# Maximum military — recruit all but 3 workers
military = get_military()
idle = get_idle()
if idle > 3:
    recruit()
    recruit()
    recruit()
# Bridge management — always defensive
threat = get_threat()
if threat > 0:
    raise_bridge()
if threat == 0:
    morale = get_morale()
    if morale > 0.3:
        lower_bridge()
";
    }
}
