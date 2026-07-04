using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Fortified;

// 移动触发方式
public enum BuildingMoveTrigger
{
    Gizmo,      // 玩家手动按钮
    Signal,     // 信号驱动
    Proximity   // 靠近自动
}

// 移动距离方式
public enum BuildingMoveMode
{
    Teleport,   // 瞬移一格
    Slide,      // 平滑滑动一格
    SlideToEnd  // 滑动到底
}

// 水平方向定义
public enum BuildingMoveAxis
{
    FacingSides,    // 朝向左右两侧
    FacingFourWay,  // 朝向前后左右四向
    MapEastWest,    // 固定地图东西向
    Custom          // 配置指定方向
}

// 相对朝向单方向
public enum BuildingRelativeDir
{
    Forward,    // 建筑正面
    Backward,   // 建筑背面
    Left,       // 建筑左侧
    Right,      // 建筑右侧
    Custom,     // 配置绝对偏移
    MoveReverse // 当前移动反向
}

// 信号可触发的动作
public enum BuildingMoverAction
{
    Open,       // 开门到底
    Close,      // 关门归位
    Toggle,     // 开关切换
    Move,       // 定向移动
    Rotate      // 旋转
}

// 信号到动作的映射项
public class BuildingMoverSignalAction
{
    // 监听信号标记
    public string signalTag;
    // 收到后执行的动作
    public BuildingMoverAction action = BuildingMoverAction.Toggle;
    // 移动相对方向
    public BuildingRelativeDir moveDir = BuildingRelativeDir.Forward;
    // 自定义移动偏移
    public IntVec3 customDirection = IntVec3.East;
    // 移动距离覆盖
    public int moveDistance = 0;
    // 完成信号标记
    public string sendSignalOnComplete;
}

public class CompProperties_BuildingMover : CompProperties
{
    // 启用的触发方式
    public bool allowGizmo = true;
    public bool allowSignal = false;
    public bool allowProximity = false;

    // 移动方式
    public BuildingMoveMode moveMode = BuildingMoveMode.Slide;

    // 水平方向定义
    public BuildingMoveAxis axis = BuildingMoveAxis.FacingSides;

    // 自定义方向偏移
    public IntVec3 customDirection = IntVec3.East;

    // 单次移动格数
    public int moveDistance = 1;

    // 开关轨道往复模式
    public bool trackToggle = false;

    // 仅保留正向单按钮
    public bool oneWayOnly = false;

    // 去程是否碾压通过
    public bool crushOutbound = false;

    // 回程是否碾压通过
    public bool crushReturn = false;

    // 碾压摧毁挡路建筑物品
    public bool crushBuildings = false;

    // 碾压摧毁挡路物品
    public bool crushItems = false;

    // 强行摧毁不可摧毁物
    public bool crushIndestructible = false;

    // 碾压碾杀挡路小人
    public bool crushPawns = false;

    // 碾压矿脉采集矿物
    public bool mineWhileCrushing = false;

    // 采矿产量系数
    public float mineYieldFactor = 1f;

    // 搬运碾过掉落物
    public bool haulCrushedDrops = false;

    // 掉落物搬运方向
    public BuildingRelativeDir haulDropDirection = BuildingRelativeDir.MoveReverse;

    // 自定义搬运偏移
    public IntVec3 haulDropCustomDir = IntVec3.South;

    // 碾压伤害类型 默认钝击
    public DamageDef crushDamageDef;

    // 对小人碾压伤害
    public float crushDamagePawn = 9999f;

    // 对建筑矿脉碾压伤害
    public float crushDamageBuilding = 9999f;

    // 滑动速度
    public float slideCellsPerTick = 0.05f;

    // 移动总耗时
    public int moveDurationTicks = 0;

    // 速度缓动曲线
    public SimpleCurve slideSpeedCurve;

    // 碾压把小人推到旁边
    public bool crushPushPawn = false;

    // 信号触发标签
    public string listenSignalTag = "Fortified.BuildingMover";

    // 信号动作映射
    public List<BuildingMoverSignalAction> signalActions;

    // 信号移动后禁用
    public bool disableAfterSignalMove = false;

    // 靠近触发检测半径
    public float proximityRadius = 3f;

    // 靠近触发检测间隔
    public int proximityCheckInterval = 30;

    // 靠近仅检测人形
    public bool proximityOnlyHumanlike = false;

    // 靠近仅检测敌对
    public bool proximityOnlyHostile = true;

    // 移动音效
    public SoundDef moveSound;

    // 滑动烟尘粒子
    public FleckDef slideFleckDef;
    // 烟尘生成间隔
    public int slideFleckIntervalTicks = 3;
    // 烟尘缩放
    public float slideFleckScale = 1.5f;

    // 按钮图标路径
    public string gizmoIconPath;

    // 按钮文本覆盖正向
    public string gizmoLabelForward;

    // 按钮文本覆盖反向
    public string gizmoLabelBackward;

    // 按钮提示覆盖
    public string gizmoDesc;

    // 启用旋转按钮
    public bool allowRotate = false;
    // 旋转按钮图标路径
    public string rotateIconPath;

    // 启用感应门
    public bool sensorDoor = false;

    // 感应门移动方向
    public BuildingMoveAxis sensorDoorAxis = BuildingMoveAxis.FacingSides;

    // 感应门让路格数
    public int sensorDoorDistance = 1;

    // 感应门移动耗时
    public int sensorDoorOpenTicks = 60;

    // 感应门自动关闭
    public bool sensorDoorAutoClose = true;

    // 自动关闭延迟
    public int sensorDoorCloseDelay = 120;

    // 检查开门阵营
    public bool sensorDoorCheckFaction = true;

    // 关门寻路代价
    public int sensorDoorPathCost = 50;

    // 每格等待代价
    public int sensorDoorWaitCostPerCell = 12;

    // 感应门允许手动开关按钮
    public bool sensorDoorAllowManual = false;

    public CompProperties_BuildingMover()
    {
        compClass = typeof(CompBuildingMover);
    }

    // 验证信号接收
    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string e in base.ConfigErrors(parentDef)) yield return e;
        bool wantsSignal = allowSignal || (signalActions != null && signalActions.Count > 0);
        if (wantsSignal && !parentDef.receivesSignals)
            yield return parentDef.defName + " 配置了信号触发(allowSignal/signalActions)但未设 receivesSignals=true 无法接收信号";
    }

    // 绘制放置路径
    public override void DrawGhost(IntVec3 center, Rot4 rot, ThingDef thingDef, Color ghostCol, AltitudeLayer drawAltitude, Thing thing = null)
    {
        base.DrawGhost(center, rot, thingDef, ghostCol, drawAltitude, thing);
        BuildingMoverGhostDrawer.DrawPath(this, thingDef, center, rot);
    }

    // 注册放置提示
    public override void ResolveReferences(ThingDef parentDef)
    {
        base.ResolveReferences(parentDef);
        parentDef.placeWorkers ??= new List<Type>();
        if (!parentDef.placeWorkers.Contains(typeof(PlaceWorker_BuildingMover)))
            parentDef.placeWorkers.Add(typeof(PlaceWorker_BuildingMover));
    }
}
