using System;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace Fortified
{
    // 动态挂载感应门寻路补丁 仅地图存在感应门时生效
    public static class FFF_SensorDoorPatchManager
    {
        private static Harmony _harmony;
        private static bool _enabled;
        private static int _activeCount;

        // 全地图感应门计数
        private static int _doorCount;

        private static void Init()
        {
            if (_harmony == null) _harmony = new Harmony("Fortified.SensorDoor");
        }

        // 组件spawn时登记
        public static void NotifySpawned()
        {
            _doorCount++;
            if (_doorCount > 0) SetEnabled(true);
        }

        // 组件despawn时注销
        public static void NotifyDespawned()
        {
            _doorCount--;
            if (_doorCount <= 0) { _doorCount = 0; SetEnabled(false); }
        }

        private static void SetEnabled(bool enable)
        {
            if (enable == _enabled) return;
            _enabled = enable;
            Init();

            if (enable)
            {
                Log.Message("[Fortified-SensorDoor] 感应门上线 挂载寻路补丁");
                PatchAll();
                Log.Message($"[Fortified-SensorDoor] 挂载完毕 共{_activeCount}个补丁");
            }
            else
            {
                Log.Message("[Fortified-SensorDoor] 无感应门 卸载寻路补丁");
                _harmony.UnpatchAll(_harmony.Id);
                _activeCount = 0;
            }
        }

        private static void PatchAll()
        {
            _activeCount = 0;

            Patch(typeof(RegionTypeUtility), nameof(RegionTypeUtility.GetExpectedRegionType), null,
                postfix: Get(typeof(Patch_SensorDoor_RegionType), "Postfix"));

            Patch(typeof(Thing), nameof(Thing.BlocksPawn), null,
                postfix: Get(typeof(Patch_SensorDoor_BlocksPawn), "Postfix"));

            Patch(typeof(PathGrid), nameof(PathGrid.CalculatedCostAt), null,
                postfix: Get(typeof(Patch_SensorDoor_PathCost), "Postfix"));

            Patch(typeof(Pawn_PathFollower), "TryEnterNextPathCell", null,
                prefix: Get(typeof(Patch_SensorDoor_TryEnter), "Prefix"));

            Patch(typeof(GenGrid), nameof(GenGrid.CanBeSeenOver), new[] { typeof(Building) },
                postfix: Get(typeof(Patch_SensorDoor_CanBeSeenOver), "Postfix"));
        }

        private static void Patch(Type type, string method, Type[] args,
            HarmonyMethod prefix = null, HarmonyMethod postfix = null)
        {
            var original = args != null
                ? AccessTools.Method(type, method, args)
                : AccessTools.Method(type, method);
            if (original == null)
            {
                Log.Error($"[Fortified-SensorDoor] 找不到方法 {type.Name}.{method}");
                return;
            }
            _harmony.Patch(original, prefix: prefix, postfix: postfix);
            _activeCount++;
        }

        private static HarmonyMethod Get(Type type, string method)
            => new HarmonyMethod(AccessTools.Method(type, method));
    }
}
