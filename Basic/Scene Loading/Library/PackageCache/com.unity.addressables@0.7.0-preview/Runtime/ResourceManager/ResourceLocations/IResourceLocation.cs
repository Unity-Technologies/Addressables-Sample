using System;
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement.ResourceLocations
{
    /// <summary>
    /// Contains enough information to load an asset (what/where/how/dependencies)
    /// </summary>
    public interface IResourceLocation
    {
        /// <summary>
        /// Internal name used by the provider to load this location
        /// </summary>
        /// <value>The identifier.</value>
        string InternalId { get; }

        /// <summary>
        /// Matches the provider used to provide/load this location
        /// </summary>
        /// <value>The provider id.</value>
        string ProviderId { get; }

        /// <summary>
        /// Gets the dependencies to other IResourceLocations
        /// </summary>
        /// <value>The dependencies.</value>
        IList<IResourceLocation> Dependencies { get; }

        /// <summary>
        /// The hash of this location combined with the specified type.
        /// </summary>
        /// <param name="resultType">The type of the result.</param>
        /// <returns>The combined hash of the location and the type.</returns>
        int Hash(Type resultType);

        /// <summary>
        /// The precomputed hash code of the dependencies.
        /// </summary>
        int DependencyHashCode { get; }

        /// <summary>
        /// Gets the dependencies to other IResourceLocations
        /// </summary>
        /// <value>The dependencies.</value>
        bool HasDependencies { get; }

        /// <summary>
        /// Gets any data object associated with this locations
        /// </summary>
        /// <value>The object.</value>
        object Data { get; }

    }
}
