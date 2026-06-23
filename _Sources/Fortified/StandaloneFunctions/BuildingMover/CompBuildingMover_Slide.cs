using Verse;

namespace Fortified;

public partial class CompBuildingMover
{
    // 推进滑动动画
    private void TickSlide()
    {
        slideTicksElapsed++;

        // 滑向当前格前先确认 防止贴图侵入障碍格
        if (slidePending > 0 && !nextCellConfirmed && !ConfirmNextCell()) { EndSlide(); return; }
        if (slidePending <= 0) { EndSlide(); return; }

        slideProgress += SlideAdvancePerTick();

        // 滑满一格真实推进 再确认下一格
        while (slideProgress >= 1f && slidePending > 0)
        {
            slideProgress -= 1f;
            slidePending--;
            Relocate(slideDir);
            trackOffset += DirSign(slideDir);
            nextCellConfirmed = false;

            if (ShouldStopSensorDoorSlide()) { EndSlide(); return; }
            if (slidePending <= 0) { EndSlide(); return; }
            if (!ConfirmNextCell()) { EndSlide(); return; }
        }
    }

    // 确认下一格可进入 碾压模式逐格清障
    private bool ConfirmNextCell()
    {
        bool ok = slideCrush ? CrushCellsForMove(slideDir) : MaxMovableDistance(slideDir, 1) > 0;
        if (ok) nextCellConfirmed = true;
        return ok;
    }

    // 结束滑动并归位
    private void EndSlide()
    {
        sliding = false;
        slideProgress = 0f;
        slidePending = 0;
        nextCellConfirmed = false;
        // 刷新本体mesh 防止滑动取消后贴图丢失
        if (parent.Map != null) MarkRectDirty(parent.Map, parent.OccupiedRect());
        DirtyDamageLayer();
        // 滑动结束在非滑动态重建region 恢复感应门Portal
        if (parent.Map != null && Props.sensorDoor) RefreshDoorPathCost();
        ContinueSensorDoorTarget();
        // 动作彻底完成才发信号 感应门分段续滑时不发
        if (!sliding) CompleteMoveAction();
    }

    // 计算本tick滑动推进量
    private float SlideAdvancePerTick()
    {
        int duration = slideDurationOverride > 0 ? slideDurationOverride : Props.moveDurationTicks;
        if (duration <= 0 || slideTotalCells <= 0)
            return Props.slideCellsPerTick;

        // 基础匀速 总格数除以总耗时
        float baseSpeed = (float)slideTotalCells / duration;
        if (Props.slideSpeedCurve == null || Props.slideSpeedCurve.PointsCount == 0)
            return baseSpeed;

        // 曲线按进度调速
        float t = (float)slideTicksElapsed / duration;
        return baseSpeed * Props.slideSpeedCurve.Evaluate(t);
    }
}
