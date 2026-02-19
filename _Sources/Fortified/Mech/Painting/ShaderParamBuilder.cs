using HarmonyLib;
using Verse;
using UnityEngine;
using System.Collections.Generic;

namespace Fortified
{
    // 属性构建工具
    public static class ShaderParamBuilder
    {
        private static readonly System.Reflection.FieldInfo nameField = AccessTools.Field(typeof(ShaderParameter), "name");
        private static readonly System.Reflection.FieldInfo typeField = AccessTools.Field(typeof(ShaderParameter), "type");
        private static readonly System.Reflection.FieldInfo valueField = AccessTools.Field(typeof(ShaderParameter), "value");
        private static readonly System.Reflection.FieldInfo texField = AccessTools.Field(typeof(ShaderParameter), "valueTex");
        private static readonly object typeFloat = System.Enum.ToObject(typeof(ShaderParameter).GetNestedType("Type", System.Reflection.BindingFlags.NonPublic), 0);
        private static readonly object typeVector = System.Enum.ToObject(typeof(ShaderParameter).GetNestedType("Type", System.Reflection.BindingFlags.NonPublic), 1);
        private static readonly object typeTexture = System.Enum.ToObject(typeof(ShaderParameter).GetNestedType("Type", System.Reflection.BindingFlags.NonPublic), 3);

        // 构建参数列表
        public static List<ShaderParameter> Build(Color col3, FFF_CamoDef camo)
        {
            var list = new List<ShaderParameter>();
            try
            {
                // 设置颜色3
                var pCol3 = new ShaderParameter();
                nameField.SetValue(pCol3, "_ColorThree");
                typeField.SetValue(pCol3, typeVector);
                valueField.SetValue(pCol3, (Vector4)col3);
                list.Add(pCol3);

                // 设置迷彩参数
                if (camo?.Texture != null)
                {
                    var pFlag = new ShaderParameter();
                    nameField.SetValue(pFlag, "_UseCamo");
                    typeField.SetValue(pFlag, typeFloat);
                    valueField.SetValue(pFlag, new Vector4(1f, 0, 0, 0));
                    list.Add(pFlag);

                    var pCamo = new ShaderParameter();
                    nameField.SetValue(pCamo, "_CamoTex");
                    typeField.SetValue(pCamo, typeTexture);
                    texField.SetValue(pCamo, camo.Texture);
                    list.Add(pCamo);
                }
            }
            catch (System.Exception ex) { Log.ErrorOnce($"[Fortified] 着色器属性构建异常: {ex}", 912384); }
            return list;
        }
    }
}
