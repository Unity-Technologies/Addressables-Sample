using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Experimental.UIElements;
using UnityEngine.ResourceManagement.AsyncOperations;

public class SpriteControlTest : MonoBehaviour
{
    public SpriteRenderer singleSpriteTest;
    public AssetReferenceSprite swapToSingle;
    Sprite singleEntry = null;

    public SpriteRenderer spriteSheetTest;
    public string spriteSheetAddress;
    Sprite sheetEntry = null;

    int clickCount = 0;
    public void OnButtonClick()
    {
        
        clickCount++;
        if(clickCount == 1)
            swapToSingle.LoadAsset().Completed += SingleDone;
        else if(clickCount == 2)
            Addressables.LoadAsset<IList<Sprite>>(spriteSheetAddress).Completed += SheetDone;
        

    }

    void Update()
    {
        if (sheetEntry != null)
        {
            spriteSheetTest.sprite = sheetEntry;
            sheetEntry = null;
        }

        if (singleEntry != null)
        {
            singleSpriteTest.sprite = singleEntry;
            singleEntry = null;
        }
    }
    
    void SingleDone(AsyncOperationHandle<Sprite> op)
    {
        if (op.Result == null)
        {
            Debug.LogError("no sheets here.");
            return;
        }
        
        //saving result, and setting on sprite later to work around engine bug :(
        singleEntry = op.Result;
    }

    void SheetDone(AsyncOperationHandle<IList<Sprite>> op)
    {
        if (op.Result == null)
        {
            Debug.LogError("no sheets here.");
            return;
        }

        //saving result, and setting on sprite later to work around engine bug :(
        sheetEntry = op.Result[3];
    }
}
