using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

// ReSharper disable DelegateSubtraction

namespace UnityEditor.AddressableAssets.GUI
{
    [CustomEditor(typeof(AddressableAssetGroupTemplate))]
    class AddressableAssetGroupTemplateInspector : Editor
    {
        List<Type> m_SchemaTypes;
        bool[] m_FoldoutState;
        
        AddressableAssetGroupTemplate m_AddressableAssetGroupTarget;

        void OnEnable()
        {
            m_AddressableAssetGroupTarget = target as AddressableAssetGroupTemplate;
            
            if (m_AddressableAssetGroupTarget != null)
            {
                m_SchemaTypes = AddressableAssetUtility.GetTypes<AddressableAssetGroupSchema>();
                m_FoldoutState = new bool[m_AddressableAssetGroupTarget.SchemaObjects.Count];
            }

            for (int i = 0; i < m_FoldoutState.Length; i++)
                m_FoldoutState[i] = true;
        }

        public override void OnInspectorGUI()
        {
            try
            {
                serializedObject.Update();
                
                EditorGUILayout.LabelField( "Group Template Description" );
                m_AddressableAssetGroupTarget.Description = EditorGUILayout.TextArea( m_AddressableAssetGroupTarget.Description );
                EditorGUILayout.LabelField("", UnityEngine.GUI.skin.horizontalSlider);

                int objectCount = m_AddressableAssetGroupTarget.SchemaObjects.Count;
                if (m_FoldoutState == null || m_FoldoutState.Length != objectCount)
                    m_FoldoutState = new bool[objectCount];

                for (int i = 0; i < objectCount; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    m_FoldoutState[i] = EditorGUILayout.Foldout(m_FoldoutState[i], m_AddressableAssetGroupTarget.SchemaObjects[i].GetType().Name);
                    
                    if (GUILayout.Button("X", GUILayout.Width(40)))
                    {
                        if (EditorUtility.DisplayDialog("Delete selected schema?", "Are you sure you want to delete the selected schema?\n\nYou cannot undo this action.", "Yes", "No"))
                        {
                            if( m_AddressableAssetGroupTarget.RemoveSchema( i ) )
                            {
                                var newFoldoutstate = new bool[objectCount-1];
                                for( int j = 0; j < newFoldoutstate.Length; j++ )
                                {
                                    if( j < i )
                                        newFoldoutstate[j] = m_FoldoutState[i];
                                    else
                                        newFoldoutstate[j] = m_FoldoutState[i + 1];
                                }

                                m_FoldoutState = newFoldoutstate;
                                EditorGUILayout.EndHorizontal();
                            }
                            break;
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                    
                    if (m_FoldoutState[i])
                    {
                        m_AddressableAssetGroupTarget.SchemaObjects[i].OnGUI();
                    }
                }

                EditorGUILayout.LabelField("", UnityEngine.GUI.skin.horizontalSlider);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                
                    if (EditorGUILayout.DropdownButton(new GUIContent("Add Schema", "Add new schema to this group."), FocusType.Keyboard))
                    {
                        var menu = new GenericMenu();
                        for (int i = 0; i < m_SchemaTypes.Count; i++)
                        {
                            var type = m_SchemaTypes[i];
                            menu.AddItem(new GUIContent(type.Name, ""), false, OnAddSchema, type);
                        }

                        menu.ShowAsContext();
                    }

                EditorGUILayout.EndHorizontal();

                serializedObject.ApplyModifiedProperties();
            }
            catch (UnityEngine.ExitGUIException )
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        void OnAddSchema(object context)
        {
            if( !m_AddressableAssetGroupTarget.AddSchema( context as Type ) )
                return;
            
            var newFoldoutState = new bool[m_AddressableAssetGroupTarget.SchemaObjects.Count];
            for (int i = 0; i < m_FoldoutState.Length; i++)
                newFoldoutState[i] = m_FoldoutState[i];
            m_FoldoutState = newFoldoutState;
            m_FoldoutState[m_FoldoutState.Length - 1] = true;
        }
        
    }

}
