using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.ResourceManagement.ResourceLocations
{
    /// <summary>
    /// Interface for computing size of loading a location.
    /// </summary>
    public interface ILocationSizeData
    {
        /// <summary>
        /// Compute the numder of bytes need to download for the specified location.
        /// </summary>
        /// <param name="loc">The location to compute the size for.</param>
        /// <returns>The size in bytes of the data needed to be downloaded.</returns>
        long ComputeSize(IResourceLocation loc);
    }
}