using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.HostingServices;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

[assembly: InternalsVisibleTo("Unity.Addressables.Editor.Tests")]
[assembly: InternalsVisibleTo("Unity.Addressables.Tests")]

namespace UnityEditor.AddressableAssets.Settings
{
    /// <summary>
    /// Contains editor data for the addressables system.
    /// </summary>
    public class AddressableAssetSettings : ScriptableObject
    {
        [InitializeOnLoadMethod]
        static void RegisterWithAssetPostProcessor()
        {
            //if the Library folder has been deleted, this will be null and it will have to be set on the first access of the settings object
            if (AddressableAssetSettingsDefaultObject.Settings != null)
                AddressablesAssetPostProcessor.OnPostProcess = AddressableAssetSettingsDefaultObject.Settings.OnPostprocessAllAssets;
        }
        /// <summary>
        /// Default name of a newly created group.
        /// </summary>
        public const string kNewGroupName = "New Group";
        /// <summary>
        /// Default name of local build path.
        /// </summary>
        public const string kLocalBuildPath = "LocalBuildPath";
        /// <summary>
        /// Default name of local load path.
        /// </summary>
        public const string kLocalLoadPath = "LocalLoadPath";
        /// <summary>
        /// Default name of remote build path.
        /// </summary>
        public const string kRemoteBuildPath = "RemoteBuildPath";
        /// <summary>
        /// Default name of remote load path.
        /// </summary>
        public const string kRemoteLoadPath = "RemoteLoadPath";

        /// <summary>
        /// Enumeration of different event types that are generated.
        /// </summary>
        public enum ModificationEvent
        {
            GroupAdded,
            GroupRemoved,
            GroupRenamed,
            GroupSchemaAdded,
            GroupSchemaRemoved,
            GroupSchemaModified,
            GroupTemplateAdded,
            GroupTemplateRemoved,
            GroupTemplateSchemaAdded,
            GroupTemplateSchemaRemoved,
            EntryCreated,
            EntryAdded,
            EntryMoved,
            EntryRemoved,
            LabelAdded,
            LabelRemoved,
            ProfileAdded,
            ProfileRemoved,
            ProfileModified,
            ActiveProfileSet,
            EntryModified,
            BuildSettingsChanged,
            ActiveBuildScriptChanged,
            DataBuilderAdded,
            DataBuilderRemoved,
            InitializationObjectAdded,
            InitializationObjectRemoved,
            ActivePlayModeScriptChanged,
            BatchModification, // <-- posted object will be null.
            HostingServicesManagerModified
        }

        /// <summary>
        /// The path of the settings asset.
        /// </summary>
        public string AssetPath
        {
            get
            {
                string guid;
                long localId;
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(this, out guid, out localId))
                    return AddressableAssetSettingsDefaultObject.DefaultAssetPath;
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath))
                    return AddressableAssetSettingsDefaultObject.DefaultAssetPath;
                return assetPath;
            }
        }

        /// <summary>
        /// The folder of the settings asset.
        /// </summary>
        public string ConfigFolder
        {
            get
            {
                return Path.GetDirectoryName(AssetPath);
            }
        }

        /// <summary>
        /// The folder for the group assets.
        /// </summary>
        public string GroupFolder
        {
            get
            {
                return ConfigFolder + "/AssetGroups";
            }
        }
        /// <summary>
        /// The folder for the script assets.
        /// </summary>
        public string DataBuilderFolder
        {
            get
            {
                return ConfigFolder + "/DataBuilders";
            }
        }
        /// <summary>
        /// The folder for the asset group schema assets.
        /// </summary>
        public string GroupSchemaFolder
        {
            get
            {
                return GroupFolder + "/Schemas";
            }
        }
        
        /// <summary>
        /// The default folder for the group template assets.
        /// </summary>
        public string GroupTemplateFolder
        {
            get
            {
                return ConfigFolder + "/AssetGroupTemplates";
            }
        }
        
        /// <summary>
        /// Event for handling settings changes.  The object passed depends on the event type.
        /// </summary>
        public Action<AddressableAssetSettings, ModificationEvent, object> OnModification { get; set; }

        /// <summary>
        /// Event for handling settings changes on all instances of AddressableAssetSettings.  The object passed depends on the event type.
        /// </summary>
        public static event Action<AddressableAssetSettings, ModificationEvent, object> OnModificationGlobal;

        /// <summary>
        /// Event for handling the result of a DataBuilder.Build call.
        /// </summary>
        public Action<AddressableAssetSettings, IDataBuilder, IDataBuilderResult> OnDataBuilderComplete { get; set; }

        [FormerlySerializedAs("m_defaultGroup")]
        [SerializeField]
        string m_DefaultGroup;
        [FormerlySerializedAs("m_cachedHash")]
        [SerializeField]
        Hash128 m_CachedHash;

        bool m_IsTemporary;
        /// <summary>
        /// Returns whether this settings object is persisted to an asset.
        /// </summary>
        public bool IsPersisted { get { return !m_IsTemporary; } }

        [SerializeField]
        bool m_BuildRemoteCatalog = false;

        /// <summary>
        /// Determine if a remote catalog should be built-for and loaded-by the app.
        /// </summary>
        public bool BuildRemoteCatalog
        {
            get { return m_BuildRemoteCatalog; }
            set { m_BuildRemoteCatalog = value; }
        }

        [SerializeField]
        ProfileValueReference m_RemoteCatalogBuildPath;
        /// <summary>
        /// The path to place a copy of the content catalog for online retrieval.  To do any content updates
        /// to an existing built app, there must be a remote catalog. Overwriting the catalog is how the app
        /// gets informed of the updated content.
        /// </summary>
        public ProfileValueReference RemoteCatalogBuildPath
        {
            get
            {
                if (m_RemoteCatalogBuildPath.Id == null)
                {
                    m_RemoteCatalogBuildPath = new ProfileValueReference();
                    m_RemoteCatalogBuildPath.SetVariableByName(this, kRemoteBuildPath);
                }
                return m_RemoteCatalogBuildPath;
            }
            set { m_RemoteCatalogBuildPath = value; }
        }

        [SerializeField]
        ProfileValueReference m_RemoteCatalogLoadPath;
        /// <summary>
        /// The path to load the remote content catalog from.  This is the location the app will check to
        /// look for updated catalogs, which is the only indication the app has for updated content.
        /// </summary>
        public ProfileValueReference RemoteCatalogLoadPath
        {
            get
            {
                if (m_RemoteCatalogLoadPath.Id == null)
                {
                    m_RemoteCatalogLoadPath = new ProfileValueReference();
                    m_RemoteCatalogLoadPath.SetVariableByName(this, kRemoteLoadPath);
                }
                return m_RemoteCatalogLoadPath;
            }
            set { m_RemoteCatalogLoadPath = value; }
        }


        /// <summary>
        /// Hash of the current settings.  This value is recomputed if anything changes.
        /// </summary>
        public Hash128 currentHash
        {
            get
            {
                if (m_CachedHash.isValid)
                    return m_CachedHash;
                var stream = new MemoryStream();
                var formatter = new BinaryFormatter();
                m_BuildSettings.SerializeForHash(formatter, stream);
                formatter.Serialize(stream, activeProfileId);
                formatter.Serialize(stream, m_LabelTable);
                formatter.Serialize(stream, m_ProfileSettings);
                formatter.Serialize(stream, m_GroupAssets.Count);
                foreach (var g in m_GroupAssets)
                    g.SerializeForHash(formatter, stream);
                return (m_CachedHash = HashingMethods.Calculate(stream).ToHash128());
            }
        }

        internal void DataBuilderCompleted(IDataBuilder builder, IDataBuilderResult result)
        {
            if (OnDataBuilderComplete != null)
                OnDataBuilderComplete(this, builder, result);
        }

        /// <summary>
        /// Create an AssetReference object.  If the asset is not already addressable, it will be added.
        /// </summary>
        /// <param name="guid">The guid of the asset reference.</param>
        /// <returns>Returns the newly created AssetReference.</returns>
        public AssetReference CreateAssetReference(string guid)
        {
            CreateOrMoveEntry(guid, DefaultGroup);
            return new AssetReference(guid);
        }
        [SerializeField]
        string m_overridePlayerVersion = "";
        /// <summary>
        /// Allows for overriding the player version used to generated catalog names.
        /// </summary>
        public string OverridePlayerVersion
        {
            get { return m_overridePlayerVersion; }
            set { m_overridePlayerVersion = value; }
        }
        /// <summary>
        /// The version of the player build.  This is implemented as a timestamp int UTC of the form  string.Format("{0:D4}.{1:D2}.{2:D2}.{3:D2}.{4:D2}.{5:D2}", now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second).
        /// </summary>
        public string PlayerBuildVersion
        {
            get
            {
                if (!string.IsNullOrEmpty(m_overridePlayerVersion))
                    return profileSettings.EvaluateString(activeProfileId, m_overridePlayerVersion);
                var now = DateTime.UtcNow;
                return string.Format("{0:D4}.{1:D2}.{2:D2}.{3:D2}.{4:D2}.{5:D2}", now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
            }
        }

        [FormerlySerializedAs("m_groupAssets")]
        [SerializeField]
        List<AddressableAssetGroup> m_GroupAssets = new List<AddressableAssetGroup>();
        /// <summary>
        /// List of asset groups.
        /// </summary>
        public List<AddressableAssetGroup> groups { get { return m_GroupAssets; } }

        [FormerlySerializedAs("m_buildSettings")]
        [SerializeField]
        AddressableAssetBuildSettings m_BuildSettings = new AddressableAssetBuildSettings();
        /// <summary>
        /// Build settings object.
        /// </summary>
        public AddressableAssetBuildSettings buildSettings { get { return m_BuildSettings; } }

        [FormerlySerializedAs("m_profileSettings")]
        [SerializeField]
        AddressableAssetProfileSettings m_ProfileSettings = new AddressableAssetProfileSettings();
        /// <summary>
        /// Profile settings object.
        /// </summary>
        public AddressableAssetProfileSettings profileSettings { get { return m_ProfileSettings; } }

        [FormerlySerializedAs("m_labelTable")]
        [SerializeField]
        LabelTable m_LabelTable = new LabelTable();
        /// <summary>
        /// LabelTable object.
        /// </summary>
        internal LabelTable labelTable { get { return m_LabelTable; } }
        [FormerlySerializedAs("m_schemaTemplates")]
        [SerializeField]
        List<AddressableAssetGroupSchemaTemplate> m_SchemaTemplates = new List<AddressableAssetGroupSchemaTemplate>();

        /// <summary>
        /// Remove  the schema at the specified index.
        /// </summary>
        /// <param name="index">The index to remove at.</param>
        /// <param name="postEvent">Indicates if an even should be posted to the Addressables event system for this change.</param>
        /// <returns>True if the schema was removed.</returns>
        public bool RemoveSchemaTemplate(int index, bool postEvent = true)
        {
            if (index < 0 || index >= m_SchemaTemplates.Count)
            {
                Debug.LogWarningFormat("Invalid index for schema template: {0}.", index);
                return false;
            }
            var s = m_SchemaTemplates[index];
            m_SchemaTemplates.RemoveAt(index);
            SetDirty(ModificationEvent.GroupSchemaRemoved, s, postEvent);
            return true;
        }
        
         [SerializeField]
        List<ScriptableObject> m_GroupTemplateObjects = new List<ScriptableObject>();
        
        /// <summary>
        /// List of ScriptableObjects that implement the IGroupTemplate interface for providing new templates.
        /// For use in the AddressableAssetsWindow to display new groups to create
        /// </summary>
        public List<ScriptableObject> GroupTemplateObjects
        {
            get { return m_GroupTemplateObjects; }
        }

        /// <summary>
        /// Get the IGroupTemplate at the specified index.
        /// </summary>
        /// <param name="index">The index of the template object.</param>
        /// <returns>The AddressableAssetGroupTemplate object at the specified index.</returns>
        public IGroupTemplate GetGroupTemplateObject(int index)
        {
            if (m_GroupTemplateObjects.Count == 0)
                return null;
            if (index < 0 || index >= m_GroupTemplateObjects.Count)
            {
                Debug.LogWarningFormat("Invalid index for group template: {0}.", index);
                return null;
            }
            return m_GroupTemplateObjects[Mathf.Clamp(index, 0, m_GroupTemplateObjects.Count)] as IGroupTemplate;
        }

        /// <summary>
        /// Adds a AddressableAssetsGroupTemplate object.
        /// </summary>
        /// <param name="templateObject">The AddressableAssetGroupTemplate object to add.</param>
        /// <param name="postEvent">Indicates if an even should be posted to the Addressables event system for this change.</param>
        /// <returns>True if the initialization object was added.</returns>
        public bool AddGroupTemplateObject(IGroupTemplate templateObject, bool postEvent = true)
        {
            if (templateObject == null)
            {
                Debug.LogWarning("Cannot add null IGroupTemplate");
                return false;
            }
            var so = templateObject as ScriptableObject;
            if (so == null)
            {
                Debug.LogWarning("Group Template objects must inherit from ScriptableObject.");
                return false;
            }

            m_GroupTemplateObjects.Add(so);
            SetDirty(ModificationEvent.GroupTemplateAdded, so, postEvent);
            return true;
        }

        /// <summary>
        /// Remove the AddressableAssetGroupTemplate object at the specified index.
        /// </summary>
        /// <param name="index">The index to remove.</param>
        /// <param name="postEvent">Indicates if an event should be posted to the Addressables event system for this change.</param>
        /// <returns>True if the initialization object was removed.</returns>
        public bool RemoveGroupTemplateObject(int index, bool postEvent = true)
        {
            if (m_GroupTemplateObjects.Count <= index)
                return false;
            var so = m_GroupTemplateObjects[index];
            m_GroupTemplateObjects.RemoveAt(index);
            SetDirty(ModificationEvent.GroupTemplateRemoved, so, postEvent);
            return true;
        }

        /// <summary>
        /// Sets the initialization object at the specified index.
        /// </summary>
        /// <param name="index">The index to set the initialization object.</param>
        /// <param name="initObject">The initialization object to set.  This must be a valid scriptable object that implements the IInitializationObject interface.</param>
        /// <param name="postEvent">Indicates if an even should be posted to the Addressables event system for this change.</param>
        /// <returns>True if the initialization object was set, false otherwise.</returns>
        public bool SetGroupTemplateObjectAtIndex(int index, IGroupTemplate initObject, bool postEvent = true)
        {
            if (m_GroupTemplateObjects.Count <= index)
                return false;
            if (initObject == null)
            {
                Debug.LogWarning("Cannot add null IGroupTemplate");
                return false;
            }
            var so = initObject as ScriptableObject;
            if (so == null)
            {
                Debug.LogWarning("AddressableAssetGroupTemplate objects must inherit from ScriptableObject.");
                return false;
            }
        
            m_GroupTemplateObjects[index] = so;
            SetDirty(ModificationEvent.GroupTemplateAdded, so, postEvent);
            return true;
        }


        [FormerlySerializedAs("m_initializationObjects")]
        [SerializeField]
        List<ScriptableObject> m_InitializationObjects = new List<ScriptableObject>();
        /// <summary>
        /// List of ScriptableObjects that implement the IObjectInitializationDataProvider interface for providing runtime initialization.
        /// </summary>
        public List<ScriptableObject> InitializationObjects
        {
            get { return m_InitializationObjects; }
        }

        /// <summary>
        /// Get the IObjectInitializationDataProvider at a specifc index.
        /// </summary>
        /// <param name="index">The index of the initialization object.</param>
        /// <returns>The initialization object at the specified index.</returns>
        public IObjectInitializationDataProvider GetInitializationObject(int index)
        {
            if (m_InitializationObjects.Count == 0)
                return null;
            if (index < 0 || index >= m_InitializationObjects.Count)
            {
                Debug.LogWarningFormat("Invalid index for data builder: {0}.", index);
                return null;
            }
            return m_InitializationObjects[Mathf.Clamp(index, 0, m_InitializationObjects.Count)] as IObjectInitializationDataProvider;
        }

        /// <summary>
        /// Adds an initialization object.
        /// </summary>
        /// <param name="initObject">The initialization object to add.</param>
        /// <param name="postEvent">Indicates if an even should be posted to the Addressables event system for this change.</param>
        /// <returns>True if the initialization object was added.</returns>
        public bool AddInitializationObject(IObjectInitializationDataProvider initObject, bool postEvent = true)
        {
            if (initObject == null)
            {
                Debug.LogWarning("Cannot add null IObjectInitializationDataProvider");
                return false;
            }
            var so = initObject as ScriptableObject;
            if (so == null)
            {
                Debug.LogWarning("Initialization objects must inherit from ScriptableObject.");
                return false;
            }

            m_InitializationObjects.Add(so);
            SetDirty(ModificationEvent.InitializationObjectAdded, so, postEvent);
            return true;
        }

        /// <summary>
        /// Remove the initialization object at the specified index.
        /// </summary>
        /// <param name="index">The index to remove.</param>
        /// <param name="postEvent">Indicates if an even should be posted to the Addressables event system for this change.</param>
        /// <returns>True if the initialization object was removed.</returns>
        public bool RemoveInitializationObject(int index, bool postEvent = true)
        {
            if (m_InitializationObjects.Count <= index)
                return false;
            var so = m_InitializationObjects[index];
            m_InitializationObjects.RemoveAt(index);
            SetDirty(ModificationEvent.InitializationObjectRemoved, so, postEvent);
            return true;
        }

        /// <summary>
        /// Sets the initialization object at the specified index.
        /// </summary>
        /// <param name="index">The index to set the initialization object.</param>
        /// <param name="initObject">The initialization object to set.  This must be a valid scriptable object that implements the IInitializationObject interface.</param>
        /// <param name="postEvent">Indicates if an even should be posted to the Addressables event system for this change.</param>
        /// <returns>True if the initialization object was set, false otherwise.</returns>
        public bool SetInitializationObjectAtIndex(int index, IObjectInitializationDataProvider initObject, bool postEvent = true)
        {
            if (m_InitializationObjects.Count <= index)
                return false;
            if (initObject == null)
            {
                Debug.LogWarning("Cannot add null IObjectInitializationDataProvider");
                return false;
            }
            var so = initObject as ScriptableObject;
            if (so == null)
            {
                Debug.LogWarning("Initialization objects must inherit from ScriptableObject.");
                return false;
            }

            m_InitializationObjects[index] = so;
            SetDirty(ModificationEvent.InitializationObjectAdded, so, postEvent);
            return true;
        }


        [SerializeField]
        [SerializedTypeRestriction(type = typeof(UnityEngine.Networking.CertificateHandler))]
        SerializedType m_CertificateHandlerType;

        /// <summary>
        /// The type of CertificateHandler to use for this provider.
        /// </summary>
        public Type CertificateHandlerType
        {
            get
            {
                return m_CertificateHandlerType.Value;
            }
            set
            {
                m_CertificateHandlerType.Value = value;
            }
        }

        [FormerlySerializedAs("m_activePlayerDataBuilderIndex")]
        [SerializeField]
        int m_ActivePlayerDataBuilderIndex = 3;
        [FormerlySerializedAs("m_activePlayModeDataBuilderIndex")]
        [SerializeField]
        int m_ActivePlayModeDataBuilderIndex;
        [FormerlySerializedAs("m_dataBuilders")]
        [SerializeField]
        List<ScriptableObject> m_DataBuilders = new List<ScriptableObject>();
        /// <summary>
        /// List of ScriptableObjects that implement the IDataBuilder interface.  These are used to create data for editor play mode and for player builds.
        /// </summary>
        public List<ScriptableObject> DataBuilders { get { return m_DataBuilders; } }
        /// <summary>
        /// Get The data builder at a specifc index.
        /// </summary>
        /// <param name="index">The index of the builder.</param>
        /// <returns>The data builder at the specified index.</returns>
        public IDataBuilder GetDataBuilder(int index)
        {
            if (m_DataBuilders.Count == 0)
                return null;
            if (index < 0 || index >= m_DataBuilders.Count)
            {
                Debug.LogWarningFormat("Invalid index for data builder: {0}.", index);
                return null;
            }
            return m_DataBuilders[Mathf.Clamp(index, 0, m_DataBuilders.Count)] as IDataBuilder;
        }

        /// <summary>
        /// Adds a data builder.
        /// </summary>
        /// <param name="builder">The data builder to add.</param>
        /// <param name="postEvent">Indicates if an even should be posted to the Addressables event system for this change.</param>
        /// <returns>True if the data builder was added.</returns>
        public bool AddDataBuilder(IDataBuilder builder, bool postEvent = true)
        {
            if (builder == null)
            {
                Debug.LogWarning("Cannot add null IDataBuilder");
                return false;
            }
            var so = builder as ScriptableObject;
            if (so == null)
            {
                Debug.LogWarning("Data builders must inherit from ScriptableObject.");
                return false;
            }

            m_DataBuilders.Add(so);
            SetDirty(ModificationEvent.DataBuilderAdded, so, postEvent);
            return true;
        }

        /// <summary>
        /// Remove the data builder at the sprcified index.
        /// </summary>
        /// <param name="index">The index to remove.</param>
        /// <param name="postEvent">Indicates if an even should be posted to the Addressables event system for this change.</param>
        /// <returns>True if the builder was removed.</returns>
        public bool RemoveDataBuilder(int index, bool postEvent = true)
        {
            if (m_DataBuilders.Count <= index)
                return false;
            var so = m_DataBuilders[index];
            m_DataBuilders.RemoveAt(index);
            SetDirty(ModificationEvent.DataBuilderRemoved, so, postEvent);
            return true;
        }

        /// <summary>
        /// Sets the data builder at the specified index.
        /// </summary>
        /// <param name="index">The index to set the builder.</param>
        /// <param name="builder">The builder to set.  This must be a valid scriptable object that implements the IDataBuilder interface.</param>
        /// <param name="postEvent">Indicates if an even should be posted to the Addressables event system for this change.</param>
        /// <returns>True if the builder was set, false otherwise.</returns>
        public bool SetDataBuilderAtIndex(int index, IDataBuilder builder, bool postEvent = true)
        {
            if (m_DataBuilders.Count <= index)
                return false;
            if (builder == null)
            {
                Debug.LogWarning("Cannot add null IDataBuilder");
                return false;
            }
            var so = builder as ScriptableObject;
            if (so == null)
            {
                Debug.LogWarning("Data builders must inherit from ScriptableObject.");
                return false;
            }

            m_DataBuilders[index] = so;
            SetDirty(ModificationEvent.DataBuilderAdded, so, postEvent);
            return true;
        }

        /// <summary>
        /// Get the active data builder for player data.
        /// </summary>
        public IDataBuilder ActivePlayerDataBuilder
        {
            get
            {
                return GetDataBuilder(m_ActivePlayerDataBuilderIndex);
            }
        }

        /// <summary>
        /// Get the active data builder for editor play mode data.
        /// </summary>
        public IDataBuilder ActivePlayModeDataBuilder
        {
            get
            {
                return GetDataBuilder(m_ActivePlayModeDataBuilderIndex);
            }
        }

        /// <summary>
        /// Get the index of the active player data builder.
        /// </summary>
        public int ActivePlayerDataBuilderIndex
        {
            get
            {
                return m_ActivePlayerDataBuilderIndex;
            }
            set
            {
                m_ActivePlayerDataBuilderIndex = value;
                SetDirty(ModificationEvent.ActiveBuildScriptChanged, ActivePlayerDataBuilder, true);
            }
        }

        /// <summary>
        /// Get the index of the active play mode data builder.
        /// </summary>
        public int ActivePlayModeDataBuilderIndex
        {
            get
            {
                return m_ActivePlayModeDataBuilderIndex;
            }
            set
            {
                m_ActivePlayModeDataBuilderIndex = value;
                SetDirty(ModificationEvent.ActivePlayModeScriptChanged, ActivePlayModeDataBuilder, true);
            }
        }


        /// <summary>
        /// Add a new label.
        /// </summary>
        /// <param name="label">The label name.</param>
        /// <param name="postEvent">Send modification event.</param>
        public void AddLabel(string label, bool postEvent = true)
        {
            m_LabelTable.AddLabelName(label);
            SetDirty(ModificationEvent.LabelAdded, label, postEvent);
        }

        /// <summary>
        /// Remove a label by name.
        /// </summary>
        /// <param name="label">The label name.</param>
        /// <param name="postEvent">Send modification event.</param>
        public void RemoveLabel(string label, bool postEvent = true)
        {
            m_LabelTable.RemoveLabelName(label);
            SetDirty(ModificationEvent.LabelRemoved, label, postEvent);
        }

        [FormerlySerializedAs("m_activeProfileId")]
        [SerializeField]
        string m_ActiveProfileId;
        /// <summary>
        /// The active profile id.
        /// </summary>
        public string activeProfileId
        {
            get
            {
                if (string.IsNullOrEmpty(m_ActiveProfileId))
                    m_ActiveProfileId = m_ProfileSettings.CreateDefaultProfile();
                return m_ActiveProfileId;
            }
            set
            {
                var oldVal = m_ActiveProfileId;
                m_ActiveProfileId = value;

                if (oldVal != value)
                {
                    SetDirty(ModificationEvent.ActiveProfileSet, value, true);
                }
            }
        }

        [FormerlySerializedAs("m_hostingServicesManager")]
        [SerializeField]
        HostingServicesManager m_HostingServicesManager;
        /// <summary>
        /// Get the HostingServicesManager object.
        /// </summary>
        public HostingServicesManager HostingServicesManager
        {
            get
            {
                if (m_HostingServicesManager == null)
                    m_HostingServicesManager = new HostingServicesManager();

                if (!m_HostingServicesManager.IsInitialized)
                    m_HostingServicesManager.Initialize(this);

                return m_HostingServicesManager;
            }

            // For unit tests
            internal set { m_HostingServicesManager = value; }
        }

        /// <summary>
        /// Gets all asset entries from all groups.
        /// </summary>
        /// <param name="assets">The list of asset entries.</param>
        /// <param name="groupFilter">A method to filter groups.  Groups will be processed if filter is null, or it returns TRUE</param>
        /// <param name="entryFilter">A method to filter entries.  Entries will be processed if filter is null, or it returns TRUE</param>
        public void GetAllAssets(List<AddressableAssetEntry> assets, Func<AddressableAssetGroup, bool> groupFilter = null, Func<AddressableAssetEntry, bool> entryFilter = null)
        {
            foreach (var g in groups)
                if (groupFilter == null || groupFilter(g))
                    g.GatherAllAssets(assets, true, true, entryFilter);
        }

        /// <summary>
        /// Remove an asset entry.
        /// </summary>
        /// <param name="guid">The  guid of the asset.</param>
        /// <param name="postEvent">Send modifcation event.</param>
        /// <returns>True if the entry was found and removed.</returns>
        public bool RemoveAssetEntry(string guid, bool postEvent = true)
        {
            var entry = FindAssetEntry(guid);
            if (entry != null)
            {
                if (entry.parentGroup != null)
                    entry.parentGroup.RemoveAssetEntry(entry, postEvent);
                SetDirty(ModificationEvent.EntryRemoved, entry, postEvent);
                return true;
            }
            return false;
        }

        void OnEnable()
        {
            profileSettings.OnAfterDeserialize(this);
            buildSettings.OnAfterDeserialize(this);
            Validate();
            HostingServicesManager.OnEnable();
        }

        void OnDisable()
        {
            HostingServicesManager.OnDisable();
        }

        private string m_DefaultGroupTemplateName = "Packed Assets";
        void Validate()
        {
            // Begin update any SchemaTemplate to GroupTemplateObjects
            if (m_SchemaTemplates != null && m_SchemaTemplates.Count > 0)
            {
                for (int i = m_SchemaTemplates.Count - 1; i >= 0; --i)
                {
                    string assetPath = GroupTemplateFolder + "/" + m_SchemaTemplates[i].DisplayName + ".asset";
                    if (File.Exists(assetPath))
                    {
                        if(LoadGroupTemplateObject(this, assetPath))
                            m_SchemaTemplates.RemoveAt(i);
                    }
                    else
                    {
                        if (CreateAndAddGroupTemplate(m_SchemaTemplates[i].DisplayName,
                                m_SchemaTemplates[i].Description, m_SchemaTemplates[i].GetTypes()))
                            m_SchemaTemplates.RemoveAt(i);
                    }
                }
                m_SchemaTemplates = null;
            }
            if (m_GroupTemplateObjects.Count == 0)
                CreateDefaultGroupTemplate(this);
            // End update of SchemaTemplate to GroupTemplates

            if (m_BuildSettings == null)
                m_BuildSettings = new AddressableAssetBuildSettings();
            if (m_ProfileSettings == null)
                m_ProfileSettings = new AddressableAssetProfileSettings();
            if (m_LabelTable == null)
                m_LabelTable = new LabelTable();
            if (string.IsNullOrEmpty(m_ActiveProfileId))
                m_ActiveProfileId = m_ProfileSettings.CreateDefaultProfile();
            if (m_DataBuilders == null || m_DataBuilders.Count == 0)
            {
                m_DataBuilders = new List<ScriptableObject>();
                m_DataBuilders.Add(CreateScriptAsset<BuildScriptFastMode>());
                m_DataBuilders.Add(CreateScriptAsset<BuildScriptVirtualMode>());
                m_DataBuilders.Add(CreateScriptAsset<BuildScriptPackedPlayMode>());
                m_DataBuilders.Add(CreateScriptAsset<BuildScriptPackedMode>());
            }

            if (ActivePlayerDataBuilder != null && !ActivePlayerDataBuilder.CanBuildData<AddressablesPlayerBuildResult>())
                ActivePlayerDataBuilderIndex = m_DataBuilders.IndexOf(m_DataBuilders.Find(s => s.GetType() == typeof(BuildScriptPackedMode)));
            if (ActivePlayModeDataBuilder != null && !ActivePlayModeDataBuilder.CanBuildData<AddressablesPlayModeBuildResult>())
                ActivePlayModeDataBuilderIndex = m_DataBuilders.IndexOf(m_DataBuilders.Find(s => s.GetType() == typeof(BuildScriptFastMode)));

            profileSettings.Validate(this);
            buildSettings.Validate(this);
        }

        T CreateScriptAsset<T>() where T : ScriptableObject
        {
            var script = CreateInstance<T>();
            if (!Directory.Exists(DataBuilderFolder))
                Directory.CreateDirectory(DataBuilderFolder);
            var path = DataBuilderFolder + "/" + typeof(T).Name + ".asset";
            if (!File.Exists(path))
                AssetDatabase.CreateAsset(script, path);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        internal const string PlayerDataGroupName = "Built In Data";
        internal const string DefaultLocalGroupName = "Default Local Group";

        /// <summary>
        /// Create a new AddressableAssetSettings object.
        /// </summary>
        /// <param name="configFolder">The folder to store the settings object.</param>
        /// <param name="configName">The name of the settings object.</param>
        /// <param name="createDefaultGroups">If true, create groups for player data and local packed content.</param>
        /// <param name="isPersisted">If true, assets are created.</param>
        /// <returns></returns>
        public static AddressableAssetSettings Create(string configFolder, string configName, bool createDefaultGroups, bool isPersisted)
        {
            AddressableAssetSettings aa;
            var path = configFolder + "/" + configName + ".asset";
            aa = isPersisted ? AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(path) : null;
            if (aa == null)
            {
                aa = CreateInstance<AddressableAssetSettings>();
                aa.m_IsTemporary = !isPersisted;
                aa.activeProfileId = aa.profileSettings.Reset();
                aa.name = configName;

                if (isPersisted)
                {
                    Directory.CreateDirectory(configFolder);
                    AssetDatabase.CreateAsset(aa, path);
                }

                if (createDefaultGroups)
                {
                    CreateBuiltInData(aa);
                    CreateDefaultGroup(aa);
                }

                if (isPersisted)
                    AssetDatabase.SaveAssets();
            }
            return aa;
        }

        /// <summary>
        /// Creates a new AddressableAssetGroupTemplate Object with the set of schema types with default settings for use in the editor GUI.
        /// </summary>
        /// <param name="displayName">The display name of the template.</param>
        /// <param name="description">Description text use with the template.</param>
        /// <param name="types">The schema types for the template.</param>
        /// <returns>True if the template was added, false otherwise.</returns>
        public bool CreateAndAddGroupTemplate(string displayName, string description, params Type[] types)
        {
            string assetPath = GroupTemplateFolder + "/" + displayName + ".asset";

            if (!CanCreateGroupTemplate(displayName, assetPath, types))
                return false;

            if (!Directory.Exists(GroupTemplateFolder))
                Directory.CreateDirectory(GroupTemplateFolder);
            
            AddressableAssetGroupTemplate newAssetGroupTemplate = ScriptableObject.CreateInstance<AddressableAssetGroupTemplate>();
            newAssetGroupTemplate.Description = description;

            AssetDatabase.CreateAsset(newAssetGroupTemplate, assetPath);
            AssetDatabase.SaveAssets();

            AddGroupTemplateObject(newAssetGroupTemplate);

            foreach (Type type in types)
                newAssetGroupTemplate.AddSchema(type);

            

            return true;
        }

        private bool CanCreateGroupTemplate(string displayName, string assetPath, Type[] types)
        {
            if (string.IsNullOrEmpty(displayName))
            {
                Debug.LogWarningFormat("CreateAndAddGroupTemplate - Group template must have a valid name.");
                return false;
            }
            if (types.Length == 0)
            {
                Debug.LogWarningFormat("CreateAndAddGroupTemplate - Group template {0} must contain at least 1 schema type.", displayName);
                return false;
            }
            bool typesAreValid = true;
            for (int i = 0; i < types.Length; i++)
            {
                var t = types[i];
                if (t == null)
                {
                    Debug.LogWarningFormat("CreateAndAddGroupTemplate - Group template {0} schema type at index {1} is null.", displayName, i);
                    typesAreValid = false;
                }
                else if (!typeof(AddressableAssetGroupSchema).IsAssignableFrom(t))
                {
                    Debug.LogWarningFormat("CreateAndAddGroupTemplate - Group template {0} schema type at index {1} must inherit from AddressableAssetGroupSchema.  Specified type was {2}.", displayName, i, t.FullName);
                    typesAreValid = false;
                }
            }
            if (!typesAreValid)
            {
                Debug.LogWarningFormat("CreateAndAddGroupTemplate - Group template {0} must contains at least 1 invalid schema type.", displayName);
                return false;
            }

            if (File.Exists(assetPath))
            {
                Debug.LogWarningFormat("CreateAndAddGroupTemplate - Group template {0} already exists at location {1}.", displayName, assetPath);
                return false;
            }

            return true;
        }


        /// <summary>
        /// Find asset group by functor.
        /// </summary>
        /// <param name="func">The functor to call on each group.  The first group that evaluates to true is returned.</param>
        /// <returns>The group found or null.</returns>
        public AddressableAssetGroup FindGroup(Func<AddressableAssetGroup, bool> func)
        {
            return groups.Find(g => func(g));
        }


        /// <summary>
        /// Find asset group by name.
        /// </summary>
        /// <param name="groupName">The name of the group.</param>
        /// <returns>The group found or null.</returns>
        public AddressableAssetGroup FindGroup(string groupName)
        {
            return FindGroup(g => g.Name == groupName);
        }

        /// <summary>
        /// The default group.  This group is used when marking assets as addressable via the inspector.
        /// </summary>
        public AddressableAssetGroup DefaultGroup
        {
            get
            {
                AddressableAssetGroup group = null;
                if (string.IsNullOrEmpty(m_DefaultGroup))
                    group = groups.Find(s => s.CanBeSetAsDefault());
                else
                {
                    group = groups.Find(s => s.Guid == m_DefaultGroup);
                    if (group == null || !group.CanBeSetAsDefault())
                    {
                        group = groups.Find(s => s.CanBeSetAsDefault());
                        if (group != null)
                            m_DefaultGroup = group.Guid;
                    }
                }

                if (group == null)
                {
                    Addressables.LogWarning("A valid default group could not be found.  One will be created.");
                    group = CreateDefaultGroup(this);
                }

                return group;
            }
            set
            {
                if (value == null)
                    Addressables.LogError("Unable to set null as the Default Group.  Default Groups must not be ReadOnly.");

                else if (!value.CanBeSetAsDefault())
                    Addressables.LogError("Unable to set " + value.Name + " as the Default Group.  Default Groups must not be ReadOnly.");
                else
                    m_DefaultGroup = value.Guid;
            }
        }

        private static AddressableAssetGroup CreateBuiltInData(AddressableAssetSettings aa)
        {
            var playerData = aa.CreateGroup(PlayerDataGroupName, false, false, false, null, typeof(PlayerDataGroupSchema));
            var resourceEntry = aa.CreateOrMoveEntry(AddressableAssetEntry.ResourcesName, playerData);
            resourceEntry.IsInResources = true;
            aa.CreateOrMoveEntry(AddressableAssetEntry.EditorSceneListName, playerData);
            return playerData;
        }

        private static AddressableAssetGroup CreateDefaultGroup(AddressableAssetSettings aa)
        {
            var localGroup = aa.CreateGroup(DefaultLocalGroupName, true, false, false, null, typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema));
            var schema = localGroup.GetSchema<BundledAssetGroupSchema>();
            schema.BuildPath.SetVariableByName(aa, kLocalBuildPath);
            schema.LoadPath.SetVariableByName(aa, kLocalLoadPath);
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            aa.m_DefaultGroup = localGroup.Guid;
            return localGroup;
        }
        
        private static bool CreateDefaultGroupTemplate( AddressableAssetSettings aa)
        {
            string assetPath = aa.GroupTemplateFolder + "/" + aa.m_DefaultGroupTemplateName + ".asset";

            if (File.Exists(assetPath))
                return LoadGroupTemplateObject(aa, assetPath);

            return aa.CreateAndAddGroupTemplate(aa.m_DefaultGroupTemplateName, "Pack assets into asset bundles.", typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
        }

        private static bool LoadGroupTemplateObject(AddressableAssetSettings aa, string assetPath)
        {
            return aa.AddGroupTemplateObject(AssetDatabase.LoadAssetAtPath(assetPath, typeof(ScriptableObject)) as IGroupTemplate);
        }

        AddressableAssetEntry CreateEntry(string guid, string address, AddressableAssetGroup parent, bool readOnly, bool postEvent = true)
        {
            var entry = new AddressableAssetEntry(guid, address, parent, readOnly);
            if (!readOnly)
                SetDirty(ModificationEvent.EntryCreated, entry, postEvent);
            return entry;
        }

        /// <summary>
        /// Marks the object as modified.
        /// </summary>
        /// <param name="modificationEvent">The event type that is changed.</param>
        /// <param name="eventData">The object data that corresponds to the event.</param>
        /// <param name="postEvent">If true, the event is propagated to callbacks.</param>
        public void SetDirty(ModificationEvent modificationEvent, object eventData, bool postEvent)
        {
            if (modificationEvent == ModificationEvent.ProfileRemoved && eventData as string == activeProfileId)
                activeProfileId = null;
            if (this != null)
            {
                if (postEvent)
                {
                    if (OnModificationGlobal != null)
                        OnModificationGlobal(this, modificationEvent, eventData);
                    if (OnModification != null)
                        OnModification(this, modificationEvent, eventData);
                }

                var unityObj = eventData as Object;
                if (unityObj != null && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(unityObj)))
                    EditorUtility.SetDirty(unityObj);

                if (IsPersisted)
                    EditorUtility.SetDirty(this);
            }

            m_CachedHash = default(Hash128);
        }

        /// <summary>
        /// Find and asset entry by guid.
        /// </summary>
        /// <param name="guid">The asset guid.</param>
        /// <returns>The found entry or null.</returns>
        public AddressableAssetEntry FindAssetEntry(string guid)
        {
            foreach (var g in groups)
            {
                var e = g.GetAssetEntry(guid);
                if (e != null)
                    return e;
            }
            return null;
        }

        internal void MoveAssetsFromResources(Dictionary<string, string> guidToNewPath, AddressableAssetGroup targetParent)
        {
            if (guidToNewPath == null)
                return;
            var entries = new List<AddressableAssetEntry>();
            AssetDatabase.StartAssetEditing();
            foreach (var item in guidToNewPath)
            {

                var dirInfo = new FileInfo(item.Value).Directory;
                if (dirInfo != null && !dirInfo.Exists)
                {
                    dirInfo.Create();
                    AssetDatabase.StopAssetEditing();
                    AssetDatabase.Refresh();
                    AssetDatabase.StartAssetEditing();
                }

                var oldPath = AssetDatabase.GUIDToAssetPath(item.Key);
                var errorStr = AssetDatabase.MoveAsset(oldPath, item.Value);
                if (!string.IsNullOrEmpty(errorStr))
                {
                    Addressables.LogError("Error moving asset: " + errorStr);
                }
                else
                {
                    AddressableAssetEntry e = FindAssetEntry(item.Key);
                    if (e != null)
                        e.IsInResources = false;

                    var newEntry = CreateOrMoveEntry(item.Key, targetParent, false, false);
                    var index = oldPath.ToLower().LastIndexOf("resources/");
                    if (index >= 0)
                    {
                        var newAddress = Path.GetFileNameWithoutExtension(oldPath.Substring(index + 10));
                        if (!string.IsNullOrEmpty(newAddress))
                        {
                            newEntry.address = newAddress;
                        }
                    }
                    entries.Add(newEntry);
                }

            }
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
            SetDirty(ModificationEvent.EntryMoved, entries, true);
        }

        /// <summary>
        /// Move an existing entry to a group.
        /// </summary>
        /// <param name="entries">The entries to move.</param>
        /// <param name="targetParent">The group to add the entries to.</param>
        /// <param name="readOnly">Should the entries be read only.</param>
        /// <param name="postEvent">Send modification event.</param>
        /// <returns></returns>
        public void MoveEntries(List<AddressableAssetEntry> entries, AddressableAssetGroup targetParent, bool readOnly = false, bool postEvent = true)
        {
            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    MoveEntry(entry, targetParent, readOnly, false);
                }

                SetDirty(ModificationEvent.EntryMoved, entries, postEvent);
            }
        }

        /// <summary>
        /// Move an existing entry to a group.
        /// </summary>
        /// <param name="entry">The entry to move.</param>
        /// <param name="targetParent">The group to add the entry to.</param>
        /// <param name="readOnly">Should the entry be read only.</param>
        /// <param name="postEvent">Send modification event.</param>
        /// <returns></returns>
        public void MoveEntry(AddressableAssetEntry entry, AddressableAssetGroup targetParent, bool readOnly = false, bool postEvent = true)
        {
            if (targetParent == null || entry == null)
                return;

            entry.ReadOnly = readOnly;

            if (entry.parentGroup != null && entry.parentGroup != targetParent)
                entry.parentGroup.RemoveAssetEntry(entry, postEvent);

            targetParent.AddAssetEntry(entry, postEvent);
        }

        /// <summary>
        /// Create a new entry, or if one exists in a different group, move it into the new group.
        /// </summary>
        /// <param name="guid">The asset guid.</param>
        /// <param name="targetParent">The group to add the entry to.</param>
        /// <param name="readOnly">Is the new entry read only.</param>
        /// <param name="postEvent">Send modification event.</param>
        /// <returns></returns>
        public AddressableAssetEntry CreateOrMoveEntry(string guid, AddressableAssetGroup targetParent, bool readOnly = false, bool postEvent = true)
        {
            if (targetParent == null || string.IsNullOrEmpty(guid))
                return null;

            AddressableAssetEntry entry = FindAssetEntry(guid);
            if (entry != null) //move entry to where it should go...
            {
                MoveEntry(entry, targetParent, readOnly, postEvent);
            }
            else //create entry
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AddressableAssetUtility.IsPathValidForEntry(path))
                {
                    entry = CreateEntry(guid, path, targetParent, readOnly, postEvent);
                }
                else
                {
                    entry = CreateEntry(guid, guid, targetParent, true, postEvent);
                }

                targetParent.AddAssetEntry(entry, postEvent);
            }

            return entry;
        }

        internal AddressableAssetEntry CreateSubEntryIfUnique(string guid, string address, AddressableAssetEntry parentEntry)
        {
            if (string.IsNullOrEmpty(guid))
                return null;

            var entry = FindAssetEntry(guid);
            if (entry == null)
            {
                entry = CreateEntry(guid, address, parentEntry.parentGroup, true);
                entry.IsSubAsset = true;
                return entry;
            }

            //if the sub-entry already exists update it's info.  This mainly covers the case of dragging folders around.
            if (entry.IsSubAsset)
            {
                entry.parentGroup = parentEntry.parentGroup;
                entry.IsInResources = parentEntry.IsInResources;
                entry.address = address;
                entry.ReadOnly = true;
                return entry;
            }
            return null;
        }

        /// <summary>
        /// Create a new asset group.
        /// </summary>
        /// <param name="groupName">The group name.</param>
        /// <param name="setAsDefaultGroup">Set the new group as the default group.</param>
        /// <param name="readOnly">Is the new group read only.</param>
        /// <param name="postEvent">Post modification event.</param>
        /// <param name="schemasToCopy">Schema set to copy from.</param>
        /// <param name="types">Types of schemas to add.</param>
        /// <returns>The newly created group.</returns>
        public AddressableAssetGroup CreateGroup(string groupName, bool setAsDefaultGroup, bool readOnly, bool postEvent, List<AddressableAssetGroupSchema> schemasToCopy, params Type[] types)
        {
            if (string.IsNullOrEmpty(groupName))
                groupName = kNewGroupName;
            string validName = FindUniqueGroupName(groupName);
            var group = CreateInstance<AddressableAssetGroup>();
            group.Initialize(this, validName, GUID.Generate().ToString(), readOnly);

            if (IsPersisted)
            {
                if (!Directory.Exists(GroupFolder))
                    Directory.CreateDirectory(GroupFolder);
                AssetDatabase.CreateAsset(group, GroupFolder + "/" + group.Name + ".asset");
            }
            if (schemasToCopy != null)
            {
                foreach (var s in schemasToCopy)
                    group.AddSchema(s, false);
            }
            foreach (var t in types)
                group.AddSchema(t);

            groups.Add(group);

            if (setAsDefaultGroup)
                DefaultGroup = group;
            SetDirty(ModificationEvent.GroupAdded, group, postEvent);
            return group;
        }

        internal string FindUniqueGroupName(string potentialName)
        {
            var cleanedName = potentialName.Replace('/', '-');
            cleanedName = cleanedName.Replace('\\', '-');
            if (cleanedName != potentialName)
                Addressables.Log("Group names cannot include '\\' or '/'.  Replacing with '-'. " + cleanedName);
            var validName = cleanedName;
            int index = 1;
            bool foundExisting = true;
            while (foundExisting)
            {
                if (index > 1000)
                {
                    Addressables.LogError("Unable to create valid name for new Addressable Assets group.");
                    return cleanedName;
                }
                foundExisting = IsNotUniqueGroupName(validName);
                if (foundExisting)
                {
                    validName = cleanedName + index;
                    index++;
                }
            }

            return validName;
        }

        internal bool IsNotUniqueGroupName(string groupName)
        {
            bool foundExisting = false;
            foreach (var g in groups)
            {
                if (g.Name == groupName)
                {
                    foundExisting = true;
                    break;
                }
            }
            return foundExisting;
        }

        /// <summary>
        /// Remove an asset group.
        /// </summary>
        /// <param name="g"></param>
        public void RemoveGroup(AddressableAssetGroup g)
        {
            RemoveGroupInternal(g, true, true);
        }

        internal void RemoveGroupInternal(AddressableAssetGroup g, bool deleteAsset, bool postEvent)
        {
            g.ClearSchemas(true);
            groups.Remove(g);
            SetDirty(ModificationEvent.GroupRemoved, g, postEvent);
            if (deleteAsset)
            {
                string guidOfGroup;
                long localId;
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(g, out guidOfGroup, out localId))
                {
                    var groupPath = AssetDatabase.GUIDToAssetPath(guidOfGroup);
                    if (!string.IsNullOrEmpty(groupPath))
                        AssetDatabase.DeleteAsset(groupPath);
                }
            }
        }


        internal void SetLabelValueForEntries(List<AddressableAssetEntry> entries, string label, bool value, bool postEvent = true)
        {
            if (value)
                AddLabel(label);

            foreach (var e in entries)
                e.SetLabel(label, value, false);

            SetDirty(ModificationEvent.EntryModified, entries, postEvent);
        }

        internal void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            var aa = this;
            bool modified = false;
            foreach (string str in importedAssets)
            {
                var assetType = AssetDatabase.GetMainAssetTypeAtPath(str);

                if (typeof(AddressableAssetGroup).IsAssignableFrom(assetType))
                {
                    AddressableAssetGroup group = aa.FindGroup(Path.GetFileNameWithoutExtension(str));
                    if (group != null)
                        group.DedupeEnteries();
                }

                if (typeof(AddressableAssetEntryCollection).IsAssignableFrom(assetType))
                {
                    aa.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(str), aa.DefaultGroup);
                    modified = true;
                }
                var guid = AssetDatabase.AssetPathToGUID(str);
                if (aa.FindAssetEntry(guid) != null)
                    modified = true;

                if (AddressableAssetUtility.IsInResources(str))
                    modified = true;
            }

            if (deletedAssets.Length > 0)
            {
                // if any directly referenced assets were deleted while Unity was closed, the path isn't useful, so Remove(null) is our only option
                //  this can lead to orphaned schema files.
                if (groups.Remove(null) ||
                    DataBuilders.Remove(null) ||
                    GroupTemplateObjects.Remove(null) ||
                    InitializationObjects.Remove(null))
                {
                    modified = true;
                }

            }

            foreach (string str in deletedAssets)
            {
                if (AddressableAssetUtility.IsInResources(str))
                    modified = true;
                else
                {
                    if (CheckForGroupDataDeletion(str))
                    {
                        modified = true;
                        continue;
                    }

                    var guidOfDeletedAsset = AssetDatabase.AssetPathToGUID(str);
                    if (aa.RemoveAssetEntry(guidOfDeletedAsset))
                    {
                        modified = true;
                    }
                }
            }
            for (int i = 0; i < movedAssets.Length; i++)
            {
                var str = movedAssets[i];
                var assetType = AssetDatabase.GetMainAssetTypeAtPath(str);
                if (typeof(AddressableAssetGroup).IsAssignableFrom(assetType))
                {
                    var oldGroupName = Path.GetFileNameWithoutExtension(movedFromAssetPaths[i]);
                    var group = aa.FindGroup(oldGroupName);
                    if (group != null)
                    {
                        var newGroupName = Path.GetFileNameWithoutExtension(str);
                        group.Name = newGroupName;
                    }
                }
                else
                {
                    var guid = AssetDatabase.AssetPathToGUID(str);
                    bool isAlreadyAddressable = aa.FindAssetEntry(guid) != null;
                    bool startedInResources = AddressableAssetUtility.IsInResources(movedFromAssetPaths[i]);
                    bool endedInResources = AddressableAssetUtility.IsInResources(str);
                    bool inEditorSceneList = BuiltinSceneCache.Contains(new GUID(guid));

                    //move to Resources
                    if (isAlreadyAddressable && endedInResources)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(str);
                        Addressables.Log("You have moved addressable asset " + fileName + " into a Resources directory.  It has been unmarked as addressable, but can still be loaded via the Addressables API via its Resources path.");
                        aa.RemoveAssetEntry(guid, false);
                    }
                    else if(inEditorSceneList)
                        BuiltinSceneCache.ClearState();

                    //any addressables move or resources move (even resources to within resources) needs to refresh the UI.
                    modified = isAlreadyAddressable || startedInResources || endedInResources || inEditorSceneList;
                }
            }

            if (modified)
                aa.SetDirty(ModificationEvent.BatchModification, null, true);
        }

        bool CheckForGroupDataDeletion(string str)
        {
            bool modified = false;
            var fileName = Path.GetFileNameWithoutExtension(str);
            AddressableAssetGroup groupToDelete = null;
            bool deleteGroup = false;
            foreach (var group in groups)
            {
                if (group.Name == fileName)
                {
                    groupToDelete = group;
                    deleteGroup = true;
                    break;
                }

                if (group.Schemas.Remove(null))
                    modified = true;
            }

            if (deleteGroup)
            {
                RemoveGroupInternal(groupToDelete, false, true);
                modified = true;
            }

            return modified;
        }

        /// <summary>
        /// Runs the active player data build script to create runtime data.
        /// </summary>
        public static void BuildPlayerContent()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressable Asset Settings does not exist.");
                return;
            }
            settings.BuildPlayerContentImpl();
        }
        
        internal void BuildPlayerContentImpl()
        {
            if (Directory.Exists(Addressables.BuildPath))
            {
                try
                {
                    Directory.Delete(Addressables.BuildPath, true);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            var buildContext = new AddressablesDataBuilderInput(this);
            var result = ActivePlayerDataBuilder.BuildData<AddressablesPlayerBuildResult>(buildContext);
            if (!string.IsNullOrEmpty(result.Error))
                Debug.LogError(result.Error);
            AddressableAnalytics.Report(this);
            if (BuildScript.buildCompleted != null)
                BuildScript.buildCompleted(result);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Deletes all created runtime data for the active player data builder.
        /// </summary>
        /// <param name="builder">The builder to call ClearCachedData on.  If null, all builders will be cleaned</param>
        public static void CleanPlayerContent(IDataBuilder builder = null)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressable Asset Settings does not exist.");
                return;
            }
            settings.CleanPlayerContentImpl(builder);
        }

        internal void CleanPlayerContentImpl(IDataBuilder builder = null)
        {
            if (builder != null)
            {
                builder.ClearCachedData();
            }
            else
            {
                for (int i = 0; i < DataBuilders.Count; i++)
                {
                    var m = GetDataBuilder(i);
                    m.ClearCachedData();
                }
            }
            AssetDatabase.Refresh();
        }
    }
}
