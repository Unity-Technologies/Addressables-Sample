using System;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace UnityEditor.AddressableAssets.GUI
{
    [CustomPropertyDrawer(typeof(AssetLabelReference), true)]
    class AssetLabelReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var currentLabel = property.FindPropertyRelative("m_LabelString");
            var smallPos = EditorGUI.PrefixLabel(position, label);
            if (AddressableAssetSettingsDefaultObject.Settings == null)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUI.LabelField(smallPos, new GUIContent(currentLabel.stringValue));
            }
            else
            {
                var labelList = AddressableAssetSettingsDefaultObject.Settings.labelTable.labelNames.ToArray();
                var currIndex = Array.IndexOf(labelList, currentLabel.stringValue);
                var newIndex = EditorGUI.Popup(smallPos, currIndex, labelList);
                if (newIndex != currIndex)
                {
                    currentLabel.stringValue = labelList[newIndex];
                }
            }
            EditorGUI.EndProperty();
        }

    }

}
