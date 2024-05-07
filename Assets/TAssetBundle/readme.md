# TAssetBundle

TAssetBundle are copyright 2022 All rights reserved by tigu77.

TAssetBundle is a powerful asset bundle integrated management system.

---ㅇㅊ

- [Manual](https://tigu77.github.io/TAssetBundle-api-doc/manual/manual.html)

- [Api Document](https://tigu77.github.io/TAssetBundle-api-doc/api/index.html)

- [Video Tutorials](https://www.youtube.com/playlist?list=PLB3Wee-5ukiFD7RUFiFaxbp8OQ8PTbJ0d)

---

## Good Points

- Very easy!
- Very powerful!
- Very simple!
- Customizable!

---

## Key Features

- Build and content update with one click
- A build system that guarantees the same output every time
- Incredible build speed with build cache
- You don't have to worry about AssetBundle names or dependencies at all.
- Load assets at runtime via asset path or asset reference
- Supports 2 play modes in the editor (Editor Assets, Asset Bundle)
- Split download by asset, tag and scene
- You can encrypt Asset Bundles and Asset Catalogs.
- Organize all AssetBundles automatically with Composition Strategies
- Web server support for remote testing in the editor
- Runtime Asset and AssetBundle Reference Tracking
- AssetBundle Dependency Checker
- All platforms supported

---

## Content Update Flow

1. Configure asset bundles
    - Configure manually
    - Configure Automatically Using Composition Strategies
2. Build the AssetBundle
3. Upload the result to a remote storage
4. Finish!

---

## Change Logs

### 3.9.2

- Fixed a bug where references would be lost when using AssetRef inside an asset that has already been AssetBundled when playing in AssetBundle mode in the editor.

### 3.9.1

- Fixed so that if an exception occurs during AssetBundle building, the exception is also passed to the parent. (this is mainly an issue where the next command is executed even if an exception occurs when building with a script)

- Improved way of catching duplicate assets in TAssetBundleManifest.

### 3.9.0

- Added Asset Dependency Finder (Right click on the asset in the project window [TAssetBundle] Run Dependency Finder)

### 3.8.1

- Fixed bug where assets were not excluded from the build cache if only their properties were changed

### 3.8.0

- Build number has been added to settings. When loading a catalog, it has been modified to use the catalog with the higher build number between the local catalog and the remote catalog.

### 3.7.3

- Fixed bug where local AssetBundle could not be found on Android

### 3.7.2

- Fixed initialization in AssetManager to support Unity EnterPlayMode.

### 3.7.1

- Fixed bug where disk space check was incorrect in SpecificAssetBundleProvider

### 3.7.0

- The scene path to be saved in the catalog has been modified to be saved as is rather than converted to lowercase letters.
- Changed the remote url to one for more efficient networking and simplicity.
- Cleaned up source code and added help menu

### 3.6.1

- Fixed a bug where an error occurred during build if an embedded asset bundle exists when using 'Append Hash From File Name' in the build options in Settings.
- Fixed a bug where an error occurred in the log when reading a remotely updated AssetBundle as if it were an AssetBundle included in the build.

### 3.6.0

- WebGL platform supports all Unity versions and bug fixes
- Limitations related to the WebGL platform: When using UnityRemoteAssetBundleProvider in versions after 2022, the cache information of remotely received asset bundles cannot be obtained. We recommend disabling the Use UnityRemoteAssetBundleProvider option.

### 3.5.0

- Added tag rename function to tag editor
- Added context menu to tag editor
- Added a function to check the manifest using the tag in the tag editor

### 3.4.0

- Added ignore assets to allow specific assets to be ignored in the manifest
- Efficient modifications to the method for finding embeddable assets

### 3.3.0

- Fixed a bug where the AssetBundle could not be released when the AssetBundle scene loaded with Additive was unloaded
- Fixed a bug where dependent AssetBundles would not be rebuilt if they were not cached
- Support scene loading from scene path
- Support for loading progress of assets and scenes
- Added scene activation callback after scene loading

### 3.2.1

- Fixed bug with AssetRef not updating in editor

### 3.2.0

- Added Manual
- Added Api Documentation
- Added api comment
- Added TAssetBundle, TAssetBundle.Editor asmdef
- Class scope refactoring

### 3.1.0

- Added Runtime Asset Reference Tracker
- Added AssetBundle Dependency Checker

### 3.0.0

- Added TAssetBundle Browser
- Added AssetReference
- Added Web Server Test
- Newly developed simple tag system
- Added downloads as Assets and AssetReference
- Optimize asset bundle loading
- Build cache bug fixed
- TAssetBundleManifest bug fixed
- WebGL support

### 2.0.0

- added catalog compress and encryption
- added asset bundle encryption
- added inherit based tag system
- check download size based on tags, download based on tags
- optimize and refactoring
- minor bug fixes
