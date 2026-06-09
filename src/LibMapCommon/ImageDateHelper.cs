namespace LibMapCommon;

public record DateMatchResult<T>(T DatedElement, DateOnly DesiredDate) where T : IDatedElement
{
	public bool IsExactMatch { get; } = DatedElement.Date.DayNumber == DesiredDate.DayNumber;
	public int DistanceInDays { get; } = DatedElement.Date.DayNumber - DesiredDate.DayNumber;
}

public enum DateMatchType
{
	Closest,
	Exact,
	ClosestBefore,
	ClosestAfter
}

public interface IDatedElement
{
	public DateOnly Date { get; }
}

public static class ImageDateHelper
{
	/// <summary> Sort a collection of dated elements by their proximity to a set of desired dates, using the specified <see cref="DateMatchType"/>. </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="datedElements">An enumeration of dated elements</param>
	/// <param name="desiredDates">A collection of desired dates</param>
	/// <param name="dateMatchType">The type of date match to perform</param>
	/// <returns>All elements in <paramref name="datedElements"/>, sorted by distance from the dates in <paramref name="desiredDates"/> </returns>
	public static IEnumerable<DateMatchResult<T>> SortByNearestDates<T>(this IEnumerable<T> datedElements, IEnumerable<DateOnly> desiredDates, DateMatchType dateMatchType) where T : IDatedElement
	{
		DateOnly[] desiredDateList = desiredDates.ToArray();
		T[] allElements = datedElements.ToArray();
		DateMatchResult<T>[][] dateMatchSets = new DateMatchResult<T>[desiredDateList.Length][];

		for (int i = 0; i < dateMatchSets.Length; i++)
		{
			dateMatchSets[i] = new DateMatchResult<T>[allElements.Length];
		}
		for (int i = 0; i < dateMatchSets.Length; i++)
		{
			for(int dtIndex = 0; dtIndex < allElements.Length; dtIndex++)
			{
				dateMatchSets[i][dtIndex] = new(allElements[dtIndex], desiredDateList[i]);
			}
		}
		for (int i = 0; i < dateMatchSets.Length; i++)
		{
			Array.Sort(dateMatchSets[i], (a, b) => Math.Abs(a.DistanceInDays).CompareTo(Math.Abs(b.DistanceInDays)));
		}
		return
			dateMatchSets
			.SelectMany((ms, i) => ms.Select(m => new { DesiredIndex = i, Element = m }))
			.Where(p => Predicate(p.Element, dateMatchType))
			.OrderBy(p => Math.Abs(p.Element.DistanceInDays))
			.ThenBy(p => p.DesiredIndex) //Break ties by choosing the deaired date that was passed first
			.Select(p => p.Element)
			.Distinct();
	}
	private static bool Predicate<T>(DateMatchResult<T> element, DateMatchType dateMatchType) where T : IDatedElement => dateMatchType switch
	{
		DateMatchType.Closest or DateMatchType.Exact => true,
		DateMatchType.ClosestBefore => element.DatedElement.Date <= element.DesiredDate,
		DateMatchType.ClosestAfter => element.DatedElement.Date >= element.DesiredDate,
		_ => throw new ArgumentException($"Unsupported DateMatchType: {dateMatchType}")
	};

	private record MatchSet<T> where T : IDatedElement
	{
		public bool HasFullSet => Positive is not null && Negative is not null;
		public DateMatchResult<T>? Positive { get; set; }
		public DateMatchResult<T>? Negative { get; set; }
	}

	/// <summary>
	/// Find the closest dated element to any of the desired dates, using the specified <see cref="DateMatchType"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="datedElements">An enumeration of dated elements. MUST be sorted by date (either ascending or descending)</param>
	/// <param name="desiredDates">A collection of desired dates</param>
	/// <param name="dateMatchType">The type of date match to perform</param>
	/// <returns>The dated element closest to any of the desired dates</returns>
	public static async Task<DateMatchResult<T>?> GetClosestDatedElement<T>(this IAsyncEnumerable<T> datedElements, IEnumerable<DateOnly> desiredDates, DateMatchType dateMatchType) where T : IDatedElement
	{
		DateOnly[] desiredDateList = desiredDates.ToArray();
		MatchSet<T>[] dateMatchSets = Enumerable.Range(0, desiredDateList.Length).Select(_ => new MatchSet<T>()).ToArray();
		await foreach (var dt in datedElements)
		{
			for (int i = 0; i < desiredDateList.Length; i++)
			{
				var matchSet = dateMatchSets[i];
				var matchResult = new DateMatchResult<T>(dt, desiredDateList[i]);
				if (matchResult.DistanceInDays == 0)
				{
					return matchResult;
				}
				else if (matchResult.DistanceInDays > 0)
				{
					if (matchSet.Positive is null || matchSet.Positive.DistanceInDays > matchResult.DistanceInDays)
					{
						matchSet.Positive = matchResult;
					}
				}
				else if (matchSet.Negative is null || matchSet.Negative.DistanceInDays < matchResult.DistanceInDays)
				{
					matchSet.Negative = matchResult;
				}
			}
			if (dateMatchSets.All(e => e.HasFullSet))
				break;
		}
		var checkSet = dateMatchType switch
		{
			DateMatchType.Closest or DateMatchType.Exact => dateMatchSets.SelectMany(e => new[] { e.Negative, e.Positive }),
			DateMatchType.ClosestBefore => dateMatchSets.Select(e => e.Negative),
			DateMatchType.ClosestAfter => dateMatchSets.Select(e => e.Positive),
			_ => throw new ArgumentException($"Unsupported DateMatchType: {dateMatchType}")
		};
		return checkSet.OfType<DateMatchResult<T>>().OrderBy(e => Math.Abs(e.DistanceInDays)).FirstOrDefault();
	}
}
