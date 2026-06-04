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
    private const string PaintBundleName = "fortified_shaders";

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
        Shader fallback = null;
        foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading)
        {
            if (!mod.PackageId.ToLower().Contains("aoba.framework")) continue;
            if (mod.assetBundles?.loadedAssetBundles == null) continue;

            foreach (AssetBundle bundle in mod.assetBundles.loadedAssetBundles)
            {
                if (!IsPaintBundle(bundle)) continue;

                var shader = LoadShader(bundle);
                if (shader == null) continue;

                if (bundle.name == PaintBundleName + CurrentBundleSuffix()) return shader;
                fallback ??= shader;
            }
        }
        return fallback;
    }

    private static bool IsPaintBundle(AssetBundle bundle)
    {
        if (bundle == null) return false;

        string name = bundle.name;
        return name == PaintBundleName || name == PaintBundleName + CurrentBundleSuffix();
    }

    private static Shader LoadShader(AssetBundle bundle)
    {
        var shader = bundle.LoadAsset<Shader>("Assets/Shaders/FF_StandardPaintable.shader");
        return shader ?? bundle.LoadAllAssets<Shader>().FirstOrDefault();
    }

    private static string CurrentBundleSuffix()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.LinuxPlayer:
            case RuntimePlatform.LinuxEditor:
                return "_linux";
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.OSXEditor:
                return "_mac";
            default:
                return "_win";
        }
    }
}
