using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Fortified
{
    [HarmonyPatch(typeof(Building_TurretGun), nameof(Building_TurretGun.GetGizmos))]
    public static class Patch_BuildingTurretGun_GetGizmos
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Building_TurretGun __instance)
        {
            foreach (var g in __result) yield return g;

            if (__instance?.gun == null) yield break;
            if (__instance.Faction != Faction.OfPlayer) yield break;

            CompAmmoSwitch comp = __instance.gun.TryGetComp<CompAmmoSwitch>();
            if (comp == null || !comp.HasAnyAmmoOption) yield break;

            var cmd = new Command_AmmoSwitch
            {
                comp = comp,
                messageTarget = __instance,
                defaultLabel = $"彈種: {comp.CurrentLabel}",
                defaultDesc = comp.GetGizmoDesc() + "\n\n左鍵：查看目前投射物資訊卡",
                icon = comp.CurrentIcon
            };
            cmd.action = cmd.OpenCurrentProjectileInfoCard;
            yield return cmd;
        }
    }
}