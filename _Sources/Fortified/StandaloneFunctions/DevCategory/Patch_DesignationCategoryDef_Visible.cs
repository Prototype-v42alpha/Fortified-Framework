using Verse;
namespace Fortified
{
    public static class Patch_DesignationCategoryDef_Visible
    {
        private const string TargetCategoryDefName = "FFF_DevCategory";

        public static bool Prefix(DesignationCategoryDef __instance, ref bool __result)
        {
            if (__instance?.defName == TargetCategoryDefName)
            {
                __result = DebugSettings.godMode;
                return false;
            }

            return true;
        }
    }
}