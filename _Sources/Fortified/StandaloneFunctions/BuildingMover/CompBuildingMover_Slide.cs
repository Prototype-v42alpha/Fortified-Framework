using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Fortified;

public partial class CompBuildingMover
{
    // 推进滑动动画
    private void TickSlide()
    {
        slideTicksElapsed++;

        // 确认下一格
        if (slidePending > 0 && !nextCellConfirmed && !ConfirmNextCell()) { EndSlide(); return; }
        if (slidePending <= 0) { EndSlide(); return; }

        slideProgress += SlideAdvancePerTick();

        // 生成滑动烟尘
        if (Props.slideFleckDef != null && parent.Map != null
            && slideTicksElapsed % Props.slideFleckIntervalTicks == 0)
            SpawnSlideFleck();

        // 推进整格移动
        while (slideProgress >= 1f && slidePending > 0)
        {
            slideProgress -= 1f;
            slidePending--;
            Relocate(slideDir);
            trackOffset += DirSign(slideDir);
            nextCellConfirmed = false;

            // 播放移动音效
            if (slideTotalCells - slidePending == 1)
                Props.moveSound?.PlayOneShot(SoundInfo.InMap(parent));

            // 触发门后解雾
            if (Props.sensorDoor && parent.def.MakeFog
                && slideDir == SensorDoorOpenDir()
                && slideTotalCells - slidePending == 1
                && parent.Map != null)
                parent.Map.fogGrid.Notify_FogBlockerRemoved(parent);

            if (ShouldStopSensorDoorSlide()) { EndSlide(); return; }
            if (slidePending <= 0) { EndSlide(); return; }
            if (!ConfirmNextCell()) { EndSlide(); return; }
        }
    }

    // 确认进入下一格
    private bool ConfirmNextCell()
    {
        bool ok = slideCrush ? CrushCellsForMove(slideDir) : MaxMovableDistance(slideDir, 1) > 0;
        if (ok) nextCellConfirmed = true;
        return ok;
    }

    // 结束滑动并归位
    private void EndSlide()
    {
        // 记录滑动方向
        IntVec3 finishedSlideDir = slideDir;
        sliding = false;
        slideProgress = 0f;
        slidePending = 0;
        nextCellConfirmed = false;
        // 刷新本体网格
        if (parent.Map != null) MarkRectDirty(parent.Map, parent.OccupiedRect());
        DirtyDamageLayer();
        // 恢复门洞区域
        if (parent.Map != null && Props.sensorDoor) RefreshDoorPathCost();
        ContinueSensorDoorTarget();
        // 完成后发信号
        if (!sliding) CompleteMoveAction();
        // 触发门后解雾
        if (!sliding && parent.Map != null && parent.def.MakeFog
            && Props.sensorDoor && finishedSlideDir == SensorDoorOpenDir())
            parent.Map.fogGrid.Notify_FogBlockerRemoved(parent);
    }

    // 生成烟尘粒子
    private void SpawnSlideFleck()
    {
        Map map = parent.Map;
        CellRect rect = parent.OccupiedRect();

        // 选取烟尘位置
        float x = Rand.Range(rect.minX, rect.maxX + 1f);
        float z = Rand.Range(rect.minZ, rect.maxZ + 1f);
        Vector3 loc = new Vector3(x, Altitudes.AltitudeFor(AltitudeLayer.DoorMoveable) - 1f, z);
        loc += SlideDrawOffset;

        if (!loc.InBounds(map) || !loc.ShouldSpawnMotesAt(map)) return;

        FleckCreationData data = FleckMaker.GetDataStatic(loc, map, Props.slideFleckDef, Props.slideFleckScale);
        data.rotationRate = Rand.Range(-60f, 60f);
        data.velocityAngle = Rand.Range(0, 360);
        data.velocitySpeed = Rand.Range(0.4f, 0.8f);
        map.flecks.CreateFleck(data);
    }

    // 计算滑动推进
    private float SlideAdvancePerTick()
    {
        int duration = slideDurationOverride > 0 ? slideDurationOverride : Props.moveDurationTicks;
        if (duration <= 0 || slideTotalCells <= 0)
            return Props.slideCellsPerTick;

        // 计算基础速度
        float baseSpeed = (float)slideTotalCells / duration;
        if (Props.slideSpeedCurve == null || Props.slideSpeedCurve.PointsCount == 0)
            return baseSpeed;

        // 曲线按进度调速
        float t = (float)slideTicksElapsed / duration;
        return baseSpeed * Props.slideSpeedCurve.Evaluate(t);
    }
}
