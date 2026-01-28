// 当白昼倾坠之时
using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Fortified.Structures
{
    // 所有的结构定义（新旧系统）最终都会转换成这个接口
    public interface IFFF_Structure
    {
        Sketch GetSketch();
        List<FFF_PawnGenRequest> GetPawns();
        List<IFFF_GenerationTask> GetGenerationTasks();
        
        // 新增支持旋转与偏移的方法
        List<FFF_PawnGenRequest> GetPawns(Rot4 rot, IntVec3 offset);
        List<IFFF_GenerationTask> GetTasks(Rot4 rot, IntVec3 offset);

        int FrontDist { get; } // 建筑正面（门）到边界的距离
        IntVec2 Size { get; }  // 建筑尺寸
    }

    public interface IFFF_PawnProvider
    {
        List<FFF_PawnGenRequest> GetPawns(Rot4 rot, IntVec3 offset);
    }

    public interface IFFF_TaskProvider
    {
        List<IFFF_GenerationTask> GetTasks(Rot4 rot, IntVec3 offset);
    }

    public struct FFF_PawnGenRequest
    {
        public PawnKindDef Kind;
        public FactionDef Faction;
        public IntVec3 Position;
        public bool DefendSpawnPoint;
    }
}
