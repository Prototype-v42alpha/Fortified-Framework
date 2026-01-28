// 当白昼倾坠之时
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Fortified.Structures
{
    // 在世界生成时放置特定地点
    public class WorldGenStep_FFFSpawnWorldObjects : WorldGenStep
    {
        public override int SeedPart => 1616161616;

        public override void GenerateFresh(string seed, PlanetLayer layer)
        {
            var worldobjects = DefDatabase<WorldObjectDef>.AllDefsListForReading
                .Where(wo => wo.HasModExtension<SpawnAtWorldGen>());

            foreach (var worldObject in worldobjects)
            {
                SpawnAtWorldGen ext = worldObject.GetModExtension<SpawnAtWorldGen>();
                SpawnObjects(worldObject, ext);
            }
        }

        private void SpawnObjects(WorldObjectDef def, SpawnAtWorldGen ext)
        {
            for (int i = 0; i < ext.spawnCount; i++)
            {
                Site wo = (Site)WorldObjectMaker.MakeWorldObject(def);
                if (ext.spawnPartOfFaction != null)
                {
                    wo.SetFaction(Find.FactionManager.FirstFactionOfDef(ext.spawnPartOfFaction));
                }
                
                wo.Tile = RandomTileFor(ext);
                if (wo.Tile <= 0) continue;

                AddPartsToSite(wo, ext);
                Find.WorldObjects.Add(wo);
            }
        }

        private void AddPartsToSite(Site wo, SpawnAtWorldGen ext)
        {
            SitePartParams parms = new SitePartParams { points = 500f };
            foreach (var part in ext.parts)
            {
                SitePart sitePart = new SitePart(wo, part, parms);
                wo.AddPart(sitePart);
            }
        }

        private int RandomTileFor(SpawnAtWorldGen ext)
        {
            for (int i = 0; i < 500; i++)
            {
                int tileID = Rand.Range(0, Find.WorldGrid.TilesCount);
                if (IsTileValid(tileID, ext)) return tileID;
            }
            return -1;
        }

        private bool IsTileValid(int tileID, SpawnAtWorldGen ext)
        {
            Tile tile = Find.WorldGrid[tileID];
            if (!tile.PrimaryBiome.canBuildBase || tile.hilliness == Hilliness.Impassable) return false;
            if (Find.WorldObjects.AnyWorldObjectAt(tileID) || Find.WorldObjects.AnySettlementBaseAtOrAdjacent(tileID)) return false;
            if (ext.allowedBiomes.Any() && !ext.allowedBiomes.Contains(tile.PrimaryBiome)) return false;
            if (ext.disallowedBiomes.Contains(tile.PrimaryBiome)) return false;
            return true;
        }
    }
}
