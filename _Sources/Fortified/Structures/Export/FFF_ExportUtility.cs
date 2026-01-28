// 当白昼倾坠之时
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Fortified.Structures
{
    public static class FFF_ExportUtility
    {
        public class ExportOptions
        {
            public bool includeTerrain = true;
            public bool includeRoofs = true;
            public bool includeTerrainColors = true;
            public bool includeThings = true;
            public string defName = "NewStructure";
            public HashSet<Def> excludedDefs = new HashSet<Def>();
        }

        public static string ExportToXML(CellRect rect, Map map, ExportOptions options)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
            sb.AppendLine("<Defs>");
            sb.AppendLine("  <Fortified.Structures.FFF_StructureDef>");
            sb.AppendLine($"    <defName>{options.defName}</defName>");
            sb.AppendLine($"    <size>({rect.Width}, {rect.Height})</size>");
            sb.AppendLine("    <elements>");

            IntVec3 origin = rect.Min;

            // 1. 导出建筑/物品 (带有横向合并优化)
            if (options.includeThings)
            {
                ExportThingsOptimized(rect, map, origin, options, sb);
            }

            // 2. 导出地形 (带有横向合并优化)
            if (options.includeTerrain)
            {
                ExportTerrainOptimized(rect, map, origin, options, sb);
            }

            // 3. 导出颜色 (带有横向合并优化)
            if (options.includeTerrainColors)
            {
                ExportColorsOptimized(rect, map, origin, options, sb);
            }

            sb.AppendLine("    </elements>");
            sb.AppendLine("  </Fortified.Structures.FFF_StructureDef>");
            sb.AppendLine("</Defs>");

            return sb.ToString();
        }

        private static void ExportThingsOptimized(CellRect rect, Map map, IntVec3 origin, ExportOptions options, StringBuilder sb)
        {
            var rows = CollectThingsIntoRows(rect, map, origin, options);

            foreach (var row in rows)
            {
                var xList = row.Value.OrderBy(x => x).ToList();
                AppendThingRowXML(row.Key, xList, sb);
            }
        }

        private static Dictionary<(ThingDef, ThingDef, Rot4, int), List<int>> CollectThingsIntoRows(CellRect rect, Map map, IntVec3 origin, ExportOptions options)
        {
            var rows = new Dictionary<(ThingDef, ThingDef, Rot4, int), List<int>>();
            foreach (IntVec3 cell in rect)
            {
                foreach (Thing thing in cell.GetThingList(map))
                {
                    if (ShouldSkipThing(thing, options)) continue;
                    IntVec3 rel = cell - origin;
                    var key = (thing.def, thing.Stuff, thing.Rotation, rel.z);
                    if (!rows.ContainsKey(key)) rows[key] = new List<int>();
                    rows[key].Add(rel.x);
                }
            }
            return rows;
        }

        private static bool ShouldSkipThing(Thing thing, ExportOptions options)
        {
            if (options.excludedDefs.Contains(thing.def)) return true;
            if (thing.def.category != ThingCategory.Building && thing.def.category != ThingCategory.Item && thing.def.category != ThingCategory.Plant) return true;
            if (thing.def.building?.isNaturalRock == true) return true;
            return thing.def.defName.Contains("Blueprint") || thing.def.defName.Contains("Frame");
        }

        private static void AppendThingRowXML((ThingDef def, ThingDef stuff, Rot4 rot, int z) key, List<int> xList, StringBuilder sb)
        {
            int startX = xList[0], lastX = xList[0];
            for (int i = 1; i <= xList.Count; i++)
            {
                if (i < xList.Count && xList[i] == lastX + 1) lastX = xList[i];
                else
                {
                    AppendSingleElementXML(key, startX, lastX, sb);
                    if (i < xList.Count) { startX = xList[i]; lastX = xList[i]; }
                }
            }
        }

        private static void AppendSingleElementXML((ThingDef def, ThingDef stuff, Rot4 rot, int z) key, int startX, int lastX, StringBuilder sb)
        {
            bool isRect = startX != lastX;
            sb.AppendLine(isRect ? "      <li Class=\"Fortified.Structures.FFF_Element_ThingRect\">" : "      <li Class=\"Fortified.Structures.FFF_Element_Thing\">");
            sb.AppendLine($"        <def>{key.def.defName}</def>");
            sb.AppendLine($"        <pos>({startX}, 0, {key.z})</pos>");
            if (isRect) sb.AppendLine($"        <size>({lastX - startX + 1}, 1)</size>");
            if (key.rot != Rot4.North) sb.AppendLine($"        <rot>{key.rot.AsInt}</rot>");
            if (key.stuff != null) sb.AppendLine($"        <stuff>{key.stuff.defName}</stuff>");
            sb.AppendLine("      </li>");
        }

        private static void ExportTerrainOptimized(CellRect rect, Map map, IntVec3 origin, ExportOptions options, StringBuilder sb)
        {
            var rows = new Dictionary<(TerrainDef, int), List<int>>();
            foreach (IntVec3 cell in rect)
            {
                TerrainDef t = cell.GetTerrain(map);
                if (options.excludedDefs.Contains(t)) continue;
                
                IntVec3 rel = cell - origin;
                var key = (t, rel.z);
                if (!rows.ContainsKey(key)) rows[key] = new List<int>();
                rows[key].Add(rel.x);
            }

            foreach (var row in rows)
            {
                var xList = row.Value.OrderBy(x => x).ToList();
                int startX = xList[0];
                int lastX = xList[0];

                for (int i = 1; i <= xList.Count; i++)
                {
                    if (i < xList.Count && xList[i] == lastX + 1)
                    {
                        lastX = xList[i];
                    }
                    else
                    {
                        if (startX == lastX)
                        {
                            sb.AppendLine("      <li Class=\"Fortified.Structures.FFF_Element_Terrain\">");
                            sb.AppendLine($"        <def>{row.Key.Item1.defName}</def>");
                            sb.AppendLine($"        <pos>({startX}, 0, {row.Key.Item2})</pos>");
                            sb.AppendLine("      </li>");
                        }
                        else
                        {
                            sb.AppendLine("      <li Class=\"Fortified.Structures.FFF_Element_TerrainRect\">");
                            sb.AppendLine($"        <def>{row.Key.Item1.defName}</def>");
                            sb.AppendLine($"        <pos>({startX}, 0, {row.Key.Item2})</pos>");
                            sb.AppendLine($"        <size>({lastX - startX + 1}, 1)</size>");
                            sb.AppendLine("      </li>");
                        }
                        if (i < xList.Count) { startX = xList[i]; lastX = xList[i]; }
                    }
                }
            }
        }

        private static void ExportColorsOptimized(CellRect rect, Map map, IntVec3 origin, ExportOptions options, StringBuilder sb)
        {
            var rows = new Dictionary<(ColorDef, int), List<int>>();
            foreach (IntVec3 cell in rect)
            {
                ColorDef color = map.terrainGrid.ColorAt(cell);
                if (color == null) continue;
                IntVec3 rel = cell - origin;
                var key = (color, rel.z);
                if (!rows.ContainsKey(key)) rows[key] = new List<int>();
                rows[key].Add(rel.x);
            }

            foreach (var row in rows)
            {
                var xList = row.Value.OrderBy(x => x).ToList();
                int startX = xList[0];
                int lastX = xList[0];

                for (int i = 1; i <= xList.Count; i++)
                {
                    if (i < xList.Count && xList[i] == lastX + 1)
                    {
                        lastX = xList[i];
                    }
                    else
                    {
                        if (startX == lastX)
                        {
                            sb.AppendLine("      <li Class=\"Fortified.Structures.FFF_Element_TerrainColor\">");
                            sb.AppendLine($"        <color>{row.Key.Item1.defName}</color>");
                            sb.AppendLine($"        <pos>({startX}, 0, {row.Key.Item2})</pos>");
                        }
                        else
                        {
                            sb.AppendLine("      <li Class=\"Fortified.Structures.FFF_Element_TerrainColorRect\">");
                            sb.AppendLine($"        <color>{row.Key.Item1.defName}</color>");
                            sb.AppendLine($"        <pos>({startX}, 0, {row.Key.Item2})</pos>");
                            sb.AppendLine($"        <size>({lastX - startX + 1}, 1)</size>");
                        }
                        sb.AppendLine("      </li>");
                        if (i < xList.Count) { startX = xList[i]; lastX = xList[i]; }
                    }
                }
            }
        }
    }
}
