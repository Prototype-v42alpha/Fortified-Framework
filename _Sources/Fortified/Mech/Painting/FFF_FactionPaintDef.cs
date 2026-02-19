using System.Collections.Generic;
using Verse;
using RimWorld;
using UnityEngine;

namespace Fortified
{
    // 涂装选项
    public class FFF_PaintOption
    {
        public float weight = 1f;           // 权重
        public ColorInt color1 = new ColorInt(255, 255, 255);
        public ColorInt color2 = new ColorInt(255, 255, 255);
        public ColorInt color3 = new ColorInt(255, 255, 255);
        public FFF_CamoDef camoDef;
        public float brightness = 0f;
        public FFF_OverlayDef overlayDef;
    }

    // 派系预设涂装定义
    public class FFF_FactionPaintDef : Def
    {
        public FactionDef faction;            // 目标派系
        public PawnKindDef pawnKind;          // 兵种过滤

        // 权重化的候选项列表
        public List<FFF_PaintOption> options = new List<FFF_PaintOption>();

        // 默认/单一配置（向下兼容或简单使用）
        public ColorInt color1 = new ColorInt(255, 255, 255);
        public ColorInt color2 = new ColorInt(255, 255, 255);
        public ColorInt color3 = new ColorInt(255, 255, 255);
        public FFF_CamoDef camoDef;
        public float brightness = 0f;
        public FFF_OverlayDef overlayDef;
    }
}
