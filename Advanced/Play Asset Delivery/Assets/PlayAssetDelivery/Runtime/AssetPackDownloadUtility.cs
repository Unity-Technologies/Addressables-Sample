using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;

namespace PlayAssetDelivery
{
    public class AssetPackDownloadUtility
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="assetPackName">Name of the asset pack.</param>
        /// <param name="status">Status of the asset pack.</param>
        /// <param name="throwException">Set to true to throw an exception if an error occurs. Otherwise the error is logged.</param>
        public static void ProcessAssetPackStatus(string assetPackName, AndroidAssetPackStatus status)
        {
            if (status == AndroidAssetPackStatus.Failed)
                Debug.LogError($"Failed to retrieve the state of asset pack '{assetPackName}'."); 
            else if (status == AndroidAssetPackStatus.Unknown)
                Debug.LogError($"Asset pack '{assetPackName}' is unavailable for this application. This can occur if the app was not installed through Google Play.");
            else if (status == AndroidAssetPackStatus.Canceled)
                Debug.LogError($"Cancelled asset pack download request '{assetPackName}'.");
            else if (status == AndroidAssetPackStatus.WaitingForWifi)
                AndroidAssetPacks.RequestToUseMobileDataAsync(result =>
                {
                    if (!result.allowed)
                        Debug.LogError("Request to use mobile data was denied.");
                });
        }
    }
}
