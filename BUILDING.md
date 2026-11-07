# Building Peggle Enhanced

Every platform comes in two editions:

- **HD**: Peggle Classic HD 2.0.6 (iPad, 1024x768). The default.
- **iPhone**: Peggle Classic 1.7.2 (iPhone 5, 1136x640 with the 960x640 art).

Pick one with `-p:PeggleEdition=HD` or `-p:PeggleEdition=iPhone` (or the edition argument of the scripts below). The two editions are separate apps, so both can be installed side by side.

## Downloads (GitHub Releases)

The `Build` workflow (`.github/workflows/build.yml`) builds every platform for both editions and publishes them all as one GitHub release:

- Every push to `main`, and every manual run (Actions → **Build** → **Run workflow**), publishes a **pre-release** tagged and titled `<version>-prerelease+<n>`, e.g. `1.1-prerelease+17`, once every platform is built. Its notes are generated from the commits.
- Pushing a version tag (e.g. `1.2`, without a `v`, like the existing releases) publishes a full release. Set `<Version>` in `Directory.Build.props` to match before tagging: a tag build's app version is exactly `<Version>`. Each platform's files are added as soon as that platform is built, so Windows, Linux and Android are up before iOS and macOS. If the release doesn't exist yet, it's created with the tag as its title and marked as the latest release. If it already exists (e.g. you published it with your own notes, which creates the tag), only files are added: its title and notes stay as they are, apart from an invisible `<!-- peggle-build ... -->` line added to the end of the notes if it isn't there, which lists the release in the AltStore / SideStore source.
- If the tagged commit already has a pre-release (from its `main` build), the full release reuses that pre-release's files instead of building them again, which is much faster. If that `main` build is still running, the tag's run waits for it. The files only differ by the build number: their version is the pre-release's `<version>.<n>`.
- Automatic builds are never marked as the latest release. The latest release stays the last tagged one: it's what the in-game update checker compares its version with, and what the README's download links serve. The AltStore / SideStore source lists every build, pre-releases included.
- Pull requests only build; their files are under the run's **Artifacts** (kept for 14 days, each wrapped in a zip). Pushes to other branches don't start a build by themselves (macOS runners are scarce), so test a branch through its pull request or a manual run.

| Platform | Files |
|---|---|
| Windows | `PeggleHD-windows-x64.zip`, `Peggle-windows-x64.zip` |
| macOS | `PeggleHD-macOS-osx-x64.zip` (Intel), `PeggleHD-macOS-osx-arm64.zip` (Apple Silicon), same for `Peggle` |
| Linux | `PeggleHD-x86_64.AppImage`, `PeggleHD-linux-x64.tar.gz`, same for `Peggle` |
| Android | `PeggleHD-android.apk`, `Peggle-android.apk` |
| iOS / iPadOS | `PeggleHD-unsigned.ipa`, `Peggle-unsigned.ipa` |
| Web | `PeggleHD-web.zip`, `Peggle-web.zip` (the site, also put on GitHub Pages: see [Web](#web)) |

Each automatic build's version is `<version>.<n>`: the `<Version>` in `Directory.Build.props` plus the build number `<n>` (the workflow's run number), e.g. `1.1.17` for build 17. A version tag's build is exactly `<Version>`. It's the iOS `CFBundleShortVersionString`, the Android `versionName` and the macOS app version, and `<n>` alone is the Android `versionCode` and iOS `CFBundleVersion`. So every build installs as an update, and AltStore / SideStore offer it as one.

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

The workflow builds unsigned `.ipa` files that need iOS 15 or later. Install one with a tool that signs apps when it installs them: Sideloadly or AltStore/SideStore (with a free Apple ID), or TrollStore.

### AltStore / SideStore source

Each build also updates an AltStore / SideStore source on GitHub Pages, at `https://<owner>.github.io/peggle-enhanced/ios/`. The page has **Add to AltStore** / **Add to SideStore** buttons. Once the source is added, both editions can be installed from it, and new builds show up as updates. The source itself is `https://<owner>.github.io/peggle-enhanced/ios/apps.json`. It lists the IPAs of the last 15 releases and is generated by `distribution/altstore/make_source.py`.

To turn it on once: Settings → Pages → Source: **GitHub Actions**. Until then, the Pages workflow only shows a warning. Saves are kept in the app's Documents folder, which you can open in the Files app (On My iPhone/iPad → Peggle).

To build it on a Mac (needs Xcode 26 or later and `dotnet workload install ios`):

```sh
dotnet build Peggle.iOS -c Release -r ios-arm64 -p:PeggleEdition=HD -p:EnableCodeSigning=false
```

This makes `PeggleClassic.app` under `Peggle.iOS/bin/`. To turn it into an `.ipa`, put it in a folder named `Payload` and zip that folder.

## Web (experimental)

Both editions run in a browser (WebAssembly and WebGL, through [KNI](https://github.com/kniEngine/kni), an XNA implementation with a web backend). After every build, the `Pages` workflow (`.github/workflows/pages.yml`) rebuilds the GitHub Pages site from the releases:

| Path | Plays |
|---|---|
| `https://<owner>.github.io/peggle-enhanced/` | Peggle (iPhone edition), latest full release |
| `https://<owner>.github.io/peggle-enhanced/hd/` | Peggle HD, latest full release |
| `https://<owner>.github.io/peggle-enhanced/pre-release/` | Peggle (iPhone edition), newest pre-release |
| `https://<owner>.github.io/peggle-enhanced/pre-release/hd/` | Peggle HD, newest pre-release |
| `https://<owner>.github.io/peggle-enhanced/ios/` | the AltStore / SideStore source |

Each release keeps its web build as `Peggle-web.zip` / `PeggleHD-web.zip`, and `distribution/make_pages.py` puts the latest full release's and the newest pre-release's on the site. Until a full release has them, `/` and `/hd/` lead to the pre-release. The four builds would exceed Pages' 1 GB limit, so their game data goes into one shared folder (`/data/`), each file once.

Saves are kept in the browser (IndexedDB), one set per edition, shared by the release and the pre-release. Clearing the site's data deletes them. Phones and tablets play with touch, as the mobile versions do; browsers with a mouse play like the PC version. Everything loads while the loading screen shows (Low Memory Mode is always off on the web, and hidden), so the first visit takes longer than later ones, which use the browser's cache. Images are converted to WebP when the site is built (`distribution/web_content.py`, Pillow) and decoded with SkiaSharp, which only the web version uses.

To build it (needs `dotnet workload install wasm-tools`, which links SkiaSharp in, and Python 3 with Pillow; Release is compiled ahead of time, which takes a while):

```sh
# -> distribution/out/web/<Exe>/ and distribution/out/<Exe>-web.zip (needs zip)
distribution/build_web.sh HD
python3 -m http.server -d distribution/out/web/PeggleHD
```

The site must be opened through a web server, not as a file. `CONFIGURATION=Debug distribution/build_web.sh HD` builds faster without ahead-of-time compilation, but runs much slower. The project needs `-p:PeggleWeb=true`, which builds the game libraries against KNI instead of MonoGame, so it isn't part of `PeggleClassic.sln`.

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

The release zips are NativeAOT builds: extract one and run `PeggleHD.exe` or `Peggle.exe`. Saves are kept next to the executable. To build them yourself:

```bat
distribution\build_windows_hd.bat
distribution\publish_windows_hd.bat
```

`build_*` makes a regular build. `publish_*` makes a NativeAOT build, which needs Visual Studio with "Desktop development with C++". The scripts without `_hd` build the iPhone edition.
