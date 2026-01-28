// 当白昼倾坠之时
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace Fortified
{
    // 修复机兵容器蓝图渲染问题
    // 目标是让已放置在地面上的蓝图（Blueprint_Install）显示为原版风格的半透明机兵虚影
    [HarmonyPatch(typeof(Blueprint), "get_Graphic")]
    public static class Patch_Blueprint_Graphic
    {
        [HarmonyPostfix]
        public static void Postfix(Blueprint __instance, ref Graphic __result)
        {
            try
            {
                if (__instance is Blueprint_Install install && install.ThingToInstall is Building_MechCapsule capsule && capsule.HasMech)
                {
                    __result = GhostUtility.GhostGraphicFor(capsule.Graphic, __instance.def, __instance.DrawColor);
                }
            }
            catch (System.Exception e) { Log.Error($"[FFF] Patch_Blueprint_Graphic Error: {e}"); }
        }
    }
}
