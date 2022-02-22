using System;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AddressablesPlayAssetDelivery.Editor
{
    [CustomEditor(typeof(CustomAssetPackSettings))]
    class CustomAssetPackSettingsInspector : UnityEditor.Editor
    {
        CustomAssetPackSettings m_Settings;
        [SerializeField]
        ReorderableList m_CustomAssetPacks;

        void OnEnable()
        {
            m_Settings = target as CustomAssetPackSettings;
            if (m_Settings == null)
                return;

            m_CustomAssetPacks = new ReorderableList(m_Settings.CustomAssetPacks, typeof(CustomAssetPackEditorInfo), true, true, true, true);
            m_CustomAssetPacks.drawElementCallback = DrawCustomAssetPackCallback;
            m_CustomAssetPacks.headerHeight = 0;
            m_CustomAssetPacks.onAddCallback = OnAddCustomAssetPack;
            m_CustomAssetPacks.onRemoveCallback = OnRemoveCustomAssetPack;
        }

        public override void OnInspectorGUI()
        {
            m_CustomAssetPacks.DoLayoutList();
        }

        void DrawCustomAssetPackCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            float halfW = rect.width * 0.4f;
            CustomAssetPackEditorInfo currentAssetPack = m_Settings.CustomAssetPacks[index];

            EditorGUI.BeginDisabledGroup(currentAssetPack.AssetPackName == CustomAssetPackSettings.kInstallTimePackName);

            string oldName = currentAssetPack.AssetPackName;
            var newName = EditorGUI.DelayedTextField(new Rect(rect.x, rect.y, halfW, rect.height), oldName);
            if (newName != oldName)
                m_Settings.SetAssetPackName(index, newName);

            DeliveryType oldType = currentAssetPack.DeliveryType;
            var newType = (DeliveryType)EditorGUI.EnumPopup(new Rect(rect.x + halfW, rect.y, rect.width - halfW, rect.height), new GUIContent(""), oldType, IsDeliveryTypeEnabled);
            if (oldType != newType)
                m_Settings.SetDeliveryType(index, newType);

            EditorGUI.EndDisabledGroup();
        }

        bool IsDeliveryTypeEnabled(Enum e)
        {
            var deliveryType = (DeliveryType)e;
            if (deliveryType != DeliveryType.InstallTime)
                return true;
            return false;
        }

        void OnAddCustomAssetPack(ReorderableList list)
        {
            m_Settings.AddUniqueAssetPack();
        }

        void OnRemoveCustomAssetPack(ReorderableList list)
        {
            if (m_Settings.CustomAssetPacks[list.index].AssetPackName == CustomAssetPackSettings.kInstallTimePackName)
                Debug.LogError($"Cannot delete asset pack name '{CustomAssetPackSettings.kInstallTimePackName}'. It represents the generated asset packs which will contain all install-time content.");
            else
                m_Settings.RemovePackAtIndex(list.index);
        }
    }
}
