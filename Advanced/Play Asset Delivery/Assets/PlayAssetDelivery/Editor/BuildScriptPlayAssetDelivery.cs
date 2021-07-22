#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;

namespace AddressablesPlayAssetDelivery.Editor
{
    /// <summary>
    /// In addition to the Default Build Script behavior (building AssetBundles), this script creates prepares bundled content for custom asset pack creation
    /// https://docs.unity3d.com/Manual/play-asset-delivery.html#custom-asset-packs. 
    /// 
    /// At build time the AddressablesAssetPostProcessor will temporarily move all built content to 'Assets/StreamingAssets'.
    /// This means that any Addressables content will be automatically be included in the streaming assets pack even if they are not assigned to a custom asset pack. 
    /// 
    /// We also assign any content marked for install-time delivery to the streaming assets pack. In most cases the streaming assets pack will use "install-time" delivery, 
    /// but in large projects it may use "fast-follow" delivery instead. For more information see https://docs.unity3d.com/Manual/play-asset-delivery.html#generated-asset-packs.
    ///  
    /// For other delivery types bundles will be moved into a {asset pack name}.androidpack folder located in 'Assets/PlayAssetDelivery/CustomAssetPackContent'.
    /// A 'build.gradle' file will also be created in each .androidpack folder unless the delivery type is set to "none". 
    /// In this case Unity automatically assumes that any asset packs without a 'build.gradle' file use "on-demand" delivery.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildScriptPlayAssetDelivery.asset", menuName = "Addressables/Custom Build/Play Asset Delivery")]
    public class BuildScriptPlayAssetDelivery : BuildScriptPackedMode
    {
        /// <inheritdoc/>
        public override string Name
        {
            get { return "Play Asset Delivery"; }
        }

        static string m_AssetPackFolderName = "CustomAssetPackContent";
        static string m_RootDirectory = "Assets/PlayAssetDelivery";
        static string PackContentRootDirectory
        {
            get { return $"{m_RootDirectory}/{m_AssetPackFolderName}"; }
        }

        protected override TResult DoBuild<TResult>(AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext)
        {
            TResult result = base.DoBuild<TResult>(builderInput, aaContext);
            AddressableAssetSettings settings = aaContext.Settings;
            var assetPackToDataEntry = new Dictionary<string, CustomAssetPackDataEntry>();

            // Reset schema data if the settings file was deleted
            bool resetSchemaData = !CustomAssetPackSettings.SettingsExists ? true : false;

            List<CustomAssetPackEditorInfo> customAssetPacks = CustomAssetPackSettings.GetSettings().CustomAssetPacks;
            
            // Clear out the 'Assets/PlayAssetDelivery/CustomAssetPackContent' directory
            if(Directory.Exists(PackContentRootDirectory))
                AssetDatabase.DeleteAsset(PackContentRootDirectory);
            AssetDatabase.CreateFolder(m_RootDirectory, m_AssetPackFolderName);

            // Create config files for all custom asset packs
            foreach(CustomAssetPackEditorInfo assetPack in customAssetPacks)
            {
                if (assetPack.AssetPackName != CustomAssetPackSettings.k_InstallTimePackName)
                {
                    assetPackToDataEntry[assetPack.AssetPackName] = new CustomAssetPackDataEntry(assetPack.AssetPackName, assetPack.DeliveryType, new List<string>());
                    string androidPackDir = CreateAndroidPackDirectory(assetPack.AssetPackName);
                    CreateGradleFile(androidPackDir, assetPack.AssetPackName, assetPack.DeliveryType);
                }
            }

            // Move bundle files to the 'Assets/PlayAssetDelivery/CustomAssetPackContent' directory
            foreach(AddressableAssetGroup group in settings.groups)
            {
                if (!group.HasSchema<PlayAssetDeliverySchema>())
                {
                    // If creating an Android App Bundle, any Addressables content will be automatically assigned to the streaming assets pack. 
                    continue;
                }
                if (!group.HasSchema<BundledAssetGroupSchema>())
                {
                    Addressables.LogWarning($"Group '{group.name}' has a '{typeof(PlayAssetDeliverySchema).Name}' but not a '{typeof(BundledAssetGroupSchema).Name}'. " +
                        $"It does not contain any content to be placed into an asset pack.");
                    continue;
                }

                var assetPackSchema = group.GetSchema<PlayAssetDeliverySchema>();
                if(resetSchemaData)
                    assetPackSchema.Reset();

                CustomAssetPackEditorInfo assetPack = customAssetPacks[assetPackSchema.AssetPackIndex];
                if (assetPack.DeliveryType == DeliveryType.InstallTime)
                {
                    // We expect any install-time content to be assigned to the streaming assets pack.
                    continue;
                }
                ProcessGroup(group, assetPack.AssetPackName, assetPackToDataEntry);
            }
            
            // Create the CustomAssetPacksData.json file. It contains all custom asset pack information that can be used at runtime.
            SerializeCustomAssetPacksData(assetPackToDataEntry.Values.ToList());
            return result;
        }
        
        string ConstructAndroidPackDirectoryName(string assetPackName)
        {
            return $"{assetPackName}.androidpack";
        }

        string CreateAndroidPackDirectory(string assetPackName)
        {
            string folderName = ConstructAndroidPackDirectoryName(assetPackName);
            string androidPackFolder = Path.Combine(PackContentRootDirectory, folderName).Replace("\\", "/");
            AssetDatabase.CreateFolder(PackContentRootDirectory, folderName);
            return androidPackFolder;
        }

        void ProcessGroup(AddressableAssetGroup group, string assetPackName, Dictionary<string, CustomAssetPackDataEntry> assetPackToDataEntry)
        {
            foreach (AddressableAssetEntry entry in group.entries)
            {
                string bundleBuildPath = AddressablesRuntimeProperties.EvaluateString(entry.BundleFileId);
                string bundleName = Path.GetFileNameWithoutExtension(bundleBuildPath);
                string bundlePackDir = Path.Combine(PackContentRootDirectory, ConstructAndroidPackDirectoryName(assetPackName));

                // Save bundle to asset pack data
                assetPackToDataEntry[assetPackName].AssetBundles.Add(bundleName);

                // Move bundle to the appropriate .androidpack folder
                string assetsFolderPath = Path.Combine(bundlePackDir, Path.GetFileName(bundleBuildPath));
                File.Move(bundleBuildPath, assetsFolderPath);
            }
        }

        void CreateGradleFile(string androidPackDir, string assetPackName, DeliveryType deliveryType)
        {
            if(deliveryType == DeliveryType.None)
            {
                Addressables.LogWarning($"Asset pack '{assetPackName}' has its delivery type set to 'None'. " +
                $"No gradle file will be created for this asset pack. Unity assumes that any custom asset packs with no gradle file use on-demand delivery.");
                return;
            }

            // Warn about other gradle files in the .androidpack directory
            List<string> gradleFiles = Directory.GetFiles(androidPackDir, "*.gradle").Where(x => Path.GetFileName(x) != "build.gradle").ToList();
            if (gradleFiles.Count > 0)
            {
                Addressables.LogWarning($"Custom asset pack at '{androidPackDir}' contains {gradleFiles.Count} files with .gradle extension which will be ignored. " +
                    $"Only the 'build.gradle' file will be included in the Android App Bundle.");
            }
            
            // Create the 'build.gradle' file in the .androidpack directory
            string deliveryTypeString = CustomAssetPackUtility.DeliveryTypeToGradleString(deliveryType);
            string buildFilePath = Path.Combine(androidPackDir, "build.gradle");
            string content = $"apply plugin: 'com.android.asset-pack'\n\nassetPack {{\n\tpackName = \"{assetPackName}\"\n\tdynamicDelivery {{\n\t\tdeliveryType = \"{deliveryTypeString}\"\n\t}}\n}}";
            File.WriteAllText(buildFilePath, content);
        }
        
        void SerializeCustomAssetPacksData(List<CustomAssetPackDataEntry> entries)
        {
            var customPackData = new CustomAssetPackData(entries);
            string contents = JsonUtility.ToJson(customPackData);

            if (!Directory.Exists(Application.streamingAssetsPath))
                Directory.CreateDirectory(Application.streamingAssetsPath);
            string path = Path.Combine(Application.streamingAssetsPath, "CustomAssetPacksData.json");
            File.WriteAllText(path, contents);
        }
    }
}
#endif