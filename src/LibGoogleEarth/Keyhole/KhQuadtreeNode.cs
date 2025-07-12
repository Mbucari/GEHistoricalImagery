namespace Keyhole;

internal class KhQuadtreeNode : IQuadtreeNode
{
	public KhQuadTreeBTG Children { get; }
	public int CacheNodeEpoch { get; }
	IReadOnlyList<IQuadtreeLayer> IQuadtreeNode.Layer => Layers;
	IReadOnlyList<IQuadtreeChannel> IQuadtreeNode.Channel => Channels;

	public readonly List<KhQuadtreeLayer> Layers;
	public readonly KhQuadtreeChannel[] Channels;

	public KhQuadtreeNode(KhQuadTreeQuantum16 khQuadTreeQuantum16, short[] channelTypes, short[] channelVersions)
	{
		ArgumentNullException.ThrowIfNull(channelTypes, nameof(channelTypes));
		ArgumentNullException.ThrowIfNull(channelVersions, nameof(channelVersions));
		ArgumentOutOfRangeException.ThrowIfNotEqual(channelTypes.Length, khQuadTreeQuantum16.num_channels, nameof(channelTypes));
		ArgumentOutOfRangeException.ThrowIfNotEqual(channelVersions.Length, khQuadTreeQuantum16.num_channels, nameof(channelVersions));

		Children = khQuadTreeQuantum16.children;
		CacheNodeEpoch = khQuadTreeQuantum16.cnode_version;

		Channels = new KhQuadtreeChannel[khQuadTreeQuantum16.num_channels];
		for (int i = 0; i < Channels.Length; i++)
			Channels[i] = new KhQuadtreeChannel(channelTypes[i], channelVersions[i]);

		int layerCount = 0;
		if (Children.HasTerrain)
			layerCount++;
		if (Children.HasDrawable)
			layerCount++;
		if (Children.HasImage)
			layerCount++;

		Layers = new(layerCount);

		if (Children.HasImage)
			Layers.Add(new KhQuadtreeLayer
			(
				QuadtreeLayer.Types.LayerType.Imagery,
				khQuadTreeQuantum16.image_version,
				khQuadTreeQuantum16.image_data_provider
			));
		if (Children.HasTerrain)
			Layers.Add(new KhQuadtreeLayer
			(
				QuadtreeLayer.Types.LayerType.Terrain,
				khQuadTreeQuantum16.terrain_version,
				khQuadTreeQuantum16.terrain_data_provider
			));
		if (Children.HasDrawable)
			Layers.Add(new KhQuadtreeLayer(QuadtreeLayer.Types.LayerType.Vector, 0, 0));
	}
}
