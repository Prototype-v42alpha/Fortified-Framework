using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Fortified;

/// <summary>
/// Quest part that creates a LordJob_AssaultColony for all RaidWave pawns once they land.
/// The base QuestPart_MakeLord handles signal wiring, pawn tracking, faction, mapParent,
/// and ExposeData — this class only needs to define which LordJob to create.
/// </summary>
public class QuestPart_RaidWave : QuestPart_MakeLord
{
    protected override Lord MakeLord()
    {
        // canKidnap/canSteal/canTimeoutOrFlee false: these raiders fight until defeated.
        LordJob_AssaultColony lordJob = new LordJob_AssaultColony(
            assaulterFaction: faction,
            canKidnap: false,
            canTimeoutOrFlee: false,
            canSteal: false);
        return LordMaker.MakeNewLord(faction, lordJob, mapParent.Map);
    }
}
