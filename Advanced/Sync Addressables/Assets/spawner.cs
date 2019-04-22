using UnityEngine;

public class spawner : MonoBehaviour
{

    int counter = 0;
    void FixedUpdate()
    {
        if (!SyncAddressables.Ready)
            return;
        
        counter++;

        if (counter == 1)
        {
//            var cube = SyncAddressables.LoadAsset<GameObject>("Cube");
//            if(cube == null)
//                Debug.LogWarning("null :(");
//            else
//                Debug.LogWarning("not null :)");

            var go = SyncAddressables.Instantiate("Cube");
           
            go.transform.forward = new Vector3(Random.Range(0,180), Random.Range(0,180), Random.Range(0,180));
        }


        if (counter >= 60)
            counter = 0;
    }
}
