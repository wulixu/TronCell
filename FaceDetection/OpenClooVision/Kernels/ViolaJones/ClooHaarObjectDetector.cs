#region License
/*

Copyright (c) 2010-2011 by Hans Wolff

Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

*/
#endregion

using System;
using System.Drawing;
using System.Linq;
using Cloo;
using OpenClooVision.Imaging;

namespace OpenClooVision.Kernels.ViolaJones
{
    /// <summary>
    /// Contains all needed buffers for object detection
    /// </summary>
    [CLSCompliant(false)]
    public class ClooHaarObjectDetector : BaseHaarObjectDetector
    {
        /// <summary>
        /// Cloo context
        /// </summary>
        public ClooContext Context { get; set; }

        /// <summary>
        /// Cloo command queue
        /// </summary>
        public ClooCommandQueue Queue { get; set; }

        protected ClooProgramViolaJones _clooProgram = null;

        /// <summary>
        /// Performs object detection on the given frame on GPU
        /// </summary>
        /// <param name="image">image where to perform the detection</param>
        /// <exception cref="ArgumentNullException">image</exception>
        public override int ProcessFrame(IImage2DByteA image)
        {
            if (image == null) throw new ArgumentNullException("image");

            if (image.Width > _integralImage.Width || image.Height > _integralImage.Height)
                throw new ArgumentException("Specified image (" + image.Width + " x " + image.Height + ") is too big, maximum size is (" + _integralImage.Width + " x " + _integralImage.Height + ")");

            // create integral image
            _clooProgram.Integral(Queue, (ClooImage2DByteA)image, (ClooImage2DUIntA)_integralImage);
            _clooProgram.IntegralSquare(Queue, (ClooImage2DByteA)image, (ClooImage2DUIntA)_integral2Image);

            return ProcessFrame();
        }

        /// <summary>
        /// Performs object detection on the given frame.
        /// </summary>
        public override int ProcessFrame()
        {
            _resultRectanglesCount = 0;

            int width = _integralImage.Width;
            int height = _integralImage.Height;

            float fstart, fstop, fstep;
            bool inv;

            switch (_scalingMode)
            {
                case ScalingMode.SmallerToLarger:
                    {
                        fstart = 1f;
                        fstop = Math.Min(width / (float)WindowWidth, height / (float)WindowHeight);
                        fstep = _scalingFactor;
                        inv = false;
                        break;
                    }
                case ScalingMode.LargerToSmaller:
                    {
                        fstart = Math.Min(width / (float)WindowWidth, height / (float)WindowHeight);
                        fstop = 1f;
                        fstep = 1f / _scalingFactor;
                        inv = true;
                        break;
                    }
                default:
                    {
                        fstart = Math.Min(width / (float)_minSize.Width, height / (float)_minSize.Height);
                        fstop = fstart + 1;
                        fstep = _scalingFactor;
                        inv = false;
                        break;
                    }
            }

            // clear rectangles first
            _clooProgram.ClearViolaJonesRectangles(Queue, (ClooBuffer<Rectangle>)_resultRectangles);

            for (float f = fstart; (inv && f > fstop) || (!inv && f < fstop); f *= fstep)
            {
                // get scaled window size
                int windowWidth = (int)(WindowWidth * f);
                int windowHeight = (int)(WindowHeight * f);

                // check if the window is lesser than the minimum size
                if (windowWidth < _minSize.Width || windowHeight < _minSize.Height)
                {
                    if (inv) break; else continue;
                }

                // check if the window is bigger than the minimum size
                if (windowWidth > _maxSize.Width || windowHeight > _maxSize.Height)
                {
                    if (inv) continue; else break;
                }

                int endX = width - windowWidth;
                int endY = height - windowHeight;

                int countX = endX / _stepX;
                int countY = endY / _stepY;

                // scan the integral image searching for positives
                _clooProgram.ProcessViolaJonesFrame(Queue, (ClooBuffer<HaarFeatureNode>)_stageNodes,
                    _stageCount, (ClooBuffer<int>)_stageNodeCounts, (ClooBuffer<float>)_stageThresholds,
                    (ClooImage2DUIntA)_integralImage, (ClooImage2DUIntA)_integral2Image,
                    (ClooBuffer<Rectangle>)_resultRectangles,
                    f, countX, countY, _stepX, _stepY, windowWidth, windowHeight);
            }
            Queue.Finish();
            ((ClooBuffer<Rectangle>)_resultRectangles).ReadFromDevice(Queue);

            // move rectangles in array to the beginning
            int pos = 0;
            for (int i = 0; i < _resultRectangles.HostBuffer.Length; i++)
            {
                Rectangle rect = _resultRectangles.HostBuffer[i];
                if (rect.Width > 0)
                {
                    if (i != pos) _resultRectangles.HostBuffer[pos] = rect;
                    pos++;
                }
            }
            _resultRectanglesCount = pos;

            return _resultRectanglesCount;
        }

        /// <summary>
        /// Creates a face detector for GPU processing
        /// </summary>
        /// <param name="fileName">XML file containing Haar cascade stages (in OpenCL format)</param>
        /// <param name="context">Cloo context</param>
        /// <param name="queue">Cloo command queue</param>
        /// <param name="imageWidth">image width</param>
        /// <param name="imageHeight">image height</param>
        /// <returns>HaarObjectDetector</returns>
        public static ClooHaarObjectDetector CreateDetectorFromFile(string fileName, ClooContext context, ClooCommandQueue queue, int imageWidth, int imageHeight)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (queue == null) throw new ArgumentNullException("queue");

            ClooHaarObjectDetector res = new ClooHaarObjectDetector();
            res._clooProgram = ClooProgramViolaJones.Create(context);
            res.Context = context;
            res.Queue = queue;

            res._integralImage = ClooImage2DUIntA.Create(context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, imageWidth, imageHeight);
            res._integral2Image = ClooImage2DUIntA.Create(context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, imageWidth, imageHeight);
            res._resultRectangles = new ClooBuffer<Rectangle>(context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.AllocateHostPointer, (long)MaxResultRectangles);

            var tuple = LoadFromXml(fileName);
            res.WindowWidth = tuple.WindowSize.Width;
            res.WindowHeight = tuple.WindowSize.Height;
            res._stageNodes = new ClooBuffer<HaarFeatureNode>(context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, tuple.StageNodes.ToArray());
            res._stageNodeCounts = new ClooBuffer<int>(context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, tuple.StageNodesCount.ToArray());
            res._stageThresholds = new ClooBuffer<float>(context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, tuple.StageThresholds.ToArray());
            res._totalNodesCount = tuple.StageNodes.Count;
            res._stageCount = tuple.StageNodesCount.Count;
            return res;
        }

        /// <summary>
        /// Creates a face detector for GPU processing
        /// </summary>
        /// <param name="context">Cloo context</param>
        /// <param name="queue">Cloo command queue</param>
        /// <param name="imageWidth">image width</param>
        /// <param name="imageHeight">image height</param>
        /// <returns>HaarObjectDetector</returns>
        public static ClooHaarObjectDetector CreateFaceDetector(ClooContext context, ClooCommandQueue queue, int imageWidth, int imageHeight)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (queue == null) throw new ArgumentNullException("queue");

            ClooHaarObjectDetector res = new ClooHaarObjectDetector();
            res._clooProgram = ClooProgramViolaJones.Create(context);
            res.Context = context;
            res.Queue = queue;
            res.WindowWidth = 20;
            res.WindowHeight = 20;

            res._integralImage = ClooImage2DUIntA.Create(context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, imageWidth, imageHeight);
            res._integral2Image = ClooImage2DUIntA.Create(context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, imageWidth, imageHeight);
            res._resultRectangles = new ClooBuffer<Rectangle>(context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.AllocateHostPointer, (long)MaxResultRectangles);

            var tuple = CreateFaceDetector();
            res.WindowWidth = tuple.WindowSize.Width;
            res.WindowHeight = tuple.WindowSize.Height;
            res._stageNodes = new ClooBuffer<HaarFeatureNode>(context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, tuple.StageNodes.ToArray());
            res._stageNodeCounts = new ClooBuffer<int>(context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, tuple.StageNodesCount.ToArray());
            res._stageThresholds = new ClooBuffer<float>(context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, tuple.StageThresholds.ToArray());
            res._totalNodesCount = tuple.StageNodes.Count;
            res._stageCount = tuple.StageNodesCount.Count;
            return res;
        }
    }
}
