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
        public static string kDefaultConfigFolder = "Assets/PlayAssetDelivery/Data";
        public static string kDefaultConfigObjectName = "CustomAssetPackSettings";

        public static string kInstallTimePackName = "InstallTimeContent";
        public static string kDefaultPackName = "AssetPack";
        public static DeliveryType kDefaultDeliveryType = DeliveryType.OnDemand;

        public static string kDefaultSettingsPath
        {
            get
            {
                return $"{kDefaultConfigFolder}/{kDefaultConfigObjectName}.asset";
            }
        }

        [SerializeField]
        List<CustomAssetPackEditorInfo> m_CustomAssetPacks = new List<CustomAssetPackEditorInfo>();
        /// <summary>
        /// Store all custom asset pack information. By default it has an entry named "InstallTimeContent" that should be used for all install-time content.
        /// This is a "placeholder" asset pack that is representative of the generated asset packs. No custom asset pack named "InstallTimeContent" is actually created.
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
            string assetPackName = GenerateUniqueName(kDefaultPackName, CustomAssetPacks.Select(p => p.AssetPackName));
            AddCustomAssetPack(assetPackName, kDefaultDeliveryType);
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
            get { return !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(kDefaultSettingsPath)); }
        }

        public static CustomAssetPackSettings GetSettings(bool create)
        {
            var settings = AssetDatabase.LoadAssetAtPath<CustomAssetPackSettings>(kDefaultSettingsPath);
            if (create && settings == null)
            {
                settings = CreateInstance<CustomAssetPackSettings>();

                if (!AssetDatabase.IsValidFolder(kDefaultConfigFolder))
                    Directory.CreateDirectory(kDefaultConfigFolder);
                AssetDatabase.CreateAsset(settings, kDefaultSettingsPath);
                settings = AssetDatabase.LoadAssetAtPath<CustomAssetPackSettings>(kDefaultSettingsPath);

                // Entry used for all content marked for install-time delivery
                settings.AddCustomAssetPack(kInstallTimePackName, DeliveryType.InstallTime);

                AssetDatabase.SaveAssets();
            }
            return settings;
        }
    }
}
