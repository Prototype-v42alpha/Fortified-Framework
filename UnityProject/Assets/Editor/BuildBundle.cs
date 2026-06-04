using UnityEditor;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

public class BuildBundle
{
    private static string OutputPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "1.6", "AssetBundles"));
    private const string BundleName = "fortified_shaders";
    private static readonly PlatformBundle[] PlatformBundles =
    {
        new PlatformBundle(BuildTarget.StandaloneWindows64, "_win"),
        new PlatformBundle(BuildTarget.StandaloneLinux64, "_linux"),
        new PlatformBundle(BuildTarget.StandaloneOSX, "_mac")
    };

    private struct PlatformBundle
    {
        public readonly BuildTarget target;
        public readonly string suffix;

        public PlatformBundle(BuildTarget target, string suffix)
        {
            this.target = target;
            this.suffix = suffix;
        }
    }

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
            PrepareOutput(outputPath);

            AssetDatabase.Refresh();
            List<string> assets = FindShaderAssets();

            if (assets.Count == 0)
            {
                Debug.LogError("[Fortified] Build Error: No shader assets found in Assets/Shaders!");
                return;
            }

            int builtCount = 0;
            foreach (var platform in PlatformBundles)
            {
                if (BuildPlatformBundle(outputPath, assets, platform)) builtCount++;
            }

            if (builtCount == 0)
            {
                Debug.LogError($"[Fortified] AssetBundle '{BundleName}' 构建失败");
                return;
            }

            Debug.Log($"[Fortified] AssetBundle '{BundleName}' 构建完成！平台数 {builtCount}。输出至: {outputPath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Fortified] FATAL BUILD ERROR: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private static List<string> FindShaderAssets()
    {
        List<string> assets = new List<string>();
        string shaderDir = "Assets/Shaders";
        if (!Directory.Exists(shaderDir)) return assets;

        foreach (string file in Directory.GetFiles(shaderDir, "*.shader", SearchOption.AllDirectories))
        {
            string assetPath = file.Replace("\\", "/");
            assets.Add(assetPath);
            Debug.Log($"[Fortified] Found asset: {assetPath}");
        }
        return assets;
    }

    private static void PrepareOutput(string outputPath)
    {
        if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);

        DeleteIfExists(Path.Combine(outputPath, BundleName));
        DeleteIfExists(Path.Combine(outputPath, BundleName + ".manifest"));
        DeleteIfExists(Path.Combine(outputPath, "AssetBundles"));
        DeleteIfExists(Path.Combine(outputPath, "AssetBundles.manifest"));
        foreach (var platform in PlatformBundles)
        {
            DeleteIfExists(Path.Combine(outputPath, BundleName + platform.suffix));
            DeleteIfExists(Path.Combine(outputPath, BundleName + platform.suffix + ".manifest"));
        }
    }

    private static bool BuildPlatformBundle(string outputPath, List<string> assets, PlatformBundle platform)
    {
        string bundleName = BundleName + platform.suffix;
        string tempPath = Path.Combine(Path.GetTempPath(), "FortifiedAssetBundles", platform.target.ToString());

        if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
        Directory.CreateDirectory(tempPath);

        AssetBundleBuild[] builds = new AssetBundleBuild[1];
        builds[0].assetBundleName = bundleName;
        builds[0].assetNames = assets.ToArray();

        var manifest = BuildPipeline.BuildAssetBundles(tempPath, builds,
            BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
            platform.target);

        if (manifest == null)
        {
            Debug.LogError($"[Fortified] AssetBundle build failed for {platform.target}");
            return false;
        }

        CopyBundleFile(tempPath, outputPath, bundleName);
        Directory.Delete(tempPath, true);
        return true;
    }

    private static void CopyBundleFile(string tempPath, string outputPath, string bundleName)
    {
        string sourceBundle = Path.Combine(tempPath, bundleName);
        string targetBundle = Path.Combine(outputPath, bundleName);
        string sourceManifest = sourceBundle + ".manifest";
        string targetManifest = targetBundle + ".manifest";

        File.Copy(sourceBundle, targetBundle, true);
        if (File.Exists(sourceManifest)) File.Copy(sourceManifest, targetManifest, true);
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }
}
