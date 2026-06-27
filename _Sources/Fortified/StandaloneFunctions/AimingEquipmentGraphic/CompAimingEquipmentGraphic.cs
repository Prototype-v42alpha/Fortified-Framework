using UnityEngine;
using Verse;

namespace Fortified
{
    // 瞄准时显示手持图
    public class CompAimingEquipmentGraphic : ThingComp
    {
        public CompProperties_AimingEquipmentGraphic Props => props as CompProperties_AimingEquipmentGraphic;

        public Graphic AimingGraphic => Props.graphicData?.Graphic;
    }

    public class CompProperties_AimingEquipmentGraphic : CompProperties
    {
        // 手持瞄准贴图
        public GraphicData graphicData;

        public CompProperties_AimingEquipmentGraphic()
        {
            compClass = typeof(CompAimingEquipmentGraphic);
        }

        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);
            graphicData?.ResolveReferencesSpecial();
        }
    }
}
