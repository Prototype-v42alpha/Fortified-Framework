// 当白昼倾坠之时
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Fortified
{
    // 骇入机兵休眠容器
    public class JobDriver_HackMechCapsule : JobDriver
    {
        private Building_MechCapsule Capsule => (Building_MechCapsule)job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Capsule, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => !Capsule.HasMech);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            Toil hackToil = Toils_General.WaitWith(TargetIndex.A, 500, true, true, false, TargetIndex.A);
            hackToil.WithEffect(EffecterDefOf.Hacking, TargetIndex.A);
            hackToil.WithProgressBarToilDelay(TargetIndex.A);
            yield return hackToil;

            yield return new Toil
            {
                initAction = delegate
                {
                    Capsule.ActivateMech(pawn);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }

    // 销毁机兵休眠容器中的机兵
    public class JobDriver_EjectMechCapsule : JobDriver
    {
        private Building_MechCapsule Capsule => (Building_MechCapsule)job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Capsule, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => !Capsule.HasMech);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            Toil workToil = Toils_General.WaitWith(TargetIndex.A, 250, true, true, false, TargetIndex.A);
            workToil.WithEffect(EffecterDefOf.ConstructMetal, TargetIndex.A);
            workToil.WithProgressBarToilDelay(TargetIndex.A);
            yield return workToil;

            yield return new Toil
            {
                initAction = delegate
                {
                    Capsule.EjectAndDestroy(pawn);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}
