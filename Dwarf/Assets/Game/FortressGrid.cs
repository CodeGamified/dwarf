// Copyright CodeGamified 2025-2026
// MIT License — Dwarf
using UnityEngine;

namespace Dwarf.Game
{
    /// <summary>
    /// Cell types in the fortress cross-section grid (XZ plane).
    /// X = horizontal, Z = depth (0 = surface, positive = deeper underground).
    /// </summary>
    public enum CellType
    {
        Solid       = 0,   // unexcavated rock
        Open        = 1,   // dug-out empty space
        Surface     = 2,   // surface ground level
        Stairs      = 3,   // connects z-levels
        Workshop    = 4,   // crafting station (Carpenter, Mason, Smelter, etc.)
        Farm        = 5,   // underground farm plot
        Stockpile   = 6,   // storage zone
        Wall        = 7,   // constructed wall (fortification)
        Entrance    = 8,   // main fortress entrance (surface level)
        Bridge      = 9,   // retractable drawbridge at entrance
    }

    /// <summary>
    /// Workshop subtypes — determines what the workshop produces.
    /// </summary>
    public enum WorkshopType
    {
        None        = 0,
        Carpenter   = 1,   // wood → furniture, barrels
        Mason       = 2,   // stone → blocks, furniture
        Smelter     = 3,   // ore → metal bars
        Forge       = 4,   // bars → weapons, armor
        Still       = 5,   // plants → drink
        Kitchen     = 6,   // raw food → meals
        Craftsdwarf = 7,   // stone/bone → trade goods
    }

    /// <summary>
    /// Fortress cross-section grid — the primary domain object.
    ///
    /// 2D slice through the hillside: X = horizontal, Z = depth (underground).
    /// Z=0 is the surface. Positive Z = deeper underground.
    /// The entrance is at the surface center. Dwarves dig down and out.
    ///
    ///   Surface (z=0)    ☼  ☼  ▲  ☼  ☼  ☼  ☼   (trees, entrance)
    ///   Level -1 (z=1)   ██ ░░ ≡≡ ░░ ██ ██ ██   (dug rooms, stairs)
    ///   Level -2 (z=2)   ██ ░░ ≡≡ ░░ π  ░░ ██   (workshops)
    ///   Level -3 (z=3)   ██ ██ ≡≡ ░░ ░░ ░░ ██   (farms, stockpiles)
    /// </summary>
    public class FortressGrid : MonoBehaviour
    {
        public int Width { get; private set; }
        public int Depth { get; private set; }

        private CellType[,] _cells;
        private WorkshopType[,] _workshops;

        // Resources
        public int Food { get; private set; }
        public int Drink { get; private set; }
        public int Wood { get; private set; }
        public int Stone { get; private set; }
        public int Ore { get; private set; }
        public int Bars { get; private set; }
        public int Goods { get; private set; }   // trade goods value

        // Bridge state
        public bool BridgeRaised { get; private set; }

        // Events
        public System.Action OnGridChanged;

        public void Initialize(int width, int depth)
        {
            Width = width;
            Depth = depth;
            _cells = new CellType[width, depth];
            _workshops = new WorkshopType[width, depth];
            Generate();
        }

        public void Reset()
        {
            System.Array.Clear(_cells, 0, _cells.Length);
            System.Array.Clear(_workshops, 0, _workshops.Length);
            Food = 0; Drink = 0; Wood = 0; Stone = 0;
            Ore = 0; Bars = 0; Goods = 0;
            BridgeRaised = false;
            Generate();
            OnGridChanged?.Invoke();
        }

        // ═══════════════════════════════════════════════════════════════
        // QUERIES
        // ═══════════════════════════════════════════════════════════════

        public CellType GetCell(int x, int z)
        {
            if (x < 0 || x >= Width || z < 0 || z >= Depth) return CellType.Solid;
            return _cells[x, z];
        }

        public WorkshopType GetWorkshop(int x, int z)
        {
            if (x < 0 || x >= Width || z < 0 || z >= Depth) return WorkshopType.None;
            return _workshops[x, z];
        }

        public bool IsDiggable(int x, int z)
        {
            if (x < 0 || x >= Width || z < 0 || z >= Depth) return false;
            return _cells[x, z] == CellType.Solid;
        }

        public bool IsOpen(int x, int z)
        {
            if (x < 0 || x >= Width || z < 0 || z >= Depth) return false;
            var c = _cells[x, z];
            return c == CellType.Open || c == CellType.Stairs || c == CellType.Entrance
                || c == CellType.Workshop || c == CellType.Farm || c == CellType.Stockpile;
        }

        /// <summary>Check if a cell is reachable from the entrance via open/stair path.</summary>
        public bool IsReachable(int x, int z)
        {
            // Simple adjacency check — must neighbor an open cell or be on z=0
            if (z == 0) return true;
            if (IsOpen(x, z - 1) && GetCell(x, z - 1) == CellType.Stairs) return true;
            if (IsOpen(x - 1, z)) return true;
            if (IsOpen(x + 1, z)) return true;
            if (IsOpen(x, z + 1) && GetCell(x, z + 1) == CellType.Stairs) return true;
            return false;
        }

        public int CountCells(CellType type)
        {
            int count = 0;
            for (int x = 0; x < Width; x++)
                for (int z = 0; z < Depth; z++)
                    if (_cells[x, z] == type) count++;
            return count;
        }

        public int CountWorkshops(WorkshopType type)
        {
            int count = 0;
            for (int x = 0; x < Width; x++)
                for (int z = 0; z < Depth; z++)
                    if (_workshops[x, z] == type) count++;
            return count;
        }

        public int TotalWorkshops()
        {
            int count = 0;
            for (int x = 0; x < Width; x++)
                for (int z = 0; z < Depth; z++)
                    if (_cells[x, z] == CellType.Workshop) count++;
            return count;
        }

        // ═══════════════════════════════════════════════════════════════
        // COMMANDS
        // ═══════════════════════════════════════════════════════════════

        public bool Dig(int x, int z)
        {
            if (!IsDiggable(x, z)) return false;
            if (!IsReachable(x, z)) return false;
            _cells[x, z] = CellType.Open;
            Stone++;
            OnGridChanged?.Invoke();
            return true;
        }

        public bool BuildStairs(int x, int z)
        {
            if (x < 0 || x >= Width || z < 0 || z >= Depth) return false;
            if (_cells[x, z] != CellType.Open) return false;
            if (Stone < 1) return false;
            Stone--;
            _cells[x, z] = CellType.Stairs;
            OnGridChanged?.Invoke();
            return true;
        }

        public bool BuildWorkshop(int x, int z, WorkshopType type)
        {
            if (x < 0 || x >= Width || z < 0 || z >= Depth) return false;
            if (_cells[x, z] != CellType.Open) return false;
            if (Stone < 2) return false;
            Stone -= 2;
            _cells[x, z] = CellType.Workshop;
            _workshops[x, z] = type;
            OnGridChanged?.Invoke();
            return true;
        }

        public bool BuildFarm(int x, int z)
        {
            if (x < 0 || x >= Width || z < 0 || z >= Depth) return false;
            if (_cells[x, z] != CellType.Open) return false;
            _cells[x, z] = CellType.Farm;
            OnGridChanged?.Invoke();
            return true;
        }

        public bool BuildStockpile(int x, int z)
        {
            if (x < 0 || x >= Width || z < 0 || z >= Depth) return false;
            if (_cells[x, z] != CellType.Open) return false;
            _cells[x, z] = CellType.Stockpile;
            OnGridChanged?.Invoke();
            return true;
        }

        public bool BuildWall(int x, int z)
        {
            if (x < 0 || x >= Width || z < 0 || z >= Depth) return false;
            if (_cells[x, z] != CellType.Open) return false;
            if (Stone < 1) return false;
            Stone--;
            _cells[x, z] = CellType.Wall;
            OnGridChanged?.Invoke();
            return true;
        }

        public bool SetBridge(bool raised)
        {
            BridgeRaised = raised;
            OnGridChanged?.Invoke();
            return true;
        }

        // ── Resource operations ─────────────────────────────────

        public void AddFood(int amount) { Food += amount; }
        public void AddDrink(int amount) { Drink += amount; }
        public void AddWood(int amount) { Wood += amount; }
        public void AddStone(int amount) { Stone += amount; }
        public void AddOre(int amount) { Ore += amount; }
        public void AddBars(int amount) { Bars += amount; }
        public void AddGoods(int amount) { Goods += amount; }

        public bool SpendFood(int amount) { if (Food < amount) return false; Food -= amount; return true; }
        public bool SpendDrink(int amount) { if (Drink < amount) return false; Drink -= amount; return true; }
        public bool SpendWood(int amount) { if (Wood < amount) return false; Wood -= amount; return true; }
        public bool SpendStone(int amount) { if (Stone < amount) return false; Stone -= amount; return true; }
        public bool SpendOre(int amount) { if (Ore < amount) return false; Ore -= amount; return true; }
        public bool SpendBars(int amount) { if (Bars < amount) return false; Bars -= amount; return true; }

        // ═══════════════════════════════════════════════════════════════
        // GENERATION — initial fortress layout
        // ═══════════════════════════════════════════════════════════════

        private void Generate()
        {
            // Fill everything with solid rock
            for (int x = 0; x < Width; x++)
                for (int z = 0; z < Depth; z++)
                {
                    _cells[x, z] = CellType.Solid;
                    _workshops[x, z] = WorkshopType.None;
                }

            // Surface level (z=0) — open ground
            for (int x = 0; x < Width; x++)
                _cells[x, 0] = CellType.Surface;

            // Entrance at center of surface
            int mid = Width / 2;
            _cells[mid, 0] = CellType.Entrance;

            // Bridge at entrance
            _cells[mid, 0] = CellType.Entrance;
            BridgeRaised = false;

            // Pre-dig a small starting area (wagon unloaded here)
            // Stairs down from entrance
            _cells[mid, 1] = CellType.Stairs;
            // Small room at z=1
            for (int dx = -1; dx <= 1; dx++)
            {
                int nx = mid + dx;
                if (nx >= 0 && nx < Width)
                    _cells[nx, 1] = dx == 0 ? CellType.Stairs : CellType.Open;
            }

            // Starting resources — wagon supplies
            Food = 20;
            Drink = 20;
            Wood = 10;
            Stone = 5;
            Ore = 0;
            Bars = 0;
            Goods = 0;

            // Scatter some ore veins in the deeper levels
            for (int i = 0; i < 8; i++)
            {
                int rx = Random.Range(1, Width - 1);
                int rz = Random.Range(3, Depth);
                // Mark ore veins as solid (they'll yield ore when dug)
                // We track this via a separate lookup — for simplicity,
                // digging any cell deeper than z=3 has a chance to yield ore
            }
        }
    }
}
