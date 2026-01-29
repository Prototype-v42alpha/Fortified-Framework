// 当白昼倾坠之时
using RimWorld;
using System.Collections.Generic;
using Verse;
using Fortified.Structures;

namespace Fortified
{
    // 处理场景强制好感度锁定
    public class FFF_FactionGoodwillManager : GameComponent
    {
        public FFF_FactionGoodwillManager(Game game) { }

        public static List<ScenPart_ForcedFactionGoodwill> activeRules = new List<ScenPart_ForcedFactionGoodwill>();

        // 缓存原始 permanentEnemy 状态以便恢复
        private static Dictionary<FactionDef, bool> originalPermanentEnemy = new Dictionary<FactionDef, bool>();

        public override void FinalizeInit()
        {
            activeRules.Clear();
            originalPermanentEnemy.Clear();

            // 收集所有派系的原始敌对状态
            foreach (var def in DefDatabase<FactionDef>.AllDefsListForReading)
            {
                if (!originalPermanentEnemy.ContainsKey(def))
                    originalPermanentEnemy[def] = def.permanentEnemy;
            }

            // 收集所有好感度规则
            foreach (var part in Find.Scenario.AllParts)
            {
                if (part is ScenPart_ForcedFactionGoodwill rule) 
                    activeRules.Add(rule);
            }

            // 应用 alwaysHostile 规则到 FactionDef
            ApplyPermanentEnemyRules();
        }

        private void ApplyPermanentEnemyRules()
        {
            // 先恢复所有派系的原始状态
            foreach (var kvp in originalPermanentEnemy)
            {
                kvp.Key.permanentEnemy = kvp.Value;
            }

            // 按规则顺序（从后往前）应用 alwaysHostile
            for (int i = activeRules.Count - 1; i >= 0; i--)
            {
                var rule = activeRules[i];
                if (!rule.alwaysHostile) continue;

                foreach (var def in DefDatabase<FactionDef>.AllDefsListForReading)
                {
                    if (def.isPlayer) continue;
                    if (rule.factionDef != null && rule.factionDef != def) continue;
                    if (!rule.affectHiddenFactions && def.hidden) continue;

                    def.permanentEnemy = true;
                }
            }
        }

        // 不再需要定期 Tick 强制修改好感度
        // 自然好感度调整由 Patch_Faction_NaturalGoodwill 处理
    }
}
