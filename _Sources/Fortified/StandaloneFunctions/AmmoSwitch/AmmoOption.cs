using UnityEngine;
using Verse;

namespace Fortified
{
    public class AmmoOption
    {
        public string label;
        public ThingDef projectileDef;
        public string iconPath;
        public string description;

        public Texture2D ResolveIcon()
        {
            if (!iconPath.NullOrEmpty())
            {
                return ContentFinder<Texture2D>.Get(iconPath, false);
            }
            return projectileDef?.uiIcon;
        }

        public string ResolveLabel()
        {
            if (!label.NullOrEmpty()) return label;
            return projectileDef?.label ?? "N/A";
        }
    }
}