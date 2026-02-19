using System.Collections.Generic;
using Verse;
using UnityEngine;

namespace Fortified
{
    // 叠加层纹理定义
    public class FFF_OverlayDef : Def
    {
        // 基础路径自动发现四向后缀
        [NoTranslate]
        public string texPath;

        // 叠乘基础纹理获取起伏效果
        public bool multiplyBase = false;

        // 适用的机械体种类列表
        public List<PawnKindDef> applicablePawnKinds = new List<PawnKindDef>();

        // 四向纹理缓存 NESW
        private Texture2D[] cached;

        // 获取对应朝向的纹理
        public Texture2D GetTexture(Rot4 rot)
        {
            if (cached == null) BuildCache();
            return cached[rot.AsInt];
        }

        // 发现并回退四向纹理
        private void BuildCache()
        {
            cached = new Texture2D[4];
            if (texPath.NullOrEmpty()) return;

            // 尝试加载四向
            var n = ContentFinder<Texture2D>.Get(texPath + "_north", false);
            var e = ContentFinder<Texture2D>.Get(texPath + "_east", false);
            var s = ContentFinder<Texture2D>.Get(texPath + "_south", false);
            var w = ContentFinder<Texture2D>.Get(texPath + "_west", false);

            // 无后缀回退：尝试直接路径作为南向
            if (s == null) s = ContentFinder<Texture2D>.Get(texPath, false);

            // 回退
            if (n == null) n = s ?? e ?? w;
            if (s == null) s = n;
            if (e == null) e = w ?? n;
            if (w == null) w = e ?? n;

            // NESW 顺序
            cached[0] = n;
            cached[1] = e;
            cached[2] = s;
            cached[3] = w;
        }
    }
}
