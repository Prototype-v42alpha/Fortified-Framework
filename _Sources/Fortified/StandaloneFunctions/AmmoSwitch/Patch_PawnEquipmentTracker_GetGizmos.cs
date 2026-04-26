using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Fortified
{
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.GetGizmos))]
    public static class Patch_PawnEquipmentTracker_GetGizmos
    {
        public static void Postfix(Pawn_EquipmentTracker __instance, ref IEnumerable<Gizmo> __result)
        {
            ThingWithComps primary = __instance?.Primary;
            CompAmmoSwitch comp = primary?.TryGetComp<CompAmmoSwitch>();
            if (comp == null || !comp.HasAnyAmmoOption) return;

            __result = Append(__result, comp, primary);
        }

        private static IEnumerable<Gizmo> Append(IEnumerable<Gizmo> original, CompAmmoSwitch comp, ThingWithComps weapon)
        {
            foreach (var g in original) yield return g;

            var cmd = new Command_AmmoSwitch
            {
                comp = comp,
                messageTarget = weapon,
                defaultLabel = $"彈種: {comp.CurrentLabel}",
                defaultDesc = comp.GetGizmoDesc() + "\n\n左鍵：查看目前投射物資訊卡",
                icon = comp.CurrentIcon
            };
            cmd.action = cmd.OpenCurrentProjectileInfoCard;
            yield return cmd;
        }
    }
}