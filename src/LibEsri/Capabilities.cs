using System.Xml.Linq;

namespace LibEsri;

internal class Capabilities
{
	public readonly Layer[] Layers;
	private Capabilities(XElement capabilities, Layer[] layers)
	{
		Layers = layers;
	}

	public static async Task<Capabilities?> LoadAsync(Stream xmlStream)
	{
		var document = await XDocument.LoadAsync(xmlStream, LoadOptions.None, default);

		if (document.Root is not XElement capsXml)
			return null;

		var ns = capsXml.GetDefaultNamespace();

		if (capsXml.Element(ns + "Contents") is not XElement contentsXml)
			return null;

		var layers = contentsXml.Elements(ns + "Layer").Select(Layer.Parse).ToArray();

		return new Capabilities(capsXml, layers);
	}
}
