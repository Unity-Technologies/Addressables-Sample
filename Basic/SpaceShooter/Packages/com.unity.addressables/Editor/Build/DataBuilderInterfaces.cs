using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Build
{
    /// <summary>
    /// The result of IDataBuilder.Build.
    /// </summary>
    public interface IDataBuilderResult
    {
        /// <summary>
        /// Duration of the build in seconds.
        /// </summary>
        double Duration { get; set; }
        /// <summary>
        /// Error string, if any.  If Succeeded is true, this may be null.
        /// </summary>
        string Error { get; set; }
        /// <summary>
        /// Path of runtime settings file
        /// </summary>
        string OutputPath { get; set; }
        /// <summary>
        /// Registry of files created during the build
        /// </summary>
        FileRegistry FileRegistry { get; set; }
    }

    /// <summary>
    /// Builds objects of type IDataBuilderResult.
    /// </summary>
    public interface IDataBuilder
    {
        /// <summary>
        /// The name of the builder, used for GUI.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Can this builder build the type of data requested.
        /// </summary>
        /// <typeparam name="T">The data type.</typeparam>
        /// <returns>True if the build can build it.</returns>
        bool CanBuildData<T>() where T : IDataBuilderResult;
        /// <summary>
        /// Build the data of a specific type.
        /// </summary>
        /// <typeparam name="TResult">The data type.</typeparam>
        /// <param name="builderInput">The builderInput used to build the data.</param>
        /// <returns>The built data.</returns>
        TResult BuildData<TResult>(AddressablesDataBuilderInput builderInput) where TResult : IDataBuilderResult;

        /// <summary>
        /// Clears all cached data.
        /// </summary>
        void ClearCachedData();
    }
}