using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Multiplayer.API;

namespace Fortified
{
    public class CompMultipleTurretGun : ThingComp
    {
        public bool IsApparel => this.parent.def.IsApparel;
        public Pawn PawnOwner
        {
            get
            {
                if (parent is Pawn result) return result;
                if (parent is Apparel apparel) return apparel.Wearer;
                return null;
            }
        }
        public CompPropertiesMultipleTurretGun Props => (CompPropertiesMultipleTurretGun)this.props;
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                SetupTurrets();
            }
            turrets.RemoveDuplicates((a, b) => a.ID == b.ID);
            currentTurret ??= turrets.First().ID;
        }
        private void SetupTurrets()
        {
            Props.subTurrets.ForEach(t =>
            {
                SubTurret turret = new SubTurret() { ID = t.ID, parent = this.PawnOwner };
                turret.Init(t);
                turrets.Add(turret);
            });
            turrets.RemoveDuplicates((a, b) => a.ID == b.ID);
            currentTurret ??= turrets.First().ID;
        }
        public override void CompTick()
        {
            base.CompTick();
            if (!this.parent.Spawned) return;
            for (int i = 0; i < turrets.Count; i++)
            {
                turrets[i].Tick();
            }
        }
        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            foreach (Gizmo item in base.CompGetWornGizmosExtra())
            {
                yield return item;
            }
            if (!IsApparel) yield break;
            foreach (Gizmo gizmo in GetGizmos())
            {
                yield return gizmo;
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo item in base.CompGetGizmosExtra())
            {
                yield return item;
            }
            if (IsApparel) yield break;
            foreach (Gizmo gizmo in GetGizmos())
            {
                yield return gizmo;
            }
        }
        private IEnumerable<Gizmo> GetGizmos()
        {
            if (PawnOwner != null && PawnOwner.Faction != null && PawnOwner.Faction.IsPlayer)
            {
                yield return new SubturretGizmo(this);
            }
        }
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            if (!this.Props.subTurrets.NullOrEmpty())
            {
                foreach (SubTurretProperties t in this.Props.subTurrets)
                {
                    yield return new StatDrawEntry(StatCategoryDefOf.PawnCombat, "Turret".Translate(), t.turret.LabelCap, "Stat_Thing_TurretDesc".Translate(), 5600, null, Gen.YieldSingle<Dialog_InfoCard.Hyperlink>(new Dialog_InfoCard.Hyperlink(t.turret, -1)), false, false);
                }
            }
            yield break;
        }
        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            SetupTurrets();
        }
        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref turrets, "turrets", LookMode.Deep);
            Scribe_Values.Look(ref currentTurret, "currentTurrent");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Init();
            }
        }
        public override void Notify_DefsHotReloaded()
        {
            base.Notify_DefsHotReloaded();
            Init();
        }
        public override void CompDrawWornExtras()
        {
            base.CompDrawWornExtras();
            if (!IsApparel || PawnOwner == null ||PawnOwner.DeadOrDowned) return;
            foreach (SubTurret t in turrets)
            {
                if (t.TurretProp.renderNodeProperties.NullOrEmpty()) continue;
                DrawTuret(PawnOwner, t, t.turret);
            }
        }
        protected void DrawTuret(Pawn pawn, SubTurret turret, Thing equipment)
        {
            foreach (PawnRenderNodeProperties item in turret.TurretProp.renderNodeProperties)
            {
                if (item.nodeClass.IsAssignableFrom(typeof(Fortified.PawnRenderNode_SubTurretGun)))
                {
                    float aimAngle = (turret.HasTarget) ? turret.curRotation : item.drawData.RotationOffsetForRot(pawn.Rotation) + pawn.Rotation.AsAngle;
                    aimAngle -= 90;
                    aimAngle %= 360;
                    Vector3 drawLoc = pawn.DrawPos + item.drawData.OffsetForRot(pawn.Rotation);
                    Vector3 drawsize = new Vector3(item.drawSize.x, 0f, item.drawSize.y);

                    Mesh mesh;
                    if (aimAngle > 20f && aimAngle < 160f)
                    {
                        mesh = MeshPool.plane10;
                        aimAngle += equipment.def.equippedAngleOffset;
                    }
                    else if (aimAngle > 200f && aimAngle < 340f)
                    {
                        mesh = MeshPool.plane10Flip;
                        aimAngle -= 180f;
                        aimAngle -= equipment.def.equippedAngleOffset;
                    }
                    else
                    {
                        mesh = MeshPool.plane10;
                        aimAngle += equipment.def.equippedAngleOffset;
                    }
                    aimAngle %= 360f;
                    drawLoc.y = Altitudes.AltInc * item.drawData.LayerForRot(pawn.Rotation, 1) + pawn.DrawPos.y;
                    Material material = ((!(equipment.Graphic is Graphic_StackCount graphic_StackCount)) ? equipment.Graphic.MatSingleFor(equipment) : graphic_StackCount.SubGraphicForStackCount(1, equipment.def).MatSingleFor(equipment));
                    Matrix4x4 matrix = Matrix4x4.TRS(s: drawsize, pos: drawLoc, q: Quaternion.AngleAxis(aimAngle, Vector3.up));
                    Graphics.DrawMesh(mesh, matrix, material, 0);
                }
            }
        }

        public override List<PawnRenderNode> CompRenderNodes()
        {
            if (!IsApparel && PawnOwner != null)
            {
                List<PawnRenderNode> list = new List<PawnRenderNode>();

                foreach (SubTurret t in turrets)
                {
                    if (t.TurretProp.renderNodeProperties.NullOrEmpty()) continue;
                    list.AddRange(t.RenderNodes(PawnOwner));
                }
                return list;
            }
            return base.CompRenderNodes();
        }
        public void Init()
        {
            foreach (var t in turrets)
            {
                t.parent = PawnOwner;
                t.Init(Props.subTurrets.Find(
                     p => p.ID == t.ID));
            }
        }
        public List<SubTurret> turrets = new List<SubTurret>();
        public string currentTurret;
    }


    [StaticConstructorOnStartup]
    public class SubTurret : IAttackTargetSearcher, IExposable
    {
        public bool HasTarget => currentTarget != null;
        public Thing Thing => this.parent;
        // 缓存主武器verb避免每tick线性扫描
        public Verb CurrentEffectiveVerb => cachedPrimaryVerb ??= this.GunCompEq.PrimaryVerb;
        // 缓存装备组件避免每tick遍历comp
        public CompEquippable GunCompEq => cachedGunCompEq ??= this.turret.TryGetComp<CompEquippable>();
        public LocalTargetInfo LastAttackedTarget => this.lastAttackedTarget;
        public int LastAttackTargetTick => this.lastAttackTargetTick;
        public Pawn PawnOwner
        {
            get
            {
                if (!(parent is Apparel { Wearer: var wearer }))
                {
                    if (parent is Pawn result)
                    {
                        return result;
                    }
                    return null;
                }
                return wearer;
            }
        }
        private bool CanShoot(Pawn owner)
        {
            if (owner != null)
            {
                if (!owner.Spawned || owner.Downed || owner.Dead || !owner.Awake()) return false;
                if (owner.stances.stunner.Stunned) return false;
                if (IsTurretDestroyed(owner)) return false;
                if (owner.IsColonyMechPlayerControlled && !this.fireAtWill) return false;
            }
            if (!dormantResolved)
            {
                cachedDormant = this.parent.TryGetComp<CompCanBeDormant>();
                dormantResolved = true;
            }
            return cachedDormant == null || cachedDormant.Awake;
        }
        private bool WarmingUp => this.burstWarmupTicksLeft > 0;
        public bool TurretDestroyed => IsTurretDestroyed(PawnOwner);
        // 复用已取的owner避免重复模式匹配
        private bool IsTurretDestroyed(Pawn owner)
        {
            return owner != null
                && this.CurrentEffectiveVerb.verbProps.linkedBodyPartsGroup != null
                && this.CurrentEffectiveVerb.verbProps.ensureLinkedBodyPartsGroupAlwaysUsable
                && PawnCapacityUtility.CalculateNaturalPartsAverageEfficiency(owner.health.hediffSet, this.CurrentEffectiveVerb.verbProps.linkedBodyPartsGroup) <= 0f;
        }
        public SubTurretProperties TurretProp
        {
            get
            {
                if (turretProp == null)
                {
                    parent.TryGetComp<CompMultipleTurretGun>().Init();
                }
                return turretProp;
            }
        }
        public void Init(SubTurretProperties prop)
        {
            this.turretProp = prop;
            this.turret ??= ThingMaker.MakeThing(this.TurretProp.turret, null);
            // 重置缓存防换枪后失效
            cachedGunCompEq = null;
            cachedPrimaryVerb = null;
            cachedDormant = null;
            dormantResolved = false;
            this.UpdateGunVerbs();
        }
        private void UpdateGunVerbs()
        {
            List<Verb> allVerbs = this.turret.TryGetComp<CompEquippable>().AllVerbs;
            for (int i = 0; i < allVerbs.Count; i++)
            {
                Verb verb = allVerbs[i];
                verb.caster = this.parent;
                verb.verbProps.warmupTime = 0;
                verb.castCompleteCallback = delegate ()
                {
                    this.burstCooldownTicksLeft = this.CurrentEffectiveVerb.verbProps.defaultCooldownTime.SecondsToTicks();
                };
            }
        }
        public void Tick()
        {
            Pawn owner = PawnOwner;
            if (CanShoot(owner) == false)
            {
                return;
            }

            if (CheckTarget())
            {
                this.curRotation = (this.currentTarget.Cell.ToVector3Shifted() - owner.DrawPos).AngleFlat() + this.TurretProp.angleOffset;
            }
            else
            {
                this.curRotation = this.TurretProp.angleOffset + this.TurretProp.IdleAngleOffset + owner.Rotation.AsAngle;
            }
            this.CurrentEffectiveVerb.VerbTick();
            if (this.CurrentEffectiveVerb.state != VerbState.Bursting)
            {
                if (this.WarmingUp)
                {
                    this.burstWarmupTicksLeft--;
                    if (this.burstWarmupTicksLeft == 0)
                    {
                        this.CurrentEffectiveVerb.TryStartCastOn(this.currentTarget, this.currentTarget, false, true, false, true);
                        this.lastAttackTargetTick = Find.TickManager.TicksGame;
                        this.lastAttackedTarget = this.currentTarget;
                        return;
                    }
                }
                else
                {
                    if (this.burstCooldownTicksLeft > 0)
                    {
                        this.burstCooldownTicksLeft--;
                    }
                    if (this.burstCooldownTicksLeft <= 0 && this.PawnOwner.IsHashIntervalTick(10))
                    {
                        if (this.TurretProp.autoAttack && !this.forcedTarget.IsValid)
                        {
                            if (!IsPlayerPawn || (IsPlayerPawn && Drafted))
                            {
                                this.currentTarget = (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(this, TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable, null, 0f, 9999f);
                            }
                        }
                        if (this.currentTarget.IsValid)
                        {
                            this.burstWarmupTicksLeft = this.TurretProp.warmingTime.SecondsToTicks();
                            return;
                        }
                        this.ResetCurrentTarget();
                    }
                }
            }
        }
        private bool IsPlayerPawn
        {
            get
            {
                if (this.parent is Pawn pawn && pawn.Faction != null && pawn.Faction.IsPlayer)
                {
                    return true;
                }
                return false;
            }
        }
        private bool Drafted => (this.parent is Pawn pawn) && pawn.Drafted;

        private bool CheckTarget()
        {
            if (!currentTarget.IsValid )
            {
                this.forcedTarget = LocalTargetInfo.Invalid;
                return false;
            }
            if (this.currentTarget.ThingDestroyed || (currentTarget.TryGetPawn(out var p) && p.DeadOrDowned))
            {
                this.currentTarget = LocalTargetInfo.Invalid;
                return false;
            }

            return true;
        }

        private void ResetCurrentTarget()
        {
            this.currentTarget = LocalTargetInfo.Invalid;
            this.burstWarmupTicksLeft = 0;
        }
        public List<PawnRenderNode> RenderNodes(Pawn pawn)
        {
            List<PawnRenderNode> result = new List<PawnRenderNode>();
            TurretProp.renderNodeProperties.ForEach(p =>
            {
                PawnRenderNode_SubTurretGun pawnRenderNode_TurretGun = (PawnRenderNode_SubTurretGun)Activator.CreateInstance(p.nodeClass, new object[]
                {
                        pawn,
                        p,
                        pawn.Drawer.renderer.renderTree
                });
                pawnRenderNode_TurretGun.subturret = this;
                result.Add(pawnRenderNode_TurretGun);
            });
            return result;
        }
        public void SwitchAutoFire()   
        {
            this.fireAtWill = !this.fireAtWill;
            
        }

        public void Targetting()
        {
            
            var tar = Find.Targeter;

            tar.BeginTargeting(this.CurrentEffectiveVerb.targetParams, (t) =>
            {
                [SyncMethod]
                void SyncTarget(LocalTargetInfo t, SubTurret self)
                {
                    self.forcedTarget = t; self.currentTarget = t; } 
                SyncTarget(t, this);
            } );
        }


        public void ClearTarget()
        {
            
            [SyncMethod] void SyncClearTarget( SubTurret self) {
                self.forcedTarget = LocalTargetInfo.Invalid; self.currentTarget = LocalTargetInfo.Invalid;
                }
            SyncClearTarget(this);
        }
        //

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.ID, "ID");

            Scribe_References.Look(ref this.parent, "parent");
            Scribe_Deep.Look(ref this.turret, "turret");

            Scribe_Values.Look<int>(ref this.burstCooldownTicksLeft, "burstCooldownTicksLeft", 0, false);
            Scribe_Values.Look<int>(ref this.burstWarmupTicksLeft, "burstWarmupTicksLeft", 0, false);
            Scribe_TargetInfo.Look(ref this.forcedTarget, "forcedTarget");
            Scribe_TargetInfo.Look(ref this.currentTarget, "currentTarget");

            Scribe_Values.Look<bool>(ref this.fireAtWill, "fireAtWill", true, false);

        }
        [NoTranslate]
        public string ID = "null";

        public Thing parent;
        public Thing turret;
        

        protected int burstCooldownTicksLeft;
        protected int burstWarmupTicksLeft;
        public LocalTargetInfo forcedTarget = LocalTargetInfo.Invalid;
        protected LocalTargetInfo currentTarget = LocalTargetInfo.Invalid;

        public bool fireAtWill = true;
        private LocalTargetInfo lastAttackedTarget = LocalTargetInfo.Invalid;
        private int lastAttackTargetTick;
        private SubTurretProperties turretProp;

        // 逻辑热路径缓存
        private CompEquippable cachedGunCompEq;
        private Verb cachedPrimaryVerb;
        private CompCanBeDormant cachedDormant;
        private bool dormantResolved;

        public float curRotation;
    }
}
