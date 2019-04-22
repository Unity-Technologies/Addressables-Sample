using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace UnityEngine.AddressableAssets.ResourceLocators
{
    /// <summary>
    /// Simple locator that acts as a passthrough for assets loaded from resources directories.
    /// </summary>
    public class LegacyResourcesLocator : IResourceLocator
    {
        /// <summary>
        /// The key is converted to a string and used as the internal id of the location added to the locations parameter.
        /// </summary>
        /// <param name="key">The key of the location.  This should be a string with the resources path of the asset.</param>
        /// <param name="locations">The list of locations.  This will have at most one item.</param>
        /// <returns>True if the key is a string object and a location was created, false otherwise.</returns>
        public bool Locate(object key, out IList<IResourceLocation> locations)
        {
            locations = null;
            var strKey = key as string; 
            if (strKey == null)
                return false;
            locations = new List<IResourceLocation>();
            locations.Add(new ResourceLocationBase("LegacyResourceLocation", strKey, typeof(LegacyResourcesProvider).FullName));
            return true;
        }

        /// <summary>
        /// The keys available in this locator.
        /// </summary>
        public IEnumerable<object> Keys
        {
            get
            {
                return null;
            }
        }

    }
}
