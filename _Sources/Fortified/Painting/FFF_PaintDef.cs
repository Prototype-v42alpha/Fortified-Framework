using Verse;

namespace Fortified;

// 涂装定义
public class FFF_PaintDef : Def
{
    // 图标纹理路径
    public string graphicPath;

    // 可选的喷涂遮罩路径
    public string maskPath;

    // 默认图标纹理缓存
    [Unsaved(false)]
    private Graphic graphic;

    public Graphic Graphic => graphic ??= GraphicDatabase.Get<Graphic_Single>(graphicPath, ShaderDatabase.Cutout);
}
