using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Fortified
{
    public class JobDriver_Replenish : JobDriver
    {
        private const int DurationTicks = 600;

        private Thing Material => job.GetTarget(TargetIndex.A).Thing;
        private Thing PlateThing => job.GetTarget(TargetIndex.B).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (pawn.Reserve(Material, job, 1, -1, null, errorOnFailed))
            {
                return pawn.Reserve(PlateThing, job, 1, -1, null, errorOnFailed);
            }
            return false;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Go to material
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch)
                .FailOnDespawnedOrNull(TargetIndex.A);

            // Pick up material
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);

            // Go to plate (apparel)
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch)
                .FailOnDespawnedOrNull(TargetIndex.B);

            // Wait while "installing" -- 目標改為 TargetIndex.B
            Toil wait = Toils_General.WaitWith(TargetIndex.B, DurationTicks, true, true,face:TargetIndex.B);
            wait.FailOnDespawnedOrNull(TargetIndex.B);
            wait.FailOnCannotTouch(TargetIndex.B, PathEndMode.Touch);
            wait.WithEffect(EffecterDefOf.ConstructMetal, TargetIndex.B);
            yield return wait;

            // Do the refill action
            yield return Toils_General.Do(PerformReplenish);
        }

        private void PerformReplenish()
        {
            Thing plateThing = job.GetTarget(TargetIndex.B).Thing;
            if (plateThing == null || !plateThing.Spawned) return;

            // 從實際的 comps（ThingWithComps.AllComps）尋找實作 IReplenishable 的 comp
            IReplenishable replenishable = null;
            if (plateThing is ThingWithComps twc && !twc.AllComps.NullOrEmpty())
            {
                foreach (var comp in twc.AllComps)
                {
                    if (comp is IReplenishable r)
                    {
                        replenishable = r;
                        break;
                    }
                }
            }
            if (replenishable == null) return;

            Thing carried = pawn.carryTracker?.CarriedThing;
            if (carried == null) return;

            int needed = replenishable.GetMaterialCostForRefill();
            int available = carried.stackCount;
            int use = Mathf.Min(needed, available);

            Thing used = carried.SplitOff(use);
            used.Destroy();

            replenishable.Replenish(pawn, use);
        }
    }
}