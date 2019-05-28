using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets.Settings.GroupSchemas
{
    /// <summary>
    /// Schema for content updates.
    /// </summary>
    [CreateAssetMenu(fileName = "ContentUpdateGroupSchema.asset", menuName = "Addressable Assets/Group Schemas/Content Update")]
    public class ContentUpdateGroupSchema : AddressableAssetGroupSchema
    {
        [FormerlySerializedAs("m_staticContent")]
        [SerializeField]
        bool m_StaticContent;
        /// <summary>
        /// Is the group static.  This property is used in determining which assets need to be moved to a new remote group during the content update process.
        /// </summary>
        public bool StaticContent
        {
            get { return m_StaticContent; }
            set
            {
                m_StaticContent = value;
                SetDirty(true);
            }
        }
    }
}