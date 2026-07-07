using System.Runtime.CompilerServices;

namespace GEHistoricalImagery;

internal class ParallelProcessor<TResult>
{
	private volatile int _parallelism;
	public int Parallelism
	{
		get => _parallelism;
		set
		{
			ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, nameof(Parallelism));
			ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100, nameof(Parallelism));
			_parallelism = value;
		}
	}
	public ParallelProcessor(int parallelism)
	{
		Parallelism = parallelism;
	}

	public IAsyncEnumerable<TResult> EnumerateResults(IEnumerable<Func<TResult>> generator, CancellationToken cancellationToken = default)
		=> EnumerateResults(generator.Select(Task.Run), cancellationToken);

	public IAsyncEnumerable<TResult> EnumerateResults(IEnumerable<Func<Task<TResult>?>> generator, CancellationToken cancellationToken = default)
		=> EnumerateResults(generator.Select(Task.Run), cancellationToken);

	public async IAsyncEnumerable<TResult> EnumerateResults(IEnumerable<Task<TResult>> generator, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		HashSet<Task<TResult>> tasks = new(Parallelism);
		foreach (var t in generator)
		{
			tasks.Add(t);
			while (tasks.Count >= Parallelism)
				yield return await popOne();
		}

		while (tasks.Count > 0)
			yield return await popOne();

		async Task<TResult> popOne()
		{
			cancellationToken.ThrowIfCancellationRequested();
			Task<TResult> completedTask = await Task.WhenAny(tasks);
			tasks.Remove(completedTask);
			return completedTask.Result;
		}
	}
}
