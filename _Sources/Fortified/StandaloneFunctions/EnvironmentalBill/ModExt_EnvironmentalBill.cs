using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Fortified;

public class ModExt_EnvironmentalBill : DefModExtension
{
    public bool OnlyInCleanliness = false;
    public bool OnlyInDarkness = false;
    public bool OnlyInVacuum = false;
    public bool OnlyInMicroGravity = false;

    public IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        List<string> reportTexts = new();
        if (OnlyInCleanliness)
            reportTexts.Add("FFF.CleanRoom".Translate());
        if (OnlyInDarkness)
            reportTexts.Add("FFF.Darkness".Translate());
        if (OnlyInVacuum)
            reportTexts.Add("FFF.Vacuum".Translate());
        if (OnlyInMicroGravity)
            reportTexts.Add("FFF.MicroGravity".Translate());

        yield return new StatDrawEntry(
            StatCategoryDefOf.Basics,
            "FFF.EnvironmentRestriction".Translate(),
            string.Join(' ', reportTexts),
            null, 1145);
    }
}