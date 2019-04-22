using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal class MonoBehaviourCallbackHooks : MonoBehaviour
{
    public event Action<float> OnUpdateDelegate;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        if (OnUpdateDelegate != null)
            OnUpdateDelegate(Time.unscaledDeltaTime);
    }
}
