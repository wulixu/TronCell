// create histogram backprojection into byte image
__kernel void ImageByteRgbHistogramByteBP(
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
    uint4 destColor = color;
    color /= (256 / n);
	uint index = color.x + color.y * n + color.z * n * n;

	float val = (frameHistogram[index] > 0) ? 255.0f * srcHistogram[index] / frameHistogram[index] : 0;
	val = clamp(val, 0.0f, 255.0f);
	destColor.w = (char)val;
	write_imageui(dstImage, coords, destColor);
}