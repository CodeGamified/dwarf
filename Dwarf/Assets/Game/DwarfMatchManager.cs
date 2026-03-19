// Copyright CodeGamified 2025-2026
// MIT License — Dwarf
using UnityEngine;
using CodeGamified.Time;

namespace Dwarf.Game
{
    /// <summary>
    /// Threat level — what dangers face the fortress.
    /// </summary>
    public enum ThreatLevel
    {
        None    = 0,
        Ambush  = 1,   // small raiding party
        Siege   = 2,   // full goblin siege
    }

    /// <summary>
    /// Match manager for Dwarf — fortress lifecycle.
    ///
    /// Tracks dwarves, resources, morale, threats, production.
    /// Ticks each sim-second. Game over when all dwarves are dead or abandon fortress.
    ///
    /// Player writes code to issue Overseer commands:
    ///   - dig(x,z) → excavate rock
    ///   - build_workshop(x,z,type) → place production workshop
    ///   - brew() → convert plants to drink
    ///   - smelt() → convert ore to metal bars
    ///   - forge() → convert bars to weapons/armor
    ///   - recruit() → conscript a dwarf into the militia
    ///   - raise_bridge() / lower_bridge() → seal/open entrance
    /// </summary>
    public class DwarfMatchManager : MonoBehaviour
    {
        private FortressGrid _fortress;

        // Config
        private bool _autoRestart;
        private float _restartDelay;

        // ── Population ──────────────────────────────────────────

        public int Population { get; private set; }
        public int Military { get; private set; }
        public int IdleDwarves { get; private set; }
        public int Migrants { get; private set; }  // total migrants arrived

        // ── Morale (0-1) ────────────────────────────────────────

        public float Morale { get; private set; }

        // ── Threat ──────────────────────────────────────────────

        public ThreatLevel Threat { get; private set; }
        public int ThreatTimer { get; private set; }  // ticks until threat resolves

        // ── Score ───────────────────────────────────────────────

        public int Score { get; private set; }
        public int HighScore { get; private set; }
        public bool GameOver { get; private set; }
        public bool MatchInProgress { get; private set; }
        public int MatchesPlayed { get; private set; }

        // ── Accessors ───────────────────────────────────────────

        public FortressGrid Fortress => _fortress;
        public int Food => _fortress.Food;
        public int Drink => _fortress.Drink;
        public int Wood => _fortress.Wood;
        public int Stone => _fortress.Stone;
        public int Ore => _fortress.Ore;
        public int Bars => _fortress.Bars;
        public int Goods => _fortress.Goods;

        // ── Events ──────────────────────────────────────────────

        public System.Action OnMatchStarted;
        public System.Action OnGameOver;
        public System.Action<int> OnPopulationChanged;
        public System.Action OnBoardChanged;

        // ── Tick timing ─────────────────────────────────────────

        private float _tickAccumulator;
        private const float TICK_INTERVAL = 1f; // one sim-tick per sim-second
        private int _totalTicks;

        public void Initialize(FortressGrid fortress, int startPop,
                               bool autoRestart = true, float restartDelay = 5f)
        {
            _fortress = fortress;
            Population = startPop;
            _autoRestart = autoRestart;
            _restartDelay = restartDelay;
        }

        public void StartMatch()
        {
            _fortress.Reset();
            Population = 7; // canonical DF starting count
            Military = 0;
            IdleDwarves = 7;
            Migrants = 0;
            Morale = 0.7f;
            Threat = ThreatLevel.None;
            ThreatTimer = 0;
            Score = 0;
            GameOver = false;
            MatchInProgress = true;
            _tickAccumulator = 0f;
            _totalTicks = 0;

            OnMatchStarted?.Invoke();
            OnBoardChanged?.Invoke();
        }

        private void Update()
        {
            if (!MatchInProgress || GameOver) return;

            float timeScale = SimulationTime.Instance?.timeScale ?? 1f;
            if (SimulationTime.Instance != null && SimulationTime.Instance.isPaused) return;

            float simDelta = Time.deltaTime * timeScale;
            _tickAccumulator += simDelta;

            while (_tickAccumulator >= TICK_INTERVAL)
            {
                _tickAccumulator -= TICK_INTERVAL;
                SimTick();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // SIMULATION TICK — one sim-second of fortress life
        // ═══════════════════════════════════════════════════════════════

        private void SimTick()
        {
            _totalTicks++;

            // ── Food & drink consumption ────────────────────────
            // Each dwarf eats ~1 food per 30 ticks, drinks ~1 per 20 ticks
            if (_totalTicks % 30 == 0 && Population > 0)
            {
                int consumed = Mathf.Max(1, Population / 4);
                _fortress.SpendFood(consumed);
            }
            if (_totalTicks % 20 == 0 && Population > 0)
            {
                int consumed = Mathf.Max(1, Population / 4);
                _fortress.SpendDrink(consumed);
            }

            // ── Farm production — farms produce food ────────────
            int farms = _fortress.CountCells(CellType.Farm);
            if (farms > 0 && _totalTicks % 15 == 0)
                _fortress.AddFood(farms);

            // ── Still production — stills brew drink ────────────
            int stills = _fortress.CountWorkshops(WorkshopType.Still);
            if (stills > 0 && _totalTicks % 20 == 0 && _fortress.Food > stills)
            {
                _fortress.SpendFood(stills);
                _fortress.AddDrink(stills * 2);
            }

            // ── Mining yield — deep digging finds ore ───────────
            // (Handled in Dig command — deeper = more ore chance)

            // ── Morale ──────────────────────────────────────────
            float moraleDrift = 0f;
            if (_fortress.Food <= 0) moraleDrift -= 0.02f;    // starving
            if (_fortress.Drink <= 0) moraleDrift -= 0.03f;   // no booze (critical in DF)
            if (_fortress.Food > Population) moraleDrift += 0.005f;
            if (_fortress.Drink > Population) moraleDrift += 0.005f;
            // Stockpiles boost morale (organized fortress)
            int stockpiles = _fortress.CountCells(CellType.Stockpile);
            moraleDrift += stockpiles * 0.002f;
            // Crowding — need enough open space
            int openCells = _fortress.CountCells(CellType.Open) + _fortress.CountCells(CellType.Stairs);
            if (openCells < Population) moraleDrift -= 0.01f;
            Morale = Mathf.Clamp01(Morale + moraleDrift);

            // ── Idle dwarves — not mining, not in military, not farming ────
            int working = Military + farms + _fortress.TotalWorkshops();
            IdleDwarves = Mathf.Max(0, Population - working);

            // ── Migrants — arrive periodically if morale > 0.4 ──
            // Every ~180 ticks (3 minutes sim-time), up to 3 migrants
            if (_totalTicks % 180 == 0 && Morale > 0.4f && Population < 50)
            {
                int wave = Random.Range(1, 4);
                Population += wave;
                Migrants += wave;
                IdleDwarves += wave;
                OnPopulationChanged?.Invoke(Population);
            }

            // ── Threats — goblin ambush/siege ───────────────────
            UpdateThreats();

            // ── Deaths — starvation, low morale tantrum spiral ──
            if (_fortress.Food <= 0 && Random.value < 0.05f && Population > 0)
            {
                Population--;
                OnPopulationChanged?.Invoke(Population);
            }
            if (Morale < 0.1f && Random.value < 0.03f && Population > 0)
            {
                Population--; // tantrum spiral death
                OnPopulationChanged?.Invoke(Population);
            }

            // ── Score — fortress wealth per tick ────────────────
            int wealth = _fortress.Food + _fortress.Drink + _fortress.Wood
                       + _fortress.Stone * 2 + _fortress.Ore * 3
                       + _fortress.Bars * 5 + _fortress.Goods * 4
                       + Population * 10 + _fortress.TotalWorkshops() * 20;
            Score += wealth / 100;
            if (Score > HighScore) HighScore = Score;

            OnBoardChanged?.Invoke();

            // ── Game over ───────────────────────────────────────
            if (Population <= 0)
                EndGame();
        }

        // ═══════════════════════════════════════════════════════════════
        // THREATS
        // ═══════════════════════════════════════════════════════════════

        private void UpdateThreats()
        {
            if (Threat != ThreatLevel.None)
            {
                ThreatTimer--;
                if (ThreatTimer <= 0)
                    ResolveThreat();
                return;
            }

            // New threats — more likely in later years, higher wealth
            if (_totalTicks < 300) return; // grace period (~5 minutes)

            float threatChance = 0.002f + (_totalTicks / 100000f);
            if (Random.value < threatChance)
            {
                Threat = Random.value < 0.7f ? ThreatLevel.Ambush : ThreatLevel.Siege;
                ThreatTimer = Threat == ThreatLevel.Ambush ? 30 : 60;
            }
        }

        private void ResolveThreat()
        {
            if (Threat == ThreatLevel.None) return;

            if (_fortress.BridgeRaised)
            {
                // Bridge up — siege repelled with no losses
                Threat = ThreatLevel.None;
                return;
            }

            int attackPower = Threat == ThreatLevel.Ambush ? 2 : 5;
            int defensePower = Military;

            if (defensePower >= attackPower)
            {
                // Military handles it — some casualties possible
                int losses = Mathf.Max(0, attackPower - defensePower / 2);
                int militaryLoss = Mathf.Min(losses, Military);
                Military -= militaryLoss;
                Population -= militaryLoss;
                OnPopulationChanged?.Invoke(Population);
            }
            else
            {
                // Military overwhelmed — civilian casualties
                int losses = attackPower - defensePower + Random.Range(0, 3);
                losses = Mathf.Min(losses, Population);
                int militaryLoss = Mathf.Min(Military, losses);
                Military -= militaryLoss;
                Population -= losses;
                Morale = Mathf.Clamp01(Morale - 0.15f);
                OnPopulationChanged?.Invoke(Population);
            }

            Threat = ThreatLevel.None;
        }

        // ═══════════════════════════════════════════════════════════════
        // OVERSEER COMMANDS — called by IOHandler
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Dig a cell. Yields stone (always) and ore (chance, deeper = more).</summary>
        public int DigCell(int x, int z)
        {
            if (!MatchInProgress || GameOver) return 0;
            if (!_fortress.Dig(x, z)) return 0;
            // Ore chance increases with depth
            if (z >= 3 && Random.value < z * 0.06f)
                _fortress.AddOre(1);
            return 1;
        }

        /// <summary>Build stairs at an open cell. Costs 1 stone.</summary>
        public int BuildStairs(int x, int z)
        {
            if (!MatchInProgress || GameOver) return 0;
            return _fortress.BuildStairs(x, z) ? 1 : 0;
        }

        /// <summary>Build a workshop. Costs 2 stone. Type: 1=Carpenter,2=Mason,3=Smelter,4=Forge,5=Still,6=Kitchen,7=Craftsdwarf.</summary>
        public int BuildWorkshop(int x, int z, int type)
        {
            if (!MatchInProgress || GameOver) return 0;
            if (type < 1 || type > 7) return 0;
            return _fortress.BuildWorkshop(x, z, (WorkshopType)type) ? 1 : 0;
        }

        /// <summary>Build a farm plot. Free to designate on open cells.</summary>
        public int BuildFarm(int x, int z)
        {
            if (!MatchInProgress || GameOver) return 0;
            return _fortress.BuildFarm(x, z) ? 1 : 0;
        }

        /// <summary>Build a stockpile. Free to designate on open cells.</summary>
        public int BuildStockpile(int x, int z)
        {
            if (!MatchInProgress || GameOver) return 0;
            return _fortress.BuildStockpile(x, z) ? 1 : 0;
        }

        /// <summary>Build a constructed wall. Costs 1 stone.</summary>
        public int BuildWall(int x, int z)
        {
            if (!MatchInProgress || GameOver) return 0;
            return _fortress.BuildWall(x, z) ? 1 : 0;
        }

        /// <summary>Brew drink at a Still. Converts 1 food → 2 drink. Returns 1=success.</summary>
        public int Brew()
        {
            if (!MatchInProgress || GameOver) return 0;
            if (_fortress.CountWorkshops(WorkshopType.Still) == 0) return 0;
            if (!_fortress.SpendFood(1)) return 0;
            _fortress.AddDrink(2);
            return 1;
        }

        /// <summary>Smelt ore at a Smelter. Converts 1 ore → 1 bar. Returns 1=success.</summary>
        public int Smelt()
        {
            if (!MatchInProgress || GameOver) return 0;
            if (_fortress.CountWorkshops(WorkshopType.Smelter) == 0) return 0;
            if (!_fortress.SpendOre(1)) return 0;
            _fortress.AddBars(1);
            return 1;
        }

        /// <summary>Forge equipment at a Forge. Converts 1 bar → 5 goods. Returns 1=success.</summary>
        public int Forge()
        {
            if (!MatchInProgress || GameOver) return 0;
            if (_fortress.CountWorkshops(WorkshopType.Forge) == 0) return 0;
            if (!_fortress.SpendBars(1)) return 0;
            _fortress.AddGoods(5);
            return 1;
        }

        /// <summary>Craft trade goods. Converts 1 stone → 3 goods. Returns 1=success.</summary>
        public int Craft()
        {
            if (!MatchInProgress || GameOver) return 0;
            if (_fortress.CountWorkshops(WorkshopType.Craftsdwarf) == 0) return 0;
            if (!_fortress.SpendStone(1)) return 0;
            _fortress.AddGoods(3);
            return 1;
        }

        /// <summary>Recruit an idle dwarf into the militia. Returns 1=success.</summary>
        public int Recruit()
        {
            if (!MatchInProgress || GameOver) return 0;
            if (IdleDwarves <= 0) return 0;
            Military++;
            IdleDwarves--;
            return 1;
        }

        /// <summary>Raise the drawbridge — blocks entrance. Returns 1=success.</summary>
        public int RaiseBridge()
        {
            if (!MatchInProgress || GameOver) return 0;
            return _fortress.SetBridge(true) ? 1 : 0;
        }

        /// <summary>Lower the drawbridge — opens entrance. Returns 1=success.</summary>
        public int LowerBridge()
        {
            if (!MatchInProgress || GameOver) return 0;
            return _fortress.SetBridge(false) ? 1 : 0;
        }

        // ═══════════════════════════════════════════════════════════════
        // GAME OVER
        // ═══════════════════════════════════════════════════════════════

        private void EndGame()
        {
            GameOver = true;
            MatchInProgress = false;
            MatchesPlayed++;
            if (Score > HighScore) HighScore = Score;
            OnGameOver?.Invoke();

            if (_autoRestart)
                StartCoroutine(RestartAfterDelay());
        }

        private System.Collections.IEnumerator RestartAfterDelay()
        {
            float waited = 0f;
            while (waited < _restartDelay)
            {
                if (SimulationTime.Instance != null && !SimulationTime.Instance.isPaused)
                    waited += Time.deltaTime * (SimulationTime.Instance?.timeScale ?? 1f);
                yield return null;
            }
            StartMatch();
        }
    }
}
