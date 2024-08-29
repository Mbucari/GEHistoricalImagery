using GEHistoricalImagery;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace QtTest
{
	[TestClass]
	public class QtPathTest
	{
		#region Pre-Computed Values
		private static readonly ReadOnlyDictionary<string, int> SubIndexDict;
		private static readonly ReadOnlyDictionary<string, int> RootIndexDict;
		#endregion

		static QtPathTest()
		{
			var projectDir = Path.Combine(".", "..", "..", "..", "..");
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
				Assert.IsTrue(QtPath.TryParse(qtPath, out var p));
				Assert.AreEqual(SubIndexDict[iStr], p.SubIndex);
				Assert.AreEqual(qtPath, p.Path);
				for (int j = 0; j < 4; j++)
				{	
					var jStr = iStr + j;
					qtPath = qtIndex + jStr;
					Assert.IsTrue(QtPath.TryParse(qtPath, out p));
					Assert.AreEqual(SubIndexDict[jStr], p.SubIndex);
					Assert.AreEqual(qtPath, p.Path);
					for (int k = 0; k < 4; k++)
					{
						var kStr = jStr + k;
						qtPath = qtIndex + kStr;
						Assert.IsTrue(QtPath.TryParse(qtPath, out p));
						Assert.AreEqual(SubIndexDict[kStr], p.SubIndex);
						Assert.AreEqual(qtPath, p.Path);
						for (int l = 0; l < 4; l++)
						{
							var lStr = kStr + l;
							qtPath = qtIndex + lStr;
							Assert.IsTrue(QtPath.TryParse(qtPath, out p));
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
				Assert.IsTrue(QtPath.TryParse(iStr, out var p));
				Assert.AreEqual(RootIndexDict[iStr], p.SubIndex);
				Assert.AreEqual(iStr, p.Path);
				for (int j = 0; j < 4; j++)
				{
					var jStr = iStr + j;
					Assert.IsTrue(QtPath.TryParse(jStr, out p));
					Assert.AreEqual(RootIndexDict[jStr], p.SubIndex);
					Assert.AreEqual(jStr, p.Path);
					for (int k = 0; k < 4; k++)
					{
						var kStr = jStr + k;
						Assert.IsTrue(QtPath.TryParse(kStr, out p));
						Assert.AreEqual(RootIndexDict[kStr], p.SubIndex);
						Assert.AreEqual(kStr, p.Path);
					}
				}
			}
		}

		[TestMethod]
		public void RootIndex2()
		{
			Assert.IsTrue(QtPath.TryParse("0", out var p));
			Assert.IsTrue(p.IsRoot);
			Assert.AreEqual(0, p.SubIndex);
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
		[DataRow(null)]
		public void BadPaths(string qtpath)
		{
			Assert.IsFalse(QtPath.TryParse(qtpath, out _));
		}

		[TestMethod]
		public void EnumerateIndices()
		{
			for (int i = 1; i < 25; i++)
			{
				var qtp = RandomQuadTreePath(i);

				Assert.IsTrue(QtPath.TryParse(qtp, out var p));
				Assert.AreEqual(qtp, p.Path);

				int index = 0;
				foreach (var qtpIndex in p.EnumerateIndices())
				{
					var expected = qtp.Substring(0, index += 4);
					Assert.AreEqual(expected, qtpIndex.Path);
				}

				var diff = qtp.Length - index;
				var expectedDiff = ((qtp.Length - 1) % 4) + 1;
				Assert.AreEqual(expectedDiff, diff);
			}
		}

		private static readonly Random random = new Random();
		private static string RandomQuadTreePath(int length)
		{
			char[] path = new char[length];
			path[0] = '0';

			for (int i = 1; i < length; i++)
				path[i] = (char)random.Next(0x30, 0x34);

			return new string(path);
		}
	}
}