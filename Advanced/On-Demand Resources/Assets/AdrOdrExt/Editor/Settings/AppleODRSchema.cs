using System.ComponentModel;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.Serialization;

[DisplayName("Apple ODR Schema")]
public class AppleODRSchema : AddressableAssetGroupSchema
{
    public enum PrefetchCategory
    {
        InitialInstallTags,
        PrefetchedTagOrder,
        DownloadOnlyOnDemand
    }
    
    [SerializeField]
    [Tooltip("Bundle prefetch behaviour.")]
    PrefetchCategory m_prefetchCategory = PrefetchCategory.DownloadOnlyOnDemand;
    
    /// <summary>
    /// On Demand Resources prefetch behaviour.
    /// </summary>
    public PrefetchCategory Category
    {
        get { return m_prefetchCategory; }
    }
    
    [SerializeField]
    [Tooltip("Bundle download order (only applies when using Prefetched Tag Order).")]
    int m_order = 0;
    
    /// <summary>
    /// Bundle download order (only applies when using Prefetched Tag Order).
    /// </summary>
    public int Order
    {
        get { return m_order; }
    }
    
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
