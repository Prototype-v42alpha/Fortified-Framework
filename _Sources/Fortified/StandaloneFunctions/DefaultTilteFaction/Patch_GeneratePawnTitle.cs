// 当白昼倾坠之时
using Verse;
using RimWorld;
using HarmonyLib;
using System;

namespace Fortified
{
    [StaticConstructorOnStartup]
    [HarmonyPatch(new Type[] { typeof(PawnGenerationRequest) })]
    [HarmonyPatch(typeof(PawnGenerator))]
    [HarmonyPatch(nameof(PawnGenerator.GeneratePawn))]

    internal static class Patch_GeneratePawnTitle //確保生成的NPC具有正確的官銜陣營
    {
        public static void Postfix(ref Pawn __result)
        {
            try
            {
                if (__result == null || !ModsConfig.RoyaltyActive) return;
                if (__result.ageTracker.AgeBiologicalYears < 1) return;
                if (!__result.kindDef.HasModExtension<DefaultTilteFactionExtension>()) return;

                ApplyFactionTitle(__result);
            }
            catch (Exception e) { Log.Error($"[FFF] Patch_GeneratePawnTitle Error: {e}"); }
        }

        private static void ApplyFactionTitle(Pawn pawn)
        {
            var ext = pawn.kindDef.GetModExtension<DefaultTilteFactionExtension>();
            Faction faction = Find.FactionManager?.FirstFactionOfDef(ext.faction);
            if (faction != null)
            {
                foreach (RoyalTitle item in pawn.royalty.AllTitlesForReading) item.faction = faction;
            }
        }
    }
}