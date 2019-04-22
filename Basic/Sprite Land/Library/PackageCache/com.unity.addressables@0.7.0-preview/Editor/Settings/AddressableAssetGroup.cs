using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets.Settings
{
    /// <summary>
    /// Contains the collection of asset entries associated with this group.
    /// </summary>
    [Serializable]
    public class AddressableAssetGroup : ScriptableObject, IComparer<AddressableAssetEntry>, ISerializationCallbackReceiver
    {
        [FormerlySerializedAs("m_name")]
        [SerializeField]
        string m_GroupName;
        [FormerlySerializedAs("m_data")]
        [SerializeField]
        KeyDataStore m_Data;
        [FormerlySerializedAs("m_guid")]
        [SerializeField]
        string m_GUID;
        [FormerlySerializedAs("m_serializeEntries")]
        [SerializeField]
        List<AddressableAssetEntry> m_SerializeEntries = new List<AddressableAssetEntry>();
        [FormerlySerializedAs("m_readOnly")]
        [SerializeField]
        internal bool m_ReadOnly;
        [FormerlySerializedAs("m_settings")]
        [SerializeField]
        AddressableAssetSettings m_Settings;
        [FormerlySerializedAs("m_schemaSet")]
        [SerializeField]
        AddressableAssetGroupSchemaSet m_SchemaSet = new AddressableAssetGroupSchemaSet();

        Dictionary<string, AddressableAssetEntry> m_EntryMap = new Dictionary<string, AddressableAssetEntry>();

        /// <summary>
        /// The group name.
        /// </summary>
        public virtual string Name
        {
            get
            {
                if (string.IsNullOrEmpty(m_GroupName))
                    m_GroupName = Guid;

                return m_GroupName;
            }
            set
            {
                m_GroupName = value;
                m_GroupName = m_GroupName.Replace('/', '-');
                m_GroupName = m_GroupName.Replace('\\', '-');
                if(m_GroupName != value)
                    Debug.Log("Group names cannot include '\\' or '/'.  Replacing with '-'. " + m_GroupName);
                if (m_GroupName != name)
                {
                    string guid;
                    long localId;
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(this, out guid, out localId))
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        if (!string.IsNullOrEmpty(path))
                        {
                            var newPath = path.Replace(name, m_GroupName);
                            if (path != newPath)
                            {
                                var setPath = AssetDatabase.MoveAsset(path, newPath);
                                if (!string.IsNullOrEmpty(setPath))
                                {
                                    //unable to rename group due to invalid file name
                                    Debug.LogError("Rename of Group failed. " + setPath);
                                }
                                m_GroupName = name;

                            }
                        }
                    }
                    else
                    {
                        //this isn't a valid asset, which means it wasn't persisted, so just set the object name to the desired display name.
                        name = m_GroupName;
                    }
                    SetDirty(AddressableAssetSettings.ModificationEvent.GroupRenamed, this, true);
                }
            }
        }
        /// <summary>
        /// The group GUID.
        /// </summary>
        public virtual string Guid
        {
            get
            {
                if (string.IsNullOrEmpty(m_GUID))
                    m_GUID = GUID.Generate().ToString();
                return m_GUID;
            }
        }

        /// <summary>
        /// List of schemas for this group.
        /// </summary>
        public List<AddressableAssetGroupSchema> Schemas { get { return m_SchemaSet.Schemas; } }

        /// <summary>
        /// Get the types of added schema for this group.
        /// </summary>
        public List<Type> SchemaTypes { get { return m_SchemaSet.Types; } }

        string GetSchemaAssetPath(Type type)
        {
            return Settings.IsPersisted ? (Settings.GroupSchemaFolder + "/" + m_GUID + "_" + type.Name + ".asset") : string.Empty;
        }

        /// <summary>
        /// Adds a copy of the provided schema object.
        /// </summary>
        /// <param name="schema">The schema to add. A copy will be made and saved in a folder relative to the main Addressables settings asset. </param>
        /// <param name="postEvent">Determines if this method call will post an event to the internal addressables event system</param>
        /// <returns>The created schema object.</returns>
        public AddressableAssetGroupSchema AddSchema(AddressableAssetGroupSchema schema, bool postEvent = true)
        {
            var added = m_SchemaSet.AddSchema(schema, GetSchemaAssetPath);
            if (added != null)
            {
                added.Group = this;
                SetDirty(AddressableAssetSettings.ModificationEvent.GroupSchemaAdded, this, postEvent);
            }
            return added;
        }

        /// <summary>
        /// Creates and adds a schema of a given type to this group.  The schema asset will be created in the GroupSchemas directory relative to the settings asset.
        /// </summary>
        /// <param name="type">The schema type. This type must not already be added.</param>
        /// <param name="postEvent">Determines if this method call will post an event to the internal addressables event system</param>
        /// <returns>The created schema object.</returns>
        public AddressableAssetGroupSchema AddSchema(Type type, bool postEvent = true)
        {
            var added = m_SchemaSet.AddSchema(type, GetSchemaAssetPath);
            if (added != null)
            {
                added.Group = this;
                SetDirty(AddressableAssetSettings.ModificationEvent.GroupSchemaAdded, this, postEvent);
            }
            return added;
        }

        /// <summary>
        /// Creates and adds a schema of a given type to this group.
        /// </summary>
        /// <param name="postEvent">Determines if this method call will post an event to the internal addressables event system</param>
        /// <typeparam name="TSchema">The schema type. This type must not already be added.</typeparam>
        /// <returns>The created schema object.</returns>
        public TSchema AddSchema<TSchema>(bool postEvent = true) where TSchema : AddressableAssetGroupSchema
        {
            return AddSchema(typeof(TSchema), postEvent) as TSchema;
        }

        /// <summary>
        ///  Remove a given schema from this group.
        /// </summary>
        /// <param name="type">The schema type.</param>
        /// <param name="postEvent">Determines if this method call will post an event to the internal addressables event system</param>
        /// <returns>True if the schema was found and removed, false otherwise.</returns>
        public bool RemoveSchema(Type type, bool postEvent = true)
        {
            if (!m_SchemaSet.RemoveSchema(type))
                return false;

            SetDirty(AddressableAssetSettings.ModificationEvent.GroupSchemaRemoved, this, postEvent);
            return true;
        }

        /// <summary>
        ///  Remove a given schema from this group.
        /// </summary>
        /// <param name="postEvent">Determines if this method call will post an event to the internal addressables event system</param>
        /// <typeparam name="TSchema">The schema type.</typeparam>
        /// <returns>True if the schema was found and removed, false otherwise.</returns>
        public bool RemoveSchema<TSchema>(bool postEvent = true)
        {
            return RemoveSchema(typeof(TSchema), postEvent);
        }

        /// <summary>
        /// Gets an added schema of the specified type.
        /// </summary>
        /// <typeparam name="TSchema">The schema type.</typeparam>
        /// <returns>The schema if found, otherwise null.</returns>
        public TSchema GetSchema<TSchema>() where TSchema : AddressableAssetGroupSchema
        {
            return GetSchema(typeof(TSchema)) as TSchema;
        }

        /// <summary>
        /// Gets an added schema of the specified type.
        /// </summary>
        /// <param name="type">The schema type.</param>
        /// <returns>The schema if found, otherwise null.</returns>
        public AddressableAssetGroupSchema GetSchema(Type type)
        {
            return m_SchemaSet.GetSchema(type);
        }

        /// <summary>
        /// Checks if the group contains a schema of a given type.
        /// </summary>
        /// <typeparam name="TSchema">The schema type.</typeparam>
        /// <returns>True if the schema type or subclass has been added to this group.</returns>
        public bool HasSchema<TSchema>()
        {
            return HasSchema(typeof(TSchema));
        }

        /// <summary>
        /// Removes all schemas and optionally deletes the assets associated with them.
        /// </summary>
        /// <param name="deleteAssets">If true, the schema assets will also be deleted.</param>
        /// <param name="postEvent">Determines if this method call will post an event to the internal addressables event system</param>
        public void ClearSchemas(bool deleteAssets, bool postEvent = true)
        {
            m_SchemaSet.ClearSchemas(deleteAssets);
            SetDirty(AddressableAssetSettings.ModificationEvent.GroupRemoved, this, postEvent);
        }

        /// <summary>
        /// Checks if the group contains a schema of a given type.
        /// </summary>
        /// <param name="type">The schema type.</param>
        /// <returns>True if the schema type or subclass has been added to this group.</returns>
        public bool HasSchema(Type type)
        {
            return GetSchema(type) != null;
        }

        /// <summary>
        /// Is this group read only.  This is normally false.  Built in resources (resource folders and the scene list) are put into a special read only group.
        /// </summary>
        public virtual bool ReadOnly
        {
            get { return m_ReadOnly; }
        }

        internal AddressableAssetSettings Settings
        {
            get
            {
                if (m_Settings == null)
                    m_Settings = AddressableAssetSettingsDefaultObject.Settings;

                return m_Settings;
            }
        }

        /// <summary>
        /// The collection of asset entries.
        /// </summary>
        public virtual ICollection<AddressableAssetEntry> entries
        {
            get
            {
                return m_EntryMap.Values;
            }
        }

        /// <summary>
        /// Is the default group.
        /// </summary>
        public virtual bool Default
        {
            get { return Guid == Settings.DefaultGroup.Guid; }
        }

        /// <inheritdoc/>
        public virtual int Compare(AddressableAssetEntry x, AddressableAssetEntry y)
        {
            if (x == null && y == null)
                return 0;
            if (x == null)
                return -1;
            if (y == null)
                return 1;
            return x.guid.CompareTo(y.guid);
        }

        internal void SerializeForHash(BinaryFormatter formatter, Stream stream)
        {
            formatter.Serialize(stream, m_GroupName);
            formatter.Serialize(stream, m_GUID);
            formatter.Serialize(stream, entries.Count);
            foreach (var e in entries)
                e.SerializeForHash(formatter, stream);
            formatter.Serialize(stream, m_ReadOnly);
            //TODO: serialize group data
        }

        /// <summary>
        /// Converts data to serializable format.
        /// </summary>
        public void OnBeforeSerialize()
        {
            m_SerializeEntries.Clear();
            foreach (var e in entries)
                m_SerializeEntries.Add(e);

            m_SerializeEntries.Sort(this);
        }

        /// <summary>
        /// Converts data from serializable format.
        /// </summary>
        public void OnAfterDeserialize()
        {
            ResetEntryMap();
            m_SerializeEntries.Clear();
        }

        internal void ResetEntryMap()
        {
            m_EntryMap.Clear();
            foreach (var e in m_SerializeEntries)
            {
                try
                {
                    e.parentGroup = this;
                    e.IsSubAsset = false;
                    m_EntryMap.Add(e.guid, e);
                }
                catch (Exception ex)
                {
                    Addressables.Log(e.address);
                    Debug.LogException(ex);
                }
            }

        }

        void OnEnable()
        {
            Validate();
        }

        internal void Validate()
        {

            bool allValid = false;
            while (!allValid)
            {
                allValid = true;
                for (int i = 0; i < m_SchemaSet.Schemas.Count; i++)
                {
                    if (m_SchemaSet.Schemas[i] == null)
                    {
                        m_SchemaSet.Schemas.RemoveAt(i);
                        allValid = false;
                        break;
                    }
                    if(m_SchemaSet.Schemas[i].Group == null)
                        m_SchemaSet.Schemas[i].Group = this;
                }
            }

            var editorList = GetAssetEntry(AddressableAssetEntry.EditorSceneListName);
            if (editorList != null)
            {
                if (m_GroupName == null)
                    m_GroupName = AddressableAssetSettings.PlayerDataGroupName;
                if (m_Data != null)
                {
                    if(!HasSchema<PlayerDataGroupSchema>())
                        AddSchema<PlayerDataGroupSchema>();
                    m_Data = null;
                }
            }
            else if(Settings != null)
            {
                if (m_GroupName == null)
                    m_GroupName = Settings.FindUniqueGroupName("Packed Content Group");
                m_Data = null;
            }
        }

        internal void DedupeEnteries()
        {
            List<AddressableAssetEntry> removeEntries = new List<AddressableAssetEntry>();
            foreach (AddressableAssetEntry e in m_EntryMap.Values)
            {
                AddressableAssetEntry lookedUpEntry = m_Settings.FindAssetEntry(e.guid);
                if (lookedUpEntry.parentGroup != this)
                {
                    Debug.LogWarning(  e.address
                                     + " is already a member of group "
                                     + lookedUpEntry.parentGroup
                                     + " but group "
                                     + m_GroupName
                                     + " contained a reference to it.  Removing referece.");
                    removeEntries.Add(e);
                }
            }

            RemoveAssetEntries(removeEntries);
        }

        internal void Initialize(AddressableAssetSettings settings, string groupName, string guid, bool readOnly)
        {
            m_Settings = settings;
            m_GroupName = groupName;
            m_ReadOnly = readOnly;
            m_GUID = guid;
        }

        /// <summary>
        /// Gathers all asset entries.  Each explicit entry may contain multiple sub entries. For example, addressable folders create entries for each asset contained within.
        /// </summary>
        /// <param name="results">The generated list of entries.  For simple entries, this will contain just the entry itself if specified.</param>
        /// <param name="includeSelf">Determines if the entry should be contained in the result list or just sub entries.</param>
        /// <param name="recurseAll">Determines if full recursion should be done when gathering entries.</param>
        /// <param name="entryFilter">Optional predicate to run against each entry, only returning those that pass.  A null filter will return all entries</param>
        public virtual void GatherAllAssets(List<AddressableAssetEntry> results, bool includeSelf, bool recurseAll, Func<AddressableAssetEntry, bool> entryFilter = null)
        {
            foreach (var e in entries)
                if(entryFilter == null || entryFilter(e))
                    e.GatherAllAssets(results, includeSelf, recurseAll, entryFilter);
        }

        internal void AddAssetEntry(AddressableAssetEntry e, bool postEvent = true)
        {
            e.IsSubAsset = false;
            e.parentGroup = this;
            m_EntryMap[e.guid] = e;
            SetDirty(AddressableAssetSettings.ModificationEvent.EntryAdded, e, postEvent);
        }

        /// <summary>
        /// Get an entry via the asset guid.
        /// </summary>
        /// <param name="guid">The asset guid.</param>
        /// <returns></returns>
        public virtual AddressableAssetEntry GetAssetEntry(string guid)
        {
            if (m_EntryMap.ContainsKey(guid))
                return m_EntryMap[guid];
            return null;
        }

        /// <summary>
        /// Marks the object as modified.
        /// </summary>
        /// <param name="modificationEvent">The event type that is changed.</param>
        /// <param name="eventData">The object data that corresponds to the event.</param>
        /// <param name="postEvent">If true, the event is propagated to callbacks.</param>
        public void SetDirty(AddressableAssetSettings.ModificationEvent modificationEvent, object eventData, bool postEvent)
        {
            if (Settings != null)
            {
                if (Settings.IsPersisted && this != null)
                    EditorUtility.SetDirty(this);
                Settings.SetDirty(modificationEvent, eventData, postEvent);
            }
        }

        /// <summary>
        /// Remove an entry.
        /// </summary>
        /// <param name="entry">The entry to remove.</param>
        /// <param name="postEvent">If true, post the event to callbacks.</param>
        public void RemoveAssetEntry(AddressableAssetEntry entry, bool postEvent = true)
        {
            m_EntryMap.Remove(entry.guid);
            entry.parentGroup = null;
            SetDirty(AddressableAssetSettings.ModificationEvent.EntryRemoved, entry, postEvent);
        }

        internal void RemoveAssetEntries(IEnumerable<AddressableAssetEntry> removeEntries, bool postEvent = true)
        {
            foreach (AddressableAssetEntry entry in removeEntries)
            {
                m_EntryMap.Remove(entry.guid);
                entry.parentGroup = null;
            }

            SetDirty(AddressableAssetSettings.ModificationEvent.EntryRemoved, removeEntries.ToArray(), postEvent);
        }

        /// <summary>
        /// Check to see if a group is the Default Group.
        /// </summary>
        /// <returns></returns>
        public bool IsDefaultGroup()
        {
            return Guid == m_Settings.DefaultGroup.Guid;
        }

        /// <summary>
        /// Check if a group has the appropriate schemas and attributes that the Default Group requires.
        /// </summary>
        /// <returns></returns>
        public bool CanBeSetAsDefault()
        {
            return !m_ReadOnly;
        }
    }
}
