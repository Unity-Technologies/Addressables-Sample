# Addressables-Sample
Demo project using Addressables package

This sample is broken up into projects based on high level functionality.  Each project has multiple scenes as needed to 

## Projects

#### *Basic AssetReference*
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
  * ...

#### *Scene Loading*
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
  
#### *Sprite Land*
A scene showing different ways to access sprites.
* Scenes/SampleScene
  * After hitting play, clicking on the screen with trigger each sprite swap (one swap per click)
  * The first sprite swap is a directly referenced sprite.
  * The second is pulling a sprite out of a sprite sheet.  NOTE: THIS WILL CRASH a standalone player.  We are currently investigating this.
  * Still to come is working with Sprite Atlas assets.  
  
  