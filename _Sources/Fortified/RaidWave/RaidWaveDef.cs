using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Fortified;

/// <summary>
/// Defines a raid wave event: a series of escalating PawnKind groups that spawn
/// as a hostile attack. Unlike BossgroupDef, requires no BossDef or summoning building.
/// Triggered programmatically via RaidWaveWorker.Resolve().
/// </summary>
public class RaidWaveDef : Def
{
    /// <summary>Faction that the raiders belong to. Required.</summary>
    public FactionDef factionDef;

    /// <summary>Ordered list of waves. Each call escalates to the next; repeats from repeatWaveStartIndex thereafter.</summary>
    public List<RaidWave> waves = new List<RaidWave>();

    /// <summary>After exhausting the wave list, subsequent calls randomly pick from [repeatWaveStartIndex, waves.Count).</summary>
    public int repeatWaveStartIndex;

    /// <summary>Optional custom worker class for override of resolve/cooldown logic.</summary>
    public Type workerClass = typeof(RaidWaveWorker);

    /// <summary>Quest script to run when this RaidWave is triggered. Defaults to FFF_RaidWave_Default.</summary>
    public QuestScriptDef quest;

    private RaidWaveWorker workerInt;

    public RaidWaveWorker Worker
    {
        get
        {
            if (workerInt == null)
            {
                workerInt = (RaidWaveWorker)Activator.CreateInstance(workerClass);
                workerInt.def = this;
            }
            return workerInt;
        }
    }

    /// <summary>
    /// Returns the actual wave list index for the given call count.
    /// Beyond the list, picks randomly within the repeat range (seeded for determinism).
    /// </summary>
    public int GetWaveIndex(int timesCalled)
    {
        if (timesCalled < waves.Count)
            return timesCalled;

        Rand.PushState(Gen.HashCombine(Find.World.info.Seed, timesCalled));
        int idx = Rand.Range(repeatWaveStartIndex, waves.Count);
        Rand.PopState();
        return idx;
    }

    public RaidWave GetWave(int timesCalled) => waves[GetWaveIndex(timesCalled)];

    /// <summary>
    /// Returns a human-readable list of pawn kinds and counts for the given call count.
    /// Used by the confirm dialog before the player triggers the UseEffect.
    /// </summary>
    public string GetWaveDescription(int timesCalled)
    {
        RaidWave wave = GetWave(timesCalled);
        return wave.pawns
            .Where(e => e.kindDef != null && e.count > 0)
            .Select(e => $"{GenLabel.BestKindLabel(e.kindDef, Gender.None).CapitalizeFirst()} x{e.count}")
            .ToLineList("  - ");
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string e in base.ConfigErrors())
            yield return e;

        if (factionDef == null)
            yield return $"{defName}: factionDef is required.";

        if (waves.NullOrEmpty())
            yield return $"{defName}: no waves defined.";

        if (!waves.NullOrEmpty() && repeatWaveStartIndex >= waves.Count)
            yield return $"{defName}: repeatWaveStartIndex ({repeatWaveStartIndex}) must be less than waves.Count ({waves.Count}).";
    }
}
