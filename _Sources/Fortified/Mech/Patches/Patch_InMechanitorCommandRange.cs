using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Fortified
{
    [HarmonyPatch(typeof(MechanitorUtility), "InMechanitorCommandRange")]
    internal class Patch_InMechanitorCommandRange
    {
        private static void Postfix(Pawn mech, LocalTargetInfo target, ref bool __result)
        {
            if (__result) return;
            if (mech.TryGetComp<CompDeadManSwitch>() is CompDeadManSwitch compDMS && compDMS.woken)
            {
                __result = true;
                return;
            }
            if (mech.TryGetComp<CompCommandRelay>(out var _c))
            {
                __result = true;
                return;
            }
            if (mech.TryGetComp<CompDrone>() != null)
            {
                __result = true;
                return;
            }

            Pawn overseer = MechanitorUtility.GetOverseer(mech);
            if (overseer == null) return;

            var relays = CompCommandRelay.allRelays;
            for (int i = 0; i < relays.Count; i++)
            {
                CompCommandRelay relay = relays[i];
                Pawn relayPawn = (Pawn)relay.parent;
                if (relayPawn.Spawned && relayPawn.MapHeld == mech.MapHeld && relayPawn.GetOverseer() == overseer)
                {
                    if (relay.Props.coverWholeMap || CheckUtility.InRange(relayPawn.Position, target, relay.SquaredDistance))
                    {
                        __result = true;
                        return;
                    }
                }
            }

            if (CheckUtility.HasSubRelayInMapAndInbound(mech, target))
            {
                __result = true;
                return;
            }
        }
    }
}
