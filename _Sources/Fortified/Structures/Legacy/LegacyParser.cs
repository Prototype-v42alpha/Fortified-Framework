// 当白昼倾坠之时
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Fortified.Structures
{
    public static class LegacyParser
    {
        public static void ParseToSketch(StructureLayoutDef def, Sketch sketch, List<FFF_PawnGenRequest> outPawns = null, List<IFFF_GenerationTask> outTasks = null)
        {
            if (def.layouts.NullOrEmpty()) return;

            // 1. 确定整体尺寸（取第一个有效层级的尺寸）
            var firstLayer = def.layouts.FirstOrDefault(l => !l.NullOrEmpty());
            if (firstLayer.NullOrEmpty()) return;
            
            int height = firstLayer.Count;
            int width = firstLayer[0].Split(',').Length;

            // 2. 解析所有层级（建筑层 + 电缆层 + 其他层）并叠加
            int parsedCount = 0;
            foreach (var layer in def.layouts)
            {
                if (layer.NullOrEmpty()) continue;
                
                for (int z = 0; z < Math.Min(height, layer.Count); z++)
                {
                    if (layer[z].NullOrEmpty()) continue;
                    string[] cells = layer[z].Split(',');
                    for (int x = 0; x < Math.Min(width, cells.Length); x++)
                    {
                        string cell = cells[x].Trim();
                        if (cell.NullOrEmpty() || cell == ".") continue;
                        
                        ParseSymbol(cell, sketch, new IntVec3(x, 0, z), outPawns, def.spawnConduits, outTasks);
                        parsedCount++;
                    }
                }
            }
            
            if (Prefs.DevMode && parsedCount > 0)
                Log.Message($"[FFF] 解析布局 {def.defName}: 符号={parsedCount}, Things={sketch.Things.ToList().Count}, Terrain={sketch.Terrain.ToList().Count}");

            // 3. 解析各种地形层级
            ParseAllTerrainGrids(def, sketch, outTasks, width, height);
            
            // 屋顶由 FFF_StructureUtility 统一处理
        }

        private static void ParseAllTerrainGrids(StructureLayoutDef def, Sketch sketch, List<IFFF_GenerationTask> outTasks, int width, int height)
        {
            // 基础层级
            ParseTerrainLayer(def.terrainGrid, width, height, (pos, tDef) => sketch.AddTerrain(tDef, pos));
            
            // 兼容层级 (通过任务执行，因为 Sketch 只存一层)
            if (outTasks != null)
            {
                ParseTerrainLayer(def.foundationGrid, width, height, (pos, tDef) => outTasks.Add(new Task_ApplyTerrainLayer { pos = pos, terrain = tDef, layerType = TerrainLayerType.Foundation }));
                ParseTerrainLayer(def.underGrid, width, height, (pos, tDef) => outTasks.Add(new Task_ApplyTerrainLayer { pos = pos, terrain = tDef, layerType = TerrainLayerType.Under }));
                ParseTerrainLayer(def.tempGrid, width, height, (pos, tDef) => outTasks.Add(new Task_ApplyTerrainLayer { pos = pos, terrain = tDef, layerType = TerrainLayerType.Temp }));
                
                // 地形染色
                ParseColorLayer(def.terrainColorGrid, width, height, (pos, cDef) => outTasks.Add(new Task_ApplyTerrainColor { pos = pos, color = cDef }));

                // 屋顶解析 (全量解析新增)
                ParseRoofLayer(def.roofGrid, width, height, (pos, rDef) => outTasks.Add(new Task_ApplyRoof { pos = pos, roofDef = rDef, force = def.forceGenerateRoof }));
            }
        }

        private static void ParseTerrainLayer(List<string> grid, int width, int height, Action<IntVec3, TerrainDef> action)
        {
            if (grid.NullOrEmpty()) return;
            for (int z = 0; z < Math.Min(height, grid.Count); z++)
            {
                string[] cells = grid[z].Split(',');
                for (int x = 0; x < Math.Min(width, cells.Length); x++)
                {
                    string cell = cells[x].Trim();
                    if (cell.NullOrEmpty() || cell == ".") continue;
                    var tDef = DefDatabase<TerrainDef>.GetNamedSilentFail(cell);
                    if (tDef != null) action(new IntVec3(x, 0, z), tDef);
                }
            }
        }

        private static void ParseColorLayer(List<string> grid, int width, int height, Action<IntVec3, ColorDef> action)
        {
            if (grid.NullOrEmpty()) return;
            for (int z = 0; z < Math.Min(height, grid.Count); z++)
            {
                string[] cells = grid[z].Split(',');
                for (int x = 0; x < Math.Min(width, cells.Length); x++)
                {
                    string cell = cells[x].Trim();
                    if (cell.NullOrEmpty() || cell == ".") continue;
                    var cDef = DefDatabase<ColorDef>.GetNamedSilentFail(cell);
                    if (cDef != null) action(new IntVec3(x, 0, z), cDef);
                }
            }
        }

        private static void ParseRoofLayer(List<string> grid, int width, int height, Action<IntVec3, RoofDef> action)
        {
            if (grid.NullOrEmpty()) return;
            for (int z = 0; z < Math.Min(height, grid.Count); z++)
            {
                string[] cells = grid[z].Split(',');
                for (int x = 0; x < Math.Min(width, cells.Length); x++)
                {
                    string cell = cells[x].Trim();
                    if (cell.NullOrEmpty() || cell == "." || cell == "0") continue;
                    
                    RoofDef roof = null;
                    if (cell == "1") roof = RoofDefOf.RoofConstructed;
                    else if (cell == "2") roof = RoofDefOf.RoofRockThin;
                    else if (cell == "3") roof = RoofDefOf.RoofRockThick;
                    
                    if (roof != null) action(new IntVec3(x, 0, z), roof);
                }
            }
        }

        private static void ParseSymbol(string raw, Sketch sketch, IntVec3 pos, List<FFF_PawnGenRequest> outPawns = null, bool spawnConduits = false, List<IFFF_GenerationTask> outTasks = null)
        {
            if (raw.NullOrEmpty()) return;

            // 1. 尝试提取方向后缀
            Rot4 rot = Rot4.North;
            string cleanName = raw;
            bool hasSuffix = false;

            if (raw.EndsWith("_North")) { rot = Rot4.North; cleanName = raw.Substring(0, raw.Length - 6); hasSuffix = true; }
            else if (raw.EndsWith("_East")) { rot = Rot4.East; cleanName = raw.Substring(0, raw.Length - 5); hasSuffix = true; }
            else if (raw.EndsWith("_South")) { rot = Rot4.South; cleanName = raw.Substring(0, raw.Length - 6); hasSuffix = true; }
            else if (raw.EndsWith("_West")) { rot = Rot4.West; cleanName = raw.Substring(0, raw.Length - 5); hasSuffix = true; }

            // 2. 依次尝试解析剥离后缀后的名称
            if (TryParseSymbolDef(cleanName, sketch, pos, outPawns, outTasks, spawnConduits, rot)) return;
            if (TryParseStuffThing(cleanName, sketch, pos, spawnConduits, outTasks, rot)) return;
            if (TryParseDirectThing(cleanName, sketch, pos, spawnConduits, outTasks, rot)) return;
            
            // 3. 兜底：如果带后缀没解析出来，尝试直接解析原始字符串（防止 defName 本身就带方向后缀）
            if (hasSuffix)
            {
                if (TryParseSymbolDef(raw, sketch, pos, outPawns, outTasks, spawnConduits, Rot4.North)) return;
                if (TryParseStuffThing(raw, sketch, pos, spawnConduits, outTasks, Rot4.North)) return;
                TryParseDirectThing(raw, sketch, pos, spawnConduits, outTasks, Rot4.North);
            }
        }

        private static bool TryParseSymbolDef(string raw, Sketch sketch, IntVec3 pos, List<FFF_PawnGenRequest> outPawns, List<IFFF_GenerationTask> outTasks, bool spawnConduits, Rot4 rot)
        {
            var symbol = DefDatabase<SymbolDef>.GetNamedSilentFail(raw);
            if (symbol == null) return false;

            // 如果显式传入了旋转（来自后缀），则覆盖 SymbolDef 默认值
            if (rot != Rot4.North || raw.EndsWith("_North"))
            {
                var tempSymbol = symbol.ShallowCopy();
                tempSymbol.rotation = rot;
                tempSymbol.AddToSketch(sketch, pos, outPawns, outTasks);
            }
            else
            {
                symbol.AddToSketch(sketch, pos, outPawns, outTasks);
            }

            TryAddConduit(pos, symbol.thing, spawnConduits, outTasks);
            return true;
        }



        private static bool TryParseStuffThing(string raw, Sketch sketch, IntVec3 pos, bool spawnConduits, List<IFFF_GenerationTask> outTasks, Rot4 rot)
        {
            if (!raw.Contains("_")) return false;

            int sepIndex = raw.LastIndexOf('_');
            string tName = raw.Substring(0, sepIndex);
            string sName = raw.Substring(sepIndex + 1);

            var tDef = DefDatabase<ThingDef>.GetNamedSilentFail(tName);
            var sDef = DefDatabase<ThingDef>.GetNamedSilentFail(sName);

            if (tDef != null && sDef != null)
            {
                sketch.AddThing(tDef, pos, rot, sDef);
                TryAddConduit(pos, tDef, spawnConduits, outTasks);
                return true;
            }
            return false;
        }

        private static bool TryParseDirectThing(string raw, Sketch sketch, IntVec3 pos, bool spawnConduits, List<IFFF_GenerationTask> outTasks, Rot4 rot)
        {
            var directDef = DefDatabase<ThingDef>.GetNamedSilentFail(raw);
            if (directDef != null)
            {
                sketch.AddThing(directDef, pos, rot);
                TryAddConduit(pos, directDef, spawnConduits, outTasks);
                return true;
            }
            return false;
        }

        private static void TryAddConduit(IntVec3 pos, ThingDef tDef, bool spawnConduits, List<IFFF_GenerationTask> outTasks)
        {
            if (!spawnConduits || tDef == null || outTasks == null) return;
            
            // 如果是墙、门等不可通行建筑，添加电缆生成任务
            if (tDef.passability == Traversability.Impassable || tDef.IsDoor)
            {
                outTasks.Add(new Task_SpawnConduit { pos = pos });
            }
        }
    }
}
