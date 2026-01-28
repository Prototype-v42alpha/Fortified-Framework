// 当白昼倾坠之时
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace Fortified.Structures
{
    public class FFF_StructureDef : Def, IFFF_Structure, IFFF_PawnProvider, IFFF_TaskProvider
    {
        public IntVec2 size = new IntVec2(5, 5);
        public List<FFF_Element> elements = new List<FFF_Element>();
        public List<string> tags = new List<string>();
        public List<string> zoneTags = new List<string>(); // 适用区域标签
        public float baseWeight = 1f; // 基础出现权重
        public int frontDist = 0; // 离正门距离

        public int FrontDist => frontDist;
        public IntVec2 Size => size;

        public IntVec2 GetSize(Rot4 rot)
        {
            return rot.IsHorizontal ? new IntVec2(size.z, size.x) : size;
        }

        [Unsaved]
        private Sketch cachedSketch;

        public Sketch GetSketch()
        {
            if (cachedSketch == null)
            {
                cachedSketch = new Sketch();
                if (elements != null)
                {
                    foreach (var element in elements) element.AddToSketch(cachedSketch);
                }
            }
            return cachedSketch;
        }

        public List<FFF_PawnGenRequest> GetPawns()
        {
            List<FFF_PawnGenRequest> pawns = new List<FFF_PawnGenRequest>();
            if (elements != null)
            {
                foreach (var element in elements)
                {
                    if (element is IFFF_PawnProvider provider)
                    {
                        pawns.AddRange(provider.GetPawns(Rot4.North, IntVec3.Zero));
                    }
                }
            }
            return pawns;
        }

        public List<IFFF_GenerationTask> GetGenerationTasks()
        {
            List<IFFF_GenerationTask> tasks = new List<IFFF_GenerationTask>();
            if (elements != null)
            {
                foreach (var element in elements)
                {
                    if (element is IFFF_TaskProvider provider)
                    {
                        tasks.AddRange(provider.GetTasks(Rot4.North, IntVec3.Zero));
                    }
                }
            }
            return tasks;
        }

        public List<FFF_PawnGenRequest> GetPawns(Rot4 rot, IntVec3 offset)
        {
            return GetPawns().ConvertAll(p => new FFF_PawnGenRequest 
            { 
                Kind = p.Kind, 
                Faction = p.Faction, 
                Position = p.Position.RotatedBy(rot) + offset,
                DefendSpawnPoint = p.DefendSpawnPoint
            });
        }

        public List<IFFF_GenerationTask> GetTasks(Rot4 rot, IntVec3 offset)
        {
            return GetGenerationTasks().ConvertAll(t => t.Transformed(rot, offset));
        }
    }
}
