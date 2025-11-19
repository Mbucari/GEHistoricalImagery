using LibMapCommon.Geometry;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace LibMapCommon;

public static class Extensions
{
	/// <summary> Create a GeoPolygon from the <see cref="ITile{TCoordinate}"/>'s four corners </summary>
	public static GeoPolygon<TCoordinate> GetGeoPolygon<TCoordinate>(this ITile<TCoordinate> tile)
		where TCoordinate : IGeoCoordinate<TCoordinate>
		=> new(tile.LowerLeft, tile.UpperLeft, tile.UpperRight, tile.LowerRight);

	/// <summary> Create a GDAL GeoTransform from the <see cref="ITile{TCoordinate}"/>'s properties </summary>
	public static OSGeo.GDAL.GeoTransform GetGeoTransform<TCoordinate>(this ITile<TCoordinate> tile)
		where TCoordinate : IGeoCoordinate<TCoordinate>
	{
		const int TILE_SZ = 256;
		long globalPixels = TILE_SZ * (1L << tile.Level);
		return new OSGeo.GDAL.GeoTransform
		{
			UpperLeft_X = tile.UpperLeft.X,
			UpperLeft_Y = tile.UpperLeft.Y,
			PixelWidth = TCoordinate.Equator / globalPixels,
			PixelHeight = -TCoordinate.Equator / globalPixels
		};
	}
}
