using RimWorld;
using Verse;

namespace Fortified
{
    public class HeavyEquippableExtension : DefModExtension
    {
        public HeavyEquippableDef EquippableDef;

        /// <summary>
        /// 處裡以下狀況：
        /// 1.是特定種族(Race)
        /// 2.穿戴特定服裝
        /// 3.具有特定Hediff
        /// 4.具有特定基因
        /// 5.體型判定。(-1 為非體型判斷的固定武器)
        /// 6.以上皆符合則回傳true，否則false
        /// </summary>
        public bool CanEquippedBy(Pawn pawn) //無論是其他種族還是機兵都吃這個判斷，但能夠從Tag層面上就能使用該武器的機兵不受限制
        {
            if (EquippableDef == null)
            {
                Log.Warning("HeavyEquippableExtension doesn't have a HeavyEquippableDef sets!");
                return true;
            }

            // 先處理所有可免除體型檢查的條件
            if (EquippableDef.EquippableByRace.NotNullAndContains(pawn.def)) return true;
            if (CheckUtility.HasAnyApparelOf(pawn, EquippableDef.EquippableWithApparel)) return true;
            if (CheckUtility.HasAnyHediffOf(pawn, EquippableDef.EquippableWithHediff)) return true;
            if (CheckUtility.HasAnyGeneOf(pawn, EquippableDef.EquippableWithGene)) return true;

            // 體型判定。(-1 為非體型判斷的固定武器)
            float requiredSize = EquippableDef.EquippableBaseBodySize;
            if (requiredSize == -1f) return false;

            // 機械體特殊處理：僅當有 MechWeaponExtension 且 EnableWeaponFilter == false 時才允許以體型判定
            if (pawn.RaceProps.IsMechanoid)
            {
                var ext = pawn.def.GetModExtension<MechWeaponExtension>();
                if (ext == null) return false;
                if (!ext.EnableWeaponFilter && pawn.BodySize >= requiredSize) return true;
            }
            else if(pawn.BodySize >= requiredSize) return true;
            return false;
        }

        /// <summary>
        /// 初始化階段提供清單生成使用的判斷。
        /// </summary>
        public bool CanEquippedBy(ThingDef pawnRaceDef) //無論是其他種族還是機兵都吃這個判斷，但能夠從Tag層面上就能使用該武器的機兵不受限制
        {
            if (EquippableDef == null)
            {
                Log.Warning("HeavyEquippableExtension doesn't have a HeavyEquippableDef sets!");
                return true;
            }
            if (EquippableDef.EquippableByRace.Contains(pawnRaceDef)) return true;

            var ext = pawnRaceDef.GetModExtension<MechWeaponExtension>();
            //啟用體型篩選的機械體無法通過此判定
            if (ext == null || ext.EnableTechLevelFilter) return false;

            float requiredSize = EquippableDef.EquippableBaseBodySize;
            if (requiredSize == -1f) return false;
            if (pawnRaceDef?.race != null && pawnRaceDef.race.baseBodySize >= requiredSize) return true;

            return false;
        }
    }
}
