using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Fortified;

/// <summary>
/// CompProperties for a useable building that triggers a RaidWave when activated.
/// Attach to any ThingDef that has CompUseable in its comps list.
/// </summary>
public class CompProperties_UseEffect_CallRaidWave : CompProperties_UseEffect
{
    /// <summary>The RaidWave definition to trigger on use. Required.</summary>
    public RaidWaveDef raidWaveDef;

    /// <summary>Optional one-shot effecter played on the building when the use completes.</summary>
    public EffecterDef effecterDef;

    /// <summary>Optional looping effecter ticked on the building while the use is being prepared (channel time).</summary>
    public EffecterDef prepareEffecterDef;

    public CompProperties_UseEffect_CallRaidWave()
    {
        compClass = typeof(CompUseEffect_CallRaidWave);
    }

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string e in base.ConfigErrors(parentDef))
            yield return e;

        if (raidWaveDef == null)
            yield return $"{parentDef.defName}: CompProperties_UseEffect_CallRaidWave requires a raidWaveDef.";
    }
}
