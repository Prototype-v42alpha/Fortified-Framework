using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Fortified
{
    // 远离恐惧来源并冒感叹号
    public class JobGiver_FleeFromInstigator : ThinkNode_JobGiver
    {
        // 逃跑距离格区间
        public FloatRange fleeDistRange = new FloatRange(8f, 12f);

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            var obj = (JobGiver_FleeFromInstigator)base.DeepCopy(resolve);
            obj.fleeDistRange = fleeDistRange;
            return obj;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            Pawn instigator = pawn.MentalState?.causedByPawn;
            if (instigator == null) return null;

            int dist = Mathf.CeilToInt(fleeDistRange.RandomInRange);
            Job job = FleeUtility.FleeJob(pawn, instigator, dist);
            if (job == null) return null;

            // 头顶冒感叹号
            MoteMaker.MakeColonistActionOverlay(pawn, ThingDefOf.Mote_ColonistFleeing);
            return job;
        }
    }
}
