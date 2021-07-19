using System;
using System.Collections;
using System.IO;
using PlayAssetDelivery.Editor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Android;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace PlayAssetDelivery
{
    /// <summary>
    /// Set up Addressables to locate content in asset packs.
    /// </summary>
    public class InitializeAddressables : MonoBehaviour
    {
        /// <summary>
        /// Set to true once all core Unity asset packs are downloaded and we have finished configuring our custom Addressables properties. 
        /// </summary>
        public bool hasInitialized = false;

        void Start()
        {
#if UNITY_ANDROID && !UNITY_EDITOR  
            // Download the core Unity asset packs if needed. Install-time asset packs should be already installed.
            if (AndroidAssetPacks.coreUnityAssetPacksDownloaded)              
                Setup();
            else
            {
                string[] coreUnityAssetPackNames = AndroidAssetPacks.GetCoreUnityAssetPackNames(); // only returns names of asset packs that are fast-follow or on-demand delivery
                if (coreUnityAssetPackNames.Length == 0)
                    Debug.LogError("Cannot retrieve core Unity asset pack names. PlayCore Plugin is not installed.");
                else
                    AndroidAssetPacks.DownloadAssetPackAsync(coreUnityAssetPackNames, CheckDownloadStatus);
            }
#else
            Setup();
#endif
        }

        void Setup()
        {
            Addressables.ResourceManager.ResourceProviders.Add(new PlayAssetDeliveryAssetBundleProvider());
            Addressables.ResourceManager.InternalIdTransformFunc = AssetPackTransformFunc;
            hasInitialized = true;
        }

        void CheckDownloadStatus(AndroidAssetPackInfo info)
        {
            if (info.status == AndroidAssetPackStatus.Failed)
                Debug.LogError($"Failed to retrieve the state of asset pack '{info.name}'."); 
            else if (info.status == AndroidAssetPackStatus.Unknown)
                Debug.LogError($"Asset pack '{info.name}' is unavailable for this application. This can occur if the app was not installed through Google Play.");
            else if (info.status == AndroidAssetPackStatus.Canceled)
                Debug.LogError($"Cancelled asset pack download request '{info.name}'.");
            else if (info.status == AndroidAssetPackStatus.WaitingForWifi)
            {
                AndroidAssetPacks.RequestToUseMobileDataAsync(result =>
                {
                    if (!result.allowed)
                        Debug.LogError("Request to use mobile data was denied.");
                });
            }
            else if(info.status == AndroidAssetPackStatus.Completed)
            {
                string assetPackPath = AndroidAssetPacks.GetAssetPackPath(info.name);
                if (string.IsNullOrEmpty(assetPackPath))
                {
                    Debug.LogError($"Downloaded asset pack '{info.name}' but cannot locate it on device.");
                }
            }
        }
    
        string AssetPackTransformFunc(IResourceLocation location)
        {

#if UNITY_ANDROID && !UNITY_EDITOR
            string filename = Path.GetFileName(location.InternalId);

            if (location.ResourceType == typeof(IAssetBundleResource))
            {
                string assetPackName = Path.GetFileNameWithoutExtension(location.InternalId);
                if (PlayerPrefs.HasKey(assetPackName))
                {
                    // Load bundle from a custom asset pack. PlayAssetDeliveryBundleProvider.Provider previously stored its path.
                    return PlayerPrefs.GetString(assetPackName);
                }
            }

            string streamingAssetsPackPath = AndroidAssetPacks.GetAssetPackPath("UnityStreamingAssetsPack");
            if (!string.IsNullOrEmpty(streamingAssetsPackPath))
            {
                // Load resource from a fast-follow or on-demand 'UnityStreamingAssetsPack'.
                return Path.Combine(streamingAssetsPackPath, filename);
            }
            
            // Load resource that was assigned to an install-time core Unity asset pack. 
            // The pack's contents should be installed in the 'aa/Android' folder in the APK's StreamingAssets path.
            return location.InternalId;
#else
            // Load from the 'Assets/PlayAssetDelivery/AndroidAssetsPacks' folder
            if (location.ResourceType == typeof(IAssetBundleResource))
            {
                string assetPackName = Path.GetFileNameWithoutExtension(location.InternalId);
                string androidPackFolder = $"Assets/PlayAssetDelivery/AndroidAssetPacks/{assetPackName}.androidpack";
                string bundlePath = Path.Combine(androidPackFolder, Path.GetFileName(location.InternalId));
                if (File.Exists(bundlePath))
                    return bundlePath;
            }

            // Load from the 'Library/com.unity.addressables' folder
            return location.InternalId;
#endif
        }
    }
}
