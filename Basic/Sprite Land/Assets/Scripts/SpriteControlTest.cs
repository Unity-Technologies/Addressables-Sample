using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;
using UnityEngine.UI;

public class SpriteControlTest : MonoBehaviour
{
    public AssetReferenceSprite singleSpriteReference;
    public AssetReference spriteSheetReference;
    public AssetReferenceAtlas spriteAtlasReference;
    public AssetReferenceSprite spriteSubAssetReference;
    public AssetReference atlasSubAssetReference;
    public string spriteSubAddressSyntax;
    public string atlasSubAddressSyntax;

    public List<SpriteRenderer> spritesToChange;

    public Button button;
    public Text buttonText;
    

    int m_ClickCount = 0;
    public void OnButtonClick()
    {
        
        button.interactable = false;
        m_ClickCount++;
        switch (m_ClickCount)
        {
            case 1:
                singleSpriteReference.LoadAssetAsync().Completed += SingleDone;
                break;
            case 2:
                spriteSheetReference.LoadAssetAsync<IList<Sprite>>().Completed += SheetDone;
                break;
            case 3:
                spriteAtlasReference.LoadAssetAsync().Completed += AtlasDone;
                break;
            case 4:
                spriteSubAssetReference.LoadAssetAsync().Completed += SheetSubDone;
                break;
            case 5:
                atlasSubAssetReference.LoadAssetAsync<Sprite>().Completed += AtlasSubDone;
                break;
            case 6:
                Addressables.LoadAssetAsync<Sprite>(spriteSubAddressSyntax).Completed += SheetNameSubDone;
                break;
            case 7:
                Addressables.LoadAssetAsync<Sprite>(atlasSubAddressSyntax).Completed += AtlasNameSubDone;
                break;
        }
        

    }

    void SingleDone(AsyncOperationHandle<Sprite> op)
    {
        if (op.Result == null)
        {
            Debug.LogError("no sprites here.");
            return;
        }
        
        spritesToChange[0].sprite = op.Result;
        
        button.interactable = true;
        buttonText.text = "Change with sheet list";
    }

    void SheetDone(AsyncOperationHandle<IList<Sprite>> op)
    {
        if (op.Result == null)
        {
            Debug.LogError("no sheets here.");
            return;
        }

        spritesToChange[1].sprite = op.Result[3];
        
        button.interactable = true;
        buttonText.text = "Change with atlas";
    }

    void AtlasDone(AsyncOperationHandle<SpriteAtlas> op)
    {
        if (op.Result == null)
        {
            Debug.LogError("no atlases here.");
            return;
        }

        spritesToChange[2].sprite = op.Result.GetSprite("battle_1");
        
        button.interactable = true;
        buttonText.text = "Change with sprite sub-object ref";
    }

    void SheetSubDone(AsyncOperationHandle<Sprite> op)
    {
        if (op.Result == null)
        {
            Debug.LogError("no sprite in sheet here.");
            return;
        }

        spritesToChange[3].sprite = op.Result;
        
        button.interactable = true;
        buttonText.text = "Change with atlas sub-object ref";
    }

    void AtlasSubDone(AsyncOperationHandle<Sprite> op)
    {
        if (op.Result == null)
        {
            Debug.LogError("no sprite in atlas here.");
            return;
        }

        spritesToChange[4].sprite = op.Result;
        
        button.interactable = true;
        buttonText.text = "Change with sprite[name]";
    }

    void SheetNameSubDone(AsyncOperationHandle<Sprite> op)
    {
        if (op.Result == null)
        {
            Debug.LogError("no sprite in sheet here.");
            return;
        }

        spritesToChange[5].sprite = op.Result;
        
        button.interactable = true;
        buttonText.text = "Change with atlas[name]";
    }

    void AtlasNameSubDone(AsyncOperationHandle<Sprite> op)
    {
        if (op.Result == null)
        {
            Debug.LogError("no sprite in atlas here.");
            return;
        }

        spritesToChange[6].sprite = op.Result;
        
        buttonText.text = "The End";
    }

}

[Serializable]
public class AssetReferenceAtlas : AssetReferenceT<SpriteAtlas>
{
    public AssetReferenceAtlas(string guid) : base(guid) { }
}
