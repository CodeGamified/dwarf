// Copyright CodeGamified 2025-2026
// MIT License — Dwarf
using CodeGamified.Engine;
using CodeGamified.Time;
using Dwarf.Core;
using Dwarf.Game;

namespace Dwarf.Scripting
{
    /// <summary>
    /// Game I/O handler — bridges CUSTOM opcodes to fortress/match state.
    /// One switch case per opcode.
    /// </summary>
    public class DwarfIOHandler : IGameIOHandler
    {
        private readonly DwarfMatchManager _match;
        private readonly FortressGrid _fortress;

        public DwarfIOHandler(DwarfMatchManager match, FortressGrid fortress)
        {
            _match = match;
            _fortress = fortress;
        }

        public bool PreExecute(Instruction inst, MachineState state) => true;

        public void ExecuteIO(Instruction inst, MachineState state)
        {
            int op = (int)inst.Op - (int)OpCode.CUSTOM_0;

            switch ((DwarfOpCode)op)
            {
                // ── Queries → R0 ────────────────────────────────

                case DwarfOpCode.GET_POP:
                    state.SetRegister(0, _match.Population);
                    break;
                case DwarfOpCode.GET_FOOD:
                    state.SetRegister(0, _match.Food);
                    break;
                case DwarfOpCode.GET_DRINK:
                    state.SetRegister(0, _match.Drink);
                    break;
                case DwarfOpCode.GET_WOOD:
                    state.SetRegister(0, _match.Wood);
                    break;
                case DwarfOpCode.GET_STONE:
                    state.SetRegister(0, _match.Stone);
                    break;
                case DwarfOpCode.GET_ORE:
                    state.SetRegister(0, _match.Ore);
                    break;
                case DwarfOpCode.GET_BARS:
                    state.SetRegister(0, _match.Bars);
                    break;
                case DwarfOpCode.GET_GOODS:
                    state.SetRegister(0, _match.Goods);
                    break;
                case DwarfOpCode.GET_GRID_W:
                    state.SetRegister(0, _fortress.Width);
                    break;
                case DwarfOpCode.GET_GRID_H:
                    state.SetRegister(0, _fortress.Depth);
                    break;
                case DwarfOpCode.GET_SCORE:
                    state.SetRegister(0, _match.Score);
                    break;
                case DwarfOpCode.GET_GAME_OVER:
                    state.SetRegister(0, _match.GameOver ? 1f : 0f);
                    break;
                case DwarfOpCode.GET_INPUT:
                    state.SetRegister(0, DwarfInputProvider.Instance != null
                        ? DwarfInputProvider.Instance.CurrentInput : 0f);
                    break;
                case DwarfOpCode.GET_IDLE:
                    state.SetRegister(0, _match.IdleDwarves);
                    break;
                case DwarfOpCode.GET_THREAT:
                    state.SetRegister(0, (int)_match.Threat);
                    break;
                case DwarfOpCode.GET_MORALE:
                    state.SetRegister(0, _match.Morale);
                    break;
                case DwarfOpCode.GET_WORKSHOPS:
                    state.SetRegister(0, _fortress.TotalWorkshops());
                    break;
                case DwarfOpCode.GET_FARMS:
                    state.SetRegister(0, _fortress.CountCells(CellType.Farm));
                    break;
                case DwarfOpCode.GET_MILITARY:
                    state.SetRegister(0, _match.Military);
                    break;
                case DwarfOpCode.GET_SEASON:
                {
                    var simTime = SimulationTime.Instance as DwarfSimulationTime;
                    state.SetRegister(0, simTime != null ? simTime.GetSeason() : 0f);
                    break;
                }
                case DwarfOpCode.GET_YEAR:
                {
                    var simTime = SimulationTime.Instance as DwarfSimulationTime;
                    state.SetRegister(0, simTime != null ? simTime.GetYear() : 1f);
                    break;
                }

                // ── Two-arg query: R0=x, R1=z → R0 ─────────────

                case DwarfOpCode.GET_CELL:
                    state.SetRegister(0, (int)_fortress.GetCell(
                        (int)state.GetRegister(0), (int)state.GetRegister(1)));
                    break;

                // ── Two-arg commands: R0=x, R1=z → R0 ──────────

                case DwarfOpCode.DIG:
                    state.SetRegister(0, _match.DigCell(
                        (int)state.GetRegister(0), (int)state.GetRegister(1)));
                    break;
                case DwarfOpCode.BUILD_STAIRS:
                    state.SetRegister(0, _match.BuildStairs(
                        (int)state.GetRegister(0), (int)state.GetRegister(1)));
                    break;
                case DwarfOpCode.BUILD_FARM:
                    state.SetRegister(0, _match.BuildFarm(
                        (int)state.GetRegister(0), (int)state.GetRegister(1)));
                    break;
                case DwarfOpCode.BUILD_STOCKPILE:
                    state.SetRegister(0, _match.BuildStockpile(
                        (int)state.GetRegister(0), (int)state.GetRegister(1)));
                    break;
                case DwarfOpCode.BUILD_WALL:
                    state.SetRegister(0, _match.BuildWall(
                        (int)state.GetRegister(0), (int)state.GetRegister(1)));
                    break;

                // ── Three-arg command: R0=x, R1=z, R2=type → R0 ─

                case DwarfOpCode.BUILD_WORKSHOP:
                    state.SetRegister(0, _match.BuildWorkshop(
                        (int)state.GetRegister(0), (int)state.GetRegister(1),
                        (int)state.GetRegister(2)));
                    break;

                // ── No-arg commands → R0 ────────────────────────

                case DwarfOpCode.BREW:
                    state.SetRegister(0, _match.Brew());
                    break;
                case DwarfOpCode.SMELT:
                    state.SetRegister(0, _match.Smelt());
                    break;
                case DwarfOpCode.FORGE:
                    state.SetRegister(0, _match.Forge());
                    break;
                case DwarfOpCode.CRAFT:
                    state.SetRegister(0, _match.Craft());
                    break;
                case DwarfOpCode.RECRUIT:
                    state.SetRegister(0, _match.Recruit());
                    break;
                case DwarfOpCode.RAISE_BRIDGE:
                    state.SetRegister(0, _match.RaiseBridge());
                    break;
                case DwarfOpCode.LOWER_BRIDGE:
                    state.SetRegister(0, _match.LowerBridge());
                    break;
            }
        }

        public float GetTimeScale() => SimulationTime.Instance?.timeScale ?? 1f;
        public double GetSimulationTime() => SimulationTime.Instance?.simulationTime ?? 0.0;
    }
}
