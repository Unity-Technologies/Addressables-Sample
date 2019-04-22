using System;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.WriteTypes
{
    /// <summary>
    /// Explicit implementation for writing a scene serialized file that can be used with the player data systems.
    /// </summary>
    [Serializable]
    public class SceneDataWriteOperation : IWriteOperation
    {
        /// <inheritdoc />
        public WriteCommand Command { get; set; }
        /// <inheritdoc />
        public BuildUsageTagSet UsageSet { get; set; }
        /// <inheritdoc />
        public BuildReferenceMap ReferenceMap { get; set; }

        /// <summary>
        /// Source scene asset path
        /// </summary>
        public string Scene { get; set; }

        /// <summary>
        /// Processed scene path returned by the ProcessScene API.
        /// <seealso cref="ContentBuildInterface.PrepareScene"/>
        /// </summary>
        public string ProcessedScene { get; set; }

        /// <summary>
        /// Information needed for scene preloadeding. 
        /// <seealso cref="PreloadInfo"/>
        /// </summary>
        public PreloadInfo PreloadInfo { get; set; }

        /// <inheritdoc />
        public WriteResult Write(string outputFolder, BuildSettings settings, BuildUsageTagGlobal globalUsage)
        {
            return ContentBuildInterface.WriteSceneSerializedFile(outputFolder, Scene, ProcessedScene, Command, settings, globalUsage, UsageSet, ReferenceMap, PreloadInfo);
        }

        /// <inheritdoc />
        public Hash128 GetHash128()
        {
            Hash128 processedSceneHash = HashingMethods.CalculateFile(ProcessedScene).ToHash128();
            return HashingMethods.Calculate(Command, UsageSet.GetHash128(), ReferenceMap.GetHash128(), Scene, processedSceneHash, PreloadInfo).ToHash128();
        }
    }
}
