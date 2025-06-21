# Regions of Interest

The GEHistoricalImagery [availability](./availability.md), [download](./download.md), and [dump](./dump.md) commands require specifying a region of interest. Currently, there are three methods for defining a geographic area of interest.

## Two-Corner Rectangle Method

The simplest method for defining a geographic region is by providing the lower-left (southwest) and upper-right (northeast) corners of a rectangular. This is the method used in most of the documentation examples. To use this method, you must provide the WGS 1984 geographic coordinates in the format `LATITUDE,LONGITUDE`.

### Example 1 - A Typical Rectangle

A rectangular area of interest whose
- southwest corner is at 39.619819°N, 104.856121°W
- northeast corner is at 39.638393°N, 104.824990°W
- width spans 0.031131 degrees of longitude
- height spans 0.018574 degrees of latitude

```console
--lower-left 39.619819,-104.856121 --upper-right 39.638393,-104.824990
```
### Example 2 - A Rectangle Crossing the Antimeridian
A rectangular area of interest that crosses the 180°E/180°W meridian and whose
- southwest corner is at 16.862699°S, 179.968436°E
- northeast corner is at 16.799630°S, 179.956893°W
- width spans 0.074671 degrees of longitude
- height spans 0.063069 degrees of latitude

```console
--lower-left -16.862699,179.968436 --upper-right -16.799630,-179.956893
```
## Polygonal Region Method

A more complex method for defining a region is to define an irregular polygon by providing a list of its vertices (in WGS 1984 geographic coordinates). To use this method, you must provide a list of WGS 1984 geographic coordinates, delimited with a `+` character, in the format `LATITUDE,LONGITUDE+LATITUDE,LONGITUDE+LATITUDE,LONGITUDE+...`.

Note that you _shouldn't_ "close" the polygon by repeating the same vertex at the start and end of the vertex list.

### Example 1 - A Typical Polygon

A five-pointed star polygonal area of interest whose
- minimum (south-most) latitude is 39.626167°N
- maximum (north-most) latitude is 39.63453°N
- minimum (west-most) longitude is 104.847527°W
- maximum (east-most) longitude is 104.836114°W

```console
--region -104.840473,39.631336+-104.836114,39.631336+-104.839641,39.629361+-104.838294,39.626167+-104.84182,39.628141+-104.845347,39.626167+-104.844,39.629361+-104.847527,39.631336+-104.843167,39.631336+-104.84182,39.63453
```
### Example 2 - A Polygon Crossing the Antimeridian

An irregular polygonal area of interest that crosses the 180°E/180°W meridian (starting in the eastern hemisphere and crossing into the western hemisphere) and whose
- minimum (south-most) latitude is 16.849704°S
- maximum (north-most) latitude is 16.79878°S
- minimum (west-most) longitude is 179.964134°E
- maximum (east-most) longitude is 180.03262°E (aka 179.96738°W)

Note that when crossing the antimeridian from the eastern hemisphere to the western hemisphere (going from point `-16.79878,179.991` to `-16.79878,180.03262`, the points _after_ crossing increase over 180 degrees. This is necessary for the program to understand that you are crossing the antimeridian instead of taking the longer way around the earth. If the first point after crossing was written as `-16.79878,-179.96738` instead of `-16.79878,180.03262` (which are the same points on the globe modulus 360 degrees), GEHistoricalImagery would interpret that to mean that the polygon's next vertex is located 359.95838 west of point `-16.79878,179.991`.

```console
--region -16.849704,179.964134+-16.829128,179.97+-16.79878,179.991+-16.79878,180.03262+-16.849704,180.004219
```

### Example 3 - Same as Example 2 but Cross Antimeridian in Opposite Direction

An irregular polygonal area of interest that crosses the 180°W/180°E meridian (starting in the western hemisphere and crossing into the eastern hemisphere) and whose
- minimum (south-most) latitude is 16.849704°S
- maximum (north-most) latitude is 16.79878°S
- minimum (west-most) longitude is 180.035866°W (aka 179.964134°E)
- maximum (east-most) longitude is 179.96738°W
```console
--region -16.849704,-179.995781+-16.79878,-179.96738+-16.79878,-180.009+-16.829128,-180.03+-16.849704,-180.035866
```
## Using Geometry from a KMZ or KML File

The newest and perhaps easiest method to specify a region of interest is to provide a path to a [kmz/kml](https://pro.arcgis.com/en/pro-app/latest/help/data/kml/what-is-kml-.htm) file containing polygons or line strings which define the region of interest. If the supplied kmz/kml file contains more than one geometry suitable for defining a region of interest, GEHistoricalImagery will list all suitable placemarks and prompt you to choose one.

### Example - Using [RegionFile.kmz](./assets/RegionFile.kmz)

Specify the region of interest by supplying the path to a kmz/kml file containing polygons or line strings with >= 2 line segments.
```console
--region-file /path/to/RegionFile.kmz
```
Because [RegionFile.kmz](./assets/RegionFile.kmz) contains five placemarks suitable for defining a region of interest, GEHistoricalImagery presents a list of the five placemarks' feature-types, names, and approximate areas (in square kilometers). Press the number (or letter, depending on how many options are available), and the command will use the selected placemark's geometry as the region of interest.

```console
Select which placemark to use as the region of interest
=======================================================
[0]  <LineString 'Russia' (559.82 km^2)>  [1]  <Polygon 'Big Pentagon' (3462.31 km^2)>
[2]  <Polygon 'Taveuni' (26.13 km^2)>  [3]  <LineString 'Cherry Creek Country Club' (1.36 km^2)>
[4]  <Polygon 'Cherry Creek Star' (0.30 km^2)>  [Esc]  Exit
```

************************
<p align="center"><i>Updated 2025/06/20</i></p>
