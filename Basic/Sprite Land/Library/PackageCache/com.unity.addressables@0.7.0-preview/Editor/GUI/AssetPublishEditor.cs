using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets.GUI
{
    [Serializable]
    class AssetPublishEditor
    {
        [FormerlySerializedAs("scrollPosition")]
        [SerializeField]
        Vector2 m_ScrollPosition;

        [FormerlySerializedAs("fullBuildFoldout")]
        [SerializeField]
        bool m_FullBuildFoldout = true;
        [FormerlySerializedAs("updateFoldout")]
        [SerializeField]
        bool m_UpdateFoldout = true;
        [FormerlySerializedAs("snapshotPath")]
        [SerializeField]
        string m_SnapshotPath = "/Snapshots/ABuildSnapshot";

        public bool OnGUI(Rect pos)
        {
            GUILayout.BeginArea(pos);
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition, false, false, GUILayout.MaxWidth(pos.width));

            GUILayout.Space(20);
            GUILayout.Label("     NOT YET FUNCTIONAL    ");
            GUILayout.Space(10);

            m_FullBuildFoldout = EditorGUILayout.Foldout(m_FullBuildFoldout, "Full Build");
            if (m_FullBuildFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(new GUIContent("This section will create a rebuild of all content packs as well as the core player build.  A snapshot of this build must be saved in order to do updates to it later."));
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Build and Save Snapshot"))
                {
                    Addressables.Log("we aren't actually building yet.");
                }
                GUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(20);

            m_UpdateFoldout = EditorGUILayout.Foldout(m_UpdateFoldout, "Update Build");
            if (m_UpdateFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(new GUIContent("This section will not create a core player build, and it will only create the new bundles needed when compared to a given snapshot."));
                GUILayout.BeginHorizontal();
                m_SnapshotPath = EditorGUILayout.TextField(new GUIContent("Reference Snapshot"), m_SnapshotPath);
                GUILayout.Space(10);
                if (GUILayout.Button("Browse"))
                {
                    Addressables.Log("we aren't actually browsing yet.");
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Create Updated Packs"))
                {
                    Addressables.Log("we aren't actually updating yet.");
                }
                GUILayout.EndHorizontal();
                EditorGUI.indentLevel--;

            }


            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
            return false;
        }

        internal void OnEnable()
        {

        }

        internal void OnDisable()
        {

        }
    }
}
