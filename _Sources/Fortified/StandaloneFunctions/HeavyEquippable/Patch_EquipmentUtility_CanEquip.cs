using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace Fortified
{
    [HarmonyPatch(
        typeof(EquipmentUtility),
        nameof(EquipmentUtility.CanEquip),
        new Type[] { typeof(Thing), typeof(Pawn), typeof(string), typeof(bool) },
        new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal })]
    internal static class Patch_EquipmentUtility_CanEquip
    {
        static void Postfix(ref bool __result, Thing __0, Pawn __1, ref string __2, bool __3)
        {
            if (!__result) return;//因為其他原因的不可行(生物鎖之類的)
            if (!(__0 is Thing thing) || !(__1 is Pawn pawn)) return;//天知道拿了個不知道啥的鬼東西，反正也跳過。
            if (thing.TryGetComp<CompEquippable>() == null) return; //沒有comp

            if (pawn is IWeaponUsable)
            {
                //Log.Error("pawn is weapon usable");
                if (CheckUtility.IsMechUseable(pawn, thing as ThingWithComps))
                {
                    __result = true;
                }
                else
                {
                    __2 = " " + "FFF.WeaponNotSupported".Translate();
                    __result = false;
                }
            }
            else if (thing.def.HasModExtension<HeavyEquippableExtension>())
            {
                if (thing.def.GetModExtension<HeavyEquippableExtension>().CanEquippedBy(pawn))
                {
                    __result = true;
                }
                else
                {
                    __2 = " " + "FFF.BodysizeNotSupported".Translate(thing.def.GetModExtension<HeavyEquippableExtension>().EquippableDef.EquippableBaseBodySize.ToString("0.##"));
                    __result = false;
                }
            }
        }
    }
}
