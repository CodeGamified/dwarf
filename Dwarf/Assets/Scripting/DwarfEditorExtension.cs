// Copyright CodeGamified 2025-2026
// MIT License — Dwarf
using System.Collections.Generic;
using CodeGamified.Editor;

namespace Dwarf.Scripting
{
    /// <summary>
    /// Editor extension — provides fortress builtins to the tap-to-code editor.
    /// </summary>
    public class DwarfEditorExtension : IEditorExtension
    {
        public List<EditorTypeInfo> GetAvailableTypes() => new();

        public List<EditorFuncInfo> GetAvailableFunctions() => new()
        {
            // ── Queries ─────────────────────────────────────────
            new EditorFuncInfo { Name = "get_pop",         Hint = "total living dwarves",          ArgCount = 0 },
            new EditorFuncInfo { Name = "get_food",        Hint = "food stockpile",                ArgCount = 0 },
            new EditorFuncInfo { Name = "get_drink",       Hint = "drink stockpile",               ArgCount = 0 },
            new EditorFuncInfo { Name = "get_wood",        Hint = "wood stockpile",                ArgCount = 0 },
            new EditorFuncInfo { Name = "get_stone",       Hint = "stone stockpile",               ArgCount = 0 },
            new EditorFuncInfo { Name = "get_ore",         Hint = "ore stockpile",                 ArgCount = 0 },
            new EditorFuncInfo { Name = "get_bars",        Hint = "metal bars stockpile",          ArgCount = 0 },
            new EditorFuncInfo { Name = "get_goods",       Hint = "trade goods value",             ArgCount = 0 },
            new EditorFuncInfo { Name = "get_grid_width",  Hint = "fortress grid width",           ArgCount = 0 },
            new EditorFuncInfo { Name = "get_grid_depth",  Hint = "fortress depth (z-levels)",     ArgCount = 0 },
            new EditorFuncInfo { Name = "get_cell",        Hint = "cell type at (x, z)",           ArgCount = 2 },
            new EditorFuncInfo { Name = "get_idle",        Hint = "idle dwarves count",            ArgCount = 0 },
            new EditorFuncInfo { Name = "get_threat",      Hint = "0=none 1=ambush 2=siege",       ArgCount = 0 },
            new EditorFuncInfo { Name = "get_morale",      Hint = "dwarf happiness (0.0-1.0)",     ArgCount = 0 },
            new EditorFuncInfo { Name = "get_workshops",   Hint = "total workshop count",          ArgCount = 0 },
            new EditorFuncInfo { Name = "get_farms",       Hint = "farm plot count",               ArgCount = 0 },
            new EditorFuncInfo { Name = "get_military",    Hint = "military dwarves",              ArgCount = 0 },
            new EditorFuncInfo { Name = "get_season",      Hint = "0=spring 1=summer 2=autumn 3=winter", ArgCount = 0 },
            new EditorFuncInfo { Name = "get_year",        Hint = "current year number",           ArgCount = 0 },
            new EditorFuncInfo { Name = "get_score",       Hint = "fortress wealth score",         ArgCount = 0 },
            new EditorFuncInfo { Name = "get_game_over",   Hint = "1 if fortress abandoned",       ArgCount = 0 },
            new EditorFuncInfo { Name = "get_input",       Hint = "keyboard input code",           ArgCount = 0 },

            // ── Commands ────────────────────────────────────────
            new EditorFuncInfo { Name = "dig",              Hint = "excavate rock at (x,z) → +stone",     ArgCount = 2 },
            new EditorFuncInfo { Name = "build_stairs",     Hint = "place stairs at (x,z) → 1 stone",     ArgCount = 2 },
            new EditorFuncInfo { Name = "build_farm",       Hint = "designate farm plot at (x,z)",         ArgCount = 2 },
            new EditorFuncInfo { Name = "build_stockpile",  Hint = "designate stockpile at (x,z)",         ArgCount = 2 },
            new EditorFuncInfo { Name = "build_wall",       Hint = "construct wall at (x,z) → 1 stone",   ArgCount = 2 },
            new EditorFuncInfo { Name = "build_workshop",   Hint = "place workshop at (x,z,type) → 2 stone", ArgCount = 3 },
            new EditorFuncInfo { Name = "brew",             Hint = "1 food → 2 drink (needs Still)",       ArgCount = 0 },
            new EditorFuncInfo { Name = "smelt",            Hint = "1 ore → 1 bar (needs Smelter)",        ArgCount = 0 },
            new EditorFuncInfo { Name = "forge",            Hint = "1 bar → 5 goods (needs Forge)",        ArgCount = 0 },
            new EditorFuncInfo { Name = "craft",            Hint = "1 stone → 3 goods (needs Craftsdwarf)", ArgCount = 0 },
            new EditorFuncInfo { Name = "recruit",          Hint = "idle dwarf → military",                ArgCount = 0 },
            new EditorFuncInfo { Name = "raise_bridge",     Hint = "seal entrance (blocks sieges)",         ArgCount = 0 },
            new EditorFuncInfo { Name = "lower_bridge",     Hint = "open entrance",                        ArgCount = 0 },
        };

        public List<EditorMethodInfo> GetMethodsForType(string typeName) => new();

        public List<string> GetVariableNameSuggestions() => new()
        {
            "pop", "food", "drink", "wood", "stone", "ore", "bars", "goods",
            "morale", "threat", "idle", "military", "farms", "workshops",
            "season", "year", "x", "z", "mid", "cell"
        };

        public List<string> GetStringLiteralSuggestions() => new();
    }
}
