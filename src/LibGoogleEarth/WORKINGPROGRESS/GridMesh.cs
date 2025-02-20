using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LibGoogleEarth.WORKINGPROGRESS;

public class GridMesh : IEnumerable<Face3D>
{
	private const int TileSize = 32;

	private const int SizeX = 17;

	private const int SizeY = 17;

	private const double khEarthMeanRadius = 6371010.0;

	private static readonly double NegativeElevationFactor = 0.0 - Math.Pow(2.0, 32.0);

	private const double negativeElevationThreshold = 1E-12;

	private const int kNegativeElevationExponentBias = 32;

	public int Level { get; }

	public int OriginColumn { get; }

	public int OriginRow { get; }

	public double?[] Elevations { get; }

	public MeshFace[] Faces { get; }

	public GridMesh(int level, int llColumn, int llRow, double?[] elevations, MeshFace[] faces)
	{
		Level = level;
		OriginColumn = llColumn;
		OriginRow = llRow;
		Elevations = elevations;
		Faces = faces;
	}

	public IEnumerator<Face3D> GetEnumerator()
	{
		int numPoints = Elevations.Count((e) => e.HasValue);
		int[] remap = new int[Elevations.Length];
		Coordinate3D[] coords = new Coordinate3D[numPoints];
		int coordNum = 0;
		double elev = default;
		for (int j = 0; j < Elevations.Length; j++)
		{
			double? num = Elevations[j];
			int num2;
			if (num.HasValue)
			{
				elev = num.GetValueOrDefault();
				num2 = 1;
			}
			else
			{
				num2 = 0;
			}
			if (num2 != 0)
			{
				int row = j / 33;
				int col = j % 33;
				double lon = Util.RowColToLatLong(Level, OriginColumn + col / 32.0);
				double lat = Util.RowColToLatLong(Level, OriginRow + row / 32.0);
				coords[coordNum] = new Coordinate3D(lat, lon, elev);
				remap[j] = coordNum++;
			}
		}
		for (int i = 0; i < Faces.Length; i++)
		{
			MeshFace f = Faces[i];
			yield return new Face3D(coords[remap[f.A]], coords[remap[f.B]], coords[remap[f.C]]);
		}
	}


	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public static GridMesh[] ParseAllMeshes(Span<byte> bytes)
	{
		TerrainMesh.NativeMeshHeader[] headers = ReadAllMeshHeaders(bytes);
		int offset = 48;
		TerrainMesh.NativeMeshHeader[] quad = new TerrainMesh.NativeMeshHeader[4];
		GridMesh[] meshes = new GridMesh[5];
		for (int i = 0; i < 5; i++)
		{
			int start = i * 4;
			if (headers.Length < start + 4)
			{
				Array.Resize(ref meshes, i);
				break;
			}
			Array.Copy(headers, start, quad, 0, quad.Length);
			offset = ParseMeshPackage(offset, bytes, quad, out meshes[i]);
		}
		return meshes;
	}

	private static int ParseMeshPackage(int offset, Span<byte> bytes, TerrainMesh.NativeMeshHeader[] headers, out GridMesh mesh)
	{
		Debug.Assert(headers.Length == 4);
		double?[] elevations = new double?[1089];
		MeshFace[] faces = new MeshFace[headers.Sum((h) => h.num_faces)];
		Span<MeshFace> facesSpan = faces;
		int packetLevel = headers[0].level;
		int numColsAtLevel = 1 << packetLevel;
		int ox = (int)double.Round((headers[0].ox + 1.0) * numColsAtLevel / 4.0);
		int oy = (int)double.Round((headers[0].oy + 1.0) * numColsAtLevel / 4.0);
		mesh = new GridMesh(packetLevel - 1, ox, oy, elevations, faces);
		int faceStart = 0;
		for (int q = 0; q < 4; q++)
		{
			int dataSize = headers[q].source_size - 48 + 4;
			int r = q / 2;
			int c = (q + r) % 2;
			ParseSingleMesh(c, r, bytes.Slice(offset, dataSize), headers[q], elevations, facesSpan.Slice(faceStart, headers[q].num_faces));
			faceStart += headers[q].num_faces;
			offset += dataSize + 48;
		}
		int notnull = elevations.Count((e) => e.HasValue);
		int numVerts = headers.Sum((h) => h.num_points);
		return offset;
	}

	private static void ParseSingleMesh(int col, int row, Span<byte> bytes, TerrainMesh.NativeMeshHeader header, double?[] elevationGrid, Span<MeshFace> meshFaces)
	{
		int packetLevel = header.level;
		int numColsAtLevel = 1 << packetLevel;
		Span<TerrainMesh.NativeMeshVertex> vertices = MemoryMarshal.Cast<byte, TerrainMesh.NativeMeshVertex>(bytes.Slice(0, header.num_points * 6));
		Span<TerrainMesh.NativeMeshFace> faces = MemoryMarshal.Cast<byte, TerrainMesh.NativeMeshFace>(bytes.Slice(header.num_points * 6, header.num_faces * 6));
		int[] vertexRemap = new int[vertices.Length];
		for (int j = 0; j < vertices.Length; j++)
		{
			TerrainMesh.NativeMeshVertex v = vertices[j];
			double colFraction = v.X * header.dx * numColsAtLevel / 2.0;
			double rowFraction = v.Y * header.dy * numColsAtLevel / 2.0;
			int partialCol = (int)(16.0 * colFraction);
			int partialRow = (int)(16.0 * rowFraction);
			int c = partialCol + col * 16;
			int r = partialRow + row * 16;
			int tableIndex = r * 33 + c;
			double ele = ZtoElev(v.Z);
			if (elevationGrid[tableIndex].HasValue)
			{
				bool same = elevationGrid[tableIndex] == ele;
			}
			elevationGrid[tableIndex] = ele;
			vertexRemap[j] = tableIndex;
		}
		for (int i = 0; i < meshFaces.Length; i++)
		{
			meshFaces[i] = new MeshFace(vertexRemap[faces[i].A], vertexRemap[faces[i].B], vertexRemap[faces[i].C]);
		}
	}

	private static TerrainMesh.NativeMeshHeader[] ReadAllMeshHeaders(Span<byte> bytes)
	{
		int offset = 0;
		TerrainMesh.NativeMeshHeader[] nativeMeshHeaders = new TerrainMesh.NativeMeshHeader[20];
		for (int h = 0; h < nativeMeshHeaders.Length; h++)
		{
			TerrainMesh.NativeMeshHeader header = MemoryMarshal.AsRef<TerrainMesh.NativeMeshHeader>(bytes.Slice(offset, 48));
			if (header.source_size == 0)
			{
				Array.Resize(ref nativeMeshHeaders, h);
				return nativeMeshHeaders;
			}
			nativeMeshHeaders[h] = header;
			offset += header.source_size + 4;
		}
		return nativeMeshHeaders;
	}

	private static double ZtoElev(float z)
	{
		double tmp_z = z;
		if (tmp_z != 0.0 && tmp_z < 1E-12)
		{
			tmp_z *= NegativeElevationFactor;
		}
		return tmp_z * 6371010.0;
	}
}

public readonly record struct Coordinate3D(double Latitude, double Longitude, double Elevation);

public readonly record struct Face3D(Coordinate3D A, Coordinate3D B, Coordinate3D C);

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct MeshFace(int A, int B, int C);