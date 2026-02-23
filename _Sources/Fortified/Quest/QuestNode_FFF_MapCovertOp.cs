using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace Fortified
{
    // 地图级隐蔽行动 QuestPart
    public class QuestPart_FFF_MapCovertOp : QuestPart
    {
        public MapParent mapParent;
        public Faction targetFaction;
        public string inSignalEnable;
        public string inSignalDisable;

        private CovertOpContext ctx;
        private bool enabled;

        public override void Notify_QuestSignalReceived(
            Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);

            if (signal.tag == inSignalEnable && !enabled)
            {
                enabled = true;
                var intel = FFF_IntelProcessor.Instance;
                if (intel != null)
                    intel.NotifyMapGenerated(ctx);
            }

            if (signal.tag == inSignalDisable && enabled)
            {
                enabled = false;
                var intel = FFF_IntelProcessor.Instance;
                if (intel != null && ctx != null)
                    intel.NotifyOpSuccess(ctx);
            }
        }

        public override void PostQuestAdded()
        {
            base.PostQuestAdded();
            var intel = FFF_IntelProcessor.Instance;
            if (intel != null && mapParent != null)
                ctx = intel.RegisterOp(mapParent, targetFaction);
        }

        public override void Notify_PreCleanup()
        {
            base.Notify_PreCleanup();
            if (ctx != null)
                FFF_IntelProcessor.Instance
                    ?.UnregisterOp(ctx);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref mapParent, "mapParent");
            Scribe_References.Look(ref targetFaction,
                "targetFaction");
            Scribe_Values.Look(ref inSignalEnable,
                "inSignalEnable");
            Scribe_Values.Look(ref inSignalDisable,
                "inSignalDisable");
            Scribe_Values.Look(ref enabled, "enabled");

            if (Scribe.mode == LoadSaveMode.PostLoadInit
                && mapParent != null)
            {
                ctx = FFF_IntelProcessor.Instance
                    ?.RegisterOp(mapParent, targetFaction);
            }
        }
    }

    // 地图级隐蔽行动 QuestNode
    public class QuestNode_FFF_MapCovertOp : QuestNode
    {
        [NoTranslate] public SlateRef<MapParent> mapParent;
        [NoTranslate] public SlateRef<Faction> targetFaction;
        [NoTranslate] public SlateRef<string> inSignalEnable;
        [NoTranslate] public SlateRef<string> inSignalDisable;

        protected override bool TestRunInt(Slate slate)
        {
            return mapParent.GetValue(slate) != null
                && targetFaction.GetValue(slate) != null;
        }

        protected override void RunInt()
        {
            var slate = QuestGen.slate;
            var part = new QuestPart_FFF_MapCovertOp
            {
                mapParent = mapParent.GetValue(slate),
                targetFaction = targetFaction.GetValue(slate),
                inSignalEnable = QuestGenUtility
                    .HardcodedSignalWithQuestID(
                        inSignalEnable.GetValue(slate)),
                inSignalDisable = QuestGenUtility
                    .HardcodedSignalWithQuestID(
                        inSignalDisable.GetValue(slate)),
            };
            QuestGen.quest.AddPart(part);
        }
    }
}
