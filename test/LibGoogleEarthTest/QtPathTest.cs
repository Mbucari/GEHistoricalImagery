using LibGoogleEarth;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace LibGoogleEarthTest;

[TestClass]
public class QtPathTest
{
	#region Pre-Computed Values
	private static readonly ReadOnlyDictionary<string, int> SubIndexDict;
	private static readonly ReadOnlyDictionary<string, int> RootIndexDict;
	#endregion

	static QtPathTest()
	{
		var projectDir = Path.Combine(".", "..", "..", "..");
		var rootIndicesPath = Path.Combine(projectDir, "RootIndexDictionary.json");
		var subIndicesPath = Path.Combine(projectDir, "SubIndexDictionary.json");
		RootIndexDict = JsonSerializer.Deserialize<Dictionary<string, int>>(File.OpenRead(rootIndicesPath))!.AsReadOnly();
		SubIndexDict = JsonSerializer.Deserialize<Dictionary<string, int>>(File.OpenRead(subIndicesPath))!.AsReadOnly();
	}

	[DataTestMethod]
	[DataRow("0000")]
	[DataRow("00000000")]
	[DataRow("000000000000")]
	[DataRow("0000000000000000")]
	[DataRow("00000000000000000000")]
	[DataRow("000000000000000000000000")]
	public void SubIndices(string qtIndex)
	{

		for (int i = 0; i < 4; i++)
		{
			var iStr = i.ToString();
			var qtPath = qtIndex + iStr;
			var p = new KeyholeTile(qtPath);
			Assert.AreEqual(SubIndexDict[iStr], p.SubIndex);
			Assert.AreEqual(qtPath, p.Path);
			for (int j = 0; j < 4; j++)
			{
				var jStr = iStr + j;
				qtPath = qtIndex + jStr;
				p = new KeyholeTile(qtPath);
				Assert.AreEqual(SubIndexDict[jStr], p.SubIndex);
				Assert.AreEqual(qtPath, p.Path);
				for (int k = 0; k < 4; k++)
				{
					var kStr = jStr + k;
					qtPath = qtIndex + kStr;
					p = new KeyholeTile(qtPath);
					Assert.AreEqual(SubIndexDict[kStr], p.SubIndex);
					Assert.AreEqual(qtPath, p.Path);
					for (int l = 0; l < 4; l++)
					{
						var lStr = kStr + l;
						qtPath = qtIndex + lStr;
						p = new KeyholeTile(qtPath);
						Assert.AreEqual(SubIndexDict[lStr], p.SubIndex);
						Assert.AreEqual(qtPath, p.Path);
					}
				}
			}
		}
	}


	[TestMethod]
	public void RootIndex()
	{
		for (int i = 0; i < 4; i++)
		{
			var iStr = "0" + i.ToString();
			var p = new KeyholeTile(iStr);
			Assert.AreEqual(RootIndexDict[iStr], p.SubIndex);
			Assert.AreEqual(iStr, p.Path);
			for (int j = 0; j < 4; j++)
			{
				var jStr = iStr + j;
				p = new KeyholeTile(jStr);
				Assert.AreEqual(RootIndexDict[jStr], p.SubIndex);
				Assert.AreEqual(jStr, p.Path);
				for (int k = 0; k < 4; k++)
				{
					var kStr = jStr + k;
					p = new KeyholeTile(kStr);
					Assert.AreEqual(RootIndexDict[kStr], p.SubIndex);
					Assert.AreEqual(kStr, p.Path);
				}
			}
		}
	}

	[TestMethod]
	public void RootIndex2()
	{
		var p = new KeyholeTile("0");
		Assert.IsTrue(p.IsRoot);
		Assert.AreEqual(0, p.SubIndex);
		Assert.AreEqual(p.Level, 0);
		Assert.AreEqual("0", p.Path);
	}

	[DataTestMethod]
	[DataRow("1")]
	[DataRow("01234")]
	[DataRow("012334")]
	[DataRow("0000134")]
	[DataRow("00001304")]
	[DataRow("10001304")]
	[DataRow(" 02322")]
	[DataRow("")]
	public void BadPaths(string quadTreePath)
	{
		Assert.ThrowsException<ArgumentException>(() => new KeyholeTile(quadTreePath));
	}

	[TestMethod]
	public void NullPath()
		=> Assert.ThrowsException<ArgumentNullException>(() => new KeyholeTile(null!));

	[TestMethod]
	public void EnumerateIndices()
	{
		for (int level = 0; level <= KeyholeTile.MaxLevel; level++)
		{
			var qtp = RandomQuadTreePath(level + 1);

			var p = new KeyholeTile(qtp);
			Assert.AreEqual(qtp, p.Path);
			Assert.AreEqual(level, p.Level);

			int index = 0;
			foreach (var qtpIndex in p.Indices)
			{
				var expected = qtp.Substring(0, index += 4);
				Assert.AreEqual(expected, qtpIndex.Path);
			}

			var diff = qtp.Length - index;
			var expectedDiff = ((qtp.Length - 1) % 4) + 1;
			Assert.AreEqual(expectedDiff, diff);
		}
	}

	private static readonly Random random = new();
	private static string RandomQuadTreePath(int length)
	{
		char[] path = new char[length];
		path[0] = '0';

		for (int i = 1; i < length; i++)
			path[i] = (char)random.Next(0x30, 0x34);

		return new string(path);
	}
}