# Info
_Get imagery info at a specified location._

This command prints out all arial imagery dates at a specified location.

## Options

### `--location <LAT>,<LONG>` (`-l <LAT>,<LONG>`)
Required. Geographic location to query (WGS 84).

### `--zoom <N>` (`-z <N>`)
Required. The zoom level at which imagery is downloaded. Valid values are [1,23], although practically, Google Earth caps out at 21 and Wayback caps out at 20). [Read about zoom levels](https://developers.arcgis.com/documentation/mapping-and-location-services/reference/zoom-levels-and-scale/). Cannot be used with the `--min-zoom` or `--max-zoom` options.

### `--min-zoom <N>`
Optional. The minimum zoom level in a range to query. Cannot be used with the `--zoom` option.

### `--max-zoom <N>`
Optional. The maximum zoom level in a range to query. Cannot be used with the `--zoom` option.

### `--output=<info.json>` (`-o <info.json>`)
Optional. File path to save the info data as JSON. Use `-o -` to write the JSON to the console's standard output.

### `--parallel <N>` (`-p <N>`)
Optional. The number of concurrent downloads and image processing threads. This number is capped to 10 when using `--provider=Wayback` because I determined empirically that any higher number resulted in a reduced speed. Default is `ALL_CPUS`

### `--provider <Provider>`
Optional. The aerial imagery provider to query. Options are:
- `TM`: Google Earth time machine
- `Wayback`: Esri Wayback provider.

Default is `TM`.

### `--no-cache`
Optional. Disables caching of imagery and metadata, causing fresh API calls to be required on every run.

**Notes on the Cache Directory**

App data is cached in a directory named `GEHI_cache`, inside the app's directory or in the system's temp directory if the app has no write access to its directory. This location can be changed with an environment variable: `GEHistoricalImagery_Cache`.
### `-q`
Optional. Quiet mode. Nothing written to stderr.

## Example 1 - Get imagery into at a location for a single zoom level
**Command:**
```Console
GEHistoricalImagery info --location 39.630575,-104.841299 --zoom 21
```
**Output:**
```Console
Dated Imagery at 39.6305750,-104.8412990
 Level = 21, Path = 0301232010121332030111
   date = 2017/05/14, version = 233
   date = 2019/10/03, version = 276
   date = 2020/10/03, version = 909
```
## Example 2 - Get imagery into at a location for all zoom levels

**Command:**
```Console
GEHistoricalImagery info --location 39.630575,-104.841299
```
**Output:**
<details>
 <summary>Expand to see the full command output</summary>

 ```Console
 Dated Imagery at 39.6305750,-104.8412990
  Level = 0, Path = 0
    date = 1934/10/08, version = 124
    date = 1937/12/31, version = 124
    date = 1939/12/31, version = 124
    date = 1940/12/31, version = 124
    date = 1943/12/31, version = 124
    date = 1944/12/31, version = 124
    date = 1945/12/31, version = 124
    date = 1948/09/26, version = 124
    date = 1950/12/31, version = 124
    date = 1953/05/03, version = 124
    date = 1955/12/31, version = 124
    date = 1956/12/31, version = 124
    date = 1960/12/31, version = 124
    date = 1961/12/31, version = 124
    date = 1962/12/31, version = 124
    date = 1963/12/31, version = 124
    date = 1965/12/31, version = 124
    date = 1969/12/31, version = 124
    date = 1970/12/31, version = 124
    date = 1972/12/31, version = 124
    date = 1973/12/31, version = 124
    date = 1974/09/05, version = 124
    date = 1975/12/12, version = 124
    date = 1976/04/20, version = 124
    date = 1978/12/31, version = 124
    date = 1979/12/31, version = 124
    date = 1980/07/20, version = 124
    date = 1982/07/09, version = 124
    date = 1983/12/14, version = 124
    date = 1984/12/31, version = 159
    date = 1985/12/31, version = 159
    date = 1986/12/31, version = 159
    date = 1987/12/31, version = 159
    date = 1988/12/31, version = 159
    date = 1989/12/31, version = 159
    date = 1990/12/31, version = 159
    date = 1991/12/31, version = 159
    date = 1992/12/31, version = 159
    date = 1993/12/31, version = 159
    date = 1994/12/31, version = 159
    date = 1995/12/31, version = 159
    date = 1996/12/31, version = 159
    date = 1997/12/31, version = 159
    date = 1998/12/31, version = 159
    date = 1999/12/31, version = 159
    date = 2000/12/31, version = 159
    date = 2001/12/31, version = 159
    date = 2002/12/31, version = 159
    date = 2003/12/31, version = 159
    date = 2004/12/31, version = 159
    date = 2005/12/31, version = 159
    date = 2006/12/31, version = 159
    date = 2007/12/31, version = 159
    date = 2008/12/31, version = 159
    date = 2009/12/31, version = 159
    date = 2010/12/31, version = 159
    date = 2011/12/31, version = 159
    date = 2012/12/31, version = 159
    date = 2013/12/31, version = 159
    date = 2014/12/31, version = 159
    date = 2015/12/31, version = 159
    date = 2016/12/31, version = 159
    date = 2017/12/31, version = 273
    date = 2018/12/31, version = 273
    date = 2019/12/31, version = 273
    date = 2020/12/31, version = 273
  Level = 1, Path = 03
    date = 1934/10/08, version = 124
    date = 1937/12/31, version = 124
    date = 1939/12/31, version = 124
    date = 1940/12/31, version = 124
    date = 1943/12/31, version = 124
    date = 1944/12/31, version = 124
    date = 1945/12/31, version = 124
    date = 1948/09/26, version = 124
    date = 1950/12/31, version = 124
    date = 1953/05/03, version = 124
    date = 1955/12/31, version = 124
    date = 1956/12/31, version = 124
    date = 1960/12/31, version = 124
    date = 1961/12/31, version = 124
    date = 1962/12/31, version = 124
    date = 1963/12/31, version = 124
    date = 1965/12/31, version = 124
    date = 1969/12/31, version = 124
    date = 1970/12/31, version = 124
    date = 1972/12/31, version = 124
    date = 1973/12/31, version = 124
    date = 1974/09/05, version = 124
    date = 1975/12/12, version = 124
    date = 1976/04/20, version = 124
    date = 1978/12/31, version = 124
    date = 1979/12/31, version = 124
    date = 1980/07/20, version = 124
    date = 1982/07/09, version = 124
    date = 1983/12/14, version = 124
    date = 1984/12/31, version = 159
    date = 1985/12/31, version = 159
    date = 1986/12/31, version = 159
    date = 1987/12/31, version = 159
    date = 1988/12/31, version = 159
    date = 1989/12/31, version = 159
    date = 1990/12/31, version = 159
    date = 1991/12/31, version = 159
    date = 1992/12/31, version = 159
    date = 1993/12/31, version = 159
    date = 1994/12/31, version = 159
    date = 1995/12/31, version = 159
    date = 1996/12/31, version = 159
    date = 1997/12/31, version = 159
    date = 1998/12/31, version = 159
    date = 1999/12/31, version = 159
    date = 2000/12/31, version = 159
    date = 2001/12/31, version = 159
    date = 2002/12/31, version = 159
    date = 2003/12/31, version = 159
    date = 2004/12/31, version = 159
    date = 2005/12/31, version = 159
    date = 2006/12/31, version = 159
    date = 2007/12/31, version = 159
    date = 2008/12/31, version = 159
    date = 2009/12/31, version = 159
    date = 2010/12/31, version = 159
    date = 2011/12/31, version = 159
    date = 2012/12/31, version = 159
    date = 2013/12/31, version = 159
    date = 2014/12/31, version = 159
    date = 2015/12/31, version = 159
    date = 2016/12/31, version = 159
    date = 2017/12/31, version = 273
    date = 2018/12/31, version = 273
    date = 2019/12/31, version = 273
    date = 2020/12/31, version = 273
  Level = 2, Path = 030
    date = 1931/12/31, version = 334
    date = 1934/10/08, version = 334
    date = 1937/12/31, version = 334
    date = 1938/12/31, version = 334
    date = 1939/12/31, version = 334
    date = 1940/12/31, version = 334
    date = 1943/12/31, version = 334
    date = 1944/12/31, version = 334
    date = 1945/12/31, version = 334
    date = 1948/09/26, version = 334
    date = 1949/12/31, version = 334
    date = 1950/12/31, version = 334
    date = 1953/12/31, version = 334
    date = 1954/12/31, version = 334
    date = 1955/12/31, version = 334
    date = 1956/12/31, version = 334
    date = 1957/12/31, version = 334
    date = 1960/12/31, version = 334
    date = 1961/12/31, version = 334
    date = 1962/12/31, version = 334
    date = 1965/12/31, version = 334
    date = 1969/12/31, version = 334
    date = 1970/12/31, version = 334
    date = 1971/12/31, version = 334
    date = 1972/12/31, version = 334
    date = 1973/11/25, version = 334
    date = 1974/09/05, version = 334
    date = 1975/03/25, version = 334
    date = 1978/12/31, version = 334
    date = 1979/12/31, version = 334
    date = 1980/12/31, version = 334
    date = 1982/07/09, version = 334
    date = 1984/12/31, version = 334
    date = 1985/12/31, version = 334
    date = 1986/12/31, version = 334
    date = 1987/12/31, version = 334
    date = 1988/12/31, version = 334
    date = 1989/12/31, version = 334
    date = 1990/12/31, version = 334
    date = 1991/12/31, version = 334
    date = 1992/12/31, version = 334
    date = 1993/12/31, version = 334
    date = 1994/12/31, version = 334
    date = 1995/12/31, version = 334
    date = 1996/12/31, version = 334
    date = 1997/12/31, version = 334
    date = 1998/12/31, version = 334
    date = 1999/12/31, version = 334
    date = 2000/12/31, version = 334
    date = 2001/12/31, version = 334
    date = 2002/12/31, version = 334
    date = 2003/12/31, version = 334
    date = 2004/12/31, version = 334
    date = 2005/12/31, version = 334
    date = 2006/12/31, version = 334
    date = 2007/12/31, version = 334
    date = 2008/12/31, version = 334
    date = 2009/12/31, version = 334
    date = 2010/12/31, version = 334
    date = 2011/12/31, version = 334
    date = 2012/12/31, version = 334
    date = 2013/12/31, version = 334
    date = 2014/12/31, version = 334
    date = 2015/12/31, version = 334
    date = 2016/12/31, version = 334
    date = 2017/12/31, version = 334
    date = 2018/12/31, version = 334
    date = 2019/12/31, version = 334
    date = 2020/12/31, version = 334
  Level = 3, Path = 0301
    date = 1931/12/31, version = 334
    date = 1937/12/31, version = 334
    date = 1938/12/31, version = 334
    date = 1939/12/31, version = 334
    date = 1940/12/31, version = 334
    date = 1943/12/31, version = 334
    date = 1944/12/31, version = 334
    date = 1946/07/29, version = 334
    date = 1948/09/26, version = 334
    date = 1949/12/31, version = 334
    date = 1950/12/31, version = 344
    date = 1953/12/31, version = 344
    date = 1954/12/31, version = 344
    date = 1955/12/31, version = 344
    date = 1956/12/31, version = 344
    date = 1957/12/31, version = 344
    date = 1961/12/31, version = 344
    date = 1962/12/31, version = 344
    date = 1965/12/31, version = 344
    date = 1966/12/31, version = 344
    date = 1968/10/12, version = 344
    date = 1969/12/31, version = 344
    date = 1970/12/31, version = 344
    date = 1971/12/31, version = 344
    date = 1974/09/05, version = 344
    date = 1977/06/03, version = 344
    date = 1978/12/31, version = 344
    date = 1979/12/31, version = 344
    date = 1980/12/31, version = 346
    date = 1982/07/09, version = 346
    date = 1984/12/31, version = 346
    date = 1985/12/31, version = 346
    date = 1986/12/31, version = 346
    date = 1987/12/31, version = 346
    date = 1988/12/31, version = 346
    date = 1989/12/31, version = 346
    date = 1990/12/31, version = 346
    date = 1991/12/31, version = 346
    date = 1992/12/31, version = 346
    date = 1993/12/31, version = 346
    date = 1994/12/31, version = 346
    date = 1995/12/31, version = 346
    date = 1996/12/31, version = 346
    date = 1997/12/31, version = 346
    date = 1998/12/31, version = 346
    date = 1999/12/31, version = 346
    date = 2000/12/31, version = 346
    date = 2001/12/31, version = 346
    date = 2002/12/31, version = 346
    date = 2003/12/31, version = 346
    date = 2004/12/31, version = 346
    date = 2005/12/31, version = 346
    date = 2006/12/31, version = 346
    date = 2007/12/31, version = 346
    date = 2008/12/31, version = 346
    date = 2009/12/31, version = 346
    date = 2010/12/31, version = 346
    date = 2011/12/31, version = 346
    date = 2012/12/31, version = 346
    date = 2013/12/31, version = 346
    date = 2014/12/31, version = 346
    date = 2015/12/31, version = 346
    date = 2016/12/31, version = 346
    date = 2017/12/31, version = 346
    date = 2018/12/31, version = 346
    date = 2019/12/31, version = 346
    date = 2020/12/31, version = 346
  Level = 4, Path = 03012
    date = 1931/12/31, version = 334
    date = 1937/12/31, version = 334
    date = 1938/12/31, version = 334
    date = 1939/12/31, version = 334
    date = 1940/12/31, version = 334
    date = 1943/12/31, version = 334
    date = 1944/12/31, version = 334
    date = 1946/07/29, version = 334
    date = 1948/09/26, version = 334
    date = 1949/12/31, version = 334
    date = 1950/12/31, version = 344
    date = 1953/12/31, version = 344
    date = 1954/12/31, version = 344
    date = 1955/12/31, version = 344
    date = 1956/12/31, version = 344
    date = 1957/12/31, version = 344
    date = 1961/12/31, version = 344
    date = 1962/12/31, version = 344
    date = 1965/12/31, version = 344
    date = 1966/12/31, version = 344
    date = 1968/10/12, version = 344
    date = 1969/12/31, version = 344
    date = 1970/12/31, version = 344
    date = 1971/12/31, version = 344
    date = 1974/09/05, version = 344
    date = 1975/01/01, version = 344
    date = 1976/12/31, version = 344
    date = 1977/06/03, version = 344
    date = 1978/12/31, version = 344
    date = 1979/12/31, version = 344
    date = 1980/12/31, version = 346
    date = 1982/07/09, version = 346
    date = 1984/12/31, version = 159
    date = 1985/12/31, version = 157
    date = 1986/12/31, version = 157
    date = 1987/12/31, version = 157
    date = 1988/12/31, version = 157
    date = 1989/12/31, version = 157
    date = 1990/12/31, version = 157
    date = 1991/12/31, version = 157
    date = 1992/12/31, version = 157
    date = 1993/12/31, version = 157
    date = 1994/12/31, version = 157
    date = 1995/12/31, version = 157
    date = 1996/12/31, version = 157
    date = 1997/12/31, version = 157
    date = 1998/12/31, version = 157
    date = 1999/12/31, version = 157
    date = 2000/12/31, version = 157
    date = 2001/12/31, version = 157
    date = 2002/12/31, version = 157
    date = 2003/12/31, version = 157
    date = 2004/12/31, version = 157
    date = 2005/12/31, version = 157
    date = 2006/12/31, version = 157
    date = 2007/12/31, version = 157
    date = 2008/12/31, version = 157
    date = 2009/12/31, version = 157
    date = 2010/12/31, version = 157
    date = 2011/12/31, version = 157
    date = 2012/12/31, version = 157
    date = 2013/12/31, version = 157
    date = 2014/12/31, version = 157
    date = 2015/12/31, version = 157
    date = 2016/12/31, version = 157
    date = 2017/12/31, version = 273
    date = 2018/12/31, version = 273
    date = 2019/12/31, version = 273
    date = 2020/12/31, version = 273
  Level = 5, Path = 030123
    date = 1937/12/31, version = 78
    date = 1938/12/31, version = 78
    date = 1943/12/31, version = 78
    date = 1944/12/31, version = 78
    date = 1950/12/31, version = 78
    date = 1953/12/31, version = 78
    date = 1954/12/31, version = 78
    date = 1955/12/31, version = 78
    date = 1956/12/31, version = 78
    date = 1957/12/31, version = 78
    date = 1961/12/31, version = 78
    date = 1962/12/31, version = 78
    date = 1965/12/31, version = 78
    date = 1968/10/12, version = 78
    date = 1969/12/31, version = 78
    date = 1970/12/31, version = 103
    date = 1974/09/05, version = 103
    date = 1977/06/26, version = 103
    date = 1978/12/31, version = 103
    date = 1979/12/31, version = 103
    date = 1980/02/21, version = 103
    date = 1982/07/09, version = 103
    date = 1984/12/31, version = 159
    date = 1985/12/31, version = 157
    date = 1986/12/31, version = 157
    date = 1987/12/31, version = 157
    date = 1988/12/31, version = 157
    date = 1989/12/31, version = 157
    date = 1990/12/31, version = 157
    date = 1991/12/31, version = 157
    date = 1992/12/31, version = 157
    date = 1993/12/31, version = 157
    date = 1994/12/31, version = 157
    date = 1995/12/31, version = 157
    date = 1996/12/31, version = 157
    date = 1997/12/31, version = 157
    date = 1998/12/31, version = 157
    date = 1999/12/31, version = 157
    date = 2000/12/31, version = 157
    date = 2001/12/31, version = 157
    date = 2002/12/31, version = 157
    date = 2003/12/31, version = 157
    date = 2004/12/31, version = 157
    date = 2005/12/31, version = 157
    date = 2006/12/31, version = 157
    date = 2007/12/31, version = 157
    date = 2008/12/31, version = 157
    date = 2009/12/31, version = 157
    date = 2010/12/31, version = 157
    date = 2011/12/31, version = 157
    date = 2012/12/31, version = 157
    date = 2013/12/31, version = 157
    date = 2014/12/31, version = 157
    date = 2015/12/31, version = 157
    date = 2016/12/31, version = 157
    date = 2017/12/31, version = 273
    date = 2018/12/31, version = 273
    date = 2019/12/31, version = 273
    date = 2020/12/31, version = 273
  Level = 6, Path = 0301232
    date = 1937/12/31, version = 35
    date = 1955/12/31, version = 35
    date = 1956/12/31, version = 35
    date = 1970/02/12, version = 35
    date = 1977/06/26, version = 35
    date = 1984/12/31, version = 159
    date = 1985/12/31, version = 157
    date = 1986/12/31, version = 157
    date = 1987/12/31, version = 157
    date = 1988/12/31, version = 157
    date = 1989/12/31, version = 157
    date = 1990/12/31, version = 157
    date = 1991/12/31, version = 157
    date = 1992/12/31, version = 157
    date = 1993/12/31, version = 157
    date = 1994/12/31, version = 157
    date = 1995/12/31, version = 157
    date = 1996/12/31, version = 157
    date = 1997/12/31, version = 157
    date = 1998/12/31, version = 157
    date = 1999/12/31, version = 157
    date = 2000/12/31, version = 157
    date = 2001/12/31, version = 157
    date = 2002/12/31, version = 157
    date = 2003/12/31, version = 157
    date = 2004/12/31, version = 157
    date = 2005/12/31, version = 157
    date = 2006/12/31, version = 157
    date = 2007/12/31, version = 157
    date = 2008/12/31, version = 157
    date = 2009/12/31, version = 157
    date = 2010/12/31, version = 157
    date = 2011/12/31, version = 157
    date = 2012/12/31, version = 157
    date = 2013/12/31, version = 157
    date = 2014/12/31, version = 157
    date = 2015/12/31, version = 157
    date = 2016/12/31, version = 157
    date = 2017/12/31, version = 273
    date = 2018/12/31, version = 273
    date = 2019/12/31, version = 273
    date = 2020/12/31, version = 273
  Level = 7, Path = 03012320
    date = 1937/12/31, version = 35
    date = 1955/12/31, version = 35
    date = 1956/12/31, version = 35
    date = 1984/12/31, version = 159
    date = 1985/12/31, version = 157
    date = 1986/12/31, version = 157
    date = 1987/12/31, version = 157
    date = 1988/12/31, version = 157
    date = 1989/12/31, version = 157
    date = 1990/12/31, version = 157
    date = 1991/12/31, version = 157
    date = 1992/12/31, version = 157
    date = 1993/12/31, version = 157
    date = 1994/12/31, version = 157
    date = 1995/12/31, version = 157
    date = 1996/12/31, version = 157
    date = 1997/12/31, version = 157
    date = 1998/12/31, version = 157
    date = 1999/12/31, version = 157
    date = 2000/12/31, version = 157
    date = 2001/12/31, version = 157
    date = 2002/12/31, version = 157
    date = 2003/12/31, version = 157
    date = 2004/12/31, version = 157
    date = 2005/12/31, version = 157
    date = 2006/12/31, version = 157
    date = 2007/12/31, version = 157
    date = 2008/12/31, version = 157
    date = 2009/12/31, version = 157
    date = 2010/12/31, version = 157
    date = 2011/12/31, version = 157
    date = 2012/12/31, version = 157
    date = 2013/12/31, version = 157
    date = 2014/12/31, version = 157
    date = 2015/12/31, version = 157
    date = 2016/12/31, version = 157
    date = 2017/12/31, version = 273
    date = 2018/12/31, version = 273
    date = 2019/12/31, version = 273
    date = 2020/12/31, version = 273
  Level = 8, Path = 030123201
    date = 1937/12/31, version = 35
    date = 1955/12/31, version = 35
    date = 1956/12/31, version = 35
    date = 1984/12/31, version = 159
    date = 1985/12/31, version = 157
    date = 1986/12/31, version = 157
    date = 1987/12/31, version = 157
    date = 1988/12/31, version = 157
    date = 1989/12/31, version = 157
    date = 1990/12/31, version = 157
    date = 1991/12/31, version = 157
    date = 1992/12/31, version = 157
    date = 1993/12/31, version = 157
    date = 1994/12/31, version = 157
    date = 1995/12/31, version = 157
    date = 1996/12/31, version = 157
    date = 1997/12/31, version = 157
    date = 1998/12/31, version = 157
    date = 1999/12/31, version = 157
    date = 2000/12/31, version = 157
    date = 2001/12/31, version = 157
    date = 2002/12/31, version = 157
    date = 2003/12/31, version = 157
    date = 2004/12/31, version = 157
    date = 2005/12/31, version = 157
    date = 2006/12/31, version = 157
    date = 2007/12/31, version = 157
    date = 2008/12/31, version = 157
    date = 2009/12/31, version = 157
    date = 2010/12/31, version = 157
    date = 2011/12/31, version = 157
    date = 2012/12/31, version = 157
    date = 2013/12/31, version = 157
    date = 2014/12/31, version = 157
    date = 2015/12/31, version = 157
    date = 2016/12/31, version = 157
    date = 2017/12/31, version = 273
    date = 2018/12/31, version = 273
    date = 2019/12/31, version = 273
    date = 2020/12/31, version = 273
  Level = 9, Path = 0301232010
    date = 1937/12/31, version = 35
    date = 1955/12/31, version = 35
    date = 1956/12/31, version = 35
    date = 1984/12/31, version = 159
    date = 1985/12/31, version = 157
    date = 1986/12/31, version = 157
    date = 1987/12/31, version = 157
    date = 1988/12/31, version = 157
    date = 1989/12/31, version = 157
    date = 1990/12/31, version = 157
    date = 1991/12/31, version = 157
    date = 1992/12/31, version = 157
    date = 1993/12/31, version = 157
    date = 1994/12/31, version = 157
    date = 1995/12/31, version = 157
    date = 1996/12/31, version = 157
    date = 1997/12/31, version = 157
    date = 1998/12/31, version = 157
    date = 1999/12/31, version = 157
    date = 2000/12/31, version = 157
    date = 2001/12/31, version = 157
    date = 2002/12/31, version = 157
    date = 2003/12/31, version = 157
    date = 2004/12/31, version = 157
    date = 2005/12/31, version = 157
    date = 2006/12/31, version = 157
    date = 2007/12/31, version = 157
    date = 2008/12/31, version = 157
    date = 2009/12/31, version = 157
    date = 2010/12/31, version = 157
    date = 2011/12/31, version = 157
    date = 2012/12/31, version = 157
    date = 2013/12/31, version = 157
    date = 2014/12/31, version = 157
    date = 2015/12/31, version = 157
    date = 2016/12/31, version = 157
    date = 2017/12/31, version = 273
    date = 2018/12/31, version = 273
    date = 2019/12/31, version = 273
    date = 2020/12/31, version = 273
  Level = 10, Path = 03012320101
    date = 1937/12/31, version = 35
    date = 1955/12/31, version = 35
    date = 1956/12/31, version = 35
    date = 1984/12/31, version = 159
    date = 1985/12/31, version = 157
    date = 1986/12/31, version = 157
    date = 1987/12/31, version = 157
    date = 1988/12/31, version = 157
    date = 1989/12/31, version = 157
    date = 1990/12/31, version = 157
    date = 1991/12/31, version = 157
    date = 1992/12/31, version = 157
    date = 1993/12/31, version = 157
    date = 1994/12/31, version = 157
    date = 1995/12/31, version = 157
    date = 1996/12/31, version = 157
    date = 1997/12/31, version = 157
    date = 1998/12/31, version = 157
    date = 1999/12/31, version = 157
    date = 2000/12/31, version = 157
    date = 2001/12/31, version = 157
    date = 2002/12/31, version = 157
    date = 2003/12/31, version = 157
    date = 2004/12/31, version = 157
    date = 2005/12/31, version = 157
    date = 2006/12/31, version = 157
    date = 2007/12/31, version = 157
    date = 2008/12/31, version = 157
    date = 2009/12/31, version = 157
    date = 2010/12/31, version = 157
    date = 2011/12/31, version = 157
    date = 2012/12/31, version = 157
    date = 2013/12/31, version = 157
    date = 2014/12/31, version = 157
    date = 2015/12/31, version = 157
    date = 2016/12/31, version = 157
    date = 2017/12/31, version = 273
    date = 2018/12/31, version = 273
    date = 2019/12/31, version = 273
    date = 2020/12/31, version = 273
  Level = 11, Path = 030123201012
    date = 1937/12/31, version = 35
    date = 1955/12/31, version = 35
    date = 1956/12/31, version = 35
    date = 1984/12/31, version = 159
    date = 1985/12/31, version = 157
    date = 1986/12/31, version = 157
    date = 1987/12/31, version = 157
    date = 1988/12/31, version = 157
    date = 1989/12/31, version = 157
    date = 1990/12/31, version = 157
    date = 1991/12/31, version = 157
    date = 1992/12/31, version = 157
    date = 1993/12/31, version = 157
    date = 1994/12/31, version = 157
    date = 1995/12/31, version = 157
    date = 1996/12/31, version = 157
    date = 1997/12/31, version = 157
    date = 1998/12/31, version = 157
    date = 1999/12/31, version = 157
    date = 2000/12/31, version = 157
    date = 2001/12/31, version = 157
    date = 2002/12/31, version = 157
    date = 2003/12/31, version = 157
    date = 2004/12/31, version = 157
    date = 2005/12/31, version = 157
    date = 2006/12/31, version = 157
    date = 2007/12/31, version = 157
    date = 2008/12/31, version = 157
    date = 2009/12/31, version = 157
    date = 2010/12/31, version = 157
    date = 2011/12/31, version = 157
    date = 2012/12/31, version = 157
    date = 2013/12/31, version = 157
    date = 2014/12/31, version = 157
    date = 2015/12/31, version = 157
    date = 2016/12/31, version = 157
    date = 2017/12/31, version = 273
    date = 2018/12/31, version = 273
    date = 2019/12/31, version = 273
    date = 2020/12/31, version = 273
  Level = 12, Path = 0301232010121
    date = 1937/12/31, version = 35
    date = 1955/12/31, version = 35
    date = 1984/12/31, version = 159
    date = 1985/12/31, version = 157
    date = 1986/12/31, version = 157
    date = 1987/12/31, version = 157
    date = 1988/12/31, version = 157
    date = 1989/12/31, version = 157
    date = 1990/12/31, version = 157
    date = 1991/12/31, version = 157
    date = 1992/12/31, version = 157
    date = 1993/12/31, version = 157
    date = 1994/12/31, version = 157
    date = 1995/12/31, version = 157
    date = 1996/12/31, version = 157
    date = 1997/12/31, version = 157
    date = 1998/12/31, version = 157
    date = 1999/12/31, version = 157
    date = 2000/12/31, version = 157
    date = 2001/12/31, version = 157
    date = 2002/12/31, version = 157
    date = 2003/12/31, version = 157
    date = 2004/12/31, version = 157
    date = 2005/12/31, version = 157
    date = 2006/12/31, version = 157
    date = 2007/12/31, version = 157
    date = 2008/12/31, version = 157
    date = 2009/12/31, version = 157
    date = 2010/12/31, version = 157
    date = 2011/12/31, version = 157
    date = 2012/12/31, version = 157
    date = 2013/12/31, version = 157
    date = 2014/12/31, version = 157
    date = 2015/12/31, version = 157
    date = 2016/12/31, version = 157
    date = 2017/12/31, version = 273
    date = 2018/12/31, version = 273
    date = 2019/12/31, version = 273
    date = 2020/12/31, version = 273
  Level = 13, Path = 03012320101213
    date = 1937/12/31, version = 35
    date = 1955/12/31, version = 35
    date = 1984/12/31, version = 159
    date = 1985/12/31, version = 157
    date = 1986/12/31, version = 157
    date = 1987/12/31, version = 157
    date = 1988/12/31, version = 157
    date = 1989/12/31, version = 157
    date = 1990/12/31, version = 157
    date = 1991/12/31, version = 157
    date = 1992/12/31, version = 157
    date = 1993/12/31, version = 157
    date = 1994/12/31, version = 157
    date = 1995/12/31, version = 157
    date = 1996/12/31, version = 157
    date = 1997/12/31, version = 157
    date = 1998/12/31, version = 157
    date = 1999/12/31, version = 157
    date = 2000/12/31, version = 157
    date = 2001/12/31, version = 157
    date = 2002/12/31, version = 157
    date = 2003/12/31, version = 157
    date = 2004/12/31, version = 157
    date = 2005/12/31, version = 157
    date = 2006/12/31, version = 157
    date = 2007/12/31, version = 157
    date = 2008/12/31, version = 157
    date = 2009/12/31, version = 157
    date = 2010/12/31, version = 157
    date = 2011/12/31, version = 157
    date = 2012/12/31, version = 157
    date = 2013/12/31, version = 157
    date = 2014/12/31, version = 157
    date = 2015/12/31, version = 157
    date = 2016/12/31, version = 157
    date = 2017/12/31, version = 273
    date = 2018/12/31, version = 273
    date = 2019/12/31, version = 273
    date = 2020/12/31, version = 273
  Level = 14, Path = 030123201012133
    date = 1937/12/31, version = 35
    date = 1955/12/31, version = 35
    date = 1985/12/31, version = 272
    date = 1993/06/26, version = 272
    date = 1993/06/27, version = 272
    date = 1994/09/23, version = 272
    date = 1999/09/30, version = 272
    date = 2002/09/09, version = 272
    date = 2002/12/31, version = 17
    date = 2003/05/13, version = 158
    date = 2004/04/16, version = 158
    date = 2004/09/02, version = 158
    date = 2004/09/03, version = 80
    date = 2004/12/31, version = 80
    date = 2005/06/22, version = 158
    date = 2005/07/23, version = 158
    date = 2005/10/07, version = 19
    date = 2005/10/23, version = 158
    date = 2006/03/27, version = 63
    date = 2006/04/30, version = 19
    date = 2006/05/24, version = 158
    date = 2006/08/31, version = 158
    date = 2007/03/11, version = 17
    date = 2007/07/31, version = 44
    date = 2008/03/31, version = 158
    date = 2010/06/16, version = 64
    date = 2011/05/05, version = 76
    date = 2011/10/28, version = 80
    date = 2012/10/08, version = 98
    date = 2013/10/07, version = 123
    date = 2014/06/03, version = 119
    date = 2014/10/07, version = 123
    date = 2015/10/10, version = 138
    date = 2017/05/14, version = 212
    date = 2017/06/10, version = 198
    date = 2018/06/01, version = 228
    date = 2019/09/13, version = 262
    date = 2019/10/03, version = 272
    date = 2020/06/07, version = 270
    date = 2020/09/30, version = 273
    date = 2020/10/03, version = 276
    date = 2021/06/11, version = 277
    date = 2021/06/15, version = 276
    date = 2021/08/17, version = 278
    date = 2021/08/22, version = 278
    date = 2022/09/26, version = 307
    date = 2023/04/24, version = 318
    date = 2023/04/29, version = 318
    date = 2023/05/17, version = 318
    date = 2023/05/28, version = 320
    date = 2023/05/30, version = 320
    date = 2023/09/05, version = 344
    date = 2024/06/05, version = 345
  Level = 15, Path = 0301232010121332
    date = 1937/12/31, version = 35
    date = 1955/12/31, version = 35
    date = 1993/06/27, version = 272
    date = 1999/09/30, version = 272
    date = 2002/09/09, version = 158
    date = 2002/12/31, version = 17
    date = 2003/05/13, version = 158
    date = 2004/04/16, version = 158
    date = 2004/09/02, version = 158
    date = 2004/09/03, version = 80
    date = 2004/12/31, version = 80
    date = 2005/07/23, version = 158
    date = 2005/10/07, version = 19
    date = 2005/10/23, version = 158
    date = 2006/03/27, version = 63
    date = 2006/04/30, version = 19
    date = 2006/05/24, version = 158
    date = 2006/08/31, version = 158
    date = 2007/03/11, version = 17
    date = 2007/07/31, version = 44
    date = 2008/03/31, version = 158
    date = 2010/06/16, version = 64
    date = 2011/05/05, version = 76
    date = 2011/10/28, version = 80
    date = 2012/10/08, version = 98
    date = 2013/10/07, version = 123
    date = 2014/06/03, version = 119
    date = 2014/10/07, version = 123
    date = 2015/10/10, version = 138
    date = 2017/05/14, version = 212
    date = 2017/06/10, version = 198
    date = 2018/06/01, version = 228
    date = 2019/09/13, version = 262
    date = 2019/10/03, version = 272
    date = 2020/06/07, version = 270
    date = 2020/09/30, version = 273
    date = 2020/10/03, version = 276
    date = 2021/06/11, version = 277
    date = 2021/06/15, version = 276
    date = 2021/08/17, version = 278
    date = 2021/08/22, version = 278
    date = 2022/09/26, version = 307
    date = 2023/04/29, version = 318
    date = 2023/05/28, version = 320
    date = 2023/09/05, version = 344
    date = 2024/06/05, version = 345
  Level = 16, Path = 03012320101213320
    date = 1937/12/31, version = 35
    date = 1993/06/27, version = 5
    date = 1999/09/30, version = 5
    date = 2002/09/09, version = 158
    date = 2002/12/31, version = 17
    date = 2003/05/13, version = 158
    date = 2004/04/16, version = 17
    date = 2004/09/02, version = 158
    date = 2004/09/03, version = 80
    date = 2004/12/31, version = 17
    date = 2005/07/23, version = 158
    date = 2005/10/07, version = 19
    date = 2005/10/23, version = 158
    date = 2006/03/27, version = 63
    date = 2006/04/30, version = 19
    date = 2006/05/24, version = 158
    date = 2007/03/11, version = 17
    date = 2007/07/31, version = 44
    date = 2008/03/31, version = 158
    date = 2010/06/16, version = 64
    date = 2011/05/05, version = 76
    date = 2011/10/28, version = 80
    date = 2012/10/08, version = 98
    date = 2013/10/07, version = 123
    date = 2014/06/03, version = 119
    date = 2014/10/07, version = 123
    date = 2015/10/10, version = 138
    date = 2017/05/14, version = 212
    date = 2017/06/10, version = 198
    date = 2018/06/01, version = 228
    date = 2019/09/13, version = 262
    date = 2019/10/03, version = 272
    date = 2020/06/07, version = 270
    date = 2020/09/30, version = 273
    date = 2020/10/03, version = 276
    date = 2021/06/11, version = 277
    date = 2021/06/15, version = 276
    date = 2021/08/17, version = 278
    date = 2022/09/26, version = 307
    date = 2023/04/29, version = 318
    date = 2023/05/28, version = 320
    date = 2024/06/05, version = 345
  Level = 17, Path = 030123201012133203
    date = 1937/12/31, version = 35
    date = 1993/06/27, version = 5
    date = 1999/09/30, version = 5
    date = 2002/09/09, version = 158
    date = 2002/12/31, version = 17
    date = 2003/05/13, version = 158
    date = 2004/04/16, version = 17
    date = 2004/09/02, version = 158
    date = 2004/12/31, version = 17
    date = 2005/07/23, version = 158
    date = 2005/10/07, version = 19
    date = 2005/10/23, version = 158
    date = 2006/03/27, version = 63
    date = 2006/04/30, version = 19
    date = 2006/05/24, version = 158
    date = 2007/03/11, version = 17
    date = 2007/07/31, version = 44
    date = 2008/03/31, version = 158
    date = 2010/06/16, version = 64
    date = 2011/05/05, version = 98
    date = 2011/10/28, version = 80
    date = 2012/10/08, version = 112
    date = 2013/10/07, version = 123
    date = 2014/06/03, version = 119
    date = 2014/10/07, version = 139
    date = 2015/10/10, version = 179
    date = 2017/05/14, version = 233
    date = 2017/06/10, version = 212
    date = 2018/06/01, version = 263
    date = 2019/09/13, version = 273
    date = 2019/10/03, version = 276
    date = 2020/06/07, version = 270
    date = 2020/09/30, version = 273
    date = 2020/10/03, version = 909
    date = 2021/06/11, version = 277
    date = 2021/06/15, version = 276
    date = 2021/08/17, version = 278
    date = 2022/09/26, version = 307
    date = 2023/04/29, version = 318
    date = 2023/05/28, version = 320
    date = 2024/06/05, version = 345
  Level = 18, Path = 0301232010121332030
    date = 1993/06/27, version = 5
    date = 1999/09/30, version = 5
    date = 2002/09/09, version = 158
    date = 2002/12/31, version = 17
    date = 2003/05/13, version = 158
    date = 2004/04/16, version = 17
    date = 2004/09/02, version = 158
    date = 2004/12/31, version = 17
    date = 2005/07/23, version = 158
    date = 2005/10/07, version = 19
    date = 2005/10/23, version = 158
    date = 2006/04/30, version = 19
    date = 2006/05/24, version = 158
    date = 2007/03/11, version = 17
    date = 2007/07/31, version = 44
    date = 2008/03/31, version = 158
    date = 2010/06/16, version = 64
    date = 2011/05/05, version = 98
    date = 2012/10/08, version = 112
    date = 2013/10/07, version = 123
    date = 2014/06/03, version = 119
    date = 2014/10/07, version = 139
    date = 2015/10/10, version = 179
    date = 2017/05/14, version = 233
    date = 2017/06/10, version = 212
    date = 2018/06/01, version = 263
    date = 2019/09/13, version = 273
    date = 2019/10/03, version = 276
    date = 2020/06/07, version = 270
    date = 2020/09/30, version = 273
    date = 2020/10/03, version = 909
    date = 2021/06/11, version = 277
    date = 2021/06/15, version = 276
    date = 2021/08/17, version = 278
    date = 2023/04/29, version = 318
    date = 2023/05/28, version = 320
    date = 2024/06/05, version = 345
  Level = 19, Path = 03012320101213320301
    date = 2002/12/31, version = 17
    date = 2004/12/31, version = 17
    date = 2006/04/30, version = 19
    date = 2007/07/31, version = 44
    date = 2010/06/16, version = 64
    date = 2011/05/05, version = 98
    date = 2012/10/08, version = 112
    date = 2013/10/07, version = 123
    date = 2014/06/03, version = 119
    date = 2014/10/07, version = 139
    date = 2015/10/10, version = 179
    date = 2017/05/14, version = 233
    date = 2017/06/10, version = 212
    date = 2018/06/01, version = 263
    date = 2019/09/13, version = 273
    date = 2019/10/03, version = 276
    date = 2020/06/07, version = 270
    date = 2020/09/30, version = 273
    date = 2020/10/03, version = 909
    date = 2021/06/11, version = 277
    date = 2021/06/15, version = 276
    date = 2021/08/17, version = 278
    date = 2023/04/29, version = 318
    date = 2023/05/28, version = 320
    date = 2024/06/05, version = 345
  Level = 20, Path = 030123201012133203011
    date = 2010/06/16, version = 64
    date = 2011/05/05, version = 98
    date = 2012/10/08, version = 112
    date = 2013/10/07, version = 123
    date = 2014/06/03, version = 119
    date = 2014/10/07, version = 139
    date = 2015/10/10, version = 179
    date = 2017/05/14, version = 233
    date = 2017/06/10, version = 212
    date = 2018/06/01, version = 263
    date = 2019/09/13, version = 273
    date = 2019/10/03, version = 276
    date = 2020/06/07, version = 270
    date = 2020/09/30, version = 273
    date = 2020/10/03, version = 909
    date = 2021/06/11, version = 277
    date = 2021/06/15, version = 276
    date = 2023/04/29, version = 318
    date = 2023/05/28, version = 320
    date = 2024/06/05, version = 345
  Level = 21, Path = 0301232010121332030111
    date = 2017/05/14, version = 233
    date = 2019/10/03, version = 276
    date = 2020/10/03, version = 909
  Level = 22, Path = 03012320101213320301113
    NO AVAILABLE IMAGERY
  ```
</details>

## Example 3 - Get imagery into at a location and export to JSON
Use the `-q` option to silence stderr, and specify `-` for the output file to write the JSON to stdout.
**Command:**
```Console
GEHistoricalImagery info --location 39.630575,-104.841299 --min-zoom 19 --max-zoom 21 -q -o -
```
**Output:**

<details>
 <summary>Expand to see the full command output</summary>

  ```JSON
  {
    "latitude": 39.630575,
    "longitude": -104.841299,
    "web_mercator_x": -11670880.02,
    "web_mercator_y": 4812402.78,
    "provider": "TM",
    "level_infos": [
      {
        "zoom_level": 19,
        "column": 109457,
        "row": 319860,
        "quadtree_path": "03012320101213320301",
        "tile_infos": [
          {
            "provider": "Image © 2025 Airbus",
            "epoch": 366,
            "imagery_date": "2025-11-02"
          },
          {
            "provider": "Image © 2025 Airbus",
            "epoch": 366,
            "imagery_date": "2025-10-28"
          },
          {
            "provider": "Image © 2025 Airbus",
            "epoch": 361,
            "imagery_date": "2025-06-07"
          },
          {
            "provider": "Image © 2025 Airbus",
            "epoch": 363,
            "imagery_date": "2025-03-03"
          },
          {
            "provider": "Image © 2025 Airbus",
            "epoch": 362,
            "imagery_date": "2025-02-05"
          },
          {
            "provider": "Image © 2025 Airbus",
            "epoch": 349,
            "imagery_date": "2024-06-05"
          },
          {
            "epoch": 1019,
            "imagery_date": "2023-10-20"
          },
          {
            "provider": "Image © 2025 Airbus",
            "epoch": 320,
            "imagery_date": "2023-05-28"
          },
          {
            "provider": "Image © 2025 Airbus",
            "epoch": 318,
            "imagery_date": "2023-04-29"
          },
          {
            "provider": "Image © 2025 CNES / Airbus",
            "epoch": 278,
            "imagery_date": "2021-08-17"
          },
          {
            "provider": "Image © 2025 Maxar Technologies",
            "epoch": 276,
            "imagery_date": "2021-06-15"
          },
          {
            "epoch": 366,
            "imagery_date": "2021-06-11"
          },
          {
            "epoch": 357,
            "imagery_date": "2020-10-03"
          },
          {
            "epoch": 366,
            "imagery_date": "2020-09-30"
          },
          {
            "provider": "Image © 2025 Maxar Technologies",
            "epoch": 270,
            "imagery_date": "2020-06-07"
          },
          {
            "epoch": 276,
            "imagery_date": "2019-10-03"
          },
          {
            "epoch": 366,
            "imagery_date": "2019-09-13"
          },
          {
            "epoch": 366,
            "imagery_date": "2018-06-01"
          },
          {
            "epoch": 212,
            "imagery_date": "2017-06-10"
          },
          {
            "epoch": 233,
            "imagery_date": "2017-05-14"
          },
          {
            "epoch": 179,
            "imagery_date": "2015-10-10"
          },
          {
            "epoch": 139,
            "imagery_date": "2014-10-07"
          },
          {
            "epoch": 119,
            "imagery_date": "2014-06-03"
          },
          {
            "epoch": 123,
            "imagery_date": "2013-10-07"
          },
          {
            "epoch": 112,
            "imagery_date": "2012-10-08"
          },
          {
            "epoch": 98,
            "imagery_date": "2011-05-05"
          },
          {
            "epoch": 64,
            "imagery_date": "2010-06-16"
          },
          {
            "epoch": 44,
            "imagery_date": "2007-07-31"
          },
          {
            "provider": "Image U.S. Geological Survey",
            "epoch": 19,
            "imagery_date": "2006-04-30"
          },
          {
            "provider": "Image © 2025 Sanborn",
            "epoch": 17,
            "imagery_date": "2004-12-31"
          },
          {
            "provider": "Image U.S. Geological Survey",
            "epoch": 17,
            "imagery_date": "2002-12-31"
          }
        ]
      },
      {
        "zoom_level": 20,
        "column": 218915,
        "row": 639720,
        "quadtree_path": "030123201012133203011",
        "tile_infos": [
          {
            "provider": "Image © 2025 Airbus",
            "epoch": 366,
            "imagery_date": "2025-11-02"
          },
          {
            "provider": "Image © 2025 Airbus",
            "epoch": 366,
            "imagery_date": "2025-10-28"
          },
          {
            "provider": "Image © 2025 Airbus",
            "epoch": 361,
            "imagery_date": "2025-06-07"
          },
          {
            "provider": "Image © 2025 Airbus",
            "epoch": 363,
            "imagery_date": "2025-03-03"
          },
          {
            "provider": "Image © 2025 Airbus",
            "epoch": 362,
            "imagery_date": "2025-02-05"
          },
          {
            "provider": "Image © 2025 Airbus",
            "epoch": 349,
            "imagery_date": "2024-06-05"
          },
          {
            "epoch": 1019,
            "imagery_date": "2023-10-20"
          },
          {
            "provider": "Image © 2025 Airbus",
            "epoch": 320,
            "imagery_date": "2023-05-28"
          },
          {
            "provider": "Image © 2025 Airbus",
            "epoch": 318,
            "imagery_date": "2023-04-29"
          },
          {
            "provider": "Image © 2025 Maxar Technologies",
            "epoch": 276,
            "imagery_date": "2021-06-15"
          },
          {
            "epoch": 366,
            "imagery_date": "2021-06-11"
          },
          {
            "epoch": 357,
            "imagery_date": "2020-10-03"
          },
          {
            "epoch": 366,
            "imagery_date": "2020-09-30"
          },
          {
            "provider": "Image © 2025 Maxar Technologies",
            "epoch": 270,
            "imagery_date": "2020-06-07"
          },
          {
            "epoch": 276,
            "imagery_date": "2019-10-03"
          },
          {
            "epoch": 366,
            "imagery_date": "2019-09-13"
          },
          {
            "epoch": 366,
            "imagery_date": "2018-06-01"
          },
          {
            "epoch": 212,
            "imagery_date": "2017-06-10"
          },
          {
            "epoch": 233,
            "imagery_date": "2017-05-14"
          },
          {
            "epoch": 179,
            "imagery_date": "2015-10-10"
          },
          {
            "epoch": 139,
            "imagery_date": "2014-10-07"
          },
          {
            "epoch": 119,
            "imagery_date": "2014-06-03"
          },
          {
            "epoch": 123,
            "imagery_date": "2013-10-07"
          },
          {
            "epoch": 112,
            "imagery_date": "2012-10-08"
          },
          {
            "epoch": 98,
            "imagery_date": "2011-05-05"
          },
          {
            "epoch": 64,
            "imagery_date": "2010-06-16"
          }
        ]
      },
      {
        "zoom_level": 21,
        "column": 437831,
        "row": 1279440,
        "quadtree_path": "0301232010121332030111",
        "tile_infos": [
          {
            "epoch": 1019,
            "imagery_date": "2023-10-20"
          },
          {
            "epoch": 357,
            "imagery_date": "2020-10-03"
          },
          {
            "epoch": 276,
            "imagery_date": "2019-10-03"
          },
          {
            "epoch": 233,
            "imagery_date": "2017-05-14"
          }
        ]
      }
    ]
  }
  ```
</details>

************************
<p align="center"><i>Updated 2026/06/10</i></p>
