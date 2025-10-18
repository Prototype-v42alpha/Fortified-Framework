using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Fortified
{
    public class FloatMenuOptionProvider_WeaponUsable : FloatMenuOptionProvider
    {
        protected override bool Drafted => true;
        protected override bool Undrafted => true;
        protected override bool Multiselect => false;
        protected override bool MechanoidCanDo => true;

        //實際開始判定前的可用性檢測(Tracker跟Type)
        public override bool Applies(FloatMenuContext context)
        {
            if (!base.Applies(context)) return false;
            return context.FirstSelectedPawn is IWeaponUsable;
        }
        protected override bool AppliesInt(FloatMenuContext context)
        {
            return true;
        }
        public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
        {
            var ext = context.FirstSelectedPawn.def.GetModExtension<MechWeaponExtension>();
            foreach (FloatMenuOption item in FloatMenuUtility.GetExtraFloatMenuOptionsFor(context, clickedThing, ext))
            {
                yield return item;
            }
        }
    }
}