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

        public override void FinalizeInit()
        {
            activeRules.Clear();
            foreach (var part in Find.Scenario.AllParts)
            {
                if (part is ScenPart_ForcedFactionGoodwill rule) activeRules.Add(rule);
            }
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % 5000 != 0) return;
            ApplyRules();
        }

        private void ApplyRules()
        {
            foreach (var rule in activeRules)
            {
                foreach (var f in Find.FactionManager.AllFactions)
                {
                    if (rule.Affects(f)) ApplyRuleToFaction(rule, f);
                }
            }
        }

        private void ApplyRuleToFaction(ScenPart_ForcedFactionGoodwill rule, Faction f)
        {
            if (rule.alwaysHostile)
            {
                f.TryAffectGoodwillWith(Faction.OfPlayer, -100 - f.PlayerGoodwill, false, false);
                return;
            }
            if (rule.affectNaturalGoodwill)
            {
                int current = f.PlayerGoodwill;
                if (current < rule.naturalGoodwillRange.min || current > rule.naturalGoodwillRange.max)
                {
                    int target = rule.naturalGoodwillRange.RandomInRange;
                    f.TryAffectGoodwillWith(Faction.OfPlayer, target - current, false, false);
                }
            }
        }
    }
}
