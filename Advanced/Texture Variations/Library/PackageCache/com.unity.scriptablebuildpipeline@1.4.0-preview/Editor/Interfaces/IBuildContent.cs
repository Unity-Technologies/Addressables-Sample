using System.Collections.Generic;

namespace UnityEditor.Build.Pipeline.Interfaces
{
    /// <summary>
    /// Base interface for feeding Assets to the Scriptable Build Pipeline.
    /// </summary>
    public interface IBuildContent : IContextObject
    {
        /// <summary>
        /// List of Assets to include.
        /// </summary>
        List<GUID> Assets { get; }

        /// <summary>
        /// List of Scenes to include.
        /// </summary>
        List<GUID> Scenes { get; }
    }

    /// <summary>
    /// Base interface for feeding Assets with explicit Asset Bundle layout to the Scriptable Build Pipeline.
    /// </summary>
    public interface IBundleBuildContent : IBuildContent
    {
        /// <summary>
        /// Specific layout of asset bundles to assets or scenes.
        /// </summary>
        Dictionary<string, List<GUID>> BundleLayout { get; }

        /// <summary>
        /// Custom loading identifiers to use for Assets or Scenes.
        /// </summary>
        Dictionary<GUID, string> Addresses { get; }
    }
}