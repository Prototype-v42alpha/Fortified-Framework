using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using System;

namespace Fortified
{
    // 补丁1：压制隐蔽行动相关的好感度变动
    public static class Patch_FFF_CovertOps_Goodwill
    {
        public static bool Prefix(
            Faction __instance, Faction other,
            GlobalTargetInfo? lookTarget,
            ref bool __result)
        {
            var intel = FFF_IntelProcessor.Instance;
            if (intel == null) return true;

            bool playerInvolved =
                __instance.IsPlayer || other.IsPlayer;
            if (!playerInvolved) return true;

            Faction nonPlayer = __instance.IsPlayer
                ? other : __instance;

            if (ShouldSuppress(intel, lookTarget, nonPlayer))
            {
                __result = true;
                return false;
            }
            return true;
        }

        private static bool ShouldSuppress(
            FFF_IntelProcessor intel,
            GlobalTargetInfo? target,
            Faction faction)
        {
            // 临时标记检查(空投/商队入口设置)
            if (FFF_IntelProcessor.suppressGoodwillChange)
            {
                FFF_IntelProcessor.suppressGoodwillChange = false;
                return true;
            }

            if (!target.HasValue) return false;
            var t = target.Value;
            if (!t.IsValid) return false;

            // Thing有Map：直接检查Map
            if (t.HasThing && t.Thing?.Map != null)
                return intel.IsCovertOp(t.Thing.Map, faction);

            // WorldObject是MapParent：检查MapParent
            if (t.HasWorldObject)
            {
                if (t.WorldObject is MapParent mp)
                    return intel.IsCovertOp(mp, faction);
                return false;
            }

            // 带地图的Cell目标
            if (t.Map != null)
                return intel.IsCovertOp(t.Map, faction);

            // Tile级匹配(大地图操作)
            if (t.Tile >= 0)
                return intel.IsCovertOpAtTile(t.Tile, faction);

            return false;
        }
    }

    // 补丁2：Thing级敌对覆写
    public static class Patch_FFF_CovertOps_ThingHostility
    {
        public static void Postfix(Thing a, Thing b, ref bool __result)
        {
            if (__result) return;
            if (a == null || b == null) return;
            if (a.Faction == null || b.Faction == null) return;

            bool aPlayer = a.Faction.IsPlayer;
            bool bPlayer = b.Faction.IsPlayer;
            if (!aPlayer && !bPlayer) return;

            var intel = FFF_IntelProcessor.Instance;
            if (intel == null) return;

            Faction targetFac = aPlayer ? b.Faction : a.Faction;
            Map map = a.Map ?? b.Map;
            if (map == null) return;

            if (intel.IsCovertOp(map, targetFac))
                __result = true;
        }
    }

    // 补丁4：Thing vs Faction 敌对覆写
    public static class Patch_FFF_CovertOps_ThingFactionHostility
    {
        public static void Postfix(Thing t, Faction fac,
            ref bool __result)
        {
            if (__result) return;
            if (t?.Faction == null || fac == null) return;

            bool playerInvolved =
                t.Faction.IsPlayer || fac.IsPlayer;
            if (!playerInvolved) return;

            var intel = FFF_IntelProcessor.Instance;
            if (intel == null) return;

            Faction targetFac = fac.IsPlayer
                ? t.Faction : fac;
            Map map = t.Map;
            if (map == null) return;

            if (intel.IsCovertOp(map, targetFac))
                __result = true;
        }
    }

    // 补丁5：强制Lord AI状态机识别敌对
    public static class Patch_FFF_CovertOps_LordTrigger
    {
        public static void Postfix(
            Verse.AI.Group.Lord lord, ref bool __result)
        {
            if (__result) return;
            if (lord?.faction == null) return;

            var intel = FFF_IntelProcessor.Instance;
            if (intel == null) return;

            Map map = lord.Map;
            if (map == null) return;

            if (intel.IsCovertOp(map, lord.faction))
                __result = true;
        }
    }

    // 补丁6：阻止好感度情景系统检测隐蔽行动地图
    public static class Patch_FFF_CovertOps_SettlementAttack
    {
        public static void Postfix(
            Faction faction, ref bool __result)
        {
            if (!__result) return;

            var intel = FFF_IntelProcessor.Instance;
            if (intel == null) return;

            var maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                var parent = maps[i].info.parent;
                if (parent == null) continue;
                if (parent.Faction != faction) continue;

                // 如果该地图不在隐蔽行动中则保持原判定
                if (!intel.IsCovertOp(maps[i], faction))
                    return;
            }

            // 所有匹配地图都是隐蔽行动 取消判定
            __result = false;
        }
    }

    // 补丁7：商队/空投进入时跳过好感度惩罚
    public static class Patch_FFF_CovertOps_CaravanEntry
    {
        public static bool Prefix(MapParent mapParent)
        {
            if (mapParent?.Faction == null) return true;

            var intel = FFF_IntelProcessor.Instance;
            if (intel == null) return true;

            if (intel.IsCovertOp(mapParent, mapParent.Faction))
                return false;

            return true;
        }
    }

    // 补丁8：空投到站点时设置临时压制标记
    public static class Patch_FFF_CovertOps_DropPodEntry
    {
        public static void Prefix(
            TransportersArrivalAction_VisitSite __instance)
        {
            var siteField = AccessTools.Field(
                typeof(TransportersArrivalAction_VisitSite),
                "site");
            if (siteField == null) return;
            var site = siteField.GetValue(__instance) as Site;
            if (site?.Faction == null) return;

            var intel = FFF_IntelProcessor.Instance;
            if (intel == null) return;

            if (intel.IsCovertOp(site, site.Faction))
                FFF_IntelProcessor.suppressGoodwillChange = true;
        }

        public static void Postfix()
        {
            FFF_IntelProcessor.suppressGoodwillChange = false;
        }
    }

    // 补丁9：名字颜色修正
    public static class Patch_FFF_CovertOps_NameColor
    {
        public static void Postfix(Pawn pawn, ref Color __result)
        {
            if (pawn?.Faction == null) return;
            if (pawn.Faction.IsPlayer) return;
            if (pawn.Map == null) return;

            var intel = FFF_IntelProcessor.Instance;
            if (intel == null) return;

            if (intel.IsCovertOp(pawn.Map, pawn.Faction))
                __result = PawnNameColorUtility.ColorBaseHostile;
        }
    }

    // 补丁10：Pawn级伪装 受伤好感度抑制
    public static class Patch_FFF_CovertOps_AgentDamage
    {
        public static void Prefix(DamageInfo dinfo)
        {
            if (dinfo.Instigator == null) return;
            var intel = FFF_IntelProcessor.Instance;
            if (intel == null) return;

            if (dinfo.Instigator is Pawn attacker
                && intel.IsCovertAgent(attacker))
            {
                FFF_IntelProcessor.suppressGoodwillChange = true;
            }
        }

        public static void Postfix()
        {
            FFF_IntelProcessor.suppressGoodwillChange = false;
        }
    }

    // 补丁11：Pawn级伪装 击杀好感度抑制
    public static class Patch_FFF_CovertOps_AgentKill
    {
        public static void Prefix(DamageInfo? dinfo)
        {
            if (!dinfo.HasValue) return;
            if (dinfo.Value.Instigator == null) return;
            var intel = FFF_IntelProcessor.Instance;
            if (intel == null) return;

            if (dinfo.Value.Instigator is Pawn attacker
                && intel.IsCovertAgent(attacker))
            {
                FFF_IntelProcessor.suppressGoodwillChange = true;
            }
        }

        public static void Postfix()
        {
            FFF_IntelProcessor.suppressGoodwillChange = false;
        }
    }
}
