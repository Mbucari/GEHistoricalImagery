# Dump
_Dump historical image tiles into a folder._

This command will download historical imagery from within a region on a specified date and save all 256x256 pixel image tiles to a folder.
If imagery is not available for the specified date, the downloader will use the image from the next nearest date.

## Usage
```Console
 GEHistoricalImagery dump --lower-left [LAT,LONG] --upper-right [LAT,LONG] -z [N] -d [yyyy/mm/dd] -o [Directory] [--format [FORMAT_STRING]] [-p [N]]

  --lower-left=LAT,LONG                             Required. Geographic coordinate of the lower-left (southwest) corner
                                                    of the rectangular area of interest.

  --upper-right=LAT,LONG                            Required. Geographic coordinate of the upper-right (northeast)
                                                    corner of the rectangular area of interest.

  -z N, --zoom=N                                    Required. Zoom level [1-24]

  -d yyyy/MM/dd, --date=yyyy/MM/dd                  Required. Imagery Date

  -o [Directory], --output=[Directory]              Required. Output image tile save directory

  -f [FilenameFormat], --format=[FilenameFormat]    (Default: z={Z}-Col={c}-Row={r}.jpg)
                                                    Filename formatter:
                                                      "{Z}" = tile's zoom level
                                                      "{C}" = tile's global column number
                                                      "{R}" = tile's global row number
                                                      "{c}" = tile's column number within the rectangle
                                                      "{r}" = tile's row number within the rectangle

  -p N, --parallel=N                                (Default: ALL_CPUS) Number of concurrent downloads
  
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
   GEHistoricalImagery dump --lower-left 39.619819,-104.856121 --upper-right 39.638393,-104.824990 --zoom 20 --date 2024/06/05 -f "Zoom={Z}, Column={c}, Row={r}.jpg" -o ".\Tiles"
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
   GEHistoricalImagery dump --lower-left 39.619819,-104.856121 --upper-right 39.638393,-104.824990 --zoom 20 --date 2024/06/05 -f "Zoom={Z}, Global Column={C}, Global Row={R}.jpg" -o ".\Tiles"
   ```
   **Output:**
   ```
   Zoom=20, Global Column=218872, Global Row=639689.jpg
   ...
   Zoom=20, Global Column=218963, Global Row=639743.jpg
   ```
## Convert Between Lat/Long and Row/Column numbers
**Global** row/column numbers can be related to latitude/longitude using the following formulae:

$$G=\frac{360}{2^{Z}}N-180$$ or $$N=\left\lfloor \frac{G+180}{360}2^{Z} \right\rfloor$$

Where:

$G$ is the geographic latitude/longitude<br>
$N$ is the row/column<br>
$Z$ is the zoom level.<br>

************************
<p align="center"><i>Updated 2024/12/4</i></p>
