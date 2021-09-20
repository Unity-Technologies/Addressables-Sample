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
An example project that shows how to use [Unity's Play Asset Delivery API](https://docs.unity3d.com/Manual/play-asset-delivery.html) with Addressables. SampleScene contains 3 buttons that will load or unload an asset that was assigned to an asset pack of a specific delivery type.

If you want to make [custom asset packs](https://docs.unity3d.com/Manual/play-asset-delivery.html#custom-asset-packs) use this project as a guide. Otherwise you don't need to change how Addressables is configured in your project. Just follow the instructions specified in the [Play Asset Delivery]((https://docs.unity3d.com/Manual/play-asset-delivery.html) manual page.

This is possible because the AddressablesPlayerBuildProcessor moves all content located in Addressables.BuildPath to the streaming assets path. Unity assigns streaming assets to the [generated asset packs](https://docs.unity3d.com/Manual/play-asset-delivery.html#generated-asset-packs).

##### Basic Workflow:
- Assign an Addressable Group to an asset pack. Multiple groups can be assigned to the same asset pack. Just be mindful of the [size restrictions per delivery mode](https://developer.android.com/guide/playcore/asset-delivery#custom-asset-packs).
- Unity automatically creates [generated asset packs](https://docs.unity3d.com/Manual/play-asset-delivery.html#generated-asset-packs) for built-in data and streaming assets. To assign content to the generated asset packs, we can just rely on the default Addressables behavior: any groups that use the Addressables.BuildPath will have their content included in the streaming assets path (see AddressablesPlayerBuildProcessor).
- For additional [custom asset packs](https://docs.unity3d.com/Manual/play-asset-delivery.html#custom-asset-packs), an “{asset pack name}.androidpack” directory in the Assets folder must be created for each asset pack, optionally containing a .gradle file specifying the asset pack's delivery type (default type is "on-demand" delivery). See how we do this in BuildScriptPlayerAssetDelivery.cs.
- To assign an AssetBundle to an asset pack, move the .bundle file to the “{asset pack name}.androidpack” directory (see PlayAssetDeliveryPreBuildProcessor.cs). We also maintain a system that keeps track of all our custom asset pack names which bundles are assigned to them (see BuildScriptPlayerAssetDelivery.cs and AddressablesInitSingleton.cs).
- Build the Android App Bundle and upload it to the Google Play Console to generate the APK.
- At runtime download asset packs before loading bundles from them (see PlayAssetDeliveryAssetBundleProvider.cs).

##### Setup Instructions:
 **Note**: [Play Asset Delivery](https://docs.unity3d.com/Manual/play-asset-delivery.html) requires Unity 2019.4+

###### Configure Build & Player Settings
Configure Unity to build Android App Bundles. For more information see ["Play Asset Delivery"](https://docs.unity3d.com/Manual/play-asset-delivery.html#using-play-asset-delivery).
1. In File > Build Settings:
  - Set the Platform to Android.
  - If "Export Project is enabled", enable "Export for App Bundle". Otherwise, enable "Build App Bundle (Google Play)".
2. If you want any bundled content to use "install-time" delivery, select "Split Application Binary" in Edit > Project Settings > Player > Publishing Settings.

###### Configure custom Addressable scripts
The sample project uses custom Addressable scripts that make it easier to build and load AssetBundles assigned to asset packs.

In 'Assets/PlayAssetDelivery/Editor' we have:
- A custom group template (Asset Pack Content.asset).
- A custom data builder script (BuildScriptPlayAssetDelivery.cs) and its serialized asset (BuildScriptPlayAssetDelivery.asset)
- A custom build processor (PlayAssetDeliveryBuildProcessor.cs)
- A custom schema (PlayAssetDeliverySchema.cs)

In 'Assets/PlayAssetDelivery/Runtime' we have:
- A custom AssetBundle Provider (PlayAssetDeliveryAssetBundleProvider.cs)

Configure the following properties in AddressableAssetSettings:
1. In "Build and Play Mode Scripts" add the custom data builder asset (BuildScriptPlayAssetDelivery.asset).
2. In "Asset Group Templates" add the custom group template (Asset Pack Content.asset).

###### Assign Addressable Groups to asset packs
1. In the Addressables Groups window (Window > Asset Management > Addressables Groups) toolbar, select Create > Group > Asset Pack Content to create a new group whose content will be assigned to an asset pack. The group contains 2 schemas “Content Packing & Loading” and "Play Asset Delivery”.
2. Specify the assigned asset pack in “Play Asset Delivery” schema. Select "Manage Asset Packs" to modify custom asset packs. Be mindful of the [size restrictions per delivery mode](https://developer.android.com/guide/playcore/asset-delivery#custom-asset-packs).
   - Assign all content intended for "install-time" delivery to the "InstallTimeContent" asset pack. This is a "placeholder" asset pack that is representative of the [generated asset packs](https://docs.unity3d.com/Manual/play-asset-delivery.html#generated-asset-packs). No custom asset pack named "InstallTimeContent" is actually created.
   - In most cases the generated asset packs will use "install-time" delivery, but in large projects it may use "fast-follow" delivery instead. For more information see ["Generated Asset Packs"](https://docs.unity3d.com/Manual/play-asset-delivery.html#generated-asset-packs).
   - **Note**: To exclude the group from the asset pack either disable "Include In Asset Pack" or remove the "Play Asset Delivery" schema. Additionally make sure that its "Content Packing & Loading" > “Build Path" does not use the Addressables.BuildPath or Application.streamingAssetsPath. Any content in those directories will be assigned to the generated asset packs.
3. In the “Content Packing & Loading” schema:
   - Set the “Build & Load Paths” to the default local paths (LocalBuildPath and LocalLoadPath). At runtime we will configure the load paths to use asset pack locations (see AddressablesInitSingleton.cs).
     - **Note**: Since the Google Play Console doesn't provide remote URLs for uploaded content, it is not possible to use remote paths or the Content Update workflow for content assigned to asset packs. Remote content will need to be hosted on a different CDN.
   - In Advanced Options > Asset Bundle Provider use the “Play Asset Delivery Provider”. This will download asset packs before loading bundles from them.

###### Build Addressables
Build Addressables using the custom “Play Asset Delivery” build script. In the Addressables Groups Window, do Build > New Build > Play Asset Delivery.
This script will:
1. Create the config files necessary for creating [custom asset packs](https://docs.unity3d.com/Manual/play-asset-delivery.html#custom-asset-packs)
   - Each asset pack will have a directory named “{asset pack name}.androidpack” in 'Assets/PlayAssetDelivery/Build/CustomAssetPackContent'.  
     - **Note**: All .bundle files created from a previous build will be deleted from 'Assets/PlayAssetDelivery/Build/CustomAssetPackContent'. 
   - Each .androidpack directory also contains a ‘build.gradle’ file. If this file is missing, Unity will assume that the asset pack uses "on-demand" delivery.
2. Generate files that store build and runtime data that are located in 'Assets/PlayAssetDelivery/Build':
   - Create a 'BuildProcessorData.json' file to store the build paths and .androidpack paths for bundles that should be assigned to custom asset packs. At build time this will be used by the PlayAssetDeliveryPreBuildProcessor to relocate bundles to their corresponding .androidpack paths.
   - Create a 'CustomAssetPacksData.json' file to store custom asset pack information to be used at runtime.

###### Configure Runtime Scripts
Prepare Addressables to load content from asset packs at runtime (see AddressablesInitSingleton.cs):
1. Make sure that the generated asset packs are downloaded.
2. Configure our custom InternalIdTransformFunc, which converts internal ids to their respective asset pack location.
3. Load all custom asset pack data from the "CustomAssetPacksData.json".

Once configured, you can load assets using the Addressables API (see LoadObject.cs).

###### Build the Android App Bundle
When ready to build the Android App Bundle, open File > Build Settings. Enable "Build App Bundle (Google Play)" and click “Build”.
**Note** If you want to upload the App Bundle to the Google Play Console, make sure that you are doing a release build.

This will build all of our custom asset packs along with the generated asset packs. The PlayAssetDeliveryPreBuildProcessor will automatically move bundles to their "{asset pack name}.androidpack” directories in 'Assets/PlayAssetDelivery/Build/CustomAssetPackContent', so that they will be assigned to their corresponding custom asset pack.
