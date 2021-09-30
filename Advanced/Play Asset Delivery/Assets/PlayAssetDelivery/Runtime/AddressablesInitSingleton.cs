using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Android;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

namespace AddressablesPlayAssetDelivery
{
    public class AddressablesInitSingleton : ComponentSingleton<AddressablesInitSingleton>
    {
        /// <summary>
        /// Maps an asset bundle name to the name of its assigned asset pack.
        /// </summary>
        Dictionary<string, CustomAssetPackDataEntry> m_BundleNameToAssetPack = new Dictionary<string, CustomAssetPackDataEntry>();
        public Dictionary<string, CustomAssetPackDataEntry> BundleNameToAssetPack
        {
            get { return m_BundleNameToAssetPack; }
        }

        /// <summary>
        /// Maps an asset pack name to the location where it has been downloaded.
        /// </summary>
        Dictionary<string, string> m_AssetPackNameToDownloadPath = new Dictionary<string, string>();
        public Dictionary<string, string> AssetPackNameToDownloadPath
        {
            get { return m_AssetPackNameToDownloadPath; }
        }

        /// <summary>
        /// Handle to the operation that sets up Addressables to load content from their expected location.
        /// </summary>
        AsyncOperationHandle<bool> m_InitializeOperation;
        public AsyncOperationHandle<bool> InitializeOperation
        {
            get { return m_InitializeOperation; }
            set { m_InitializeOperation = value;  }
        }

        [Tooltip("Show warnings that occur when initializing the singleton.")]
        public bool logInitializationWarnings = true;

        void Start()
        {
            InitializeOperation = Initialize(logInitializationWarnings);
        }

        /// <summary>
        /// Sets up Addressables to locate content from their expected location.
        /// </summary>
        /// <param name="logWarnings">Set to true to log warnings. Otherwise set to false to disable warnings.</param>
        /// <returns>The handle of this operation.</returns>
        public static AsyncOperationHandle<bool> Initialize(bool logWarnings)
        {
            var op = new PlayAssetDeliveryInitializeOperation();
            return op.Start(logWarnings);
        }
    }

    public class PlayAssetDeliveryInitializeOperation : AsyncOperationBase<bool>
    {
        bool m_LogWarnings = false;
        bool m_IsDone = false; // AsyncOperationBase.IsDone is internal
        bool m_HasExecuted = false;  // AsyncOperationBase.HasExecuted is internal

        public AsyncOperationHandle<bool> Start(bool logWarnings)
        {
            m_LogWarnings = logWarnings;
            return Addressables.ResourceManager.StartOperation(this, default);
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
                    AddressablesInitSingleton.Instance.BundleNameToAssetPack.Add(bundle, entry);
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
                if (AddressablesInitSingleton.Instance.BundleNameToAssetPack.ContainsKey(bundleName))
                {
                    string assetPackName = AddressablesInitSingleton.Instance.BundleNameToAssetPack[bundleName].AssetPackName;
                    if (AddressablesInitSingleton.Instance.AssetPackNameToDownloadPath.ContainsKey(assetPackName))
                    {
                        // Load bundle that was assigned to a custom fast-follow or on-demand asset pack.
                        // PlayAssetDeliveryBundleProvider.Provider previously saved the asset pack path.
                        return Path.Combine(AddressablesInitSingleton.Instance.AssetPackNameToDownloadPath[assetPackName], Path.GetFileName(location.InternalId));
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
                if (AddressablesInitSingleton.Instance.BundleNameToAssetPack.ContainsKey(bundleName))
                {
                    string assetPackName = AddressablesInitSingleton.Instance.BundleNameToAssetPack[bundleName].AssetPackName;
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
