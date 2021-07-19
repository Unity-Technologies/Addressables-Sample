#if UNITY_EDITOR
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace PlayAssetDelivery.Editor
{
    /// <summary>
    /// Serializable representation of 'Unity.Android.Types.AndroidAssetPackDeliveryType'.
    /// </summary>
    public enum DeliveryType
    {
        /// <summary>
        /// No delivery type specified.
        /// </summary>
        None = 0,

        /// <summary>
        /// Content is downloaded when the app is installed.
        /// </summary>
        InstallTime = 1,

        /// <summary>
        /// Content is downloaded automatically as soon as the the app is installed.
        /// </summary>
        FastFollow = 2,

        /// <summary>
        /// Content is downloaded while the app is running.
        /// </summary>
        OnDemand = 3
    }

    [DisplayName("Play Asset Delivery")]
    public class PlayAssetDeliverySchema : AddressableAssetGroupSchema
    {
        [SerializeField]
        [Tooltip("The delivery type for all asset packs with bundled content from this group. Each pack contains one bundle.")]
        DeliveryType m_DeliveryType = DeliveryType.InstallTime;
    
        /// <summary>
        /// The delivery type for all asset packs with bundled content from this group. Each pack contains one bundle.
        /// </summary>
        public DeliveryType DeliveryType
        {
            get { return m_DeliveryType; }
            set
            {
                if(m_DeliveryType != value)
                {
                    m_DeliveryType = value;
                    SetDirty(true);
                };
            }
        }
        
        /// <inheritdoc/>
        public override void OnGUIMultiple(List<AddressableAssetGroupSchema> otherSchemas)
        {
            var so = new SerializedObject(this);
            var prop = so.FindProperty("m_DeliveryType");

            ShowMixedValue(prop, otherSchemas, typeof(bool), "m_DeliveryType");
            EditorGUI.BeginChangeCheck();
            DeliveryType current = m_DeliveryType;
            var newType = (DeliveryType)EditorGUILayout.EnumPopup("Delivery Type", current);
            if (EditorGUI.EndChangeCheck())
            {
                DeliveryType = newType;
                foreach (var s in otherSchemas)
                    (s as PlayAssetDeliverySchema).DeliveryType = newType;
            }
            EditorGUI.showMixedValue = false;

            so.ApplyModifiedProperties();
        }
    }
}
#endif