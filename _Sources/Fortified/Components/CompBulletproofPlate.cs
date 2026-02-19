using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Fortified;


public class CompProperties_BulletproofPlate : CompProperties
{
    public float armorRating = 0.4f;
    public float maxDurability = 100f;
    public ThingDef plateThing;
    public float durabilityRestorePerMaterial = 10f;

    public CompProperties_BulletproofPlate()
    {
        compClass = typeof(CompBulletproofPlate);
    }
}
public class CompBulletproofPlate : ThingComp
{
    private CompProperties_BulletproofPlate Props => (CompProperties_BulletproofPlate)props;
    private float currentDurability;

    public float DurabilityPercent => currentDurability / Props.maxDurability;

    public Pawn wearer;
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        wearer = (parent as Apparel)?.Wearer;
        if (currentDurability == 0 && !respawningAfterLoad)
        {
            currentDurability = Props.maxDurability;
        }
    }

    public bool IsInCoveredGRoups(DamageInfo dinfo)
    {
        return parent.def.apparel.CoversBodyPart(dinfo.HitPart);
    }
    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref currentDurability, "currentDurability", Props.maxDurability);
    }

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
        if (!IsInCoveredGRoups(dinfo)) return;

        DamageDef damage = dinfo.Def;
        damage.defaultArmorPenetration -= Props.armorRating;
        float damageAmount = ArmorUtility.GetPostArmorDamage(wearer, dinfo.Amount, dinfo.ArmorPenetrationInt, dinfo.HitPart, ref damage, out var _A, out var _B);

        dinfo.Def = damage;

        currentDurability -= damageAmount;

        if (currentDurability < 0)
        {
            currentDurability = 0;
        }

        dinfo.SetAmount(damageAmount);
    }

    public void RestoreDurability(float amount)
    {
        currentDurability = Math.Min(currentDurability + amount, Props.maxDurability);
    }

    public float GetCurrentDurability()
    {
        return currentDurability;
    }

    public float GetMaxDurability()
    {
        return Props.maxDurability;
    }

    public int GetMaterialCostForRefill()
    {
        if (Props.plateThing == null || Props.durabilityRestorePerMaterial <= 0)
        {
            return 0;
        }

        float durabilityNeeded = Props.maxDurability - currentDurability;
        return Math.Max(1, (int)Math.Ceiling(durabilityNeeded / Props.durabilityRestorePerMaterial));
    }

    public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
    {
        if (Props.plateThing == null)
        {
            yield break;
        }

        if (parent is not Apparel apparel || apparel.Wearer != selPawn)
        {
            yield break;
        }

        if (Math.Abs(currentDurability - Props.maxDurability) < 0.01f)
        {
            yield break;
        }

        int materialCost = GetMaterialCostForRefill();
        string label = $"Refill {parent.LabelCap} ({materialCost}x {Props.plateThing.label})";

        FloatMenuOption option = new FloatMenuOption(label, () =>
        {
            List<Thing> availableMaterials = HaulAIUtility.FindFixedIngredientCount(selPawn, Props.plateThing, materialCost);

            if (!availableMaterials.NullOrEmpty())
            {
                Job job = HaulAIUtility.HaulToContainerJob(selPawn, availableMaterials[0], parent);
                job.count = Mathf.Min(job.count, materialCost);
                job.targetQueueB = new List<LocalTargetInfo> { new LocalTargetInfo(parent) };
                selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }
            else
            {
                Messages.Message($"Not enough {Props.plateThing.label} to refill {parent.LabelCap}.", MessageTypeDefOf.RejectInput);
            }
        });

        yield return option;
    }
    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
        {
            yield return gizmo;
        }

        if (parent is Apparel apparel && apparel.Wearer != null && (apparel.Wearer.Faction == Faction.OfPlayer))
        {
            yield return new Gizmo_BulletproofPlateStatus
            {
                bulletproofPlate = this
            };
        }
    }

    public override string CompInspectStringExtra()
    {
        string text = $"Durability: {currentDurability:F1}/{Props.maxDurability:F1} ({DurabilityPercent:P0})";

        if (Props.plateThing != null)
        {
            int materialCost = GetMaterialCostForRefill();
            text += $"\nRefill cost: {materialCost}x {Props.plateThing.label}";
        }

        return text;
    }
}