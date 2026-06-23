using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Fortified;

public partial class CompBuildingMover
{
    // 碾压护甲穿透满值
    private const float CrushArmorPen = 999f;

    // 侧推搜索最大格数
    private const int MaxPushAsideCells = 5;

    // 碾压伤害类型兜底
    private DamageDef CrushDamage => Props.crushDamageDef ?? DamageDefOf.Crush;

    // 碾压新占格 返回是否全部清空
    private bool CrushCellsForMove(IntVec3 dir)
    {
        CellRect target = parent.OccupiedRect().MovedBy(dir);
        CellRect self = parent.OccupiedRect();
        bool clear = true;
        foreach (IntVec3 c in target)
        {
            if (self.Contains(c) || !c.InBounds(parent.Map)) continue;
            if (!CrushCell(c)) clear = false;
        }
        return clear;
    }

    // 碾碎单格 返回该格是否已清空
    private bool CrushCell(IntVec3 c)
    {
        List<Thing> list = c.GetThingList(parent.Map);
        for (int i = list.Count - 1; i >= 0; i--)
        {
            Thing t = list[i];
            if (t == parent) continue;
            CrushThing(t);
        }
        if (Props.haulCrushedDrops) HaulDropsFromCell(c);
        return CellFreeForParent(c, true);
    }

    // 按类型碾碎单个物体
    private void CrushThing(Thing t)
    {
        if (t is Pawn p) { if (Props.crushPawns) CrushPawn(p); return; }
        // 矿脉无视覆盖规则直接碾
        if (t is Mineable mineable) { if (Props.crushBuildings) CrushMineable(mineable); return; }
        bool isBuilding = t.def.category == ThingCategory.Building;
        bool isItem = t.def.category == ThingCategory.Item;
        if (isBuilding && !Props.crushBuildings) return;
        if (isItem && !Props.crushItems) return;
        if (!isBuilding && !isItem) return;
        // 不可摧毁物按配置强行摧毁
        if (!t.def.destroyable)
        {
            if (Props.crushIndestructible) t.Destroy(DestroyMode.KillFinalize);
            return;
        }
        t.TakeDamage(new DamageInfo(CrushDamage, Props.crushDamageBuilding, CrushArmorPen, -1f, parent));
    }

    // 碾压矿脉并产矿
    private void CrushMineable(Mineable mineable)
    {
        if (!Props.mineWhileCrushing)
        {
            mineable.TakeDamage(new DamageInfo(CrushDamage, Props.crushDamageBuilding, CrushArmorPen, -1f, parent));
            return;
        }
        // 按系数拉满yieldPct再走原版采矿
        int amount = Mathf.CeilToInt(mineable.MaxHitPoints * Props.mineYieldFactor);
        mineable.Notify_TookMiningDamage(amount, null);
        mineable.DestroyMined(null);
    }

    // 把该格掉落物搬运到本体后方
    private void HaulDropsFromCell(IntVec3 c)
    {
        Map map = parent.Map;
        IntVec3 target = HaulTargetCell(c);
        if (!target.InBounds(map)) target = c;
        List<Thing> list = c.GetThingList(map);
        for (int i = list.Count - 1; i >= 0; i--)
        {
            Thing t = list[i];
            if (t.def.category != ThingCategory.Item) continue;
            t.DeSpawn();
            // 后方放不下则原地落地避免丢物
            if (!GenPlace.TryPlaceThing(t, target, map, ThingPlaceMode.Near))
                GenPlace.TryPlaceThing(t, c, map, ThingPlaceMode.Direct);
        }
    }

    // 计算搬运落点 沿配置方向越过本体
    private IntVec3 HaulTargetCell(IntVec3 c)
    {
        IntVec3 dir = ResolveRelativeDir(Props.haulDropDirection, parent.Rotation, Props.haulDropCustomDir, -slideDir);
        CellRect rect = parent.OccupiedRect();
        int span = dir.x != 0 ? rect.Width : rect.Height;
        return c + dir * (span + 1);
    }

    // 碾压小人
    private void CrushPawn(Pawn p)
    {
        if (p.Dead) return;
        DamageInfo dinfo = new DamageInfo(CrushDamage, Props.crushDamagePawn, CrushArmorPen, -1f, parent,
            p.health.hediffSet.GetBrain(), null, DamageInfo.SourceCategory.Collapse);
        p.TakeDamage(dinfo);
        // 受伤后推到旁边空格
        if (Props.crushPushPawn && !p.Dead) PushPawnAside(p);
    }

    // 把小人挤到路径侧面格
    private void PushPawnAside(Pawn p)
    {
        // 垂直于移动方向的两个侧向
        IntVec3 side1 = slideDir.RotatedBy(RotationDirection.Clockwise);
        IntVec3 side2 = slideDir.RotatedBy(RotationDirection.Counterclockwise);

        IntVec3 dest;
        if (FindSideCell(p.Position, side1, out dest) || FindSideCell(p.Position, side2, out dest))
        {
            p.Position = dest;
            p.Notify_Teleported(false, true);
        }
    }

    // 沿侧向逐格找第一个可站立空格
    private bool FindSideCell(IntVec3 from, IntVec3 side, out IntVec3 dest)
    {
        CellRect self = parent.OccupiedRect();
        for (int d = 1; d <= MaxPushAsideCells; d++)
        {
            IntVec3 c = from + side * d;
            if (!c.InBounds(parent.Map)) break;
            if (self.Contains(c)) continue;
            if (c.Standable(parent.Map) && !c.Fogged(parent.Map)) { dest = c; return true; }
        }
        dest = IntVec3.Invalid;
        return false;
    }
}
