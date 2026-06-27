// 当白昼倾坠之时
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Fortified
{
    // 瞄准时替换主武器图为手持装备图
    // 让挂CompAimingEquipmentGraphic的腰带类apparel在warmup时借主武器渲染位显示举枪图
    [HarmonyPatch(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawEquipmentAndApparelExtras))]
    public static class Patch_PawnRenderUtility_DrawAimingApparel
    {
        // 当前需替换图的Thing
        public static Thing swapTarget;
        // 替换用手持图
        public static Graphic swapGraphic;

        [HarmonyPrefix]
        public static void Prefix(Pawn pawn)
        {
            swapTarget = null;
            swapGraphic = null;
            try { ResolveSwap(pawn); }
            catch (System.Exception e) { Log.Error($"[FFF] Patch_DrawAimingApparel Prefix Error: {e}"); }
        }

        // 无主武器时借apparel自绘手持图
        [HarmonyPostfix]
        public static void Postfix(Pawn pawn, Vector3 drawPos, PawnRenderFlags flags)
        {
            try { DrawWeaponlessAiming(pawn, drawPos, flags); }
            catch (System.Exception e) { Log.Error($"[FFF] Patch_DrawAimingApparel Postfix Error: {e}"); }
        }

        [HarmonyFinalizer]
        public static void Finalizer()
        {
            swapTarget = null;
            swapGraphic = null;
        }

        // 有主武器则记录替换其图
        private static void ResolveSwap(Pawn pawn)
        {
            Thing primary = pawn.equipment?.Primary;
            if (primary == null) return;
            Apparel apparel = GetAimingApparel(pawn, out Graphic graphic);
            if (apparel == null) return;
            swapTarget = primary;
            swapGraphic = graphic;
        }

        // 无主武器时复刻原版瞄准绘制
        private static void DrawWeaponlessAiming(Pawn pawn, Vector3 drawPos, PawnRenderFlags flags)
        {
            if (pawn.equipment?.Primary != null) return;
            if (flags.HasFlag(PawnRenderFlags.NeverAimWeapon)) return;
            Job curJob = pawn.CurJob;
            if (curJob != null && curJob.def?.neverShowWeapon == true) return;
            if (!(pawn.stances?.curStance is Stance_Busy stance)) return;
            if (stance.neverAimWeapon || !stance.focusTarg.IsValid) return;

            Apparel apparel = GetAimingApparel(pawn, out Graphic graphic);
            if (apparel == null) return;

            float aimAngle = ResolveAimAngle(pawn, stance);
            float distFactor = pawn.ageTracker.CurLifeStage.equipmentDrawDistanceFactor;
            drawPos += new Vector3(0f, 0f, 0.4f + apparel.def.equippedDistanceOffset).RotatedBy(aimAngle) * distFactor;

            swapTarget = apparel;
            swapGraphic = graphic;
            PawnRenderUtility.DrawEquipmentAiming(apparel, drawPos, aimAngle);
            swapTarget = null;
            swapGraphic = null;
        }

        // 找正用本apparel verb瞄准的来源
        private static Apparel GetAimingApparel(Pawn pawn, out Graphic graphic)
        {
            graphic = null;
            if (!(pawn.stances?.curStance is Stance_Warmup warmup)) return null;
            if (!(warmup.verb?.EquipmentSource is Apparel apparel)) return null;
            CompAimingEquipmentGraphic comp = apparel.TryGetComp<CompAimingEquipmentGraphic>();
            if (comp?.AimingGraphic == null) return null;
            graphic = comp.AimingGraphic;
            return apparel;
        }

        // 复刻原版瞄准角计算
        private static float ResolveAimAngle(Pawn pawn, Stance_Busy stance)
        {
            float num = 0f;
            Vector3 target = stance.focusTarg.HasThing
                ? stance.focusTarg.Thing.DrawPos
                : stance.focusTarg.Cell.ToVector3Shifted();
            if ((target - pawn.DrawPos).MagnitudeHorizontalSquared() > 0.001f)
            {
                num = (target - pawn.DrawPos).AngleFlat();
            }
            Verb verb = pawn.CurrentEffectiveVerb;
            if (verb != null && verb.AimAngleOverride.HasValue)
            {
                num = verb.AimAngleOverride.Value;
            }
            return num;
        }
    }

    // 绘制瞄准期间把主武器图替换为手持图
    [HarmonyPatch(typeof(Thing), "get_Graphic")]
    public static class Patch_Thing_Graphic_AimingSwap
    {
        [HarmonyPostfix]
        public static void Postfix(Thing __instance, ref Graphic __result)
        {
            if (Patch_PawnRenderUtility_DrawAimingApparel.swapGraphic != null
                && __instance == Patch_PawnRenderUtility_DrawAimingApparel.swapTarget)
            {
                __result = Patch_PawnRenderUtility_DrawAimingApparel.swapGraphic;
            }
        }
    }

    // 瞄准期间隐藏apparel自身背包图
    [HarmonyPatch(typeof(PawnRenderNodeWorker), nameof(PawnRenderNodeWorker.CanDrawNow))]
    public static class Patch_PawnRenderNodeWorker_HideAimingApparel
    {
        [HarmonyPostfix]
        public static void Postfix(PawnRenderNode node, ref bool __result)
        {
            if (!__result) return;
            if (!(node is PawnRenderNode_Apparel apparelNode)) return;
            Apparel apparel = apparelNode.apparel;
            if (apparel == null) return;
            if (apparel.TryGetComp<CompAimingEquipmentGraphic>() == null) return;
            if (IsAimingWith(apparel)) __result = false;
        }

        // 判穿戴者是否正用本apparel瞄准
        private static bool IsAimingWith(Apparel apparel)
        {
            if (!(apparel.Wearer is Pawn pawn)) return false;
            if (!(pawn.stances?.curStance is Stance_Warmup warmup)) return false;
            return warmup.verb?.EquipmentSource == apparel;
        }
    }
}
