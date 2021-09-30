using System.Collections.Generic;
using UnityEngine.ResourceManagement.Util;

namespace AddressablesPlayAssetDelivery
{
    /// <summary>
    /// Stores runtime data for loading content from asset packs.
    /// </summary>
    public class PlayAssetDeliveryRuntimeDataSingleton : ComponentSingleton<PlayAssetDeliveryRuntimeDataSingleton>
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
    }
}
