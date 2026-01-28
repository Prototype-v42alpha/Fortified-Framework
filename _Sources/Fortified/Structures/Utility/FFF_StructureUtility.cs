// 当白昼倾坠之时
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace Fortified.Structures
{
    public static class FFF_StructureUtility
    {
        public static void Generate(IFFF_Structure def, IntVec3 center, Map map, Faction faction = null, Rot4? rot = null)
        {
            if (def == null || map == null) return;

            Rot4 finalRot = rot ?? Rot4.North;
            Sketch sketch = def.GetSketch().DeepCopy();
            if (finalRot != Rot4.North) sketch.Rotate(finalRot);

            IntVec3 offset = center - sketch.OccupiedRect.CenterCell;
            CellRect occupiedRect = sketch.OccupiedRect.MovedBy(offset);

            ClearConflictArea(map, occupiedRect);
            SpawnTerrain(map, sketch, offset);
            SpawnThings(map, sketch, offset, faction);
            SpawnPawns(def, finalRot, offset, map, faction);
            HandleRoofs(def, sketch, offset, map, finalRot);
            HandleLegacyLogic(def, offset, map, finalRot);
            FinishGeneration(map, occupiedRect, def, finalRot, offset);
        }

        private static void ClearConflictArea(Map map, CellRect rect)
        {
            // 只清理植物、污垢和碎石，建筑由 GenSpawn 的 WipeMode 处理
            foreach (IntVec3 c in rect)
            {
                if (!c.InBounds(map)) continue;
                List<Thing> thingList = c.GetThingList(map).ToList();
                for (int i = 0; i < thingList.Count; i++)
                {
                    Thing t = thingList[i];
                    if (!t.Spawned || !t.def.destroyable) continue;
                    
                    // 1. 只删除植物、污垢、碎石
                    if (t.def.category == ThingCategory.Plant || 
                        t.def.category == ThingCategory.Filth ||
                        (t.def.thingCategories != null && t.def.thingCategories.Contains(ThingCategoryDefOf.Chunks)))
                    {
                        t.Destroy(DestroyMode.Vanish);
                    }
                    // 2. 其它非天然建筑/属性物件：移动到附近，而不是直接消失
                    else if (t.def.category == ThingCategory.Building || t.def.category == ThingCategory.Item)
                    {
                        // 如果是天然岩石（山体），在这种情况下我们需要“挖掘”它，否则建筑无法嵌入山内
                        if (t.def.building?.isNaturalRock ?? false)
                        {
                            t.Destroy(DestroyMode.Vanish);
                            continue;
                        }
                        
                        IntVec3 oldPos = t.Position;
                        t.DeSpawn();
                        // 尝试在附近找空位放下，防止稀有残骸被新建筑覆盖
                        GenPlace.TryPlaceThing(t, oldPos, map, ThingPlaceMode.Near);
                    }
                }
            }
        }

        private static void MoveNonDestroyables(Map map, CellRect rect, HashSet<Thing> nonDestroyables)
        {
            // 已废弃，保留以防需要
        }

        private static void DestroyDestroyables(Map map, CellRect rect)
        {
            // 已废弃，保留以防需要
        }

        private static bool IsRemovable(ThingDef def)
        {
            // 已废弃，保留以防需要
            return false;
        }

        private static void SpawnTerrain(Map map, Sketch sketch, IntVec3 offset)
        {
            foreach (var terrain in sketch.Terrain)
            {
                IntVec3 pos = terrain.pos + offset;
                if (pos.InBounds(map)) map.terrainGrid.SetTerrain(pos, terrain.def);
            }
        }

        private static void SpawnThings(Map map, Sketch sketch, IntVec3 offset, Faction faction)
        {
            var sortedThings = sketch.Things.OrderBy(t => t.SpawnOrder).ToList();
            
            if (Prefs.DevMode)
                Log.Message($"[FFF] SpawnThings: 尝试生成 {sortedThings.Count} 个物体");
            
            int spawnedCount = 0;
            foreach (var skThing in sortedThings)
            {
                IntVec3 pos = skThing.pos + offset;
                
                // 边界检查
                CellRect thingRect = GenAdj.OccupiedRect(pos, skThing.rot, skThing.def.size);
                if (!thingRect.InBounds(map))
                {
                    if (Prefs.DevMode) Log.Warning($"[FFF] 跳过越界物体: {skThing.def.defName} at {pos}");
                    continue;
                }

                Thing thing = skThing.Instantiate();
                if (faction != null && thing.def.CanHaveFaction) 
                    thing.SetFactionDirect(faction);
                
                GenSpawn.Spawn(thing, pos, map, skThing.rot, WipeMode.VanishOrMoveAside);
                InitializeBuildingState(thing);
                spawnedCount++;
            }
            
            if (Prefs.DevMode)
                Log.Message($"[FFF] SpawnThings: 成功生成 {spawnedCount}/{sortedThings.Count}");
        }

        private static void SpawnPawns(IFFF_Structure def, Rot4 rot, IntVec3 offset, Map map, Faction faction)
        {
            var pawns = def.GetPawns(rot, offset);
            if (pawns == null) return;

            foreach (var req in pawns)
            {
                if (!req.Position.InBounds(map)) continue;

                Faction pawnFaction = faction;
                if (req.Faction != null) pawnFaction = Find.FactionManager.FirstFactionOfDef(req.Faction);
                if (pawnFaction == null) pawnFaction = Faction.OfPlayer;

                Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(req.Kind, pawnFaction, PawnGenerationContext.NonPlayer, -1));
                GenSpawn.Spawn(pawn, req.Position, map, WipeMode.VanishOrMoveAside);

                if (req.DefendSpawnPoint && pawn.mindState != null)
                    pawn.mindState.duty = new PawnDuty(DutyDefOf.Defend, req.Position, -1f);
            }
        }

        private static void HandleRoofs(IFFF_Structure def, Sketch sketch, IntVec3 offset, Map map, Rot4 rot)
        {
            if (def is StructureLayoutDef layout && !layout.roofGrid.NullOrEmpty())
            {
                ApplyLegacyRoof(layout, offset, map, rot);
            }
            else
            {
                foreach (IntVec3 roofCell in sketch.GetSuggestedRoofCells())
                {
                    IntVec3 c = roofCell + offset;
                    if (c.InBounds(map) && !c.Roofed(map))
                        map.roofGrid.SetRoof(c, RoofDefOf.RoofConstructed);
                }
            }
        }

        private static void HandleLegacyLogic(IFFF_Structure def, IntVec3 offset, Map map, Rot4 rot)
        {
            if (def is StructureLayoutDef legacy)
                ApplyLegacyTerrainColor(legacy, offset, map, rot);
        }

        private static void FinishGeneration(Map map, CellRect rect, IFFF_Structure def, Rot4 rot, IntVec3 offset)
        {
            foreach (IntVec3 c in rect)
            {
                if (c.InBounds(map)) map.fogGrid.Unfog(c);
            }

            ReconnectPower(map);

            var tasks = def.GetTasks(rot, offset);
            if (tasks != null)
            {
                foreach (var task in tasks)
                {
                    try { task.Execute(map, IntVec3.Zero); }
                    catch (Exception e) { Log.Error($"[FFF] Error executing task {task.GetType().Name}: {e}"); }
                }
            }
        }

        private static void InitializeBuildingState(Thing thing)
        {
            // 自动补满燃料
            var refuelable = thing.TryGetComp<CompRefuelable>();
            if (refuelable != null)
            {
                refuelable.Refuel(refuelable.Props.fuelCapacity);
            }

            // 自动补满电量
            var battery = thing.TryGetComp<CompPowerBattery>();
            if (battery != null)
            {
                battery.SetStoredEnergyPct(1f);
            }

            // 应用派系样式与染色
            if (ModsConfig.IdeologyActive && thing.Faction != null && thing.Faction.ideos?.PrimaryIdeo is Ideo ideo)
            {
                thing.SetStyleDef(ideo.GetStyleFor(thing.def));
            }

            if (thing is Building b && b.def.building?.paintable == true)
            {
                // 这里暂时没有从 SymbolDef 传来的颜色信息，但可以在任务中覆盖
            }
        }

        private static void ApplyLegacyRoof(StructureLayoutDef def, IntVec3 offset, Map map, Rot4 rot)
        {
            if (def.roofGrid.NullOrEmpty()) return;

            IntVec2 srcSize = new IntVec2(def.roofGrid[0].Split(',').Length, def.roofGrid.Count);
            IntVec2 rotatedSize = rot.IsHorizontal ? new IntVec2(srcSize.z, srcSize.x) : srcSize;

            for (int z = 0; z < rotatedSize.z; z++)
            {
                for (int x = 0; x < rotatedSize.x; x++)
                {
                    IntVec3 srcPos = GetSourceCoords(x, z, rot, srcSize);
                    string[] cells = def.roofGrid[srcPos.z].Split(',');
                    if (srcPos.x >= cells.Length) continue;

                    string roof = cells[srcPos.x];
                    if (roof == "." || roof.NullOrEmpty()) continue;

                    IntVec3 targetPos = offset + new IntVec3(x, 0, z);
                    if (!targetPos.InBounds(map)) continue;

                    if (roof == "0") // 强制去除
                    {
                        if (def.forceGenerateRoof) map.roofGrid.SetRoof(targetPos, null);
                    }
                    else if (roof == "1") // 构造
                    {
                        if (def.forceGenerateRoof || !targetPos.Roofed(map))
                        {
                            map.roofGrid.SetRoof(targetPos, RoofDefOf.RoofConstructed);
                        }
                    }
                    else if (roof == "2") // 岩石（薄）
                    {
                        if (def.forceGenerateRoof || !targetPos.Roofed(map))
                        {
                            map.roofGrid.SetRoof(targetPos, RoofDefOf.RoofRockThin);
                        }
                    }
                    else if (roof == "3") // 岩石（厚）
                    {
                        map.roofGrid.SetRoof(targetPos, RoofDefOf.RoofRockThick);
                    }
                }
            }
        }

        private static void ApplyLegacyTerrainColor(StructureLayoutDef def, IntVec3 offset, Map map, Rot4 rot)
        {
            if (def.terrainColorGrid.NullOrEmpty()) return;

            IntVec2 srcSize = new IntVec2(def.terrainColorGrid[0].Split(',').Length, def.terrainColorGrid.Count);
            IntVec2 rotatedSize = rot.IsHorizontal ? new IntVec2(srcSize.z, srcSize.x) : srcSize;

            for (int z = 0; z < rotatedSize.z; z++)
            {
                for (int x = 0; x < rotatedSize.x; x++)
                {
                    IntVec3 srcPos = GetSourceCoords(x, z, rot, srcSize);
                    string[] cells = def.terrainColorGrid[srcPos.z].Split(',');
                    if (srcPos.x >= cells.Length || cells[srcPos.x] == ".") continue;

                    var cDef = DefDatabase<ColorDef>.GetNamedSilentFail(cells[srcPos.x]);
                    if (cDef != null)
                    {
                        IntVec3 targetPos = offset + new IntVec3(x, 0, z);
                        if (targetPos.InBounds(map))
                        {
                            map.terrainGrid.SetTerrainColor(targetPos, cDef);
                        }
                    }
                }
            }
        }

        private static IntVec3 GetSourceCoords(int x, int z, Rot4 rot, IntVec2 srcSize)
        {
            switch (rot.AsInt)
            {
                case 0: return new IntVec3(x, 0, z);
                case 1: return new IntVec3(srcSize.x - 1 - z, 0, x);
                case 2: return new IntVec3(srcSize.x - 1 - x, 0, srcSize.z - 1 - z);
                case 3: return new IntVec3(z, 0, srcSize.z - 1 - x);
                default: return IntVec3.Invalid;
            }
        }

        private static void ReconnectPower(Map map)
        {
            map.powerNetManager?.UpdatePowerNetsAndConnections_First();
        }
    }
}
