using UnityEngine;

public class spawner : MonoBehaviour
{
    int m_Counter = 0;
    void FixedUpdate()
    {
        if (!SyncAddressables.Ready)
            return;
        
        m_Counter++;

        if (m_Counter == 10)
        {
            var go = SyncAddressables.Instantiate("Cube");
            go.transform.forward = new Vector3(Random.Range(0, 180), Random.Range(0, 180), Random.Range(0, 180));
        }

        if (m_Counter >= 60)
            m_Counter = 0;
    }
}
