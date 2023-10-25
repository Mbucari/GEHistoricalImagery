# GEHistoricalImagery
GEHistoricalImagery is a utility for downloading historical aerial imagery from Google Earth.

**Features**
- Find historical imagery availability at any location and zoom level
- Always uses the most recent Google Earth data
- Automatically substitutes unavailable tiles with temporally closest available tile
- Outputs a georeferenced GeoTiff
- Supports warping to new coordinate systems
- Fast! Parallel downloading and local caching

**Commands**
|Command|Description|
|-|-|
|[info](#info)|Get imagery info at a specified location.|
|[availability](#availability)|Get imagery date availability in a specified region.|
|[download](#download)|Download historical imagery.|

## Info
_Get imagery info at a specified location._

This command prints out all arial imagery dates at a specified location.
### Usage
```Console
GEHistoricalImagery info --location [LAT,LONG] [--zoom [N]]

  -l LAT,LONG, --location=LAT,LONG    Required. Geographic location
  
  -z N, --zoom=N                      Zoom level (Optional, [0-24])
```
### Example
1. Get imagery dates at 39.6305750,-104.8412990 for zoom level 21.

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
       date = 2020/10/03, version = 276
   ```
2. Get imagery dates at 39.6305750,-104.8412990 for all zoom levels.

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
       date = 1934/10/08, version = 124
       date = 1937/12/31, version = 124
       date = 1938/12/31, version = 124
       date = 1939/12/31, version = 124
       date = 1940/12/31, version = 124
       date = 1943/12/31, version = 124
       date = 1944/12/31, version = 124
       date = 1945/12/31, version = 124
       date = 1948/09/26, version = 124
       date = 1950/12/31, version = 124
       date = 1953/12/31, version = 124
       date = 1954/12/31, version = 124
       date = 1955/12/31, version = 124
       date = 1956/12/31, version = 124
       date = 1957/12/31, version = 124
       date = 1960/12/31, version = 124
       date = 1961/12/31, version = 124
       date = 1962/12/31, version = 124
       date = 1965/12/31, version = 124
       date = 1969/12/31, version = 124
       date = 1970/12/31, version = 124
       date = 1972/12/31, version = 124
       date = 1973/11/25, version = 124
       date = 1974/09/05, version = 124
       date = 1975/03/25, version = 124
       date = 1978/12/31, version = 124
       date = 1979/12/31, version = 124
       date = 1982/07/09, version = 124
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
     Level = 3, Path = 0301
       date = 1937/12/31, version = 92
       date = 1938/12/31, version = 92
       date = 1939/12/31, version = 92
       date = 1940/12/31, version = 92
       date = 1943/12/31, version = 92
       date = 1944/12/31, version = 92
       date = 1946/07/29, version = 92
       date = 1948/09/26, version = 92
       date = 1950/12/31, version = 92
       date = 1953/12/31, version = 92
       date = 1954/12/31, version = 92
       date = 1955/12/31, version = 92
       date = 1956/12/31, version = 92
       date = 1957/12/31, version = 92
       date = 1961/12/31, version = 92
       date = 1962/12/31, version = 92
       date = 1965/12/31, version = 92
       date = 1968/10/12, version = 92
       date = 1969/12/31, version = 92
       date = 1970/12/31, version = 103
       date = 1974/09/05, version = 103
       date = 1977/06/03, version = 103
       date = 1978/12/31, version = 103
       date = 1979/12/31, version = 103
       date = 1982/07/09, version = 103
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
     Level = 4, Path = 03012
       date = 1931/05/27, version = 113
       date = 1937/12/31, version = 113
       date = 1938/12/31, version = 113
       date = 1939/12/31, version = 113
       date = 1940/12/31, version = 113
       date = 1943/12/31, version = 113
       date = 1944/12/31, version = 113
       date = 1946/07/29, version = 113
       date = 1948/09/26, version = 113
       date = 1949/05/01, version = 115
       date = 1950/12/31, version = 115
       date = 1953/12/31, version = 115
       date = 1954/12/31, version = 115
       date = 1955/12/31, version = 115
       date = 1956/12/31, version = 115
       date = 1957/12/31, version = 115
       date = 1961/12/31, version = 115
       date = 1962/12/31, version = 115
       date = 1965/12/31, version = 115
       date = 1968/10/12, version = 115
       date = 1969/12/31, version = 115
       date = 1970/12/31, version = 115
       date = 1971/05/01, version = 115
       date = 1974/09/05, version = 115
       date = 1975/01/01, version = 115
       date = 1977/06/03, version = 115
       date = 1978/12/31, version = 115
       date = 1979/12/31, version = 115
       date = 1980/02/21, version = 115
       date = 1982/07/09, version = 115
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
       date = 2020/10/03, version = 276
       date = 2021/06/11, version = 277
       date = 2021/06/15, version = 276
       date = 2021/08/17, version = 278
       date = 2022/09/26, version = 307
       date = 2023/04/29, version = 318
       date = 2023/05/28, version = 320
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
       date = 2020/10/03, version = 276
       date = 2021/06/11, version = 277
       date = 2021/06/15, version = 276
       date = 2021/08/17, version = 278
       date = 2023/04/29, version = 318
       date = 2023/05/28, version = 320
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
       date = 2020/10/03, version = 276
       date = 2021/06/11, version = 277
       date = 2021/06/15, version = 276
       date = 2021/08/17, version = 278
       date = 2023/04/29, version = 318
       date = 2023/05/28, version = 320
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
       date = 2020/10/03, version = 276
       date = 2021/06/11, version = 277
       date = 2021/06/15, version = 276
       date = 2023/04/29, version = 318
       date = 2023/05/28, version = 320
     Level = 21, Path = 0301232010121332030111
       date = 2017/05/14, version = 233
       date = 2019/10/03, version = 276
       date = 2020/10/03, version = 276
     Level = 22, Path = 03012320101213320301113
       NO AVAILABLE IMAGERY
     ```
   </details>
## Availability
_Get imagery date availability in a specified region._

This command shows a diagram of image tile availablity within the specified region.
Tiles that are available from a specific date are shaded, and unavailable tiles are represented with a dot.

### Usage
```Console
GEHistoricalImagery availability --lower-left [LAT,LONG] --upper-right [LAT,LONG] --zoom [N] [--parallel [N]]

  --lower-left=LAT,LONG     Required. Geographic location
  
  --upper-right=LAT,LONG    Required. Geographic location
  
  -z N, --zoom=N            Required. Zoom level (Optional, [0-24])
  
  -p N, --parallel=N        (Default: 20) Number of concurrent downloads
```

### Example
Gets the availability diagram for the rectangular region defined by the lower-left (southwest) corner 39.619819,-104.856121 and upper-right (northeast) corner 39.638393,-104.824990.

**Command:**
```console
GEHistoricalImagery availability --lower-left 39.619819,-104.856121 --upper-right 39.638393,-104.824990 --zoom 20
```
**Output:**
```Console
Loading Quad Tree Packets: Done!
[0]  2023/05/28  [1]  2023/04/29  [2]  2022/09/26  [3]  2021/08/17  [4]  2021/06/15
[5]  2021/06/11  [6]  2020/10/03  [7]  2020/09/30  [8]  2020/06/07  [9]  2019/10/03
[a]  2019/09/13  [b]  2018/06/01  [c]  2017/06/10  [d]  2017/05/14  [e]  2015/10/10
[f]  2014/10/07  [g]  2014/06/03  [h]  2013/10/07  [i]  2012/10/08  [j]  2011/05/05
[k]  2010/06/16
```

From here you can select different dates to display the imagery availability.

<details>
  <summary>Expand to see imagery availability for 2023/04/29</summary>
  This diagram is shown by pressing '1' in the console.
  
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
</details>
<details>
  <summary>Expand to see imagery availability for 2021/08/17</summary>
  This diagram is shown by pressing '3' in the console.

  ```console
  Tile availability on 2021/08/17
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
</details>

## Download
_Download historical imagery._

This command will download historical imagery from within a region on a specified date and save it as a single GeoTiff file. You may optionally specify an output spatial reference to warp the image.
If imagery is not available for the specified date, the downloader will use the image from the next nearest date.

### Usage
```Console
 GEHistoricalImagery download --lower-left [LAT,LONG] --upper-right [LAT,LONG] -z [N] -d [yyyy/mm/dd] -o [PATH] [--target-sr "SPATIAL REFERENCE"]] [-p [N]]

  --lower-left=LAT,LONG                   Required. Geographic location

  --upper-right=LAT,LONG                  Required. Geographic location

  -z N, --zoom=N                          Required. Zoom level [0-24]

  -d 10/23/2023, --date=10/23/2023        Required. Imagery Date

  -o out.tif, --output=out.tif            Required. Output GeoTiff save location

  -p N, --parallel=N                      (Default: ALL_CPUS) Number of concurrent downloads

  --target-sr=https://epsg.io/1234.wkt    Warp image to Spatial Reference

  --scale=S                               (Default: 1) Geo transform scale factor

  --offset-x=X                            (Default: 0) Geo transform X offset (post-scaling)

  --offset-y=Y                            (Default: 0) Geo transform Y offset (post-scaling)
```

### Example
Download historical imagery at zoom level 20 from within the region defined by the lower-left (southwest) corner 39.619819,-104.856121 and upper-right (northeast) corner 39.638393,-104.824990. Transform the image to SPCS Colorado Central - Feet.

_NOTE: Example images are not actual output files. Actual files from this region at this zoom level are ~150MB._
1. Get imagery from 2023/04/29

   **Command:**
   ```Console
   GEHistoricalImagery download --lower-left 39.619819,-104.856121 --upper-right 39.638393,-104.824990 --zoom 20 --date 2023/04/29 --target-sr https://epsg.io/103248.wkt --output ".\Cherry Creek.tif"
   ```
   **Output:**
   ![Cherry_Creek_1](https://github.com/Mbucari/GEHistoricalImagery/assets/37587114/ed978e2b-ea28-4983-9080-e9e0f7b458bb)

2. Get imagery from 2021/08/17

   **Command:**
   ```Console
   GEHistoricalImagery download --lower-left 39.619819,-104.856121 --upper-right 39.638393,-104.824990 --zoom 20 --date 2021/08/17 --target-sr https://epsg.io/103248.wkt --output ".\Cherry Creek.tif"
   ```
   **Output:**
   ![Cherry_Creek_2](https://github.com/Mbucari/GEHistoricalImagery/assets/37587114/c1b767cb-b0e4-442f-bf16-a072d42a29f3)



   
