using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Fortified
{
    // 滑动时损坏覆盖层跟随贴图
    // 静态烘焙层无法亚格平移 改在滑动时跳过烘焙并动态重绘
    [HarmonyPatch(typeof(SectionLayer_BuildingsDamage), "PrintDamageVisualsFrom")]
    internal static class Patch_DamageLayer_BuildingMover
    {
        // 滑动建筑跳过静态烘焙
        public static bool Prefix(Building b)
        {
            return CompBuildingMover.GetSlideOffset(b) == Vector3.zero;
        }
    }
}
