using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Fortified;

// 移动路径预览绘制 镜像comp运行时方向与距离
public static class BuildingMoverGhostDrawer
{
    // 单段预览 方向与距离
    private struct Segment
    {
        public IntVec3 dir;
        public int dist;
        public Segment(IntVec3 d, int n) { dir = d; dist = n; }
    }

    // 画全部预览段的白框与箭头
    public static void DrawPath(CompProperties_BuildingMover props, ThingDef def, IntVec3 center, Rot4 rot)
    {
        if (props == null || def == null) return;

        CellRect self = GenAdj.OccupiedRect(center, rot, def.size);
        foreach (Segment seg in BuildSegments(props, rot))
        {
            if (seg.dir == IntVec3.Invalid || seg.dir == IntVec3.Zero || seg.dist <= 0) continue;
            DrawPathEdges(center, def, rot, seg.dir, seg.dist, self);
            DrawArrow(center, def, rot, seg.dir, seg.dist);
        }
    }

    // 按comp模式分支生成预览段 顺序与运行时一致
    private static IEnumerable<Segment> BuildSegments(CompProperties_BuildingMover props, Rot4 rot)
    {
        // 感应门 单向让路
        if (props.sensorDoor)
        {
            IntVec3 door = FirstDir(props, rot, props.sensorDoorAxis);
            yield return new Segment(door, Mathf.Max(1, props.sensorDoorDistance));
            yield break;
        }

        int baseDist = MoveDistance(props);

        // 信号动作映射 各自方向与距离
        if (props.signalActions != null && props.signalActions.Count > 0)
        {
            foreach (Segment s in SignalSegments(props, rot, baseDist)) yield return s;
            yield break;
        }

        List<IntVec3> dirs = new List<IntVec3>(CompBuildingMover.ResolveDirections(props, rot));
        if (dirs.Count == 0) yield break;

        // 开关轨道 首方向正反往返
        if (props.trackToggle)
        {
            yield return new Segment(dirs[0], baseDist);
            yield return new Segment(-dirs[0], baseDist);
            yield break;
        }

        // 单向仅首方向 否则全部方向
        int count = props.oneWayOnly ? 1 : dirs.Count;
        for (int i = 0; i < count; i++)
            yield return new Segment(dirs[i], baseDist);
    }

    // 信号动作的方向与距离 Move类用配置 开关类沿首方向
    private static IEnumerable<Segment> SignalSegments(CompProperties_BuildingMover props, Rot4 rot, int baseDist)
    {
        List<IntVec3> dirs = new List<IntVec3>(CompBuildingMover.ResolveDirections(props, rot));
        IntVec3 fwd = dirs.Count > 0 ? dirs[0] : IntVec3.Invalid;
        HashSet<long> seen = new HashSet<long>();

        foreach (BuildingMoverSignalAction sa in props.signalActions)
        {
            if (sa == null) continue;
            IntVec3 dir;
            int dist;
            switch (sa.action)
            {
                case BuildingMoverAction.Move:
                    dir = CompBuildingMover.ResolveRelativeDir(sa.moveDir, rot, sa.customDirection, -fwd);
                    dist = sa.moveDistance > 0 ? sa.moveDistance : baseDist;
                    break;
                case BuildingMoverAction.Open:
                    dir = fwd; dist = baseDist; break;
                case BuildingMoverAction.Close:
                    dir = -fwd; dist = baseDist; break;
                default:
                    continue; // Toggle/Rotate无固定路径
            }
            if (dir == IntVec3.Invalid || dir == IntVec3.Zero) continue;
            // 去重相同方向距离
            long key = ((long)dir.x * 31 + dir.z) * 1000 + dist;
            if (seen.Add(key)) yield return new Segment(dir, dist);
        }
    }

    // 主配置移动距离 SlideToEnd预览阶段无地图按moveDistance近似
    private static int MoveDistance(CompProperties_BuildingMover props)
    {
        return Mathf.Max(1, props.moveDistance);
    }

    // 取指定轴首个非零方向
    private static IntVec3 FirstDir(CompProperties_BuildingMover props, Rot4 rot, BuildingMoveAxis axis)
    {
        foreach (IntVec3 d in CompBuildingMover.ResolveDirections(props, rot, axis))
            if (d != IntVec3.Zero) return d;
        return IntVec3.Invalid;
    }

    // 画单段移动距离白框 排除建筑现占地
    private static void DrawPathEdges(IntVec3 center, ThingDef def, Rot4 rot, IntVec3 dir, int dist, CellRect self)
    {
        HashSet<IntVec3> seen = new HashSet<IntVec3>();
        List<IntVec3> cells = new List<IntVec3>();
        for (int i = 1; i <= dist; i++)
        {
            CellRect rect = GenAdj.OccupiedRect(center + dir * i, rot, def.size);
            foreach (IntVec3 c in rect)
            {
                if (self.Contains(c)) continue;
                if (seen.Add(c)) cells.Add(c);
            }
        }
        if (cells.Count > 0) GenDraw.DrawFieldEdges(cells, Color.white);
    }

    // 箭头落在建筑移动后占地几何中心
    private static void DrawArrow(IntVec3 center, ThingDef def, Rot4 rot, IntVec3 dir, int dist)
    {
        float angle = dir.ToVector3().AngleFlat();
        CellRect end = GenAdj.OccupiedRect(center + dir * dist, rot, def.size);
        Vector3 pos = end.CenterVector3;
        pos.y = AltitudeLayer.MetaOverlays.AltitudeFor();
        GenDraw.DrawArrowRotated(pos, angle, true);
    }
}
