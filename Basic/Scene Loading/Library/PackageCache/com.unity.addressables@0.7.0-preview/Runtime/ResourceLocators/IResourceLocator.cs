using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace UnityEngine.AddressableAssets.ResourceLocators
{
    /// <summary>
    /// Interface used by the Addressables system to find th locations of a given key.
    /// </summary>
    public interface IResourceLocator
    {
        /// <summary>
        /// The keys defined by this locator.
        /// </summary>
        IEnumerable<object> Keys { get; }
        /// <summary>
        /// Retrieve the locations from a specified key.
        /// </summary>
        /// <param name="key">The key to use.</param>
        /// <param name="locations">The resulting set of locations for the key.</param>
        /// <returns>True if any locations were found with the specified key.</returns>
        bool Locate(object key, out IList<IResourceLocation> locations);
    }
}
