# Unity Addressable Asset System

The Addressable Asset System provides an easy way to load assets by “address”. It handles asset management overhead by simplifying content pack creation and deployment.

The Addressable Asset System uses asynchronous loading to support loading from any location with any collection of dependencies. Whether you are using direct references, traditional asset bundles, or Resource folders, addressable assets provide a simpler way to make your game more dynamic.

## What is an asset?
An asset is content that you use to create your game or app. An asset can be a prefab, texture, material, audio clip, animation, etc.

## What is an address?
An address identifies the location in which something resides. For example, when you call a mobile phone, the phone number acts as an address. Whether the person is home, at work, in Paris or Pittsburgh, the phone number can connect you.

## What is an Addressable Asset?
Once an asset is marked "addressable", the addressable asset can be called from anywhere. Whether that addressable asset resides in the local player or on a content delivery network, the system will locate and return it. You can load a single addressable via its address or load many addressables using a customized group label that you define.

## Why do I care?

Traditionally, structuring game assets to efficiently load content has been difficult.

Using Addressable Assets shortens your iteration cycles, allowing you to devote more time to designing, coding, and testing your application. With Addressable Assets you identify the asset as addressable and load it.

## What problems does the Addressable Asset System solve?

* Iteration time: Referring to content by its address is super-efficient. With an address reference, the content just gets retrieved. Optimizations to the content no longer require changes to your code.
* Dependency management: The system not only returns content at the address, but also returns all dependencies of that content. The system informs you when the entire asset is ready, so all meshes, shaders, animations, and so forth are loaded before the content is returned.
* Memory management: The address not only loads assets, but also unloads them. References are counted automatically and a robust profiler helps you spot potential memory problems.
* Content packing: Because the system maps and understands complex dependency chains, it allows for efficient packing of bundles, even when assets are moved or renamed. Assets can be easily prepared for both local and remote deployment to support downloadable content (DLC) and reduced application size.

## What about my existing game?
The Addressable Asset System provides a migration path for upgrading, whether you use direct references, Resource folders, or Asset Bundles.