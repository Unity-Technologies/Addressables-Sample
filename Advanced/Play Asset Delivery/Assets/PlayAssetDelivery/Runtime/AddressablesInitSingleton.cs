using System.Collections;
using System.Collections.Generic;
using System.IO;
using AddressablesPlayAssetDelivery.Editor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Android;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

namespace AddressablesPlayAssetDelivery
{
    /// <summary>
    /// Set up Addressables to locate content in asset packs.
    /// </summary>
    public class AddressablesInitSingleton : ComponentSingleton<AddressablesInitSingleton>
    {
        /// <summary>
        /// Set to true once all core Unity asset packs are downloaded, configured the custom Addressables properties, and loaded all custom asset packs information.
        /// </summary>
        bool m_HasInitialized = false;
        public bool HasInitialized
        {
            get { return m_HasInitialized; }
            internal set { m_HasInitialized = value; }
        }

        /// <summary>
        /// Maps an asset bundle name to the name of its assigned asset pack.
        /// </summary>
        Dictionary<string, CustomAssetPackDataEntry> m_BundleNameToAssetPack = new Dictionary<string, CustomAssetPackDataEntry>();
        public Dictionary<string, CustomAssetPackDataEntry> BundleNameToAssetPack
        {
            get { return m_BundleNameToAssetPack; }
        }

        Dictionary<string, string> m_AssetPackNameToRemotePath = new Dictionary<string, string>();
        public Dictionary<string, string> AssetPackNameToRemotePath
        {
            get { return m_AssetPackNameToRemotePath; }
        }

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
            StartCoroutine(LoadCustomAssetPacksData());
        }

        IEnumerator LoadCustomAssetPacksData()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "CustomAssetPacksData.json");
            UnityWebRequest www = UnityWebRequest.Get(path);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Could not load 'CustomAssetPacksData.json' : {www.error}");
            }
            else
            {
                string contents = www.downloadHandler.text;
                CustomAssetPackData customPackData =  JsonUtility.FromJson<CustomAssetPackData>(contents);
                foreach (CustomAssetPackDataEntry entry in customPackData.Entries)
                {
                    foreach (string bundle in entry.AssetBundles)
                    {
                        BundleNameToAssetPack.Add(bundle, entry);
                    }
                }
                HasInitialized = true;
            }
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
            else if (info.status == AndroidAssetPackStatus.Completed)
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
            if (location.ResourceType == typeof(IAssetBundleResource))
            {
                string bundleName = Path.GetFileNameWithoutExtension(location.InternalId);
                if (BundleNameToAssetPack.ContainsKey(bundleName))
                {
                    string assetPackName = BundleNameToAssetPack[bundleName].AssetPackName;
                    if (AssetPackNameToRemotePath.ContainsKey(assetPackName))
                    {
                        // Load bundle that was assigned to a custom fast-follow or on-demand asset pack.
                        // PlayAssetDeliveryBundleProvider.Provider previously saved the asset pack path.
                        return Path.Combine(AssetPackNameToRemotePath[assetPackName], Path.GetFileName(location.InternalId));
                    }
                }
            }

            // Load resource from the default location. If the resource was assigned to the streaming assets pack, the default internal id
            // already points to 'Application.streamingAssetsPath'.
            return location.InternalId;
#else
            // Load bundle from the 'Assets/PlayAssetDelivery/CustomAssetPackContent' folder.
            // Only "fast-follow" or "on-demand custom" asset pack content will be located here.
            if (location.ResourceType == typeof(IAssetBundleResource))
            {
                string bundleName = Path.GetFileNameWithoutExtension(location.InternalId);
                if (BundleNameToAssetPack.ContainsKey(bundleName))
                {
                    string assetPackName = BundleNameToAssetPack[bundleName].AssetPackName;
                    string androidPackFolder = $"Assets/PlayAssetDelivery/CustomAssetPackContent/{assetPackName}.androidpack";
                    string bundlePath = Path.Combine(androidPackFolder, Path.GetFileName(location.InternalId));
                    if (File.Exists(bundlePath))
                        return bundlePath;
                }
            }

            // Load resource from the default location.
            return location.InternalId;
#endif
        }
    }
}
