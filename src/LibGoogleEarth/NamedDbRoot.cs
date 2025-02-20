using Keyhole;
using Keyhole.Dbroot;
using LibMapCommon;

namespace LibGoogleEarth;

internal class NamedDbRoot : DbRoot
{
	public override Database Database => _database;
	private readonly Database _database;

	internal NamedDbRoot(Database database, CachedHttpClient cachedHttpClient, EncryptedDbRootProto dbRootEnc)
		: base(cachedHttpClient, dbRootEnc)
	{
		_database = database;
	}

	protected override async Task<IQuadtreePacket> GetPacketAsync(KeyholeTile tile, int epoch)
	{
		const string QP2_EXTENDED = "https://khmdb.google.com/flatfile?db={0}&qp-{1}-q.{2}";

		var url = string.Format(QP2_EXTENDED, DatabaseString(Database), tile.Path, epoch);
		byte[] compressedPacket = await DownloadBytesAsync(url);
		byte[] decompressedPacket = DecompressBuffer(compressedPacket);

		return QuadtreePacket.Parser.ParseFrom(decompressedPacket);
	}

	public static string DatabaseUrl(Database database)
	{
		var databaseString = DatabaseString(database);
		return $"https://khmdb.google.com/dbRoot.v5?db={databaseString}&hl=en&gl=us&output=proto";
	}

	private static string DatabaseString(Database database) => database switch
	{
		Database.Mars => "mars",
		Database.Moon => "moon",
		Database.Sky => "sky",
		Database.TimeMachine => "tm",
		_ => throw new ArgumentException(nameof(database))
	};
}
