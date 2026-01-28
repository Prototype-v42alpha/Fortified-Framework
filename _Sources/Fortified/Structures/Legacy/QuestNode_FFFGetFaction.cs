// 当白昼倾坠之时
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace Fortified.Structures
{
    // 获取指定派系的任务节点
    public class QuestNode_FFFGetFaction : QuestNode_GetFaction
    {
        public SlateRef<FactionDef> factionDef;

        protected override bool TestRunInt(Slate slate)
        {
            var def = factionDef.GetValue(slate);
            if (def != null)
            {
                var faction = Find.FactionManager.FirstFactionOfDef(def);
                if (faction != null)
                {
                    slate.Set(storeAs.GetValue(slate), faction);
                    return true;
                }
            }
            return base.TestRunInt(slate);
        }

        protected override void RunInt()
        {
            var slate = QuestGen.slate;
            var def = factionDef.GetValue(slate);
            if (def != null)
            {
                var faction = Find.FactionManager.FirstFactionOfDef(def);
                if (faction != null)
                {
                    slate.Set(storeAs.GetValue(slate), faction);
                    if (!faction.Hidden)
                    {
                        QuestPart_InvolvedFactions questPart = new QuestPart_InvolvedFactions();
                        questPart.factions.Add(faction);
                        QuestGen.quest.AddPart(questPart);
                    }
                    return;
                }
            }
            base.RunInt();
        }
    }
}
