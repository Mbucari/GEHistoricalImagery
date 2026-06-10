using LibMapCommon;

namespace LibMapCommonTest;

record DatedElement(DateOnly Date) : IDatedElement;

[TestClass]
public sealed class DatedElementTest
{
	static DatedElement[] Elements = [
		new(new(2025, 12, 1)),
		new(new(2025, 11, 1)),
		new(new(2025, 10, 1)),
		new(new(2025, 9, 1)),
		new(new(2025, 8, 1)),
		new(new(2025, 7, 1)),
		new(new(2025, 6, 1)),
		new(new(2025, 5, 1)),
		new(new(2025, 4, 1)),
		new(new(2025, 3, 1)),
		new(new(2025, 2, 1)),
		new(new(2025, 1, 1)),
		];

	[TestMethod]
	[DataRow(new string[] { "2025-09-1", "2025-03-31" })]
	[DataRow(new string[] { "2025-08-1", "2025-06-30" })]
	public void TestSortByNearestDates_Closest(string[] dates)
	{
		DateOnly[] desiredDates = dates.Select(DateOnly.Parse).ToArray();

		//Closest and exact work the same way. Up to the caller to determine if the result is exact.
		//This is so caller can see the closest date and notife the user of it
		foreach (var type in new[] { DateMatchType.Closest, DateMatchType.Exact })
		{
			var result = Elements.SortByNearestDates(desiredDates, type).ToArray();
			int dist = 0;
			foreach (var element in result)
			{
				Assert.IsGreaterThanOrEqualTo(dist, Math.Abs(element.DistanceInDays));
				Assert.Contains(element.DesiredDate, desiredDates);
				dist = Math.Abs(element.DistanceInDays);
			}
		}
	}

	[TestMethod]
	[DataRow(new string[] { "2025-09-1", "2025-03-31" })]
	[DataRow(new string[] { "2025-08-1", "2025-06-30" })]
	public void TestSortByNearestDates_Before(string[] dates)
	{
		DateOnly[] desiredDates = dates.Select(DateOnly.Parse).ToArray();

		var result = Elements.SortByNearestDates(desiredDates, DateMatchType.ClosestBefore).ToArray();
		int dist = 0;
		foreach (var element in result)
		{
			Assert.IsGreaterThanOrEqualTo(dist, Math.Abs(element.DistanceInDays));
			Assert.Contains(element.DesiredDate, desiredDates);
			Assert.IsLessThanOrEqualTo(0, element.DistanceInDays); // Before should always be negative or 0
			dist = Math.Abs(element.DistanceInDays);
		}
	}

	[TestMethod]
	[DataRow(new string[] { "2025-09-1", "2025-03-31" })]
	[DataRow(new string[] { "2025-08-1", "2025-06-30" })]
	public void TestSortByNearestDates_After(string[] dates)
	{
		DateOnly[] desiredDates = dates.Select(DateOnly.Parse).ToArray();

		var result = Elements.SortByNearestDates(desiredDates, DateMatchType.ClosestAfter).ToArray();
		int dist = 0;
		foreach (var element in result)
		{
			Assert.IsGreaterThanOrEqualTo(dist, Math.Abs(element.DistanceInDays));
			Assert.Contains(element.DesiredDate, desiredDates);
			Assert.IsGreaterThanOrEqualTo(0, element.DistanceInDays); // After should always be positive or 0
			dist = Math.Abs(element.DistanceInDays);
		}
	}

	[TestMethod]
	[DataRow(new string[] { "2025-08-02", "2025-07-31" }, "2025-08-02", 8)] // Tied, straddling 1
	[DataRow(new string[] { "2025-07-16", "2025-07-17" }, "2025-07-16", 7)] // Tied, between 2
	[DataRow(new string[] { "2025-07-10", "2025-07-11" }, "2025-07-10", 7)]
	[DataRow(new string[] { "2025-07-19", "2025-07-20" }, "2025-07-20", 8)]
	[DataRow(new string[] { "2025-08-02", "2025-07-30" }, "2025-08-02", 8)]
	[DataRow(new string[] { "2025-08-02", "2025-08-03" }, "2025-08-02", 8)]
	[DataRow(new string[] { "2025-08-03", "2025-07-31" }, "2025-07-31", 8)]
	[DataRow(new string[] { "2025-08-03", "2025-08-02", "2025-07-31" }, "2025-08-02", 8)] // 1 and 2 tied, straddling
	[DataRow(new string[] { "2025-07-16", "2025-07-15", "2025-07-18" }, "2025-07-15", 7)] // 1 and 2 tied, between
	[DataRow(new string[] { "2025-08-03", "2025-07-30", "2025-08-02" }, "2025-08-02", 8)]
	[DataRow(new string[] { "2025-08-03", "2025-07-28", "2025-07-31" }, "2025-07-31", 8)]
	public async Task TestClosestAndExact(string[] dates, string expedtedDate, int expedtedMonth)
	{
		DateOnly[] desiredDates = dates.Select(DateOnly.Parse).ToArray();
		//Closest and exact work the same way. Up to the caller to determine if the result is exact.
		//This is so caller can see the closest date and notife the user of it
		foreach (var type in new[] { DateMatchType.Closest, DateMatchType.Exact })
		{
			var result = await Elements.ToAsyncEnumerable().GetClosestDatedElement(desiredDates, type);
			Assert.IsNotNull(result);
			Assert.AreEqual(DateOnly.Parse(expedtedDate), result.DesiredDate);
			Assert.AreEqual(expedtedMonth, result.DatedElement.Date.Month);
		}
	}

	[TestMethod]
	[DataRow(new string[] { "2025-08-02", "2025-07-31" }, "2025-07-31", 8)] // Tied, straddling 1
	[DataRow(new string[] { "2025-07-16", "2025-07-17" }, "2025-07-17", 8)] // Tied, between 2
	[DataRow(new string[] { "2025-07-10", "2025-07-11" }, "2025-07-11", 8)]
	[DataRow(new string[] { "2025-07-19", "2025-07-20" }, "2025-07-20", 8)]
	[DataRow(new string[] { "2025-08-02", "2025-07-30" }, "2025-07-30", 8)]
	[DataRow(new string[] { "2025-08-02", "2025-08-03" }, "2025-08-03", 9)]
	[DataRow(new string[] { "2025-08-03", "2025-07-31" }, "2025-07-31", 8)]
	[DataRow(new string[] { "2025-08-03", "2025-08-02", "2025-07-31" }, "2025-07-31", 8)] // 1 and 2 tied, straddling
	[DataRow(new string[] { "2025-07-16", "2025-07-15", "2025-07-18" }, "2025-07-18", 8)] // 1 and 2 tied, between
	[DataRow(new string[] { "2025-08-03", "2025-07-30", "2025-08-02" }, "2025-07-30", 8)]
	[DataRow(new string[] { "2025-08-03", "2025-07-28", "2025-07-31" }, "2025-07-31", 8)]
	public async Task TestClosestAfter(string[] dates, string expedtedDate, int expedtedMonth)
	{
		DateOnly[] desiredDates = dates.Select(DateOnly.Parse).ToArray();
		var result = await Elements.ToAsyncEnumerable().GetClosestDatedElement(desiredDates, DateMatchType.ClosestAfter);
		Assert.IsNotNull(result);
		Assert.AreEqual(DateOnly.Parse(expedtedDate), result.DesiredDate);
		Assert.AreEqual(expedtedMonth, result.DatedElement.Date.Month);
	}

	[TestMethod]
	[DataRow(new string[] { "2025-08-02", "2025-07-31" }, "2025-08-02", 8)] // Tied, straddling 1
	[DataRow(new string[] { "2025-07-16", "2025-07-17" }, "2025-07-16", 7)] // Tied, between 2
	[DataRow(new string[] { "2025-07-10", "2025-07-11" }, "2025-07-10", 7)]
	[DataRow(new string[] { "2025-07-19", "2025-07-20" }, "2025-07-19", 7)]
	[DataRow(new string[] { "2025-08-02", "2025-07-30" }, "2025-08-02", 8)]
	[DataRow(new string[] { "2025-08-02", "2025-08-03" }, "2025-08-02", 8)]
	[DataRow(new string[] { "2025-08-03", "2025-07-31" }, "2025-08-03", 8)]
	[DataRow(new string[] { "2025-08-03", "2025-08-02", "2025-07-31" }, "2025-08-02", 8)] // 1 and 2 tied, straddling
	[DataRow(new string[] { "2025-07-16", "2025-07-15", "2025-07-18" }, "2025-07-15", 7)] // 1 and 2 tied, between
	[DataRow(new string[] { "2025-08-03", "2025-07-30", "2025-08-02" }, "2025-08-02", 8)]
	[DataRow(new string[] { "2025-08-03", "2025-07-28", "2025-07-31" }, "2025-08-03", 8)]
	public async Task TestClosestBefore(string[] dates, string expedtedDate, int expedtedMonth)
	{
		DateOnly[] desiredDates = dates.Select(DateOnly.Parse).ToArray();
		var result = await Elements.ToAsyncEnumerable().GetClosestDatedElement(desiredDates, DateMatchType.ClosestBefore);
		Assert.IsNotNull(result);
		Assert.AreEqual(DateOnly.Parse(expedtedDate), result.DesiredDate);
		Assert.AreEqual(expedtedMonth, result.DatedElement.Date.Month);
	}
}
