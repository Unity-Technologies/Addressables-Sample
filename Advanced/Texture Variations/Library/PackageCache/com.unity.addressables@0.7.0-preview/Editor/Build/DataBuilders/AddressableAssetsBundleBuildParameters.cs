using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Pipeline;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UnityEditor.AddressableAssets.Build.DataBuilders
{
    /// <summary>
    /// Custom bundle parameter container that provides custom compression settings per bundle.
    /// </summary>
    public class AddressableAssetsBundleBuildParameters : BundleBuildParameters
    {
        Dictionary<string, string> m_bundleToAssetGroup;
        AddressableAssetSettings m_settings;
        /// <summary>
        /// Create a AddressableAssetsBundleBuildParameters with data needed to determine the correct compression per bundle.
        /// </summary>
        /// <param name="aaSettings">The AddressableAssetSettings object to use for retrieving groups.</param>
        /// <param name="bundleToAssetGroup">Mapping of bundle identifier to guid of asset groups.</param>
        /// <param name="target">The build target.  This is used by the BundleBuildParameters base class.</param>
        /// <param name="group">The build target group. This is used by the BundleBuildParameters base class.</param>
        /// <param name="outputFolder">The path for the output folder. This is used by the BundleBuildParameters base class.</param>
        public AddressableAssetsBundleBuildParameters(AddressableAssetSettings aaSettings, Dictionary<string, string> bundleToAssetGroup, BuildTarget target, BuildTargetGroup group, string outputFolder) : base(target, group, outputFolder)
        {
            UseCache = true;
            m_settings = aaSettings;
            m_bundleToAssetGroup = bundleToAssetGroup;
        }

        /// <summary>
        /// Get the compressions settings for the specified asset bundle.
        /// </summary>
        /// <param name="identifier">The identifier of the asset bundle.</param>
        /// <returns>The compression setting for the asset group.  If the group is not found, the default compression is used.</returns>
        public override BuildCompression GetCompressionForIdentifier(string identifier)
        {
            string groupGuid;
            if (m_bundleToAssetGroup.TryGetValue(identifier, out groupGuid))
            {
                var group = m_settings.FindGroup(g => g.Guid == groupGuid);
                if (group != null)
                {
                    var abSchema = group.GetSchema<BundledAssetGroupSchema>();
                    if (abSchema != null)
                        return abSchema.GetBuildCompressionForBundle(identifier);
                    else
                        Debug.LogWarningFormat("Bundle group {0} does not have BundledAssetGroupSchema.", group.name);
                }
                else
                {
                    Debug.LogWarningFormat("Unable to find group with guid {0}", groupGuid);
                }
            }
            return base.GetCompressionForIdentifier(identifier);
        }
    }
}