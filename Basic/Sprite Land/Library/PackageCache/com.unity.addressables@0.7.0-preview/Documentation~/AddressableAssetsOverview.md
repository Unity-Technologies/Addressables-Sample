# Addressable Assets Overview

Addressable Assets consists of three packages:

* Addressable Assets package (primary package)
* Resource Manager package (dependency)
* Scriptable Build Pipeline package (dependency)

When you install the Addressable Assets package, the Resource Manager and Scriptable Build Pipeline packages are installed at the same time.

## Concepts

* __Address__ - Identifies an Asset for easy run-time retrieval.
* __AddressableAssetData directory__ - Stores your addressable Asset metadata in your Project’s Assets directory.
* __Asset Group__ - Denotes a set of addressable Assets available for build-time processing.
* __Asset Group Schema__ - Defines a set of data that can be assigned to a group and used during the build.
* __AssetReference__ - An object that operates like a direct reference, but with deferred initialization (for example, for lazy loading). The `AssetReference` stores the GUID as an addressable that you can load on-demand.
* __Asynchronous Loading__ - allows the location of the Asset and dependencies (for example,  local, remote and generated) to change throughout the course of your development without changing the game code. Async Loading is foundational to the Addressable Asset System.
* __Build Script__ - runs Asset Group Processors to package Assets and provides the mapping between Addresses and Resource Locations for the Resource Manager.
* __Label__ - provides an additional addressable Asset identifier for run-time loading of similar items. For example:

    `PreloadDependencies("spaceHazards");`