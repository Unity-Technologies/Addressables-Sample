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

    [SerializeField] public string DefaultLabel;
    [SerializeField] public List<VariantLabelPair> Variants;
}
