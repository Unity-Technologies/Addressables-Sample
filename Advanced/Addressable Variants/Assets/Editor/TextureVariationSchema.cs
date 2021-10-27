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

    public List<ScaleAndLabelPair> Variations
    {
        get { return m_Variations;}
        set { m_Variations = value; }
    }

    [Serializable]
    public class ScaleAndLabelPair
    {
        public float textureScale = 0.5f;
        public string label;
    }

    [SerializeField]
    [Tooltip("If true, the source texture and its variants will be included in the build. Otherwise only the variants will be included.")]
    bool m_IncludeSourceTextureInBuild = false;
    public bool IncludeSourceTextureInBuild
    {
        get { return m_IncludeSourceTextureInBuild; }
        set { m_IncludeSourceTextureInBuild = value; }
    }
}
