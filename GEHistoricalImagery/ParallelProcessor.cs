using System.Runtime.CompilerServices;

namespace GoogleEarthImageDownload;

internal class ParallelProcessor<T>
{
	public int Parallelism { get; }
	private List<Task<T>> _tasks;
	public ParallelProcessor(int parallelism)
	{
		Parallelism = parallelism;
		_tasks = new List<Task<T>>(parallelism);
	}
	public IAsyncEnumerable<T> GetAsyncEnumerator(IEnumerable<Func<T>> generator, CancellationToken cancellationToken = default)
		=> EnumerateWork(generator.Select(f => Task.Run(f)), cancellationToken);
	public IAsyncEnumerable<T> GetAsyncEnumerator(IEnumerable<Func<Task<T>>> generator, CancellationToken cancellationToken = default)
		=> EnumerateWork(generator.Select(f => Task.Run(f)), cancellationToken);

	public async IAsyncEnumerable<T> EnumerateWork(IEnumerable<Task<T>> generator, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		foreach (var t in generator)
		{
			if (cancellationToken.IsCancellationRequested) yield break;

			Task<T>? completedTask;

			if (_tasks.Count == Parallelism)
			{
				completedTask = await Task.WhenAny(_tasks);

				_tasks.Remove(completedTask);
			}
			else completedTask = null;

			_tasks.Add(t);

			if (completedTask is not null)
				yield return completedTask.Result;
		}

		while (_tasks.Count > 0 && !cancellationToken.IsCancellationRequested)
		{
			var completed = await Task.WhenAny(_tasks);
			_tasks.Remove(completed);
			yield return completed.Result;
		}
	}
}
