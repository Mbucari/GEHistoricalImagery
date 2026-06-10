# Dump
_Dump historical image tiles into a folder._

This command will download historical imagery within a region of interest on a specified date and save all 256x256 pixel image tiles to a folder.
If imagery is not available for the specified date, the downloader will use the image from the next nearest date.

To learn about defining a region of interest, please refer to the [Regions of Interest article](./regions.md).

## Options
### `--output <Directory>` (`-o <Directory>`)
Required. The directory into which image tiles will be saved.

### `--format <FilenameFormat>` (`-f <FilenameFormat>`)
Optional. The string format used to create the dumped tiles' filenames.
|Format Tag|Tile Property|
|-|-|
|`{Z}`| [zoom](https://github.com/Mbucari/GEHistoricalImagery/blob/master/docs/dump.md#convert-between-latlong-and-rowcolumn-numbers) level |
|`{C}`| global [column](https://github.com/Mbucari/GEHistoricalImagery/blob/master/docs/dump.md#convert-between-latlong-and-rowcolumn-numbers) number |
|`{R}`| global [row](https://github.com/Mbucari/GEHistoricalImagery/blob/master/docs/dump.md#convert-between-latlong-and-rowcolumn-numbers) number |
|`{c}`| tile's column number within the region |
|`{r}`| tile's row number within the region |
|`{D}`| image capture date |
|`{LD}`| layer date (wayback only) |

Default is `z={Z}-Col={c}-Row={r}.jpg`
### `--world` (`-w`)
Optional. When specified, a [world file](https://en.wikipedia.org/wiki/World_file) is written for each image tile.
### `--dump-db`
Optional. Save the image tile data into an SQLite geo database. Each dump command is saved in the `dump_operations` table, and tile info is saved in the `dumped_tiles` table. Successive dump runs can be appended to the same database file.
### `--date yyyy/MM/dd` (`-d yyyy/MM/dd`)
Required. One or more dates used to search for matching imagery. Multiple dates can be specified by repeating the `--date` option, or by providing a string of dates delimited by a `,` (e.g. `--date 2025/01/01 --date 2025/08/31` or `--date 2025/01/01,2025/08/31`).
### `--date-match <MatchType>`
Optional. How the program should try to match available imagery to the date(s) specified by the `--date` option.
- `Closest`: Find the image whose date is closest in time to any of the specified date(s).
- `Exact`: Find the image whose date exactly matches any of the date(s) specified.
- `ClosestBefore`: Find the image whose date is closest in time to any of the specified date(s), but which is not after the date(s).
- `ClosestBefore`: Find the image whose date is closest in time to any of the specified date(s), but which is not before the date(s).

Default is `Closest`.
### `--exact-date`
Optional. Find the image whose date exactly matches one of the dates specified. This option will override the `--date-match` option.
### `--layer-date`
Optional. Wayback provider only. When used, this flag will try to match dates against the Wayback _layer date_ instead of the _image capture date_. This mode is significantly faster than trying to match image capture dates because of all the additional queries required to determine the image capture date. Read more about that problem [here](https://github.com/Mbucari/GEHistoricalImagery/issues/37#issuecomment-4604881707).
### `--target-sr <SpatialReference>`
Optional. The spatial reference to warp the tile image into. Analogous to the [gdalwarp -t_srs option](https://gdal.org/en/stable/programs/gdalwarp.html#cmdoption-gdalwarp-t_srs). If omitted, Google Earth images will be saved in their native WGS84 (EPSG:4326) projections, and ESRI images will be saved in their native Web Mercator (EPSG:3857) projections. Warped tiles are saved as JPEGs.
### `--region-file <file.kmz>`
Required (or an alternate region method). The path to a kmz/kml file containing polygons or line strings which define the region of interest. Cannot be used with `--region`, `--lower-left`, or`--upper-right`. [Read more about region files](./regions.md#using-geometry-from-a-kmz-or-kml-file). 
### `--region <Lat0>,<Lon0>+<Lat1>,<Lon1>+<Lat2>,<Lon2>+...`
Required (or an alternate region method). A list of WGS84 coordinates, delimited with a `+` symbol, which define the outer perimeter of a polygon. Cannot be used with `--region-file`, `--lower-left`, or`--upper-right`. [Read more about polygonal regions](./regions.md#polygonal-region-method). 
### `--lower-left <LAT>,<LONG>`
Required (or an alternate region method). The lower-left corner of a rectangular region. Must be used with `--upper-right`. Cannot be used with `--region` or `--region-file`. [Read more about rectangular regions](./regions.md#two-corner-rectangle-method)
### `--upper-right <LAT>,<LONG>`
Required (or an alternate region method). The upper-right corner of a rectangular region. Must be used with `--lower-left`. Cannot be used with `--region` or `--region-file`. [Read more about rectangular regions](./regions.md#two-corner-rectangle-method)
### `--zoom <N>` (`-z <N>`)
Required. The zoom level at which imagery is downloaded. Valid values are [1,23], although practically, Google Earth caps out at 21 and Wayback caps out at 20). [Read about zoom levels](https://developers.arcgis.com/documentation/mapping-and-location-services/reference/zoom-levels-and-scale/) 
### `--parallel <N>` (`-p <N>`)
Optional. The number of concurrent downloads and image processing threads. This number is capped to 10 when using `--provider=Wayback` because I determined empirically that any higher number resulted in a reduced speed. Default is `ALL_CPUS`
### `--provider <Provider>`
Optional. The aerial imagery provider to query. Options are:
- `TM`: Google Earth time machine
- `Wayback`: Esri Wayback provider.

Default is `TM`.
### `--no-cache`
Optional. Disables caching of imagery and metadata, causing APIs to be required on every run.

**Notes on the Cache Directory**

App data is cached in a directory named `GEHI_cache`, inside the app's directory or in the system's temp directory if the app has no write access to its directory. This location can be changed with an environment variable: `GEHistoricalImagery_Cache`.
### `-q`
Optional. Quiet mode. Nothing written to stderr.

## Examples
Download historical imagery tiles at zoom level `20` from within the region defined by the lower-left (southwest) corner `39.619819,-104.856121` and upper-right (northeast) corner `39.638393,-104.824990`.

### Example 1

Save the images with filenames in the format `"Zoom={Z}, Column={c}, Row={r}.jpg"`

`{Z}` will be replaced by the zoom level.

`{c}` will be replaced by the column number within the rectangle, starting with column 0 along the left (west) edge of the rectangle.

`{r}` will be replaced by the row number within the rectangle, starting with row 0 along the bottom (south) edge of the rectangle.

   **Command:**
   ```Console
   GEHistoricalImagery dump --lower-left 39.619819,-104.856121 --upper-right 39.638393,-104.824990 --zoom 20 --date 2024/06/05 -f "Zoom={Z}, Column={c}, Row={r}.jpg" -o "./Tiles"
   ```   
   **Output:**
   ```
   Zoom=20, Column=00, Row=00.jpg
   ...
   Zoom=20, Column=91, Row=54.jpg
   ```
### Example 2

Save the images with filenames in the format `"Zoom={Z}, Global Column={C}, Global Row={R}.jpg"`

`{Z}` will be replaced by the zoom level.

`{C}` will be replaced by the global column number.

`{R}` will be replaced by the global row number.

There are `2^zoom` number of global columns, beginning with column 0 at -180 degrees longitude.
There are `2^zoom` number of global rows, beginning with row 0 at -180 degrees latitude. Because latitudes are constrained to \[-90,90\] degrees, only the middle half of the global rows are used.

   **Command:**
   ```Console
   GEHistoricalImagery dump --lower-left 39.619819,-104.856121 --upper-right 39.638393,-104.824990 --zoom 20 --date 2024/06/05 -f "Zoom={Z}, Global Column={C}, Global Row={R}.jpg" -o "./Tiles"
   ```
   **Output:**
   ```
   Zoom=20, Global Column=218872, Global Row=639689.jpg
   ...
   Zoom=20, Global Column=218963, Global Row=639743.jpg
   ```
   
### Example 3

Same as [Example 1](#example-1), but warp each Google Earth tile to Web Mercator reference and save world files for each tile.

   **Command:**
   ```Console
   GEHistoricalImagery dump --lower-left 39.619819,-104.856121 --upper-right 39.638393,-104.824990 --zoom 20 --date 2024/06/05 -f "Zoom={Z}, Column={c}, Row={r}.jpg" --world --target-sr EPSG:3857 -o "./Tiles"
   ```   
   **Output:**
   ```
   Zoom=20, Column=00, Row=00.jpg
   Zoom=20, Column=00, Row=00.jgw
   ...
   Zoom=20, Column=91, Row=54.jpg
   Zoom=20, Column=91, Row=54.jgw
   ```
### Example 4

Same as [Example 1](#example-1), but specify multiple dates and require that imagery match either of those two dates exactly (no falling back to the next closest available tile). Because the command requires an exact date match (the --exact-date flag), only 544 of the 5060 tiles in the region were downloaded.

   **Command:**
   ```Console
   GEHistoricalImagery dump --lower-left 39.619819,-104.856121 --upper-right 39.638393,-104.824990 --zoom 20 --date 2022/09/26,2021/08/17 --exact-date -f "Zoom={Z}, Column={c}, Row={r}.jpg" -o "./Tiles"
   ```   
   **Output:**
   ```console
   544 out of 5060 downloaded
   ```
   ```
   Zoom=20, Column=00, Row=07.jpg
   ...
   Zoom=20, Column=91, Row=26.jpg
   ```
## Convert Between Lat/Long and Row/Column numbers

**Global** row/column numbers can be related to latitude/longitude using the following formulae:
### Google Earth Tiles
$$G=\frac{360}{2^{Z}}N-180$$ or $$N=\left\lfloor \frac{G+180}{360}2^{Z} \right\rfloor$$

Where:

$G$ is the geographic latitude/longitude<br>
$N$ is the row/column<br>
$Z$ is the zoom level.<br>
### Esri Tiles

$$\text{Longitude} = 360\frac{\text{Column}}{2^{Z}}-180$$

$$\text{Latitude} = \frac{360\cdot\arctan(\exp(2\pi(0.5-\frac{\text{Row}}{2^{Z}})))}{\pi}-90$$
or
$$\text{Column} = 2^{Z}\frac{\text{Longitude} + 180}{360}$$

$$\text{Row} = 2^{Z}(0.5-\frac{\ln(\tan(\frac{\pi}{360}\cdot(\text{Latitude}+90)))}{2\pi}) $$

Where:

$Z$ is the zoom level.<br>

************************
<p align="center"><i>Updated 2026/06/09</i></p>
