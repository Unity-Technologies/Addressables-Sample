using System;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.WriteTypes
{
    /// <summary>
    /// Explicit implementation for writing a scene serialized file that can be used with the upcoming raw loading systems.
    /// </summary>
    [Serializable]
    public class SceneRawWriteOperation : IWriteOperation
    {
        /// <inheritdoc />
        public WriteCommand Command { get; set; }
        /// <inheritdoc />
        public BuildUsageTagSet UsageSet { get; set; }
        /// <inheritdoc />
        public BuildReferenceMap ReferenceMap { get; set; }

        public string Scene { get; set; }
        public string ProcessedScene { get; set; }

        /// <inheritdoc />
        public WriteResult Write(string outputFolder, BuildSettings settings, BuildUsageTagGlobal globalUsage)
        {
            return ContentBuildInterface.WriteSceneSerializedFile(outputFolder, Scene, ProcessedScene, Command, settings, globalUsage, UsageSet, ReferenceMap);
        }

        /// <inheritdoc />
        public Hash128 GetHash128()
        {
            var processedSceneHash = HashingMethods.CalculateFile(ProcessedScene).ToHash128();
            return HashingMethods.Calculate(Command, UsageSet.GetHash128(), ReferenceMap.GetHash128(), Scene, processedSceneHash).ToHash128();
        }
    }
}
