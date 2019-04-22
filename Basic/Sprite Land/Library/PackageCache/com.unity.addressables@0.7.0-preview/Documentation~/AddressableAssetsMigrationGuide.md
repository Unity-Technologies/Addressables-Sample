# Migration guide

The following explains how modify your existing application to use Addressable Assets.  The three typical methods for referencing Assets are:

* __[Direct References](#directreferences)__ - Adding Assets directly into components or scenes (loaded automatically). __Note__: The Addressable Asset System loads Assets asynchronously. When you update your direct references to Asset references, you must also update your code to operate asynchronously.
* __[Resource Folders](#referencefolders) - Adding Assets to your *Resource* folder and loading them by filename.
* __[Asset Bundles](#assetbundles)__ - Adding Assets to Asset bundles, then loading them with their dependencies by path.

<a name="directreferences"></a>
### Direct References

Replace direct references to objects with Asset references. For example, change this:

`public GameObject directRefMember;`

To:

`public AssetReference AssetRefMember;`

Then drag Assets onto the owning component’s Inspector as you would do for a direct reference.

If you would like to load an Asset based on an object rather than a string name, instantiate it directly from the __AssetReference__ object you created in your setup.

For example:

`AssetRefMember.Load<GameObject>();`

or

`AssetRefMember.Instantiate<GameObject>(pos, rot);`

<a name="referencefolders"></a>
### Resource Folders

When you mark an Asset that is located in a Resources folder as addressable, the  Addressable Asset System automatically moves the Asset from the Resources folder to a new folder in your Project named *Resources_moved*.  The default address for a moved Asset is the old path starting just after "Resources".  This means your loading code can change from:

`Resources.LoadAsync<GameObject>("desert/tank.prefab");`

To:

`Addressables.Load<GameObject>("desert/tank.prefab");`

<a name="assetbundles"></a>
### Asset Bundles

When you open the Addressables window, Unity offers to convert all bundles into Addressable Asset Groups. This is the easiest way to migrate your code.

If you choose to convert your Assets manually, click the __Ignore__ button. Then use either the direct reference or resource folder methods previously described.

The default path for the address of an Asset is its file path. If you use the path as the address, you load the Asset in the same manner as you would load from a bundle. The Addressable Asset System handles the loading of the bundle and all dependencies.