using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace PlayAssetDelivery.Editor
{
    [DisplayName("Play Asset Delivery Provider")]
    public class PlayAssetDeliveryAssetBundleProvider : AssetBundleProvider
    {
        ProvideHandle m_ProviderInterface;
        string m_AssetPackName;
        string m_InstallTimePackPath;

        public override void Provide(ProvideHandle providerInterface)
        {
            Reset();
#if UNITY_ANDROID && !UNITY_EDITOR
            m_ProviderInterface = providerInterface;
            m_AssetPackName = Path.GetFileNameWithoutExtension(m_ProviderInterface.Location.InternalId);

            // Check if the bundle was assigned to an install-time custom asset pack. The APK's StreamingAssets path should contain the bundle.
            string bundleFileName = Path.GetFileName(providerInterface.Location.InternalId);
            m_InstallTimePackPath = Path.Combine(Application.streamingAssetsPath, bundleFileName);
            
            UnityWebRequest www = UnityWebRequest.Get(m_InstallTimePackPath);
            www.SendWebRequest().completed += OnWebRequestCompleted;
#else
            base.Provide(providerInterface);
#endif
        }

        public override void Release(IResourceLocation location, object asset)
        {
            base.Release(location, asset);
            Reset();
        }

        void Reset()
        {
            if(!string.IsNullOrEmpty(m_AssetPackName))
                PlayerPrefs.DeleteKey(m_AssetPackName);
            m_AssetPackName = null;
            m_InstallTimePackPath = null;
            m_ProviderInterface = default;
        }

        void OnWebRequestCompleted(UnityEngine.AsyncOperation op)
        {
            UnityWebRequestAsyncOperation webOp = op as UnityWebRequestAsyncOperation;
            if (webOp.webRequest.result == UnityWebRequest.Result.Success)
            {
                // Located bundle in the APK's StreamingAssets path.
                PlayerPrefs.SetString(m_AssetPackName, m_InstallTimePackPath);
                base.Provide(m_ProviderInterface);
                return;
            }

            // Otherwise the bundle must be assigned to a fast-follow or on-demand custom asset pack. Check if the pack was installed to the device.
            //
            // Note that most methods in the AndroidAssetPacks class are either direct wrappers of java APIs in Google's PlayCore plugin,
            // or depend on values that the PlayCore API returns. If the PlayCore plugin is missing, calling these methods will throw an InvalidOperationException exception.
            // We check for the exception here, once.
            string assetPackPath = "";
            try
            {
                assetPackPath = AndroidAssetPacks.GetAssetPackPath(m_AssetPackName);
            }
            catch(InvalidOperationException ioe)
            {
                Debug.LogError($"Cannot retrieve state for asset pack '{m_AssetPackName}'. PlayCore Plugin is not installed: {ioe.Message}");
                m_ProviderInterface.Complete(this, false, new Exception("exception"));
            }

            if (string.IsNullOrEmpty(assetPackPath))
            {
                // Asset pack is not located on device. Download it from Google Play.
                AndroidAssetPacks.DownloadAssetPackAsync(new string[] { m_AssetPackName }, CheckDownloadStatus);
            }
            else
            {
                // Asset pack was located on device. Proceed with loading the bundle.
                string bundlePath = Path.Combine(assetPackPath, Path.GetFileName(m_ProviderInterface.Location.InternalId));
                PlayerPrefs.SetString(m_AssetPackName, bundlePath);
                base.Provide(m_ProviderInterface);
            }
        }
        
        void CheckDownloadStatus(AndroidAssetPackInfo info)
        {
            string message = "";
            if (info.status == AndroidAssetPackStatus.Failed)
                message = $"Failed to retrieve the state of asset pack '{info.name}'.";
            else if (info.status == AndroidAssetPackStatus.Unknown)
                message = $"Asset pack '{info.name}' is unavailable for this application. This can occur if the app was not installed through Google Play.";
            else if (info.status == AndroidAssetPackStatus.Canceled)
                message = $"Cancelled asset pack download request '{info.name}'.";
            else if (info.status == AndroidAssetPackStatus.WaitingForWifi)
                AndroidAssetPacks.RequestToUseMobileDataAsync(OnRequestToUseMobileDataComplete);
            else if(info.status == AndroidAssetPackStatus.Completed)
            {
                string assetPackPath = AndroidAssetPacks.GetAssetPackPath(info.name);

                if (!string.IsNullOrEmpty(assetPackPath))
                {
                    // Asset pack was located on device. Proceed with loading the bundle.
                    string bundlePath = Path.Combine(assetPackPath, Path.GetFileName(m_ProviderInterface.Location.InternalId));
                    PlayerPrefs.SetString(info.name, bundlePath);
                    base.Provide(m_ProviderInterface);
                }
                else
                    message = $"Downloaded asset pack '{info.name}' but cannot locate it on device.";
            }

            if (!string.IsNullOrEmpty(message))
            {
                Debug.LogError(message);
                m_ProviderInterface.Complete(this, false, new Exception("exception"));
            }
        }

        void OnRequestToUseMobileDataComplete(AndroidAssetPackUseMobileDataRequestResult result)
        {
            if (!result.allowed)
            {
                Debug.LogError("Request to use mobile data was denied.");
                m_ProviderInterface.Complete(this, false, new Exception("exception"));
            }
        }
    }
}
