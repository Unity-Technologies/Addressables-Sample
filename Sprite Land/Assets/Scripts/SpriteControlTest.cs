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

    int m_Fired = 0;

    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            m_Fired++;
            if(m_Fired == 1)
                swapToSingle.LoadAsset().Completed += SingleDone;
            else if(m_Fired == 2)
                Addressables.LoadAsset<IList<Sprite>>(spriteSheetAddress).Completed += SheetDone;
        }

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

    void SingleDone(IAsyncOperation<Sprite> op)
    {
        if (op.Result == null)
        {
            Debug.LogError("no sheets here.");
            return;
        }

        //saving result, and setting on sprite later to work around engine bug :(
        singleEntry = op.Result;
    }

    void SheetDone(IAsyncOperation<IList<Sprite>> op)
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
