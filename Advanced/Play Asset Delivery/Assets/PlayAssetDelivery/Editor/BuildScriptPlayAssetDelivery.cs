#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Android.Types;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;

namespace PlayAssetDelivery.Editor
{
    /// <summary>
    /// In addition to the Default Build Script behavior (building AssetBundles), this script creates prepares bundled content for asset pack creation.
    /// When building an Android App Bundle, any content in a folder with a .androidpack extension (located anywhere in the Assets folder) will be placed into an asset pack. 
    /// The .androidpack folder can contain a gradle file, specifically named 'build.gradle' to specify the asset pack settings (i.e. delivery type).
    /// 
    /// This build script uses the bundle filename as the asset pack name. Each bundle will be moved into a {bundle filename}.androidpack folder located in 'Assets/AndroidAssetPacks'.
    /// A 'build.gradle' file will also be created in each {bundle filename}.androidpack folder.
    /// 
    /// If you want to manually create your own .androidpack folders or 'build.gradle' files, use the Default Build Script instead. 
    /// </summary>
    [CreateAssetMenu(fileName = "BuildScriptPlayAssetDelivery.asset", menuName = "Addressables/Custom Build/Play Asset Delivery")]
    public class BuildScriptPlayAssetDelivery : BuildScriptPackedMode
    {
        /// <inheritdoc/>
        public override string Name
        {
            get { return "Play Asset Delivery"; }
        }

        static string m_AssetPackFolderName = "AndroidAssetPacks";
        static string m_RootDirectory = "Assets/PlayAssetDelivery";
        static string AssetPackRootDirectory
        {
            get { return $"{m_RootDirectory}/{m_AssetPackFolderName}"; }
        }

        static readonly Dictionary<DeliveryType, AndroidAssetPackDeliveryType> k_DeliveryTypeToString = new Dictionary<DeliveryType, AndroidAssetPackDeliveryType>()
        {
            { DeliveryType.InstallTime, AndroidAssetPackDeliveryType.InstallTime },
            { DeliveryType.FastFollow, AndroidAssetPackDeliveryType.FastFollow },
            { DeliveryType.OnDemand, AndroidAssetPackDeliveryType.OnDemand },
        };

        protected override TResult DoBuild<TResult>(AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext)
        {
            TResult result = base.DoBuild<TResult>(builderInput, aaContext);

            var bundlefileId = new HashSet<string>();
            AddressableAssetSettings settings = aaContext.Settings;
        
            // Clear out the 'Assets/AndroidAssetPacks' directory
            if(Directory.Exists(AssetPackRootDirectory))
                AssetDatabase.DeleteAsset(AssetPackRootDirectory);
            AssetDatabase.CreateFolder(m_RootDirectory, m_AssetPackFolderName);

            // Move bundle files to the 'Assets/AndroidAssetPacks' directory
            foreach(AddressableAssetGroup group in settings.groups)
            {
                if (!group.HasSchema<PlayAssetDeliverySchema>())
                    continue;

                var assetPackSchema = group.GetSchema<PlayAssetDeliverySchema>();
                string errorString = ValidateAssetPackGroupSchema(assetPackSchema, group);
                if (!string.IsNullOrEmpty(errorString))
                {
                    Addressables.LogWarning(errorString);
                    continue;
                }

                foreach (var entry in group.entries)
                {
                    if (bundlefileId.Contains(entry.BundleFileId))
                        continue;

                    string bundleBuildPath = AddressablesRuntimeProperties.EvaluateString(entry.BundleFileId);
                    string assetPackName = Path.GetFileNameWithoutExtension(bundleBuildPath);
                    string bundlePackDir = CreateAndroidPackDirectory(assetPackName, assetPackSchema.DeliveryType);

                    // Move bundle to the created .androidpack folder
                    string bundleFileName = Path.GetFileName(bundleBuildPath);
                    string assetsFolderPath = Path.Combine(bundlePackDir, bundleFileName).Replace("\\", "/");
                    File.Move(bundleBuildPath, assetsFolderPath);

                    bundlefileId.Add(entry.BundleFileId);
                }
            }

            return result;
        }

        string ValidateAssetPackGroupSchema(PlayAssetDeliverySchema schema, AddressableAssetGroup assetGroup)
        {
            if(schema.DeliveryType == DeliveryType.None)
            {
                return $"Group '{assetGroup.name}' has a '{typeof(PlayAssetDeliverySchema).Name}' but the Delivery type is set to 'None'. " +
                    $"No asset packs will be configured for this group.";
            }
            if (!assetGroup.HasSchema<BundledAssetGroupSchema>())
            {
                return $"Group '{assetGroup.name}' has a '{typeof(PlayAssetDeliverySchema).Name}' but not a '{typeof(BundledAssetGroupSchema).Name}'. " +
                    $"No asset packs will be configured for this group";
            }
            var bundledAssetSchema = assetGroup.GetSchema<BundledAssetGroupSchema>();
            if(bundledAssetSchema.BundleNaming == BundledAssetGroupSchema.BundleNamingStyle.FileNameHash ||
                bundledAssetSchema.BundleNaming == BundledAssetGroupSchema.BundleNamingStyle.OnlyHash)
                return $"Group '{assetGroup.name}' Bundle Naming Mode cannot be just a hash. Asset pack naming conventions require the first character to be a letter. " +
                    $"Please use a different Bundle Naming Mode. No asset packs will be configured for this group.";
            return string.Empty;
        }

        string CreateAndroidPackDirectory(string assetPackName, DeliveryType deliveryType)
        {
            Regex validAssetPackName = new Regex(@"^[A-Za-z][a-zA-Z0-9_]*$", RegexOptions.Compiled);
            if (!validAssetPackName.IsMatch(assetPackName))
            {
                Addressables.LogWarning($"Custom asset pack has an invalid name '{assetPackName}'. All characters in the asset pack name must be alphanumeric or an underscore. " +
                    $"Also the first character must be a letter. No gradle file will be created for this asset pack.");
                return "";
            }
        
            // Create the .androidpack directory
            string folderName = $"{assetPackName}.androidpack";
            string androidPackFolder = Path.Combine(AssetPackRootDirectory, folderName).Replace("\\", "/");
            AssetDatabase.CreateFolder(AssetPackRootDirectory, folderName);

            // Warn about are other gradle files in the .androidpack directory
            List<string> gradleFiles = Directory.GetFiles(androidPackFolder, "*.gradle").Where(x => Path.GetFileName(x) != "build.gradle").ToList();
            if (gradleFiles.Count > 0)
                Addressables.LogWarning($"Custom asset pack at '{androidPackFolder}' contains {gradleFiles.Count} files with .gradle extension which will be ignored. " +
                    $"Only the 'build.gradle' file will be included in the Android App Bundle.");
        
            // Create the 'build.gradle' file in the .androidpack directory
            string deliveryTypeString = k_DeliveryTypeToString[deliveryType].Name;
            string buildFilePath = Path.Combine(androidPackFolder, "build.gradle");
            string content = $"apply plugin: 'com.android.asset-pack'\n\nassetPack {{\n\tpackName = \"{assetPackName}\"\n\tdynamicDelivery {{\n\t\tdeliveryType = \"{deliveryTypeString}\"\n\t}}\n}}";
            File.WriteAllText(buildFilePath, content);

            return androidPackFolder;
        }
    }
}
#endif