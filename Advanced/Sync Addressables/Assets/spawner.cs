using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

public class spawner : MonoBehaviour
{
    int m_Counter = 0;
    List<GameObject> m_instances = new List<GameObject>();

    private void Update()
    {
        if (Input.GetKeyDown("space"))
        {
            if (m_instances.Count == 0)
                return;
            Debug.Log($"Release {m_instances.Count} instances");
            foreach (var oldGo in m_instances)
            {
                Addressables.ReleaseInstance(oldGo);
            }
            m_instances.Clear();
        }
    }

    void FixedUpdate()
    {
        if (!SyncAddressables.Ready)
            return;

        m_Counter++;

        if (m_Counter == 10)
        {
            var go = SyncAddressables.Instantiate("Cube");
            go.transform.forward = new Vector3(Random.Range(0, 180), Random.Range(0, 180), Random.Range(0, 180));
            m_instances.Add(go);
        }

        if (m_Counter >= 60)
            m_Counter = 0;
    }
}
