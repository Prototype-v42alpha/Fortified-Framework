using Verse;
using RimWorld;
using System.Linq;
using System.Collections.Generic;

namespace Fortified
{
    // 涂装组件注入
    [StaticConstructorOnStartup]
    public static class DefInjection_CompPaintable
    {
        static DefInjection_CompPaintable()
        {
            InjectToMechanoids();
            InjectOverlays();
        }

        private static void InjectToMechanoids()
        {
            var mechs = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(d => d.race != null && d.race.IsMechanoid && d.modContentPack != null &&
                           (d.modContentPack.PackageIdPlayerFacing.ToLower().Contains("aoba") ||
                            d.modContentPack.PackageIdPlayerFacing.ToLower().Contains("ascension")));

            foreach (var mech in mechs)
            {
                if (mech.comps == null)
                {
                    mech.comps = new List<CompProperties>();
                }

                if (!mech.HasComp(typeof(CompPaintable)))
                {
                    mech.comps.Add(new CompProperties_Paintable());
                }
            }
        }

        // 按 OverlayDef 反向注入到对应机械体
        private static void InjectOverlays()
        {
            foreach (var overlay in DefDatabase<FFF_OverlayDef>.AllDefsListForReading)
            {
                if (overlay.applicablePawnKinds == null || overlay.applicablePawnKinds.Count == 0)
                    continue;

                foreach (var kind in overlay.applicablePawnKinds)
                {
                    if (kind?.race == null) continue;
                    var props = kind.race.GetCompProperties<CompProperties_Paintable>();
                    if (props == null) continue;
                    if (!props.availableOverlays.Contains(overlay))
                        props.availableOverlays.Add(overlay);
                }
            }
        }
    }
}
