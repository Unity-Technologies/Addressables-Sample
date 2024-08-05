ODRandAddressables Sample

The AdrOdrExt contains the files that support using Bundles from ODR

Overview.

In Andressables Profiles set the variable 'BuildPathODR' to where you want the ODR Bundles written.

In the Groups Panel for each group you wish to be ODR
    1. Add the Apple ODR Schema
    2. In the Schema select the BuildPathODR variable
    3. Change the AssetBundleProvider to ODR AssetBundle Provider    

To Build Data
    Make sure you switch to IOS Target!
    In the Build dropdown select ODR Build Script
    
To Build App
    Make sure you switch to IOS Target!
    Build App as normal, the ODR Data should be set up correctly. 
    You can see it in the UnityData.xcassets file at the bottom of the project
    
 The Sample sets asset pack_a to _c as normal Bundles, and the rest, _d to _l as ODR Bundles
 
 
 
 
