using System;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets
{
    /// <summary>
    /// Class used to get and set the default Addressable Asset settings object.
    /// </summary>
    public class AddressableAssetSettingsDefaultObject : ScriptableObject
    {
        /// <summary>
        /// Default name for the addressable assets settings
        /// </summary>
        public const string kDefaultConfigAssetName = "AddressableAssetSettings";
        /// <summary>
        /// The default folder for the serialized version of this class.
        /// </summary>
        public const string kDefaultConfigFolder = "Assets/AddressableAssetsData";
        /// <summary>
        /// The name of the default config object
        /// </summary>
        public const string kDefaultConfigObjectName = "com.unity.addressableassets";

        /// <summary>
        /// Default path for addressable asset settings assets.
        /// </summary>
        public static string DefaultAssetPath
        {
            get
            {
                return kDefaultConfigFolder + "/" + kDefaultConfigAssetName + ".asset";
            }
        }

        [FormerlySerializedAs("m_addressableAssetSettingsGuid")]
        [SerializeField]
        internal string m_AddressableAssetSettingsGuid;
        bool m_LoadingSettingsObject = false;

        internal AddressableAssetSettings LoadSettingsObject()
        {
            //prevent re-entrant stack overflow
            if (m_LoadingSettingsObject)
            {
                Debug.LogWarning("Detected stack overflow when accessing AddressableAssetSettingsDefaultObject.Settings object.");
                return null;
            }
            if (string.IsNullOrEmpty(m_AddressableAssetSettingsGuid))
            {
                Debug.LogError("Invalid guid for default AddressableAssetSettings object.");
                return null;
            }
            var path = AssetDatabase.GUIDToAssetPath(m_AddressableAssetSettingsGuid);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogErrorFormat("Unable to determine path for default AddressableAssetSettings object with guid {0}.", m_AddressableAssetSettingsGuid);
                return null;
            }
            m_LoadingSettingsObject = true;
            var settings = AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(path);
            if (settings != null)
                AddressablesAssetPostProcessor.OnPostProcess = settings.OnPostprocessAllAssets;
            m_LoadingSettingsObject = false;
            return settings;
        }

        void SetSettingsObject(AddressableAssetSettings settings)
        {
            if (settings == null)
            {
                m_AddressableAssetSettingsGuid = null;
                return;
            }
            var path = AssetDatabase.GetAssetPath(settings);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogErrorFormat("Unable to determine path for default AddressableAssetSettings object with guid {0}.", m_AddressableAssetSettingsGuid);
                return;
            }
            AddressablesAssetPostProcessor.OnPostProcess = settings.OnPostprocessAllAssets;
            m_AddressableAssetSettingsGuid = AssetDatabase.AssetPathToGUID(path);
        }

        static AddressableAssetSettings s_DefaultSettingsObject;

        /// <summary>
        /// Used to determine if a default settings asset exists.
        /// </summary>
        public static bool SettingsExists
        {
            get
            {
                AddressableAssetSettingsDefaultObject so;
                if (EditorBuildSettings.TryGetConfigObject(kDefaultConfigObjectName, out so))
                    return !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(so.m_AddressableAssetSettingsGuid));
                return false;
            }
        }

        /// <summary>
        /// Gets the default addressable asset settings object.  This will return null during editor startup if EditorApplication.isUpdating or EditorApplication.isCompiling are true.
        /// </summary>
        public static AddressableAssetSettings Settings
        {
            get
            {
                if (s_DefaultSettingsObject == null && !EditorApplication.isUpdating && !EditorApplication.isCompiling)
                {
                    AddressableAssetSettingsDefaultObject so;
                    if (EditorBuildSettings.TryGetConfigObject(kDefaultConfigObjectName, out so))
                    {
                        s_DefaultSettingsObject = so.LoadSettingsObject();
                    }
                    else
                    {
                        //legacy support, try to get the old config object and then remove it
                        if (EditorBuildSettings.TryGetConfigObject(kDefaultConfigAssetName, out s_DefaultSettingsObject))
                        {
                            EditorBuildSettings.RemoveConfigObject(kDefaultConfigAssetName);
                            so = CreateInstance<AddressableAssetSettingsDefaultObject>();
                            so.SetSettingsObject(s_DefaultSettingsObject);
                            AssetDatabase.CreateAsset(so, kDefaultConfigFolder + "/DefaultObject.asset");
                            EditorUtility.SetDirty(so);
                            AssetDatabase.SaveAssets();
                            EditorBuildSettings.AddConfigObject(kDefaultConfigObjectName, so, true);
                        }
                    }
                }
                return s_DefaultSettingsObject;
            }
            set
            {
                if (value != null)
                {
                    var path = AssetDatabase.GetAssetPath(value);
                    if (string.IsNullOrEmpty(path))
                    {
                        Debug.LogErrorFormat("AddressableAssetSettings object must be saved to an asset before it can be set as the default.");
                        return;
                    }
                }

                s_DefaultSettingsObject = value;
                AddressableAssetSettingsDefaultObject so;
                if (!EditorBuildSettings.TryGetConfigObject(kDefaultConfigObjectName, out so))
                {
                    so = CreateInstance<AddressableAssetSettingsDefaultObject>();
                    AssetDatabase.CreateAsset(so, kDefaultConfigFolder + "/DefaultObject.asset");
                    AssetDatabase.SaveAssets();
                    EditorBuildSettings.AddConfigObject(kDefaultConfigObjectName, so, true);
                }
                so.SetSettingsObject(s_DefaultSettingsObject);
                EditorUtility.SetDirty(so);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// Gets the settings object with the option to create a new one if it does not exist.
        /// </summary>
        /// <param name="create">If true and no settings object exists, a new one will be created using the default config folder and asset name.</param>
        /// <returns>The default settings object.</returns>
        public static AddressableAssetSettings GetSettings(bool create)
        {
            if (Settings == null && create)
                Settings = AddressableAssetSettings.Create(kDefaultConfigFolder, kDefaultConfigAssetName, true, true);
            return Settings;
        }

    }
}