using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace UnityEditor.AddressableAssets.Build.DataBuilders
{
    
    /// <summary>
    /// Interface for any Addressables specific context objects to be used in the Scriptable Build Pipeline context store  
    /// </summary>
    public interface IAddressableAssetsBuildContext : IContextObject { }

    /// <summary>
    /// Simple context object for passing data through SBP, between different sections of Addressables code. 
    /// </summary>
    public class AddressableAssetsBuildContext : IAddressableAssetsBuildContext
    {
        public AddressableAssetSettings settings;
        public ResourceManagerRuntimeData runtimeData;
        public List<ContentCatalogDataEntry> locations;
        public Dictionary<string, string> bundleToAssetGroup;
        public Dictionary<AddressableAssetGroup, List<string>> assetGroupToBundles;
    }
}
