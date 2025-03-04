#if UNITY_IOS
using System;
using UnityEditor;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using UnityEngine;

public class PostProcessODRBundles : IPostprocessBuildWithReport
{
    public int callbackOrder { get { return 10; } }

    private const string InitialInstallTagsKey = "ON_DEMAND_RESOURCES_INITIAL_INSTALL_TAGS";
    private const string PrefetchedTagOrderKey = "ON_DEMAND_RESOURCES_PREFETCH_ORDER";

    public void OnPostprocessBuild(BuildReport report)
    {
        // Initialize PBXProject
        var pathToBuiltProject = report.summary.outputPath;
        string projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        PBXProject pbxProject = new PBXProject();
        pbxProject.ReadFromFile(projectPath);
        
        // Set Prefetch Categories
        string mainTargetGuid = pbxProject.GetUnityMainTargetGuid();
        var categories = BuildScriptPackedModeODR.CollectPrefetchCategories();
        
        // Initial Install Tags
        var initialTags = categories[(int)AppleODRSchema.PrefetchCategory.InitialInstallTags];
        var initialTagsValue = initialTags.Count > 0 ? string.Join(" ", initialTags) : "";
        pbxProject.SetBuildProperty(mainTargetGuid, InitialInstallTagsKey, initialTagsValue);
        
        // Prefetched Tag Order
        var prefetchedTags = categories[(int)AppleODRSchema.PrefetchCategory.PrefetchedTagOrder];
        var prefetchedTagsValue = prefetchedTags.Count > 0 ? string.Join(" ", prefetchedTags) : "";
        pbxProject.SetBuildProperty(mainTargetGuid, PrefetchedTagOrderKey, prefetchedTagsValue);
        
        // Apply changes to the PBXProject
        pbxProject.WriteToFile(projectPath);
    }
}
#endif
