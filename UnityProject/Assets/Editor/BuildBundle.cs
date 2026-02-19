using UnityEditor;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

public class BuildBundle
{
    private static string OutputPath = "../1.6/AssetBundles";

    [MenuItem("Assets/Build Fortified AssetBundle")]
    public static void BuildAllAssetBundles()
    {
        Build(OutputPath);
    }

    public static void BuildFromCommandLine()
    {
        Build(OutputPath);
    }

    private static void Build(string outputPath)
    {
        try
        {
            if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);

            AssetDatabase.Refresh();

            // 显式扫描 Shader 资产
            List<string> assets = new List<string>();
            string shaderDir = "Assets/Shaders";
            if (Directory.Exists(shaderDir))
            {
                foreach (string file in Directory.GetFiles(shaderDir, "*.shader", SearchOption.AllDirectories))
                {
                    string assetPath = file.Replace("\\", "/");
                    assets.Add(assetPath);
                    Debug.Log($"[Fortified] Found asset: {assetPath}");
                }
            }

            if (assets.Count == 0)
            {
                Debug.LogError("[Fortified] Build Error: No shader assets found in Assets/Shaders!");
                return;
            }

            AssetBundleBuild[] builds = new AssetBundleBuild[1];
            builds[0].assetBundleName = "fortified_shaders";
            builds[0].assetNames = assets.ToArray();

            var manifest = BuildPipeline.BuildAssetBundles(outputPath, builds,
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
                BuildTarget.StandaloneWindows64);

            if (manifest == null)
            {
                Debug.LogError("[Fortified] AssetBundle build failed! Check console for shader compilation errors.");
            }
            else
            {
                Debug.Log($"[Fortified] AssetBundle 'fortified_visuals' 构建完成！包含 {assets.Count} 个资源。输出至: {outputPath}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Fortified] FATAL BUILD ERROR: {ex.Message}\n{ex.StackTrace}");
        }
    }
}