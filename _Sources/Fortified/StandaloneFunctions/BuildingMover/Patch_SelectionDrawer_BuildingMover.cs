using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Fortified
{
    // 滑动时选择框跟随贴图
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(SelectionDrawer), nameof(SelectionDrawer.DrawSelectionBracketFor))]
    internal static class Patch_SelectionDrawer_BuildingMover
    {
        private static readonly Material BracketMat = AccessTools
            .Field(typeof(SelectionDrawer), "SelectionBracketMat")
            .GetValue(null) as Material;

        private static readonly Vector3[] bracketLocs = new Vector3[4];

        public static bool Prefix(object obj, Material overrideMat)
        {
            if (!(obj is ThingWithComps thing)) return true;
            // 多comp时取正在滑动的那个
            CompBuildingMover comp = FindSlidingComp(thing);
            if (comp == null) return true;

            DrawShiftedBracket(thing, comp, overrideMat);
            return false;
        }

        // 查找正在滑动的移动组件
        private static CompBuildingMover FindSlidingComp(ThingWithComps thing)
        {
            foreach (CompBuildingMover c in thing.GetComps<CompBuildingMover>())
            {
                if (c.Sliding) return c;
            }
            return null;
        }

        // 按滑动偏移绘制选择框
        private static void DrawShiftedBracket(ThingWithComps thing, CompBuildingMover comp, Material overrideMat)
        {
            Vector3 center = thing.DrawPos + comp.SlideDrawOffset;
            SelectionDrawerUtility.CalculateSelectionBracketPositionsWorld(
                bracketLocs, thing, center, thing.RotatedSize.ToVector2(),
                SelectionDrawer.SelectTimes, Vector2.one, 1f,
                thing.def.deselectedSelectionBracketFactor);

            int angle = 0;
            for (int i = 0; i < 4; i++)
            {
                Quaternion q = Quaternion.AngleAxis(angle, Vector3.up);
                Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(bracketLocs[i], q, Vector3.one),
                    overrideMat ?? BracketMat, 0);
                angle -= 90;
            }
        }
    }
}
