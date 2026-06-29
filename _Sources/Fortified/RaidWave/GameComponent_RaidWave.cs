using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Fortified;

/// <summary>
/// Global game component tracking RaidWave state:
/// - Global cooldown timestamp (independent of GameComponent_Bossgroup)
/// - Per-def call count for wave escalation
/// </summary>
public class GameComponent_RaidWave : GameComponent
{
    /// <summary>TicksGame when the last RaidWave was triggered. Initialised far in the past so the first call is never blocked.</summary>
    public int lastRaidWaveCalled = -9999999;

    private Dictionary<RaidWaveDef, int> timesCalledRaidWaves = new Dictionary<RaidWaveDef, int>();

    // Scribe helpers
    private List<RaidWaveDef> _defKeys;
    private List<int> _countValues;

    public GameComponent_RaidWave(Game game) { }

    /// <summary>Returns how many times the given RaidWaveDef has been triggered this save.</summary>
    public int NumTimesCalledRaidWave(RaidWaveDef def)
    {
        return timesCalledRaidWaves.TryGetValue(def, out int count) ? count : 0;
    }

    /// <summary>Records a RaidWave trigger: updates the global cooldown and increments per-def count.</summary>
    public void Notify_RaidWaveCalled(RaidWaveDef def)
    {
        lastRaidWaveCalled = Find.TickManager.TicksGame;

        if (timesCalledRaidWaves.ContainsKey(def))
            timesCalledRaidWaves[def]++;
        else
            timesCalledRaidWaves[def] = 1;
    }

    /// <summary>Resets all wave counts and cooldown (useful for debug/scenario resets).</summary>
    public void DebugReset()
    {
        lastRaidWaveCalled = -9999999;
        timesCalledRaidWaves.Clear();
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref lastRaidWaveCalled, "lastRaidWaveCalled", -9999999);
        Scribe_Collections.Look(ref timesCalledRaidWaves, "timesCalledRaidWaves",
            LookMode.Def, LookMode.Value,
            ref _defKeys, ref _countValues);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            timesCalledRaidWaves ??= new Dictionary<RaidWaveDef, int>();
        }
    }
}
