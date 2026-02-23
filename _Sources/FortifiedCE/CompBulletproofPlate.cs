using CombatExtended;
using Fortified;
using RimWorld;
using UnityEngine;
using Verse;

namespace FortifiedCE;

public class CompBulletproofPlate : Fortified.CompBulletproofPlate
{
    public override string armorString => "FFF.Armor.CE".Translate(Props.armorRating);
    //{0}毫米等效均質鋼
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

        if (BypassChance()) return; // 有一定几率完全无视部位覆盖，模拟子弹偶尔会偏移或击中装甲缝隙
        //if (!IsInCoveredGroups(dinfo))
        //{
        //    return;
        //}

        if (dinfo.HitPart == null) dinfo.SetHitPart(Wearer.health.hediffSet.GetBodyPartRecord(BodyPartDefOf.Torso));

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
            preventCascade: dinfo.PreventCascade
        );
        
        DamageInfo afterArmorDinfo = ArmorUtilityCE.GetAfterArmorDamage(
            damage,
            Wearer,
            damage.HitPart,
            out bool armorDeflected,
            out bool armorReduced,
            out bool shieldAbsorbed
        );

        float incoming = damage.Amount;
        float afterAmt = afterArmorDinfo.Amount;
        float absorbedByPlate = incoming - afterAmt;

        if (absorbedByPlate <= 0f)
        {
            // Nothing absorbed by the plate
            dinfo = afterArmorDinfo;
            return;
        }

        DeflectEffect(dinfo);
        //absorbed = true;

        if (currentDurability >= absorbedByPlate)
        {
            // Plate fully absorbs its portion and survives
            currentDurability -= absorbedByPlate;
            dinfo.SetAmount(0f);
            absorbed = true;
        }
        else
        {
            // Plate breaks while absorbing part of the absorbedByPlate
            float oldDurability = currentDurability;
            float remainingDamageToApply = Mathf.Max(0f, incoming - oldDurability);
            currentDurability = 0f;
            dinfo.SetAmount(remainingDamageToApply);
        }
    }
}