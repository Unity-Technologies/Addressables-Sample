using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "MaterialsData", menuName = "ScriptableObjects/MaterialsData", order = 1)]
public class MaterialsData : ScriptableObject
{
    public List<Material> materials;
}
