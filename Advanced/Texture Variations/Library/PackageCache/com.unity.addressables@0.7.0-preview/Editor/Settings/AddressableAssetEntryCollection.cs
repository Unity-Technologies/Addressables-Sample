using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets.Settings
{
    /// <summary>
    /// Contains a list of AddressableAssetEntries that can be included in the settings.  The purpose of this class is to provide a way of combining entries from external sources such as packages into your project settings.
    /// </summary>
    public class AddressableAssetEntryCollection : ScriptableObject
    {
        [FormerlySerializedAs("m_serializeEntries")]
        [SerializeField]
        List<AddressableAssetEntry> m_SerializeEntries = new List<AddressableAssetEntry>();
        /// <summary>
        /// The collection of entries.
        /// </summary>
        public List<AddressableAssetEntry> Entries { get { return m_SerializeEntries; } }
    }
}