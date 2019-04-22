#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Serialization;

namespace UnityEngine.ResourceManagement.ResourceProviders.Simulation
{
    /// <summary>
    /// Serialized data containing the asset bundle layout.
    /// </summary>
    [Serializable]
    public class VirtualAssetBundleRuntimeData
    {
        [FormerlySerializedAs("m_simulatedAssetBundles")]
        [SerializeField]
        List<VirtualAssetBundle> m_SimulatedAssetBundles = new List<VirtualAssetBundle>();
        [FormerlySerializedAs("m_remoteLoadSpeed")]
        [SerializeField]
        long m_RemoteLoadSpeed = 1024 * 100;
        [FormerlySerializedAs("m_localLoadSpeed")]
        [SerializeField]
        long m_LocalLoadSpeed = 1024 * 1024 * 10;
        /// <summary>
        /// The list of asset bundles to simulate.
        /// </summary>
        public List<VirtualAssetBundle> AssetBundles { get { return m_SimulatedAssetBundles; } }
        /// <summary>
        /// Bandwidth value (in bytes per second) to simulate loading from a remote location.
        /// </summary>
        public long RemoteLoadSpeed { get { return m_RemoteLoadSpeed; } }
        /// <summary>
        /// Bandwidth value (in bytes per second) to simulate loading from a local location.
        /// </summary>
        public long LocalLoadSpeed { get { return m_LocalLoadSpeed; } }

        /// <summary>
        /// Construct a new VirtualAssetBundleRuntimeData object.
        /// </summary>
        public VirtualAssetBundleRuntimeData() { }
        /// <summary>
        /// Construct a new VirtualAssetBundleRuntimeData object.
        /// </summary>
        /// <param name="localSpeed">Bandwidth value (in bytes per second) to simulate loading from a local location.</param>
        /// <param name="remoteSpeed">Bandwidth value (in bytes per second) to simulate loading from a remote location.</param>
        public VirtualAssetBundleRuntimeData(long localSpeed, long remoteSpeed)
        {
            m_LocalLoadSpeed = localSpeed;
            m_RemoteLoadSpeed = remoteSpeed;
        }
    }
}
#endif