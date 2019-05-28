using System;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets.GUI
{
    class AddressableAssetsWindow : EditorWindow
    {
        [FormerlySerializedAs("m_groupEditor")]
        [SerializeField]
        AddressableAssetsSettingsGroupEditor m_GroupEditor;

        [FormerlySerializedAs("m_ignoreLegacyBundles")]
        [SerializeField]
        bool m_IgnoreLegacyBundles;

        [MenuItem("Window/Asset Management/Addressable Assets", priority = 2050)]
        static void Init()
        {
            var window = GetWindow<AddressableAssetsWindow>();
            window.titleContent = new GUIContent("Addressables");
            window.Show();
        }
        public static Vector2 GetWindowPosition()
        {
            var window = GetWindow<AddressableAssetsWindow>();
            return new Vector2(window.position.x, window.position.y);
        }

        public void OnEnable()
        {
            if (!m_IgnoreLegacyBundles)
            {
                var bundleList = AssetDatabase.GetAllAssetBundleNames();
                if (bundleList != null && bundleList.Length > 0)
                    OfferToConvert();
            }
            if (m_GroupEditor != null)
                m_GroupEditor.OnEnable();
        }

        public void OnDisable()
        {
            if (m_GroupEditor != null)
                m_GroupEditor.OnDisable();
        }

        internal void OfferToConvert()
        {
            var bundleList = AssetDatabase.GetAllAssetBundleNames();
            if (EditorUtility.DisplayDialog("Legacy Bundles Detected", "We have detected the use of legacy bundles in this project.  Would you like to auto-convert those into Addressables? \nThis will take each asset bundle you have defined (we have detected " + bundleList.Length + " bundles), create an Addressables group with a matching name, then move all assets from those bundles into corresponding groups.  This will remove the asset bundle assignment from all assets, and remove all asset bundle definitions from this project.  This cannot be undone.", "Convert", "Ignore"))
            {
                AddressableAssetUtility.ConvertAssetBundlesToAddressables();
            }
            else
                m_IgnoreLegacyBundles = true;
        }

        public void OnGUI()
        {
            if (AddressableAssetSettingsDefaultObject.Settings == null)
            {
                GUILayout.Space(50);
                if (GUILayout.Button("Create Addressables Settings"))
                {
                    m_GroupEditor = null;
                    AddressableAssetSettingsDefaultObject.Settings = AddressableAssetSettings.Create(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder, AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, true, true);
                }
                if (GUILayout.Button("Import Addressables Settings"))
                {
                    m_GroupEditor = null;
                    var path = EditorUtility.OpenFilePanel("Addressables Settings Object", AddressableAssetSettingsDefaultObject.kDefaultConfigFolder, "asset");
                    if (!string.IsNullOrEmpty(path))
                    {
                        var i = path.ToLower().IndexOf("/assets/");
                        if (i > 0)
                        {
                            path = path.Substring(i + 1);
                            Addressables.LogFormat("Loading Addressables Settings from {0}", path);
                            var obj = AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(path);
                            if (obj != null)
                                AddressableAssetSettingsDefaultObject.Settings = obj;
                            else
                                Debug.LogWarning("Unable to load asset settings from: "
                                                 + path
                                                 + "\nPlease ensure the location included in the project directory."
                                );
                        }
                    }
                }
                GUILayout.Space(20);
                GUILayout.BeginHorizontal();
                GUILayout.Space(50);
                UnityEngine.GUI.skin.label.wordWrap = true;
                GUILayout.Label("Click the \"Create\" or \"Import\" button above or simply drag an asset into this window to start using Addressables.  Once you begin, the Addressables system will save some assets to your project to keep up with its data");
                GUILayout.Space(50);
                GUILayout.EndHorizontal();
                switch (Event.current.type)
                {
                    case EventType.DragPerform:
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        foreach (var path in DragAndDrop.paths)
                        {
                            if (AddressableAssetUtility.IsPathValidForEntry(path))
                            {
                                var guid = AssetDatabase.AssetPathToGUID(path);
                                if (!string.IsNullOrEmpty(guid))
                                {
                                    if (AddressableAssetSettingsDefaultObject.Settings == null)
                                        AddressableAssetSettingsDefaultObject.Settings = AddressableAssetSettings.Create(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder, AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, true, true);
                                    Undo.RecordObject(AddressableAssetSettingsDefaultObject.Settings, "AddressableAssetSettings");
                                    AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(guid, AddressableAssetSettingsDefaultObject.Settings.DefaultGroup);
                                }
                            }
                        }
                        break;
                    case EventType.DragUpdated:
                    case EventType.DragExited:
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        break;
                }
            }
            else
            {
                Rect contentRect = new Rect(0, 0, position.width, position.height);

                if (m_GroupEditor == null)
                {
                    m_GroupEditor = new AddressableAssetsSettingsGroupEditor(this);
                    m_GroupEditor.OnEnable();
                }
                if (m_GroupEditor.OnGUI(contentRect))
                    Repaint();
            }
        }
    }
}
