using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.Serialization;

public class TextureVariationSchema : AddressableAssetGroupSchema
{

    [SerializeField]
    string m_BaselineLabel = "HD";
    public string BaselineLabel
    {
        get { return m_BaselineLabel; }
    }

    [SerializeField]
    List<ScaleAndLabelPair> m_Variations;
    public List<ScaleAndLabelPair> Variations { get; set; }

    [Serializable]
    public class ScaleAndLabelPair
    {
        [SerializeField]
        float m_TextureScale = 0.5f;
        [SerializeField]
        string m_Label;
    }
    
}
