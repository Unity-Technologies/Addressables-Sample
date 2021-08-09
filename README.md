# Addressables-Sample
Demo project using Addressables package

These samples are broken up into projects based on high level functionality.  These are intended as jumping off points for your own development.  These have not been tested, and are not guaranteed to work in your situation.  They are just examples, to make some concepts easier to understand, or easier to replicate in your own project.  Use at your own risk.

## Projects

#### *Basic/Basic AssetReference*
Several sample scenes to display functionality surrounding the asset reference class.
* Scenes/BasicReference
  * Simplest example using a reference to spawn and destroy a game object
  * Each object is instantiated directly via the `AssetReference` which will increment the ref count.
  * Each object spawned has a script on it that will cause it to release itself after a certain amount of time. This destroys the object and decrements the ref count.
  * Any instances still around when the scene closes will automatically be released (decrementing ref count) by the scene closing process (even if their self-destruct script were disabled).
* Scenes/ListOfReferences
  * Showcases using references within a list.
  * Key feature: once an `AssetReference` is loaded it keeps a member called `.Asset`.  In this example, you would not want to use the on-complete callback to save off the asset as the loads may not complete in the same order as they were triggered. Thus, it's useful that the reference keeps up with its own loaded asset.
  * Here the objects are instantiated via the traditional `GameObject.Instantiate` which will not increment the Addressables ref count.  These objects still call into Addressables to release themselves, but since they were not instantiated via Addressables, the release only destroys the object, and does not decrement the ref count.
  * The manager of these AssetReferences must release them in `OnDestroy` or the ref count will survive the closing of the scene.
* Scenes/FilteredReferences
  * Showcases utilizing the various filtering options on the `AssetReference` class.
  * This scene also shows an alternative loading patter to the one used in other scenes.  It shows how you can utilize the Asset property.  It is recommended that you only use the Asset for ease of load.  You could theoretically also use it to poll completion, but you would never find out about errors in that usage.
  * This sample shows loading via the `AssetReference` but instantiating via Unity's built in method.  This will only increment the ref count once (for the load).
  * Currently, the objects created are being destroyed with Addressables.ReleaseInstance even though they were not created that way.  As of version 0.8, this will throw a warning, but still delete the asset.  In the future, our intent is to make this method not destroy the asset, or print a warning.  Instead it will return a boolean so you can destroy manually if needed.
* Scenes/SubobjectReference
  * Showcases using references with sub objects.
  * An `AssetReference` contains an main asset (`editorAsset`) and an optional sub object. Certain reference types (for example, references to sprite sheets and sprite atlases) can use sub objects. If the reference uses a sub object, then it will load the main asset during edit mode and load the sub object during runtime.
  * This scene shows loading a sprite from a sprite sheet (main asset) and loading a sprite as a sub object during runtime.

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

#### *Basic/ComponentReference*
This example creates an AssetReference that is restricted to having a specific Component.
* Scenes/SampleScene
  * This scene has a Spawner game object that alternates between spawning a direct reference prefab and an addressable one.
  * Both the direct reference and the addressable ComponentReference can only be set to one of the prefabs with the component ColorChanger on it.
* Scripts/ComponentReference - ComponentReference<TComponent>
  * This is the class that inherits from AssetReference.  It is generic and does not specify which Components it might care about.  A concrete child of this class is required for serialization to work.
  * At edit-time it validates that the asset set on it is a GameObject with the required Component.
  * At runtime it can load/instantiate the GameObject, then return the desired component.  API matches base class (LoadAssetAsync & InstantiateAsync).
* Scripts/ColorChanger - ColorChanger & ComponentReferenceColorChanger
  * The component type we chose to care about.
  * Note that this file includes a concrete version of the ComponentReference.  This is needed because if your game code just specified a ComponentReference<ColorChanger> it could not serialize or show up in the inspector.  This ComponentReferenceColorChanger is what makes serialization and the inspector UI work.
  * Releasing a ComponentReference<TComponent> should be done through `ReleaseInstance()` in the ComponentReference<TComponent> class. To release an instance directly, see our implementation of ReleaseInstance to understand the requirements.

#### *Basic/Sprite Land*
*2019.3.0a11+* - Sprite demo is back.  There was an engine bug causing a crash when loading out of sprite sheets that caused us to remove this demo.  This is in 2019.3 alpha, and is being backported to 2019.2 and 2018.4.  If you use this demo, and your game crashes, or you get warnings about "gfx" not on main thread, you don't have a fixed version of the platform.
There are three sprite access methods currently demo'd in this sample.  The on-screen button will change functionality with each click, so you can demo the methods in order.
* Scenes/SampleScene
  * First is having an AssetReference directly to a single sprite.  Since this sprite is a single entry, we can reference the asset directly and get the sprite.  This is the most simple case.
  * Second is accessing a sprite from within a sprite sheet. This is the one that was causing a crash, but should be fixed now.  Here we load the sprite sheet asset as type IList<Sprite>.  This tells addressables to load all the sub-objects that are sprites, and return them as a list.
  * Third is accessing a sprite from within an atlas. In this case, you have to use addressables to load the sprite atlas, then use the normal atlas API to load a given sprite from it.  This example also shows extending the AssetReference<T> to provide a typed reference that Addressables doesn't come with (AssetReferenceT<SpriteAtlas> in this case).
* All code is in Scripts/SpriteControlTest.cs

#### *Basic/Space Shooter*
A very simple Unity tutorial that we converted to use addressables.  The main code file to look at would be Done_GameController.cs, but in general, this is just meant as a simple project to explore.

#### *Advanced/Addressables Variants*
An example project to show two use cases or workflows for creating "variants".  The new build pipeline (Scriptable Build Pipeline) upon which Addressables is built, does not support asset bundle variants.  This old mechanism was useful in many instances, so this sample is meant to show how to accomplish similar results for one of those instances.  There are other purpose for variants not shown here.  Some will be coming in future samples.
* Scenes/TextureScalerScene
  * In the scene, there's a prefab with an existing texture that can load alternate textures based on button input.  (VariationController.cs)
  * The project only has one instance of the texture in question (Texture/tree2.png).  It is marked as addressable, with the address "tree" and no labels
  * The group containing "tree" has a custom schema added to it (TextureVariationSchema).  This defines the label for the provided texture, and a scale and label for alternate textures.
  * For "Use Asset Database (fastest)" in the player, run with the play mode script of "Variant Use Asset Database (fastest)".  This will look at all the label variations defined in the schema, and apply all labels to the "tree".  This will then go into play mode in the normal "Use Asset Database (fastest)" setup of loading from AssetDatabase, but will fake having an "HD", "SD", etc. version of the texture.
  * For "Simulate Groups (Advanced)" in the player, run with the play mode script of "Variant Simulate Groups (Advanced)".  This will do the same things as the "Variant Use Asset Database (fastest)" script above.  Note, this is not a very accurate "Simulate Groups (Advanced)" mode right now because it does not emulate the fact that each variety should be in its own bundle.
  * With the build script of "Pack Variations" selected, building the player content will:
    * Find all groups with the TextureVariationSchema
    * Loop over all size/label pairs, and copy the textures on-disk.
    * Change the import settings on the created textures so as to scale them.
    * Build all bundles
    * Remove the extra files/groups that were created.
  * After building with "Pack Variations", you can enter play mode using the standard "Use Existing Build (requires built groups)" script.
* Scenes/PrefabTextureScalerScene
    * In this scene an Addressable prefab that gets duplicated with variant textures.
    * The project only has one instance of the prefab (Assets/Cube.prefab).  This prefab has a Material that references a texture in the project.
    * The group containing the prefab has a custom schema attached to it (PrefabTextureVariantSchema.cs).  This schema defines the label for the default prefab as well as labels and texture scales for the variant prefabs and their textures.  PrefabTextureVariantSchema will only iterate over GameObjects that have a MeshRenderer with a valid material and texture assigned to it.
    * For "Use Asset Database (fastest)" in the player, run with the play mode script of "Variant Use Asset Database (fastest)".  This will look at all the label variations defined in the schema, and apply all labels to the "Prefab".  This will then go into play mode in the normal "Use Asset Database (fastest)" setup of loading from AssetDatabase, but will fake having an "LowRes", "MediumRes", etc. version of the prefab.
    * For "Simulate Groups (Advanced)" in the player, run with the play mode script of "Variant Simulate Groups (Advanced)".  This will do the same things as the "Variant Use Asset Database (fastest)" script above.  Note, this is not a very accurate "Simulate Groups (Advanced)" mode right now because it does not emulate the fact that each variety should be in its own bundle.
    * With the build script of "Pack Variations" selected, building the player content will:
        * Find all groups with the PrefabTextureVariantSchema.
        * Iterate through the group and duplicate any GameObjects into an "AddressablesGenerated" folder on-disk and mark the duplicates as Addressables.
        * Check if the asset hash for an entry has changed and only create new variants whose source has a new asset hash.
        * Iterate through each label/scale pair and create on-disk copies of the materials and textures for each of those GameObjects.
        * Change the import settings on the created textures so as to scale them accordingly.
        * Build the AssetBundles.
        * Remove the extra groups that were created.
        * Removed unused variant assets.
    * After building with "Pack Variations", you can enter play mode using the standard "Use Existing Build (requires built groups)" script.

#### *Advanced/Sync Addressables*
Synchronous Addressables!  What a crazy thing.  The value of exploring this demo can be broken into two categories.  One is looking at what would be involved in making addressables synchronous.  The other is looking at creating custom providers.
On the synchronous front, this can be used as a starting point for making your own project support synchronous loading.  As you can see in the code, there are a lot of fail cases, but if you can know that things are on-device and ready to go, it should work.
For custom providers, this project has a couple examples.  Custom providers are a really good way to extend addressables.  They are relatively easy to create and set up, but open up a lot of opportunity to inject logic during your load or instantiate.

Why don't we put these sync methods in Addressables itself?  The best way to understand that is to look at SyncAddressables/SyncAddressables.cs and search for "throw".  The code is very specific about how it needs to be used, and will cause pain for the caller if not used in the right way at the right time.  That being said, if you want to create a game built on sync interfaces, you can copy this code, and run with it.  If you are using it, all the existing async methods would still work, so you are capable of doing a mix & match in your game, if you are willing to accept the constraints when doing things sync. Note that the group schema is what associates a given asset group with either the sync providers or the regular ones.  So you could not mix & match within a group.

One common workflow not shown here would have been to set things up to support async loading, but sync instantiation.  This would only work if the game always instantiated after loading was complete. That complicates the game-code, but is a simplified version of this demo from the addressables standpoint.

*Not all play modes done.*  Packed content (for play mode, or the player) needs no custom builders.  "Use Asset Database (fastest)" mode and "Simulate Groups (advanced)" mode on the other hand do.  At this point, we have only implemented a sample script for "Use Asset Database (fastest)" Mode.
* Scenes/SampleScene
  * This scene waits until the SyncAddressables system has been initialized, and then starts spawning a cube every 60 fixed-update calls.
* SyncAddressables code
  * SyncAddressables.cs - A class that simply calls into Addressables and adds some synchronous guards.  Contains methods for:
    * `Ready()` - True if the main addressables has finished initializing.
    * `LoadAsset<>()` & `Instantiate()` - Calls the addressables version of the method, returning the result if things were ready, throwing exceptions if not.
  * SyncBundleProvider.cs - Loads the asset bundle into memory using synchronous methods.  If the bundle is online this will fail.  Also note, in it's current form, this will fail on Android as loading there is a little more complex.  It can load sync, we just didn't have time to add that support to this demo.  This is the most likely point in the flow for there to be an issue in the sync process.  If this were used in production, it would probably need extended error checking.
  * SyncBundledAssetProvider.cs - Loads from an asset bundle using the synchronous methods.  This is unlikely to be a failure point, as it isn't called until the bundle is loaded successfully.
  * Editor/SyncFastModeBuild.cs - Since "Use Asset Database (fastest)" mode does not load from bundles, the default "Use Asset Database (fastest)" mode script has to inject it's own provider for all assets.  This custom script just replaces that standard provider with a sync one.
  * SyncAssetDatabaseProvider.cs - An overridden provider to do asset database loads immediately.
  * No Change Needed: SyncBuildScriptPackedMode or SyncBuildScriptPackedPlayMode.  Since the group schema allows you to specify provider, the standard build script works as is.
  * Missing - the two main things missing from this demo are "Simulate Groups (Advanced)" and the ability to load from Resources using the sync interfaces.

#### *Advanced/Custom Analyze Rule*
This sample shows how to create custom AnalyzeRules for use within the Analyze window.  Both rules follow the recommended pattern for adding themselves to the UI.  There are no scenes to look at in this project, just analyze code.
* Editor/AddressHasC
  * This is a non-fixable rule (meaning it will not fix itself).
  * When run, it checks that all addresses have a capital C in them.  Any that do not are flagged as errors.
  * A rule like this would be useful if your studio enforced some sort of naming convention on addresses. (though it would probably be best if it could fix itself)
* Editor/PathAddressIsPath
  * This is a fixable rule.  Running fix on it will change addresses to comply with the rule.
  * When run, it first identifies all addresses that seem to be paths.  Of those, it makes sure that the address actually matches the path of the asset.
  * This would be useful if you primarily left the addresses of your assets as the path (which is the default when marking an asset addressable).  If the asset is moved within the project, then the address no longer maps to where it is. This rule could fix that.

#### *Advanced/Play Asset Delivery*
An example project that shows how to use [Play Asset Delivery](https://docs.unity3d.com/Manual/play-asset-delivery.html) with Addressables. SampleScene contains 3 buttons that will load or unload an asset that was assigned to an asset pack of a specific delivery type.
The basic workflow is:
- An Addressable Group is assigned to an asset pack. Multiple groups can be assigned to the same asset pack. Just be mindful of the [size restrictions per delivery mode](https://developer.android.com/guide/playcore/asset-delivery#custom-asset-packs). Note, at build time the build processor (either PlayAssetDeliveryBuildProcessor if using Unity 2021.2+ and Addressables 1.19.0+, or AddressablesPlayerBuildProcessor otherwise) will temporarily move all local content to 'Assets/StreamingAssets'. This means that any local content will be automatically included in the streaming assets pack even if they are not assigned to a custom asset pack.
  - Each group uses local build & load paths. At runtime these paths are overwritten to use asset pack locations.
  - Also each group uses a custom AssetBundleProvider that ensures the asset pack containing the AssetBundle is installed/downloaded before attempting to load the bundle.
- All content marked for "install-time" delivery is assigned to the streaming assets pack. Custom asset packs are created for all other delivery modes.
- Use the custom build script to prepare bundled content for [custom asset pack creation](https://docs.unity3d.com/Manual/play-asset-delivery.html#custom-asset-packs). This will also create a json file that stores custom asset pack information to be used at runtime.
- At runtime make sure that [generated asset packs](https://docs.unity3d.com/Manual/play-asset-delivery.html#generated-asset-packs) are downloaded, configure custom Addressables properties, and load all custom asset pack information.

Setup Instructions:
1. Configure Unity to build Android App Bundles and split the application binary. For more information see ["Using Play Asset Delivery"](https://docs.unity3d.com/Manual/play-asset-delivery.html#using-play-asset-delivery).
  1. In File > Build Settings:
    - Set the Platform to Android.
    - If "Export Project is enabled", enable "Export for App Bundle". Otherwise, enable "Build App Bundle (Google Play)".
  2. If you want any bundled content to use "install-time" delivery, select "Split Application Binary" in Edit > Project Settings > Player > Publishing Settings.
3. Open the Addressables Groups window (Window > Asset Management > Addressables Groups).
4. In the Groups window toolbar, select Create > Group > Asset Pack Content to create a new group whose content will be assigned to an asset pack. The group contains 2 schemas “Content Packing & Loading” and "Play Asset Delivery”. 
  - If you don't want the group's content to be assigned to an asset pack (i.e. remote content that will be hosted on a different CDN) remove the "Play Asset Delivery” schema. Then in "Content Packing & Loading" set the “Build & Load Paths” to remote paths (RemoteBuildPath and RemoteLoadPath) and in "Advanced Options" set the "Asset Bundle Provider" to the default bundle provider (AssetBundle Provider). Alternatively you can do Create > Group > Packed Assets to create a default bundled asset group, but make sure to use remote paths (RemoteBuildPath and RemoteLoadPath). Any local content will be automatically assigned to the streaming assets pack.
5. Specify the assigned asset pack in “Play Asset Delivery” schema. Select "Manage Asset Packs" to modify custom asset packs.
  - Any groups that do not have this schema or use "install-time" delivery will have their bundles assigned to streaming assets pack. In most cases the streaming assets pack will use "install-time" delivery, but in large projects it may use "fast-follow" delivery instead. For more information see ["Generated Asset Packs"](https://docs.unity3d.com/Manual/play-asset-delivery.html#generated-asset-packs).
  - Assign all content intended for "install-time" delivery to the "InstallTimeContent" asset pack. This is a "placeholder" asset pack that is representative of the streaming assets pack. No custom asset pack named "InstallTimeContent" is actually created.
6. In the “Content Packing & Loading” schema:
  1. Set the “Build & Load Paths” to the default local paths (LocalBuildPath and LocalLoadPath). At runtime we will configure the load paths to use asset pack locations (see AddressablesInitSingleton.cs).
    - Since the Google Play Console doesn't provide remote URLs for uploaded content, it is not possible to use remote paths or the Content Update workflow for content assigned to asset packs. Remote content will need to be hosted on a different CDN.
  2. In Advanced Options > Asset Bundle Provider use the “Play Asset Delivery Provider”. This will make sure that asset packs are downloaded before loading content from AssetBundles.
7. Build Addressables using the custom “Play Asset Delivery” build script. In the Addressables Groups Window, do Build > New Build > Play Asset Delivery.
  1. Prepare each bundle file to be assigned to its own asset pack:
    - Each asset pack will have a directory named “{asset pack name}.androidpack” in “Assets/PlayAssetDelivery/CustomAssetPackContent".
    - Each .androidpack directory also contains a ‘build.gradle’ file that specifies that delivery type for the asset pack. If this file is missing, Unity will assume that the asset pack’s delivery type is “on-demand”.
  2. Create a "CustomAssetPacksData.json" file in 'Assets/StreamingAssets' that stores all custom asset pack information to be used at runtime.
8. Prepare Addressables to load content from asset packs at runtime (we do this in AddressablesInitSingleton.cs).
  1. Make sure that the generated asset packs are downloaded.
  2. Configure our custom InternalIdTransformFunc, which converts internal ids to their respective asset pack location.
  3. Load all custom asset pack data from the "CustomAssetPacksData.json".
  4. Then load assets using the Addressables API (we do this in LoadObject.cs).
9. When ready to build the Android App Bundle, open File > Build Settings and click “Build”. This will create all of our custom asset packs along with the generated asset packs.
  - If you want to upload the App Bundle to the Google Play Console, make sure that you are doing a release build.
