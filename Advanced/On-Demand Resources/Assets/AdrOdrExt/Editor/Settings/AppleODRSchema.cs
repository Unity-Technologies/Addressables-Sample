using System.ComponentModel;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.Serialization;

[DisplayName("Apple ODR Schema")]
public class AppleODRSchema : AddressableAssetGroupSchema
{
    [FormerlySerializedAs("m_buildPath")]
    [SerializeField]
    [Tooltip("The path to copy asset bundles to.")]
    ProfileValueReference m_BuildPath = new ProfileValueReference();
    /// <summary>
    /// The path to copy asset bundles to.
    /// </summary>
    public ProfileValueReference BuildPath
    {
        get { return m_BuildPath; }
    }

    public override void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        base.OnGUI();
        if (EditorGUI.EndChangeCheck())
            SetDirty(this);
    }
}
