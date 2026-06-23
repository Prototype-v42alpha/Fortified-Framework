using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.Sound;

namespace Fortified;

// 自毁演出阶段
public enum SelfDestructPhase
{
    Idle,       // 未启动
    Move,       // 四向各移1格
    Rise,       // 视觉上升
    Boom        // 爆炸摧毁
}

public class CompProperties_SelfDestructSequence : CompProperties
{
    // 每个移动阶段间隔tick
    public int moveStepTicks = 40;
    // 上升持续tick
    public int riseTicks = 90;
    // 爆炸半径
    public float explosionRadius = 8f;
    // 爆炸伤害
    public int explosionDamage = 200;
    // 爆炸伤害类型
    public DamageDef explosionDamageDef;
    // 爆炸音效
    public SoundDef explosionSound;
    // 监听触发自毁的信号tag
    public string triggerSignalTag;

    public CompProperties_SelfDestructSequence()
    {
        compClass = typeof(CompSelfDestructSequence);
    }
}

// 自毁演出状态机 移动-旋转-上升-爆炸
public class CompSelfDestructSequence : ThingComp
{
    private SelfDestructPhase phase = SelfDestructPhase.Idle;
    private int phaseTicks;
    private int moveStep;

    private CompBuildingMover mover;

    public CompProperties_SelfDestructSequence Props => (CompProperties_SelfDestructSequence)props;

    private CompBuildingMover Mover => mover ??= parent.GetComp<CompBuildingMover>();

    // 四向偏移 上下左右
    private static readonly IntVec3[] Dirs =
        { IntVec3.North, IntVec3.South, IntVec3.East, IntVec3.West };

    public bool Active => phase != SelfDestructPhase.Idle;

    // 外部触发启动
    public void StartSequence()
    {
        if (Active) return;
        phase = SelfDestructPhase.Move;
        phaseTicks = 0;
        moveStep = 0;
    }

    // 信号触发自毁
    public override void Notify_SignalReceived(Signal signal)
    {
        base.Notify_SignalReceived(signal);
        if (Props.triggerSignalTag.NullOrEmpty()) return;
        if (signal.tag != Props.triggerSignalTag) return;
        StartSequence();
    }

    public override void CompTick()
    {
        base.CompTick();
        if (phase == SelfDestructPhase.Idle || parent.Map == null) return;
        phaseTicks++;

        switch (phase)
        {
            case SelfDestructPhase.Move: TickMove(); break;
            case SelfDestructPhase.Rise: TickRise(); break;
            case SelfDestructPhase.Boom: DoBoom(); break;
        }
    }

    // 四向逐个移动1格
    private void TickMove()
    {
        if (Mover == null) { StartRise(); return; }
        if (Mover.Sliding) return;
        if (phaseTicks < Props.moveStepTicks) return;
        phaseTicks = 0;

        if (moveStep >= Dirs.Length)
        {
            StartRise();
            return;
        }
        Mover.TryMove(Dirs[moveStep], 1);
        moveStep++;
    }

    // 进入上升阶段
    private void StartRise()
    {
        phase = SelfDestructPhase.Rise;
        phaseTicks = 0;
    }

    // 上升阶段计时
    private void TickRise()
    {
        if (phaseTicks >= Props.riseTicks)
        {
            phase = SelfDestructPhase.Boom;
        }
    }

    // 爆炸并摧毁
    private void DoBoom()
    {
        IntVec3 pos = parent.Position;
        Map map = parent.Map;
        DamageDef dam = Props.explosionDamageDef ?? DamageDefOf.Bomb;
        GenExplosion.DoExplosion(pos, map, Props.explosionRadius, dam, parent,
            Props.explosionDamage, -1f, Props.explosionSound, parent.def);
        if (!parent.Destroyed) parent.Destroy(DestroyMode.KillFinalize);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref phase, "selfDestructPhase", SelfDestructPhase.Idle);
        Scribe_Values.Look(ref phaseTicks, "selfDestructPhaseTicks", 0);
        Scribe_Values.Look(ref moveStep, "selfDestructMoveStep", 0);
    }
}
