# UnityCef
A web browser component for Unity

[![GitHub release](https://img.shields.io/github/release/WiiPlayer2/UnityCef.svg)](https://github.com/WiiPlayer2/UnityCef/releases/latest)
[![Github Releases](https://img.shields.io/github/downloads/WiiPlayer2/UnityCef/total.svg)](https://github.com/WiiPlayer2/UnityCef/releases/latest)
[![Github commits (since latest release)](https://img.shields.io/github/commits-since/WiiPlayer2/UnityCef/latest.svg)](https://github.com/WiiPlayer2/UnityCef/commits/master)
[![Donate](https://img.shields.io/badge/liberapay-donate-red.svg)](https://liberapay.com/WiiPlayer2/)

## Usage
1. Download and import the latest release package.
2. Add the `WebBrowser` component to an object.
3. `WebBrowser.Texture` is a `Texture2D` of the rendered web content.

## Building
### Requirements
- Python 2.7 (required for building cefglue)
- MSBuild (required for building the companion app and library)
- Unity (required for building the package)

### Steps
1. Clone or download the repository.
2. Optional: Copy `cake/vars.sample.cake` to `cake/vars.cake` and edit values (most importantly `python27_path`).
3. Run `build.ps1 -target unity-package`.

If everything worked correctly you should now have a `UnityCef-X.Y.HASH.unitypackage` file in your directory.

## Donate
Please consider donating a bit to keep this and other projects going. It would mean a lot to me.

## Credits
This project uses
- Chromium Embedded Framework (https://bitbucket.org/chromiumembedded/cef)
- cefglue (https://gitlab.com/xiliumhq/chromiumembedded/cefglue)
- SharpZipLib (https://github.com/icsharpcode/SharpZipLib)
