namespace GoogleEarthImageDownload.Cli;

internal abstract class OptionsBase
{
	public abstract Task Run();

	public double Progress { get; set; }

	private int lastProgLen;
	protected void ReportProgress(double progress)
	{
		lock (this)
		{
			if (progress >= Progress)
			{
				var p = progress.ToString("P");
				Console.Write(new string('\b', lastProgLen) + p);
				lastProgLen = p.Length;
				Progress = progress;
			}
		}
	}

	protected void ReplaceProgress(string text)
	{
		var newText = new string('\b', lastProgLen);

		newText = newText + new string(' ', lastProgLen) + newText + text;

		Console.Write(newText);
		Progress = 0;
		lastProgLen = 0;
	}
}
