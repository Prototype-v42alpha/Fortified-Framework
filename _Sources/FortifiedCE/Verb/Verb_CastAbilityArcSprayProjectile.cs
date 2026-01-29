// 当白昼倾坠之时
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using CombatExtended;

namespace FortifiedCE
{
    // CE版弧形散射技能Verb 继承原版Verb_CastAbility避免武器干扰
    public class Verb_CastAbilityArcSprayProjectile : Verb_CastAbility
    {
        protected List<IntVec3> path = new List<IntVec3>();
        protected Vector3 initialTargetPosition;

        protected override int ShotsPerBurst => verbProps.burstShotCount;

        public override float? AimAngleOverride
        {
            get
            {
                if (state == VerbState.Bursting && Available() && path.Any())
                {
                    int idx = Mathf.Min(path.Count - 1, ShotsPerBurst - burstShotsLeft);
                    return (path[idx].ToVector3Shifted() - caster.DrawPos).AngleFlat();
                }
                return null;
            }
        }

        protected override bool TryCastShot()
        {
            // 首发调用基类启动冷却
            if (burstShotsLeft == verbProps.burstShotCount && !base.TryCastShot())
            {
                return false;
            }
            if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
            {
                return false;
            }
            if (verbProps.stopBurstWithoutLos && !TryFindShootLineFromTo(caster.Position, currentTarget, out _))
            {
                return false;
            }

            int idx = ShotsPerBurst - burstShotsLeft;
            if (idx < path.Count)
            {
                HitCell(path[idx]);
            }
            lastShotTick = Find.TickManager.TicksGame;
            return true;
        }

        public override bool Available()
        {
            return ShotsPerBurst - burstShotsLeft >= 0;
        }

        public override void WarmupComplete()
        {
            burstShotsLeft = ShotsPerBurst;
            state = VerbState.Bursting;
            initialTargetPosition = currentTarget.CenterVector3;
            PreparePath();
            TryCastNextBurstShot();
        }

        // 准备弧形散射路径
        protected void PreparePath()
        {
            path.Clear();
            Vector3 normalized = (currentTarget.CenterVector3 - caster.Position.ToVector3Shifted()).Yto0().normalized;
            Vector3 tan = normalized.RotatedBy(90f);
            for (int i = 0; i < verbProps.sprayNumExtraCells; i++)
            {
                for (int j = 0; j < 15; j++)
                {
                    float value = Rand.Value;
                    float num = Rand.Value - 0.5f;
                    float num2 = value * verbProps.sprayWidth * 2f - verbProps.sprayWidth;
                    float num3 = num * verbProps.sprayThicknessCells + num * 2f * verbProps.sprayArching;
                    IntVec3 item = (currentTarget.CenterVector3 + num2 * tan - num3 * normalized).ToIntVec3();
                    if (!path.Contains(item) || Rand.Value < 0.25f)
                    {
                        path.Add(item);
                        break;
                    }
                }
            }
            path.Add(currentTarget.Cell);
            path.SortBy((IntVec3 c) => (c.ToVector3Shifted() - caster.DrawPos).Yto0().normalized.AngleToFlat(tan));
        }

        // 使用CE弹道系统发射投射物
        protected virtual void HitCell(IntVec3 cell)
        {
            verbProps.sprayEffecterDef?.Spawn(caster.Position, cell, caster.Map);

            ThingDef projectileDef = verbProps.defaultProjectile;
            if (projectileDef == null) return;

            // 检查是否是CE投射物
            if (typeof(ProjectileCE).IsAssignableFrom(projectileDef.thingClass))
            {
                LaunchProjectileCE(projectileDef, cell);
            }
            else
            {
                // 原版投射物回退
                Projectile projectile = (Projectile)GenSpawn.Spawn(projectileDef, caster.Position, caster.Map, WipeMode.Vanish);
                projectile.Launch(caster, caster.DrawPos, cell, cell, ProjectileHitFlags.All, false, null, null);
            }
        }

        // CE投射物发射逻辑
        protected void LaunchProjectileCE(ThingDef projectileDef, IntVec3 targetCell)
        {
            Pawn pawn = caster as Pawn;
            if (pawn == null) return;

            Vector3 sourcePos = pawn.TrueCenter();
            float shotHeight = new CollisionVertical(pawn).shotHeight;
            sourcePos = sourcePos.WithY(shotHeight);

            // 目标是地面格子，高度为0
            Vector3 targetPos = targetCell.ToVector3Shifted().WithY(0f);

            if (projectileDef.projectile is ProjectilePropertiesCE ppce)
            {
                float shotAngle = ppce.TrajectoryWorker.ShotAngle(ppce, sourcePos, targetPos);
                float shotRotation = ppce.TrajectoryWorker.ShotRotation(ppce, sourcePos, targetPos);
                CE_Utility.LaunchProjectileCE(
                    projectileDef,
                    new Vector2(sourcePos.x, sourcePos.z),
                    new LocalTargetInfo(targetCell),
                    pawn,
                    shotAngle,
                    shotRotation,
                    shotHeight,
                    ppce.speed
                );
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref path, "path", LookMode.Value);
            Scribe_Values.Look(ref initialTargetPosition, "initialTargetPosition");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && path == null)
            {
                path = new List<IntVec3>();
            }
        }
    }
}