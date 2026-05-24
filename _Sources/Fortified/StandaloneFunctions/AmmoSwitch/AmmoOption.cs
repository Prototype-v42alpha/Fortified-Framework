using UnityEngine;
using Verse;

namespace Fortified
{
	public class AmmoOption
	{
		[MustTranslate]
		public string label;
		public ThingDef projectileDef;
		public string iconPath;
		[MustTranslate]
		public string description;
		public float warmUpFactor = 1f;
		public float accuracyFactor = 1f;
		public float rangeFactor = 1f;
		public int? burstShotCountOverride = null;
		/// <summary>
		/// If true, this option represents using the weapon's default projectile instead of a custom one.
		/// When useDefaultProjectile is true, projectileDef will be ignored and the base verb projectile will be used.
		/// </summary>
		public bool useDefaultProjectile = false;

		public Texture2D ResolveIcon()
		{
			if (!iconPath.NullOrEmpty())
			{
				return ContentFinder<Texture2D>.Get(iconPath, false);
			}
			// If using default projectile or has custom projectileDef, try to get its icon
			return projectileDef?.uiIcon ?? BaseContent.BadTex;
		}

		public string ResolveLabel()
		{
			if (!label.NullOrEmpty()) return label;
			if (useDefaultProjectile) return "FFF.AmmoSwitch.DefaultAmmo".Translate();
			return projectileDef?.label ?? "N/A";
		}
    }
}