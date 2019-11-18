using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class AddObject : MonoBehaviour {

    public string addressToAdd;
    Button m_AddButton;

    void Start()
    {
        
        m_AddButton = GetComponent<Button>();
        m_AddButton.onClick.AddListener(OnButtonClick);
    }

    void OnButtonClick()
    {
        var randSpot = new Vector3(Random.Range(-5, 1), Random.Range(-10, 10), Random.Range(0, 100));
        Addressables.InstantiateAsync("ball", randSpot, Quaternion.identity);
    }
}
