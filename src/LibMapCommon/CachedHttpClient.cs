using LibMapCommon.IO;
using System.Net;
using System.Text;

namespace LibMapCommon;

public class CachedHttpClient
{
	private readonly HttpClient HttpClient;
	public DirectoryInfo? CacheDir { get; }
	public CachedHttpClient(DirectoryInfo? cacheDir)
	{
		CacheDir = cacheDir;
		var handler = new HttpClientHandler()
		{
			AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
		};
		HttpClient = new HttpClient(handler);
	}

	public Task<Stream> GetStreamAsync(string url)
		=> HttpClient.GetStreamAsync(url);

	public async Task<byte[]> GetBytesIfNewer(string url)
	{
		var fileName = await UrlToFileNameAsync(url);

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
	/// Download and cache a file.
	/// </summary>
	/// <param name="url">The asset Url</param>
	/// <returns>The asset's bytes</returns>
	public async Task<byte[]> GetByteArrayAsync(string url, Action<Span<byte>>? postDownloadAction = null)
	{
		if (CacheDir?.Exists is true)
		{
			var fileName = await UrlToFileNameAsync(url);
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

	/// <summary>
	/// Download and cache a file.
	/// </summary>
	/// <param name="url">The asset Url</param>
	/// <param name="content">The content to post to the url</param>
	/// <returns>The asset's bytes</returns>
	public async Task<byte[]> PostByteArrayAsync(string url, HttpContent content, Action<Span<byte>>? postDownloadAction = null)
	{
		if (CacheDir?.Exists is true)
		{
			var fileName = await UrlToFileNameAsync(url, content);
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
			var response = await HttpClient.PostAsync(url, content);
			var data = await response.Content.ReadAsByteArrayAsync();
			postDownloadAction?.Invoke(data);
			return data;
		}
	}

	public async Task DeleteCachedPageAsync(string url, HttpContent? content = null)
	{
		if (CacheDir is not { FullName: string fullName }) return;
		var fileName = await UrlToFileNameAsync(url, content);
		var filePath = Path.Combine(fullName, fileName);
		await using var mutex = await AsyncMutex.AcquireAsync("Global\\" + fileName);
		if (File.Exists(filePath))
			File.Delete(filePath);
	}

	private static async Task<string> UrlToFileNameAsync(string url, HttpContent? content = null)
		=> content is null ? HashString(url) : HashString(url + await content.ReadAsStringAsync());

	private static string HashString(string s)
		=> Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(Encoding.UTF8.GetBytes(s)));
}
