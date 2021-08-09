#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AddressablesPlayAssetDelivery.Editor
{
    [Serializable]
    public class CustomAssetPackEditorInfo
    {
        public string AssetPackName;
        public DeliveryType DeliveryType;

        public CustomAssetPackEditorInfo(string assetPackName, DeliveryType deliveryType)
        {
            AssetPackName = assetPackName;
            DeliveryType = deliveryType;
        }
    }

    /// <summary>
    /// Stores information (name & delivery type) for all custom asset packs.
    /// </summary>
    public class CustomAssetPackSettings : ScriptableObject
    {
        public static string k_DefaultConfigFolder = "Assets/PlayAssetDelivery";
        public static string k_DefaultConfigObjectName = "CustomAssetPackSettings";

        public static string k_InstallTimePackName = "InstallTimeContent";
        public static string k_DefaultPackName = "AssetPack";
        public static DeliveryType k_DefaultDeliveryType = DeliveryType.OnDemand;

        public static string k_DefaultSettingsPath
        {
            get
            {
                return $"{k_DefaultConfigFolder}/{k_DefaultConfigObjectName}.asset";
            }
        }

        [SerializeField]
        List<CustomAssetPackEditorInfo> m_CustomAssetPacks = new List<CustomAssetPackEditorInfo>();
        /// <summary>
        /// Store all custom asset pack information. By default it has an entry named "InstallTimeContent" that should be used for all install-time content.
        /// This is a "placeholder" asset pack that is representative of the streaming assets pack. No custom asset pack named "InstallTimeContent" is actually created.
        /// </summary>
        public List<CustomAssetPackEditorInfo> CustomAssetPacks
        {
            get { return m_CustomAssetPacks; }
        }

        void AddCustomAssetPack(string assetPackName, DeliveryType deliveryType)
        {
            CustomAssetPacks.Add(new CustomAssetPackEditorInfo(assetPackName, deliveryType));
            EditorUtility.SetDirty(this);
        }

        public void AddUniqueAssetPack()
        {
            string assetPackName = GenerateUniqueName(k_DefaultPackName, CustomAssetPacks.Select(p => p.AssetPackName));
            AddCustomAssetPack(assetPackName, k_DefaultDeliveryType);
        }

        internal string GenerateUniqueName(string baseName, IEnumerable<string> enumerable)
        {
            var set = new HashSet<string>(enumerable);
            int counter = 1;
            var newName = baseName;
            while (set.Contains(newName))
            {
                newName = baseName + counter;
                counter++;
                if (counter == int.MaxValue)
                    throw new OverflowException();
            }
            return newName;
        }

        public void RemovePackAtIndex(int index)
        {
            CustomAssetPacks.RemoveAt(index);
            EditorUtility.SetDirty(this);
        }

        public static bool SettingsExists
        {
            get { return !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(k_DefaultSettingsPath)); }
        }

        public static CustomAssetPackSettings GetSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<CustomAssetPackSettings>(k_DefaultSettingsPath);
            if (settings == null)
            {
                settings = CreateInstance<CustomAssetPackSettings>();

                if (!AssetDatabase.IsValidFolder(k_DefaultConfigFolder))
                    Directory.CreateDirectory(k_DefaultConfigFolder);
                AssetDatabase.CreateAsset(settings, k_DefaultSettingsPath);
                settings = AssetDatabase.LoadAssetAtPath<CustomAssetPackSettings>(k_DefaultSettingsPath);

                // Entry used for all content marked for install-time delivery
                settings.AddCustomAssetPack(k_InstallTimePackName, DeliveryType.InstallTime);

                AssetDatabase.SaveAssets();
            }
            return settings;
        }
    }
}
#endif
