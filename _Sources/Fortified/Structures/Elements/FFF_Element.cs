// 当白昼倾坠之时
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fortified.Structures
{
    public abstract class FFF_Element
    {
        public FFF_Element() { }
        public abstract void AddToSketch(Sketch sketch);
    }

    public enum FFF_FacingMode
    {
        None,
        Outward, // 朝向远离中心的方向
        Inward,  // 朝向中心方向
        Clockwise,
        CounterClockwise
    }

    public class FFF_Element_Thing : FFF_Element
    {
        public FFF_Element_Thing() { }
        public ThingDef def;
        public IntVec3 pos;
        public Rot4 rot = Rot4.North;
        public ThingDef stuff;

        public override void AddToSketch(Sketch sketch)
        {
            if (def != null) sketch.AddThing(def, pos, rot, stuff);
        }
    }

    public class FFF_Element_Terrain : FFF_Element
    {
        public FFF_Element_Terrain() { }
        public TerrainDef def;
        public IntVec3 pos;

        public override void AddToSketch(Sketch sketch)
        {
            if (def != null) sketch.AddTerrain(def, pos);
        }
    }

    // 矩形块优化版本
    public class FFF_Element_ThingRect : FFF_Element
    {
        public FFF_Element_ThingRect() { }
        public ThingDef def;
        public IntVec3 pos;
        public IntVec2 size;
        public Rot4 rot = Rot4.North;
        public ThingDef stuff;

        public override void AddToSketch(Sketch sketch)
        {
            if (def == null) return;
            CellRect rect = new CellRect(pos.x, pos.z, size.x, size.z);
            foreach (IntVec3 p in rect) sketch.AddThing(def, p, rot, stuff);
        }
    }

    public class FFF_Element_TerrainRect : FFF_Element
    {
        public FFF_Element_TerrainRect() { }
        public TerrainDef def;
        public IntVec3 pos;
        public IntVec2 size;

        public override void AddToSketch(Sketch sketch)
        {
            if (def == null) return;
            CellRect rect = new CellRect(pos.x, pos.z, size.x, size.z);
            foreach (IntVec3 p in rect) sketch.AddTerrain(def, p);
        }
    }

    public class FFF_Element_TerrainColorRect : FFF_Element
    {
        public FFF_Element_TerrainColorRect() { }
        public ColorDef color;
        public CellRect rect;

        public override void AddToSketch(Sketch sketch) { }
    }

    public class FFF_Element_Pawn : FFF_Element, IFFF_PawnProvider
    {
        public FFF_Element_Pawn() { }
        public PawnKindDef kind;
        public FactionDef faction;
        public IntVec3 pos;
        public bool defendSpawnPoint;

        public override void AddToSketch(Sketch sketch) { }

        public List<FFF_PawnGenRequest> GetPawns(Rot4 rot, IntVec3 offset)
        {
            return new List<FFF_PawnGenRequest>
            {
                new FFF_PawnGenRequest
                {
                    Kind = kind,
                    Faction = faction,
                    Position = pos.RotatedBy(rot) + offset,
                    DefendSpawnPoint = defendSpawnPoint
                }
            };
        }
    }

    // 次要结构元素（支持嵌套与径向朝向）
    public class FFF_Element_SubStructure : FFF_Element, IFFF_PawnProvider, IFFF_TaskProvider
    {
        public FFF_Element_SubStructure() { }
        public string subStructureDefName;
        public IntVec3 pos;
        public FFF_FacingMode facingMode = FFF_FacingMode.None;

        [Unsaved]
        protected IFFF_Structure cachedSub;

        protected virtual IFFF_Structure GetSub()
        {
            if (cachedSub == null) cachedSub = (IFFF_Structure)GenDefDatabase.GetDef(typeof(FFF_StructureDef), subStructureDefName, false) ?? (IFFF_Structure)GenDefDatabase.GetDef(typeof(StructureLayoutDef), subStructureDefName, false);
            return cachedSub;
        }

        public override void AddToSketch(Sketch sketch)
        {
            var sub = GetSub();
            if (sub == null) return;

            Rot4 rot = GetFacingRot();
            Sketch subSketch = sub.GetSketch().DeepCopy();
            subSketch.Rotate(rot);
            sketch.MergeAt(subSketch, pos);
        }

        public List<FFF_PawnGenRequest> GetPawns(Rot4 rot, IntVec3 offset)
        {
            if (!(GetSub() is IFFF_PawnProvider provider)) return new List<FFF_PawnGenRequest>();

            Rot4 facingRot = GetFacingRot();
            Rot4 finalRot = new Rot4((facingRot.AsInt + rot.AsInt) % 4);
            IntVec3 finalOffset = pos.RotatedBy(rot) + offset;

            return provider.GetPawns(finalRot, finalOffset);
        }

        public List<IFFF_GenerationTask> GetTasks(Rot4 rot, IntVec3 offset)
        {
            if (!(GetSub() is IFFF_TaskProvider provider)) return new List<IFFF_GenerationTask>();

            Rot4 facingRot = GetFacingRot();
            Rot4 finalRot = new Rot4((facingRot.AsInt + rot.AsInt) % 4);
            IntVec3 finalOffset = pos.RotatedBy(rot) + offset;

            return provider.GetTasks(finalRot, finalOffset);
        }

        protected Rot4 GetFacingRot()
        {
            if (facingMode == FFF_FacingMode.None) return Rot4.North;
            
            // 简单的径向判定
            if (Mathf.Abs(pos.x) > Mathf.Abs(pos.z))
            {
                if (pos.x > 0) return facingMode == FFF_FacingMode.Outward ? Rot4.East : Rot4.West;
                return facingMode == FFF_FacingMode.Outward ? Rot4.West : Rot4.East;
            }
            else
            {
                if (pos.z > 0) return facingMode == FFF_FacingMode.Outward ? Rot4.North : Rot4.South;
                return facingMode == FFF_FacingMode.Outward ? Rot4.South : Rot4.North;
            }
        }
    }

    // 随机次要结构（从池中选一，支持标签）
    public class FFF_Element_RandomSubStructure : FFF_Element_SubStructure
    {
        public FFF_Element_RandomSubStructure() { }
        public List<string> pool; // DefName 列表
        public string tag;        // 通过标签检索
        public float chance = 1f;

        protected override IFFF_Structure GetSub()
        {
            if (cachedSub != null) return cachedSub;
            if (Rand.Value > chance) return null;

            if (!pool.NullOrEmpty())
            {
                string choice = pool.RandomElement();
                cachedSub = (IFFF_Structure)GenDefDatabase.GetDef(typeof(FFF_StructureDef), choice, false) ?? (IFFF_Structure)GenDefDatabase.GetDef(typeof(StructureLayoutDef), choice, false);
            }
            else if (!tag.NullOrEmpty())
            {
                // 从所有 Structure 库中按标签筛选
                var matches = DefDatabase<StructureLayoutDef>.AllDefs.Where(x => x.tags != null && x.tags.Contains(tag)).Cast<IFFF_Structure>()
                    .Concat(DefDatabase<FFF_StructureDef>.AllDefs.Where(x => x.tags != null && x.tags.Contains(tag)).Cast<IFFF_Structure>());
                cachedSub = matches.RandomElementWithFallback();
            }
            return cachedSub;
        }

        public override void AddToSketch(Sketch sketch)
        {
            if (GetSub() != null) base.AddToSketch(sketch);
        }
    }

    // 集群生成器（用于生成村庄/群落）
    public class FFF_Element_Scatter : FFF_Element, IFFF_PawnProvider, IFFF_TaskProvider
    {
        public FFF_Element_Scatter() { }
        public string structureDefName;
        public int count = 1;
        public float radius = 5f;
        public bool randomRotation = true;

        [Unsaved]
        private List<(IFFF_Structure sub, IntVec3 pos, Rot4 rot)> instances;

        private void EnsureInstances()
        {
            if (instances != null) return;
            instances = new List<(IFFF_Structure, IntVec3, Rot4)>();

            IFFF_Structure sub = (IFFF_Structure)GenDefDatabase.GetDef(typeof(FFF_StructureDef), structureDefName, false) ?? (IFFF_Structure)GenDefDatabase.GetDef(typeof(StructureLayoutDef), structureDefName, false);
            if (sub == null) return;

            for (int i = 0; i < count; i++)
            {
                Vector2 randPos = Rand.InsideUnitCircle * radius;
                IntVec3 finalPos = new IntVec3((int)randPos.x, 0, (int)randPos.y);
                Rot4 finalRot = randomRotation ? Rot4.Random : Rot4.North;
                instances.Add((sub, finalPos, finalRot));
            }
        }

        public override void AddToSketch(Sketch sketch)
        {
            EnsureInstances();
            foreach (var inst in instances)
            {
                Sketch subSketch = inst.sub.GetSketch().DeepCopy();
                subSketch.Rotate(inst.rot);
                sketch.MergeAt(subSketch, inst.pos);
            }
        }

        public List<FFF_PawnGenRequest> GetPawns(Rot4 rot, IntVec3 offset)
        {
            EnsureInstances();
            List<FFF_PawnGenRequest> pawns = new List<FFF_PawnGenRequest>();
            foreach (var inst in instances)
            {
                if (inst.sub is IFFF_PawnProvider provider)
                {
                    Rot4 combinedRot = new Rot4((inst.rot.AsInt + rot.AsInt) % 4);
                    IntVec3 combinedOffset = inst.pos.RotatedBy(rot) + offset;
                    pawns.AddRange(provider.GetPawns(combinedRot, combinedOffset));
                }
            }
            return pawns;
        }

        public List<IFFF_GenerationTask> GetTasks(Rot4 rot, IntVec3 offset)
        {
            EnsureInstances();
            List<IFFF_GenerationTask> tasks = new List<IFFF_GenerationTask>();
            foreach (var inst in instances)
            {
                if (inst.sub is IFFF_TaskProvider provider)
                {
                    Rot4 combinedRot = new Rot4((inst.rot.AsInt + rot.AsInt) % 4);
                    IntVec3 combinedOffset = inst.pos.RotatedBy(rot) + offset;
                    tasks.AddRange(provider.GetTasks(combinedRot, combinedOffset));
                }
            }
            return tasks;
        }
    }
}
