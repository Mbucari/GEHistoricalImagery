# GEHistoricalImagery
GEHistoricalImagery is a utility for downloading historical aerial imagery from Google Earth...
**and now also from Esri's World Atlas Wayback**

**Features**
- Find historical imagery availability at any location and zoom level
- Always uses the most recent provider data
- Automatically substitutes unavailable tiles with temporally closest available tile
- Outputs a georeferenced GeoTiff or dumps image tiles to a folder
- Supports warping to new coordinate systems
- Fast! Parallel downloading and local caching

**Commands**
|Command|Description|
|-|-|
|[info](https://github.com/Mbucari/GEHistoricalImagery/blob/master/docs/info.md)|Get imagery info at a specified location.|
|[availability](https://github.com/Mbucari/GEHistoricalImagery/blob/master/docs/availability.md)|Get imagery date availability in a specified region.|
|[download](https://github.com/Mbucari/GEHistoricalImagery/blob/master/docs/download.md)|Download historical imagery.|
|[dump](https://github.com/Mbucari/GEHistoricalImagery/blob/master/docs/dump.md)|Dump historical image tiles into a folder.|

To learn about defining regions of interest for these commands, please refer to the [Regions of Interest article](https://github.com/Mbucari/GEHistoricalImagery/blob/master/docs/regions.md).

************************
## Build and Run on Linux (x64 and arm64)

Ideally you should use the Release binary packaged, but I've provided `gehinix.sh` to download, build and run GEHistoricalImagery.

The script will:
- download and install the dotnet sdk (if necessary)
- Clone and build the master branch of this repo (if necessary)
- And finally run GEHistoricalImagery with arguments

```console
wget https://raw.githubusercontent.com/Mbucari/GEHistoricalImagery/refs/heads/master/gehinix.sh
chmod +x gehinix.sh
./gehinix.sh
```

************************
<p align="center"><i>Updated 2025/06/20</i></p>
