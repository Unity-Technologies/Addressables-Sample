using System;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.WriteTypes
{
    /// <summary>
    /// Explicit implementation for writing a serialized file that can be used with the upcoming raw loading systems.
    /// </summary>
    [Serializable]
    public class RawWriteOperation : IWriteOperation
    {
        /// <inheritdoc />
        public WriteCommand Command { get; set; }
        /// <inheritdoc />
        public BuildUsageTagSet UsageSet { get; set; }
        /// <inheritdoc />
        public BuildReferenceMap ReferenceMap { get; set; }

        /// <inheritdoc />
        public WriteResult Write(string outputFolder, BuildSettings settings, BuildUsageTagGlobal globalUsage)
        {
            return ContentBuildInterface.WriteSerializedFile(outputFolder, Command, settings, globalUsage, UsageSet, ReferenceMap);
        }

        /// <inheritdoc />
        public Hash128 GetHash128()
        {
            return HashingMethods.Calculate(Command, UsageSet.GetHash128(), ReferenceMap.GetHash128()).ToHash128();
        }
    }
}
