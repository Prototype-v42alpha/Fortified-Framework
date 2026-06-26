using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Fortified
{
    public class CompAbilityEffect_RemoveHediffOnDepleted : CompAbilityEffect
    {
        public new CompProperties_RemoveHediffOnDepleted Props => (CompProperties_RemoveHediffOnDepleted)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            // 需耗尽时检查次数 未用尽则跳过
            if (Props.requireDepleted && (!parent.UsesCharges || parent.RemainingCharges > 0)) return;

            Hediff source = FindSourceHediff();
            if (source == null) return;

            parent.pawn.health.RemoveHediff(source);

            // 移除后替换为指定Hediff
            if (Props.replaceWithHediffDef != null)
            {
                parent.pawn.health.AddHediff(Props.replaceWithHediffDef);
            }

            if (!Props.depletedMessageKey.NullOrEmpty())
            {
                // 使用者自定义翻译ID 支持PAWN占位
                string text = Props.depletedMessageKey.Translate(parent.pawn.Named("PAWN"));
                Messages.Message(text, parent.pawn, MessageTypeDefOf.NeutralEvent);
            }
        }

        // 反查持有该能力的改装Hediff
        private Hediff FindSourceHediff()
        {
            List<Hediff> hediffs = parent.pawn?.health?.hediffSet?.hediffs;
            if (hediffs == null) return null;

            foreach (Hediff hediff in hediffs)
            {
                if (hediff.def.abilities.NullOrEmpty()) continue;
                if (hediff.AllAbilitiesForReading.Contains(parent)) return hediff;
            }
            return null;
        }
    }

    public class CompProperties_RemoveHediffOnDepleted : CompProperties_AbilityEffect
    {
        public string depletedMessageKey;

        public HediffDef replaceWithHediffDef;

        // 是否仅在次数耗尽时触发
        public bool requireDepleted = true;

        public CompProperties_RemoveHediffOnDepleted()
        {
            compClass = typeof(CompAbilityEffect_RemoveHediffOnDepleted);
        }
    }
}
