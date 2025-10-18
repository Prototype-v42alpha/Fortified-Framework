using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Fortified
{
    public class FloatMenuOptionProvider_UseStylingStation : FloatMenuOptionProvider
    {
        protected override bool Drafted => false;
        protected override bool Undrafted => true;
        protected override bool Multiselect => false;
        protected override bool MechanoidCanDo => true;
        public override bool Applies(FloatMenuContext context)
        {
            if (!base.Applies(context)) return false;
            extension = context.FirstSelectedPawn.def.GetModExtension<HumanlikeMechExtension>();
            return extension != null && extension.canChangeHairStyle;
        }
        private HumanlikeMechExtension extension;
        public override bool TargetThingValid(Thing thing, FloatMenuContext context)
        {
            return thing is Building_StylingStation;
        }
        public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
        {
            return (clickedThing as Building_StylingStation).GetFloatMenuOptions(context.FirstSelectedPawn);
        }
    }
}