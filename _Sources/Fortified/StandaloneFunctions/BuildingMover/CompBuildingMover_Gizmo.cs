using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Fortified;

public partial class CompBuildingMover
{
    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo g in base.CompGetGizmosExtra()) yield return g;
        if (disabled) yield break;
        if (!Props.allowGizmo || !parent.Spawned) yield break;
        // 感应门由寻路驱动 仅在允许手动时出开关按钮
        if (Props.sensorDoor)
        {
            if (Props.sensorDoorAllowManual) yield return MakeDoorToggleCommand();
            yield break;
        }

        if (Props.trackToggle)
        {
            yield return MakeToggleCommand();
        }
        else
        {
            List<IntVec3> dirs = new List<IntVec3>(ResolveDirections());
            int count = Props.oneWayOnly ? Mathf.Min(1, dirs.Count) : dirs.Count;
            for (int i = 0; i < count; i++)
            {
                IntVec3 dir = dirs[i];
                // 碾压模式仅受地图边界限制
                int reach = Props.crushOutbound ? MaxDistanceInBounds(dir, WantedDistance()) : MaxMovableDistance(dir, WantedDistance());
                bool enabled = !sliding && reach > 0;
                yield return MakeMoveCommand(dir, i, enabled);
            }
        }

        if (Props.allowRotate) yield return MakeRotateCommand();
    }

    // 构造感应门手动开关按钮
    private Command_Action MakeDoorToggleCommand()
    {
        string label = doorOpen
            ? (Props.gizmoLabelBackward.NullOrEmpty() ? "FFF.BuildingMover.Dir1".Translate().ToString() : Props.gizmoLabelBackward.Translate().ToString())
            : (Props.gizmoLabelForward.NullOrEmpty() ? "FFF.BuildingMover.Dir0".Translate().ToString() : Props.gizmoLabelForward.Translate().ToString());
        Command_Action cmd = new Command_Action
        {
            defaultLabel = label,
            defaultDesc = Props.gizmoDesc.NullOrEmpty() ? "FFF.BuildingMover.Desc".Translate().ToString() : Props.gizmoDesc.Translate().ToString(),
            action = () => SyncedDoorToggle(this)
        };
        if (!Props.gizmoIconPath.NullOrEmpty())
            cmd.icon = ContentFinder<Texture2D>.Get(Props.gizmoIconPath, false);
        if (sliding) cmd.Disable("FFF.BuildingMover.Blocked".Translate());
        return cmd;
    }

    // 构造开关轨道按钮
    private Command_Action MakeToggleCommand()
    {
        bool outbound = trackOffset == 0;
        string label = outbound
            ? (Props.gizmoLabelForward.NullOrEmpty() ? "FFF.BuildingMover.Dir0".Translate().ToString() : Props.gizmoLabelForward.Translate().ToString())
            : (Props.gizmoLabelBackward.NullOrEmpty() ? "FFF.BuildingMover.Dir1".Translate().ToString() : Props.gizmoLabelBackward.Translate().ToString());
        Command_Action cmd = new Command_Action
        {
            defaultLabel = label,
            defaultDesc = Props.gizmoDesc.NullOrEmpty() ? "FFF.BuildingMover.Desc".Translate().ToString() : Props.gizmoDesc.Translate().ToString(),
            action = () => SyncedToggle(this)
        };
        if (!Props.gizmoIconPath.NullOrEmpty())
            cmd.icon = ContentFinder<Texture2D>.Get(Props.gizmoIconPath, false);
        if (sliding) cmd.Disable("FFF.BuildingMover.Blocked".Translate());
        return cmd;
    }

    // 构造移动按钮
    private Command_Action MakeMoveCommand(IntVec3 dir, int index, bool enabled)
    {
        Command_Action cmd = new Command_Action
        {
            defaultLabel = ResolveLabel(index),
            defaultDesc = Props.gizmoDesc.NullOrEmpty() ? "FFF.BuildingMover.Desc".Translate().ToString() : Props.gizmoDesc.Translate().ToString(),
            action = () => SyncedMove(this, dir)
        };
        if (!Props.gizmoIconPath.NullOrEmpty())
            cmd.icon = ContentFinder<Texture2D>.Get(Props.gizmoIconPath, false);
        if (!enabled) cmd.Disable("FFF.BuildingMover.Blocked".Translate());
        return cmd;
    }

    // 解析按钮文本
    private string ResolveLabel(int index)
    {
        if (index == 0 && !Props.gizmoLabelForward.NullOrEmpty()) return Props.gizmoLabelForward.Translate().ToString();
        if (index == 1 && !Props.gizmoLabelBackward.NullOrEmpty()) return Props.gizmoLabelBackward.Translate().ToString();
        return ("FFF.BuildingMover.Dir" + index).Translate();
    }

    // 旋转按钮
    private Command_Action MakeRotateCommand()
    {
        Command_Action cmd = new Command_Action
        {
            defaultLabel = "FFF.BuildingMover.Rotate".Translate(),
            defaultDesc = "FFF.BuildingMover.RotateDesc".Translate(),
            action = () => SyncedRotate(this)
        };
        if (!Props.rotateIconPath.NullOrEmpty())
            cmd.icon = ContentFinder<Texture2D>.Get(Props.rotateIconPath, false);
        if (sliding) cmd.Disable("FFF.BuildingMover.Blocked".Translate());
        return cmd;
    }
}
