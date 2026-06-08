using LibMapCommon;

namespace OSGeo.GDAL;

/// <summary>
/// Flags use by <see cref="GDAL.Gdal.OpenEx(string, uint, string[], string[], string[])"/>
/// </summary>
[Flags]
public enum GDAL_OF : uint
{
	/// <summary> Open in read-only mode.</summary>
	READONLY = 0,
	/// <summary> Allow raster and vector drivers to be used. </summary>
	ALL = 0,
	/// <summary> Open in update mode. </summary>
	UPDATE = 1,
	/// <summary> Allow raster drivers to be used. </summary>
	RASTER = 2,
	/// <summary> Allow vector drivers to be used. </summary>
	VECTOR = 4,
	/// <summary> Allow gnm drivers to be used. </summary>
	GNM = 8,
	/// <summary> Allow multidimensional raster drivers to be used. </summary>
	MULTIDIM_RASTER = 0x10,
	/// <summary> Open in shared mode. </summary>
	SHARED = 0x20,
	/// <summary> Emit error message in case of failed open. </summary>
	VERBOSE_ERROR = 0x40,
	/// <summary>Open as internal dataset.<para/>
	/// Such dataset isn't registered in the global list of opened dataset. Cannot be used with <see cref="SHARED"/>.
	/// </summary>
	INTERNAL = 0x80,
	/// <summary> Let GDAL decide if a array-based or hashset-based storage strategy for cached blocks must be used.<para/>
	/// <see cref="DEFAULT_BLOCK_ACCESS"/>, <see cref="ARRAY_BLOCK_ACCESS"/> and <see cref="HASHSET_BLOCK_ACCESS"/> are mutually exclusive. </summary>
	DEFAULT_BLOCK_ACCESS = 0,
	/// <summary> Use a array-based storage strategy for cached blocks.<para/>
	/// <see cref="DEFAULT_BLOCK_ACCESS"/>, <see cref="ARRAY_BLOCK_ACCESS"/> and <see cref="HASHSET_BLOCK_ACCESS"/> are mutually exclusive. </summary>
	ARRAY_BLOCK_ACCESS = 0x100,
	/// <summary> Use a hashset-based storage strategy for cached blocks.<para/>
	/// <see cref="DEFAULT_BLOCK_ACCESS"/>, <see cref="ARRAY_BLOCK_ACCESS"/> and <see cref="HASHSET_BLOCK_ACCESS"/> are mutually exclusive. </summary>
	HASHSET_BLOCK_ACCESS = 0x200,
	/// <summary>
	/// Open in thread-safe mode.<para/>
	/// Not compatible with <see cref="VECTOR"/>, <see cref="MULTIDIM_RASTER"/> or <see cref="UPDATE"/></summary>
	THREAD_SAFE = 0x800
}

public static class GdalExtensions
{
	public static GeoTransform GetGeoTransform(this Dataset dataset)
	{
		var geoTransform = new GeoTransform();
		dataset.GetGeoTransform(geoTransform.Transformation);
		return geoTransform;
	}

	public static void SetGeoTransform(this Dataset dataset, GeoTransform transform)
	{
		dataset.SetGeoTransform(transform.Transformation);
	}

	/// <summary> Create a GDAL GeoTransform from the <see cref="ITile{TCoordinate}"/>'s properties </summary>
	public static GeoTransform GetGeoTransform<TCoordinate>(this ITile<TCoordinate> tile)
		where TCoordinate : IGeoCoordinate<TCoordinate>
	{
		const int TILE_SZ = 256;
		long globalPixels = TILE_SZ * (1L << tile.Level);
		return new GeoTransform
		{
			UpperLeft_X = tile.UpperLeft.X,
			UpperLeft_Y = tile.UpperLeft.Y,
			PixelWidth = TCoordinate.Equator / globalPixels,
			PixelHeight = -TCoordinate.Equator / globalPixels
		};
	}
}
