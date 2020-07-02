using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class PrefabTextureVariantSchema : AddressableAssetGroupSchema
{
    [Serializable]
    public struct VariantLabelPair
    {
        public float TextureScale;
        public string Label;
    }

    [Serializable]
    public struct Variant
    {
        public AssetReference MainEntry;
        public string DefaultLabel;
        public List<VariantLabelPair> VariantEntries;
    }

    [SerializeField] public List<Variant> Variants;
}
