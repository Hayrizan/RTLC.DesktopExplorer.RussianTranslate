using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BuildLocalizationBundle
{
    public static void Build()
    {
        const string root = "Assets/RTLC/";
        string[] assets = AssetDatabase.GetAllAssetPaths()
            .Where(path => path.StartsWith(root, StringComparison.Ordinal) && !AssetDatabase.IsValidFolder(path))
            .OrderBy(path => path, StringComparer.Ordinal).ToArray();
        if (assets.Length == 0) throw new InvalidOperationException("No localization resources staged.");
        string output = Path.GetFullPath("BundleOutput");
        Directory.CreateDirectory(output);
        var build = new AssetBundleBuild {
            assetBundleName = "rtlc.bundle",
            assetNames = assets,
            addressableNames = assets.Select(path => path.Substring(root.Length)
                .Replace(".bytes", "").ToLowerInvariant()).ToArray()
        };
        if (BuildPipeline.BuildAssetBundles(output, new[] { build },
            BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode | BuildAssetBundleOptions.ForceRebuildAssetBundle,
            BuildTarget.StandaloneWindows64) == null)
            throw new InvalidOperationException("AssetBundle build failed.");
        Debug.Log("RTLC bundle resources: " + assets.Length);
    }
}
