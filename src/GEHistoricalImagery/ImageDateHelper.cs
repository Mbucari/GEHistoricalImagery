namespace GEHistoricalImagery;

public record DateMatchResult<T>(T DatedElement, DateOnly DesiredDate, int DistanceInDays)
{
	public bool IsExactMatch => DistanceInDays == 0;
}
internal static class ImageDateHelper
{
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="datedElements">An enumeration of dated elements</param>
	/// <param name="dateSelector">Delegate to get the element's date</param>
	/// <param name="desiredDates">A collection of desired dates</param>
	/// <returns>All elements in <paramref name="datedElements"/>, sorted by distance from the dates in <paramref name="desiredDates"/> </returns>
	public static IEnumerable<DateMatchResult<T>> SortByNearestDates<T>(this IEnumerable<T> datedElements, Func<T, DateOnly> dateSelector, IEnumerable<DateOnly> desiredDates)
	{
		var desiredDateList = desiredDates.ToList();
		var allElements = datedElements.ToArray();
		int[] distToDates = Enumerable.Repeat(int.MaxValue, desiredDateList.Count).ToArray();
		DateMatchResult<T>[][] closestLayer = new DateMatchResult<T>[distToDates.Length][];

		for (int i = 0; i < desiredDateList.Count; i++)
		{
			closestLayer[i] ??= new DateMatchResult<T>[allElements.Length];

			for (int j = 0; j < allElements.Length; j++)
			{
				var dist = int.Abs(dateSelector(allElements[j]).DayNumber - desiredDateList[i].DayNumber);
				closestLayer[i][j] = new(allElements[j], desiredDateList[i], dist);
			}
		}
		for (int i = 0; i < closestLayer.Length; i++)
		{
			Array.Sort(closestLayer[i], (a, b) => a.DistanceInDays.CompareTo(b.DistanceInDays));
		}
		return
			closestLayer
			.SelectMany((ms, i) => ms.Select(m => new { DesiredIndex = i, Element = m }))
			.OrderBy(p => p.Element.DistanceInDays)
			.ThenBy(p => p.DesiredIndex) //Break ties by choosing the deaired date that was passed first
			.Select(p => p.Element)
			.Distinct();
	}

	/// <summary>
	/// Find the closest dated element to any of the desired dates
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="datedElements">An enumeration of dated elements</param>
	/// <param name="dateSelector">Delegate to get the element's date</param>
	/// <param name="desiredDates">A collection of desired dates</param>
	/// <returns>The dated element closest to any of the desired dates</returns>
	public static async Task<DateMatchResult<T>?> GetCloseteDatedElement<T>(this IAsyncEnumerable<T> datedElements, Func<T, DateOnly> dateSelector, IEnumerable<DateOnly> desiredDates)
	{
		DateOnly[] desiredDateList = desiredDates.ToArray();
		int[] distToDates = Enumerable.Repeat(int.MaxValue, desiredDateList.Length).ToArray();
		T?[] closestElement = new T?[distToDates.Length];
		await foreach (var dt in datedElements)
		{
			bool gotCloser = false;
			for (int i = 0; i < desiredDateList.Length; i++)
			{
				var date = desiredDateList[i];
				var dist = int.Abs(dateSelector(dt).DayNumber - date.DayNumber);
				if (dist == 0)
					return new DateMatchResult<T>(dt, date, dist);
				if (dist < distToDates[i])
				{
					distToDates[i] = dist;
					closestElement[i] = dt;
					gotCloser |= true;
				}
			}
			if (!gotCloser)
				break;
		}

		int minIndex = distToDates.IndexOf(distToDates.Min());
		var closest = closestElement[minIndex];
		return closest is null ? null : new DateMatchResult<T>(closest, desiredDateList[minIndex], distToDates[minIndex]);
	}
}
