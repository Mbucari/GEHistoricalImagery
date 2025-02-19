namespace LibMapCommon.IO;

internal interface ITaskCompletionSource<TResult>
{
	void SetResult(TResult result);
	void SetException(Exception exception);
	void SetCanceled(CancellationToken cancellationToken);
	ValueTask<TResult> GetValueTask();
}
