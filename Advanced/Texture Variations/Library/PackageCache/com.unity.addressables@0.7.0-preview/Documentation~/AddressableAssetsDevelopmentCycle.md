# Addressable Assets development

One of the key benefits of Addressables Assets is the decoupling of how you arrange your content, how you build your content, and how you load your content. Traditionally these facets of development are heavily tied together. If you arrange your content into Resources directories, the content is built into the base player, and you must load the content by calling the [Resources.Load](https://docs.unity3d.com/ScriptReference/Resources.Load.html) method and supplying the path to the resource.

To access content stored elsewhere, you used direct references or Asset bundles. If you used Asset bundles, you again loaded by path, tying your loading and your arranging together. If your Asset bundles were remote, or have dependencies in other bundles, you had to write a code to manage downloading, loading, and unloading of all of your bundles.

Giving an Asset an address, allows you to load it by that address, no matter where it is in your Project or how it was built.  You can change an Asset’s path or filename without problem.  You can also move the Asset from Resources, or from a local build destination, to some other build location, including remote ones, without your loading code ever changing.

## Asset Group Schemas
Schemas define a set of data and can be attached to asset groups vie their inspector.  The set of schemas attached to a group define how its contents are processed during a build.  For example, when building in packed mode, groups that have the BundledAssetGroupSchema attached to them are used as the sources for the bundles.  Sets of schemas can be combined into schema templates and these will be used to define new groups.  The schema templates can be added to the main Addressable Assets settings asset through the inspector.

## Build Scripts
Build scripts are represented as ScriptableObject assets in the project that implement the IDataBuilder interface.  Users can create their own build scripts and add them to the Addressable Asset settings object through its inspector.  Currently 3 scripts have been implemented to support a the full player build and 3 play modes for iterating in the editor.

## Play Mode Iteration

Addressable Assets has three build scripts that create play mode data to help you accelerate your app development.

Fast mode allows you to run the game quickly as you work through the flow of your game. Fast mode loads Assets directly through the Asset Database for quick iteration with no analysis or Asset bundle creation.

Virtual mode analyzes content for layout and dependencies without creating Asset bundles. Assets load from the Asset Database though the ResourceManager as if they were loaded through bundles. To see when bundles load/unload during game play, view the Asset usage in the Addressable Profiler window. To open the Addressable Profiler window, navigate to **Window** > **Asset Management** > **Addressable Profiler**.

Virtual mode helps you simulate load strategies and tweak your content groups to find that right balance for a production release.

Packed mode uses asset bundles that have already been built. This mode will most closely match a deployed player build but it requires the data to be built as a separate step.  If assets are not being modified, this mode will be the fastest since it does not process any data when entering play mode.  To build the content to be used by this mode, you must either select _Build->Build Player Content_ in the __Addressables__ window or use the `AddressableAssetSettings.BuildPlayerContent()` API.

Each mode has its own time and place during development and deployment.

The following table shows segment of the development cycle in which a particular mode is useful.

| | Design | Develop | Build | Test / Play | Publish |
|:---|:---|:---|:---|:---|:---|
| Fast| x | x |   | x In Editor only |   |
|
Virtual| x | x | x Asset Bundle Layout | x In Editor only |  |
| Packed|   |   | x Asset Bundles  | x | x |

## Analisys and Debugging

By default, Addressables logging will only show warnings and errors.  You can enable versbose logging by adding the ADDRESSABLES_LOG_ALL compiler flag to the player settings.  Exceptions can also be disabled by unchecking the "Log Runtime Exceptions" option in the AddressableAssetSettings object inspector.  The ResourceManager.ExceptionHandler property can be set with your own exception handler if desired, but this should be done after the Addressables runtime has finished initialization.

## Initialization objects

You can attach objects to the Addressable Assets settings and pass them to the initialization process at run time. The `CacheInitializationSettings` object is used to control the Unity's Caching API at runtime. To create your own initialization object, you can create a `ScriptableObject` that implements the `IObjectInitializationDataProvider` interface. It is the editor component of the system and is responsible for creating the `ObjectInitializationData` that is serialized with the run time data.

## Content update workflow

The recommended approach to content updates is to structure your game data into two categories: static content that you expect never to update and dynamic content that you expect to update. In this content structure, static content ships with the Player (or download soon after install) and resides in a single or a few large bundles. Dynamic content resides online and should be in smaller bundles to minimize the amount of data needed for each update. One of the goals of the Addressables system is to make this structure easy to work with and modify without having to change scripts. Sometimes you find yourself in a situation that requires changes to the "never update" content but you do not want to publish a full player build. 

### How it works

Addressables uses what we refer to as a "content catalog" to map an address to a specific "where and how" to load.  In order to provide your app with the ability to modify that mapping, your original app must be aware of an online copy of this catalog.  To set that up, enable *Build Remote Catalog* on the main AddressableAssetSettings object.  With that enabled, a copy of the catalog will be built-to and loaded-from the specified paths.  This load path cannot change once your app has shipped.  The content update process creates a new version of the catalog (with the same file name) to overwrite the file at the previously specified load path.

When you build a Player, a unique Player content version string is generated. This version is used to identify which content catalog each player should load.  Thus a given server can contain catalogs of multiple versions of your app without conflict.  The version string, along with hash information for each asset that is in a group marked as `StaticContent`, is stored in the *addressables_content_state.bin* file. By default, this file is stored in the same folder as your AddressableAssetSettings.asset file.  

The *addressables_content_state.bin* file contains hash and dependency information for every `StaticContent` asset group in the Addressables system. All groups buliding to the streaming assets folder should be marked as `StaticContent`, though remote groups that are large may also benefit from this designation.  During the **Prepare for Content Update** step, this hash information is used to determine if any `StaticContent` groups contain changed asses, and thus need those assets moved elsewhere.

### Prepare for Content Update
If you have modified assets in any `StaticContent` groups, you need to run **Prepare for Content Update**.  This will take any modified asset out of the static groups, and move them to a new group.  To generate the new asset groups:

1. In the Editor, on the menu bar, click **Window**.
1. Click **Asset Management**, then select **Addressable Assets**.
1. In the Addressable Assets window, on the menu bar, click **Build**, then select **Prepare for Content Update**.
1. In the **Build Data File** dialog, select the *addressables_content_state.bin* file which is probably in *Assets/AddressableAssetsData*.

This data is used to determine which assets or dependencies have been modified since the player was built. These assets are moved to a new group in preparation of the content update build.  Note that this step will do nothing if all your changes are confined to the non-Static groups.  

### Build for Content Update

To build for a content update:

1. In the Editor, on the menu bar, click Window.
2. Click Asset Management, then select Addressable Assets.
3. In the **Addressable Assets** window, on the menu bar, click **Build**, then select **Build for Content Update**.
4. In the **Build Data File** dialog, select the build folder of an existing Player build. The build folder must contain an *addressables_content_state.bin* file. 

The build generates a content catalog, a hash file, and the asset bundles.

The generated content catalog has the same name as the catalog in the selected Player build and is overwritten as is the hash file. The hash file is loaded by the Player to determine if a new catalog is available. Assets that have not been modified are loaded from existing bundles that were shipped with the Player or already downloaded.

The Addressable Assets build system uses the content version string and location information from *addressables_content_state.bin* file to create the asset bundles. 
Asset bundles that do not contain updated content are written using the same file names as those in the build selected for the update. If an asset bundle contains updated content, a new asset bundle is generated that contains the updated content and is given a new file name so that it can coexist with the original. Only asset bundles with new file names must be copied to the location that hosts your content.  

Asset bundles for `StaticContent` groups are also built, but they do not need to be uploaded to the content hosting location as they are not referenced by any Addressable asset entries.

### Examples
Let's say I've built my player with awareness the following groups:
```
Local_Static
- AssetA
- AssetB
- AssetC
-----------------------
Remote_Static
- AssetL
- AssetM
- AssetN
-----------------------
Remote_NonStatic
- AssetX
- AssetY
- AssetZ
```
Now, I've shipped this.  So there are players out in the world that have Local_Static on their devices, and potentially have either or both of the Remote bundles cached locally.  Next I modified one asset from each group (A, L, X) and run "Prepare For Content Update".  
**Before running Prepare** we recommend branching your version control system.  Prepare rearranges your groups in a way suited for updating content.  It is recommended that you branch so that next time you ship a new player, you can return to your preferred content arrangement.
The results _in my local Addressable Settings_ are:
```
Local_Static
- AssetB
- AssetC
-----------------------
Remote_Static
- AssetM
- AssetN
-----------------------
Remote_NonStatic
- AssetX
- AssetY
- AssetZ
-----------------------
content_update_group (non-static)
- AssetA
- AssetL
```
Note that Prepare actually edits the groups that are Static (can't be edited).  This may seem counter intuitive.  The key, is that I build the above layout, but discard the build results for any _Static groups.  As such, I end up with the following from my player's perspective:
```
Local_Static   <-- already on device, as we can't change that
- AssetA       <-- this version of AssetA is no longer referenced. It's stuck on their device as dead data.
- AssetB
- AssetC
-----------------------
Remote_Static  <-- This bundle is unchanged.  If it is not already cached on device,
                   it will be downloaded when M or N is requested
- AssetL       <-- this version of AssetL is no longer referenced. It's stuck in this bundle as dead data.
- AssetM
- AssetN
-----------------------
Remote_NonStatic (old) <-- this could be deleted from the server, but if not will never be re-downloaded.  
                            If cached, it will leave the cache eventually (in-progress feature to 
                            immediately remove automatically)
- AssetX     <-- old version
- AssetY
- AssetZ
-----------------------
Remote_NonStatic (new) <-- new version distinguished on server by hash.   
- AssetX     <-- new version
- AssetY
- AssetZ
-----------------------
content_update_group 
- AssetA
- AssetL
```
Here are the implications of the above:
1. Local: Any changed local assets will remain unused on the user's device forever.  
2. Remote_NonStatic: If the user already cached a non-static bundle, they will require re-download of even the unchanged assets (AssetY and AssetZ). If they have not cached the bundles, then this is the ideal scenario (only downloading new_Remote_NonStatic)
3. Remote_Static: If the user has already cached the static remote bundle, then they only need to download the updated asset (AssetL, via content_update_group).  This is the ideal in this case.  If the user has not cached the group, then they will download both the new AssetL via content_update_group and the now-dead AssetL via un-touched Remote_Static.  Regardless of initial cache state, at some point the user will have the dead AssetL on their device, cached indefinitely despite not being accessed. 

Which setup is better for your remote content depends on your specific scenario.

## Analyzing your data
To analyze your data configuration for potential problems, open the Addressables Window, and click the **Analyze** button on the top bar of that window.  This will open a sub-pane within the Addressables window.  From there you can click "Run Tests" to execute the analyze rules in the project.  After running a test, if there are any potential problems, you can manually alter your groups and rerun, or click "Fix All" to have the system automatically do it. 

### Check Duplicate Bundle Dependencies
The only rule currently present checks for potentially duplicated assets.  It does so by scanning all groups with BundledAssetGroupSchemas, and spies on the planned asset bundle layout.  This requires essentially triggering a full build, so this check is time consuming and performance intensive.  

Duplicated assets are caused by assets in different bundles sharing dependencies.  An example would be marking two prefabs that share a material as addressable in different groups.  That material (and any of its dependencies) will be pulled into the bundles with each prefab.  To prevent this, the material has to be marked as addressable, either with one of the prefabs, or in its own space.  Doing so will put the material and its dependencies in a separate bundle.  

If this check finds any issues, and the "Fix All" button is pressed, a new group will be created, and all dependent assets will be moved into that group.

There is one scenario in which this removal of duplicates will be incorrect.  If you have an asset containing multiple objects, it is possible for different bundles to only be pulling in portions of the asset (some objects), and not actually duplicate.  An example would be an FBX with many meshes.  If one mesh is in BundleA and another is in BundleB, this check will think that the FBX is shared, and will pull it out into its own bundle.  In this rare case, that was actually harmful as neither bundle had the full FBX asset.

### Future rule structure
Right now, Analyze only provides one rule.  In the future, the system will come with additional rules, and there will be the ability to write custom rules and integrate them into this analyze workflow. 

