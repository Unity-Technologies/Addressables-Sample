using System;
using UnityEngine;

public class ColorChanger : MonoBehaviour
{
    public void SetColor(Color col)
    {
        var rend = GetComponent<MeshRenderer>();
        var material = rend.material;
        material.color = col;
    }
}

[Serializable]
public class ComponentReferenceColorChanger : ComponentReference<ColorChanger>
{
    public ComponentReferenceColorChanger(string guid)
        : base(guid) { }
}
    
