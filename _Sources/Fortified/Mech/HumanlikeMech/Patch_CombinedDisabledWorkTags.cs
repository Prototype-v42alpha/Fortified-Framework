using HarmonyLib;
using RimWorld;
using Verse;

namespace Fortified
{
    /// <summary>
    /// 修复 HumanlikeMech 在初始化过程中访问 CombinedDisabledWorkTags 时的 NullReferenceException。
    /// 问题：合成人有 SkillRecord，当 WorkSettings 初始化时会访问 CombinedDisabledWorkTags，
    /// 但此时 kindDef 可能还是 null，导致 NPE。
    /// </summary>
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
