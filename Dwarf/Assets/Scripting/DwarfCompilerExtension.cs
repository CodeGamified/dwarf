// Copyright CodeGamified 2025-2026
// MIT License — Dwarf
using System.Collections.Generic;
using CodeGamified.Engine;
using CodeGamified.Engine.Compiler;

namespace Dwarf.Scripting
{
    /// <summary>
    /// Opcode enum for Dwarf — fortress queries and overseer commands.
    ///
    /// Convention:
    ///   - Queries first (read fortress state → R0)
    ///   - Commands last (act on fortress, result → R0: 1=success, 0=fail)
    ///
    /// Grid: X = horizontal (0..Width-1), Z = depth (0=surface, deeper = higher Z).
    /// </summary>
    public enum DwarfOpCode
    {
        // ── Queries: no args, result in R0 ───────────────────
        GET_POP          = 0,   // total living dwarves
        GET_FOOD         = 1,   // food stockpile
        GET_DRINK        = 2,   // drink stockpile
        GET_WOOD         = 3,   // wood stockpile
        GET_STONE        = 4,   // stone stockpile
        GET_ORE          = 5,   // ore stockpile
        GET_BARS         = 6,   // metal bars stockpile
        GET_GOODS        = 7,   // trade goods value
        GET_GRID_W       = 8,   // fortress width
        GET_GRID_H       = 9,   // fortress depth (z-levels)
        GET_SCORE        = 10,  // fortress wealth score
        GET_GAME_OVER    = 11,  // 1 if fortress abandoned
        GET_INPUT        = 12,  // keyboard input code
        GET_IDLE         = 13,  // idle dwarves count
        GET_THREAT       = 14,  // 0=none, 1=ambush, 2=siege
        GET_MORALE       = 15,  // average morale (0.0-1.0)
        GET_WORKSHOPS    = 16,  // total workshop count
        GET_FARMS        = 17,  // farm plot count
        GET_MILITARY     = 18,  // military dwarf count
        GET_SEASON       = 19,  // 0=spring, 1=summer, 2=autumn, 3=winter
        GET_YEAR         = 20,  // current year number

        // ── Two-arg query: R0=x, R1=z → R0=cell type ────────
        GET_CELL         = 21,

        // ── Commands: args in R0 (and R1), result in R0 ──────
        DIG              = 22,  // dig(x, z) → excavate rock
        BUILD_STAIRS     = 23,  // build_stairs(x, z) → place staircase
        BUILD_FARM       = 24,  // build_farm(x, z) → designate farm
        BUILD_STOCKPILE  = 25,  // build_stockpile(x, z) → designate storage
        BUILD_WALL       = 26,  // build_wall(x, z) → construct wall
        BREW             = 27,  // brew() → 1 food → 2 drink
        SMELT            = 28,  // smelt() → 1 ore → 1 bar
        FORGE            = 29,  // forge() → 1 bar → 5 goods
        CRAFT            = 30,  // craft() → 1 stone → 3 goods
        RECRUIT          = 31,  // recruit() → idle dwarf → military

        // ── Extended commands (use CUSTOM_0 + value) ─────────
        // These overflow into the 32+ range but the engine supports it
        BUILD_WORKSHOP   = 32,  // build_workshop(x, z, type) → place workshop
        RAISE_BRIDGE     = 33,  // raise_bridge() → seal entrance
        LOWER_BRIDGE     = 34,  // lower_bridge() → open entrance
    }

    /// <summary>
    /// Compiler extension — maps Python builtin names to CUSTOM opcodes.
    /// One switch case per builtin function.
    /// </summary>
    public class DwarfCompilerExtension : ICompilerExtension
    {
        public void RegisterBuiltins(CompilerContext ctx) { }

        public bool TryCompileCall(string functionName, List<AstNodes.ExprNode> args,
                                   CompilerContext ctx, int sourceLine)
        {
            switch (functionName)
            {
                // ══════════════════════════════════════════════════
                // QUERIES: no args, result in R0
                // ══════════════════════════════════════════════════

                case "get_pop":
                    Emit(ctx, DwarfOpCode.GET_POP, sourceLine, "get_pop → R0");
                    return true;
                case "get_food":
                    Emit(ctx, DwarfOpCode.GET_FOOD, sourceLine, "get_food → R0");
                    return true;
                case "get_drink":
                    Emit(ctx, DwarfOpCode.GET_DRINK, sourceLine, "get_drink → R0");
                    return true;
                case "get_wood":
                    Emit(ctx, DwarfOpCode.GET_WOOD, sourceLine, "get_wood → R0");
                    return true;
                case "get_stone":
                    Emit(ctx, DwarfOpCode.GET_STONE, sourceLine, "get_stone → R0");
                    return true;
                case "get_ore":
                    Emit(ctx, DwarfOpCode.GET_ORE, sourceLine, "get_ore → R0");
                    return true;
                case "get_bars":
                    Emit(ctx, DwarfOpCode.GET_BARS, sourceLine, "get_bars → R0");
                    return true;
                case "get_goods":
                    Emit(ctx, DwarfOpCode.GET_GOODS, sourceLine, "get_goods → R0");
                    return true;
                case "get_grid_width":
                    Emit(ctx, DwarfOpCode.GET_GRID_W, sourceLine, "get_grid_width → R0");
                    return true;
                case "get_grid_depth":
                    Emit(ctx, DwarfOpCode.GET_GRID_H, sourceLine, "get_grid_depth → R0");
                    return true;
                case "get_score":
                    Emit(ctx, DwarfOpCode.GET_SCORE, sourceLine, "get_score → R0");
                    return true;
                case "get_game_over":
                    Emit(ctx, DwarfOpCode.GET_GAME_OVER, sourceLine, "get_game_over → R0");
                    return true;
                case "get_input":
                    Emit(ctx, DwarfOpCode.GET_INPUT, sourceLine, "get_input → R0");
                    return true;
                case "get_idle":
                    Emit(ctx, DwarfOpCode.GET_IDLE, sourceLine, "get_idle → R0");
                    return true;
                case "get_threat":
                    Emit(ctx, DwarfOpCode.GET_THREAT, sourceLine, "get_threat → R0");
                    return true;
                case "get_morale":
                    Emit(ctx, DwarfOpCode.GET_MORALE, sourceLine, "get_morale → R0");
                    return true;
                case "get_workshops":
                    Emit(ctx, DwarfOpCode.GET_WORKSHOPS, sourceLine, "get_workshops → R0");
                    return true;
                case "get_farms":
                    Emit(ctx, DwarfOpCode.GET_FARMS, sourceLine, "get_farms → R0");
                    return true;
                case "get_military":
                    Emit(ctx, DwarfOpCode.GET_MILITARY, sourceLine, "get_military → R0");
                    return true;
                case "get_season":
                    Emit(ctx, DwarfOpCode.GET_SEASON, sourceLine, "get_season → R0");
                    return true;
                case "get_year":
                    Emit(ctx, DwarfOpCode.GET_YEAR, sourceLine, "get_year → R0");
                    return true;

                // ══════════════════════════════════════════════════
                // TWO-ARG QUERY: get_cell(x, z) → R0
                // ══════════════════════════════════════════════════

                case "get_cell":
                    if (args != null && args.Count >= 2)
                    {
                        args[0].Compile(ctx);                       // x → R0
                        ctx.Emit(OpCode.PUSH, 0);                  // save R0
                        args[1].Compile(ctx);                       // z → R0
                        ctx.Emit(OpCode.MOV, 1, 0);                // R0 → R1
                        ctx.Emit(OpCode.POP, 0);                   // restore x → R0
                    }
                    Emit(ctx, DwarfOpCode.GET_CELL, sourceLine, "get_cell(R0=x, R1=z) → R0");
                    return true;

                // ══════════════════════════════════════════════════
                // TWO-ARG COMMANDS: dig(x, z), build_*(x, z)
                // ══════════════════════════════════════════════════

                case "dig":
                    CompileTwoArgs(args, ctx);
                    Emit(ctx, DwarfOpCode.DIG, sourceLine, "dig(R0=x, R1=z) → R0");
                    return true;
                case "build_stairs":
                    CompileTwoArgs(args, ctx);
                    Emit(ctx, DwarfOpCode.BUILD_STAIRS, sourceLine, "build_stairs(R0=x, R1=z) → R0");
                    return true;
                case "build_farm":
                    CompileTwoArgs(args, ctx);
                    Emit(ctx, DwarfOpCode.BUILD_FARM, sourceLine, "build_farm(R0=x, R1=z) → R0");
                    return true;
                case "build_stockpile":
                    CompileTwoArgs(args, ctx);
                    Emit(ctx, DwarfOpCode.BUILD_STOCKPILE, sourceLine, "build_stockpile(R0=x, R1=z) → R0");
                    return true;
                case "build_wall":
                    CompileTwoArgs(args, ctx);
                    Emit(ctx, DwarfOpCode.BUILD_WALL, sourceLine, "build_wall(R0=x, R1=z) → R0");
                    return true;

                // ══════════════════════════════════════════════════
                // THREE-ARG COMMAND: build_workshop(x, z, type)
                // ══════════════════════════════════════════════════

                case "build_workshop":
                    if (args != null && args.Count >= 3)
                    {
                        // Compile x → R0, push
                        args[0].Compile(ctx);
                        ctx.Emit(OpCode.PUSH, 0);
                        // Compile z → R0, push
                        args[1].Compile(ctx);
                        ctx.Emit(OpCode.PUSH, 0);
                        // Compile type → R0, move to R2
                        args[2].Compile(ctx);
                        ctx.Emit(OpCode.MOV, 2, 0);
                        // Pop z → R1
                        ctx.Emit(OpCode.POP, 0);
                        ctx.Emit(OpCode.MOV, 1, 0);
                        // Pop x → R0
                        ctx.Emit(OpCode.POP, 0);
                    }
                    Emit(ctx, DwarfOpCode.BUILD_WORKSHOP, sourceLine, "build_workshop(R0=x, R1=z, R2=type) → R0");
                    return true;

                // ══════════════════════════════════════════════════
                // NO-ARG COMMANDS
                // ══════════════════════════════════════════════════

                case "brew":
                    Emit(ctx, DwarfOpCode.BREW, sourceLine, "brew → R0");
                    return true;
                case "smelt":
                    Emit(ctx, DwarfOpCode.SMELT, sourceLine, "smelt → R0");
                    return true;
                case "forge":
                    Emit(ctx, DwarfOpCode.FORGE, sourceLine, "forge → R0");
                    return true;
                case "craft":
                    Emit(ctx, DwarfOpCode.CRAFT, sourceLine, "craft → R0");
                    return true;
                case "recruit":
                    Emit(ctx, DwarfOpCode.RECRUIT, sourceLine, "recruit → R0");
                    return true;
                case "raise_bridge":
                    Emit(ctx, DwarfOpCode.RAISE_BRIDGE, sourceLine, "raise_bridge → R0");
                    return true;
                case "lower_bridge":
                    Emit(ctx, DwarfOpCode.LOWER_BRIDGE, sourceLine, "lower_bridge → R0");
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>Compile two args into R0 and R1 using push/pop pattern.</summary>
        private static void CompileTwoArgs(List<AstNodes.ExprNode> args, CompilerContext ctx)
        {
            if (args != null && args.Count >= 2)
            {
                args[0].Compile(ctx);              // arg0 → R0
                ctx.Emit(OpCode.PUSH, 0);          // save R0
                args[1].Compile(ctx);              // arg1 → R0
                ctx.Emit(OpCode.MOV, 1, 0);        // R0 → R1
                ctx.Emit(OpCode.POP, 0);            // restore arg0 → R0
            }
        }

        private static void Emit(CompilerContext ctx, DwarfOpCode op, int line, string comment)
        {
            ctx.Emit(OpCode.CUSTOM_0 + (int)op, 0, 0, 0, line, comment);
        }

        public bool TryCompileMethodCall(string objectName, string methodName,
                                         List<AstNodes.ExprNode> args,
                                         CompilerContext ctx, int sourceLine) => false;

        public bool TryCompileObjectDecl(string typeName, string varName,
                                         List<AstNodes.ExprNode> constructorArgs,
                                         CompilerContext ctx, int sourceLine) => false;
    }
}
