using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using HarmonyLib;

namespace Fortified
{
    // 配方继承扩展：指定从哪些工作台继承配方
    public class ModExtension_RecipeInheritance : DefModExtension
    {
        public List<ThingDef> inheritRecipesFrom;
        public List<RecipeDef> allowedRecipes;
        public List<RecipeDef> disallowedRecipes;
        public ThingFilter allowedProductFilter;
        public ThingFilter disallowedProductFilter;

        public bool Allows(RecipeDef recipe)
        {
            if (allowedRecipes != null && allowedRecipes.Contains(recipe)) return true;
            if (disallowedRecipes != null && disallowedRecipes.Contains(recipe)) return false;

            var producedThingDef = recipe.ProducedThingDef;
            if (producedThingDef != null)
            {
                if (allowedProductFilter != null && !allowedProductFilter.Allows(producedThingDef)) return false;
                if (disallowedProductFilter != null && disallowedProductFilter.Allows(producedThingDef)) return false;
            }

            return true;
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var error in base.ConfigErrors()) yield return error;
            if (inheritRecipesFrom == null) yield return "inheritRecipesFrom is null.";
        }
    }

    // 实现配方动态继承
    [StaticConstructorOnStartup]
    public static class RecipeInheritanceUtility
    {
        private static readonly AccessTools.FieldRef<ThingDef, List<RecipeDef>> AllRecipesCachedField =
            AccessTools.FieldRefAccess<ThingDef, List<RecipeDef>>("allRecipesCached");

        static RecipeInheritanceUtility()
        {
            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                var ext = def.GetModExtension<ModExtension_RecipeInheritance>();
                if (ext?.inheritRecipesFrom == null) continue;

                if (def.recipes == null) def.recipes = new List<RecipeDef>();
                HashSet<RecipeDef> currentRecipes = new HashSet<RecipeDef>(def.recipes);

                foreach (var parentBench in ext.inheritRecipesFrom)
                {
                    if (parentBench?.AllRecipes == null) continue;

                    foreach (var recipe in parentBench.AllRecipes)
                    {
                        if (ext.Allows(recipe) && currentRecipes.Add(recipe))
                        {
                            def.recipes.Add(recipe);
                        }
                    }
                }

                // 重置缓存
                AllRecipesCachedField(def) = null;
            }
        }
    }
}
