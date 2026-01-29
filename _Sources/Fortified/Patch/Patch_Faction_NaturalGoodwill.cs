// 当白昼倾坠之时
using HarmonyLib;
using RimWorld;
using Verse;
using Fortified.Structures;

namespace Fortified
{
    // Harmony Patch: 修改派系的自然好感度倾向
    [HarmonyPatch(typeof(Faction), nameof(Faction.NaturalGoodwill), MethodType.Getter)]
    public static class Patch_Faction_NaturalGoodwill
    {
        [HarmonyPostfix]
        public static void Postfix(Faction __instance, ref int __result)
        {
            if (__instance.IsPlayer) return;

            var rules = FFF_FactionGoodwillManager.activeRules;
            if (rules.NullOrEmpty()) return;

            for (int i = rules.Count - 1; i >= 0; i--)
            {
                var rule = rules[i];
                if (rule.affectNaturalGoodwill && rule.Affects(__instance))
                {
                    // 以规则设定的自然好感度范围均值覆盖原版计算结果
                    __result = (rule.naturalGoodwillRange.min + rule.naturalGoodwillRange.max) / 2;
                    return;
                }
            }
        }
    }
}
