using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace Fortified
{
    /// <summary>
    /// Harmony Patch for PawnWeaponGenerator.TryGenerateWeaponFor
    /// 
    /// Enables HumanlikeMech and other pawns without pawn.ideo (pawn.Ideo == null)
    /// to apply StylePack based on their faction's ideo.
    /// 
    /// This patch modifies the style assignment logic to:
    /// 1. Check if pawn has weaponStyleDef in kindDef (existing behavior)
    /// 2. Check if pawn has personal ideo (existing behavior)
    /// 3. Check if pawn's faction has ideo (NEW - handles HumanlikeMech case)
    /// </summary>
    [HarmonyPatch(typeof(PawnWeaponGenerator), nameof(PawnWeaponGenerator.TryGenerateWeaponFor))]
    internal static class Patch_PawnWeaponGenerator_TryGenerateWeaponFor
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn pawn)
        {
            try
            {
                WeaponStylePostProcessor.ApplyWeaponStyle(pawn);
            }
            catch (Exception e)
            {
                Log.Error($"[FFF] Patch_PawnWeaponGenerator_TryGenerateWeaponFor Error: {e}");
            }
        }
    }

    /// <summary>
    /// Helper class to handle weapon style application for pawns without personal ideo
    /// </summary>
    internal static class WeaponStylePostProcessor
    {
        /// <summary>
        /// Apply weapon style based on pawn's ideo or faction's ideo
        /// </summary>
        public static void ApplyWeaponStyle(Pawn pawn)
        {
            // Check if pawn has equipment
            if (pawn?.equipment?.Primary == null)
            {
                return;
            }

            ThingWithComps weapon = pawn.equipment.Primary;
            CompEquippable compEquippable = weapon.TryGetComp<CompEquippable>();

            if (compEquippable == null)
            {
                return;
            }

            // If weapon already has a style, don't override it
            if (weapon.StyleDef != null)
            {
                return;
            }

            // Try to get style from pawn's personal ideo first
            if (pawn.Ideo != null)
            {
                ThingStyleDef ideoStyle = pawn.Ideo.GetStyleFor(weapon.def);
                if (ideoStyle != null)
                {
                    weapon.StyleDef = ideoStyle;
                    return;
                }
            }

            // Try to get style from pawn's faction's ideo (handles HumanlikeMech case)
            if (pawn.Faction?.ideos?.PrimaryIdeo != null)
            {
                ThingStyleDef factionIdeoStyle = pawn.Faction.ideos.PrimaryIdeo.GetStyleFor(weapon.def);
                if (factionIdeoStyle != null)
                {
                    weapon.StyleDef = factionIdeoStyle;
                    return;
                }
            }
        }
    }
}
