using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets.Settings.GroupSchemas
{
    /// <summary>
    /// Schema for the player data asset group
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerDataGroupSchema.asset", menuName = "Addressable Assets/Group Schemas/Player Data")]
    public class PlayerDataGroupSchema : AddressableAssetGroupSchema
    {
        [FormerlySerializedAs("m_includeResourcesFolders")]
        [SerializeField]
        bool m_IncludeResourcesFolders = true;
        /// <summary>
        /// If enabled, all assets in resources folders will have addresses generated during the build.
        /// </summary>
        public bool IncludeResourcesFolders
        {
            get
            {
                return m_IncludeResourcesFolders;
            }
            set
            {
                m_IncludeResourcesFolders = value;
                SetDirty(true);
            }
        }
        [FormerlySerializedAs("m_includeBuildSettingsScenes")]
        [SerializeField]
        bool m_IncludeBuildSettingsScenes = true;
        /// <summary>
        /// If enabled, all scenes in the editor build settings will have addresses generated during the build.
        /// </summary>
        public bool IncludeBuildSettingsScenes
        {
            get
            {
                return m_IncludeBuildSettingsScenes;
            }
            set
            {
                m_IncludeBuildSettingsScenes = value;
                SetDirty(true);
            }
        }
    }
}