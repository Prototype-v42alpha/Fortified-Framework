using Verse;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using System.Security.Cryptography;

namespace Fortified
{
    public class StatWorker_MechWeapon : StatWorker
    {
        public override string GetStatDrawEntryLabel(StatDef stat, float value, ToStringNumberSense numberSense, StatRequest optionalReq, bool finalized = true)
        {
            return "FFF.ViewUsableWeapons".TranslateSimple();
        }
        public override bool ShouldShowFor(StatRequest req)
        {
            return base.ShouldShowFor(req) && req.Def.HasModExtension<MechWeaponExtension>();
        }

        public override IEnumerable<Dialog_InfoCard.Hyperlink> GetInfoCardHyperlinks(StatRequest statRequest)
        {
            MechWeaponExtension ext = statRequest.Def.GetModExtension<MechWeaponExtension>();
            if (ext != null)
            {
                var users = ext.UsableWeaponTags;
                List<ThingDef> list = WeaponTagUtil.GetWeapons(users).ToList();
                list.SortBy(v => v.BaseMass);

                if (!users.NullOrEmpty())
                {
                    foreach (var def in list)
                    {
                        if (!ext.EnableTechLevelFilter || ext.UsableTechLevels.Contains(def.techLevel))
                        {
                            yield return new Dialog_InfoCard.Hyperlink(def);
                        }

                    }
                }
                if (!ext.BypassUsableWeapons.NullOrEmpty())
                {
                    foreach (var item in ext.BypassUsableWeapons)
                    {
                        if (WeaponTagUtil.WeaponExists(item, out var def))
                        {
                            yield return new Dialog_InfoCard.Hyperlink(def);
                        }
                    }
                }
            }
            TurretMannableExtension ext2 = statRequest.Def.GetModExtension<TurretMannableExtension>();
            if (ext2 != null)
            {
                if (ext2.mannableByDefault)
                {
                    foreach (ThingDef item in WeaponTagUtil.GetTurrets)
                    {
                        yield return new Dialog_InfoCard.Hyperlink(item);
                    }
                }
                else if (!ext2.BypassMannable.NullOrEmpty())
                {
                    foreach (string item in ext2.BypassMannable)
                    {
                        if (WeaponTagUtil.WeaponExistsInTurretDict(item, out ThingDef def))
                        {
                            yield return new Dialog_InfoCard.Hyperlink(def);
                        }
                    }
                }
            }
        }

        public override string GetExplanationFinalizePart(StatRequest req, ToStringNumberSense numberSense, float finalVal)
        {
            string s = "";
            MechWeaponExtension ext = req.Def.GetModExtension<MechWeaponExtension>();
            if (ext != null)
            {
                if (ext.EnableTechLevelFilter)
                {
                    //必須是下列科技等級的裝備
                    s += "FFF.Req_TechLevel".Translate() + "\n";
                    foreach (TechLevel item in ext.UsableTechLevels)
                    {
                        s += "- "+item.ToStringHuman().Translate() + "\n";
                    }
                }
                s += "\n";
                if (!ext.EnableWeaponFilter)
                {
                    if (req.Def is ThingDef def && def.race?.baseBodySize > 1)
                    {
                        //在無額外裝備的情況下能直接使用支援體型要求不高於: {0} 的武器。
                        s += "FFF.Req_BodySize".Translate(def.race.baseBodySize) + "\n\n";
                    }

                    //沒有其他裝備限制。
                    s += "FFF.Req_NoWeaponFilter".Translate();
                }
                else
                {
                    //僅能夠使用下列的武器。
                    s += "FFF.Req_WeaponFilter".Translate();
                }
            }
            return s;
        }
    }
}
