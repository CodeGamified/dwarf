// Copyright CodeGamified 2025-2026
// MIT License — Dwarf
using UnityEngine;
using CodeGamified.Quality;

namespace Dwarf.Game
{
    /// <summary>
    /// 2.5D cross-section renderer for the fortress.
    ///
    /// Renders the XZ grid as a side-view slice:
    ///   X = horizontal position
    ///   Y = vertical (Z=0 at top, deeper underground goes down)
    ///   Physical Z = depth lane for layering
    ///
    /// Surface is at the top. Digging goes downward.
    /// Each cell type gets a distinct color and cube.
    /// </summary>
    public class FortressRenderer : MonoBehaviour, IQualityResponsive
    {
        private FortressGrid _fortress;

        private GameObject _cellParent;
        private GameObject _surfaceParent;

        private bool _dirty = true;

        public const float CellSize = 0.5f;
        private const float CellGap = 0.02f;

        // ── Color palette ───────────────────────────────────────

        private static readonly Color SolidColor     = new Color(0.20f, 0.18f, 0.15f);  // dark rock
        private static readonly Color OpenColor      = new Color(0.05f, 0.04f, 0.06f);  // dug-out dark
        private static readonly Color SurfaceColor   = new Color(0.15f, 0.30f, 0.10f);  // green hillside
        private static readonly Color StairsColor    = new Color(0.40f, 0.35f, 0.25f);  // carved stone stairs
        private static readonly Color WorkshopColor  = new Color(0.70f, 0.45f, 0.15f);  // amber workshop glow
        private static readonly Color FarmColor      = new Color(0.20f, 0.50f, 0.15f);  // green farm
        private static readonly Color StockpileColor = new Color(0.35f, 0.30f, 0.45f);  // purple storage
        private static readonly Color WallColor      = new Color(0.45f, 0.42f, 0.38f);  // light stone wall
        private static readonly Color EntranceColor  = new Color(0.50f, 0.40f, 0.20f);  // golden entrance
        private static readonly Color BridgeUpColor  = new Color(0.60f, 0.30f, 0.10f);  // raised bridge (warning)
        private static readonly Color BridgeDownColor= new Color(0.35f, 0.25f, 0.10f);  // lowered bridge

        // Workshop subtype accent colors
        private static readonly Color CarpenterColor   = new Color(0.55f, 0.35f, 0.15f);
        private static readonly Color MasonColor       = new Color(0.50f, 0.50f, 0.48f);
        private static readonly Color SmelterColor     = new Color(0.80f, 0.30f, 0.10f);
        private static readonly Color ForgeColor       = new Color(0.90f, 0.50f, 0.10f);
        private static readonly Color StillColor       = new Color(0.40f, 0.55f, 0.30f);
        private static readonly Color KitchenColor     = new Color(0.60f, 0.40f, 0.30f);
        private static readonly Color CraftsdwarfColor = new Color(0.50f, 0.40f, 0.60f);

        public void Initialize(FortressGrid fortress)
        {
            _fortress = fortress;

            _cellParent = new GameObject("Cells");
            _cellParent.transform.SetParent(transform, false);
            _surfaceParent = new GameObject("Surface");
            _surfaceParent.transform.SetParent(transform, false);

            _fortress.OnGridChanged += () => _dirty = true;
            QualityBridge.Register(this);
        }

        private void OnDisable() => QualityBridge.Unregister(this);
        public void OnQualityChanged(QualityTier tier) => _dirty = true;

        private void LateUpdate()
        {
            if (!_dirty) return;
            _dirty = false;
            Render();
        }

        public void MarkDirty() => _dirty = true;

        public float FortressWorldWidth => _fortress.Width * CellSize;
        public float FortressWorldHeight => _fortress.Depth * CellSize;

        // ═══════════════════════════════════════════════════════════════
        // RENDER
        // ═══════════════════════════════════════════════════════════════

        private void Render()
        {
            ClearChildren(_cellParent);

            for (int x = 0; x < _fortress.Width; x++)
            {
                for (int z = 0; z < _fortress.Depth; z++)
                {
                    var cell = _fortress.GetCell(x, z);
                    Color color = GetCellColor(cell, x, z);
                    float size = CellSize - CellGap;

                    // Skip rendering completely empty open cells for visual clarity
                    if (cell == CellType.Open)
                        size *= 0.15f; // tiny cube to show dug-out space

                    float worldX = x * CellSize + CellSize * 0.5f;
                    float worldY = -z * CellSize - CellSize * 0.5f; // surface at top, deeper = lower
                    float worldZ = 0f;

                    var go = CreatePrimitive($"Cell_{x}_{z}", _cellParent.transform);
                    go.transform.localPosition = new Vector3(worldX, worldY, worldZ);
                    go.transform.localScale = new Vector3(size, size, size);
                    SetColor(go, color);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // COLOR LOOKUP
        // ═══════════════════════════════════════════════════════════════

        private Color GetCellColor(CellType cell, int x, int z)
        {
            switch (cell)
            {
                case CellType.Solid:     return SolidColor;
                case CellType.Open:      return OpenColor;
                case CellType.Surface:   return SurfaceColor;
                case CellType.Stairs:    return StairsColor;
                case CellType.Workshop:  return GetWorkshopColor(x, z);
                case CellType.Farm:      return FarmColor;
                case CellType.Stockpile: return StockpileColor;
                case CellType.Wall:      return WallColor;
                case CellType.Entrance:  return EntranceColor;
                case CellType.Bridge:
                    return _fortress.BridgeRaised ? BridgeUpColor : BridgeDownColor;
                default:                 return SolidColor;
            }
        }

        private Color GetWorkshopColor(int x, int z)
        {
            var ws = _fortress.GetWorkshop(x, z);
            switch (ws)
            {
                case WorkshopType.Carpenter:   return CarpenterColor;
                case WorkshopType.Mason:       return MasonColor;
                case WorkshopType.Smelter:     return SmelterColor;
                case WorkshopType.Forge:       return ForgeColor;
                case WorkshopType.Still:       return StillColor;
                case WorkshopType.Kitchen:     return KitchenColor;
                case WorkshopType.Craftsdwarf: return CraftsdwarfColor;
                default:                       return WorkshopColor;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════

        private GameObject CreatePrimitive(string name, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            return go;
        }

        private static void ClearChildren(GameObject parent)
        {
            for (int c = parent.transform.childCount - 1; c >= 0; c--)
                Destroy(parent.transform.GetChild(c).gameObject);
        }

        private static void SetColor(GameObject go, Color color)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            var mat = r.material;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.color = color;
        }
    }
}
