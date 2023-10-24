using System.Runtime.CompilerServices;

namespace GoogleEarthImageDownload;

internal class ParallelProcessor<T>
{
	private int _parallelism;
	public int Parallelism
	{
		get => _parallelism;
		set
		{
			if (value < 1 || value > 100)
				throw new ArgumentOutOfRangeException(nameof(Parallelism), "Valid values are [0, 100]");
			_parallelism = value;
		}
	}
	public ParallelProcessor(int parallelism)
	{
		Parallelism = parallelism;
	}

	public IAsyncEnumerable<T> EnumerateWorkAsync(IEnumerable<Func<T>> generator, CancellationToken cancellationToken = default)
		=> EnumerateWorkAsync(generator.Select(Task.Run), cancellationToken);

	public IAsyncEnumerable<T> EnumerateWorkAsync(IEnumerable<Func<Task<T>>> generator, CancellationToken cancellationToken = default)
		=> EnumerateWorkAsync(generator.Select(Task.Run), cancellationToken);

	public async IAsyncEnumerable<T> EnumerateWorkAsync(IEnumerable<Task<T>> generator, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		Task<T>?[] tasks = new Task<T>[Parallelism];
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
				var newTasks = new Task<T>[newParallelism];
				Array.Copy(tasks, 0, newTasks, 0, taskCount);
				tasks = newTasks;
			}

			if (taskCount < tasks.Length)
				pushOne(t);
		}

		while (taskCount > 0 && !cancellationToken.IsCancellationRequested)
			yield return await popOne();

		void pushOne(Task<T> task)
			=> tasks[taskCount++] = task;

		async Task<T> popOne()
		{
			var completedTask = await Task.WhenAny(tasks.OfType<Task<T>>());
			var completedIndex = Array.IndexOf(tasks, completedTask);
			tasks[completedIndex] = null;
			taskCount--;
			(tasks[completedIndex], tasks[taskCount]) = (tasks[taskCount], tasks[completedIndex]);
			return completedTask.Result;
		}
	}
}
