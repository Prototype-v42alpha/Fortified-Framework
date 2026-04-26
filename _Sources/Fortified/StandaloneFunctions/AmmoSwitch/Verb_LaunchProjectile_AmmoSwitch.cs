using Verse;

namespace Fortified
{
    public class Verb_LaunchProjectile_AmmoSwitch : Verb_LaunchProjectile
    {
        public override ThingDef Projectile
        {
            get
            {
                CompAmmoSwitch comp = EquipmentSource?.TryGetComp<CompAmmoSwitch>();
                if (comp?.CurrentProjectile != null)
                    return comp.CurrentProjectile;

                return base.Projectile;
            }
        }

        public override bool Available()
        {
            if (!base.Available()) return false;

            CompAmmoSwitch comp = EquipmentSource?.TryGetComp<CompAmmoSwitch>();
            if (comp != null && comp.IsOnSwitchCooldown && state != VerbState.Bursting)
                return false;

            return true;
        }

        public override bool TryStartCastOn(
            LocalTargetInfo castTarg,
            LocalTargetInfo destTarg,
            bool surpriseAttack = false,
            bool canHitNonTargetPawns = true,
            bool preventFriendlyFire = false,
            bool nonInterruptingSelfCast = false)
        {
            CompAmmoSwitch comp = EquipmentSource?.TryGetComp<CompAmmoSwitch>();
            if (comp != null && comp.IsOnSwitchCooldown && state != VerbState.Bursting)
                return false;

            return base.TryStartCastOn(castTarg, destTarg, surpriseAttack, canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);
        }
    }
}