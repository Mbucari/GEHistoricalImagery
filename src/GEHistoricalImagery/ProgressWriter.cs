using System.Text;

namespace GEHistoricalImagery;

internal class ProgressWriter : StreamWriter
{
	public static ProgressWriter Instance { get; } = new();
	public double Progress { get; private set; } = -1;
	private DateTime startTime;
	private string? taskMessage;
	private int lastProgLen;
	private readonly Lock progressReportLock = new();
	private ProgressWriter() : base(Console.OpenStandardError(), Encoding.UTF8) { }
	public override void WriteLine() => WriteLine(string.Empty);
	public override void WriteLine(string? message)
	{
		lock (progressReportLock)
		{
			if (Progress < 0)
			{
				base.WriteLine(message);
			}
			else
			{
				base.Write($"\e[G\e[K{message}{Environment.NewLine}{taskMessage}\e[{lastProgLen}C");
				ReportProgress(Progress);
			}
		}
	}

	public void BeginProgress(string text)
	{
		lock (progressReportLock)
		{
			if (text[^1] != ' ')
				text += ' ';
			taskMessage = text;
			base.Write(text);
			startTime = DateTime.UtcNow;
			lastProgLen = 0;
			Progress = 0;
			ReportProgress(0);
		}
	}

	public void ReportProgress(double progress)
	{
		lock (progressReportLock)
		{
			if (Progress >= 0 && progress >= Progress)
			{
				var p = progress.ToString("P");
				var message = lastProgLen == 0 ? $"\e[K{p}" : $"\e[{lastProgLen}D\e[K{p}";
				base.Write(message);
				lastProgLen = p.Length;
				Progress = progress;
			}
		}
	}

	public void EndProgress()
	{
		lock (progressReportLock)
		{
			var elapsed = DateTime.UtcNow - startTime;
			base.WriteLine($"\e[G\e[K{taskMessage}Done! ({elapsed:h\\:mm\\:ss\\.FF})");
			lastProgLen = 0;
			Progress = -1;
		}
	}
}
