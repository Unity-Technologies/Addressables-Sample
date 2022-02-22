using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

        private Regex m_ValidAssetPackName = new Regex(@"^[A-Za-z][a-zA-Z0-9_]*$", RegexOptions.Compiled);

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

        public void SetAssetPackName(int index, string assetPackName)
        {
            if (index >= 0 && index < CustomAssetPacks.Count)
            {
                if (!m_ValidAssetPackName.IsMatch(assetPackName))
                {
                    Debug.LogError($"Cannot name custom asset pack '{assetPackName}'. All characters must be alphanumeric or an underscore. " +
                        $"Also the first character must be a letter.");
                }
                else
                {
                    string newName = GenerateUniqueName(assetPackName, CustomAssetPacks.Select(p => p.AssetPackName));
                    CustomAssetPacks[index].AssetPackName = newName;
                    EditorUtility.SetDirty(this);
                }
            }
        }

        public void SetDeliveryType(int index, DeliveryType deliveryType)
        {
            if (index >= 0 && index < CustomAssetPacks.Count)
            {
                CustomAssetPacks[index].DeliveryType = deliveryType;
                EditorUtility.SetDirty(this);
            }
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
