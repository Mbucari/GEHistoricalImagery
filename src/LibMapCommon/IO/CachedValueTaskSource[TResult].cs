using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks.Sources;

namespace LibMapCommon.IO;

internal class CachedValueTaskSource<TResult>(int capacity)
{
	private readonly ValueTaskSource[] ValueTaskSources
			= Enumerable.Range(0, capacity)
			.Select(_ => new ValueTaskSource())
			.ToArray();

	public ITaskCompletionSource<TResult> GetFreeTaskSource()
		=> FirstFreeSlot() ?? new TaskCompletionSourceEx();

	private ITaskCompletionSource<TResult>? FirstFreeSlot()
	{
		for (int i = 0; i < ValueTaskSources.Length; i++)
		{
			if (Interlocked.CompareExchange(ref ValueTaskSources[i].Index, i, -1) == -1)
				return ValueTaskSources[i];
		}
		return null;
	}

	private class TaskCompletionSourceEx : TaskCompletionSource<TResult>, ITaskCompletionSource<TResult>
	{
		public ValueTask<TResult> GetValueTask() => new(Task);
	}

	/// <summary>Provides the core logic for implementing a <see cref="IValueTaskSource{TResult}"/>.
	/// Cribbed from <see cref="ManualResetValueTaskSourceCore{TResult}"/></summary>
	/// <typeparam name="TResult">Specifies the type of results of the operation represented by this instance.</typeparam>
	private class ValueTaskSource : ITaskCompletionSource<TResult>, IValueTaskSource<TResult>
	{
		public int Index = -1;
		/// <summary>
		/// The callback to invoke when the operation completes if <see cref="OnCompleted"/> was called before the operation completed,
		/// or <see cref="CompletionSentinel(object?)"/> if the operation completed before a callback was supplied,
		/// or null if a callback hasn't yet been provided and the operation hasn't yet completed.
		/// </summary>
		private Action<object?>? _continuation;
		/// <summary>State to pass to <see cref="_continuation"/>.</summary>
		private object? _continuationState;
		/// <summary>The exception with which the operation failed, or null if it hasn't yet completed or completed successfully.</summary>
		private ExceptionDispatchInfo? _error;
		/// <summary>The result with which the operation succeeded, or the default value if it hasn't yet completed or failed.</summary>
		private TResult? _result;
		/// <summary>Whether the current operation has completed.</summary>
		private bool _completed;

		public ValueTask<TResult> GetValueTask()
			=> new ValueTask<TResult>(this, (short)Index);

		public void Reset()
		{
			Index = -1;
			_continuation = null;
			_continuationState = null;
			_error = null;
			_result = default;
			_completed = false;
		}

		/// <summary>Completes with a successful result.</summary>
		/// <param name="result">The result.</param>
		public void SetResult(TResult result)
		{
			_result = result;
			SignalCompletion();
		}

		/// <summary>Completes with an error.</summary>
		/// <param name="error">The exception.</param>
		public void SetException(Exception error)
		{
			_error = ExceptionDispatchInfo.Capture(error);
			SignalCompletion();
		}

		public void SetCanceled(CancellationToken cancellationToken)
			=> SetException(new OperationCanceledException(cancellationToken));

		/// <summary>Gets the result of the operation.</summary>
		[StackTraceHidden]
		TResult IValueTaskSource<TResult>.GetResult(short token)
		{
			ValidateToken(token);
			if (!_completed || _error is not null)
				ThrowForFailedGetResult();

			var result = _result!;
			Reset();
			return result;
		}

		/// <summary>Gets the status of the operation.</summary>
		ValueTaskSourceStatus IValueTaskSource<TResult>.GetStatus(short token)
		{
			ValidateToken(token);
			return Volatile.Read(ref _continuation) is null || !_completed ? ValueTaskSourceStatus.Pending :
				_error is null ? ValueTaskSourceStatus.Succeeded :
				_error.SourceException is OperationCanceledException ? ValueTaskSourceStatus.Canceled :
				ValueTaskSourceStatus.Faulted;
		}

		/// <summary>Schedules the continuation action for this operation.</summary>
		/// <param name="continuation">The continuation to invoke when the operation has completed.</param>
		/// <param name="state">The state object to pass to <paramref name="continuation"/> when it's invoked.</param>
		/// <param name="flags">The flags describing the behavior of the continuation.</param>
		void IValueTaskSource<TResult>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
		{
			ArgumentNullException.ThrowIfNull(continuation, nameof(continuation));
			ValidateToken(token);

			// We need to set the continuation state before we swap in the delegate, so that
			// if there's a race between this and SetResult/Exception and SetResult/Exception
			// sees the _continuation as non-null, it'll be able to invoke it with the state
			// stored here.  However, this also means that if this is used incorrectly (e.g.
			// awaited twice concurrently), _continuationState might get erroneously overwritten.
			// To minimize the chances of that, we check preemptively whether _continuation
			// is already set to something other than the completion sentinel.
			object? storedContinuation = _continuation;
			if (storedContinuation is null)
			{
				_continuationState = state;
				storedContinuation = Interlocked.CompareExchange(ref _continuation, continuation, null);
				if (storedContinuation is null)
				{
					// Operation hadn't already completed, so we're done. The continuation will be
					// invoked when SetResult/Exception is called at some later point.
					return;
				}
			}

			// Operation already completed, so we need to queue the supplied callback.
			// At this point the storedContinuation should be the sentinel; if it's not, the instance was misused.
			Debug.Assert(storedContinuation is not null, $"{nameof(storedContinuation)} is null");
			ThreadPool.QueueUserWorkItem(continuation, state, preferLocal: true);
		}

		private void ValidateToken(short token)
		{
			ArgumentOutOfRangeException.ThrowIfNegative(token, nameof(token));
			if (Index != token)
				throw new InvalidOperationException();
		}

		/// <summary>Signals that the operation has completed.  Invoked after the result or error has been set.</summary>
		private void SignalCompletion()
		{
			if (_completed)
				throw new InvalidOperationException();

			_completed = true;

			Action<object?>? continuation =
				Volatile.Read(ref _continuation) ??
				Interlocked.CompareExchange(ref _continuation, CompletionSentinel, null);

			if (continuation is not null)
			{
				Debug.Assert(continuation is not null, $"{nameof(continuation)} is null");
				continuation(_continuationState);
			}
		}

		private static void CompletionSentinel(object? _) // named method to aid debugging
		{
			Debug.Fail("The sentinel delegate should never be invoked.");
			throw new InvalidOperationException();
		}

		[StackTraceHidden]
		private void ThrowForFailedGetResult()
		{
			_error?.Throw();
			throw new InvalidOperationException();
		}
	}
}
