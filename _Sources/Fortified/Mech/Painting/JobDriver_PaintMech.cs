using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;

namespace Fortified
{
    // 机械体涂装Job
    public class JobDriver_PaintMech : JobDriver
    {
        // TargetA = 机械体
        // TargetB = 染料

        private const int WorkTicks = 300;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetA, job, errorOnFailed: errorOnFailed)
                && pawn.Reserve(TargetB, job, errorOnFailed: errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);

            // 前往染料
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(TargetIndex.B)
                .FailOnSomeonePhysicallyInteracting(TargetIndex.B);

            // 拿起染料
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);

            // 前往机械体
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // 下达等待任务
            yield return Toils_General.Do(delegate
            {
                Pawn mech = TargetA.Thing as Pawn;
                if (mech?.jobs != null)
                {
                    var waitJob = JobMaker.MakeJob(FFF_JobDefOf.FFF_WaitForPainting);
                    mech.jobs.StartJob(waitJob, JobCondition.InterruptForced);
                }
            });

            // 执行着色工作
            yield return Toils_General.Wait(WorkTicks, TargetIndex.A)
                .WithProgressBarToilDelay(TargetIndex.A)
                .WithEffect(EffecterDefOf.ConstructMetal, TargetIndex.A);

            // 完成着色任务
            yield return Toils_General.Do(delegate
            {
                // 按机械体体型计算染料消耗
                Pawn mechForCost = TargetA.Thing as Pawn;
                int dyeCost = mechForCost != null
                    ? Mathf.CeilToInt(mechForCost.BodySize * 4f)
                    : 1;

                Thing carried = pawn.carryTracker.CarriedThing;
                if (carried != null && carried.def == job.GetTarget(TargetIndex.B).Thing?.def)
                {
                    int toConsume = Mathf.Min(dyeCost, carried.stackCount);
                    carried.SplitOff(toConsume).Destroy();
                }
                else
                {
                    Thing targetDye = job.GetTarget(TargetIndex.B).Thing;
                    if (targetDye != null)
                    {
                        int toConsume = Mathf.Min(dyeCost, targetDye.stackCount);
                        targetDye.SplitOff(toConsume).Destroy();
                    }
                }

                // 应用颜色
                Pawn target = job.GetTarget(TargetIndex.A).Thing as Pawn;
                if (target != null)
                {
                    var comp = target.TryGetComp<CompPaintable>();
                    if (comp != null)
                    {
                        if (comp.activePaintRequest)
                        {
                            comp.color1 = comp.requestColor;
                            comp.color2 = comp.requestColor2;
                            comp.color3 = comp.requestColor3;
                            comp.camoDef = comp.requestCamo;
                            comp.brightness = comp.requestBrightness;
                            comp.overlayDef = comp.requestOverlay;
                            comp.activePaintRequest = false;
                        }
                        comp.Notify_ColorChanged();
                    }

                    // 结束等待Job
                    if (target.jobs?.curDriver is JobDriver_WaitForPainting)
                        target.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
            });
        }
    }
}
