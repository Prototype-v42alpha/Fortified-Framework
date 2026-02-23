using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Fortified
{
    // 统一管理所有隐蔽行动补丁的动态挂载
    public static class FFF_CovertOpsPatchManager
    {
        private static Harmony _harmony;
        private static bool _enabled;
        private static int _patchedCount;

        public static void Init()
        {
            if (_harmony == null)
            {
                _harmony = new Harmony("Fortified.CovertOps");
            }
        }

        // 行动激活/停止时一次性切换全部补丁
        public static void SetEnabled(bool enable)
        {
            if (enable == _enabled) return;
            _enabled = enable;

            if (enable)
            {
                Log.Message("[Fortified-CovertOps] 隐蔽行动机制已上线，正在挂载专用环境补丁...");
                PatchAll();
                Log.Message($"[Fortified-CovertOps] 挂载完毕。成功激活 {_patchedCount} 个关键补丁。");
            }
            else
            {
                Log.Message("[Fortified-CovertOps] 隐蔽行动结束或过期，正在清理环境补丁...");
                UnpatchAll();
                Log.Message("[Fortified-CovertOps] 所有隐蔽行动相关的拦截补丁均已卸载。");
            }
        }

        private static void PatchAll()
        {
            _patchedCount = 0;

            Patch(
                typeof(Faction),
                nameof(Faction.TryAffectGoodwillWith),
                null,
                prefix: Get(typeof(Patch_FFF_CovertOps_Goodwill), "Prefix"));

            Patch(
                typeof(GenHostility),
                nameof(GenHostility.HostileTo),
                new[] { typeof(Thing), typeof(Thing) },
                postfix: Get(typeof(Patch_FFF_CovertOps_ThingHostility), "Postfix"));

            Patch(
                typeof(GenHostility),
                nameof(GenHostility.HostileTo),
                new[] { typeof(Thing), typeof(Faction) },
                postfix: Get(typeof(Patch_FFF_CovertOps_ThingFactionHostility), "Postfix"));

            Patch(
                typeof(Trigger_BecamePlayerEnemy),
                "ActivateOn",
                null,
                postfix: Get(typeof(Patch_FFF_CovertOps_LordTrigger), "Postfix"));

            Patch(
                typeof(SettlementUtility),
                nameof(SettlementUtility.IsPlayerAttackingAnySettlementOf),
                null,
                postfix: Get(typeof(Patch_FFF_CovertOps_SettlementAttack), "Postfix"));

            Patch(
                typeof(SettlementUtility),
                nameof(SettlementUtility.AffectRelationsOnAttacked),
                null,
                prefix: Get(typeof(Patch_FFF_CovertOps_CaravanEntry), "Prefix"));

            Patch(
                typeof(TransportersArrivalAction_VisitSite),
                nameof(TransportersArrivalAction_VisitSite.Arrived),
                null,
                prefix: Get(typeof(Patch_FFF_CovertOps_DropPodEntry), "Prefix"),
                postfix: Get(typeof(Patch_FFF_CovertOps_DropPodEntry), "Postfix"));

            Patch(
                typeof(PawnNameColorUtility),
                nameof(PawnNameColorUtility.PawnNameColorOf),
                null,
                postfix: Get(typeof(Patch_FFF_CovertOps_NameColor), "Postfix"));

            Patch(
                typeof(Faction),
                nameof(Faction.Notify_MemberTookDamage),
                null,
                prefix: Get(typeof(Patch_FFF_CovertOps_AgentDamage), "Prefix"),
                postfix: Get(typeof(Patch_FFF_CovertOps_AgentDamage), "Postfix"));

            Patch(
                typeof(Faction),
                nameof(Faction.Notify_MemberDied),
                null,
                prefix: Get(typeof(Patch_FFF_CovertOps_AgentKill), "Prefix"),
                postfix: Get(typeof(Patch_FFF_CovertOps_AgentKill), "Postfix"));
        }

        private static void UnpatchAll()
        {
            _harmony.UnpatchAll(_harmony.Id);
            _patchedCount = 0;
            Log.Message("[Fortified-CovertOps] -> UnpatchAll(\"Fortified.CovertOps\") executed.");
        }

        private static void Patch(
            Type type, string method, Type[] args,
            HarmonyMethod prefix = null,
            HarmonyMethod postfix = null)
        {
            var original = args != null
                ? AccessTools.Method(type, method, args)
                : AccessTools.Method(type, method);
            if (original == null)
            {
                Log.Error($"[Fortified-CovertOps] 找不到方法 {type.Name}.{method} 挂载失败！");
                return;
            }
            _harmony.Patch(original, prefix: prefix, postfix: postfix);
            _patchedCount++;
            string patchType = (prefix != null ? "PREFIX " : "") + (postfix != null ? "POSTFIX" : "");
            Log.Message($"[Fortified-CovertOps] -> Patched [{patchType.Trim()}] {type.Name}.{method}");
        }

        private static HarmonyMethod Get(Type type, string method)
            => new HarmonyMethod(AccessTools.Method(type, method));
    }
}
