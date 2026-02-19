using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace Fortified
{
    // 机械体涂装工作分配
    public class WorkGiver_PaintMech : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        // 检查是否有工作
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t is not Pawn mech || !mech.Spawned) return false;

            // 检查组件和请求状态
            var comp = mech.TryGetComp<CompPaintable>();
            if (comp == null || !comp.activePaintRequest) return false;

            // 计算所需染料数量
            int needed = Mathf.CeilToInt(mech.BodySize * 4f);

            // 检查是否有足够染料
            if (!FindDye(pawn, needed, out _))
            {
                JobFailReason.Is("FFF_NoDye".Translate());
                return false;
            }

            // 检查能否到达
            if (!pawn.CanReserve(mech, 1, -1, null, forced)) return false;

            return true;
        }

        // 创建工作
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t is not Pawn mech) return null;
            int needed = Mathf.CeilToInt(mech.BodySize * 4f);
            if (FindDye(pawn, needed, out var dye))
            {
                var job = JobMaker.MakeJob(FFF_JobDefOf.FFF_PaintMech, t, dye);
                job.count = needed;
                return job;
            }
            return null;
        }

        // 查找最近的可用染料且数量足够
        private bool FindDye(Pawn pawn, int needed, out Thing dye)
        {
            ThingDef dyeDef = DefDatabase<ThingDef>.GetNamed("Dye", false);
            if (dyeDef == null) { dye = null; return false; }

            dye = GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                ThingRequest.ForDef(dyeDef),
                PathEndMode.ClosestTouch,
                TraverseParms.For(pawn),
                9999f,
                validator: (t) => !t.IsForbidden(pawn) && pawn.CanReserve(t) && t.stackCount >= needed
            );

            return dye != null;
        }
    }
}
