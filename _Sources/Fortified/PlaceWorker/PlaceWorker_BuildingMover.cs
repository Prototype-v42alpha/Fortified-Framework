using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Fortified
{
    // 放置预览箭头下方文字标注
    public class PlaceWorker_BuildingMover : PlaceWorker
    {
        public override void DrawPlaceMouseAttachments(float curX, ref float curY, BuildableDef bdef, IntVec3 center, Rot4 rot)
        {
            if (!(bdef is ThingDef def)) return;
            CompProperties_BuildingMover props = def.GetCompProperties<CompProperties_BuildingMover>();
            if (props == null) return;

            List<string> lines = BuildLines(props);
            if (lines.Count == 0) return;

            DrawLinesUnderArrow(props, def, center, rot, lines);
        }

        // 收集模式说明文字
        private static List<string> BuildLines(CompProperties_BuildingMover props)
        {
            List<string> lines = new List<string>();
            lines.Add(props.trackToggle
                ? "FFF.BuildingMover.Place.Toggle".Translate()
                : "FFF.BuildingMover.Place.OneWay".Translate());
            if (props.crushOutbound || props.crushReturn)
                lines.Add("FFF.BuildingMover.Place.Crush".Translate());
            if (props.mineWhileCrushing)
                lines.Add("FFF.BuildingMover.Place.Mine".Translate());
            return lines;
        }

        // 文字画在主朝向箭头终点下方
        private static void DrawLinesUnderArrow(CompProperties_BuildingMover props, ThingDef def, IntVec3 center, Rot4 rot, List<string> lines)
        {
            IntVec3 dir = MainDirection(props, rot);
            if (dir == IntVec3.Invalid) return;

            int dist = Mathf.Max(1, props.moveDistance);
            CellRect end = GenAdj.OccupiedRect(center + dir * dist, rot, def.size);
            Vector3 c = end.CenterVector3;
            Vector2 anchor = new Vector2(c.x, c.z - 1f);
            for (int i = 0; i < lines.Count; i++)
                GenMapUI.DrawText(new Vector2(anchor.x, anchor.y - i * 0.6f), lines[i], Color.white);
        }

        // 取主移动方向
        private static IntVec3 MainDirection(CompProperties_BuildingMover props, Rot4 rot)
        {
            foreach (IntVec3 d in CompBuildingMover.ResolveDirections(props, rot))
                return d;
            return IntVec3.Invalid;
        }
    }
}
