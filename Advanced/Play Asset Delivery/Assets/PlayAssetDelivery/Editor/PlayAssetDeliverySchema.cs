#if UNITY_EDITOR
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace AddressablesPlayAssetDelivery.Editor
{
    [DisplayName("Play Asset Delivery")]
    public class PlayAssetDeliverySchema : AddressableAssetGroupSchema
    {
        [SerializeField]
        internal int m_AssetPackIndex = 0;
        public int AssetPackIndex
        {
            get 
            {
                return m_AssetPackIndex; 
            }
            set
            {
                if(m_AssetPackIndex != value)
                {
                    m_AssetPackIndex = value;
                    SetDirty(true);
                }
            }
        }

        internal CustomAssetPackSettings m_Settings;
        public CustomAssetPackSettings Settings
        {
            get
            {
                if (!CustomAssetPackSettings.SettingsExists)
                    Reset();
                if (m_Settings == null)
                {
                    m_Settings = CustomAssetPackSettings.GetSettings();
                    if(AssetPackIndex >= m_Settings.CustomAssetPacks.Count)
                        AssetPackIndex = 0;
                }
                return m_Settings;
            }
            set
            {
                if (m_Settings != value)
                    m_Settings = value;
            }
        }

        public void Reset()
        {
            AssetPackIndex = 0;
            m_Settings = null;
        }

        void ShowAssetPacks(SerializedObject so, List<AddressableAssetGroupSchema> otherSchemas = null)
        {
            List<CustomAssetPackEditorInfo> customAssetPacks = Settings.CustomAssetPacks;

            int current = AssetPackIndex;

            string[] displayOptions = new string[customAssetPacks.Count];
            for(int i = 0; i < customAssetPacks.Count; i++)
            {
                displayOptions[i] = $"{customAssetPacks[i].AssetPackName} ({customAssetPacks[i].DeliveryType})";
            }

            SerializedProperty prop = so.FindProperty("m_AssetPackIndex");
            if(otherSchemas != null)
                ShowMixedValue(prop, otherSchemas, typeof(int), "m_AssetPackIndex");

            EditorGUI.BeginChangeCheck();
            var newIndex = EditorGUILayout.Popup("Asset Pack", current, displayOptions);
            if (EditorGUI.EndChangeCheck())
            {
                AssetPackIndex = newIndex;
                CustomAssetPackEditorInfo newPack = customAssetPacks[AssetPackIndex];
                if(otherSchemas != null)
                {
                    foreach (AddressableAssetGroupSchema s in otherSchemas)
                    {
                        PlayAssetDeliverySchema padSchema = s as PlayAssetDeliverySchema;
                        padSchema.AssetPackIndex = newIndex;
                    }
                }
            }
            if(otherSchemas != null)
                EditorGUI.showMixedValue = false;
                
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Manage Asset Packs", "Minibutton"))
                {
                    EditorGUIUtility.PingObject(Settings);
                    Selection.activeObject = Settings;
                }
            }
        }
        
        /// <inheritdoc/>
        public override void OnGUI()
        {
            var so = new SerializedObject(this);
            ShowAssetPacks(so);
            so.ApplyModifiedProperties();
        }

        /// <inheritdoc/>
        public override void OnGUIMultiple(List<AddressableAssetGroupSchema> otherSchemas)
        {
            var so = new SerializedObject(this);
           
            ShowAssetPacks(so, otherSchemas);
            so.ApplyModifiedProperties();
        }
    }
}
#endif