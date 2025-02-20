using System.Runtime.InteropServices;

namespace LibGoogleEarth.WORKINGPROGRESS;

public class TerrainMesh : IEnumerable<Face3D>
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private readonly record struct MeshVertex(int Column, int Row, double Elevation, byte PartialColumn, byte PartialRow)
	{
		private const double NumPartialsPerWhole = 16.0;

		public Coordinate3D ToCoordinate(int level)
		{
			return new Coordinate3D(Util.RowColToLatLong(level, Row + PartialRow / 16.0), Util.RowColToLatLong(level, Column + PartialColumn / 16.0), Elevation);
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct NativeMeshHeader
	{
		public const int Size = 48;
		public readonly int source_size;
		public readonly double ox;
		public readonly double oy;
		public readonly double dx;
		public readonly double dy;
		public readonly int num_points;
		public readonly int num_faces;
		public readonly int level;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public readonly record struct NativeMeshVertex(byte X, byte Y, float Z);

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	public readonly record struct NativeMeshFace(ushort A, ushort B, ushort C);

	private readonly MeshVertex[] MeshVertices;

	private readonly MeshFace[] MeshFaces;

	private const int TileSize = 32;

	private const int SizeX = 17;

	private const int SizeY = 17;

	private const double khEarthMeanRadius = 6371010.0;

	private static readonly double NegativeElevationFactor = 0.0 - Math.Pow(2.0, 32.0);

	private const double negativeElevationThreshold = 1E-12;

	private const int kNegativeElevationExponentBias = 32;

	public int Level { get; }

	public bool IsEmpty => Level == -1 && MeshVertices.Length == 0 && MeshFaces.Length == 0;

	public static TerrainMesh Empty => new TerrainMesh(-1, Array.Empty<MeshVertex>(), Array.Empty<MeshFace>());

	private TerrainMesh(int level, MeshVertex[] vertices, MeshFace[] compactFaces)
	{
		Level = level;
		MeshVertices = vertices;
		MeshFaces = compactFaces;
	}

	public List<Coordinate3D> GetVertices()
	{
		return MeshVertices.Select((mv) => mv.ToCoordinate(Level)).ToList();
	}

	public List<MeshFace> GetFacesReferencingVertices()
	{
		return MeshFaces.ToList();
	}

	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public IEnumerator<Face3D> GetEnumerator()
	{
		return MeshFaces.Select((f) => new Face3D(MeshVertices[f.A].ToCoordinate(Level), MeshVertices[f.B].ToCoordinate(Level), MeshVertices[f.C].ToCoordinate(Level))).GetEnumerator();
	}

	public static TerrainMesh Combine(TerrainMesh meshA, TerrainMesh meshB)
	{
		return Combine(new TerrainMesh[2] { meshA, meshB });
	}

	public static TerrainMesh Combine(params TerrainMesh[] meshes)
	{
		return meshes == null || meshes.Length == 0 ? Empty : Combine(meshes.AsEnumerable());
	}

	public static TerrainMesh Combine(IEnumerable<TerrainMesh> meshes)
	{
		TerrainMesh firstMesh = meshes.FirstOrDefault() ?? Empty;
		int? meshLevel = firstMesh.IsEmpty ? null : new int?(firstMesh.Level);
		int maxNumPoints = meshes.Sum((m) => m.MeshVertices.Length);
		int totalFaces = meshes.Sum((m) => m.MeshFaces.Length);
		MeshVertex[] NewVertices = GC.AllocateUninitializedArray<MeshVertex>(maxNumPoints);
		MeshFace[] NewFaces = GC.AllocateUninitializedArray<MeshFace>(totalFaces);
		int numPoints = firstMesh.MeshVertices.Length;
		int numFaces = firstMesh.MeshFaces.Length;
		firstMesh.MeshVertices.CopyTo(NewVertices, 0);
		firstMesh.MeshFaces.CopyTo(NewFaces, 0);
		foreach (TerrainMesh mesh in meshes.Skip(1))
		{
			int level = mesh.Level;
			int valueOrDefault = meshLevel.GetValueOrDefault();
			int num;
			if (!meshLevel.HasValue)
			{
				valueOrDefault = mesh.Level;
				meshLevel = valueOrDefault;
				num = valueOrDefault;
			}
			else
			{
				num = valueOrDefault;
			}
			if (level != num)
			{
				throw new ArgumentException("Cannot merge meshes of different levels.", "meshes");
			}
			int[] remap = new int[mesh.MeshVertices.Length];
			for (int i = 0; i < mesh.MeshVertices.Length; i++)
			{
				MeshVertex v = mesh.MeshVertices[i];
				int pI = IndexOfVertex(NewVertices, v, numPoints);
				if (pI == -1)
				{
					NewVertices[numPoints] = v;
					remap[i] = numPoints++;
				}
				else
				{
					remap[i] = pI;
				}
			}
			MeshFace[] meshFaces = mesh.MeshFaces;
			for (int j = 0; j < meshFaces.Length; j++)
			{
				MeshFace f = meshFaces[j];
				MeshFace newFace = new MeshFace(remap[f.A], remap[f.B], remap[f.C]);
				NewFaces[numFaces++] = newFace;
			}
		}
		if (!meshLevel.HasValue)
		{
			throw new InvalidOperationException("Unable to determine mesh level");
		}
		Array.Resize(ref NewVertices, numPoints);
		return new TerrainMesh(meshLevel.Value, NewVertices, NewFaces);
	}

	private static int IndexOfVertex(MeshVertex[] vertices, MeshVertex value, int count)
	{
		for (int i = 0; i < count; i++)
		{
			if (vertices[i].Column == value.Column && vertices[i].Row == value.Row && vertices[i].PartialRow == value.PartialRow && vertices[i].PartialColumn == value.PartialColumn)
			{
				return i;
			}
		}
		return -1;
	}

	private static void ParseSingleMesh(int col, int row, Span<byte> bytes, NativeMeshHeader header, double[] elevationGrid, Span<MeshFace> meshFaces)
	{
		int packetLevel = header.level;
		int numColsAtLevel = 1 << packetLevel;
		Span<NativeMeshVertex> vertices = MemoryMarshal.Cast<byte, NativeMeshVertex>(bytes.Slice(0, header.num_points * 6));
		Span<NativeMeshFace> faces = MemoryMarshal.Cast<byte, NativeMeshFace>(bytes.Slice(header.num_points * 6, header.num_faces * 6));
		int[] vertexRemap = new int[vertices.Length];
		for (int j = 0; j < vertices.Length; j++)
		{
			NativeMeshVertex v = vertices[j];
			double colFraction = v.X * header.dx * numColsAtLevel / 2.0;
			double rowFraction = v.Y * header.dy * numColsAtLevel / 2.0;
			int partialCol = (int)(16.0 * colFraction);
			int partialRow = (int)(16.0 * rowFraction);
			int c = partialCol + col * 16;
			int r = partialRow + row * 16;
			int tableIndex = r * 32 + c;
			elevationGrid[tableIndex] = ZtoElev(v.Z);
			vertexRemap[j] = tableIndex;
		}
		for (int i = 0; i < meshFaces.Length; i++)
		{
			meshFaces[i] = new MeshFace(vertexRemap[faces[i].A], vertexRemap[faces[i].B], vertexRemap[faces[i].C]);
		}
	}

	private static TerrainMesh ParseSingleMesh(Span<byte> bytes, NativeMeshHeader header)
	{
		int packetLevel = header.level;
		int numColsAtLevel = 1 << packetLevel;
		int ox = (int)double.Round((header.ox + 1.0) * numColsAtLevel / 2.0);
		int oy = (int)double.Round((header.oy + 1.0) * numColsAtLevel / 2.0);
		Span<NativeMeshVertex> vertices = MemoryMarshal.Cast<byte, NativeMeshVertex>(bytes.Slice(0, header.num_points * 6));
		Span<NativeMeshFace> faces = MemoryMarshal.Cast<byte, NativeMeshFace>(bytes.Slice(header.num_points * 6, header.num_faces * 6));
		MeshVertex[] points = new MeshVertex[vertices.Length];
		for (int j = 0; j < vertices.Length; j++)
		{
			NativeMeshVertex v = vertices[j];
			double colFraction = v.X * header.dx * numColsAtLevel / 2.0;
			double rowFraction = v.Y * header.dy * numColsAtLevel / 2.0;
			double partialCol = 16.0 * colFraction;
			double partialRow = 16.0 * rowFraction;
			points[j] = new MeshVertex(ox, oy, ZtoElev(v.Z), (byte)partialCol, (byte)partialRow);
		}
		MeshFace[] faces2 = new MeshFace[faces.Length];
		for (int i = 0; i < faces2.Length; i++)
		{
			faces2[i] = new MeshFace(faces[i].A, faces[i].B, faces[i].C);
		}
		return new TerrainMesh(packetLevel, points, faces2);
	}

	private static NativeMeshHeader[] ReadAllMeshHeaders(Span<byte> bytes)
	{
		int offset = 0;
		NativeMeshHeader[] nativeMeshHeaders = new NativeMeshHeader[20];
		for (int h = 0; h < nativeMeshHeaders.Length; h++)
		{
			NativeMeshHeader header = MemoryMarshal.AsRef<NativeMeshHeader>(bytes.Slice(offset, 48));
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
