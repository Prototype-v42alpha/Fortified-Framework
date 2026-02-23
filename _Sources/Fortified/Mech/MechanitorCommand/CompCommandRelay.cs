// 当白昼倾坠之时
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Fortified
{
    //中控機體，能夠無視機械師控制範圍活動的同時自身也能作為控制範圍的延伸，但是在遠征時控制範圍會因距離而衰減
    public class CompCommandRelay : ThingComp
    {
        public static List<CompCommandRelay> allRelays = new List<CompCommandRelay>();

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            // 初始化为最大半径，避免 cacheDistance 首次计算为零
            CurrentRadius = Props.maxRelayRadius;
            cacheDistance = 0;
            if (!allRelays.Contains(this)) allRelays.Add(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            allRelays.Remove(this);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            allRelays.Remove(this);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            // 读档时重建
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (!allRelays.Contains(this)) allRelays.Add(this);
            }
        }

        public float SquaredDistance
        {
            get
            {
                if (cacheDistance == 0) cacheDistance = Mathf.Pow(CurrentRadius, 2);
                return cacheDistance;
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
            float prevRadius = CurrentRadius;
            if (overseer.MapHeld == Pawn.MapHeld) CurrentRadius = Props.maxRelayRadius;
            else if (!overseer.Spawned) CurrentRadius = Props.minRelayRadius;
            else
            {
                float dist = Find.WorldGrid.ApproxDistanceInTiles(Pawn.MapHeld.Tile, overseer.MapHeld.Tile);
                if (dist > Props.maxWorldMapRadius) CurrentRadius = Props.minRelayRadius;
                else CurrentRadius = Mathf.Lerp(Props.minRelayRadius, Props.maxRelayRadius, dist / Props.maxWorldMapRadius);
            }
            // 半径变化时清除缓存
            if (prevRadius != CurrentRadius) cacheDistance = 0;
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
