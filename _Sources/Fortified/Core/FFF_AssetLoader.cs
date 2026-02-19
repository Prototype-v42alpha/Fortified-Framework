using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace Fortified;
// 资产加载器
[StaticConstructorOnStartup]
public static class FFF_AssetLoader
{
    public static Shader PaintShader;

    static FFF_AssetLoader()
    {
        LongEventHandler.ExecuteWhenFinished(Init);
    }

    // 执行初始化
    private static void Init()
    {
        try
        {
            PaintShader = LoadShaderFromModAssets();
            if (PaintShader == null)
            {
                PaintShader = Shader.Find("Fortified/StandardPaintable");
            }

            if (PaintShader != null)
            {
                Log.Message($"[Fortified] 涂装着色器加载成功 {PaintShader.name}");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[Fortified] 资产加载异常 {ex}");
        }
    }

    // 从模组资源包加载着色器
    private static Shader LoadShaderFromModAssets()
    {
        foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading)
        {
            if (!mod.PackageId.ToLower().Contains("aoba.framework")) continue;

            foreach (AssetBundle bundle in mod.assetBundles.loadedAssetBundles)
            {
                if (bundle.name != "fortified_shaders") continue;

                var shader = bundle.LoadAsset<Shader>("Assets/Shaders/FF_StandardPaintable.shader");
                if (shader != null) return shader;

                return bundle.LoadAllAssets<Shader>().FirstOrDefault();
            }
        }
        return null;
    }
}
