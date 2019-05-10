using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

public class FilteredReferences : MonoBehaviour
{
    [Serializable]
    public class AssetReferenceMaterial : AssetReferenceT<Material>
    {
        public AssetReferenceMaterial(string guid) : base(guid) { }
    }
    
    public AssetReferenceGameObject leftObject;
    public AssetReferenceGameObject rightObject;
    public AssetReferenceMaterial spawnMaterial;
    public AssetReferenceMaterial midMaterial;
    public AssetReferenceMaterial lateMaterial;

    public Vector3 leftPosition;
    public Vector3 rightPosition;

    MeshRenderer m_LeftMeshRender;
    MeshRenderer m_RightMeshRender;
    
    void Start()
    {
        leftObject.LoadAssetAsync();
        rightObject.LoadAssetAsync();
        spawnMaterial.LoadAssetAsync();
        midMaterial.LoadAssetAsync();
        lateMaterial.LoadAssetAsync();
    }

    int m_FrameCounter = 0;

   
    //Note that we never actually wait for the loads to complete.  We just check if they are done (if the asset exists)
    //before proceeding.  This is often not going to be the best practice, but has some benefits in certain scenarios.
    void FixedUpdate()
    {
        m_FrameCounter++;
        if (m_FrameCounter == 20)
        {
            if (leftObject.Asset != null)
            {
                var leftGo = Instantiate(leftObject.Asset, leftPosition, Quaternion.identity) as GameObject;
                m_LeftMeshRender = leftGo.GetComponent<MeshRenderer>();
            }

            if (rightObject.Asset != null)
            {
                var rightGo = Instantiate(rightObject.Asset, rightPosition, Quaternion.identity) as GameObject;
                m_RightMeshRender = rightGo.GetComponent<MeshRenderer>();
            }

            if (spawnMaterial.Asset != null && m_LeftMeshRender != null && m_RightMeshRender != null)
            {
                m_LeftMeshRender.material = spawnMaterial.Asset as Material;
                m_RightMeshRender.material = spawnMaterial.Asset as Material;
            }
    }

        if (m_FrameCounter == 40)
        {
            if (midMaterial.Asset != null && m_LeftMeshRender != null && m_RightMeshRender != null)
            {
                m_LeftMeshRender.material = midMaterial.Asset as Material;
                m_RightMeshRender.material = midMaterial.Asset as Material;
            }
        }

        if (m_FrameCounter == 60)
        {
            m_FrameCounter = 0;
            if (lateMaterial.Asset != null && m_LeftMeshRender != null && m_RightMeshRender != null)
            {
                m_LeftMeshRender.material = lateMaterial.Asset as Material;
                m_RightMeshRender.material = lateMaterial.Asset as Material;
            }
        }
    }

    void OnDisable()
    {
        //note that this may be dangerous, as we are releasing the asset without knowing if the instances still exist.
        // sometimes that's fine, sometimes not.
        leftObject.ReleaseAsset();
        rightObject.ReleaseAsset();
        spawnMaterial.ReleaseAsset();
        midMaterial.ReleaseAsset();
        lateMaterial.ReleaseAsset();
    }
}
