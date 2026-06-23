using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Fortified
{
    // 感应门寻路补丁集 由FFF_SensorDoorPatchManager动态挂载
    // 让Impassable建筑挂CompBuildingMover(sensorDoor)后被授权pawn当门通行

    // 补丁1 区域类型 感应门洞格恒为Portal 使其生成稳定region
    public static class Patch_SensorDoor_RegionType
    {
        public static void Postfix(IntVec3 c, Map map, ref RegionType __result)
        {
            if (__result == RegionType.Portal) return;
            if (map == null || !c.InBounds(map)) return;
            CompBuildingMover door = CompBuildingMover.GetSensorDoorAt(map, c);
            if (door == null) return;
            // 门洞footprint恒为Portal 不看开度不看滑动 避免region翻转
            __result = RegionType.Portal;
        }
    }

    // 补丁2 关门挡路 未授权pawn被挡 授权pawn放行(走等门逻辑)
    public static class Patch_SensorDoor_BlocksPawn
    {
        public static void Postfix(Thing __instance, Pawn p, ref bool __result)
        {
            CompBuildingMover door = CompBuildingMover.GetSensorDoor(__instance);
            if (door == null) return;
            __result = door.BlocksPawnNow(p);
        }
    }

    // 补丁5 关门挡视线 门敞开时放行
    public static class Patch_SensorDoor_CanBeSeenOver
    {
        public static void Postfix(Building b, ref bool __result)
        {
            CompBuildingMover door = CompBuildingMover.GetSensorDoor(b);
            if (door == null) return;
            // 门未完全敞开则挡视线
            if (!door.DoorOpen || door.Sliding) __result = false;
        }
    }

    // 补丁3 寻路代价 Impassable感应门改为高代价可通行
    public static class Patch_SensorDoor_PathCost
    {
        public static void Postfix(IntVec3 c, ref int __result, PathGrid __instance, Map ___map)
        {
            if (___map == null || !c.InBounds(___map)) return;
            CompBuildingMover door = CompBuildingMover.GetSensorDoorAt(___map, c);
            if (door == null) return;
            int cost = door.SensorDoorPathCostAt(c);
            __result = __result >= 10000 ? cost : Mathf.Max(__result, cost);
        }
    }

    // 补丁4 走到门前 触发让路并等待开门
    [HarmonyPatch(typeof(Pawn_PathFollower), "TryEnterNextPathCell")]
    public static class Patch_SensorDoor_TryEnter
    {
        public static bool Prefix(Pawn_PathFollower __instance, Pawn ___pawn)
        {
            Pawn pawn = ___pawn;
            if (pawn?.Map == null) return true;
            IntVec3 next = __instance.nextCell;
            if (!next.InBounds(pawn.Map)) return true;

            CompBuildingMover door = CompBuildingMover.GetSensorDoorAt(pawn.Map, next);
            if (door == null) return true;
            if (!door.PawnCanOpen(pawn)) return true;

            // 等待目标格露出
            if (!door.CanPassDoorCell(next))
            {
                door.NotifyApproachAndOpen(pawn, next);
                Stance_Cooldown stance = new Stance_Cooldown(10, door.parent, null) { neverAimWeapon = true };
                pawn.stances.SetStance(stance);
                return false;
            }
            return true;
        }
    }
}
