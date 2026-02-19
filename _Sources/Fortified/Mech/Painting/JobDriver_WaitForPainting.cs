using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Fortified
{
    // 等待涂装完成Job
    public class JobDriver_WaitForPainting : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var toil = ToilMaker.MakeToil("WaitForPainting");

            // 停止移动
            toil.initAction = () => pawn.pather?.StopDead();

            // 检查涂装请求是否仍有效
            toil.tickAction = () =>
            {
                var comp = pawn.TryGetComp<CompPaintable>();
                if (comp == null || !comp.activePaintRequest)
                    EndJobWith(JobCondition.Succeeded);
            };

            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.handlingFacing = false;
            yield return toil;
        }
    }
}
