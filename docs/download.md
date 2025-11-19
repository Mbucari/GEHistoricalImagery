# Download
_Download historical imagery._

This command will download historical imagery within a region of interest on a specified date and save it as a single GeoTiff file. You may optionally specify an output spatial reference to warp the image.
If imagery is not available for the specified date, the downloader will use the image from the next nearest date. If no imagery is available for a specified date, GEHistoricalImagery will attempt to fill the holes with imagery from lower zoom levels (up to two zoom levels lower than the level specified in the command).

To learn about defining a region of interest, please refer to the [Regions of Interest article](./regions.md).

## Usage
```Console
 GEHistoricalImagery download [--region=[Lat0,Long0+Lat1,Long1+Lat2,Long2+..]] [--lower-left [LAT,LONG]] [--upper-right [LAT,LONG]] -z [N] -d [yyyy/mm/dd] -o [PATH] [--target-sr "SPATIAL REFERENCE"]] [-p [N]] [--scale [S]] [--offset-x [X]] [--offset-y [Y]] [--scale-first] [--provider [P]] [--no-cache]

  --region-file=/path/to/kmzfile.kmz           Path to a kmz or kml file containing the region geometry (polygon or
                                               polyline with at least three vertices)

  --region=Lat0,Long0+Lat1,Long1+Lat2,Long2    A list of geographic coordinates which are the vertices of the polygonal
                                               area of interest. Vertex coordinates delimiter with a '+'.

  --lower-left=LAT,LONG                        Geographic coordinate of the lower-left (southwest) corner of the
                                               rectangular area of interest.

  --upper-right=LAT,LONG                       Geographic coordinate of the upper-right (northeast) corner of the
                                               rectangular area of interest.

  -z N, --zoom=N                               Required. Zoom level [1-23]

  --provider=TM                                (Default: TM) Aerial imagery provider
                                                [TM]      Google Earth Time Machine
                                                [Wayback] ESRI World Imagery Wayback

  --no-cache                                   (Default: false) Disable local caching

  -d yyyy/MM/dd, --date=yyyy/MM/dd             Required. Imagery Date

  --layer-date                                 (Wayback only) The date specifies a layer instead of an image capture
                                               date

  -o out.tif, --output=out.tif                 Required. Output GeoTiff save location

  -p N, --parallel=N                           (Default: ALL_CPUS) Number of concurrent downloads

  --target-sr=[SPATIAL REFERENCE]              Warp image to Spatial Reference. Either EPSG:#### or path to projection
                                               file (file system or web)

  --scale=S                                    (Default: 1) Geo transform scale factor

  --offset-x=X                                 (Default: 0) Geo transform X offset

  --offset-y=Y                                 (Default: 0) Geo transform Y offset

  --scale-first                                (Default: false) Perform scaling before offsetting X and Y
```

## Examples
Download historical imagery at zoom level `20` from within the region defined by the lower-left (southwest) corner `39.619819,-104.856121` and upper-right (northeast) corner `39.638393,-104.824990`. Transform the image to SPCS Colorado Central - Feet.

### Example 1 - Get imagery from 2024/06/05

   **Command:**
   ```Console
   GEHistoricalImagery download --lower-left 39.619819,-104.856121 --upper-right 39.638393,-104.824990 --zoom 20 --date 2024/06/05 --target-sr https://epsg.io/103248.wkt --output "./Cherry Creek 1.tif"
   ```
   **Output:**
   ![Cherry Creek 1-Small.jpg](assets/Cherry%20Creek%201-Small.jpg)
   [click here to download the original file](../../../raw/d607b9c7f8851316ff893ed02396c95bb55391ef/docs/assets/Cherry%20Creek%201.tif)

### Example 2 - Get imagery from 2023/04/29

   **Command:**
   ```Console
   GEHistoricalImagery download --lower-left 39.619819,-104.856121 --upper-right 39.638393,-104.824990 --zoom 20 --date 2023/04/29 --target-sr https://epsg.io/103248.wkt --output "./Cherry Creek 2.tif"
   ```
   Notice that the left ~30% of the image is from a different date than the rest of the image. This matches the availability shown in [Availability Map 2](availability.md#availability-map-2---imagery-from-20230429).
   
   **Output:**
   ![Cherry Creek 2-Small.jpg](assets/Cherry%20Creek%202-Small.jpg)
   [click here to download the original file](../../../raw/d607b9c7f8851316ff893ed02396c95bb55391ef/docs/assets/Cherry%20Creek%202.tif)

### Example 3 -  Get imagery from 2021/08/17

   **Command:**
   ```Console
   GEHistoricalImagery download --lower-left 39.619819,-104.856121 --upper-right 39.638393,-104.824990 --zoom 20 --date 2021/08/17 --target-sr https://epsg.io/103248.wkt --output "./Cherry Creek 3.tif"
   ```
   Notice the L-shaped region of the image is from a different date than the rest of the image. This matches the availability shown in [Availability Map 3](availability.md#availability-map-3---imagery-from-20210517).

   **Output:**
   ![Cherry Creek 3-Small.jpg](assets/Cherry%20Creek%203-Small.jpg)
   [click here to download the original file](../../../raw/d607b9c7f8851316ff893ed02396c95bb55391ef/docs/assets/Cherry%20Creek%203.tif)

### Example 4 -  Get imagery from Esri Wayback version 2023/04/15

   **NOTE : The date in this command is the date of the Wayback layer, _not the image capture date_.**
   
   **Command:**
   ```Console
   GEHistoricalImagery download --provider wayback --lower-left 39.619819,-104.856121 --upper-right 39.638393,-104.824990 --zoom 19 --date 2023/04/23 --target-sr https://epsg.io/103248.wkt --output "./Cherry Creek 4.tif"
   ```

   **Output:**
   ![Cherry Creek 4-Small.jpg](assets/Cherry%20Creek%204-Small.jpg)

### Example 5 - Get imagery from an irregular region

   This it the same area, date, and zoom level as [Example 1](#example-1---get-imagery-from-20240605), but the area of interest has been defined using the `--region` option to exclude the lake and the developments in the northeast corner.

   **Command:**
   ```Console
   GEHistoricalImagery download -z 20 -d 2024/06/05 --provider tm --target-sr https://epsg.io/103248.wkt -o ".\..\Cherry Creek 5.tif" --region 39.619819,-104.856121+39.632275,-104.856121+39.631955,-104.854045+39.632230,-104.851632+39.631864,-104.850180+39.631864,-104.848694+39.632139,-104.846911+39.633238,-104.845782+39.634108,-104.843761+39.635345,-104.842513+39.637268,-104.841860+39.638393,-104.841563+39.638393,-104.828670+39.636859,-104.828686+39.636081,-104.828567+39.635228,-104.828357+39.634643,-104.828082+39.629716,-104.824990+39.619819,-104.824990
   ```

   **Output:**
   ![Cherry Creek 5-Small.jpg](assets/Cherry%20Creek%205-Small.jpg)

************************
<p align="center"><i>Updated 2025/06/20</i></p>
