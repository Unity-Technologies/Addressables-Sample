using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace UnityEngine.AddressableAssets.ResourceLocators
{
    /// <summary>
    /// Simple implementation of an IResourceLocator
    /// </summary>
    public class ResourceLocationMap : IResourceLocator
    {
        /// <summary>
        /// Construct a new ResourceLocationMap object.
        /// </summary>
        /// <param name="capacity">The expected number of items.</param>
        public ResourceLocationMap(int capacity = 0)
        {
            Locations = new Dictionary<object, IList<IResourceLocation>>(capacity == 0 ? 100 : capacity);
        }

        /// <summary>
        /// Construct a new ResourceLocationMap object with a list of locations.
        /// </summary>
        /// <param name="locations">The list of locations to initialize with.</param>
        public ResourceLocationMap(IList<ResourceLocationData> locations)
        {
            if (locations == null)
                return;
            Locations = new Dictionary<object, IList<IResourceLocation>>(locations.Count * 2);
            var locMap = new Dictionary<string, ResourceLocationBase>();
            var dataMap = new Dictionary<string, ResourceLocationData>();
            //create and collect locations
            for (int i = 0; i < locations.Count; i++)
            {
                var rlData = locations[i];
                if (rlData.Keys == null || rlData.Keys.Length < 1)
                {
                    Addressables.LogErrorFormat("Address with id '{0}' does not have any valid keys, skipping...", rlData.InternalId);
                    continue;
                }
                if (locMap.ContainsKey(rlData.Keys[0]))
                {
                    Addressables.LogErrorFormat("Duplicate address '{0}' with id '{1}' found, skipping...", rlData.Keys[0], rlData.InternalId);
                    continue;
                }
                var loc = new ResourceLocationBase(rlData.Keys[0], Addressables.ResolveInternalId(rlData.InternalId), rlData.Provider);
                locMap.Add(rlData.Keys[0], loc);
                dataMap.Add(rlData.Keys[0], rlData);
            }

            //fix up dependencies between them
            foreach (var kvp in locMap)
            {
                var data = dataMap[kvp.Key];
                if (data.Dependencies != null)
                {
                    foreach (var d in data.Dependencies)
                        kvp.Value.Dependencies.Add(locMap[d]);
                    kvp.Value.ComputeDependencyHash();
                }
            }
            foreach (KeyValuePair<string, ResourceLocationBase> kvp in locMap)
            {
                ResourceLocationData rlData = dataMap[kvp.Key];
                foreach (var k in rlData.Keys)
                    Add(k, kvp.Value);
            }
        }


        /// <summary>
        /// The mapping of key to location lists.
        /// </summary>
        public Dictionary<object, IList<IResourceLocation>> Locations { get; private set; }

        /// <summary>
        /// The keys available in this locator.
        /// </summary>
        public IEnumerable<object> Keys
        {
            get
            {
                return Locations.Keys;
            }
        }

        /// <summary>
        /// Locate all of the locations that match the given key.
        /// </summary>
        /// <param name="key">The key used to locate the locations.</param>
        /// <param name="locations">The list of found locations.  This list is shared so it should not be modified.</param>
        /// <returns></returns>
        public bool Locate(object key, out IList<IResourceLocation> locations)
        {
            return Locations.TryGetValue(key, out locations);
        }

        /// <summary>
        /// Add a new location.
        /// </summary>
        /// <param name="key">The key to reference the location.</param>
        /// <param name="location">The location to add.</param>
        public void Add(object key, IResourceLocation location)
        {
            IList<IResourceLocation> locations;
            if (!Locations.TryGetValue(key, out locations))
                Locations.Add(key, locations = new List<IResourceLocation>());
            locations.Add(location);
        }

        /// <summary>
        /// Add a list of locations.
        /// </summary>
        /// <param name="key">The key to reference the locations with.</param>
        /// <param name="locations">The list of locations to store at the given key.</param>
        public void Add(object key, IList<IResourceLocation> locations)
        {
            Locations.Add(key, locations);
        }
    }
}
