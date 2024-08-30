# Addressables-Sample
Demo project using Addressables package

These samples are broken up into projects based on high level functionality.  These are intended as jumping off points for your own development.  These have not been tested, and are not guaranteed to work in your situation.  They are just examples, to make some concepts easier to understand, or easier to replicate in your own project.  Use at your own risk.

**Note**: Please report any bugs found through the regular bug submission process https://unity3d.com/unity/qa/bug-reporting. For any questions, create a new thread on the Unity Forums https://forum.unity.com/forums/addressables.156/. 

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
* Samples/Addressables/1.19.19/ComponentReference/ComponentReference - ComponentReference<TComponent>
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

#### *Basic/Space Shooter (Legacy)*
Referenced in past Unite video(s):
* [How to use Unity's Addressable Asset system for speed and performance - Unite LA](https://www.youtube.com/watch?v=U8-yh5nC1Mg) (2018)
* [Unite Berlin 2018 - New Addressable Asset system for speed and performance](https://www.youtube.com/watch?v=iauWgEXjkEY) (2018)

A very simple Unity tutorial that we converted to use addressables.  The main code file to look at would be Done_GameController.cs, but in general, this is just meant as a simple project to explore.

#### *Advanced/Addressables Variants (Legacy)*
Referenced in past Unite video(s):
* [Addressables for live content management - Unite Copenhagen](https://www.youtube.com/watch?v=THs7h-wXHBg) (2019)

An example project to show two use cases or workflows for creating "variants".  The new build pipeline (Scriptable Build Pipeline) upon which Addressables is built, does not support asset bundle variants.  This old mechanism was useful in many instances, so this sample is meant to show how to accomplish similar results for one of those instances.
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
* Samples/Addressables/1.20.0/Custom Analyze Rules/Editor/AddressHasC
  * This is a non-fixable rule (meaning it will not fix itself).
  * When run, it checks that all addresses have a capital C in them.  Any that do not are flagged as errors.
  * A rule like this would be useful if your studio enforced some sort of naming convention on addresses. (though it would probably be best if it could fix itself)
* Samples/Addressables/1.20.0/Custom Analyze Rules/Editor/CheckBundleDupeDependenciesMultiIsolatedGroups
  * This is a fixable rule. It is similar to the "Check Duplicate Bundle Dependencies" as fixing the rule will resolve duplicate bundle dependencies. However in this case duplicates will be moved to multiple isolation groups. Duplicates referenced by the same groups will be moved to the same isolation group. 
  * In the sample project there are 3 groups provided to illustrate this behavior: 
    * The Ground Materials Group contains a ScriptableObject that references the materials DirtMat and GrassMat.
    * The Water Materials Group contains a ScriptableObject that references the material WaterMat.
    * The All Materials Group contains a ScriptableObject that references all 3 materials DirtMat, GrassMat, and WaterMat. It shares the same bundle dependencies as the Ground and Water Materials Group.
  * Fixing the "Check Duplicate Bundle Dependencies Multi-Isolated Groups" rule will create 2 new isolation groups:
    * The first Duplicate Asset Isolation group contains the materials DirtMat and GrassMat.
    * The second Duplicate Asset Isolation groups contains the material WaterMat.
* Samples/Addressables/1.20.0/Custom Analyze Rules/Editor/PathAddressIsPath
  * This is a fixable rule.  Running fix on it will change addresses to comply with the rule.
  * When run, it first identifies all addresses that seem to be paths.  Of those, it makes sure that the address actually matches the path of the asset.
  * This would be useful if you primarily left the addresses of your assets as the path (which is the default when marking an asset addressable).  If the asset is moved within the project, then the address no longer maps to where it is. This rule could fix that.

#### *Advanced/Play Asset Delivery*
An example project that shows how to use [Unity's Play Asset Delivery API](https://docs.unity3d.com/Manual/play-asset-delivery.html) with Addressables. SampleScene (located in 'Assets/Scenes') contains 3 buttons that will load or unload an asset assigned to a specific delivery type.

**Note**: [Play Asset Delivery](https://docs.unity3d.com/Manual/play-asset-delivery.html) requires Unity 2019.4+. This project uses Unity 2020.3.15f2.

Use this project as a guide to make [custom asset packs](https://docs.unity3d.com/Manual/play-asset-delivery.html#custom-asset-packs). If you don't want to use custom asset packs, follow the instructions in [Play Asset Delivery](https://docs.unity3d.com/Manual/play-asset-delivery.html) to use the Play Asset Delivery API with the default Addressables configuration.

You can use the default Addressables configuration in this case because the AddressablesPlayerBuildProcessor moves all content located in Addressables.BuildPath to the streaming assets path. Unity assigns streaming assets to the [generated asset packs](https://docs.unity3d.com/Manual/play-asset-delivery.html#generated-asset-packs).

##### Basic Workflow
1. Assign an Addressable Group to an asset pack. You can assign multiple groups to the same asset pack. If you do this, you should be aware of the size restrictions that each delivery mode has. For more information, see [Android's Play Asset Delivery documentation](https://developer.android.com/guide/playcore/asset-delivery#download-size-limits).
   - By default, Addressables includes the content of any groups that use Addressables.BuildPath in the streaming asset path.
   - For additional [custom asset packs](https://docs.unity3d.com/Manual/play-asset-delivery.html#custom-asset-packs), you must create an “{asset pack name}.androidpack” directory in the Assets folder for each asset pack. Optionally add a .gradle file to this directory to specify the asset pack's delivery type. The default type is "on-demand" delivery. For more information, see the example in the BuildScriptPlayerAssetDelivery.cs file in this project.
2. To assign an AssetBundle to an asset pack, move the .bundle file to the “{asset pack name}.androidpack” directory (see PlayAssetDeliveryBuildProcessor.cs). We also maintain a system that keeps track of all our custom asset pack names which bundles are assigned to them (see BuildScriptPlayerAssetDelivery.cs and PlayAssetDeliveryInitialization.cs).
3. Build the Android App Bundle and upload it to the Google Play Console to generate the APK.
4. At runtime, download asset packs before loading bundles from them (see PlayAssetDeliveryAssetBundleProvider.cs).

##### Configure Build & Player Settings
Configure Unity to build Android App Bundles. For more information see ["Play Asset Delivery"](https://docs.unity3d.com/Manual/play-asset-delivery.html#using-play-asset-delivery).
1. Go to **File** > **Build Settings** and set the **Platform** property to Android. If **Export Project** is enabled, enable **Export for App Bundle**. Otherwise, enable **Build App Bundle (Google Play)**.
2. If you want any bundled content to use "install-time" delivery, enable **Split Application Binary** in **Edit** > **Project Settings** > **Player** > **Publishing Settings**.

##### Configure custom Addressables scripts
The sample project uses custom Addressables scripts that make it easier to build and load AssetBundles assigned to asset packs.
- The 'Assets/PlayAssetDelivery/Data' directory contains the following files:
  - A custom group template (Asset Pack Content.asset).
  - A custom data builder asset (BuildScriptPlayAssetDelivery.asset).
  - A custom initialization object (PlayAssetDeliveryInitializationSettings.asset) 
- The 'Assets/PlayAssetDelivery/Editor' directory contains the following files:
  - A custom data builder script (BuildScriptPlayAssetDelivery.cs).
  - A custom build processor (PlayAssetDeliveryBuildProcessor.cs)
  - A custom schema (PlayAssetDeliverySchema.cs)
- The 'Assets/PlayAssetDelivery/Runtime' directory contains the following file:
  - A custom AssetBundle Provider (PlayAssetDeliveryAssetBundleProvider.cs)
  - A custom initializable object (PlayAssetDeliveryInitialization.cs)

Addressables imports most of the scripts automatically, but you need to manually configure some assets in the AddressableAssetSettings:
1. In the **Build and Play Mode Scripts** list add the custom data builder asset (BuildScriptPlayAssetDelivery.asset).
2. In the **Asset Group Templates** list add the custom group template (Asset Pack Content.asset).
3. In the **Initialization Objects** list add the custom initialization object (PlayAssetDeliveryInitializationSettings.asset).

##### Assign Addressables Groups to asset packs
1. Go to the Addressables Groups window (**Window** > **Asset Management** > **Addressables Groups**) toolbar and select **Create > Group > Asset Pack Content** to create a new group whose content will be assigned to an asset pack. The group contains 2 schemas **Content Packing & Loading** and **Play Asset Delivery**.
2. Specify the assigned asset pack in **Play Asset Delivery** schema. Select **Manage Asset Packs** to modify custom asset packs.
   - Assign all content intended for "install-time" delivery to the "InstallTimeContent" asset pack. This is a "placeholder" asset pack that is representative of the [generated asset packs](https://docs.unity3d.com/Manual/play-asset-delivery.html#generated-asset-packs). No custom asset pack named "InstallTimeContent" is actually created.
   - In most cases the generated asset packs will use "install-time" delivery, but in large projects it may use "fast-follow" delivery instead. For more information see ["Generated Asset Packs"](https://docs.unity3d.com/Manual/play-asset-delivery.html#generated-asset-packs).
   - **Note**: To exclude the group from the asset pack either disable **Include In Asset Pack** or remove the **Play Asset Delivery** schema. Additionally make sure that its **Content Packing & Loading** > **Build Path** property does not use the Addressables.BuildPath or Application.streamingAssetsPath. Any content in those directories will be assigned to the generated asset packs.
3. In the **Content Packing & Loading** schema:
   - Set the **Build & Load Paths** to the default local paths (LocalBuildPath and LocalLoadPath). At runtime, configure the load paths to use asset pack locations. For an example of how to do this, see the PlayAssetDeliveryInitialization.cs file.
   - **Note**: Since the Google Play Console doesn't provide remote URLs for uploaded content, it is not possible to use remote paths or the Content Update workflow for content assigned to asset packs. Remote content will need to be hosted on a different CDN.
   - In **Advanced Options** > **Asset Bundle Provider** use the **Play Asset Delivery Provider**. This will download asset packs before loading bundles from them.

##### Build Addressables
Build Addressables using the custom "Play Asset Delivery" build script. In the Addressables Groups Window, do **Build** > **New Build** > **Play Asset Delivery**.
This script will:
1. Create the config files necessary for creating [custom asset packs](https://docs.unity3d.com/Manual/play-asset-delivery.html#custom-asset-packs)
   - Each asset pack will have a directory named “{asset pack name}.androidpack” in the 'Assets/PlayAssetDelivery/Build/CustomAssetPackContent' directory.
   - **Note**: All .bundle files created from a previous build will be deleted from the 'Assets/PlayAssetDelivery/Build/CustomAssetPackContent' directory.
   - Each .androidpack directory also contains a ‘build.gradle’ file. If this file is missing, Unity will assume that the asset pack uses "on-demand" delivery.
2. Generate files that store build and runtime data that are located in the 'Assets/PlayAssetDelivery/Build' directory:
   - Create a 'BuildProcessorData.json' file to store the build paths and .androidpack paths for bundles that should be assigned to custom asset packs. At build time this will be used by the PlayAssetDeliveryBuildProcessor to relocate bundles to their corresponding .androidpack directories.
   - Create a 'CustomAssetPacksData.json' file to store custom asset pack information to be used at runtime.

##### Create Runtime Scripts that configure custom Addressables properties
Prepare Addressables to load content from asset packs at runtime (see PlayAssetDeliveryInitialization.cs):
1. Make sure that the generated asset packs are downloaded.
2. Configure the custom InternalIdTransformFunc, which converts internal ids to their respective asset pack location.
3. Load all custom asset pack data from the "CustomAssetPacksData.json" file.

Once configured, you can load assets using the Addressables API (see LoadObject.cs).

**Note**: To load content from AssetBundles during Play Mode, go to the Addressables Groups window (**Window** > **Asset Management** > **Addressables Groups**) toolbar and select **Play Mode Script** > **Use Existing Build (requires built groups)**.

##### Build the Android App Bundle
When you have configured the build settings according to the [Configure Build & Player Settings](#Configure-Build-&-Player-Settings) instructions, go to **File** > **Build Settings** and select **Build** to build the Android App bundle.


1234567890

**Note**: You can't upload a development build to the Google Play Console. If you want to upload your App Bundle to the Google Play Console, ensure that you create a release build. For more information, see [Build Settings](https://docs.unity3d.com/Manual/BuildSettings.html).

The PlayAssetDeliveryBuildProcessor will automatically move bundles to their "{asset pack name}.androidpack” directories in 'Assets/PlayAssetDelivery/Build/CustomAssetPackContent', so that they will be assigned to their corresponding custom asset pack. Then Unity will build all of the custom asset packs along with the generated asset packs.

#### *Advanced/On-Demand Resources*
An example project that shows how to use [Unity's On-Demand Resources API](https://docs.unity3d.com/Manual/ios-ondemand-resources.html) with Addressables. LoaderAdr (located in 'Assets/Scenes') contains a script that will load 12 bundles sequentially. Half are locally built and half are on-demand.

The Sample sets asset pack_a to _c as normal Bundles, and the rest, _d to _l as ODR Bundles,

The AdrOdrExt folder contains the files that support using Bundles from ODR.

##### Basic Workflow
In Andressables Profiles set the variable 'BuildPathODR' to where you want the ODR Bundles written.

In the Groups Panel for each group you wish to be ODR

1. Add the Apple ODR Schema
2. In the Schema select the BuildPathODR variable
3. Change the AssetBundleProvider to ODR AssetBundle Provider

##### Build Addressables
**Make sure you switch to IOS Target!**

In the Addressables Group window switch choose ODR Build Script from the Build menu.

##### Build the Player
**Make sure you switch to IOS Target!**

Build the player from the build menu asnormal, the ODR Data should be set up correctly.

You can see it in the UnityData.xcassets file at the bottom of the project.
 
 
 
 
