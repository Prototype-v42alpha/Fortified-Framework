using RimWorld;
using Verse;

namespace Fortified;

/// <summary>
/// UseEffect that triggers a RaidWave when a pawn activates the parent building.
/// Shows a confirm dialog describing the incoming wave before committing.
/// No Mechanitor or Biotech requirement — works with any pawn the caller permits.
/// </summary>
public class CompUseEffect_CallRaidWave : CompUseEffect
{
    private Effecter prepareEffecter;

    public CompProperties_UseEffect_CallRaidWave Props => (CompProperties_UseEffect_CallRaidWave)props;

    // -------------------------------------------------------------------------
    // Core use
    // -------------------------------------------------------------------------

    public override void DoEffect(Pawn usedBy)
    {
        base.DoEffect(usedBy);

        // Fire the one-shot completion effecter if configured
        if (Props.effecterDef != null)
        {
            Effecter e = new Effecter(Props.effecterDef);
            e.Trigger(new TargetInfo(parent.Position, parent.Map), TargetInfo.Invalid);
            e.Cleanup();
        }

        // Stop any looping prepare effecter
        prepareEffecter?.Cleanup();
        prepareEffecter = null;

        CallRaidWave();
    }

    private void CallRaidWave()
    {
        var comp = Current.Game.GetComponent<GameComponent_RaidWave>();
        if (comp == null)
        {
            Log.Error($"[FortifiedFramework] CompUseEffect_CallRaidWave: no GameComponent_RaidWave found on {parent.def.defName}.");
            return;
        }
        Props.raidWaveDef.Worker.Resolve(parent.Map);
    }

    // -------------------------------------------------------------------------
    // Pre-use confirm dialog
    // -------------------------------------------------------------------------

    /// <summary>
    /// Shown as a yes/no dialog before the use job is committed.
    /// Describes the wave composition the player is about to face.
    /// </summary>
    public override TaggedString ConfirmMessage(Pawn p)
    {
        var comp = Current.Game.GetComponent<GameComponent_RaidWave>();
        int timesCalled = comp?.NumTimesCalledRaidWave(Props.raidWaveDef) ?? 0;
        string waveDesc = Props.raidWaveDef.GetWaveDescription(timesCalled);

        return "FFF_RaidWave_ConfirmDialog".Translate(
            Props.raidWaveDef.label.Named("WAVE"),
            waveDesc.Named("PAWNS"));
    }

    // -------------------------------------------------------------------------
    // Prepare effecter (ticks during channel time)
    // -------------------------------------------------------------------------

    public override void PrepareTick()
    {
        if (Props.prepareEffecterDef != null && prepareEffecter == null)
            prepareEffecter = Props.prepareEffecterDef.Spawn(parent.Position, parent.MapHeld);

        prepareEffecter?.EffectTick(parent, TargetInfo.Invalid);
    }

    // -------------------------------------------------------------------------
    // Usability check
    // -------------------------------------------------------------------------

    /// <summary>
    /// Delegates to RaidWaveWorker.CanResolve() — handles cooldown and in-progress wave guards.
    /// Override RaidWaveWorker in the RaidWaveDef if additional restrictions are needed.
    /// </summary>
    public override AcceptanceReport CanBeUsedBy(Pawn p)
    {
        return Props.raidWaveDef.Worker.CanResolve();
    }

    // -------------------------------------------------------------------------
    // Cleanup
    // -------------------------------------------------------------------------

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        prepareEffecter?.Cleanup();
        prepareEffecter = null;
    }
}
