using HarmonyLib;
using Verse;

namespace Fortified
{
    // 监听pawn离开地图事件
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.ExitMap))]
    public static class Patch_FFF_Pawn_ExitMap
    {
        static void Prefix(Pawn __instance)
        {
            if (__instance.Dead) return;
            var map = __instance.Map;
            if (map == null) return;

            QuestPart_FFF_SiteRaidController
                .NotifyAll_PawnExitedMap(__instance, map);
        }
    }
}
