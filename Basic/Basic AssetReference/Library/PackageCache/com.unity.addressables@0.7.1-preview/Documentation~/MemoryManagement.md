# Memory Mangement

### Mirroring Load and Unload
The primary way to ensure your memory is cleaned up properly in Addressables is to mirror your load and unload calls correctly. How to do this depends on the type of asset and method of load.  In all cases, however, the release method can either take the thing that was loaded, or a handle to the operation returned by the load.  For example, in the case of scene creation, the load returns a `AsyncOperationHandle<SceneInstance>`.  You can release via this returned handle, or by keeping up with the `handle.Result`, which in this case will be a `SceneInstance`.

**Asset Loading**
To load, use `Addressables.LoadAsset` or `Addressables.LoadAssets`.
This will load the asset into memory, but not instantiate it.  It will add one to the ref-count for each asset loaded, each time the Load call executes.  If you call `LoadAsset` three times with the same address, you will get three different instances of an `AsyncOperationHandle` back, that all reference the same underlying operation.  That operation will have a ref-count of three for that asset.  The resulting `AsyncOperationHandle` will have the asset in the `.Result` field if the load was successful.  You can use the loaded asset to instantiate using Unity's built in instantiation methods, which will not increment the Addressables ref-count.

To unload, use `Addresslabes.Release`. This will decrement the ref-count.  Once a given asset's ref-count is at zero, it will be unloaded. 

**Scene Loading**
To load, use `Addressables.LoadScene`.
You can use this method to load a scene in `Single` mode, which will close all open scenes, or in `Additive` mode. See https://docs.unity3d.com/ScriptReference/SceneManagement.LoadSceneMode.html.  

To unload, use `Addressables.UnloadScene`, or open a new scene in `Single` mode.  When opening a new scene, you can do so either with the above Addressables interface or `SceneManager.LoadScene`.  Opening a new scene will close the current one, which will properly decrement the ref-count.

**Game Object Instantiation**
To load, use `Addressables.Instantiate`.
This will instantiate the Prefab specified by the passed in `location`. Addressables will load the Prefab and its dependencies, incrementing the ref-count of all items during that load.  Calling `Instantiate` three times on the same address will result in all dependent assets having a ref-count of three.  Unlike calling LoadAsset three times, however, each `Instantiate` call will get an `AsyncOperationHandle` pointing to a unique operation.  This is because the result of each `Instantiate` is a unique instance.  Another distinction between `Instantiate` and other load calls, is the optional parameter `trackHandle`.  If this is set to false, then you must keep the `AsyncOperationHandle` around and use that during the releasing of your instance.

To unload, use `Addressables.ReleaseInstance` or close the scene that the instantiated object is in.  This scene could have been loaded (and thus closed) in `Additive` or `Single` mode.  This scene could also have been loaded via Addressables or SceneManagement.  As noted above, if you set `trackHandle` to false, you can only call `Addressables.ReleaseInstance` with the handle, not with the actual GameObject.

A note on `Object.Destroy()` and `Addressables.ReleaseInstance`.  If you call `Addressables.ReleaseInstance` on an instance that was not created via Addressables, we detect that, and simply call Object.Destroy() on it for you.  Due to potential risks of memory leaks, we will not continue to support this forever.  Currently it prints an error while still doing the Destroy.  Eventually it will just print an error. 

`Addressables.Instantiate` has some overhead associated with it, so if you need to instantiate the same objects 100s of times per frame, you should consider loading via Addressables, then instantiate outside Addressables.  In this instance, you would call `Addressables.LoadAsset`, then save off the result, and call `GameObject.Instantiate()` on that result.  This gives you the flexibility to call Instantiate in a synchronous way.  The downside is that we will have no idea how many instances you have created.  If you still have instances floating around when you call `Addressables.Release`, you will likely end up in a bad situation.  For example, a prefab referencing a texture, would no longer have a valid (loaded) texture to reference, causing rendering issues (or worse).  These sorts of problems can be hard to track down as we may not immediately trigger the memory unload (see "When memory is cleared" below).

### Addressable Profiler
To open the Addressable Profiler window, use *Window->Asset Management->Addressable Profiler*.  You also need to enable *Send Profiler Events* on your Addressable Asset Settings.  This can be done on the inspector for that asset, either by finding the asset in your project, or by selecting a group in the Addressable window, which then allows you to navigate (in the inspector) to the top level settings.

The purpose of this window is to show you the status of ref-counts on all the Addressables System operations. These operations include asset bundle loading, asset loading, etc.
* A white vertical line indicates the frame in which a load request occurs.
* The blue background indicates that the asset in question is currently loaded.  
* The green part of the graph indicates current ref-count.

##### When is memory cleared
An asset no longer being referenced (the end of a blue section in the profiler) does not necessarily mean that the item has been unloaded.  The key scenario in question is multiple assets in an asset bundle.  Say you have three assets: "tree", "tank", and "cow" in an asset bundle called "stuff".  When "tree" is loaded, you'll see a single ref count on "tree" as well as one on "stuff".  Then later, if you load "tank", you'll see a single count on each "tree" and "tank", and a count of two on "stuff".  Now if you release "tree", it's count will go to zero, and the blue bar will go away.  The asset is not actually unloaded at this point.  With asset bundles, you can load a bundle, and part of the contents, but you cannot partially unload a bundle.  No asset in "stuff" will be unloaded until that bundle is done with and completely unloaded.  
The exception to this rule is the engine interface `Resources.UnloadUnusedAssets` (see https://docs.unity3d.com/ScriptReference/Resources.UnloadUnusedAssets.html).  If you execute this in the above scenario, "tree" will be unloaded.  Because we cannot be aware of these events, we only have the graph reflecting our ref-counts, not exactly what memory holds.  Note that if you choose to use `Resources.UnloadUnusedAssets`, it is a very slow operation, and should only be called during a screen that won't show any hitches (such as a loading screen).
