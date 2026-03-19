// Copyright CodeGamified 2025-2026
// MIT License — Dwarf
using System.Collections.Generic;
using UnityEngine;
using CodeGamified.Engine;
using CodeGamified.Engine.Runtime;
using CodeGamified.TUI;
using Dwarf.Scripting;
using static Dwarf.Scripting.DwarfOpCode;

namespace Dwarf.UI
{
    /// <summary>
    /// Adapts a DwarfProgram into the engine's IDebuggerDataSource contract.
    /// Fed to DebuggerSourcePanel, DebuggerMachinePanel, DebuggerStatePanel.
    /// </summary>
    public class DwarfDebuggerData : IDebuggerDataSource
    {
        private readonly DwarfProgram _program;
        private readonly string _label;

        public DwarfDebuggerData(DwarfProgram program, string label = null)
        {
            _program = program;
            _label = label;
        }

        // ── IDebuggerDataSource ─────────────────────────────────

        public string ProgramName => _label ?? _program?.ProgramName ?? "Dwarf";
        public string[] SourceLines => _program?.Program?.SourceLines;
        public bool HasLiveProgram =>
            _program != null && _program.Executor != null && _program.Program != null
            && _program.Program.Instructions != null && _program.Program.Instructions.Length > 0;
        public int PC
        {
            get
            {
                var s = _program?.State;
                if (s == null) return 0;
                return s.LastExecutedPC >= 0 ? s.LastExecutedPC : s.PC;
            }
        }
        public long CycleCount => _program?.State?.CycleCount ?? 0;

        public string StatusString
        {
            get
            {
                if (_program == null || _program.Executor == null)
                    return TUIColors.Dimmed("NO PROGRAM");
                var state = _program.State;
                if (state == null) return TUIColors.Dimmed("NO STATE");
                int instCount = _program.Program?.Instructions?.Length ?? 0;
                return TUIColors.Fg(TUIColors.BrightGreen, $"TICK {instCount} inst");
            }
        }

        public List<string> BuildSourceLines(int pc, int scrollOffset, int maxRows)
        {
            var lines = new List<string>();
            var src = SourceLines;
            if (src == null) return lines;

            int activeLine = -1;
            int activeEnd = -1;
            bool isHalt = false;
            Instruction activeInst = default;
            if (HasLiveProgram && _program.Program.Instructions.Length > 0
                && pc < _program.Program.Instructions.Length)
            {
                activeInst = _program.Program.Instructions[pc];
                activeLine = activeInst.SourceLine - 1;
                isHalt = activeInst.Op == OpCode.HALT;
                if (activeLine >= 0)
                    activeEnd = SourceHighlight.GetContinuationEnd(src, activeLine);
            }

            // Synthetic "while True:" at display row 0
            if (scrollOffset == 0 && lines.Count < maxRows)
            {
                string whileLine = "while True:";
                if (isHalt)
                    lines.Add(TUIColors.Fg(TUIColors.BrightGreen, $"  {TUIGlyphs.ArrowR}   {whileLine}"));
                else
                    lines.Add($"  {TUIColors.Dimmed(TUIGlyphs.ArrowR)}   {SynthwaveHighlighter.Highlight(whileLine)}");
            }

            // Find the ONE line that contains the active token
            int tokenLine = -1;
            if (activeLine >= 0)
            {
                string token = SourceHighlight.GetSourceToken(activeInst);
                if (token != null)
                {
                    for (int k = activeLine; k <= activeEnd; k++)
                    {
                        if (src[k].IndexOf(token) >= 0) { tokenLine = k; break; }
                    }
                }
                if (tokenLine < 0) tokenLine = activeLine;
            }

            // Auto-scroll to keep active source line visible
            int focusLine = tokenLine >= 0 ? tokenLine : activeLine;
            if (focusLine >= 0 && src.Length > maxRows)
                scrollOffset = Mathf.Clamp(focusLine - maxRows / 3, 0, src.Length - maxRows);

            for (int i = scrollOffset; i < src.Length && lines.Count < maxRows; i++)
            {
                if (i == tokenLine)
                {
                    lines.Add(SourceHighlight.HighlightActiveLine(
                        src[i], $" {i + 1:D3}      ", activeInst));
                }
                else
                {
                    string num = TUIColors.Dimmed($"{i + 1:D3}");
                    lines.Add($" {num}      {SynthwaveHighlighter.Highlight(src[i])}");
                }
            }
            return lines;
        }

        public List<string> BuildMachineLines(int pc, int maxRows)
        {
            var lines = new List<string>();
            if (!HasLiveProgram) return lines;

            var instructions = _program.Program.Instructions;
            int total = instructions.Length;

            int offset = 0;
            if (total > maxRows)
                offset = Mathf.Clamp(pc - maxRows / 3, 0, total - maxRows);
            int visibleCount = Mathf.Min(maxRows, total);

            for (int j = 0; j < visibleCount; j++)
            {
                int i = offset + j;
                var inst = instructions[i];
                bool isPC = (i == pc);
                string asm = inst.ToAssembly(FormatDwarfOp);
                if (isPC)
                {
                    lines.Add(TUIColors.Fg(TUIColors.BrightGreen, $" {i:X3}  {asm}"));
                }
                else
                {
                    string addr = TUIColors.Dimmed($"{i:X3}");
                    lines.Add($" {addr}  {SynthwaveHighlighter.HighlightAsm(asm)}");
                }
            }
            return lines;
        }

        public List<string> BuildStateLines()
        {
            if (!HasLiveProgram) return new List<string>();
            var s = _program.State;
            int displayPC = s.LastExecutedPC >= 0 ? s.LastExecutedPC : s.PC;
            return TUIWidgets.BuildStateLines(
                s.Registers, s.LastRegisterModified,
                s.Flags, displayPC, s.Stack.Count,
                s.NameToAddress, s.Memory);
        }

        // ── Custom opcode formatting ────────────────────────────

        static string FormatDwarfOp(Instruction inst)
        {
            int id = (int)inst.Op - (int)OpCode.CUSTOM_0;
            return (DwarfOpCode)id switch
            {
                // Queries (no args → R0)
                GET_POP           => "INP R0, POP",
                GET_FOOD          => "INP R0, FOOD",
                GET_DRINK         => "INP R0, DRINK",
                GET_WOOD          => "INP R0, WOOD",
                GET_STONE         => "INP R0, STONE",
                GET_ORE           => "INP R0, ORE",
                GET_BARS          => "INP R0, BARS",
                GET_GOODS         => "INP R0, GOODS",
                GET_GRID_W        => "INP R0, GRID_W",
                GET_GRID_H        => "INP R0, GRID_H",
                GET_SCORE         => "INP R0, SCORE",
                GET_GAME_OVER     => "INP R0, GAMEOVER",
                GET_INPUT         => "INP R0, INPUT",
                GET_IDLE          => "INP R0, IDLE",
                GET_THREAT        => "INP R0, THREAT",
                GET_MORALE        => "INP R0, MORALE",
                GET_WORKSHOPS     => "INP R0, WORKSHOPS",
                GET_FARMS         => "INP R0, FARMS",
                GET_MILITARY      => "INP R0, MILITARY",
                GET_SEASON        => "INP R0, SEASON",
                GET_YEAR          => "INP R0, YEAR",

                // Two-arg query
                GET_CELL          => "INP R0, CELL(R0,R1)",

                // Two-arg commands
                DIG               => "OUT DIG(R0,R1)",
                BUILD_STAIRS      => "OUT STAIRS(R0,R1)",
                BUILD_FARM        => "OUT FARM(R0,R1)",
                BUILD_STOCKPILE   => "OUT STOCKPILE(R0,R1)",
                BUILD_WALL        => "OUT WALL(R0,R1)",

                // Three-arg command
                BUILD_WORKSHOP    => "OUT WORKSHOP(R0,R1,R2)",

                // No-arg commands
                BREW              => "OUT BREW",
                SMELT             => "OUT SMELT",
                FORGE             => "OUT FORGE",
                CRAFT             => "OUT CRAFT",
                RECRUIT           => "OUT RECRUIT",
                RAISE_BRIDGE      => "OUT BRIDGE_UP",
                LOWER_BRIDGE      => "OUT BRIDGE_DN",

                _                 => $"CUSTOM_{id}",
            };
        }
    }
}
