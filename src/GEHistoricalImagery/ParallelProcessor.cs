using System.Runtime.CompilerServices;

namespace GEHistoricalImagery;

internal class ParallelProcessor<TResult>
{
	private int _parallelism;
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
		Task<TResult>?[] tasks = new Task<TResult>[Parallelism];
		int taskCount = 0;

		foreach (var t in generator)
		{
			int newParallelism;

			while (taskCount >= (newParallelism = Parallelism) && !cancellationToken.IsCancellationRequested)
				yield return await popOne();

			if (cancellationToken.IsCancellationRequested)
				yield break;

			if (tasks.Length != newParallelism)
			{
				var newTasks = new Task<TResult>[newParallelism];
				Array.Copy(tasks, 0, newTasks, 0, taskCount);
				tasks = newTasks;
			}

			if (taskCount < tasks.Length)
				pushOne(t);
		}

		while (taskCount > 0 && !cancellationToken.IsCancellationRequested)
			yield return await popOne();

		void pushOne(Task<TResult> task)
			=> tasks[taskCount++] = task;

		async Task<TResult> popOne()
		{
			var completedTask = await Task.WhenAny(tasks.OfType<Task<TResult>>());
			var completedIndex = Array.IndexOf(tasks, completedTask);
			tasks[completedIndex] = null;
			taskCount--;
			(tasks[completedIndex], tasks[taskCount]) = (tasks[taskCount], tasks[completedIndex]);
			return completedTask.Result;
		}
	}
}
