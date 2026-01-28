// 当白昼倾坠之时
using RimWorld;
using UnityEngine;
using Verse;

namespace Fortified
{
    //中控機體，能夠無視機械師控制範圍活動的同時自身也能作為控制範圍的延伸，但是在遠征時控制範圍會因距離而衰減
    public class CompCommandRelay : ThingComp
    {
        public float SquaredDistance
        {
            get
            {
                return cacheDistance != 0 ? cacheDistance : GetCacheDistance();
            }
        }
        private float cacheDistance = 0;
        private float GetCacheDistance()
        {
            cacheDistance = Mathf.Pow(CurrentRadius, 2);
            return cacheDistance;
        }

        public float CurrentRadius;
        public CompProperties_CommandRelay Props => (CompProperties_CommandRelay)this.props;
        public override void PostDraw()
        {
            base.PostDraw();
            if (Pawn.Drafted)
            {
                if (Pawn.GetOverseer() == null) return;
                UpdateCurrentRadius();
                GenDraw.DrawRadiusRing(this.parent.Position, CurrentRadius, Color.cyan);
            }
        }

        private void UpdateCurrentRadius()
        {
            Pawn overseer = Pawn.GetOverseer();
            if (overseer.MapHeld == Pawn.MapHeld) CurrentRadius = Props.maxRelayRadius;
            else if (!overseer.Spawned) CurrentRadius = Props.minRelayRadius;
            else
            {
                float dist = Find.WorldGrid.ApproxDistanceInTiles(Pawn.MapHeld.Tile, overseer.MapHeld.Tile);
                if (dist > Props.maxWorldMapRadius) CurrentRadius = Props.minRelayRadius;
                else CurrentRadius = Mathf.Lerp(Props.minRelayRadius, Props.maxRelayRadius, dist / Props.maxWorldMapRadius);
            }
        }
        
        Pawn Pawn => this.parent as Pawn;
    }
    public class CompProperties_CommandRelay : CompProperties
    {
        public int maxWorldMapRadius;
        public float maxRelayRadius;
        public float minRelayRadius;
        public bool coverWholeMap;
        public CompProperties_CommandRelay() => compClass = typeof(CompCommandRelay);
    }
}
