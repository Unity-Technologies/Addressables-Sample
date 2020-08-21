using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets.GUI;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class SubobjectReference : MonoBehaviour
{
    public AssetReference sheetReference;
    public AssetReference sheetSubReference;
    public List<SpriteRenderer> spritesToChange;

    public Button loadMainButton;
    public Button loadSubButton;

    public void LoadMainAsset()
    {
        loadMainButton.interactable = false;
        sheetReference.LoadAssetAsync<IList<Sprite>>().Completed += AssetDone;
    }

    public void LoadSubAsset()
    {
        loadSubButton.interactable = false;
        sheetSubReference.LoadAssetAsync<Sprite>().Completed += Subassetdone;
    }
    
    void AssetDone(AsyncOperationHandle<IList<Sprite>> op)
    {
        if (op.Result == null)
        {
            Debug.LogError("no sheets here.");
            return;
        }

        spritesToChange[0].sprite = op.Result[1];
        
        loadMainButton.interactable = false;
    }

     void Subassetdone(AsyncOperationHandle<Sprite> op)
    {
        if (op.Result == null)
        {
            Debug.LogError("no sprite in sheet here.");
            return;
        }

        spritesToChange[1].sprite = op.Result;
        
        loadSubButton.interactable = false;
    }

    void Start()
    {
        Addressables.InitializeAsync();
    }

}
