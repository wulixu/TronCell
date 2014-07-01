// create histogram backprojection into float image
__kernel void ImageByteRgbHistogramFloatBP(
						read_only image2d_t srcImage,	// source image
						write_only image2d_t dstImage,	// destination image (for propability map)
						global uint* srcHistogram,		// size must be N*N*N bytes
						global uint* frameHistogram,	// size must be N*N*N bytes
						const char n,					// number of bins
						const uint startX,				// start with X coordinate
						const uint startY				// start with Y coordinate
						)
{
	sampler_t smp = CLK_ADDRESS_NONE;

	uint x = get_global_id(0) + startX;
	uint y = get_global_id(1) + startY;
	int2 coords = (int2)(x, y);

	uint4 color = read_imageui(srcImage, smp, coords);
	float4 destColor = convert_float4(color) / 256.0f;
	color /= (256 / n);
	uint index = color.x + color.y * n + color.z * n * n;

	destColor.w = (frameHistogram[index] > 0) ? (float)srcHistogram[index] / (float)frameHistogram[index] : 0;
	write_imagef(dstImage, coords, destColor);
}