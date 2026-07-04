using RimWorld;
using Verse;

namespace Fortified
{
    // 叠加属性曲线
    public class StatPart_StatToCurve : StatPart
    {
        // 输入属性
        public StatDef inputStat;

        // 属性偏移曲线
        public SimpleCurve curve;

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (!(req.Thing is Pawn pawn)) return;
            if (inputStat == null || curve == null) return;

            float input = pawn.GetStatValue(inputStat);
            val += curve.Evaluate(input);
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (!(req.Thing is Pawn pawn)) return null;
            if (inputStat == null || curve == null) return null;

            float input = pawn.GetStatValue(inputStat);
            float offset = curve.Evaluate(input);
            string inputStr = inputStat.Worker.ValueToString(input, false);
            string offsetStr = offset >= 0f ? $"+{offset.ToStringPercent()}" : offset.ToStringPercent();
            return $"{inputStat.LabelCap} ({inputStr}): {offsetStr}";
        }
    }
}
