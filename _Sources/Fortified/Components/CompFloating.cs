using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Fortified
{
    /// <summary>
    /// 悬浮组件 让Pawn忽略地形和物品的移动成本
    /// </summary>
    public class CompFloating : ThingComp
    {
        public CompProperties_Floating Props => (CompProperties_Floating)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            FloatingTracker.Register(parent);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            FloatingTracker.Unregister(parent);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            FloatingTracker.Unregister(parent);
        }
    }

    public class CompProperties_Floating : CompProperties
    {
        public bool canCrossWater = true;

        public CompProperties_Floating()
        {
            compClass = typeof(CompFloating);
        }
    }

    /// <summary>
    /// 追踪所有悬浮单位
    /// </summary>
    public static class FloatingTracker
    {
        private static HashSet<Thing> floatingThings = new HashSet<Thing>();

        public static void Register(Thing thing)
        {
            if (!floatingThings.Contains(thing))
            {
                floatingThings.Add(thing);
            }
        }

        public static void Unregister(Thing thing)
        {
            floatingThings.Remove(thing);
        }

        public static bool IsFloating(Thing thing)
        {
            return floatingThings.Contains(thing);
        }

        public static void Clear()
        {
            floatingThings.Clear();
        }
    }

    /// <summary>
    /// Patch CostToMoveIntoCell 让悬浮单位忽略地形和物品成本 (物理移动层)
    /// </summary>
    [HarmonyPatch(typeof(Pawn_PathFollower), "CostToMoveIntoCell", new[] { typeof(Pawn), typeof(IntVec3) })]
    public static class Patch_CostToMoveIntoCell_Floating
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn pawn, IntVec3 c, ref float __result)
        {
            if (pawn?.Map == null) return;
            if (!FloatingTracker.IsFloating(pawn)) return;

            TerrainDef terrain = pawn.Map.terrainGrid.TerrainAt(c);
            float baseCost = (c.x == pawn.Position.x || c.z == pawn.Position.z) ? pawn.TicksPerMoveCardinal : pawn.TicksPerMoveDiagonal;

            if (terrain == null || (terrain.passability == Traversability.Impassable && !terrain.IsWater && !terrain.forcePassableByFlyingPawns))
            {
                __result = 10000f;
                return;
            }

            // 只检查建筑物是否阻挡
            List<Thing> things = pawn.Map.thingGrid.ThingsListAt(c);
            for (int i = 0; i < things.Count; i++)
            {
                Thing t = things[i];
                // 只有真正的建筑且不可通过才阻挡
                if (t.def.passability == Traversability.Impassable && t is Building && !t.def.forcePassableByFlyingPawns)
                {
                    __result = 10000f;
                    return;
                }
            }

            __result = baseCost;
        }
    }

    /// <summary>
    /// 让悬浮单位在寻路时使用 Flying 路径网格 (逻辑寻路层)
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetPathContext))]
    public static class Patch_Pawn_GetPathContext_Floating
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn __instance, Pathing pathing, ref PathingContext __result)
        {
            if (FloatingTracker.IsFloating(__instance))
            {
                __result = pathing.Flying;
            }
        }
    }

    /// <summary>
    /// 确保飞行路径网格忽略地形和物品的寻路成本
    /// </summary>
    [HarmonyPatch(typeof(PathGrid), nameof(PathGrid.CalculatedCostAt))]
    public static class Patch_PathGrid_CalculatedCostAt_Floating
    {
        [HarmonyPrefix]
        public static bool Prefix(PathGrid __instance, IntVec3 c, ref int __result)
        {
            // 如果是飞行路径网格
            if (__instance.def.flying)
            {
                TerrainDef terrain = __instance.map.terrainGrid.TerrainAt(c);
                if (terrain == null || (terrain.passability == Traversability.Impassable && !terrain.forcePassableByFlyingPawns && !terrain.IsWater))
                {
                    __result = 10000;
                    return false;
                }

                // 只检查建筑物是否阻挡
                List<Thing> list = __instance.map.thingGrid.ThingsListAt(c);
                for (int i = 0; i < list.Count; i++)
                {
                    Thing t = list[i];
                    // 只有墙壁等真正建筑才阻挡飞行
                    if (t.def.passability == Traversability.Impassable && t is Building && !t.def.forcePassableByFlyingPawns)
                    {
                        __result = 10000;
                        return false;
                    }
                }

                // 飞行路径的寻路成本设为 0
                __result = 0;
                return false;
            }
            return true;
        }
    }
}
