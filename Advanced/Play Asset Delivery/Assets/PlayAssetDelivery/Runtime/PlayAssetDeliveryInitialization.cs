using System;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Android;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

namespace AddressablesPlayAssetDelivery
{
    /// <summary>
    /// IInitializableObject that configures Addressables for loading content from asset packs.
    /// </summary>
    [Serializable]
    public class PlayAssetDeliveryInitialization : IInitializableObject
    {
        public bool Initialize(string id, string data)
        {
            return true;
        }

        /// <summary>
        /// Determines whether warnings should be logged during initialization.
        /// </summary>
        /// <param name="data">The JSON serialized <see cref="PlayAssetDeliveryInitializationData"/> object</param>
        /// <returns>True to log warnings, otherwise returns false. Default value is true.</returns>
        public bool LogWarnings(string data)
        {
            var initializeData = JsonUtility.FromJson<PlayAssetDeliveryInitializationData>(data);
            if (initializeData != null)
                return initializeData.LogWarnings;
            return true;
        }

        /// <inheritdoc/>
        public virtual AsyncOperationHandle<bool> InitializeAsync(ResourceManager rm, string id, string data)
        {
            var op = new PlayAssetDeliveryInitializeOperation();
            return op.Start(rm, LogWarnings(data));
        }
    }

    /// <summary>
    /// Configures Addressables for loading content from asset packs
    /// </summary>
    public class PlayAssetDeliveryInitializeOperation : AsyncOperationBase<bool>
    {
        ResourceManager m_RM;
        bool m_LogWarnings = false;

        bool m_IsDone = false; // AsyncOperationBase.IsDone is internal
        bool m_HasExecuted = false;  // AsyncOperationBase.HasExecuted is internal

        public AsyncOperationHandle<bool> Start(ResourceManager rm, bool logWarnings)
        {
            m_RM = rm;
            m_LogWarnings = logWarnings;
            return m_RM.StartOperation(this, default);
        }

        protected override bool InvokeWaitForCompletion()
        {
            if (!m_HasExecuted)
                Execute();
            return m_IsDone;
        }

        void CompleteOverride(string warningMsg)
        {
            if (m_LogWarnings && warningMsg != null)
                Debug.LogWarning($"{warningMsg} Default internal id locations will be used instead.");
            Complete(true, true, "");
            m_IsDone = true;
        }

        protected override void Execute()
        {
            Addressables.ResourceManager.ResourceProviders.Add(new PlayAssetDeliveryAssetBundleProvider());
        #if UNITY_ANDROID && !UNITY_EDITOR
            LoadFromAssetPacksIfAvailable();
        #elif UNITY_ANDROID && UNITY_EDITOR
            LoadFromEditorData();
#else
            CompleteOverride(null);
#endif
            m_HasExecuted = true;
        }

        void LoadFromAssetPacksIfAvailable()
        {
            if (AndroidAssetPacks.coreUnityAssetPacksDownloaded)
            {
                // Core Unity asset packs use install-time delivery and are already installed.
                DownloadCustomAssetPacksData();
            }
            else
            {
                // Core Unity asset packs use fast-follow or on-demand delivery and need to be downloaded.
                string[] coreUnityAssetPackNames = AndroidAssetPacks.GetCoreUnityAssetPackNames(); // only returns names of asset packs that are fast-follow or on-demand delivery
                if (coreUnityAssetPackNames.Length == 0)
                    CompleteOverride("Cannot retrieve core Unity asset pack names. PlayCore Plugin is not installed.");
                else
                    AndroidAssetPacks.DownloadAssetPackAsync(coreUnityAssetPackNames, CheckDownloadStatus);
            }
        }

        void LoadFromEditorData()
        {
            if (File.Exists(CustomAssetPackUtility.CustomAssetPacksDataEditorPath))
                InitializeBundleToAssetPackMap(File.ReadAllText(CustomAssetPackUtility.CustomAssetPacksDataEditorPath));
            else if (File.Exists(CustomAssetPackUtility.CustomAssetPacksDataRuntimePath))
                InitializeBundleToAssetPackMap(File.ReadAllText(CustomAssetPackUtility.CustomAssetPacksDataRuntimePath));

            Addressables.ResourceManager.InternalIdTransformFunc = EditorTransformFunc;
            CompleteOverride(null);
        }

        void DownloadCustomAssetPacksData()
        {
            UnityWebRequest www = UnityWebRequest.Get(CustomAssetPackUtility.CustomAssetPacksDataRuntimePath);
            www.SendWebRequest().completed += (op) =>
            {
                UnityWebRequest www = (op as UnityWebRequestAsyncOperation).webRequest;
                if (www.result != UnityWebRequest.Result.Success)
                    CompleteOverride($"Could not load '{CustomAssetPackUtility.kCustomAssetPackDataFilename}' : {www.error}.");
                else
                {
                    InitializeBundleToAssetPackMap(www.downloadHandler.text);
                    Addressables.ResourceManager.InternalIdTransformFunc = AppBundleTransformFunc;
                    CompleteOverride(null);
                }
            };
        }

        void InitializeBundleToAssetPackMap(string contents)
        {
            CustomAssetPackData customPackData =  JsonUtility.FromJson<CustomAssetPackData>(contents);
            foreach (CustomAssetPackDataEntry entry in customPackData.Entries)
            {
                foreach (string bundle in entry.AssetBundles)
                {
                    PlayAssetDeliveryRuntimeData.Instance.BundleNameToAssetPack.Add(bundle, entry);
                }
            }
        }

        void CheckDownloadStatus(AndroidAssetPackInfo info)
        {
            if (info.status == AndroidAssetPackStatus.Failed)
                CompleteOverride($"Failed to retrieve the state of asset pack '{info.name}'.");
            else if (info.status == AndroidAssetPackStatus.Unknown)
                CompleteOverride($"Asset pack '{info.name}' is unavailable for this application. This can occur if the app was not installed through Google Play.");
            else if (info.status == AndroidAssetPackStatus.Canceled)
                CompleteOverride($"Cancelled asset pack download request '{info.name}'.");
            else if (info.status == AndroidAssetPackStatus.WaitingForWifi)
            {
                AndroidAssetPacks.RequestToUseMobileDataAsync(result =>
                {
                    if (!result.allowed)
                        CompleteOverride("Request to use mobile data was denied.");
                });
            }
            else if (info.status == AndroidAssetPackStatus.Completed)
            {
                string assetPackPath = AndroidAssetPacks.GetAssetPackPath(info.name);
                if (string.IsNullOrEmpty(assetPackPath))
                    CompleteOverride($"Downloaded asset pack '{info.name}' but cannot locate it on device.");
                else if (AndroidAssetPacks.coreUnityAssetPacksDownloaded)
                    DownloadCustomAssetPacksData();
            }
        }

        string AppBundleTransformFunc(IResourceLocation location)
        {
            if (location.ResourceType == typeof(IAssetBundleResource))
            {
                string bundleName = Path.GetFileNameWithoutExtension(location.InternalId);
                if (PlayAssetDeliveryRuntimeData.Instance.BundleNameToAssetPack.ContainsKey(bundleName))
                {
                    string assetPackName = PlayAssetDeliveryRuntimeData.Instance.BundleNameToAssetPack[bundleName].AssetPackName;
                    if (PlayAssetDeliveryRuntimeData.Instance.AssetPackNameToDownloadPath.ContainsKey(assetPackName))
                    {
                        // Load bundle that was assigned to a custom fast-follow or on-demand asset pack.
                        // PlayAssetDeliveryBundleProvider.Provider previously saved the asset pack path.
                        return Path.Combine(PlayAssetDeliveryRuntimeData.Instance.AssetPackNameToDownloadPath[assetPackName], Path.GetFileName(location.InternalId));
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
                if (PlayAssetDeliveryRuntimeData.Instance.BundleNameToAssetPack.ContainsKey(bundleName))
                {
                    string assetPackName = PlayAssetDeliveryRuntimeData.Instance.BundleNameToAssetPack[bundleName].AssetPackName;
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

    /// <summary>
    /// Contains settings for <see cref="PlayAssetDeliveryInitialization"/>.
    /// </summary>
    [Serializable]
    public class PlayAssetDeliveryInitializationData
    {
        [SerializeField]
        bool m_LogWarnings = true;
        /// <summary>
        /// Enable recompression of asset bundles into LZ4 format as they are saved to the cache.  This sets the Caching.compressionEnabled value.
        /// </summary>
        public bool LogWarnings { get { return m_LogWarnings; } set { m_LogWarnings = value; } }
    }
}
