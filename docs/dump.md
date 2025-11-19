# Dump
_Dump historical image tiles into a folder._

This command will download historical imagery within a region of interest on a specified date and save all 256x256 pixel image tiles to a folder.
If imagery is not available for the specified date, the downloader will use the image from the next nearest date.

To learn about defining a region of interest, please refer to the [Regions of Interest article](./regions.md).

## Usage
```Console
 GEHistoricalImagery dump [--region=[Lat0,Long0+Lat1,Long1+Lat2,Long2+...]] [--lower-left [LAT,LONG]] [--upper-right [LAT,LONG]] -z [N] -d [yyyy/mm/dd] -o [Directory] [--format [FORMAT_STRING]] [-p [N]] [--provider [P]] [--no-cache] [--target-sr "SPATIAL REFERENCE"]] [--world]

  -d yyyy/MM/dd, --date=yyyy/MM/dd                  Required. Imagery Date

  --layer-date                                      (Wayback only) The date specifies a layer instead of an image
                                                    capture date

  -o [Directory], --output=[Directory]              Required. Output image tile save directory

  -f [FilenameFormat], --format=[FilenameFormat]    (Default: z={Z}-Col={c}-Row={r}.jpg)
                                                    Filename formatter:
                                                      "{Z}" = tile's zoom level
                                                      "{C}" = tile's global column number
                                                      "{R}" = tile's global row number
                                                      "{c}" = tile's column number within the rectangle
                                                      "{r}" = tile's row number within the rectangle
                                                      "{D}" = tile's image capture date
                                                      "{LD}" = tile's layer date (wayback only)

  -p N, --parallel=N                                (Default: ALL_CPUS) Number of concurrent downloads

  --target-sr=[SPATIAL REFERENCE]                   Warp image to Spatial Reference. Either EPSG:#### or path to
                                                    projection file (file system or web)

  -w, --world                                       Write a world file for each tile

  --region-file=/path/to/kmzfile.kmz                Path to a kmz or kml file containing the region geometry (polygon or
                                                    polyline with at least three vertices)

  --region=Lat0,Long0+Lat1,Long1+Lat2,Long2         A list of geographic coordinates which are the vertices of the
                                                    polygonal area of interest. Vertex coordinates delimiter with a '+'.

  --lower-left=LAT,LONG                             Geographic coordinate of the lower-left (southwest) corner of the
                                                    rectangular area of interest.

  --upper-right=LAT,LONG                            Geographic coordinate of the upper-right (northeast) corner of the
                                                    rectangular area of interest.

  -z N, --zoom=N                                    Required. Zoom level [1-23]

  --provider=TM                                     (Default: TM) Aerial imagery provider
                                                     [TM]      Google Earth Time Machine
                                                     [Wayback] ESRI World Imagery Wayback

  --no-cache                                        (Default: false) Disable local caching
```
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
## Convert Between Lat/Long and Row/Column numbers

**Global** row/column numbers can be related to latitude/longitude using the following formulae:
### Google Earth Tiles
$$G=\frac{360}{2^{Z}}N-180$$ or $$N=\left\lfloor \frac{G+180}{360}2^{Z} \right\rfloor$$

Where:

$G$ is the geographic latitude/longitude<br>
$N$ is the row/column<br>
$Z$ is the zoom level.<br>
### Esri Tiles

$$Longitude = 360\frac{Column}{2^{Z}}-180$$

$$Latitude = \arctan(\sinh(\pi (1-2\frac{Row}{2^{Z}}))) \frac{180}{\pi}$$
or
$$Column = 2^{Z}\frac{Longitude + 180}{360}$$

$$Row = \frac{2^{Z}}{2}(1 - \frac{1}{\pi}\ln(\tan(\frac{\pi\cdot Latitude}{180}) + \sec(\frac{\pi\cdot Latitude}{180})) $$

Where:

$Z$ is the zoom level.<br>

************************
<p align="center"><i>Updated 2025/11/19</i></p>
