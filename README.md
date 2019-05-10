# Addressables-Sample
Demo project using Addressables package

This sample is broken up into projects based on high level functionality.  These are intended as jumping off points for your own development.  

## Projects

#### *Basic/Basic AssetReference*
Several sample scenes to display functionality surrounding the asset reference class.  
* Scenes/BasicReference 
  * Simplest example using a refrence to spawn and destroy a game object
  * Each object is instantiated directly via the `AssetReference` which will increment the ref count.
  * Each object spawned has a script on it that will cause it to release itself after a certain amount of time. This destroys the object and decrements the ref count.
  * Any instances still around when the scene closes will automatically be released (decrementing ref count) by the scene closing process (even if their self-destruct script were disabled).
* Scenes/ListOfReferences
  * Showcases using references within a list.  
  * Key feature: once an `AssetReference` is loaded it keeps a member callled `.Asset`.  In this example, you would not want to use the on-complete callback to save off the asset as the loads may not complete in the same order as they were triggered. Thus it's useful that the reference keeps up with its own loaded asset.
  * Here the objects are instantiated via the traditional `GameObject.Instantiate` which will not increment the Addressables ref count.  These objects still call into Addressables to release themselves, but since they were not instantiated via Addressables, the release only destroys the object, and does not decrement the ref count.
  * The manager of these AssetReferences must release them in `OnDestroy` or the ref count will survive the closing of the scene. 
* Scenes/FilteredReferences
  * Showcases utilizing the various filtering options on the `AssetReference` class.
  * This scene also shows an alternative loading patter to the one used in other scenes.  It shows how you can utilize the Asset property.  It is recommended that you only use the Asset for ease of load.  You could theoretically also use it to poll completion, but you would never find out about errors in that usage. 
  * This sample shows loading via the `AssetReference` but instantiating via Unity's built in method.  This will only increment the ref count once (for the load).
  * Currently, the objects created are being destroyed with Addressables.ReleaseInstance even though they were not created that way.  As of version 0.8, this will throw a warning, but still delete the asset.  In the future, our intent is to make this method not destroy the asset, or print a warning.  Instead it will return a boolean so you can destroy manually if needed. 

#### *Basic/Scene Loading*
The ins and outs of scene loading.
* Scenes/Bootstrap
  * This is the scene to start with.  From this one you can transition to the other scenes.
  * "Transition to Next Scene" will open a new scene (non-additively), which will close the current scene.  
  * "Add Object" will instantiate an addressable prefab into the scene.  The point of this button is to show that these items do not need ReleaseInstance called on them.  Should you use the Profiler, you will see they get cleaned up on scene close.
* Scenes/Foundation
  * This is the scene you transition to from Bootstrap.
  * "Load *" buttons will open other scenes additively. 
  * "Unload *" buttons will unload scenes that have been additively loaded.
* Scenes/ItemScenes/*
  * These scenes just contain items with no code.  Their purpose is to be additively loaded by the Foundation scene.
  
#### ~~Basic/Sprite Land~~
This has been removed due to an engine bug.  We are investigating, but we are keeping the info here so those overly curious will know they can hunt it down.
  
#### *Advanced/Texture Variations*
An example project to show one use case or workflow for creating "variants".  The new build pipeline (Scriptable Build Pipeline) upon which Addressables is built, does not support asset bundle variants.  This old mechanism was useful in many instances, so this sample is meant to show how to accomplish similar results for one of those instances.  There are other purpose for variants not shown here.  Some will be coming in future samples.
* Scenes/SampleScene
  * In the scene, there's a prefab with an existing texture that can load alternate textures based on button input.  (VariationController.cs)
  * The project only has one instance of the texture in question (Texture/tree2.png).  It is marked as addressable, with the address "tree" and no labels
  * The group containing "tree" has a custom schema added to it (TextureVariationSchema).  This defines the label for the provided texture, and a scale and label for alternate textures.
  * For "Fast Mode" in the player, run with the play mode script of "Vary Fast".  This will look at all the label variations defined in the schema, and apply all labels to the "tree".  This will then go into play mode in the normal Fast Mode setup of loading from AssetDatabase, but will fake having an "HD", "SD", etc. version of the texture. 
  * For "Virtual Mode" in the player, run with the play mode script of "Virtual Variety".  This will do the same things as the "Vary Fast" script above.  Note, this is not a very accurate virtual mode right now because it does not emulate the fact that each variety should be in its own bundle. 
  * With the build script of "Pack Variations" selected, building the player content will:
    * Find all groups with the TextureVariationSchema
	* Loop over all size/label pairs, and copy the textures on-disk.
	* Change the import settings on the created textures so as to scale them.
	* Build all bundles
	* Remove the extra files/groups that were created.
  * After building with "Pack Variations", you can enter play mode using the standard "Packed Play Mode" script.
   
#### *Advanced/Sync Addressables*
Synchronous Addressables!  What a crazy thing.  This sample shows how you could set up your project to utilize Addressables, but in a (nearly) completely synchronous way.
Why don't we put these sync methods in Addressables itself?  The best way to understand that is to look at SyncAddressables/SyncAddressables.cs and search for "throw".  The code is very specific about how it needs to be used, and will cause pain for the caller if not used in the right way at the right time.  That being said, if you want to create a game built on sync interfaces, you can copy this code, and run with it.  If you are using it, all the existing async methods would still work, so you are capable of doing a mix & match in your game, if you are willing to accept the constraints when diong things sync. Note that the group schema is what associates a given asset group with either the sync providers or the regular ones.  So you could not mix & match within a group.
*Packed Mode and Fast Mode only*.  We have not implemented a sample script for Virtual mode.  Note that for packed mode, you actually use the standard packed script.  For packed mode, it's the bundle building that needs a custom script, not the 
* Scenes/SampleScene
  * This scene waits until the SyncAddressables system has been initialized, and then starts spawning a cube every 60 fixed-update calls. 
* SyncAddressables code
  * SyncAddressables.cs - A class that simply calls into Addressables, and adds some synchronous guards.  Contains methods for: 
    * `Ready()` - has the main addressables finished initializing.
	* `LoadAsset<>()` & `Instantiate()` - Calls the addressables version of the method, returning the result if things were ready, throwing exceptions if not.
  * SyncBundleProvider.cs - Loads the asset bundle into memory using synchronous methods.  If the bundle is online this will fail.  Also note, in it's current form, this will fail on Android as loading there is a little more complex.  It can load sync, we just didn't have time to add that support to this demo.  This is the most likely point in the flow for there to be an issue in the sync process.  If this were used in production, it would probably need much better error checking.
  * SyncBundledAssetProvider.cs - Loads from an asset bundle using the synchronous methods.  This is unlikely to be a failure point, as it isn't called until the bundle is loaded successfully.
  * Editor/SyncFastModeBuild.cs - Since fast mode does not load from bundles, the default fast mode script has to inject it's own provider for all assets.  This custom script just replaces that standard provider with a sync one. 
  * SyncAssetDatabaseProvider.cs - An overriden provider to do asset database loads immediately. 
  * Not Needed: SyncBuildScriptPackedMode or SyncBuildScriptPackedPlayMode.  Since the group schema allows you to specify provider, the standard build script works as is.
  * Missing - the two main things missing from this demo are Virtual mode and the ability to load from Resources using the sync interfaces.



  
