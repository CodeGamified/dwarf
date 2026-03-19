// Copyright CodeGamified 2025-2026
// MIT License — Dwarf
using CodeGamified.TUI;
using Dwarf.Scripting;

namespace Dwarf.UI
{
    /// <summary>
    /// Thin adapter — wires a DwarfProgram into the engine's CodeDebuggerWindow
    /// via DwarfDebuggerData (IDebuggerDataSource). All rendering lives in the engine.
    /// </summary>
    public class DwarfCodeDebugger : CodeDebuggerWindow
    {
        protected override void Awake()
        {
            base.Awake();
            windowTitle = "CODE";
        }

        public void Bind(DwarfProgram program)
        {
            SetDataSource(new DwarfDebuggerData(program));
        }
    }
}
