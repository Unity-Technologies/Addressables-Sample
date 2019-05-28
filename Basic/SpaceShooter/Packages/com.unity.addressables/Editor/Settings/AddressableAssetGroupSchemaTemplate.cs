using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets.Settings
{
    // TODO: OBSELETE: This is replaced with AddressableAssetGroupTemplate, this is needed to update existing setups to new Preset method 
    /// <summary>
    /// Contains a set of schemas used by the GUI to create predefined asset groups.
    /// </summary>
    [Serializable]
    public class AddressableAssetGroupSchemaTemplate
    {
        [FormerlySerializedAs("m_displayName")]
        [SerializeField]
        string m_DisplayName;
        [FormerlySerializedAs("m_description")]
        [SerializeField]
        string m_Description;
        [FormerlySerializedAs("m_schemaTypes")]
        [SerializeField]
        List<SerializedType> m_SchemaTypes;

        /// <summary>
        /// The display name of the template.
        /// </summary>
        public string DisplayName
        {
            get { return m_DisplayName; }
        }
         
        /// <summary>
        /// the description of the template.
        /// </summary>
        public string Description
        {
            get { return m_Description; }
        }

        /// <summary>
        /// The types of schemas in this template.
        /// </summary>
        /// <returns>The array of schema types.</returns>
        public Type[] GetTypes()
        {
            var types = new Type[m_SchemaTypes.Count];
            for (int i = 0; i < types.Length; i++)
                types[i] = m_SchemaTypes[i].Value;
            return types;
        }

        /// <summary>
        /// Creates a template with the specified name, descriptionm and schema types.
        /// </summary>
        /// <param name="name">The name of the template.</param>
        /// <param name="descr">The template description.</param>
        /// <param name="types">The schema types for the template.</param>
        /// <returns>The newly created schema template.</returns>
        public static AddressableAssetGroupSchemaTemplate Create(string name, string descr, params Type[] types)
        {
            var st = new AddressableAssetGroupSchemaTemplate { m_DisplayName = name, m_Description = descr };
            st.m_SchemaTypes = new List<SerializedType>(types.Length);
            for (int i = 0; i < types.Length; i++)
                st.m_SchemaTypes.Add(new SerializedType { Value = types[i] });
            return st;
        }
    }
}