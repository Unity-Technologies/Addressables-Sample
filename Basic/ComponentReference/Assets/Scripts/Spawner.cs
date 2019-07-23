using System;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Spawner : MonoBehaviour
{
    public ComponentReferenceColorChanger ColorShifterReference;
    public ColorChanger ColorShifterDirect;

    int counter = 10000;

    void FixedUpdate()
    {
        counter++;
        if (counter == 15)
        {
            ColorShifterReference.InstantiateAsync().Completed += SpawnDone;
        }
        else if (counter > 30)
        {
            ColorChanger changer = Instantiate(ColorShifterDirect);
            changer.SetColor(new Color(UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f)));
            counter = 0;
        }
    }

    void SpawnDone(AsyncOperationHandle<GameObject> obj)
    {
        if (obj.Result != null)
        {
            var changer = obj.Result.GetComponent<ColorChanger>();
            if(changer != null)
                changer.SetColor(new Color(UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f)));
        }
    }
}

