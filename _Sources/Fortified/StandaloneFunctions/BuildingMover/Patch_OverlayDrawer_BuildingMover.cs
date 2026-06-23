using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Fortified
{
    // 滑动时电力等脉冲覆盖层跟随贴图
    [HarmonyPatch(typeof(OverlayDrawer), "RenderPulsingOverlayInternal")]
    internal static class Patch_OverlayDrawer_BuildingMover
    {
        public static void Prefix(Thing thing, ref Vector3 drawPos)
        {
            Vector3 offset = CompBuildingMover.GetSlideOffset(thing);
            if (offset != Vector3.zero) drawPos += offset;
        }
    }
}
