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
    public SpriteRenderer singleSpriteTest;
    public AssetReferenceSprite singleSpriteReference;

    public SpriteRenderer spriteSheetTest;
    public AssetReference spriteSheetReference;

    public SpriteRenderer spriteAtlasTest;
    public AssetReferenceAtlas spriteAtlasReference;

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
        
        singleSpriteTest.sprite = op.Result;
        
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

        spriteSheetTest.sprite = op.Result[3];
        
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

        spriteAtlasTest.sprite = op.Result.GetSprite("battle_1");
        
        buttonText.text = "More options coming with 1.2";
    }

}

[Serializable]
public class AssetReferenceAtlas : AssetReferenceT<SpriteAtlas>
{
    public AssetReferenceAtlas(string guid) : base(guid) { }
}
