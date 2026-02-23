using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Fortified
{
    // 情报处理器：管理隐蔽行动上下文
    public class FFF_IntelProcessor : GameComponent
    {
        private List<CovertOpContext> activeOps = new List<CovertOpContext>();
        private HashSet<Pawn> covertAgents = new HashSet<Pawn>();

        private static FFF_IntelProcessor cachedInstance;

        // 临时标记：当前正在执行隐蔽行动入场
        public static bool suppressGoodwillChange;

        // 是否有任何活跃隐蔽行动
        public static bool IsActive { get; private set; }

        public FFF_IntelProcessor(Game game) { }

        public static FFF_IntelProcessor Instance
        {
            get
            {
                if (cachedInstance == null)
                    cachedInstance = Current.Game
                        ?.GetComponent<FFF_IntelProcessor>();
                return cachedInstance;
            }
        }

        public override void FinalizeInit()
        {
            cachedInstance = this;
        }

        // 注册隐蔽行动(地图生成前调用)
        public CovertOpContext RegisterOp(
            MapParent mapParent, Faction targetFaction)
        {
            if (mapParent == null || targetFaction == null)
                return null;

            for (int i = 0; i < activeOps.Count; i++)
            {
                var op = activeOps[i];
                if (op.mapParent == mapParent
                    && op.targetFaction == targetFaction)
                    return op;
            }

            var ctx = new CovertOpContext
            {
                mapParent = mapParent,
                targetFaction = targetFaction,
                active = true,
            };
            activeOps.Add(ctx);
            RefreshActiveState();

            // 如果地图已生成则刷新缓存
            if (mapParent.HasMap)
            {
                mapParent.Map.attackTargetsCache
                    .Notify_FactionHostilityChanged(
                        targetFaction, Faction.OfPlayer);
            }

            return ctx;
        }

        // 地图生成后刷新缓存
        public void NotifyMapGenerated(CovertOpContext ctx)
        {
            if (ctx?.mapParent == null) return;
            if (!ctx.mapParent.HasMap) return;
            RefreshActiveState();
            ctx.mapParent.Map.attackTargetsCache
                .Notify_FactionHostilityChanged(
                    ctx.targetFaction, Faction.OfPlayer);
        }

        public void UnregisterOp(CovertOpContext ctx)
        {
            if (ctx == null) return;
            activeOps.Remove(ctx);
            RefreshActiveState();
        }

        // 查询：Map级(用于Thing/Lord补丁)
        public bool IsCovertOp(Map map, Faction faction)
        {
            if (map == null || faction == null) return false;
            for (int i = 0; i < activeOps.Count; i++)
            {
                var op = activeOps[i];
                if (!op.active || op.targetFaction != faction)
                    continue;
                if (op.mapParent != null
                    && op.mapParent.HasMap
                    && op.mapParent.Map == map)
                    return true;
            }
            return false;
        }

        // 查询：MapParent级(用于好感度补丁)
        public bool IsCovertOp(
            MapParent parent, Faction faction)
        {
            if (parent == null || faction == null) return false;
            for (int i = 0; i < activeOps.Count; i++)
            {
                var op = activeOps[i];
                if (op.active && op.mapParent == parent
                    && op.targetFaction == faction)
                    return true;
            }
            return false;
        }

        // 查询：派系是否有任何活跃隐蔽行动
        public bool HasActiveOp(Faction faction)
        {
            if (faction == null) return false;
            for (int i = 0; i < activeOps.Count; i++)
            {
                if (activeOps[i].active
                    && activeOps[i].targetFaction == faction)
                    return true;
            }
            return false;
        }

        // Pawn级：注册伪装特工
        public void RegisterAgent(Pawn pawn)
        {
            if (pawn == null) return;
            covertAgents.Add(pawn);
            RefreshActiveState();
        }

        // Pawn级：注销伪装特工
        public void UnregisterAgent(Pawn pawn)
        {
            if (pawn == null) return;
            covertAgents.Remove(pawn);
            RefreshActiveState();
        }

        // Pawn级：查询是否为伪装特工
        public bool IsCovertAgent(Pawn pawn)
        {
            return pawn != null && covertAgents.Contains(pawn);
        }

        // 查询：Tile级(用于大地图好感度补丁)
        public bool IsCovertOpAtTile(
            int tile, Faction faction)
        {
            if (tile < 0 || faction == null) return false;
            for (int i = 0; i < activeOps.Count; i++)
            {
                var op = activeOps[i];
                if (op.active && op.targetFaction == faction
                    && op.mapParent?.Tile == tile)
                    return true;
            }
            return false;
        }

        // 查询：获取指定地图上的隐蔽行动
        public CovertOpContext GetOpForMap(Map map)
        {
            if (map == null) return null;
            for (int i = 0; i < activeOps.Count; i++)
            {
                var op = activeOps[i];
                if (op.active && op.mapParent != null
                    && op.mapParent.HasMap
                    && op.mapParent.Map == map)
                    return op;
            }
            return null;
        }

        // 暴露事件：有目击者逃脱
        public void NotifyWitnessEscaped(CovertOpContext ctx)
        {
            if (ctx == null || !ctx.active) return;
            ctx.active = false;
            ctx.exposed = true;

            if (ctx.targetFaction != null
                && !ctx.targetFaction.defeated)
            {
                ctx.targetFaction.TryAffectGoodwillWith(
                    Faction.OfPlayer, -100,
                    canSendMessage: true,
                    canSendHostilityLetter: true,
                    reason: HistoryEventDefOf.AttackedMember);
            }
        }

        // 行动成功
        public void NotifyOpSuccess(CovertOpContext ctx)
        {
            if (ctx == null) return;
            ctx.active = false;
            UnregisterOp(ctx);
        }

        // 清理无效引用
        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % 2000 != 0) return;
            for (int i = activeOps.Count - 1; i >= 0; i--)
            {
                var op = activeOps[i];
                if (op.mapParent == null
                    || (op.mapParent.Destroyed && !op.active))
                {
                    activeOps.RemoveAt(i);
                }
            }
            covertAgents.RemoveWhere(
                p => p == null || p.Destroyed || p.Dead);
            RefreshActiveState();
        }

        // 同步 IsActive 并通知高频补丁动态开关
        private void RefreshActiveState()
        {
            bool nowActive = covertAgents.Count > 0;
            for (int i = 0; i < activeOps.Count; i++)
            {
                if (activeOps[i].active)
                {
                    nowActive = true;
                    break;
                }
            }
            if (nowActive == IsActive) return;
            IsActive = nowActive;
            FFF_CovertOpsPatchManager.SetEnabled(nowActive);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref activeOps,
                "activeOps", LookMode.Deep);
            if (activeOps == null)
                activeOps = new List<CovertOpContext>();

            var agentList = new List<Pawn>(covertAgents);
            Scribe_Collections.Look(ref agentList,
                "covertAgents", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                covertAgents = new HashSet<Pawn>();
                if (agentList != null)
                {
                    for (int i = 0; i < agentList.Count; i++)
                    {
                        if (agentList[i] != null)
                            covertAgents.Add(agentList[i]);
                    }
                }
            }
        }
    }

    // 隐蔽行动上下文
    public class CovertOpContext : IExposable
    {
        public MapParent mapParent;
        public Faction targetFaction;
        public bool active;
        public bool exposed;

        public void ExposeData()
        {
            Scribe_References.Look(ref mapParent, "mapParent");
            Scribe_References.Look(ref targetFaction,
                "targetFaction");
            Scribe_Values.Look(ref active, "active");
            Scribe_Values.Look(ref exposed, "exposed");
        }
    }
}
