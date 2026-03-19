// Copyright CodeGamified 2025-2026
// MIT License — Dwarf
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CodeGamified.Camera;
using CodeGamified.Time;
using CodeGamified.Settings;
using CodeGamified.Quality;
using CodeGamified.Bootstrap;
using Dwarf.Game;
using Dwarf.Scripting;
using Dwarf.AI;
using Dwarf.UI;

namespace Dwarf.Core
{
    /// <summary>
    /// Bootstrap for Dwarf — code-driven fortress management.
    ///
    /// Architecture:
    ///   - 2D cross-section grid: X = horizontal, Z = depth (underground)
    ///   - Camera faces the XY plane (side-view), surface at top
    ///   - Players WRITE CODE to dig, build, craft, and survive
    ///   - Dwarves are simulated entities that consume food/drink
    ///
    /// Attach to a GameObject. Press Play → fortress appears.
    /// </summary>
    public class DwarfBootstrap : GameBootstrap, IQualityResponsive
    {
        protected override string LogTag => "DWARF";

        // =================================================================
        // INSPECTOR
        // =================================================================

        [Header("Fortress")]
        [Tooltip("Fortress grid width (horizontal cells)")]
        public int fortressWidth = 20;

        [Tooltip("Fortress grid depth (underground z-levels)")]
        public int fortressDepth = 12;

        [Header("Population")]
        [Tooltip("Starting dwarf count")]
        public int startingPopulation = 7;

        [Header("Match")]
        [Tooltip("Auto-restart after fortress abandonment")]
        public bool autoRestart = true;

        [Tooltip("Delay before restarting (sim-seconds)")]
        public float restartDelay = 5f;

        [Header("Time")]
        public bool enableTimeScale = true;

        [Header("Scripting")]
        public bool enableScripting = true;

        [Header("Camera")]
        public bool configureCamera = true;

        // =================================================================
        // RUNTIME REFERENCES
        // =================================================================

        private FortressGrid _fortress;
        private FortressRenderer _renderer;
        private DwarfMatchManager _match;
        private DwarfProgram _playerProgram;
        private DwarfEconomyController _economyController;
        private DwarfTUIManager _tuiManager;

        // Camera
        private CameraAmbientMotion _cameraSway;

        // Post-processing
        private Bloom _bloom;
        private Volume _postProcessVolume;

        // =================================================================
        // UPDATE
        // =================================================================

        private void Update()
        {
            UpdateBloomScale();
        }

        private void UpdateBloomScale()
        {
            if (_bloom == null || !_bloom.active) return;
            var cam = Camera.main;
            if (cam == null) return;
            float dist = Vector3.Distance(cam.transform.position, FortressCenter());
            float defaultDist = 12f;
            float scale = Mathf.Clamp01(defaultDist / Mathf.Max(dist, 0.01f));
            _bloom.intensity.value = Mathf.Lerp(0.5f, 1.0f, scale);
        }

        // =================================================================
        // BOOTSTRAP — mandatory wiring order
        // =================================================================

        private void Start()
        {
            Log("Dwarf Bootstrap starting...");

            // 1. Settings + Quality
            SettingsBridge.Load();
            QualityBridge.SetTier((QualityTier)SettingsBridge.QualityLevel);
            QualityBridge.Register(this);
            Log($"Settings loaded (Quality={SettingsBridge.QualityLevel})");

            // 2. Simulation time
            EnsureSimulationTime<DwarfSimulationTime>();

            // 3. Camera + post-processing
            SetupCamera();

            // 4. Fortress grid (domain object)
            CreateFortressGrid();

            // 5. Match manager (needs fortress)
            CreateMatchManager();

            // 6. Visual renderer (needs fortress)
            CreateRenderer();

            // 7. Input provider
            CreateInputProvider();

            // 8. Player program (needs match + fortress)
            if (enableScripting) CreatePlayerProgram();

            // 8b. Economy controller (runs second bytecode program)
            CreateEconomyController();

            // 8c. TUI manager (left overseer + right economy + bottom status)
            CreateTUIManager();

            // 9. Wire events + start
            WireEvents();
            StartCoroutine(RunBootSequence());
        }

        public void OnQualityChanged(QualityTier tier)
        {
            Log($"Quality changed -> {tier}");
        }

        // =================================================================
        // CAMERA — side-view of the cross-section
        // =================================================================

        /// <summary>
        /// Center of the fortress — midpoint of the grid.
        /// Surface at top, underground extends downward.
        /// </summary>
        private Vector3 FortressCenter()
        {
            float w = fortressWidth * FortressRenderer.CellSize;
            float h = fortressDepth * FortressRenderer.CellSize;
            return new Vector3(w * 0.5f, -h * 0.35f, 0f);
        }

        private void SetupCamera()
        {
            if (!configureCamera) return;

            var cam = EnsureCamera();

            // Side-view camera — faces the XY plane from -Z
            // X = fortress width, Y = depth (surface at top, underground below)
            cam.orthographic = false;
            cam.fieldOfView = 50f;
            var center = FortressCenter();
            cam.transform.position = center + new Vector3(0f, 1f, -8f);
            cam.transform.LookAt(center, Vector3.up);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.01f, 0.02f); // deep underground dark
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;

            // Ambient sway — subtle drift
            _cameraSway = cam.gameObject.AddComponent<CameraAmbientMotion>();
            _cameraSway.lookAtTarget = center;

            // Post-processing: bloom for workshop glow, ore shimmer
            var camData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null)
                camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            var volumeGO = new GameObject("PostProcessVolume");
            _postProcessVolume = volumeGO.AddComponent<Volume>();
            _postProcessVolume.isGlobal = true;
            _postProcessVolume.priority = 1;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _bloom = profile.Add<Bloom>();
            _bloom.threshold.overrideState = true;
            _bloom.threshold.value = 0.8f;
            _bloom.intensity.overrideState = true;
            _bloom.intensity.value = 1.0f;
            _bloom.scatter.overrideState = true;
            _bloom.scatter.value = 0.5f;
            _bloom.clamp.overrideState = true;
            _bloom.clamp.value = 20f;
            _bloom.highQualityFiltering.overrideState = true;
            _bloom.highQualityFiltering.value = true;
            _postProcessVolume.profile = profile;

            Log("Camera: side-view, XY cross-section, surface at top");
        }

        // =================================================================
        // FORTRESS GRID
        // =================================================================

        private void CreateFortressGrid()
        {
            var go = new GameObject("FortressGrid");
            _fortress = go.AddComponent<FortressGrid>();
            _fortress.Initialize(fortressWidth, fortressDepth);
            Log($"Created FortressGrid (width={fortressWidth}, depth={fortressDepth})");
        }

        // =================================================================
        // MATCH MANAGER
        // =================================================================

        private void CreateMatchManager()
        {
            var go = new GameObject("MatchManager");
            _match = go.AddComponent<DwarfMatchManager>();
            _match.Initialize(_fortress, startingPopulation, autoRestart, restartDelay);
            Log($"Created MatchManager (pop={startingPopulation})");
        }

        // =================================================================
        // RENDERER
        // =================================================================

        private void CreateRenderer()
        {
            _renderer = _fortress.gameObject.AddComponent<FortressRenderer>();
            _renderer.Initialize(_fortress);
            Log("Created FortressRenderer (2.5D cross-section cubes)");
        }

        // =================================================================
        // INPUT PROVIDER
        // =================================================================

        private void CreateInputProvider()
        {
            var go = new GameObject("InputProvider");
            go.AddComponent<DwarfInputProvider>();
            Log("Created InputProvider");
        }

        // =================================================================
        // PLAYER SCRIPTING
        // =================================================================

        private void CreatePlayerProgram()
        {
            var go = new GameObject("OverseerProgram");
            _playerProgram = go.AddComponent<DwarfProgram>();
            _playerProgram.Initialize(_match, _fortress);
            Log("Created OverseerProgram (code-controlled overseer)");
        }

        // =================================================================
        // ECONOMY CONTROLLER
        // =================================================================

        private void CreateEconomyController()
        {
            var go = new GameObject("EconomyController");
            _economyController = go.AddComponent<DwarfEconomyController>();
            _economyController.Initialize(_match, _fortress, EconomyDifficulty.Moderate);
            Log($"Created EconomyController (difficulty={_economyController.Difficulty})");
        }

        // =================================================================
        // TUI MANAGER
        // =================================================================

        private void CreateTUIManager()
        {
            var go = new GameObject("TUIManager");
            _tuiManager = go.AddComponent<DwarfTUIManager>();
            _tuiManager.Initialize(_match, _playerProgram, _economyController);
            Log("Created TUIManager (overseer debugger + economy debugger + status panel)");
        }

        // =================================================================
        // EVENT WIRING
        // =================================================================

        private void WireEvents()
        {
            if (SimulationTime.Instance != null)
            {
                SimulationTime.Instance.OnTimeScaleChanged += s => Log($"Time scale -> {s:F0}x");
                SimulationTime.Instance.OnPausedChanged += p => Log(p ? "PAUSED" : "RESUMED");
            }

            if (_match != null)
            {
                _match.OnMatchStarted += () =>
                {
                    Log("FORTRESS FOUNDED — Strike the earth!");
                    _renderer?.MarkDirty();
                };

                _match.OnPopulationChanged += pop =>
                {
                    _renderer?.MarkDirty();
                };

                _match.OnGameOver += () =>
                {
                    Log($"FORTRESS ABANDONED | Pop: {_match.Population} | Score: {_match.Score}");
                    if (autoRestart)
                        StartCoroutine(RestartAfterDelay());
                };

                _match.OnBoardChanged += () => _renderer?.MarkDirty();
            }
        }

        // =================================================================
        // BOOT SEQUENCE
        // =================================================================

        private IEnumerator RunBootSequence()
        {
            yield return null;
            yield return null;

            LogDivider();
            Log("DWARF — Strike the Earth");
            LogDivider();
            LogStatus("FORTRESS", $"width={fortressWidth}, depth={fortressDepth}");
            LogStatus("POPULATION", $"{startingPopulation}");
            LogEnabled("SCRIPTING", enableScripting);
            LogEnabled("TIME SCALE", enableTimeScale);
            LogEnabled("AUTO RESTART", autoRestart);
            LogDivider();

            _match.StartMatch();
            Log("First expedition embarked — dig deep, strike the earth.");
        }

        private IEnumerator RestartAfterDelay()
        {
            float waited = 0f;
            while (waited < restartDelay)
            {
                if (SimulationTime.Instance != null && !SimulationTime.Instance.isPaused)
                    waited += Time.deltaTime * (SimulationTime.Instance?.timeScale ?? 1f);
                yield return null;
            }

            _match.StartMatch();
            _playerProgram?.ResetExecution();
            Log("New expedition embarked");
        }

        private void OnDestroy()
        {
            QualityBridge.Unregister(this);
        }
    }
}
