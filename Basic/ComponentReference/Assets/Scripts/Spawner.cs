using System;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Spawner : MonoBehaviour
{
    public ComponentReferenceColorChanger ColorShifterReference;
    public ColorChanger ColorShifterDirect;

    int m_Counter = 10000;

    void FixedUpdate()
    {
        m_Counter++;
        if (m_Counter == 15)
        {
            //note that this could work as an Instantiate or a Load. 
            //ColorShifterReference.LoadComponentAsync().Completed += LoadDone;
            ColorShifterReference.InstantiateAsync().Completed += InstantiateDone;
        }
        else if (m_Counter > 30)
        {
            ColorChanger changer = Instantiate(ColorShifterDirect);
            changer.SetColor(RandomColor());
            m_Counter = 0;
        }
    }

    //if using the LoadComponentAsync version above...
    void LoadDone(AsyncOperationHandle<ColorChanger> obj)
    {
        if (obj.Result != null)
        {
            ColorChanger changer = Instantiate(obj.Result);
            changer.SetColor(RandomColor());
        }
    }

    //if using the InstantiateComponentAsync version above...
    void InstantiateDone(AsyncOperationHandle<ColorChanger> obj)
    {
        if(obj.Result != null)
            obj.Result.SetColor(RandomColor());
    }

    
    
    Color RandomColor()
    {
        return new Color(UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f));
    }

}

