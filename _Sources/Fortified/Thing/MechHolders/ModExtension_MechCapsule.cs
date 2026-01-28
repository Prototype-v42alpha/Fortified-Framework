// 当白昼倾坠之时
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Fortified
{
    // 机兵休眠容器 ModExtension
    // 用于配置遗迹生成模式的参数
    public class ModExtension_MechCapsule : DefModExtension
    {
        // 生成概率
        public float spawnChance = 0.25f;

        // 武器保留概率
        public float weaponChance = 0.25f;

        // 内部机兵绘制偏移
        public Vector3 innerPawnDrawOffset = Vector3.zero;

        // 可能生成的机兵列表
        public List<PawnGenOption> possibleGeneratePawn;

        // 损伤数量范围
        public IntRange damageCount = new IntRange(0, 0);

        // 可能添加的损伤 Hediff
        public List<HediffDef> damageHediffs;
    }
}
