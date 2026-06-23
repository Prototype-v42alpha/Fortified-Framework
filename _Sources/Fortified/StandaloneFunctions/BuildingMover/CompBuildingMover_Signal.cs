using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Multiplayer.API;

namespace Fortified;

public partial class CompBuildingMover
{
    public override void Notify_SignalReceived(Signal signal)
    {
        base.Notify_SignalReceived(signal);
        if (disabled) return;

        // 信号动作映射表 优先匹配
        if (Props.signalActions != null)
        {
            for (int i = 0; i < Props.signalActions.Count; i++)
            {
                BuildingMoverSignalAction sa = Props.signalActions[i];
                if (sa == null || sa.signalTag.NullOrEmpty()) continue;
                if (signal.tag != sa.signalTag) continue;
                SyncedSignalAction(this, i);
            }
        }

        // 兼容旧单标签触发
        if (!Props.allowSignal) return;
        if (Props.listenSignalTag.NullOrEmpty() || signal.tag != Props.listenSignalTag) return;

        if (Props.trackToggle) { SyncedToggle(this); return; }

        foreach (IntVec3 dir in ResolveDirections())
        {
            if (MaxMovableDistance(dir, WantedDistance()) > 0)
            {
                TrySignalMove(dir, Props.crushOutbound, WantedDistance());
                return;
            }
        }
    }

    // 同步分发信号动作 多人安全
    [SyncMethod]
    private static void SyncedSignalAction(CompBuildingMover comp, int actionIndex)
    {
        comp?.ExecuteSignalAction(actionIndex);
    }

    // 执行信号映射的动作 记录来源用于完成回调
    private void ExecuteSignalAction(int actionIndex)
    {
        if (Props.signalActions == null || actionIndex < 0 || actionIndex >= Props.signalActions.Count) return;
        BuildingMoverSignalAction sa = Props.signalActions[actionIndex];
        if (sa == null || disabled || sliding || parent.Map == null) return;

        pendingSignalActionIndex = actionIndex;
        bool started = false;
        switch (sa.action)
        {
            case BuildingMoverAction.Open:
                started = Props.sensorDoor ? OpenDoor() : SignalMoveToEnd(true);
                break;
            case BuildingMoverAction.Close:
                started = Props.sensorDoor ? CloseDoor() : SignalMoveToEnd(false);
                break;
            case BuildingMoverAction.Toggle:
                started = Props.sensorDoor ? TryToggleDoorManual() : TryToggleTrack();
                break;
            case BuildingMoverAction.Move:
                IntVec3 dir = ResolveRelativeDir(sa.moveDir, parent.Rotation, sa.customDirection, -slideDir);
                int want = sa.moveDistance > 0 ? sa.moveDistance : Props.moveDistance;
                started = TrySignalMove(dir, Props.crushOutbound, want);
                break;
            case BuildingMoverAction.Rotate:
                started = TryRotateInternal();
                if (started) FireSignalActionComplete();
                break;
        }
        if (!started) pendingSignalActionIndex = -1;
    }

    // 非感应门定向开关 沿首方向往返
    private bool SignalMoveToEnd(bool outbound)
    {
        List<IntVec3> dirs = new List<IntVec3>(ResolveDirections());
        if (dirs.Count == 0) return false;
        IntVec3 fwd = dirs[0];
        IntVec3 dir = outbound ? fwd : -fwd;
        int wanted = outbound ? WantedDistance() : Mathf.Abs(trackOffset);
        bool crush = outbound ? Props.crushOutbound : Props.crushReturn;
        return TryMoveInternal(dir, crush, wanted);
    }

    // 尝试信号移动
    private bool TrySignalMove(IntVec3 dir, bool crush, int wanted)
    {
        pendingDisableAfterMove = Props.disableAfterSignalMove;
        bool moved = TryMoveInternal(dir, crush, wanted);
        if (!moved) pendingDisableAfterMove = false;
        return moved;
    }

    // 发出动作完成信号
    private void FireSignalActionComplete()
    {
        if (pendingSignalActionIndex < 0) return;
        int idx = pendingSignalActionIndex;
        pendingSignalActionIndex = -1;
        if (Props.signalActions == null || idx >= Props.signalActions.Count) return;
        string tag = Props.signalActions[idx]?.sendSignalOnComplete;
        if (tag.NullOrEmpty()) return;
        Find.SignalManager.SendSignal(new Signal(tag, parent.Named("SUBJECT"), parent.Position.Named("POSITION")));
    }

    // 完成移动动作
    private void CompleteMoveAction()
    {
        DisableAfterSignalMove();
        FireSignalActionComplete();
    }

    // 禁用一次性移动
    private void DisableAfterSignalMove()
    {
        if (!pendingDisableAfterMove) return;
        pendingDisableAfterMove = false;
        disabled = true;
        doorOpen = false;
        sensorDoorTargetOpen = 0;
        if (Props.sensorDoor) DeregisterSensorDoorRuntime();
    }
}
