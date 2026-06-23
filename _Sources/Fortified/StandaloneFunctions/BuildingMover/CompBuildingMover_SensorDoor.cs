using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Fortified;

public partial class CompBuildingMover
{
    private static readonly List<CompBuildingMover> ActiveSensorDoors = new List<CompBuildingMover>();

    // 门关闭中心 不随滑动偏移
    private IntVec3 sensorDoorClosedCenter;
    // 锚点已记录
    private bool sensorDoorAnchorSet;

    // 门洞格恒为Portal 仅圈可开启的列 用常量距离保持静态
    public bool IsSensorDoorPortalCell(IntVec3 cell)
    {
        if (disabled) return false;
        if (!Props.sensorDoor) return false;
        int need = RequiredOpenDistance(cell);
        return need > 0 && need <= Props.sensorDoorDistance;
    }

    // 记录门关闭锚点 反推滑动偏移
    public void EnsureSensorDoorAnchor()
    {
        if (sensorDoorAnchorSet || !Props.sensorDoor) return;
        IntVec3 dir = SensorDoorOpenDir();
        sensorDoorClosedCenter = parent.Position - dir * Mathf.Max(0, trackOffset);
        sensorDoorAnchorSet = true;
    }

    // 外部comp平移后重锚定门洞 跟随建筑新位置
    public void SyncSensorDoorAnchorIfMoved()
    {
        if (!Props.sensorDoor || !sensorDoorAnchorSet || parent.Map == null) return;
        // 等所有comp移动结束再重锚 避免滑动途中每tick重建
        if (AnySliding(parent)) return;
        IntVec3 dir = SensorDoorOpenDir();
        int off = Mathf.Max(0, trackOffset);
        if (sensorDoorClosedCenter + dir * off == parent.Position) return;

        // 失效旧门洞region
        Map map = parent.Map;
        foreach (IntVec3 c in SensorDoorFootprint().ClipInsideMap(map))
            map.regionDirtyer.Notify_WalkabilityChanged(c, true);
        sensorDoorClosedCenter = parent.Position - dir * off;
        RefreshDoorPathCost();
    }

    // 门关闭时占格 静态Portal归类基准
    private CellRect SensorDoorFootprint()
    {
        if (!sensorDoorAnchorSet) return parent.OccupiedRect();
        return GenAdj.OccupiedRect(sensorDoorClosedCenter, parent.Rotation, parent.def.size);
    }

    public int SensorDoorPathCostAt(IntVec3 cell)
    {
        int need = RequiredOpenDistance(cell);
        if (need <= 0) return 10000;
        // 门体已滑开露出 视为普通路面 不加门代价
        if (cell.GetEdifice(parent.Map) != parent) return 1;
        // 门体仍压着 按需等待格数加代价 引导走更快露出的列
        int wait = Mathf.Max(0, need - CurrentSensorDoorOpenDistance());
        return Props.sensorDoorPathCost + wait * Props.sensorDoorWaitCostPerCell;
    }

    // 通行只看门体是否物理压住此格 门会完整打开
    public bool CanPassDoorCell(IntVec3 cell)
    {
        if (!IsSensorDoorPortalCell(cell)) return false;
        return cell.GetEdifice(parent.Map) != parent;
    }

    public int RequiredOpenDistance(IntVec3 cell)
    {
        if (!Props.sensorDoor) return 0;
        CellRect rect = SensorDoorFootprint();
        if (!rect.Contains(cell)) return 0;

        IntVec3 dir = SensorDoorOpenDir();
        if (dir.x > 0) return cell.x - rect.minX + 1;
        if (dir.x < 0) return rect.maxX - cell.x + 1;
        if (dir.z > 0) return cell.z - rect.minZ + 1;
        if (dir.z < 0) return rect.maxZ - cell.z + 1;
        return 0;
    }

    public CellRect SensorDoorRect()
    {
        IntVec3 dir = SensorDoorOpenDir();
        return parent.OccupiedRect().MovedBy(-dir * CurrentSensorDoorOpenDistance());
    }

    public IntVec3 SensorDoorOpenDir()
    {
        IEnumerable<IntVec3> dirs = ResolveDirections(Props, parent.Rotation, Props.sensorDoorAxis);
        foreach (IntVec3 dir in dirs)
            if (dir != IntVec3.Zero) return dir;
        return IntVec3.Zero;
    }

    public int CurrentSensorDoorOpenDistance()
    {
        return Mathf.Max(0, trackOffset);
    }

    public int MaxSensorDoorOpenDistance()
    {
        IntVec3 dir = SensorDoorOpenDir();
        if (dir == IntVec3.Zero || parent.Map == null) return 0;
        int current = CurrentSensorDoorOpenDistance();
        int remain = Mathf.Max(0, Props.sensorDoorDistance - current);
        return current + MaxDistanceInBounds(dir, remain);
    }

    public void RequestOpenTo(int distance)
    {
        RequestOpenToInternal(distance);
    }

    private bool RequestOpenToInternal(int distance)
    {
        if (disabled) return false;
        int target = Mathf.Clamp(distance, 0, MaxSensorDoorOpenDistance());
        if (target <= 0) return false;
        sensorDoorTargetOpen = Mathf.Max(sensorDoorTargetOpen, target);
        if (sensorDoorTargetOpen <= CurrentSensorDoorOpenDistance()) return false;
        if (sliding) return false;
        return StartSensorDoorOpen(sensorDoorTargetOpen);
    }

    private bool StartSensorDoorOpen(int target)
    {
        if (disabled) return false;
        IntVec3 dir = SensorDoorOpenDir();
        if (dir == IntVec3.Zero) return false;
        int delta = target - CurrentSensorDoorOpenDistance();
        if (delta <= 0) return false;
        doorOpen = true;
        slideDurationOverride = Props.sensorDoorOpenTicks;
        RefreshDoorPathCost();
        bool moved = TryMoveInternal(dir, true, delta);
        if (!moved) doorOpen = false;
        return moved;
    }

    private bool ShouldStopSensorDoorSlide()
    {
        if (!Props.sensorDoor || sensorDoorTargetOpen <= 0) return false;
        IntVec3 dir = SensorDoorOpenDir();
        if (slideDir == -dir) return CurrentSensorDoorOpenDistance() <= sensorDoorTargetOpen;
        return false;
    }

    private void ContinueSensorDoorTarget()
    {
        if (!Props.sensorDoor || parent.Map == null) return;
        if (sensorDoorTargetOpen > CurrentSensorDoorOpenDistance())
        {
            StartSensorDoorOpen(sensorDoorTargetOpen);
            return;
        }
        if (CurrentSensorDoorOpenDistance() <= 0)
        {
            doorOpen = false;
            sensorDoorTargetOpen = 0;
        }
    }

    public static CompBuildingMover GetSensorDoorAt(Map map, IntVec3 cell)
    {
        if (map == null || !cell.InBounds(map)) return null;
        Building edifice = cell.GetEdifice(map);
        CompBuildingMover door = GetSensorDoor(edifice);
        if (door != null && door.IsSensorDoorPortalCell(cell)) return door;

        for (int i = ActiveSensorDoors.Count - 1; i >= 0; i--)
        {
            CompBuildingMover active = ActiveSensorDoors[i];
            if (active?.parent?.Map != map) continue;
            if (active.IsSensorDoorPortalCell(cell)) return active;
        }
        return null;
    }

    private static void RegisterSensorDoor(CompBuildingMover door)
    {
        if (door == null || ActiveSensorDoors.Contains(door)) return;
        ActiveSensorDoors.Add(door);
    }

    private static void DeregisterSensorDoor(CompBuildingMover door)
    {
        if (door == null) return;
        ActiveSensorDoors.Remove(door);
    }
}
