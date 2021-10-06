using System.Collections.Generic;
using UnityEngine.ResourceManagement.Util;

namespace AddressablesPlayAssetDelivery
{
    /// <summary>
    /// Stores runtime data for loading content from asset packs.
    /// </summary>
    public class PlayAssetDeliveryRuntimeData
    {
        Dictionary<string, string> m_AssetPackNameToDownloadPath;
        Dictionary<string, CustomAssetPackDataEntry> m_BundleNameToAssetPack;
        private static PlayAssetDeliveryRuntimeData s_Instance = null;

        /// <summary>
        /// Reference to the singleton object.
        /// </summary>
        public static PlayAssetDeliveryRuntimeData Instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new PlayAssetDeliveryRuntimeData();
                return s_Instance;
            }
        }

        /// <summary>
        /// Maps an asset bundle name to the name of its assigned asset pack.
        /// </summary>
        public Dictionary<string, CustomAssetPackDataEntry> BundleNameToAssetPack
        {
            get { return m_BundleNameToAssetPack; }
        }

        /// <summary>
        /// Maps an asset pack name to the location where it has been downloaded.
        /// </summary>
        public Dictionary<string, string> AssetPackNameToDownloadPath
        {
            get { return m_AssetPackNameToDownloadPath; }
        }

        public PlayAssetDeliveryRuntimeData()
        {
            m_AssetPackNameToDownloadPath = new Dictionary<string, string>();
            m_BundleNameToAssetPack = new Dictionary<string, CustomAssetPackDataEntry>();
        }
    }
}
