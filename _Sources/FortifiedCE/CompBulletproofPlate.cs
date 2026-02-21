using CombatExtended;
using Fortified;
using Verse;

namespace FortifiedCE;

public class CompBulletproofPlate : Fortified.CompBulletproofPlate
{
    public override string armorString => $"{Props.armorRating:P0} " + "FFF.Armor.CE".Translate();
    //{0}²@¦Ìµ¥®Ä§¡½è¿û
    public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
    {
        absorbed = false;

        if (currentDurability <= 0)
        {
            return;
        }
        
        if (!dinfo.Def.harmsHealth)
        {
            return;
        }

        if (!IsInCoveredGroups(dinfo))
        {
            return;
        }

        // Use CombatExtended's ArmorUtilityCE for proper armor calculations
        float originalDamage = dinfo.Amount;

        DamageInfo damage = new DamageInfo(
            dinfo.Def,
            dinfo.Amount,
            armorPenetration: dinfo.ArmorPenetrationInt - Props.armorRating,
            angle: dinfo.Angle,
            instigator: dinfo.Instigator,
            hitPart: dinfo.HitPart,
            weapon: dinfo.Weapon,
            category: dinfo.Category,
            intendedTarget: dinfo.IntendedTarget,
            instigatorGuilty: dinfo.InstigatorGuilty,
            spawnFilth: dinfo.SpawnFilth,
            weaponQuality: dinfo.WeaponQuality,
            checkForJobOverride: dinfo.CheckForJobOverride,
            preventCascade: dinfo.PreventCascade);


        // Call CE's armor calculation system
        // This handles all armor penetration mechanics, shield deflection, and armor effects
        DamageInfo afterArmorDinfo = ArmorUtilityCE.GetAfterArmorDamage(
            damage, 
            Wearer,
            damage.HitPart, 
            out bool armorDeflected, 
            out bool armorReduced, 
            out bool shieldAbsorbed
        );
        if (armorReduced || armorDeflected)
        {
            currentDurability -= (originalDamage + afterArmorDinfo.Amount);
        }
        
        
        if (currentDurability < 0)
        {
            currentDurability = 0;
        }
        
        // Update the original damage info with CE's calculated damage
        dinfo = afterArmorDinfo;
        
        // If armor completely deflected the damage
        if (armorDeflected)
        {
            absorbed = true;
        }
          }
}
