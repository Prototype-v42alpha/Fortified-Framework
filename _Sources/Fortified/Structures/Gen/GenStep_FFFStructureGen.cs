// 当白昼倾坠之时
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using Verse;
using RimWorld.Planet;

namespace Fortified.Structures
{
    public class GenStep_FFFStructureGen : GenStep
    {
        public GenStep_FFFStructureGen() { }

        public override int SeedPart => 394857125;

        // XML 字段兼容 KCSG
        public List<StructureLayoutDef> structureLayoutDefs;
        public List<FFF_CompoundStructureDef> compoundStructureDefs; // 新增
        public string useTag; // 如果设置，则从具有此标签的定义中随机选取
        public bool fullClear = true;
        public bool preventBridgeable = false;
        public List<string> filthTypes; // 污垢散射
        public List<string> symbolResolvers; // BaseGen 符号解析器
        public bool scaleWithQuest; // 维持 XML 兼容性
        public bool randomRotation = false;
        public IntRange randomOffset = new IntRange(0, 5);

        public override void Generate(Map map, GenStepParams parms)
        {
            IntVec3 center = CalculateCenter(map);
            Faction faction = map.ParentFaction ?? parms.sitePart?.site?.Faction;

            // 优先尝试复合结构
            FFF_CompoundStructureDef compoundDef = ResolveCompoundDef();
            if (compoundDef != null)
            {
                CompoundStructureUtility.Generate(compoundDef, center, map, faction);
                return;
            }

            // 回退到单一结构
            IFFF_Structure def = ResolveStructureDef();
            if (def == null) return;

            Rot4 rot = randomRotation ? Rot4.Random : Rot4.North;
            FFF_StructureUtility.Generate(def, center, map, faction, rot);
            HandlePostScatter(def, center, map, rot);
        }

        private FFF_CompoundStructureDef ResolveCompoundDef()
        {
            if (!compoundStructureDefs.NullOrEmpty())
                return compoundStructureDefs.RandomElementWithFallback();

            if (!useTag.NullOrEmpty())
            {
                var matches = DefDatabase<FFF_CompoundStructureDef>.AllDefs
                    .Where(x => x.label != null && x.label.Contains(useTag));
                return matches.RandomElementWithFallback();
            }
            return null;
        }

        private void HandlePostScatter(IFFF_Structure def, IntVec3 center, Map map, Rot4 rot)
        {
            var sketch = def.GetSketch();
            if (rot != Rot4.North) sketch.Rotate(rot);
            
            CellRect rect = sketch.OccupiedRect.MovedBy(center - sketch.OccupiedRect.CenterCell);

            // 1. 处理污垢散射
            if (!filthTypes.NullOrEmpty())
            {
                List<ThingDef> filthDefs = filthTypes.Select(x => DefDatabase<ThingDef>.GetNamedSilentFail(x)).Where(x => x != null).ToList();
                if (filthDefs.Count > 0)
                {
                    new Task_ScatterFilth { rect = rect, filthTypes = filthDefs, chance = 0.25f }.Execute(map, IntVec3.Zero);
                }
            }

            // 2. 处理 SymbolResolvers (KCSG 兼容)
            if (!symbolResolvers.NullOrEmpty())
            {
                var rp = new RimWorld.BaseGen.ResolveParams { rect = rect };
                foreach (string resolver in symbolResolvers)
                {
                    RimWorld.BaseGen.BaseGen.symbolStack.Push(resolver, rp, null);
                }
                RimWorld.BaseGen.BaseGen.Generate();
            }
        }

        private IFFF_Structure ResolveStructureDef()
        {
            if (!useTag.NullOrEmpty())
            {
                var matches = DefDatabase<StructureLayoutDef>.AllDefs.Where(x => x.tags != null && x.tags.Contains(useTag)).Cast<IFFF_Structure>()
                    .Concat(DefDatabase<FFF_StructureDef>.AllDefs.Where(x => x.tags != null && x.tags.Contains(useTag)).Cast<IFFF_Structure>())
                    .Concat(DefDatabase<FFF_SettlementDef>.AllDefs.Where(x => x.tags != null && x.tags.Contains(useTag)).Cast<IFFF_Structure>());
                return matches.RandomElementWithFallback();
            }

            if (!structureLayoutDefs.NullOrEmpty())
                return structureLayoutDefs.RandomElementWithFallback();

            return null;
        }

        private IntVec3 CalculateCenter(Map map)
        {
            IntVec3 center = map.Center;
            if (randomOffset.max > 0)
                center += new IntVec3(randomOffset.RandomInRange, 0, randomOffset.RandomInRange).RotatedBy(Rot4.Random);
            return center;
        }

        private Faction ResolveFaction(IFFF_Structure def, Map map, GenStepParams parms)
        {
            Faction faction = parms.sitePart?.site?.Faction;
            var pawns = def.GetPawns(Rot4.North, IntVec3.Zero);
            if (faction == null && pawns?.Count > 0)
            {
                var firstReq = pawns[0];
                if (firstReq.Faction != null) faction = Find.FactionManager.FirstFactionOfDef(firstReq.Faction);
            }
            return faction ?? map.ParentFaction;
        }
    }
}
