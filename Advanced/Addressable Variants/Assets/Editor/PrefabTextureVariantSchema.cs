using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class PrefabTextureVariantSchema : AddressableAssetGroupSchema
{
    [SerializeField]
    [Tooltip("If true, the source prefab and its variants will be included in the build. Otherwise only the variants will be included.")]
    bool m_IncludeSourcePrefabInBuild = false;
    public bool IncludeSourcePrefabInBuild
    {
        get { return m_IncludeSourcePrefabInBuild; }
        set { m_IncludeSourcePrefabInBuild = value; }
    }

    [Serializable]
    public struct VariantLabelPair
    {
        public float TextureScale;
        public string Label;
    }

    [SerializeField] public string DefaultLabel;
    [SerializeField] public List<VariantLabelPair> Variants;
}
