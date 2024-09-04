# Availability
_Get imagery date availability in a specified region._

This command shows a diagram of image tile availablity within the specified region.
Tiles that are available from a specific date are shaded, and unavailable tiles are represented with a dot.

## Usage
```Console
GEHistoricalImagery availability --lower-left [LAT,LONG] --upper-right [LAT,LONG] --zoom [N] [--parallel [N]]

  --lower-left=LAT,LONG     Required. Geographic location
  
  --upper-right=LAT,LONG    Required. Geographic location
  
  -z N, --zoom=N            Required. Zoom level (Optional, [0-24])
  
  -p N, --parallel=N        (Default: 20) Number of concurrent downloads
```

## Example
Gets the availability diagram for the rectangular region defined by the lower-left (southwest) corner `39.619819,-104.856121` and upper-right (northeast) corner `39.638393,-104.824990`.

**Command:**
```console
GEHistoricalImagery availability --lower-left 39.619819,-104.856121 --upper-right 39.638393,-104.824990 --zoom 20
```
**Output:**
```Console
Loading Quad Tree Packets: Done!
[0]  2024/06/05  [1]  2023/09/05  [2]  2023/05/28  [3]  2023/04/29  [4]  2022/09/26
[5]  2021/08/17  [6]  2021/06/15  [7]  2021/06/11  [8]  2020/10/03  [9]  2020/09/30
[a]  2020/06/07  [b]  2019/10/03  [c]  2019/09/13  [d]  2018/06/01  [e]  2017/06/10
[f]  2017/05/14  [g]  2015/10/10  [h]  2014/10/07  [i]  2014/06/03  [j]  2013/10/07
[k]  2012/10/08  [l]  2011/05/05  [m]  2010/06/16  [Esc]  Exit
```

From here you can select different dates to display the imagery availability.

### Availability Map 1 - Imagery from 2024/06/05
This diagram, shown by pressing `0` in the console, shows the tils with available imagery from 2024/06/05. The shaded areas represent tiles which contain imagery for the selected date. The entire region is shaded, so imagery from 2024/06/05 is available for all tiles within the region.

```console
Tile availability on 2024/06/05
===============================

████████████████████████████████████████████████████████████████████████████████████████████
████████████████████████████████████████████████████████████████████████████████████████████
████████████████████████████████████████████████████████████████████████████████████████████
████████████████████████████████████████████████████████████████████████████████████████████
████████████████████████████████████████████████████████████████████████████████████████████
████████████████████████████████████████████████████████████████████████████████████████████
████████████████████████████████████████████████████████████████████████████████████████████
████████████████████████████████████████████████████████████████████████████████████████████
████████████████████████████████████████████████████████████████████████████████████████████
████████████████████████████████████████████████████████████████████████████████████████████
████████████████████████████████████████████████████████████████████████████████████████████
████████████████████████████████████████████████████████████████████████████████████████████
████████████████████████████████████████████████████████████████████████████████████████████
████████████████████████████████████████████████████████████████████████████████████████████
████████████████████████████████████████████████████████████████████████████████████████████
████████████████████████████████████████████████████████████████████████████████████████████
████████████████████████████████████████████████████████████████████████████████████████████
████████████████████████████████████████████████████████████████████████████████████████████
████████████████████████████████████████████████████████████████████████████████████████████
████████████████████████████████████████████████████████████████████████████████████████████
████████████████████████████████████████████████████████████████████████████████████████████
████████████████████████████████████████████████████████████████████████████████████████████
████████████████████████████████████████████████████████████████████████████████████████████
████████████████████████████████████████████████████████████████████████████████████████████
████████████████████████████████████████████████████████████████████████████████████████████
████████████████████████████████████████████████████████████████████████████████████████████
████████████████████████████████████████████████████████████████████████████████████████████
▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀
```
### Availability Map 2 - Imagery from 2023/04/29
This diagram, shown by pressing `3` in the console, shows the tils with available imagery from 2023/04/29. The shaded areas represent tiles which contain imagery for the selected date, and the dots represent tiles which have no imagery for the selected date. The right ~70% of this region is shaded, so only that area has imagery from 2023/04/29.

```console
Tile availability on 2023/04/29
===============================

::::::::::::::::::::::::::::████████████████████████████████████████████████████████████████
::::::::::::::::::::::::::::████████████████████████████████████████████████████████████████
::::::::::::::::::::::::::::████████████████████████████████████████████████████████████████
::::::::::::::::::::::::::::████████████████████████████████████████████████████████████████
::::::::::::::::::::::::::::████████████████████████████████████████████████████████████████
::::::::::::::::::::::::::::████████████████████████████████████████████████████████████████
::::::::::::::::::::::::::::████████████████████████████████████████████████████████████████
::::::::::::::::::::::::::::████████████████████████████████████████████████████████████████
::::::::::::::::::::::::::::████████████████████████████████████████████████████████████████
::::::::::::::::::::::::::::████████████████████████████████████████████████████████████████
::::::::::::::::::::::::::::████████████████████████████████████████████████████████████████
::::::::::::::::::::::::::::████████████████████████████████████████████████████████████████
::::::::::::::::::::::::::::████████████████████████████████████████████████████████████████
::::::::::::::::::::::::::::████████████████████████████████████████████████████████████████
::::::::::::::::::::::::::::████████████████████████████████████████████████████████████████
::::::::::::::::::::::::::::████████████████████████████████████████████████████████████████
::::::::::::::::::::::::::::████████████████████████████████████████████████████████████████
::::::::::::::::::::::::::::████████████████████████████████████████████████████████████████
::::::::::::::::::::::::::::████████████████████████████████████████████████████████████████
::::::::::::::::::::::::::::████████████████████████████████████████████████████████████████
::::::::::::::::::::::::::::████████████████████████████████████████████████████████████████
::::::::::::::::::::::::::::████████████████████████████████████████████████████████████████
::::::::::::::::::::::::::::████████████████████████████████████████████████████████████████
::::::::::::::::::::::::::::████████████████████████████████████████████████████████████████
::::::::::::::::::::::::::::████████████████████████████████████████████████████████████████
::::::::::::::::::::::::::::████████████████████████████████████████████████████████████████
::::::::::::::::::::::::::::████████████████████████████████████████████████████████████████
::::::::::::::::::::::::::::▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀
```
### Availability Map 3 - Imagery from 2021/05/17
This diagram, shown by pressing `5` in the console, shows the tils with available imagery from 2021/08/17. The shaded areas represent tiles which contain imagery for the selected date, and the dots represent tiles which have no imagery for the selected date. Only a narrow L-shaped region is shaded, so the majority of this region has no imagery from 2021/08/17.

```console
Tile availability on 2021/05/17
===============================

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::████::::::::
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::████::::::::
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::████::::::::
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::████::::::::
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::████::::::::
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::████::::::::
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::████::::::::
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::████::::::::
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::████::::::::
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::████::::::::
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::████::::::::
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::████::::::::
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::████::::::::
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::████::::::::
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::████::::::::
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::████::::::::
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::████::::::::
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::████::::::::
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::████::::::::
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::████::::::::
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::████::::::::
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::████::::::::
████████████████████████████████████████████████████████████████████████████████████::::::::
████████████████████████████████████████████████████████████████████████████████████::::::::
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
```

************************
<p align="center"><i>Updated 2024/9/4</i></p>