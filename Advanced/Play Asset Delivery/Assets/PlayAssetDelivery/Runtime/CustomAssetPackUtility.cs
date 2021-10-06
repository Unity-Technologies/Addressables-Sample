using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR && UNITY_ANDROID && UNITY_2021_2_OR_NEWER
using Unity.Android.Types;
#endif

namespace AddressablesPlayAssetDelivery
{
    /// <summary>
    /// Serializable representation of 'Unity.Android.Types.AndroidAssetPackDeliveryType'.
    /// </summary>
    public enum DeliveryType
    {
        /// <summary>
        /// No delivery type specified.
        /// </summary>
        None = 0,

        /// <summary>
        /// Content is downloaded when the app is installed.
        /// </summary>
        InstallTime = 1,

        /// <summary>
        /// Content is downloaded automatically as soon as the the app is installed.
        /// </summary>
        FastFollow = 2,

        /// <summary>
        /// Content is downloaded while the app is running.
        /// </summary>
        OnDemand = 3
    }

    public class CustomAssetPackUtility
    {
        public const string kBuildFolderName = "Build";
        public const string kPackContentFolderName = "CustomAssetPackContent";

        public const string kBuildProcessorDataFilename = "BuildProcessorData.json";
        public const string kCustomAssetPackDataFilename = "CustomAssetPacksData.json";

        public static string RootDirectory
        {
            get { return $"Assets/PlayAssetDelivery"; }
        }

        public static string BuildRootDirectory
        {
            get { return $"{RootDirectory}/{kBuildFolderName}"; }
        }

        public static string PackContentRootDirectory
        {
            get { return $"{BuildRootDirectory}/{kPackContentFolderName}"; }
        }

        public static string BuildProcessorDataPath
        {
            get { return Path.Combine(BuildRootDirectory, kBuildProcessorDataFilename); }
        }

        public static string CustomAssetPacksDataEditorPath
        {
            get { return Path.Combine(BuildRootDirectory, kCustomAssetPackDataFilename); }
        }

        public static string CustomAssetPacksDataRuntimePath
        {
            get { return Path.Combine(Application.streamingAssetsPath, kCustomAssetPackDataFilename); }
        }

#if UNITY_EDITOR
        public static void DeleteDirectory(string directoryPath, bool onlyIfEmpty)
        {
            bool isEmpty = !Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories).Any()
                && !Directory.EnumerateDirectories(directoryPath, "*", SearchOption.AllDirectories).Any();
            if (!onlyIfEmpty || isEmpty)
            {
                // check if the folder is valid in the AssetDatabase before deleting through standard file system
                string relativePath = directoryPath.Replace("\\", "/").Replace(Application.dataPath, "Assets");
                if (UnityEditor.AssetDatabase.IsValidFolder(relativePath))
                    UnityEditor.AssetDatabase.DeleteAsset(relativePath);
                else
                    Directory.Delete(directoryPath, true);
            }
        }

#if UNITY_ANDROID && UNITY_2021_2_OR_NEWER
        static readonly Dictionary<DeliveryType, AndroidAssetPackDeliveryType> k_DeliveryTypeToGradleString = new Dictionary<DeliveryType, AndroidAssetPackDeliveryType>()
        {
            { DeliveryType.InstallTime, AndroidAssetPackDeliveryType.InstallTime },
            { DeliveryType.FastFollow, AndroidAssetPackDeliveryType.FastFollow },
            { DeliveryType.OnDemand, AndroidAssetPackDeliveryType.OnDemand },
        };

        public static string DeliveryTypeToGradleString(DeliveryType deliveryType)
        {
            return k_DeliveryTypeToGradleString[deliveryType].Name;
        }

#else
        static readonly Dictionary<DeliveryType, string> k_DeliveryTypeToGradleString = new Dictionary<DeliveryType, string>()
        {
            { DeliveryType.InstallTime, "install-time" },
            { DeliveryType.FastFollow, "fast-follow" },
            { DeliveryType.OnDemand, "on-demand" },
        };

        public static string DeliveryTypeToGradleString(DeliveryType deliveryType)
        {
            return k_DeliveryTypeToGradleString[deliveryType];
        }

#endif
#endif
    }
}
