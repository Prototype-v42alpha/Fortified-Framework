using System.Collections.Generic;
using RimWorld;

namespace Fortified;

/// <summary>
/// Defines a single wave of raiders in a RaidWaveDef.
/// Each wave specifies which pawn kinds spawn and in what quantities.
/// </summary>
public class RaidWave
{
    public List<PawnKindDefCount> pawns = new List<PawnKindDefCount>();
}
