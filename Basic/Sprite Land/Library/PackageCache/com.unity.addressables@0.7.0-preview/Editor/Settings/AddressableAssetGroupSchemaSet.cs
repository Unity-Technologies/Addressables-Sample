using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace UnityEditor.AddressableAssets.Settings
{
    /// <summary>
    /// Collection of AddressableAssetGroupSchema objects
    /// </summary>
    [Serializable]
    public class AddressableAssetGroupSchemaSet
    {
        [FormerlySerializedAs("m_schemas")]
        [SerializeField]
        List<AddressableAssetGroupSchema> m_Schemas = new List<AddressableAssetGroupSchema>();

        /// <summary>
        /// List of schemas for this group.
        /// </summary>
        public List<AddressableAssetGroupSchema> Schemas { get { return m_Schemas; } }
        /// <summary>
        /// Get the list of schema types.
        /// </summary>
        public List<Type> Types
        {
            get
            {
                var types = new List<Type>(m_Schemas.Count);
                foreach (var s in m_Schemas)
                    types.Add(s.GetType());
                return types;
            }
        }

        /// <summary>
        /// Adds a copy of the provided schema object.
        /// </summary>
        /// <param name="schema">The schema to copy.</param>
        /// <param name="pathFunc">A function that returns the path where this method can save the schema asset.  Set to null to not create an in-project asset.</param>
        /// <returns>The created schema object.</returns>
        public AddressableAssetGroupSchema AddSchema(AddressableAssetGroupSchema schema, Func<Type, string> pathFunc)
        {
            if (schema == null)
            {
                Debug.LogWarning("Cannot add null Schema object.");
                return null;
            }
            var type = schema.GetType();

            if (GetSchema(type) != null)
            {
                Debug.LogWarningFormat("Cannot add multiple schemas of the same type: {0}.", type.FullName);
                return null;
            }

            if (pathFunc == null)
            {
                m_Schemas.Add(schema);
                return schema;
            }

            var assetName = pathFunc(type);
            if (File.Exists(assetName))
            {
                Debug.LogWarningFormat("Schema asset already exists at path {0}, relinking.", assetName);
                var existingSchema = AssetDatabase.LoadAssetAtPath(assetName, type) as AddressableAssetGroupSchema;
                m_Schemas.Add(existingSchema);
                return existingSchema;
            }

            var newSchema = Object.Instantiate(schema);
            if (!string.IsNullOrEmpty(assetName))
            {
                var dir = Path.GetDirectoryName(assetName);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                AssetDatabase.CreateAsset(newSchema, assetName);
            }

            m_Schemas.Add(newSchema);
            return newSchema;
        }

        /// <summary>
        /// Creates and adds a schema of a given type to this group.  The schema asset will be created in the GroupSchemas directory relative to the settings asset.
        /// </summary>
        /// <param name="type">The schema type. This type must not already be added.</param>
        /// <param name="pathFunc">A function that returns the path where this method can save the schema asset.  Set to null to not create an in-project asset.</param>
        /// <returns>The created schema object.</returns>
        public AddressableAssetGroupSchema AddSchema(Type type, Func<Type, string> pathFunc)
        {
            if (type == null)
            {
                Debug.LogWarning("Cannot add null Schema type.");
                return null;
            }

            if (!typeof(AddressableAssetGroupSchema).IsAssignableFrom(type))
            {
                Debug.LogWarningFormat("Invalid Schema type {0}. Schemas must inherit from AddressableAssetGroupSchema.", type.FullName);
                return null;
            }

            var existing = GetSchema(type);
            if (existing != null)
            {
                Debug.LogWarningFormat("Cannot add multiple schemas of the same type: {0}.", existing.GetType().FullName);
                return existing;
            }

            var assetName = pathFunc(type);
            if (File.Exists(assetName))
            {
                Debug.LogWarningFormat("Schema asset already exists at path {0}, relinking.", assetName);
                var existingSchema = AssetDatabase.LoadAssetAtPath(assetName, type) as AddressableAssetGroupSchema;
                m_Schemas.Add(existingSchema);
                return existingSchema;
            }

            var schema = ScriptableObject.CreateInstance(type) as AddressableAssetGroupSchema;
            if (!string.IsNullOrEmpty(assetName))
            {
                var dir = Path.GetDirectoryName(assetName);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                AssetDatabase.CreateAsset(schema, assetName);
            }

            m_Schemas.Add(schema);
            return schema;
        }

        /// <summary>
        ///  Remove a given schema from this group.
        /// </summary>
        /// <param name="type">The schema type.</param>
        /// <returns>True if the schema was found and removed, false otherwise.</returns>
        public bool RemoveSchema(Type type)
        {
            for (int i = 0; i < m_Schemas.Count; i++)
            {
                var s = m_Schemas[i];
                if (s.GetType() == type)
                {
                    m_Schemas.RemoveAt(i);
                    string guid;
                    long lfid;
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s, out guid, out lfid))
                        AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets an added schema of the specified type.
        /// </summary>
        /// <param name="type">The schema type.</param>
        /// <returns>The schema if found, otherwise null.</returns>
        public AddressableAssetGroupSchema GetSchema(Type type)
        {
            if (type == null)
            {
                Debug.LogWarning("Cannot get schema with null type.");
                return null;
            }

            if (!typeof(AddressableAssetGroupSchema).IsAssignableFrom(type))
            {
                Debug.LogWarningFormat("Invalid Schema type {0}. Schemas must inherit from AddressableAssetGroupSchema.", type.FullName);
                return null;
            }

            foreach (var s in m_Schemas)
                if (type == s.GetType())
                    return s;
            return null;
        }

        /// <summary>
        /// Removes all schemas and optionally deletes the assets associated with them.
        /// </summary>
        /// <param name="deleteAssets">If true, the schema assets will also be deleted.</param>
        public void ClearSchemas(bool deleteAssets)
        {
            if (deleteAssets)
            {
                for (int i = 0; i < m_Schemas.Count; i++)
                {
                    var s = m_Schemas[i];
                    string guid;
                    long lfid;
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s, out guid, out lfid))
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        if (File.Exists(path))
                            AssetDatabase.DeleteAsset(path);
                    }
                }
            }
            m_Schemas.Clear();
        }
    }
}