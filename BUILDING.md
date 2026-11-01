# Building Peggle Enhanced

Every platform comes in two editions:

- **HD**: Peggle Classic HD 2.0.6 (iPad, 1024x768). The default.
- **iPhone**: Peggle Classic 1.7.2 (iPhone 5, 1136x640 with the 960x640 art).

Pick one with `-p:PeggleEdition=HD` or `-p:PeggleEdition=iPhone` (or the edition argument of the scripts below). The two editions are separate apps, so both can be installed side by side.

## Downloading builds from GitHub Actions

GitHub Actions builds every platform below for both editions. Open the **Actions** tab, pick a workflow, press **Run workflow**, then download the files from the finished run's **Artifacts** section. Pushes and pull requests also start these workflows. Pushing a `v*` tag attaches the files to that tag's GitHub release.

| Platform | Workflow | Files |
|---|---|---|
| Android | `Android` | `PeggleHD-android.apk`, `Peggle-android.apk` |
| iOS / iPadOS | `iOS (unsigned IPA)` | `PeggleHD-unsigned.ipa`, `Peggle-unsigned.ipa` |
| macOS | `macOS` | `PeggleHD-macOS-osx-x64.zip` (Intel), `...-osx-arm64.zip` (Apple Silicon) |
| Linux | `Linux` | `PeggleHD-x86_64.AppImage`, `PeggleHD-linux-x64.tar.gz` |

GitHub wraps each artifact in a zip, so extract it first.

## Android (experimental)

Install the signed APK. If you build it yourself, use `<package>-Signed.apk` (e.g. `com.aidrum.peggle.hd-Signed.apk`), not the unsigned `<package>.apk` next to it, which Android refuses to install. Saves are kept in `Android/data/<package>/files`.

```sh
dotnet build Peggle.Android -c Release -p:PeggleEdition=HD
```

On Windows, `distribution/build_android.bat` and `build_android_hd.bat` do the same.

### Signing key for CI builds

Without a key of your own, each CI build is signed with a new throwaway debug key, so it won't install over the previous build. Uninstalling first deletes the saves. To sign every build with the same key, make a keystore once and add it as repository secrets. Any machine with Java works, including Termux (`pkg install openjdk-21 gh`):

```sh
keytool -genkeypair -keystore peggle.keystore -alias peggle \
  -keyalg RSA -keysize 2048 -validity 10000 -storetype PKCS12

gh auth login
gh secret set ANDROID_KEYSTORE_BASE64 -R <owner>/peggle-enhanced < <(base64 -w0 peggle.keystore)
gh secret set ANDROID_KEY_ALIAS -R <owner>/peggle-enhanced -b peggle
gh secret set ANDROID_KEYSTORE_PASSWORD -R <owner>/peggle-enhanced
```

You can also add the secrets in the browser under Settings → Secrets and variables → Actions. `ANDROID_KEY_PASSWORD` is only needed if the key's password differs from the keystore's. Keep a backup of the keystore and its password: builds signed with a different key can't be installed over the existing app.

An app signed with another key (for example an official release) has to be uninstalled before yours will install. Back up `Android/data/<package>/files` first to keep your progress.

## iOS / iPadOS (experimental)

The workflow builds unsigned `.ipa` files that need iOS 15 or later. Install one with a tool that signs apps when it installs them: Sideloadly or AltStore/SideStore (with a free Apple ID), or TrollStore. Saves are kept in the app's Documents folder, which you can open in the Files app (On My iPhone/iPad → Peggle).

To build it on a Mac (needs Xcode 26 or later and `dotnet workload install ios`):

```sh
dotnet build Peggle.iOS -c Release -r ios-arm64 -p:PeggleEdition=HD -p:EnableCodeSigning=false
```

This makes `PeggleClassic.app` under `Peggle.iOS/bin/`. To turn it into an `.ipa`, put it in a folder named `Payload` and zip that folder.

## macOS (experimental)

`.app` bundles for Intel (`osx-x64`) and Apple Silicon (`osx-arm64`). They are only ad-hoc signed, so the first time you open one, right-click it and choose **Open**, or run `xattr -dr com.apple.quarantine "Peggle HD.app"`. Saves are kept in `~/Library/Application Support/Peggle Enhanced/`.

```sh
# edition HD|iPhone, rid osx-x64|osx-arm64 -> distribution/out/
distribution/build_macos.sh HD osx-x64
```

The script works on any OS. On macOS it also signs the app and zips it.

## Linux (experimental)

x64 builds as an AppImage (make it executable and run it) and as a `.tar.gz` (extract it and run `PeggleHD` or `Peggle`). Saves are kept in `~/.local/share/Peggle Enhanced/`.

```sh
# .tar.gz, plus an AppImage if appimagetool is installed (or APPIMAGETOOL points to it)
distribution/build_linux.sh HD
```

## Windows

```bat
distribution\build_windows_hd.bat
distribution\publish_windows_hd.bat
```

`build_*` makes a regular build. `publish_*` makes a NativeAOT build, which needs Visual Studio with "Desktop development with C++". The scripts without `_hd` build the iPhone edition.
