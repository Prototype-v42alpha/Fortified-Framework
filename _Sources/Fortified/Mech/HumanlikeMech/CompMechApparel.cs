using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using RimWorld;
using UnityEngine;

namespace Fortified
{
    [StaticConstructorOnStartup]
    public class CompMechApparel : ThingComp
    {
        public static readonly int REFRESH_INTERVAL = 6000;
        public Texture2D GizmoIcon
        {
            get
            {
                if (_gizmoIcon == null) _gizmoIcon = ContentFinder<Texture2D>.Get(this.Props.gizmoIconPath);
                return _gizmoIcon;
            }
        }

        public Pawn ParentPawn
        {
            get
            {
                _parentPawn ??= this.parent as Pawn;
                return _parentPawn;
            }
        }

        public CompProperties_MechApparel Props => (CompProperties_MechApparel)this.props;
        public Pawn_OutfitTracker OutfitSource
        {
            get
            {
                if (_outfitSource == null)
                {
                    if (ParentPawn.outfits == null) ParentPawn.outfits = new Pawn_OutfitTracker(ParentPawn);
                    _outfitSource = ParentPawn.outfits;
                }
                ;
                return _outfitSource;
            }
        }

        private static Texture2D _gizmoIcon;

        private Pawn_OutfitTracker _outfitSource;
        private Pawn _parentPawn;
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            //if (ParentPawn != null && ParentPawn.outfits == null) ParentPawn.outfits = new Pawn_OutfitTracker();


        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (ParentPawn == null) yield break;
            if (parent is Pawn p && !p.IsColonyMechPlayerControlled) yield break;
            yield return new Command_SelectApparelOutfit
            {
                defaultLabel = OutfitSource.CurrentApparelPolicy.label,
                outfitSource = OutfitSource,
                icon = ContentFinder<Texture2D>.Get("UI/Dress")

            };
        }

        public override void CompTick()
        {
            base.CompTick();
            // 确保 pawn 已完全初始化且已 Spawn（不在孵化器中）
            if (this.ParentPawn?.Spawned != true) return;
            if (this.ParentPawn.mindState == null) return;
            if (this.ParentPawn.Drafted) return;
            if (!this.parent.IsHashIntervalTick(REFRESH_INTERVAL)) return;
            if (this.ParentPawn.CurJobDef == JobDefOf.Wear) return;
            this.ParentPawn.TryOptimizeApparel();
        }
    }

    public class CompProperties_MechApparel : CompProperties
    {
        [NoTranslate]
        public string gizmoIconPath;

        public List<ApparelLayerDef> ApparelLayerBlackLists = new List<ApparelLayerDef>();

        public CompProperties_MechApparel()
        {
            this.compClass = typeof(CompMechApparel);
        }
    }
}