# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.8.3] - 2019-05-08
 - *BREAKING CODE CHANGES* 
   - Chagned all asynchronous methods to include the word Async in method name.  This fits better with Unity's history and convention.  They should auto upgrade without actually breaking your game. 
   - Moved AsyncOperationHandle inside namespace UnityEngine.ResourceManagement
 - Addressable Analyze changes:
   - Analyze has been moved into it's own window.
   - CheckSceneDupeDependencies Analyze rule has been added.
   - CheckDupeDependencies has been renamed into CheckBundleDupeDependencies.
   - Analyze Rule operations for individuals or specific sets of Analyze Rules has been added via AnalyzeRule selections.

## [0.7.4] - 2019-04-19
 - Removed support for .NET 3.x as it is deprecated for Unity in general. 
 - Replaced IAsyncOperation with AsyncOperationHandle.
   - Once the asset is no longer needed, the user can call Addressables.Release, passing in either the handle, or the result the handle provided.
 - Exposed AsyncOperationBase for creating custom operations
   - These operations must be started by ResourceManager.StartOperation
 - Replaced IDataBuilderContext and it's inherited classes with simpler AddressablesDataBuilderInput.  This class is fed into all IDataBuilder.BuildData calls. 
 - Fixed Nintendo Switch and PlayStation4 support.
 - Simplified the IResourceProvider interface.
 - Refactored build script interface.  Made BuildScriptBase and the provided concrete versions easier to inherit from. 
 - Removed DelayedActionManager.
 - Removed ISceneProvider. Users can implement custom scene loading using a custom AsyncOperationBase.
 - Removed optional LRU caching of Assets and Bundles.
 - Addressables Profiler now tracks all active async operations
 - AssetBundles targetting StreamingAssets (by using the profile variable [UnityEngine.AddressableAssets.Addressables.BuildPath] now build to the Library instead of StreamingAssets.  During the player build, these files are copied into StreamingAssets, then after the build, the copies are deleted. They are also built into platform specific folders (so building for a second platform will not overwrite data from a first build).  We recommend deleting anything in Assets/StreamingAssets/aa.
 - The addressables_content_state.bin is built into a platform specific folder within Assets/AddressableAssetsData/.  We recommend deleting the addressables_content_state.bin in Assets/AddressableAssetsData to avoid future confusion.  
 - ScriptableBuildPipeline now purges stale data from its cache in the background after each build. 
 - Disabled Addressables automatic initialization.  It will now initialize itself upon the first call into it (such as Load or Instantiate).  To Initialize on startup instead of first use, call Addressables.Initialize().  
 - Optimized performance around instantiation and general garbage generation. 
 - Added per-group bundle compression settings. 
 - Fixes to AssetReference drawers. 
 - Improved the group template system for creating better defined asset groups. 
 - Fixed bug in bundle caching that caused GetDownloadSize to report incorrectly
 - Cleaned up Load/Release calls to make sure all releases could take either the handle returned by Load, or the handle.Result.
 - Added editor-only analytics (nothing added in runtime).  If you have Analytics disabled in your project nothing will be reported. Currently only run when you build addressables, it includes data such as Addressables version and Build Script name.
 - Fixed null ref issue when cleaning all the data builders
 - KNOWN ISSUE: there is still an occasional issue with code stripping on iOS.  If you run into iOS issues, try turning stripping off for now.  

## [0.6.8-preview] - 2019-03-25
- fixed Build For Content Update to no longer delete everything it built.

## [0.6.7-preview] - 2019-03-07
 - Fix for iOS and Android. Symptom was NullReferenceException dring startup resulting in nothing working.  Fix requires re-running Build Player Content
 
## [0.6.6-preview] - 2019-03-05
 - *BREAKING CODE CHANGES* 
   - to ease code navigation, we have added several layers of namespace to the code.  
   - All Instantiate API calls (Adddressables and AssetReference) have been changed to only work with GameObjects.
   - any hardcoded profile path to com.unity.addressables (specifically LocalLoadPath, RemoteLoadPath, etc) should use UnityEngine.AddressableAssets.Addressables.RuntimePath instead.  
       For build paths, replace Assets/StreamingAssets/com.unity.addressables/[BuildTarget] with [UnityEngine.AddressableAssets.Addressables.BuildPath]/[BuildTarget]
	   For load paths,  replace Assets/StreamingAssets/com.unity.addressables/[BuildTarget] with {UnityEngine.AddressableAssets.Addressables.RuntimePath}/[BuildTarget]
   - We have removed attribute AssetReferenceTypeRestriction as it is cleaner to enforce type via generics
   - Attribute AssetReferenceLabelRestriction is renamed to AssetReferenceUILabelRestriction and must be surrounded by #if UNITY_EDITOR in your game code, to enforce it's editor-only capability
   - Modifications to IResourceProvider API.
   - Removed PreloadDependencies API.  Instead use DownloadDependencies
 - Content Update calculation has changed, this will invalide previously generated addressables_content_state.bin files.
   - Some types for content update were made private as a result of the above change.
 - Minimum Unity version is now 2018.3 to address a build-time bug with progressive lightmapper.
 - Moved all of the Resource Manager package to be contained within Addressables (no longer a stand alone package).  No code change implications. 
 - Change to content catalog building: 
   - Previous model built one catalog per group, wherever that group built it's data.
   - New model builds one catalog locally, and optionally one "remote".  Remote location is set on the top level AddressableAssetSettings object.
   - Loading will now always check if remote has changes (if remote exists), and use local otherwise (or cached version of remote).
 - LoadScene API now takes the LoadSceneParameters that were added to the engine in 2018.2
 - Exposed AddressablesBuildDataBuilderContext.BuildScriptContextConstants for use in build scripts.
 - Refactored AddressablesBuildDataBuilderContext.GetValue to take default parameter.
 - Fixed Scene asset path to be consistent between different play modes in the catalog data.
 - Exposed the various IDataBuilder implementations as public classes.
 - Exposed asset and bundle provider types for BundledAssetGroupSchema.
 - Fixed several bugs when loading catalogs from other projects.
 - Added provider suffix to Initialization operation and Adddressables.LoadCatalogsFromRuntimeData API to better support overriding providers.
 - Exposed CachedProvider options in BundledAssetGroupSchema.  Each unique set of parameters will generate a separate provider.  There is also an option to force a group to have its own providers.
 - Added IEnumerable<object> Keys property to IResourceLocator interface.
 - Exposed InitializationOperation as public API.
 - Added BuildTarget to ResourceManagerRuntimeData.  This is used to check if the generated player content was built with the same build target as the player or the editor when entering play mode.
 - Removed warnings generated from not finding the cached catalog hash files, which is not an error.
 - Fixed bug where scenes were not unloading.
 - Fixed GUI exception thrown in group inspector.
 - Fixed error case where an asset (usually a bundle) was loaded multiple times as different types (object and AssetBundle).
 - Fixed divide by zero bug when computing load percent of simulated asset bundles.
 - AddressableAssetBuildResult.CreateResult now takes the settingsPath as a parameter to pass this to the result.
 - Fix AssetReference GUI when the AssetReference is inside an array of classes, part of a SerializedObject, or private.
 - Fix AssetReferenceSprite to properly support sprites (as opposed to Texture2D's).
 - Fixed bug involving scenes being repeatedly added to the build scenes list.
 - Removed deprecated and obsolete code.  If you are upgrading from a very old version of Addressables, please update to 0.5.3-preview first.
 - Removed the default MergeMode on LoadAssets calls to enforce explicit behavior. 
 - Added IAsyncOperation<long> GetDownloadSize(object key) API to compute remaining data needed to load an asset
 - Fixed assets being stuck in a read-only state in UI
 - Unified asset moving API to clean up public interface
 - Added PlayerVersion override to AddressableAssetSettings
 - Ensure UI cannot show invalide assets (such as .cs files)
 - Renamed Adddressables.LoadAddtionalCatalogs to Addressables.LoadContentCatalog and now it takes the path of the catalog instead of the settings file
 - Moved provider information from ResourceManagerRuntimeDate into ContentCatalogData
 - Updating ResourceManager to be a non-static class
 - Fixed bugs surrounding assets moving in or out of Resources (outside Addressables UI) 
 - Fixed the AssetReference dropdown to properly filter valid assets (no Resources and honoring type or label limitations). 
 - Fixed AssetReferences to handle assets inside folders marked as Addressable.
 - Added attribute AssetReferenceUIRestriction to support user-created AssetReference restrictions (they are only enforced in UI, for dropdown and drag&drop)
 - Changed addressables_content_state.bin to only build to the folder containing the AddressableAssetSettings object (Assets/AddressableAssetsData/ in most cases)
 - Fixed issue where the wrong scene would sometimes be open post-build.
 
## [0.5.3-preview] - 2018-12-19
 - fixed upgrade bug from 0.4.x or lower to 0.5.x or higher. During upgrade, the "Packed Mode" option was removed from play mode.  Now it's back and upgrades are safe from 0.4.x or from 0.5.x to 0.5.3
 
## [0.5.2-preview] - 2018-12-14
 - *IMPORTANT CHANGE TO BUILDING* 
   - We have disabled automatic asset bundle building.  That used to happen when you built the player, or entered play mode in "packed mode".  This is no longer the case.  You must now select "Build->Build Player Content" from the Addressables window, or call AddressableAssetSettings.BuildPlayerContent().  We did this because we determined that automatic building did not scale well at all for large projects.  
 - fixed regression loading local bundles
 - Added Adddressables.DownloadDependencies() interface
 - fixes for Nintendo Switch support
 - Fixed issues around referencing Addressables during an Awake() call
 - Code refactor and naming convention fixes
 - Cleaned up missing docs
 - Content update now handles not having and groups marked as Static Content
 - Fixed errors when browing for the addressables_content_state.bin and cancelling 
 - Moved addressables_content_state.bin to be generated into the addressables settings folder
 - Changed some exceptions when releasing null bundles to warnings to handle the case of releasing a failed download operation
 - Separated hash and crc options to allow them to be used independently in asset bundle loads.
 - Use CRC in AssetBundle.LoadFromFileAsync calls if specified
 - Always include AssetBundleRequestOptions for asset bundle locations
 
## [0.4.8-preview] - 2018-10-22
 - Added all referenced types in asset bundles to link.xml to prevent them from being stripped in IL2CPP builds

## [0.4.7-preview] - 2018-10-20
 - updated Scriptable Build Pipeline version in dependencies

## [0.4.6-preview] - 2018-10-16
 - MINIMUM RECOMMENDED VERSION - 2018.2.11+ 
   - We have re-enabled the addressables checkbox. Versions of 2018.2 older than the .11 release will work unless you attempt to view the Animation Import Settings inspector.  If you do have animations you need to inspect, use .11+. If you do not, use any official release version of 2018.2.
 - refactored the way IResourceProviders are initialized in the player - serialized data is constructed at runtime to control how the providers are configured
 - added readonly custom inspector for AddressableAssetEntryCollection
 - AssetReference now stores the loaded asset which can be accessed via the Asset property after LoadAsset completes.  ReleaseAsset has been modified to not need the asset passed in (the old version is marked obsolete]
 - fixed profiler details view not updating when a mouse drag is completed
 - fixed null-ref when moving Resources to Addressables when there are no Resources
 - blocked moving EditorSceneList within GUI
 - fixed cap on address name length
 - fixed workflows of marking Resources as addressable and moving an addressable into Resources.
 - fixed issue where AssetReferenceDrawer did not mark scene as dirty when changed.
 - added Hosting Services feature; provides extensible framework and implementation for serving packed content to player builds from the Editor
 - replaced addressables buildscript with an interface based system.  IDataBuilder class is now used to define builders of specific types of data.  The Addressables settings object
   contains a collection of data builders and uses these to create player and play mode data.  Users can implemented custom data builders to control the build process.
 - replaced AssetGroupProcessors with a collection of AssetGroupSchema objects.  The difference is that the schema objects only contain data and groups can have multiple schemas.  The 
   logic for processing groups now resides in the build script and uses the schemas as data sources and filters for how to build.
 - Added Initialization objects that can be created during the build to run during addressables initialization
 - Implemented Caching API initialization with initialization objects
 - Changed some API and tests to work with 2019.x
 - fixed how AssetReference's draw when within lists, arrays, or contained classes
 - Fixed the workflow of scenes moving in and out of the Editor Build Settings Scene list. 
 - Removed "Preview" and added "Analyze". 
   - The new system runs any rules it knows about. 
   - Currently this is one rule that is manually set up in code. Future work will have additional rules, and expose the ability to create/add user- or project-specific rules
   - This process can be slow, as it runs most of a build to get accurate data.
   - Within the Analyze window there is a "fix" button that allows each rule to fix any issues if the rule knows how. 
   - The current rule is a "check duplicate asset" rule. This looks for assets that are pulled into multiple asset bundles due to dependency calculations. The way it fixes things is to move all of those into a newly created group.
 - Added option to toggle logging of all exceptions within the Resource Manager
 - Refactored initialization of the addressable asset settings to prevent it getting into a bad state.

## [0.3.5-preview] - 2018-09-05
 - implemented content update workflow.  Added a dropdown to the "Build" button on main window's toolbar.   
    - "Build/Prepare for Content Update" will detect assets in locked bundles (bundles flagged as static, by default all local bundles).
    - "Build/Build for Content Update" will build assets with a catalog that is compatible with a previously released player.
	- "Build/Build Packed Data" will build in the same way entering play mode in PackedMode would.
	- implemented Clean Build. "Build/Clean/*" will clear out build caches. 
 - cleaned up streaming assets folder better after build
 - moved asset group data into separate assets in order to better support version control
 - fixed bug when canceling export of entries to an AssetEntryCollection
 - fixed several bugs related to caching packed bundles in play mode
 - added option to build settings to control whether streaming assets is cleared after each build
 - enabled CreateBuiltInShadersBundle task in build and preview
 - fixed bug in AA initialization that was cuasing tests to fail when AA is not being used.
 - fixed bug where toggling "send profiler events" would have no effect in some situations
 - default the first 2 converted groups to have StaticContent set to true
 - UI Redesign
  - Moved most data settings onto actual assets.  AddressableAssetSettings and AddressableAssetGroup assets.
    - AddressableAssetSettings asset has "Send Profile Events", list of groups, labels, and profiles
	- AddressableAssetGroup assets have all data associated with that group (such as BuildPath)
  - Made "preview" be a sub-section within the Addressables window.
  - The "Default" group can now be set with a right-click in the Addressables window.
  - Set play mode from "Mode" dropdown on main window's toolbar. 
  - Moved "Hierarchical Search" option onto magnifying glass of search bar.  Removed now empty settings cog button.
 - fixed issue when packing groups into seperate bundles generated duplicate asset bundle names, leading to an error being thrown during build
 - added support for disabling the automatic initialization of the addressables system at runtime via a script define:  ADDRESSABLES_DISABLE_AUTO_INITIALIZATION
 - added API to create AssetReference from AddressableAssetSettings object in order to create an entry if it does not exist.
 - moving resource profiler from the ResourceManager package to the Addressables package
 - fixed bug where UnloadScene operation never entered Done state or called callback.
 - fixed loading of additonal catalogs. The API has changed to Addressables.LoadCatalogsFromRuntimeData.
 - fixed bug in InitializationOperation where content catalogs were not found.
 - changed content update workflow to browse for cachedata.bin file instead of folder
 - fixed exception thrown when creating a group and using .NET 4.x
 - fixed bugs surrounding a project without addressables data.
  - AssetLabelReference inspector rendering
  - AssetReference drag and drop
 - fixed profiler details view not updating when a mouse drag is completed
 - fixes surrounding the stability of interacting with the "default" group.
 - Added docs for the Content Update flow.
 - Adjusted UI slightly so single-clicking groups shows their inspector.
 - removed not-helpful "Build/Build Packed Data" item from menu.  
 - fixed bug where you could no longer create groups, and group assets were not named correctly
 
## [0.2.2-preview] - 2018-08-08
 - disabled asset inspector gui for addressables checkbox due to editor bug
 
## [0.2.1-preview] - 2018-07-26
 - smoothed transition from 0.1.x data to 0.2.x data
 - added checks for adding duplicate scenes into the EditorBuildSettings.scenes list
 - fixed exception when deleting group via delete key, added confirmation to all deletions

## [0.2.0-preview] - 2018-07-23
 - Fixed bundles being built with default compression instead of compression from settings
 - Fixed bug in tracking loaded assets resulting in not being able to release them properly
 - Added Key property to IAsyncOperation to allow for retrieval of key that requested the operation
 - Added AssetLabelReference to provide inspector UI for selecting the string name of a label
 - Fixed dragging from Resources to a group.
 - Added ability to re-initialize Addressables with multiple runtime data paths.  This is to support split projects.
 - Clean up StreamingAssets folder after build/play mode
 
## [0.1.2-preview] - 2018-06-11
 - fixed Application.streamingAssetsPath being stripped in IL2CPP platforms

## [0.1.1-preview] - 2018-06-07
 - MIN VERSION NOW 2018.2.0b6
 - updated dependency

## [0.1.0-preview] - 2018-06-05
 - MIN VERSION NOW 2018.2.0b6
 - added better checks for detecting modified assets in order to invalidate cache
 - fixed preview window showing scenes in wrong bundle
 - exclude current processor type from conversion context menu
 - fixed exception when right clicking asset groups
 - added support for adding extra data to resource locations
 - made Addressables.ReleaseInstance destroy even non-addressable assets.
 - append hash to all bundle names
 - pass crc & hash to bundle provider
 - clear catalog cache whenever packed mode content is rebuilt
 
## [0.0.27-preview] - 2018-05-31
 - fixed ResourceManager initialization to work as the stand-alone player 
 
## [0.0.26-preview] - 2018-05-24
 - re-added Instantiate(AssetReference) for the sake of backwards compatability.
 
## [0.0.25-preview] - 2018-05-23
 - workaround for engine bug surrounding shader build.  Fix to engine is on it's way in.
 
## [0.0.24-preview] - 2018-05-21
 - minor bug fix
 
## [0.0.23-preview] - 2018-05-21
 - new format for content catalogs
 - detects changes in project and invalidates cached runtime data and catalogs
 - data is not copied into StreamingAssets folder when running fast or virtual mode
 - added external AssetEntry collections for use by packages
 - modifying large number of asset entries on the UI is no longer unresponsive
 - added an option to search the asset list in a hierarchical fashion. Helps track down which group an asset is in.
 - many small bug fixes.
 
## [0.0.22-preview] - 2018-05-03
 - dependency update.
 
## [0.0.21-preview] - 2018-05-03
 - fixed build-time object deletion bug.
 
## [0.0.20-preview] - 2018-05-02
 - Added support for extracting Built-In Shaders to a common bundle
 - Added build task for generating extra data for sprite loading edge case
 - fix build related bugs introduced in 0.0.19.

## [0.0.19-preview] - 2018-05-01
 - Complete UI rework.
	- Moved all functionality to one tab
	- Preview is a toggle to view in-line.
	- Profiles are edied from second window (this part is somewhat placeholder pending a better setup)
 - Dependency updates
 - Minor cleanup to build scripts

## [0.0.18-preview] - 2018-04-13
 - minor bug fixes
 - exposed memory cache parameters to build settings, changed defaults to use LRU and timed releases to make preloading dependencies more effective
 
## [0.0.17-preview] - 2018-04-13
 - added tests
 - fixed bugs
 - major API rewrite
	- all API that deals with addresses or keys have been moved to Addressables
	- LoadDependencies APIs moved to Addressables
	- Async suffix removed from all Load APIs
 
## [0.0.16-preview] - 2018-04-04
- added BuildResult and callback for BuildScript
- added validation of instance to scene and scene to instance maps to help debug instances that change scenes and have not been updated
- added ResourceManager.RecordInstanceSceneChange() method to allow RM to track when an instance is moved to another scene
- moved variable expansion of location data to startup 

## [0.0.15-preview] - 2018-03-28
- fixed scene unloading
- release all instances when a scene unloads that contains unreleased instances
- fixed overflow error in virtual mode load speeds

## [0.0.14-preview] - 2018-03-20
- Updated dependencies


## [0.0.12-preview] - 2018-03-20
- Minor UI updates
- doc updates
- fixed bug involving caching of "all assets"
- improved error checking & logging
- minor bug fixes.

## [0.0.8-preview] - 2018-02-08
- Initial submission for package distribution


