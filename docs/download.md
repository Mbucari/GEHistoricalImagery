# Download
_Download historical imagery._

This command will download historical imagery within a region of interest on a specified date and save it as a single GeoTiff file. You may optionally specify an output spatial reference to warp the image.
If imagery is not available for the specified date, the downloader will use the image from the next nearest date. If no imagery is available for a specified date, GEHistoricalImagery will attempt to fill the holes with imagery from lower zoom levels (up to two zoom levels lower than the level specified in the command).

To learn about defining a region of interest, please refer to the [Regions of Interest article](./regions.md).

## Options

### `--output <file.ext>` (`-o <file.ext>`)
Required. The file path to save the completed raster image file.

### `--date yyyy/MM/dd` (`-d yyyy/MM/dd`)
Required. One or more dates used to search for matching imagery. Multiple dates can be specified by repeating the `--date` option, or by providing a string of dates delimited by a `,` (e.g. `--date 2025/01/01 --date 2025/08/31` or `--date 2025/01/01,2025/08/31`).

### `--zoom <N>` (`-z <N>`)
Required. The zoom level at which imagery is downloaded. Valid values are [1,23], although practically, Google Earth caps out at 21 and Wayback caps out at 20). [Read about zoom levels](https://developers.arcgis.com/documentation/mapping-and-location-services/reference/zoom-levels-and-scale/).

### `--region-file <file.kmz>`
Required (or an alternate region method). The path to a kmz/kml file containing polygons or line strings which define the region of interest. Cannot be used with `--region`, `--lower-left`, or`--upper-right`. [Read more about region files](./regions.md#using-geometry-from-a-kmz-or-kml-file).

### `--region <Lat0>,<Lon0>+<Lat1>,<Lon1>+<Lat2>,<Lon2>+...`
Required (or an alternate region method). A list of WGS84 coordinates, delimited with a `+` symbol, which define the outer perimeter of a polygon. Cannot be used with `--region-file`, `--lower-left`, or`--upper-right`. [Read more about polygonal regions](./regions.md#polygonal-region-method).

### `--lower-left <LAT>,<LONG>`
Required (or an alternate region method). The lower-left corner of a rectangular region. Must be used with `--upper-right`. Cannot be used with `--region` or `--region-file`. [Read more about rectangular regions](./regions.md#two-corner-rectangle-method).

### `--upper-right <LAT>,<LONG>`
Required (or an alternate region method). The upper-right corner of a rectangular region. Must be used with `--lower-left`. Cannot be used with `--region` or `--region-file`. [Read more about rectangular regions](./regions.md#two-corner-rectangle-method).

### `--of <format>`
Optional. The raster image file format to save the output file. Analogous to the [gdalwarp -of option](https://gdal.org/en/stable/programs/gdalwarp.html#cmdoption-gdalwarp-of). Accepts any of dozens of raster dataset types [supported by GDAL](https://gdal.org/en/stable/drivers/raster/index.html), but the format **must support 8-bit data types and multiple bands**, and **GDAL must be able to create the format file**. Default is [GTiff](https://gdal.org/en/stable/drivers/raster/gtiff.html).

### `--co <NAME>=<VALUE>`
Optional. Any of a number of raster dataset creation options supported by the output format. Analogous to the [gdalwarp -co option](https://gdal.org/en/stable/programs/gdalwarp.html#cmdoption-gdalwarp-co). Defaults are:
- `--of GTiff`: `COMPRESS=JPEG`, `JPEG_QUALITY=75`, `PHOTOMETRIC=YCBCR`, `TILED=TRUE`
- `--of COG`: `COMPRESS=JPEG`, `QUALITY=75`

### `--scale <S>`
Optional. The scale factor to apply to the output image. Only affects the [GeoTransform](https://gdal.org/en/stable/tutorials/geotransforms_tut.html) of the worldfile, not any of the pixels. Both the pixel sizes and the upper-left corner coordinate are scaled. Default is 1.

### `--offset-x <X>`
Optional. Distance to shift the X-coordinate of the upper-left corner. Only affects the [GeoTransform](https://gdal.org/en/stable/tutorials/geotransforms_tut.html) of the worldfile, not any of the pixels. Default is 0.

### `--offset-y <Y>`
Optional. Distance to shift the Y-coordinate of the upper-left corner. Only affects the [GeoTransform](https://gdal.org/en/stable/tutorials/geotransforms_tut.html) of the worldfile, not any of the pixels. Default is 0.

### `--scale-first`
Optional. By default, the GeoTransform is translated by the X and Y offsets and then is scaled by the scale factor. If this flag is used, the [GeoTransform](https://gdal.org/en/stable/tutorials/geotransforms_tut.html) is scaled first and then is translated by the offset distances.

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
Optional. The spatial reference to warp the image into. Analogous to the [gdalwarp -t_srs option](https://gdal.org/en/stable/programs/gdalwarp.html#cmdoption-gdalwarp-t_srs). If omitted, Google Earth images will be saved in their native WGS84 (EPSG:4326) projections, and ESRI images will be saved in their native Web Mercator (EPSG:3857) projections.

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

### Example 3 - Get imagery from 2021/08/17

   **Command:**
   ```Console
   GEHistoricalImagery download --lower-left 39.619819,-104.856121 --upper-right 39.638393,-104.824990 --zoom 20 --date 2021/08/17 --target-sr https://epsg.io/103248.wkt --output "./Cherry Creek 3.tif"
   ```
   Notice the L-shaped region of the image is from a different date than the rest of the image. This matches the availability shown in [Availability Map 3](availability.md#availability-map-3---imagery-from-20210517).

   **Output:**
   ![Cherry Creek 3-Small.jpg](assets/Cherry%20Creek%203-Small.jpg)
   [click here to download the original file](../../../raw/d607b9c7f8851316ff893ed02396c95bb55391ef/docs/assets/Cherry%20Creek%203.tif)

### Example 4 - Get imagery from Esri Wayback version 2023/04/15

   **NOTE** : The date in this command is the image capture date. Determining the image capture date for each tile is slow because it requires an additional query for every tile. For faster downloads, use the `--layer-date` option to download imagery from a single Wayback layer.

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

### Example 6 - Get imagery from Esri wayback on 2023/05/05 and 2023/04/16 only

   This is the same area, as [Example 1](#example-1---get-imagery-from-20240605), but using the Esri Wayback provider at zoom level 18, and specifying two imagery dates, and requiring that imagery match either of those two dates _exactly_ (no falling back to the next closest available tile). Because the command requires an exact date match (the `--exact-date` flag), the image is black everywhere that imagery is unavailable on those dates.

   **Command:**
   ```Console
   GEHistoricalImagery download --provider wayback --lower-left 39.619819,-104.856121 --upper-right 39.638393,-104.824990 --output "./cherry Creek 6.tif" --zoom 18 --date 2023/05/05,2023/04/16 --exact-date --target-sr EPSG:2232
   ```

   **Output:**
   ![Cherry Creek 6-Small.jpg](assets/Cherry%20Creek%206-Small.jpg)

************************
<p align="center"><i>Updated 2026/06/10</i></p>
