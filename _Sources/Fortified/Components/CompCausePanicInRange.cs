using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Fortified
{
    // 范围内敌人定期概率惊慌
    public class CompCausePanicInRange : ThingComp
    {
        private int ticksSinceScan;

        public CompProperties_CausePanicInRange Props =>
            (CompProperties_CausePanicInRange)props;

        // 实际触发的状态
        private MentalStateDef StateDef =>
            Props.mentalState ?? FFF_DefOf.FFF_FleeInPlace;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
                ticksSinceScan = parent.thingIDNumber % Props.tickInterval;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ticksSinceScan, nameof(ticksSinceScan), 0);
        }

        public override void CompTickInterval(int delta)
        {
            if (!parent.Spawned) return;

            ticksSinceScan += delta;
            if (ticksSinceScan < Props.tickInterval) return;

            ticksSinceScan = 0;
            ScanAndPanic();
        }

        // 扫描敌对目标并触发
        private void ScanAndPanic()
        {
            if (!(parent is IAttackTargetSearcher searcher)) return;

            float rangeSq = Props.range * Props.range;
            IntVec3 origin = parent.Position;

            // 复用敌对缓存列表 零分配且已按敌对关系过滤
            List<IAttackTarget> targets =
                parent.Map.attackTargetsCache.GetPotentialTargetsFor(searcher);

            for (int i = 0; i < targets.Count; i++)
            {
                if (!(targets[i].Thing is Pawn pawn)) continue;
                if (pawn.Position.DistanceToSquared(origin) > rangeSq) continue;
                if (!IsValidVictim(pawn)) continue;
                if (!Rand.Chance(Props.chancePerScan)) continue;

                pawn.mindState.mentalStateHandler.TryStartMentalState(
                    StateDef, causedByDamage: true, otherPawn: parent as Pawn);
            }
        }

        // 判定可否被赋予状态
        private bool IsValidVictim(Pawn pawn)
        {
            if (pawn == parent || pawn.Dead || !pawn.Spawned) return false;
            if (pawn.InMentalState) return false;
            return StateDef.Worker.StateCanOccur(pawn);
        }
    }

    public class CompProperties_CausePanicInRange : CompProperties
    {
        // 扫描间隔
        public int tickInterval = 300;

        // 影响半径
        public float range = 5f;

        // 每次扫描每人触发概率
        public float chancePerScan = 1f;

        // 赋予的心理状态
        public MentalStateDef mentalState;

        public CompProperties_CausePanicInRange()
        {
            compClass = typeof(CompCausePanicInRange);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string e in base.ConfigErrors(parentDef))
                yield return e;

            if (tickInterval <= 0)
                yield return $"{nameof(CompProperties_CausePanicInRange)}: tickInterval must be > 0.";
            if (range <= 0f)
                yield return $"{nameof(CompProperties_CausePanicInRange)}: range must be > 0.";
            if (chancePerScan <= 0f)
                yield return $"{nameof(CompProperties_CausePanicInRange)}: chancePerScan must be > 0.";
        }
    }
}
