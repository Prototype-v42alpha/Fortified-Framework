using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace Fortified
{
    // 站点突袭控制器：管理隐蔽行动生命周期
    public class QuestPart_FFF_SiteRaidController : QuestPart
    {
        public MapParent mapParent;
        public Faction targetFaction;
        public string inSignalEnable;
        public string outSignalEscaped;
        public string outSignalAllKilled;

        private List<Pawn> targets = new List<Pawn>();
        private bool enabled;
        private bool escaped;
        private bool allKilled;
        private CovertOpContext covertCtx;

        public override void Notify_QuestSignalReceived(
            Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);

            if (signal.tag == inSignalEnable)
            {
                enabled = true;

                // 刷新攻击目标缓存
                FFF_IntelProcessor.Instance
                    ?.NotifyMapGenerated(covertCtx);

                RefreshTargets();
                CheckAllKilled();
            }
        }

        private void RefreshTargets()
        {
            targets.Clear();
            if (mapParent?.Map == null) return;
            var factionPawns = mapParent.Map.mapPawns
                .PawnsInFaction(targetFaction);
            for (int i = 0; i < factionPawns.Count; i++)
                targets.Add(factionPawns[i]);
        }

        public override void Notify_PawnKilled(
            Pawn pawn, DamageInfo? dinfo)
        {
            base.Notify_PawnKilled(pawn, dinfo);
            if (enabled && !escaped && !allKilled
                && targets.Contains(pawn))
            {
                CheckAllKilled();
            }
        }

        private void CheckAllKilled()
        {
            if (!enabled || escaped || allKilled) return;
            if (targets.Count == 0 && mapParent?.Map != null)
                RefreshTargets();

            bool anyAlive = false;
            for (int i = 0; i < targets.Count; i++)
            {
                Pawn p = targets[i];
                if (p != null && !p.Dead && p.Spawned
                    && p.Map == mapParent.Map)
                {
                    anyAlive = true;
                    break;
                }
            }

            if (!anyAlive)
            {
                allKilled = true;

                // 通知情报处理器行动成功
                FFF_IntelProcessor.Instance
                    ?.NotifyOpSuccess(covertCtx);

                ClearBuildingOwnership();

                if (!outSignalAllKilled.NullOrEmpty())
                    Find.SignalManager.SendSignal(
                        new Signal(outSignalAllKilled));
            }
        }

        private void ClearBuildingOwnership()
        {
            if (mapParent?.Map == null
                || targetFaction == null) return;
            var buildings = mapParent.Map.listerBuildings
                .allBuildingsNonColonist;
            for (int i = buildings.Count - 1; i >= 0; i--)
            {
                Building b = buildings[i];
                if (b.Faction == targetFaction)
                    b.SetFaction(null);
            }
        }

        // 由Pawn.ExitMap补丁转发调用
        public void Notify_PawnExitedMap(
            Pawn pawn, Map map)
        {
            if (!enabled || escaped || allKilled) return;
            if (mapParent == null
                || targetFaction == null) return;
            if (!mapParent.HasMap
                || map != mapParent.Map) return;
            if (pawn.Dead
                || pawn.Faction != targetFaction) return;

            escaped = true;

            // 通知情报处理器目击者逃脱
            FFF_IntelProcessor.Instance
                ?.NotifyWitnessEscaped(covertCtx);

            if (!outSignalEscaped.NullOrEmpty())
                Find.SignalManager.SendSignal(
                    new Signal(outSignalEscaped));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(
                ref mapParent, "mapParent");
            Scribe_References.Look(
                ref targetFaction, "targetFaction");
            Scribe_Collections.Look(ref targets,
                "targets", LookMode.Reference);
            Scribe_Values.Look(
                ref inSignalEnable, "inSignalEnable");
            Scribe_Values.Look(
                ref outSignalEscaped, "outSignalEscaped");
            Scribe_Values.Look(
                ref outSignalAllKilled,
                "outSignalAllKilled");
            Scribe_Values.Look(ref enabled, "enabled");
            Scribe_Values.Look(ref escaped, "escaped");
            Scribe_Values.Look(ref allKilled, "allKilled");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (!activeControllers.Contains(this))
                    activeControllers.Add(this);

                // 恢复隐蔽行动上下文
                if (!escaped && !allKilled
                    && mapParent != null)
                {
                    covertCtx = FFF_IntelProcessor.Instance
                        ?.RegisterOp(
                            mapParent, targetFaction);
                }
            }
        }

        private static List<QuestPart_FFF_SiteRaidController>
            activeControllers = new();

        public override void Notify_PreCleanup()
        {
            base.Notify_PreCleanup();
            activeControllers.Remove(this);

            // 清理隐蔽行动上下文
            if (covertCtx != null)
                FFF_IntelProcessor.Instance
                    ?.UnregisterOp(covertCtx);
        }

        public override void PostQuestAdded()
        {
            base.PostQuestAdded();
            if (!activeControllers.Contains(this))
                activeControllers.Add(this);

            // 任务创建时立即注册隐蔽行动
            if (mapParent != null && covertCtx == null)
            {
                covertCtx = FFF_IntelProcessor.Instance
                    ?.RegisterOp(mapParent, targetFaction);
            }
        }

        public static void NotifyAll_PawnExitedMap(
            Pawn pawn, Map map)
        {
            for (int i = activeControllers.Count - 1;
                i >= 0; i--)
            {
                activeControllers[i]
                    .Notify_PawnExitedMap(pawn, map);
            }
        }
    }

    // 配置站点突袭控制器
    public class QuestNode_FFF_SetupRaidController
        : QuestNode
    {
        public SlateRef<MapParent> mapParent;
        public SlateRef<Faction> faction;

        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }

        protected override void RunInt()
        {
            var slate = QuestGen.slate;
            var site = mapParent.GetValue(slate);
            var fac = faction.GetValue(slate);
            if (site == null || fac == null) return;

            var quest = QuestGen.quest;
            string enableSig = QuestGenUtility
                .HardcodedSignalWithQuestID(
                    "site.MapGenerated");
            string escapeSig = QuestGenUtility
                .HardcodedSignalWithQuestID(
                    "EnemyEscaped");
            string allKilledSig = QuestGenUtility
                .HardcodedSignalWithQuestID(
                    "FFF_AllEnemiesKilled");

            var controller =
                new QuestPart_FFF_SiteRaidController
                {
                    mapParent = site,
                    targetFaction = fac,
                    inSignalEnable = enableSig,
                    outSignalEscaped = escapeSig,
                    outSignalAllKilled = allKilledSig
                };
            quest.AddPart(controller);

            slate.Set("escapedSignal", escapeSig);
            slate.Set("allKilledSignal", allKilledSig);
        }
    }
}
