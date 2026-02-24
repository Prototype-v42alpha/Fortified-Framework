using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using System;

namespace Fortified
{
    [HarmonyPatch(typeof(Pawn_StoryTracker), "get_SkinColorBase")]
    public static class Patch_FixSkinColorCrash
    {
        [HarmonyPrefix]
        public static bool Prefix(Pawn_StoryTracker __instance, ref Color __result)
        {
            if (__instance == null) return true;

            var tr = Traverse.Create(__instance);
            Color? skinColorBase = tr.Field("skinColorBase").GetValue<Color?>();

            if (skinColorBase.HasValue) return true;

            Pawn pawn = tr.Field("pawn").GetValue<Pawn>();
            if (pawn?.genes != null && pawn.genes.GetMelaninGene() != null)
            {
                return true;
            }
            __result = Color.white;
            tr.Field("skinColorBase").SetValue((Color?)Color.white);
            return false;
        }
    }
}
