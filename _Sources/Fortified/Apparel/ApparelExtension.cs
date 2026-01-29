using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using HarmonyLib;

namespace Fortified
{
    // 服装扩展：确保穿戴者的能力不低于阈值
    public class ApparelExtension : DefModExtension
    {
        public List<PawnCapacityMinLevel> pawnCapacityMinLevels;
    }

    public class PawnCapacityMinLevel
    {
        public PawnCapacityDef capacity;
        public float minLevel;
    }

    // 统计值 DefOf
    [DefOf]
    public static class FFF_StatDefOf
    {
        public static StatDef FFF_MassCarryCapacity;

        static FFF_StatDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(FFF_StatDefOf));
        }
    }

    // 补丁：应用负重能力和最小能力阈值
    [HarmonyPatch]
    public static class ApparelPatch
    {
        // 补丁负重能力
        [HarmonyPatch(typeof(MassUtility), nameof(MassUtility.Capacity))]
        [HarmonyPostfix]
        public static void CapacityPostfix(Pawn p, ref float __result)
        {
            if (p == null) return;
            __result += p.GetStatValue(FFF_StatDefOf.FFF_MassCarryCapacity);
        }

        // 补丁最小能力级别
        [HarmonyPatch(typeof(PawnCapacityUtility), nameof(PawnCapacityUtility.CalculateCapacityLevel))]
        [HarmonyPostfix]
        public static void MinLevelPostfix(HediffSet diffSet, PawnCapacityDef capacity, ref float __result)
        {
            if (diffSet?.pawn?.apparel == null) return;

            var apparelList = diffSet.pawn.apparel.WornApparel;
            for (int i = 0; i < apparelList.Count; i++)
            {
                var ext = apparelList[i].def.GetModExtension<ApparelExtension>();
                if (ext?.pawnCapacityMinLevels != null)
                {
                    for (int j = 0; j < ext.pawnCapacityMinLevels.Count; j++)
                    {
                        var minLevel = ext.pawnCapacityMinLevels[j];
                        if (minLevel.capacity == capacity)
                        {
                            __result = Math.Max(__result, minLevel.minLevel);
                        }
                    }
                }
            }
        }
    }
}
