using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

// ReSharper disable DelegateSubtraction

namespace UnityEditor.AddressableAssets.GUI
{
    [CustomEditor(typeof(AddressableAssetGroup))]
    class AddressableAssetGroupInspector : Editor
    {
        AddressableAssetGroup m_GroupTarget;
        List<Type> m_SchemaTypes;
        bool[] m_FoldoutState;

        void OnEnable()
        {
            m_GroupTarget = target as AddressableAssetGroup;
            if (m_GroupTarget != null)
            {
                m_GroupTarget.Settings.OnModification += OnSettingsModification;
                m_SchemaTypes = AddressableAssetUtility.GetTypes<AddressableAssetGroupSchema>();
                m_FoldoutState = new bool[m_GroupTarget.Schemas.Count];
            }

            for (int i = 0; i < m_FoldoutState.Length; i++)
                m_FoldoutState[i] = true;
        }

        void OnDisable()
        {
            if (m_GroupTarget != null) 
                m_GroupTarget.Settings.OnModification -= OnSettingsModification;
        }

        void OnSettingsModification(AddressableAssetSettings settings, AddressableAssetSettings.ModificationEvent evnt, object o)
        {
            switch (evnt)
            {
                case AddressableAssetSettings.ModificationEvent.GroupAdded:
                case AddressableAssetSettings.ModificationEvent.GroupRemoved:
                case AddressableAssetSettings.ModificationEvent.GroupRenamed:
                case AddressableAssetSettings.ModificationEvent.BatchModification:
                case AddressableAssetSettings.ModificationEvent.ActiveProfileSet:
                case AddressableAssetSettings.ModificationEvent.GroupSchemaAdded:
                case AddressableAssetSettings.ModificationEvent.GroupSchemaModified:
                case AddressableAssetSettings.ModificationEvent.GroupSchemaRemoved:
                    Repaint();
                    break;
            }
        }


        public override void OnInspectorGUI()
        {
            try
            {
                serializedObject.Update();
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField("Addressable Asset Settings", m_GroupTarget.Settings, typeof(AddressableAssetSettings), false);
                    var prof = m_GroupTarget.Settings.profileSettings.GetProfile(m_GroupTarget.Settings.activeProfileId);
                    EditorGUILayout.TextField("Current Profile", prof.profileName);
                }

                if (m_FoldoutState == null || m_FoldoutState.Length != m_GroupTarget.Schemas.Count)
                    m_FoldoutState = new bool[m_GroupTarget.Schemas.Count];

                for (int i = 0; i < m_GroupTarget.Schemas.Count; i++)
                {
                    var schema = m_GroupTarget.Schemas[i];
                    EditorGUILayout.BeginHorizontal();
                    m_FoldoutState[i] = EditorGUILayout.Foldout(m_FoldoutState[i], schema.GetType().Name);
                    if (!m_GroupTarget.ReadOnly)
                    {
                        if (GUILayout.Button("X", GUILayout.Width(40)))
                        {
                            if (EditorUtility.DisplayDialog("Delete selected schema?", "Are you sure you want to delete the selected schema?\n\nYou cannot undo this action.", "Yes", "No"))
                            {
                                m_GroupTarget.RemoveSchema(schema.GetType());
                                var newFoldoutstate = new bool[m_GroupTarget.Schemas.Count];
                                for (int j = 0; j < newFoldoutstate.Length; j++)
                                {
                                    if (j < i)
                                        newFoldoutstate[j] = m_FoldoutState[i];
                                    else
                                        newFoldoutstate[j] = m_FoldoutState[i + 1];
                                }

                                m_FoldoutState = newFoldoutstate;
                                EditorGUILayout.EndHorizontal();
                                break;
                            }
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                    if (m_FoldoutState[i])
                    {
                        try
                        {
                            EditorGUI.indentLevel++;
                            schema.OnGUI();
                            EditorGUI.indentLevel--;
                        }
                        catch (Exception se)
                        {
                            Debug.LogException(se);
                        }
                    }
                }



                EditorGUILayout.LabelField("", UnityEngine.GUI.skin.horizontalSlider);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (!m_GroupTarget.ReadOnly)
                {
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
            m_GroupTarget.AddSchema(context as Type);
            var newFoldoutState = new bool[m_GroupTarget.Schemas.Count];
            for (int i = 0; i < m_FoldoutState.Length; i++)
                newFoldoutState[i] = m_FoldoutState[i];
            m_FoldoutState = newFoldoutState;
            m_FoldoutState[m_FoldoutState.Length - 1] = true;
        }
        
    }

}
