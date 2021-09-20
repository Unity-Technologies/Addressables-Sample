using System.Collections;
using System.Collections.Generic;
using System.IO;
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

        [Tooltip("Show warnings that occur when initializing the singleton.")]
        public bool logInitializationWarnings = true;

        void Start()
        {
            Addressables.ResourceManager.ResourceProviders.Add(new PlayAssetDeliveryAssetBundleProvider());
#if UNITY_ANDROID && !UNITY_EDITOR
            LoadFromAssetPacksIfAvailable();
#elif UNITY_ANDROID && UNITY_EDITOR
            LoadFromEditorData();
#else
            HasInitialized = true;
#endif
        }

        void LoadFromAssetPacksIfAvailable()
        {
            if (AndroidAssetPacks.coreUnityAssetPacksDownloaded)
            {
                // Core Unity asset packs use install-time delivery and are already installed.
                StartCoroutine(DownloadCustomAssetPacksData());
            }
            else
            {
                // Core Unity asset packs use fast-follow or on-demand delivery and need to be downloaded.
                string[] coreUnityAssetPackNames = AndroidAssetPacks.GetCoreUnityAssetPackNames(); // only returns names of asset packs that are fast-follow or on-demand delivery
                if (coreUnityAssetPackNames.Length == 0)
                    LogWarning("Cannot retrieve core Unity asset pack names. PlayCore Plugin is not installed.", true);
                else
                    AndroidAssetPacks.DownloadAssetPackAsync(coreUnityAssetPackNames, CheckDownloadStatus);
            }
        }

        void LoadFromEditorData()
        {
            if (File.Exists(CustomAssetPackUtility.CustomAssetPacksDataEditorPath))
            {
                InitializeBundleToAssetPackMap(File.ReadAllText(CustomAssetPackUtility.CustomAssetPacksDataEditorPath));
                Addressables.ResourceManager.InternalIdTransformFunc = EditorTransformFunc;
            }
            else if (File.Exists(CustomAssetPackUtility.CustomAssetPacksDataRuntimePath))
            {
                InitializeBundleToAssetPackMap(File.ReadAllText(CustomAssetPackUtility.CustomAssetPacksDataRuntimePath));
                Addressables.ResourceManager.InternalIdTransformFunc = EditorTransformFunc;
            }
            HasInitialized = true;
        }

        void LogWarning(string message, bool finishInitializing)
        {
            if (logInitializationWarnings)
                Debug.LogWarning($"{message} Default internal id locations will be used instead.");
            HasInitialized = finishInitializing;
        }

        IEnumerator DownloadCustomAssetPacksData()
        {
            UnityWebRequest www = UnityWebRequest.Get(CustomAssetPackUtility.CustomAssetPacksDataRuntimePath);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
                LogWarning($"Could not load 'CustomAssetPacksData.json' : {www.error}.", true);
            else
            {
                InitializeBundleToAssetPackMap(www.downloadHandler.text);
                Addressables.ResourceManager.InternalIdTransformFunc = AppBundleTransformFunc;
                HasInitialized = true;
            }
        }

        void InitializeBundleToAssetPackMap(string contents)
        {
            CustomAssetPackData customPackData =  JsonUtility.FromJson<CustomAssetPackData>(contents);
            foreach (CustomAssetPackDataEntry entry in customPackData.Entries)
            {
                foreach (string bundle in entry.AssetBundles)
                {
                    BundleNameToAssetPack.Add(bundle, entry);
                }
            }
        }

        void CheckDownloadStatus(AndroidAssetPackInfo info)
        {
            if (info.status == AndroidAssetPackStatus.Failed)
                LogWarning($"Failed to retrieve the state of asset pack '{info.name}'.", true);
            else if (info.status == AndroidAssetPackStatus.Unknown)
                LogWarning($"Asset pack '{info.name}' is unavailable for this application. This can occur if the app was not installed through Google Play.", true);
            else if (info.status == AndroidAssetPackStatus.Canceled)
                LogWarning($"Cancelled asset pack download request '{info.name}'.", true);
            else if (info.status == AndroidAssetPackStatus.WaitingForWifi)
            {
                AndroidAssetPacks.RequestToUseMobileDataAsync(result =>
                {
                    if (!result.allowed)
                        LogWarning("Request to use mobile data was denied.", true);
                });
            }
            else if (info.status == AndroidAssetPackStatus.Completed)
            {
                string assetPackPath = AndroidAssetPacks.GetAssetPackPath(info.name);
                if (string.IsNullOrEmpty(assetPackPath))
                    LogWarning($"Downloaded asset pack '{info.name}' but cannot locate it on device.", true);
                else if (AndroidAssetPacks.coreUnityAssetPacksDownloaded)
                    StartCoroutine(DownloadCustomAssetPacksData());
            }
        }

        string AppBundleTransformFunc(IResourceLocation location)
        {
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
            // Load resource from the default location. The generated asset packs contain streaming assets.
            return location.InternalId;
        }

        string EditorTransformFunc(IResourceLocation location)
        {
            if (location.ResourceType == typeof(IAssetBundleResource))
            {
                string bundleName = Path.GetFileNameWithoutExtension(location.InternalId);
                if (BundleNameToAssetPack.ContainsKey(bundleName))
                {
                    string assetPackName = BundleNameToAssetPack[bundleName].AssetPackName;
                    string androidPackFolder = $"{CustomAssetPackUtility.PackContentRootDirectory}/{assetPackName}.androidpack";
                    string bundlePath = Path.Combine(androidPackFolder, Path.GetFileName(location.InternalId));
                    if (File.Exists(bundlePath))
                    {
                        // Load bundle from the 'Assets/PlayAssetDelivery/Build/CustomAssetPackContent' folder.
                        // The PlayAssetDeliveryBuildProcessor moves bundles assigned to "fast-follow" or "on-demand" asset packs to this location
                        // as result of a previous App Bundle build.
                        return bundlePath;
                    }
                }
            }
            // Load resource from the default location.
            return location.InternalId;
        }
    }
}
