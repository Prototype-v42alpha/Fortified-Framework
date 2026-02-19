using Verse;
using UnityEngine;
using System.Collections.Generic;

namespace Fortified
{
    // 染色组件属性
    public class CompProperties_Paintable : CompProperties
    {
        public CompProperties_Paintable()
        {
            compClass = typeof(CompPaintable);
        }

        // 默认涂装颜色
        public Color defaultColor = Color.white;

        // 是否使用派系颜色作为默认涂装
        public bool useFactionColor = false;

        // 是否允许在 UI 中切换迷彩
        public bool enableCamoSwitch = true;

        // 默认迷彩
        public FFF_CamoDef defaultCamo;

        // 运行时注入的可用叠加层
        [Unsaved(false)]
        public List<FFF_OverlayDef> availableOverlays = new List<FFF_OverlayDef>();
    }
}
