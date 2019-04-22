using System;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace UnityEditor.AddressableAssets.GUI
{
    [CustomPropertyDrawer(typeof(ProfileValueReference), true)]
    class ProfileValueReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return;
            EditorGUI.BeginProperty(position, label, property);
            var idProp = property.FindPropertyRelative("m_Id");
           
            idProp.stringValue = ProfilesEditor.ValueGUI(position, settings, label.text, idProp.stringValue);
           
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return 0; 
            var idProp = property.FindPropertyRelative("m_Id");
            return ProfilesEditor.CalcGUIHeight(settings, label.text, idProp.stringValue);
        }
    }
}
