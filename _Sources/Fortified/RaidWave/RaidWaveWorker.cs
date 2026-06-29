using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Fortified;

/// <summary>
/// Handles the logic for triggering a RaidWave: cooldown checks, map-state guards,
/// and Quest generation. Subclass and set workerClass on RaidWaveDef to override.
/// </summary>
public class RaidWaveWorker
{
    /// <summary>Global cooldown between any two RaidWave calls, in ticks (2 days).</summary>
    public const int GlobalCooldownTicks = 120000;

    public RaidWaveDef def;

    /// <summary>
    /// Returns whether this RaidWave can currently be triggered.
    /// Checks the global RaidWave cooldown stored in GameComponent_RaidWave.
    /// </summary>
    public virtual AcceptanceReport CanResolve()
    {
        var comp = Current.Game.GetComponent<GameComponent_RaidWave>();
        if (comp == null)
            return "FFF_RaidWave_NoGameComponent".Translate();

        int ticksSinceLast = Find.TickManager.TicksGame - comp.lastRaidWaveCalled;
        if (ticksSinceLast < GlobalCooldownTicks)
        {
            int remaining = GlobalCooldownTicks - ticksSinceLast;
            return "FFF_RaidWave_Cooldown".Translate(remaining.ToStringTicksToPeriod());
        }

        return true;
    }

    /// <summary>
    /// Returns whether the map is in an appropriate state to receive the raid right now.
    /// Blocks if traders, diplomats, or bestowing ceremonies are present (same logic as BossgroupWorker).
    /// </summary>
    public virtual AcceptanceReport ShouldSummonNow(Map map)
    {
        foreach (Lord lord in map.lordManager.lords)
        {
            if (lord.LordJob is LordJob_TradeWithColony
                || lord.LordJob is LordJob_BestowingCeremony
                || lord.LordJob is LordJob_VisitColony)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Triggers the RaidWave: generates the Quest and records the call in GameComponent_RaidWave.
    /// Callers should check CanResolve() first; this method does not enforce it.
    /// </summary>
    public virtual void Resolve(Map map)
    {
        var comp = Current.Game.GetComponent<GameComponent_RaidWave>();
        if (comp == null)
        {
            Log.Error($"[FortifiedFramework] RaidWaveWorker.Resolve: no GameComponent_RaidWave found.");
            return;
        }

        QuestScriptDef questScript = def.quest ?? DefDatabase<QuestScriptDef>.GetNamedSilentFail("FFF_RaidWave_Default");
        if (questScript == null)
        {
            Log.Error($"[FortifiedFramework] RaidWaveWorker.Resolve: no QuestScriptDef found for {def.defName} and FFF_RaidWave_Default is missing.");
            return;
        }

        int timesCalled = comp.NumTimesCalledRaidWave(def);

        Slate slate = new Slate();
        slate.Set("raidwave", def);
        slate.Set("map", map);
        slate.Set("wave", timesCalled);

        comp.Notify_RaidWaveCalled(def);
        QuestUtility.GenerateQuestAndMakeAvailable(questScript, slate);
    }
}
