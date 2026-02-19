using Verse;
using UnityEngine;

namespace Fortified
{
    // 迷彩纹理定义
    public class FFF_CamoDef : Def
    {
        [NoTranslate]
        public string texPath;

        private Texture2D cachedTexture;

        public Texture2D Texture
        {
            get
            {
                if (cachedTexture == null)
                    cachedTexture = ContentFinder<Texture2D>.Get(texPath, true);
                return cachedTexture;
            }
        }
    }
}
