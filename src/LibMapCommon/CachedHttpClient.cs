using LibMapCommon.IO;
using System.Text;

namespace LibMapCommon;

public class CachedHttpClient
{
	private readonly HttpClient HttpClient = new();
	public DirectoryInfo? CacheDir { get; }
	public CachedHttpClient(DirectoryInfo? cacheDir)
	{
		CacheDir = cacheDir;
	}

	public Task<Stream> GetStreamAsync(string url)
		=> HttpClient.GetStreamAsync(url);

	public async Task<byte[]> GetBytesIfNewer(string url)
	{
		var fileName = UrlToFileName(url);

		var directory = CacheDir?.Exists is true ? CacheDir.FullName : Path.GetTempPath();
		var filePath = new FileInfo(Path.Combine(directory, fileName));

		await using var mutex = await AsyncMutex.AcquireAsync("Global\\" + HashString(filePath.FullName));

		using var request = new HttpRequestMessage(HttpMethod.Get, url);

		if (filePath.Exists)
			request.Headers.IfModifiedSince = filePath.LastWriteTimeUtc;

		using var response = await HttpClient.SendAsync(request);

		if (filePath.Exists && response.StatusCode == System.Net.HttpStatusCode.NotModified)
			return File.ReadAllBytes(filePath.FullName);
		else
		{
			response.EnsureSuccessStatusCode();
			var fileBytes = await response.Content.ReadAsByteArrayAsync();
			try
			{
				File.WriteAllBytes(filePath.FullName, fileBytes);

				if (response.Content.Headers.LastModified.HasValue)
					filePath.LastWriteTimeUtc = response.Content.Headers.LastModified.Value.UtcDateTime;
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Failed to Cache {filePath.FullName}.");
				Console.Error.WriteLine(ex.Message);
			}
			return fileBytes;
		}
	}

	/// <summary>
	/// Download, decrypt and cache a file from Google Earth.
	/// </summary>
	/// <param name="url">The Google Earth asset Url</param>
	/// <returns>The asset's bytes</returns>
	public async Task<byte[]> GetByteArrayAsync(string url, Action<Span<byte>>? postDownloadAction = null)
	{
		if (CacheDir?.Exists is true)
		{
			var fileName = UrlToFileName(url);
			var filePath = Path.Combine(CacheDir.FullName, fileName);
			await using var mutex = await AsyncMutex.AcquireAsync("Global\\" + fileName);

			if (File.Exists(filePath) && File.ReadAllBytes(filePath) is byte[] b && b.Length > 0)
				return b;
			else
			{
				var data = await download();

				try
				{
					File.WriteAllBytes(filePath, data);
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine($"Failed to Cache {url}.");
					Console.Error.WriteLine(ex.Message);
				}
				return data;
			}
		}
		else
			return await download();

		async Task<byte[]> download()
		{
			var data = await HttpClient.GetByteArrayAsync(url);
			postDownloadAction?.Invoke(data);
			return data;
		}
	}

	private static string UrlToFileName(string url)
		=> HashString(url);

	private static string HashString(string s)
		=> Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(Encoding.UTF8.GetBytes(s)));
}
