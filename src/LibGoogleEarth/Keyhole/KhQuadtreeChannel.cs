namespace Keyhole;

internal class KhQuadtreeChannel : IQuadtreeChannel
{
	public int Type { get; }
	public int ChannelEpoch { get; }
	public KhQuadtreeChannel(int type, int channelEpoch)
	{
		Type = type;
		ChannelEpoch = channelEpoch;
	}
}
