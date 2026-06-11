using System.Text;

namespace GEHistoricalImagery;

internal class ProgressWriter : TextWriter
{
	public static ProgressWriter Instance { get; } = new();
	public double Progress { get; private set; } = -1;
	public override Encoding Encoding => Encoding.UTF8;

	private DateTime startTime;
	private string? taskMessage;
	private int lastProgLen;
	private readonly Lock progressReportLock = new();
	private readonly Stream StdErrStream = Console.OpenStandardError();
	private ProgressWriter() { }
	public override void WriteLine() => WriteLine(string.Empty);
	public override void WriteLine(string? message)
	{
		lock (progressReportLock)
		{
			if (Progress < 0)
			{
				Write(message + Environment.NewLine);
			}
			else
			{
				Write($"\e[G\e[K{message}{Environment.NewLine}{taskMessage}\e[{lastProgLen}C");
				ReportProgress(Progress);
			}
		}
	}
	public override void Write(char[] buffer, int index, int count)
		=> Write(new ReadOnlySpan<char>(buffer, index, count));
	public override void Write(string? message)
	{
		if (message is not null)
			Write(message.AsSpan());
	}
	public override void Write(ReadOnlySpan<char> buffer)
	{
		Span<byte> bytes = new byte[Encoding.GetByteCount(buffer)];
		Encoding.GetBytes(buffer, bytes);
		StdErrStream.Write(bytes);
	}
	public override void Write(char value)
		=> throw new NotSupportedException();

	public void BeginProgress(string text)
	{
		lock (progressReportLock)
		{
			if (text[^1] != ' ')
				text += ' ';
			taskMessage = text;
			Write(text);
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
				Write(message);
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
			Write($"\e[G\e[K{taskMessage}Done! ({elapsed:h\\:mm\\:ss\\.FF}){Environment.NewLine}");
			lastProgLen = 0;
			Progress = -1;
		}
	}
}
