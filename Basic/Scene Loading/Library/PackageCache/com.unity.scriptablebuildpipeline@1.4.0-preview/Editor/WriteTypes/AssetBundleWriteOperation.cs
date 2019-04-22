using System;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.WriteTypes
{
    /// <summary>
    /// Explicit implementation for writing a serialized file that can be used with the Asset Bundle systems.
    /// </summary>
    [Serializable]
    public class AssetBundleWriteOperation : IWriteOperation
    {
        /// <inheritdoc />
        public WriteCommand Command { get; set; }
        /// <inheritdoc />
        public BuildUsageTagSet UsageSet { get; set; }
        /// <inheritdoc />
        public BuildReferenceMap ReferenceMap { get; set; }

        /// <summary>
        /// Information needed for generating the Asset Bundle object to be included in the serialized file.
        /// <see cref="AssetBundleInfo"/>
        /// </summary>
        public AssetBundleInfo Info { get; set; }

        /// <inheritdoc />
        public WriteResult Write(string outputFolder, BuildSettings settings, BuildUsageTagGlobal globalUsage)
        {
            return ContentBuildInterface.WriteSerializedFile(outputFolder, Command, settings, globalUsage, UsageSet, ReferenceMap, Info);
        }

        /// <inheritdoc />
        public Hash128 GetHash128()
        {
            return HashingMethods.Calculate(Command, UsageSet.GetHash128(), ReferenceMap.GetHash128(), Info).ToHash128();
        }
    }
}
