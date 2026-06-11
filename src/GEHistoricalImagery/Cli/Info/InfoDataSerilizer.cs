using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace GEHistoricalImagery.Cli.Info;

[JsonSourceGenerationOptions(
	WriteIndented = true,
	UseStringEnumConverter = true,
	AllowDuplicateProperties = false,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(InfoData))]
internal partial class InfoDataSerilizer : JsonSerializerContext
{
	static InfoDataSerilizer()
	{
		Default = new InfoDataSerilizer(new JsonSerializerOptions(Default.Options)
		{
			Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
		});
	}
}
