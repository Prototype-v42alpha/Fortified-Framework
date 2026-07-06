using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Fortified
{
    public class CompBuildingExtraRenderer : ThingComp
    {
        public CompProperties_BuildingExtraRenderer Props => (CompProperties_BuildingExtraRenderer)props;
        public override void PostPrintOnto(SectionLayer layer)
        {
            if (layer == null) return;
            if (layerDebug == null) layerDebug = layer;
            base.PostPrintOnto(layer);
            foreach (var g in ExtraGraphic)
            {
                g?.Print(layer, parent, 0f);
            }
        }
        private SectionLayer layerDebug;
        public override void PostDraw()
        {
            base.PostDraw();
            // Parents drawn RealtimeOnly are never printed onto a SectionLayer,
            // so PostPrintOnto is never invoked for them. Draw manually here instead.
            if (parent == null || parent.def == null || parent.def.drawerType != DrawerType.RealtimeOnly) return;
            foreach (var g in ExtraGraphic)
            {
                g?.Draw(parent.DrawPos, parent.Rotation, parent);
            }
        }
        public override void Notify_DefsHotReloaded()
        {
            base.Notify_DefsHotReloaded();
            extraGraphic = null;
            if (parent != null && parent.def != null && parent.def.drawerType == DrawerType.RealtimeOnly) return;
            if (layerDebug != null) PostPrintOnto(layerDebug);
        }
        public List<Graphic> ExtraGraphic
        {
            get
            {
                if (extraGraphic == null)
                {
                    extraGraphic = new List<Graphic>();
                    if (Props?.extraGraphic != null)
                    {
                        foreach (var gd in Props.extraGraphic)
                        {
                            if (gd == null) continue;
                            extraGraphic.Add(gd.GraphicColoredFor(parent));
                        }
                    }
                }
                return extraGraphic;
            }
        }
        private List<Graphic> extraGraphic;
    }
    public class CompProperties_BuildingExtraRenderer : CompProperties
    {
        public List<GraphicData> extraGraphic;
        public CompProperties_BuildingExtraRenderer()
        {
            compClass = typeof(CompBuildingExtraRenderer);
        }
    }
}
