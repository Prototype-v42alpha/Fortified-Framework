using System.Reflection;
using RimWorld;
using Verse;

namespace Fortified;

public partial class CompBuildingMover
{
    // 反射写positionInt绕过setter避免region警告
    private static readonly FieldInfo PositionIntField = typeof(Thing).GetField("positionInt", BindingFlags.Instance | BindingFlags.NonPublic);

    // 真正改变建筑位置 统一快速路径
    private void Relocate(IntVec3 offset)
    {
        Map map = parent.Map;
        IntVec3 dest = parent.Position + offset;
        CellRect oldRect = parent.OccupiedRect();

        DetachFromCells(map, oldRect);
        WritePosition(dest);
        CellRect newRect = parent.OccupiedRect();
        AttachToCells(map, newRect);
        RefreshPathing(map, oldRect);
        // 滑动靠PostDraw自绘 中途跳过mesh重建避免每帧整层Regenerate 仅瞬移即时刷新
        if (!sliding)
        {
            MarkRectDirty(map, oldRect);
            MarkRectDirty(map, newRect);
        }
    }

    // 写位置前从旧格注销
    private void DetachFromCells(Map map, CellRect oldRect)
    {
        UpdateLightBlocker(map, oldRect, false);
        RegionListersUpdater.DeregisterInRegions(parent, map);
        map.thingGrid.Deregister(parent, false);
        map.coverGrid.DeRegister(parent);
        if (parent is Building eb && parent.def.IsEdifice()) map.edificeGrid.DeRegister(eb);
        if (parent.def.AffectsRegions) map.regionDirtyer.Notify_ThingAffectingRegionsDespawned(parent);
    }

    // 反射写positionInt绕过setter
    private void WritePosition(IntVec3 dest)
    {
        if (PositionIntField != null) PositionIntField.SetValue(parent, dest);
        else parent.Position = dest;
    }

    // 写位置后注册到新格
    private void AttachToCells(Map map, CellRect newRect)
    {
        map.thingGrid.Register(parent);
        map.coverGrid.Register(parent);
        if (parent is Building eb && parent.def.IsEdifice()) map.edificeGrid.Register(eb);
        map.gasGrid.Notify_ThingSpawned(parent);
        RegionListersUpdater.RegisterInRegions(parent, map);
        if (parent.def.AffectsRegions) map.regionDirtyer.Notify_ThingAffectingRegionsSpawned(parent);
        UpdateLightBlocker(map, newRect, true);
    }

    // 刷新新旧格寻路代价与可达
    private void RefreshPathing(Map map, CellRect oldRect)
    {
        map.pathing.RecalculatePerceivedPathCostUnderThing(parent);
        foreach (IntVec3 c in oldRect)
            if (c.InBounds(map)) map.pathing.RecalculatePerceivedPathCostAt(c);
        map.reachability.ClearCache();
    }

    // 同步遮光网格
    private void UpdateLightBlocker(Map map, CellRect rect, bool added)
    {
        if (map == null || !parent.def.blockLight) return;
        foreach (IntVec3 c in rect)
        {
            if (!c.InBounds(map)) continue;
            if (added) map.glowGrid.LightBlockerAdded(c);
            else map.glowGrid.LightBlockerRemoved(c);
        }
    }

    // 刷新占格渲染
    private static void MarkRectDirty(Map map, CellRect rect)
    {
        if (map == null) return;
        foreach (IntVec3 c in rect)
        {
            if (!c.InBounds(map)) continue;
            map.mapDrawer.MapMeshDirty(c, MapMeshFlagDefOf.Things | MapMeshFlagDefOf.Buildings);
            map.glowGrid.DirtyCell(c);
        }
    }
}
