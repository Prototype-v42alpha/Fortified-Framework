using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace Fortified
{
    // Pawn级隐蔽行动 QuestPart
    public class QuestPart_FFF_PawnCovertOp : QuestPart
    {
        public List<Pawn> pawns = new List<Pawn>();
        public string inSignalEnable;
        public string inSignalDisable;

        private bool enabled;

        public override void Notify_QuestSignalReceived(
            Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);

            if (signal.tag == inSignalEnable && !enabled)
            {
                enabled = true;
                RegisterAll();
            }

            if (signal.tag == inSignalDisable && enabled)
            {
                enabled = false;
                UnregisterAll();
            }
        }

        public override void PostQuestAdded()
        {
            base.PostQuestAdded();
            RegisterAll();
        }

        public override void Notify_PreCleanup()
        {
            base.Notify_PreCleanup();
            UnregisterAll();
        }

        private void RegisterAll()
        {
            var intel = FFF_IntelProcessor.Instance;
            if (intel == null) return;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i] != null)
                    intel.RegisterAgent(pawns[i]);
            }
        }

        private void UnregisterAll()
        {
            var intel = FFF_IntelProcessor.Instance;
            if (intel == null) return;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i] != null)
                    intel.UnregisterAgent(pawns[i]);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref pawns,
                "pawns", LookMode.Reference);
            Scribe_Values.Look(ref inSignalEnable,
                "inSignalEnable");
            Scribe_Values.Look(ref inSignalDisable,
                "inSignalDisable");
            Scribe_Values.Look(ref enabled, "enabled");
            if (pawns == null) pawns = new List<Pawn>();

            if (Scribe.mode == LoadSaveMode.PostLoadInit
                && enabled)
            {
                RegisterAll();
            }
        }
    }

    // Pawn级隐蔽行动 QuestNode
    public class QuestNode_FFF_PawnCovertOp : QuestNode
    {
        [NoTranslate] public SlateRef<List<Pawn>> pawns;
        [NoTranslate] public SlateRef<string> inSignalEnable;
        [NoTranslate] public SlateRef<string> inSignalDisable;

        protected override bool TestRunInt(Slate slate)
        {
            var list = pawns.GetValue(slate);
            return list != null && list.Count > 0;
        }

        protected override void RunInt()
        {
            var slate = QuestGen.slate;
            var list = pawns.GetValue(slate);
            var part = new QuestPart_FFF_PawnCovertOp();
            if (list != null)
                part.pawns.AddRange(list);

            var enable = inSignalEnable.GetValue(slate);
            if (!enable.NullOrEmpty())
            {
                part.inSignalEnable = QuestGenUtility
                    .HardcodedSignalWithQuestID(enable);
            }

            var disable = inSignalDisable.GetValue(slate);
            if (!disable.NullOrEmpty())
            {
                part.inSignalDisable = QuestGenUtility
                    .HardcodedSignalWithQuestID(disable);
            }

            QuestGen.quest.AddPart(part);
        }
    }
}
