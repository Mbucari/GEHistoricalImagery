namespace LibMapCommon.IO;

internal static class AsyncMutex
{
	private const int MaxValueTasks = 10;
	private static readonly CachedValueTaskSource<IAsyncDisposable> ValueTaskSources = new(MaxValueTasks);

	public static ValueTask<IAsyncDisposable> AcquireAsync(string mutexName, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		Task? mutexTask = null;

		var taskCompletionSource = ValueTaskSources.GetFreeTaskSource();
		//Create the task before starting so when it starts,
		//WaitForTask is sure to capture the non-null mutexTask.
		mutexTask = new Task(WaitForTask, cancellationToken, TaskCreationOptions.DenyChildAttach);
		mutexTask.Start();
		return taskCompletionSource.GetValueTask();

		void WaitForTask()
		{
			try
			{
				using var mutex = new Mutex(false, mutexName);
				try
				{
					// Wait for either the mutex to be acquired, or cancellation
#if LINUX
					while (!mutex.WaitOne(10))
					{
						if (cancellationToken.IsCancellationRequested)
						{
							taskCompletionSource.SetCanceled(cancellationToken);
							return;
						}
					}
#else
					if (WaitHandle.WaitAny([mutex, cancellationToken.WaitHandle]) != 0)
					{
						taskCompletionSource.SetCanceled(cancellationToken);
						return;
					}
#endif
				}
				catch (AbandonedMutexException)
				{ /* Abandoned by another process, we acquired it. */ }

				using var releaseEvent = new ManualResetEventSlim();
				taskCompletionSource.SetResult(new MutexAwaiter(mutexTask!, releaseEvent));

				// Wait until the release call
				releaseEvent.Wait(cancellationToken);
				mutex.ReleaseMutex();
			}
			catch (OperationCanceledException)
			{
				taskCompletionSource.SetCanceled(cancellationToken);
			}
			catch (Exception ex)
			{
				taskCompletionSource.SetException(ex);
			}
		}
	}

	private class MutexAwaiter(Task mutexTask, ManualResetEventSlim releaseEvent) : IAsyncDisposable
	{
		private readonly Task _mutexTask = mutexTask;
		private readonly ManualResetEventSlim _releaseEvent = releaseEvent;

		public async ValueTask DisposeAsync()
		{
			_releaseEvent.Set();
			await _mutexTask;
		}
	}
}
