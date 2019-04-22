using System;
using UnityEditor.AddressableAssets.Settings;

namespace UnityEditor.AddressableAssets.HostingServices
{
    /// <summary>
    /// Interface for providing configuration data for <see cref="IHostingService"/> implementations
    /// </summary>
    public interface IHostingServiceConfigurationProvider
    {
        /// <summary>
        /// Returns the Hosting Service content root path for the given <see cref="AddressableAssetGroup"/>
        /// </summary>
        string HostingServicesContentRoot { get; }
    }
}