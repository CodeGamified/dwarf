// Copyright CodeGamified 2025-2026
// MIT License — Dwarf
using UnityEngine;

namespace Dwarf.Core
{
    /// <summary>
    /// Fortress simulation time — season/year formatted.
    /// One day = 60 sim-seconds. One season = 90 days. One year = 4 seasons.
    /// Max 100x for fast-forwarding through fortress years.
    /// </summary>
    public class DwarfSimulationTime : CodeGamified.Time.SimulationTime
    {
        protected override float MaxTimeScale => 100f;

        public const double SECONDS_PER_DAY = 60.0;
        public const int DAYS_PER_SEASON = 90;
        public const int SEASONS_PER_YEAR = 4;
        public const double SECONDS_PER_SEASON = SECONDS_PER_DAY * DAYS_PER_SEASON;
        public const double SECONDS_PER_YEAR = SECONDS_PER_SEASON * SEASONS_PER_YEAR;

        private static readonly string[] SeasonNames = { "Spring", "Summer", "Autumn", "Winter" };

        protected override void OnInitialize()
        {
            timeScalePresets = new float[]
                { 0f, 0.25f, 0.5f, 1f, 2f, 5f, 10f, 50f, 100f };
            currentPresetIndex = 3; // Start at 1x
        }

        public int GetYear() => (int)(simulationTime / SECONDS_PER_YEAR) + 1;
        public int GetSeason() => (int)((simulationTime % SECONDS_PER_YEAR) / SECONDS_PER_SEASON);
        public int GetDay() => (int)((simulationTime % SECONDS_PER_SEASON) / SECONDS_PER_DAY) + 1;

        public override string GetFormattedTime()
        {
            int year = GetYear();
            int season = GetSeason();
            int day = GetDay();
            return $"Year {year}, {SeasonNames[season]}, Day {day}";
        }
    }
}
