// 当白昼倾坠之时
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Fortified
{
    // 机兵休眠组件属性
    public class CompProperties_MechDeactivate : CompProperties
    {
        public CompProperties_MechDeactivate()
        {
            compClass = typeof(CompMechDeactivate);
        }
    }

    // 机兵休眠组件
    // 添加到机兵 Def 上，让机兵支持主动休眠
    public class CompMechDeactivate : ThingComp
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Pawn pawn = parent as Pawn;
            if (pawn == null || pawn.Dead || !pawn.Spawned)
                yield break;

            // 只有玩家派系的机兵才能休眠
            if (pawn.Faction != Faction.OfPlayer)
                yield break;

            // 检查是否是机械体
            if (!pawn.RaceProps.IsMechanoid)
                yield break;

            if (!pawn.def.HasModExtension<ModExtension_MechCapsule>() && !pawn.kindDef.HasModExtension<ModExtension_MechCapsule>())
                yield break;

            yield return new Command_Action
            {
                defaultLabel = "FFF.DeactivateMech".Translate(),
                defaultDesc = "FFF.DeactivateMechDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/PodEject"),
                action = delegate
                {
                    var capsule = MechCapsuleUtility.DeactivateMech(pawn);
                    if (capsule != null)
                    {
                        Messages.Message("FFF.MechDeactivated".Translate(pawn.LabelCap), capsule, MessageTypeDefOf.NeutralEvent);
                    }
                }
            };
        }
    }
}
