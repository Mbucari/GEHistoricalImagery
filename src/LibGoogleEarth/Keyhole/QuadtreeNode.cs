﻿namespace Keyhole;

partial class QuadtreeNode : IQuadtreeNode
{
	IReadOnlyList<IQuadtreeLayer> IQuadtreeNode.Layer => Layer;
	IReadOnlyList<IQuadtreeChannel> IQuadtreeNode.Channel => Channel;
}