// 当白昼倾坠之时
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Fortified
{
    // 机兵休眠容器工具类
    public static class MechCapsuleUtility
    {
        #region 常量

        // 预定义的容器尺寸
        private static readonly int[] PredefinedSizes = { 1, 2, 3, 4, 5 };

        #endregion

        #region 缓存

        // 尺寸 -> 容器 Def 缓存
        private static Dictionary<int, ThingDef> _capsuleDefCache;

        #endregion

        #region 公共方法

        // 根据机兵获取合适的休眠容器 Def
        public static ThingDef GetCapsuleDefForMech(Pawn mech)
        {
            if (mech == null) return null;

            int size = GetMechSize(mech);
            return GetCapsuleDefForSize(size);
        }

        // 根据 PawnKindDef 获取合适的休眠容器 Def
        public static ThingDef GetCapsuleDefForKind(PawnKindDef kindDef)
        {
            if (kindDef == null) return null;

            int size = GetPawnKindSize(kindDef);
            return GetCapsuleDefForSize(size);
        }

        // 根据尺寸获取容器 Def
        public static ThingDef GetCapsuleDefForSize(int size)
        {
            EnsureCacheBuilt();

            // 找到最小的能容纳该尺寸的容器
            foreach (int predefinedSize in PredefinedSizes)
            {
                if (predefinedSize >= size && _capsuleDefCache.TryGetValue(predefinedSize, out var def))
                {
                    return def;
                }
            }

            // 超过最大预定义尺寸，尝试动态生成
            if (size > PredefinedSizes[PredefinedSizes.Length - 1])
            {
                return GetOrCreateDynamicCapsuleDef(size);
            }

            // 回退到最大的预定义容器
            return _capsuleDefCache.TryGetValue(PredefinedSizes[PredefinedSizes.Length - 1], out var fallback)
                ? fallback
                : null;
        }

        // 将机兵放入休眠容器
        public static Building_MechCapsule DeactivateMech(Pawn mech)
        {
            if (mech == null || mech.Dead || !mech.Spawned)
            {
                Log.Error("[FFF] DeactivateMech: invalid mech");
                return null;
            }

            ThingDef capsuleDef = GetCapsuleDefForMech(mech);
            if (capsuleDef == null)
            {
                Log.Error("[FFF] DeactivateMech: no suitable capsule def found");
                return null;
            }

            Map map = mech.Map;
            IntVec3 pos = mech.Position;
            Rot4 rot = mech.Rotation;
            Faction faction = mech.Faction;

            // 移除 overseer 关系
            Pawn overseer = mech.GetOverseer();
            if (overseer != null)
            {
                overseer.relations.RemoveDirectRelation(PawnRelationDefOf.Overseer, mech);
            }

            // 从地图移除机兵
            mech.DeSpawn();

            // 生成休眠容器
            Building_MechCapsule capsule = (Building_MechCapsule)ThingMaker.MakeThing(capsuleDef);
            capsule.SetFaction(faction);
            capsule.TryAcceptMech(mech);

            // 生成建筑
            GenSpawn.Spawn(capsule, pos, map, rot);

            return capsule;
        }

        #endregion

        #region 私有方法

        private static void EnsureCacheBuilt()
        {
            if (_capsuleDefCache != null) return;

            _capsuleDefCache = new Dictionary<int, ThingDef>();

            // 使用新的命名方式
            string[] defNames = { "FFF_MechCapsule_Small", "FFF_MechCapsule_Medium", "FFF_MechCapsule_Large", "FFF_MechCapsule_Huge", "FFF_MechCapsule_Colossal" };

            for (int i = 0; i < PredefinedSizes.Length && i < defNames.Length; i++)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defNames[i]);
                if (def != null)
                {
                    _capsuleDefCache[PredefinedSizes[i]] = def;
                }
            }
        }

        private static int GetMechSize(Pawn mech)
        {
            // 优先使用 Graphic 的 drawSize，向下取整后 -1
            if (mech.Drawer?.renderer?.BodyGraphic != null)
            {
                var drawSize = mech.Drawer.renderer.BodyGraphic.drawSize;
                float maxSize = System.Math.Max(drawSize.x, drawSize.y);
                int size = (int)System.Math.Floor(maxSize) - 1;
                return System.Math.Max(1, System.Math.Min(5, size));
            }

            // 回退到 bodySize
            float bodySize = mech.BodySize;
            if (bodySize <= 1.2f) return 1;
            if (bodySize <= 2.0f) return 2;
            if (bodySize <= 4.0f) return 3;
            if (bodySize <= 6.0f) return 4;
            return 5;
        }

        private static int GetPawnKindSize(PawnKindDef kindDef)
        {
            // 尝试从 lifeStages 的 bodyGraphicData 获取尺寸
            if (kindDef.lifeStages != null && kindDef.lifeStages.Count > 0)
            {
                var lastStage = kindDef.lifeStages[kindDef.lifeStages.Count - 1];
                if (lastStage?.bodyGraphicData != null)
                {
                    var drawSize = lastStage.bodyGraphicData.drawSize;
                    float maxSize = System.Math.Max(drawSize.x, drawSize.y);
                    int size = (int)System.Math.Floor(maxSize) - 1;
                    return System.Math.Max(1, System.Math.Min(5, size));
                }
            }

            // 回退到 bodySize
            float bodySize = kindDef.race?.race?.baseBodySize ?? 1f;
            if (bodySize <= 1.2f) return 1;
            if (bodySize <= 2.0f) return 2;
            if (bodySize <= 4.0f) return 3;
            if (bodySize <= 6.0f) return 4;
            return 5;
        }

        // 动态生成超大尺寸的容器 Def
        private static ThingDef GetOrCreateDynamicCapsuleDef(int size)
        {
            string defName = $"FFF_MechCapsule_{size}x{size}_Dynamic";
            ThingDef existing = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (existing != null) return existing;

            return CreateNewDynamicCapsuleDef(size, defName);
        }

        private static ThingDef CreateNewDynamicCapsuleDef(int size, string defName)
        {
            ThingDef baseDef = _capsuleDefCache[PredefinedSizes[PredefinedSizes.Length - 1]];
            if (baseDef == null)
            {
                Log.Error("[FFF] Cannot create dynamic capsule: base def not found");
                return null;
            }

            ThingDef newDef = NewCapsuleDef(size, defName, baseDef);
            DefDatabase<ThingDef>.Add(newDef);
            newDef.PostLoad();
            newDef.ResolveReferences();

            Log.Message($"[FFF] Dynamically created capsule def: {defName}");
            return newDef;
        }

        private static ThingDef NewCapsuleDef(int size, string defName, ThingDef baseDef)
        {
            return new ThingDef
            {
                defName = defName,
                label = $"deactivated mech capsule ({size}x{size})",
                description = baseDef.description,
                thingClass = typeof(Building_MechCapsule),
                category = ThingCategory.Building,
                size = new IntVec2(size, size),
                passability = Traversability.Impassable,
                fillPercent = 1f,
                useHitPoints = true,
                statBases = baseDef.statBases?.ListFullCopy(),
                building = baseDef.building,
                altitudeLayer = AltitudeLayer.Building,
                rotatable = false,
                selectable = true,
                drawGUIOverlay = true,
            };
        }

        #endregion
    }
}
