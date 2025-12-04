using HarmonyLib;
using RimWorld;
using Verse;

namespace Fortified
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.CombinedDisabledWorkTags), MethodType.Getter)]
    public static class Patch_CombinedDisabledWorkTags
    {
        [HarmonyPrefix]
        public static bool Prefix(Pawn __instance, ref WorkTags __result)
        {
            // 如果 kindDef 为 null，返回 None 以避免 NPE
            if (__instance.kindDef == null)
            {
                __result = __instance.story?.DisabledWorkTagsBackstoryTraitsAndGenes ?? WorkTags.None;
                return false;
            }
            return true;
        }
    }
}
