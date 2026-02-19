using Verse;
using UnityEngine;

namespace Fortified
{
    // 染色渲染节点
    public class PawnRenderNode_Painting : PawnRenderNode
    {
        public PawnRenderNode_Painting(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
        }

        public override Color ColorFor(Pawn pawn)
        {
            var comp = pawn.TryGetComp<CompPaintable>();
            if (comp == null) return base.ColorFor(pawn);
            return comp.color1;
        }

        public override Graphic GraphicFor(Pawn pawn)
        {
            var comp = pawn.TryGetComp<CompPaintable>();
            Graphic baseGraphic = base.GraphicFor(pawn);
            if (comp == null || baseGraphic == null) return baseGraphic;

            Color c1 = comp.color1;
            Color c2 = comp.color2;
            Color c3 = comp.color3;
            float br = comp.brightness;
            FFF_CamoDef camo = comp.camoDef;
            FFF_OverlayDef overlay = comp.overlayDef;

            Shader shader = FFF_AssetLoader.PaintShader ?? baseGraphic.Shader;
            if (FFF_AssetLoader.PaintShader != null) c2.a = br;

            // 复用参数构建逻辑支持旋转
            var shaderParams = comp.GetOrBuildShaderParams(c3, camo);
            var graphic = GraphicDatabase.Get(baseGraphic.GetType(), baseGraphic.path, shader,
                baseGraphic.drawSize, c1, c2, baseGraphic.data, shaderParams);

            // 对四个方向材质各自设置朝向相关属性
            Harmony_Painting.ApplyPerDirectionProps(graphic, camo, overlay);
            return graphic;
        }
    }
}
