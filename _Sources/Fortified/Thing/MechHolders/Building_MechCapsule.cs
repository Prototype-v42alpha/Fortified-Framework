// 当白昼倾坠之时
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Fortified
{
    // 通用机兵休眠容器
    // 可容纳任意尺寸的机兵，自动绘制内部机兵
    public class Building_MechCapsule : Building, IThingHolder
    {
        #region 字段

        public ThingOwner<Pawn> innerContainer;

        #endregion

        #region 构造函数

        public Building_MechCapsule()
        {
            innerContainer = new ThingOwner<Pawn>(this, false, LookMode.Deep);
            innerContainer.dontTickContents = true;
        }

        #endregion

        #region 属性

        public bool HasMech => innerContainer != null && innerContainer.Count > 0;

        public Pawn Mech => HasMech ? innerContainer[0] : null;

        private ModExtension_MechCapsule Extension => def.GetModExtension<ModExtension_MechCapsule>();

        #endregion

        #region IThingHolder

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

        #endregion

        #region 生命周期

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            // 遗迹生成模式：如果没有机兵且有配置，随机生成一个
            if (!respawningAfterLoad && !HasMech)
            {
                TryGenerateRandomMech();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);

            if (innerContainer == null)
            {
                innerContainer = new ThingOwner<Pawn>(this, false, LookMode.Deep);
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (mode != DestroyMode.Vanish)
            {
                innerContainer.ClearAndDestroyContents();
            }
            base.Destroy(mode);
        }

        #endregion

        #region 公共方法

        // 尝试放入机兵
        public bool TryAcceptMech(Pawn mech)
        {
            if (mech == null) return false;
            if (HasMech)
            {
                Log.Warning("[FFF] MechCapsule already contains a mech");
                return false;
            }

            return innerContainer.TryAdd(mech);
        }

        // 激活机兵（由机械师控制）
        public void ActivateMech(Pawn mechanitor)
        {
            if (!HasMech)
            {
                Log.Error("[FFF] ActivateMech: no mech in capsule");
                return;
            }

            if (mechanitor == null || !MechanitorUtility.IsMechanitor(mechanitor))
            {
                Log.Error("[FFF] ActivateMech: invalid mechanitor");
                return;
            }

            Pawn mech = Mech;

            // 检查带宽
            float bandwidthCost = mech.GetStatValue(StatDefOf.BandwidthCost);
            float availableBandwidth = mechanitor.mechanitor.TotalBandwidth - mechanitor.mechanitor.UsedBandwidth;
            if (availableBandwidth < bandwidthCost)
            {
                Messages.Message("FFF.NeedMoreBandwidth".Translate(bandwidthCost), mechanitor, MessageTypeDefOf.RejectInput);
                return;
            }

            // 设置派系
            mech.SetFaction(Faction.OfPlayer);

            // 建立 overseer 关系
            mechanitor.relations.AddDirectRelation(PawnRelationDefOf.Overseer, mech);

            // 释放机兵
            innerContainer.TryDropAll(Position, Map, ThingPlaceMode.Near);

            // 销毁容器
            Destroy(DestroyMode.Vanish);

            Messages.Message("FFF.MechActivated".Translate(mech.LabelCap, mechanitor.LabelShort), mech, MessageTypeDefOf.PositiveEvent);
        }

        // 弹出并销毁机兵
        public void EjectAndDestroy(Pawn actor)
        {
            if (!HasMech) return;

            Pawn mech = Mech;
            innerContainer.TryDrop(mech, ThingPlaceMode.Near, 1, out _);

            if (mech != null && !mech.Dead)
            {
                mech.SetFactionDirect(Faction.OfAncients);
                mech.Kill(new DamageInfo(DamageDefOf.ExecutionCut, 200));
            }

            Destroy(DestroyMode.Vanish);
        }

        #endregion

        #region 右键菜单

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            if (!HasMech) yield break;

            if (!selPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
            {
                yield return new FloatMenuOption("FFF.CannotReach".Translate(), null);
                yield break;
            }

            // 控制选项
            if (selPawn.WorkTypeIsDisabled(WorkTypeDefOf.Research))
            {
                yield return CreateDisabledOption("FFF.Reason.WorkTypeDisabled".Translate());
            }
            else if (!MechanitorUtility.IsMechanitor(selPawn))
            {
                yield return CreateDisabledOption("FFF.Reason.NotMechanitor".Translate());
            }
            else
            {
                Pawn mech = Mech;
                if (mech == null) yield break;

                float bandwidthCost = mech.GetStatValue(StatDefOf.BandwidthCost);
                float availableBandwidth = selPawn.mechanitor.TotalBandwidth - selPawn.mechanitor.UsedBandwidth;

                if (availableBandwidth < bandwidthCost)
                {
                    yield return CreateDisabledOption("FFF.Reason.NeedBandwidth".Translate(bandwidthCost));
                }
                else
                {
                    yield return new FloatMenuOption("FFF.DeactivatedMech_Control".Translate(Mech.LabelCap), delegate
                    {
                        selPawn.jobs.TryTakeOrderedJob(new Job(FFF_DefOf.FFF_HackMechCapsule, this));
                    });
                }
            }
        }

        private FloatMenuOption CreateDisabledOption(string reason)
        {
            return new FloatMenuOption("FFF.DeactivatedMech_CannotControl".Translate() + ": " + reason, null);
        }

        #endregion

        #region 属性重写

        // 显示机兵名字而非建筑名字
        public override string Label
        {
            get
            {
                if (HasMech)
                {
                    return Mech.LabelCap;
                }
                return base.Label;
            }
        }

        // 缓存机兵图形
        private Graphic cachedMechGraphic;

        // 返回机兵图形
        public override Graphic Graphic
        {
            get
            {
                Pawn mech = Mech;
                if (mech != null)
                {
                    if (cachedMechGraphic == null)
                    {
                        cachedMechGraphic = mech.Drawer?.renderer?.BodyGraphic;
                    }
                    if (cachedMechGraphic != null) return cachedMechGraphic;
                }
                return base.Graphic;
            }
        }

        #endregion

        #region 绘制

        // 绘制机兵图形
        public override void Print(SectionLayer layer)
        {
            if (HasMech)
            {
                // 绘制机兵图形
                Graphic mechGraphic = Mech.Drawer.renderer.BodyGraphic;
                if (mechGraphic != null)
                {
                    mechGraphic.Print(layer, this, 0f);
                }
            }
            else
            {
                // 没有机兵时绘制默认图形
                base.Print(layer);
            }
        }

        public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
        {
            if (HasMech && phase == DrawPhase.Draw)
            {
                Vector3 mechDrawLoc = drawLoc;
                if (Extension != null)
                {
                    mechDrawLoc += Extension.innerPawnDrawOffset;
                }

                Mech.Drawer.renderer.DynamicDrawPhaseAt(phase, mechDrawLoc, Rotation, false);
            }
            else
            {
                base.DynamicDrawPhaseAt(phase, drawLoc, flip);
            }
        }

        public override string GetInspectString()
        {
            string text = base.GetInspectString();

            if (HasMech)
            {
                if (!text.NullOrEmpty()) text += "\n";
                text += "FFF.ContainsMech".Translate(Mech.LabelCap);
            }

            return text;
        }

        #endregion

        #region 私有方法

        // 遗迹生成模式：随机生成机兵
        private void TryGenerateRandomMech()
        {
            var ext = Extension;
            if (ext?.possibleGeneratePawn == null || ext.possibleGeneratePawn.Count == 0 || !Rand.Chance(ext.spawnChance))
            {
                if (!HasMech) Destroy(DestroyMode.Vanish);
                return;
            }

            GenerateAndSetupMech(ext);
        }

        private void GenerateAndSetupMech(ModExtension_MechCapsule ext)
        {
            PawnGenOption selected = ext.possibleGeneratePawn.RandomElementByWeight(p => p.selectionWeight);
            if (selected?.kind == null) return;

            Pawn mech = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                selected.kind, Faction ?? Faction.OfAncients ?? Faction.OfPirates,
                PawnGenerationContext.NonPlayer, Map?.Tile ?? -1
            ));

            if (mech == null) { Destroy(DestroyMode.Vanish); return; }
            SetupPawnVisuals(mech);
            SetupPawnInventory(mech, ext);
            ApplyRandomDamage(mech, ext);
            innerContainer.TryAdd(mech);
        }

        private void SetupPawnVisuals(Pawn mech)
        {
            if (mech.kindDef?.nameMaker != null)
                mech.Name = PawnBioAndNameGenerator.GenerateFullPawnName(mech.def, mech.kindDef.nameMaker);
        }

        private void SetupPawnInventory(Pawn mech, ModExtension_MechCapsule ext)
        {
            if (!Rand.Chance(ext.weaponChance) && mech.equipment?.Primary != null && !mech.equipment.Primary.def.destroyOnDrop)
            {
                mech.equipment.DestroyAllEquipment();
                mech.inventory?.DestroyAll();
            }
        }

        private void ApplyRandomDamage(Pawn mech, ModExtension_MechCapsule ext)
        {
            if (ext.damageHediffs == null || ext.damageHediffs.Count == 0) return;

            int damageCount = ext.damageCount.RandomInRange;
            if (damageCount <= 0) return;

            for (int i = 0; i < damageCount; i++)
            {
                var parts = mech.RaceProps?.body?.AllParts;
                if (parts == null) break;

                var validParts = new List<BodyPartRecord>();
                foreach (var part in parts)
                {
                    if (!part.IsCorePart && !mech.health.hediffSet.HasMissingPartFor(part))
                    {
                        validParts.Add(part);
                    }
                }

                if (validParts.Count == 0) break;

                var targetPart = validParts.RandomElement();
                var hediffDef = ext.damageHediffs.RandomElement();
                mech.health.AddHediff(hediffDef, targetPart);
            }
        }

        #endregion
    }
}
