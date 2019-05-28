using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

public class SceneLoader : MonoBehaviour
{
    public string nextSceneAddress;
    
    // Start is called before the first frame update
    void Start()
    {
        Addressables.LoadSceneAsync(nextSceneAddress);
    }
}
