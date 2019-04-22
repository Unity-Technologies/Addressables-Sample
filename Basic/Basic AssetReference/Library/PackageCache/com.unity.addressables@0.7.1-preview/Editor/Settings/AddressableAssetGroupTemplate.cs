using System;
using System.Collections.Generic;
using UnityEditor.Presets;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.AddressableAssets.Settings
{
    [CreateAssetMenu(fileName = "AddressableAssetGroupTemplate.asset", menuName = "Addressable Assets/Groups/New Group Template")]
    public class AddressableAssetGroupTemplate : ScriptableObject, IGroupTemplate
    {
        [SerializeField]
        private List<AddressableAssetGroupSchema> m_SchemaObjects = new List<AddressableAssetGroupSchema>();
        [SerializeField]
        private string m_Description;
        [SerializeField]
        private AddressableAssetSettings m_Settings;
        
        private AddressableAssetSettings Settings
        {
            get
            {
                if (m_Settings == null)
                    m_Settings = AddressableAssetSettingsDefaultObject.Settings;

                return m_Settings;
            }
        }
        
        /// <summary>
        /// Returns a list of Preset objects for AddressableAssetGroupSchema associated with this template
        /// </summary>
        internal List<Preset> SchemaPresetObjects
        {
            get
            {
                List<Preset> m_SchemaPresetObjects = new List<Preset>(m_SchemaObjects.Count);
                foreach( AddressableAssetGroupSchema schemaObject in m_SchemaObjects )
                    m_SchemaPresetObjects.Add( new Preset( schemaObject ) );
                return m_SchemaPresetObjects;
            }
        }
        
        /// <summary>
        /// Returns the list of Preset objects of AddressableAssetGroupSchema associated with this template
        /// </summary>
        public List<AddressableAssetGroupSchema> SchemaObjects
        {
            get { return m_SchemaObjects; }
        }
        
        /// <summary>
        /// The name of the AddressableAssetGroupTemplate
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// The description of the AddressableAssetGroupTemplate
        /// </summary>
        public string Description
        {
            get { return m_Description; }
            set { m_Description = value; }
        }
        
        /// <summary>
        /// Gets the types of the AddressableAssetGroupSchema associated with this template
        /// </summary>
        /// <returns>AddressableAssetGroupSchema types for schema on this template</returns>
        public Type[] GetTypes()
        {
            var types = new Type[m_SchemaObjects.Count];
            for (int i = 0; i < types.Length; i++)
                types[i] = m_SchemaObjects[i].GetType();
            return types;
        }
        
        /// <summary>
        /// Applies schema values for the group to the schema values found in the template
        /// </summary>
        /// <param name="group">The AddressableAssetGroup to apply the schema settings to</param>
        public void ApplyToAddressableAssetGroup( AddressableAssetGroup group )
        {
            foreach( AddressableAssetGroupSchema schema in group.Schemas )
            {
                List<Preset> presets = SchemaPresetObjects;
                foreach( Preset p in presets )
                {
                    Assert.IsNotNull( p );
                    if( p.CanBeAppliedTo( schema ) )
                    {
                        p.ApplyTo( schema );
                        schema.Group = group;
                    }
                }
            }
        }
        
        /// <summary>
        /// Adds the AddressableAssetGroupSchema of type to the template.
        /// </summary>
        /// <param name="type">The Type for the AddressableAssetGroupSchema to add to this template.</param>
        /// <param name="postEvent">If true, the event is propagated to callbacks.</param>
        /// <returns>If true, the type was added successfully.</returns>
        public bool AddSchema(Type type, bool postEvent = true)
        {
            if (type == null)
            {
                Debug.LogWarning("Cannot remove schema with null type.");
                return false;
            }
            if (!typeof(AddressableAssetGroupSchema).IsAssignableFrom(type))
            {
                Debug.LogWarningFormat("Invalid Schema type {0}. Schemas must inherit from AddressableAssetGroupSchema.", type.FullName);
                return false;
            }
            
            foreach( AddressableAssetGroupSchema schemaObject in m_SchemaObjects )
            {
                if( schemaObject.GetType() == type )
                {
                    Debug.LogError( "Scheme already exists" );
                    return false;
                }
            }

            AddressableAssetGroupSchema schemaInstance = (AddressableAssetGroupSchema)CreateInstance( type );
            if( schemaInstance != null )
            {
                schemaInstance.name = type.Name;
                try
                {
                    schemaInstance.hideFlags |= HideFlags.HideInHierarchy;
                    AssetDatabase.AddObjectToAsset( schemaInstance, this );
                }
                catch( Exception e )
                {
                    Console.WriteLine( e );
                    throw;
                }
                m_SchemaObjects.Add( schemaInstance );
                
                SetDirty(AddressableAssetSettings.ModificationEvent.GroupTemplateSchemaAdded, this, postEvent);
                AssetDatabase.SaveAssets();
            }

            return schemaInstance != null;
        }
        
        /// <summary>
        /// Removes the AddressableAssetGroupSchema of the type from the template.
        /// </summary>
        /// <param name="type">The type of AddressableAssetGroupSchema to be removed.</param>
        /// <param name="postEvent">If true, the event is propagated to callbacks.</param>
        /// <returns>If true, the type was removed successfully.</returns>
        public bool RemoveSchema( Type type, bool postEvent = true)
        {
            if (type == null)
            {
                Debug.LogWarning("Cannot remove schema with null type.");
                return false;
            }
            if (!typeof(AddressableAssetGroupSchema).IsAssignableFrom(type))
            {
                Debug.LogWarningFormat("Invalid Schema type {0}. Schemas must inherit from AddressableAssetGroupSchema.", type.FullName);
                return false;
            }

            for( int i = 0; i < m_SchemaObjects.Count; ++i )
            {
                if( m_SchemaObjects[i].GetType() == type )
                    return RemoveSchema(i, postEvent);
            }
            
            return false;
        }
        
        /// <summary>
        /// Removes the Schema at the given index.
        /// </summary>
        /// <param name="index">The index of the object to be removed.</param>
        /// <param name="postEvent">If true, the event is propagated to callbacks.</param>
        /// <returns>If true, the type was removed successfully.</returns>
        internal bool RemoveSchema( int index, bool postEvent = true)
        {
            if( index == -1 )
                return false;
            
            AssetDatabase.RemoveObjectFromAsset( m_SchemaObjects[index] );
            DestroyImmediate( m_SchemaObjects[index] );
            m_SchemaObjects.RemoveAt( index );
            
            SetDirty(AddressableAssetSettings.ModificationEvent.GroupTemplateSchemaRemoved, this, postEvent);
            AssetDatabase.SaveAssets();
            return true;
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
    }
}