﻿#region License
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
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Xml;
using OpenClooVision.Imaging;

namespace OpenClooVision.Kernels.ViolaJones
{
    /// <summary>
    /// Contains all needed buffers for object detection
    /// </summary>
    [CLSCompliant(false)]
    public abstract class BaseHaarObjectDetector : IDisposable
    {
        /// <summary>
        /// Describes detector data
        /// </summary>
        public class DetectorData
        {
            /// <summary>
            /// Stage nodes
            /// </summary>
            public List<HaarFeatureNode> StageNodes { get; set; }

            /// <summary>
            /// Stage nodes count
            /// </summary>
            public List<int> StageNodesCount { get; set; }

            /// <summary>
            /// Stage thresholds
            /// </summary>
            public List<float> StageThresholds { get; set; }

            /// <summary>
            /// Window size of trained features
            /// </summary>
            public Size WindowSize { get; set; }
        }

        /// <summary>
        /// Maximum number of rectangles to be found (best if prime number)
        /// </summary>
        public const int MaxResultRectangles = 1021;

        protected Size _minSize = new Size(20, 20);
        /// <summary>
        /// Minimum dynamic window size
        /// </summary>
        public Size MinSize
        {
            get { return _minSize; }
            set { _minSize = value; }
        }

        protected Size _maxSize = new Size(Int32.MaxValue, Int32.MaxValue);
        /// <summary>
        /// Maximum dynamic window size
        /// </summary>
        public Size MaxSize
        {
            get { return _maxSize; }
            set { _maxSize = value; }
        }

        /// <summary>
        /// Window height
        /// </summary>
        public int WindowHeight { get; set; }

        /// <summary>
        /// Window width
        /// </summary>
        public int WindowWidth { get; set; }

        protected float _scalingFactor = 1.25f;
        /// <summary>
        /// Scaling factor
        /// </summary>
        public float ScalingFactor
        {
            get { return _scalingFactor; }
            set { _scalingFactor = value; }
        }

        protected ScalingMode _scalingMode = ScalingMode.SmallerToLarger;
        /// <summary>
        /// Window scaling mode
        /// </summary>
        public ScalingMode ScalingMode
        {
            get { return _scalingMode; }
            set { _scalingMode = value; }
        }

        protected int _stageCount = 0;
        /// <summary>
        /// Gets the number of stages
        /// </summary>
        public int StageCount
        {
            get { return _stageCount; }
        }

        protected int _totalNodesCount = 0;
        /// <summary>
        /// Gets the number of all nodes
        /// </summary>
        public int TotalNodesCount
        {
            get { return _totalNodesCount; }
        }

        protected IBuffer<float> _stageThresholds;
        /// <summary>
        /// Buffer that contains a threshold for each stage
        /// </summary>
        public IBuffer<float> StageThresholds
        {
            get { return _stageThresholds; }
        }

        protected IBuffer<int> _stageNodeCounts;
        /// <summary>
        /// Buffer that contains the number of nodes in a stage
        /// </summary>
        public IBuffer<int> StageNodeCounts
        {
            get { return _stageNodeCounts; }
        }

        protected IBuffer<HaarFeatureNode> _stageNodes;
        /// <summary>
        /// Buffer that contains all stages and nodes
        /// </summary>
        public IBuffer<HaarFeatureNode> StageNodes
        {
            get { return _stageNodes; }
        }

        protected bool _ownIntegralImage = true;
        protected IImage2DUIntA _integralImage;
        /// <summary>
        /// Integral image being used in object classification
        /// </summary>
        public IImage2DUIntA IntegralImage
        {
            get { return _integralImage; }
            set { _integralImage = value; _ownIntegral2Image = false; }
        }

        protected bool _ownIntegral2Image = true;
        protected IImage2DUIntA _integral2Image;
        /// <summary>
        /// Squared integral image being used in object classification
        /// </summary>
        public IImage2DUIntA Integral2Image
        {
            get { return _integral2Image; }
            set { _integral2Image = value; _ownIntegral2Image = false; }
        }

        protected int _resultRectanglesCount = 0;
        /// <summary>
        /// Amount of rectangles found in last detection operation
        /// </summary>
        public int ResultRectanglesCount
        {
            get { return _resultRectanglesCount; }
        }

        protected int _stepX = 5;
        /// <summary>
        /// Step size in X direction for sliding window
        /// </summary>
        public int StepX
        {
            get { return _stepX; }
            set { if (value > 0) _stepX = value; else throw new ArgumentOutOfRangeException("Step X must be at least 1"); }
        }

        protected int _stepY = 5;
        /// <summary>
        /// Step size in Y direction for sliding window
        /// </summary>
        public int StepY
        {
            get { return _stepY; }
            set { if (value > 0) _stepY = value; else throw new ArgumentOutOfRangeException("Step Y must be at least 1"); }
        }

        protected bool _ownResultRectangles = true;
        protected IBuffer<Rectangle> _resultRectangles;
        /// <summary>
        /// Buffer containing all result rectangles
        /// </summary>
        public IBuffer<Rectangle> ResultRectangles
        {
            get { return _resultRectangles; }
            set { _resultRectangles = value; }
        }

        /// <summary>
        /// Performs object detection on the given frame
        /// </summary>
        /// <param name="image">image where to perform the detection</param>
        /// <exception cref="ArgumentNullException">image</exception>
        public abstract int ProcessFrame(IImage2DByteA image);

        /// <summary>
        /// Perform object detection on single frame
        /// </summary>
        /// <returns></returns>
        public abstract int ProcessFrame();

        /// <summary>
        /// Load stages from XML file (in OpenCV format)
        /// </summary>
        /// <param name="xmlFile">XML file to load</param>
        /// <returns></returns>
        protected static DetectorData LoadFromXml(string xmlFile)
        {
            int index = 0;
            List<HaarFeatureNode> stageNodes = new List<HaarFeatureNode>();
            List<int> stageNodeCounts = new List<int>();
            List<double> stageThresholds = new List<double>();
            Size windowSize = new Size();

            XmlDocument xml = new XmlDocument();
            xml.PreserveWhitespace = false;
            xml.Load(xmlFile);

            var nodeSize = xml.SelectSingleNode(@"//size");
            if (nodeSize != null)
            {
                string[] parts = nodeSize.InnerText.Split(new [] {' ', '\r', '\n', '\t'}, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2) throw new XmlException("Node \"size\" in XML file contains an invalid value");
                windowSize.Width = Int32.Parse(parts[0], CultureInfo.InvariantCulture);
                windowSize.Height= Int32.Parse(parts[1], CultureInfo.InvariantCulture);
            }

            var nodeStageRoot = xml.SelectSingleNode(@"//stages");
            if (nodeStageRoot == null) throw new XmlException("File is not an XML file in OpenCV Haar cascade format");

            foreach (XmlNode nodeStage in nodeStageRoot.ChildNodes)
            {
                var nodeThreshold = nodeStage.SelectSingleNode("stage_threshold");
                if (nodeThreshold == null) throw new XmlException("Stage number " + index + " has no threshold");
                stageThresholds.Add(Double.Parse(nodeThreshold.InnerText.Trim(), CultureInfo.InvariantCulture));

                var nodeTrees = nodeStage.SelectSingleNode("trees");
                if (nodeTrees == null) throw new XmlException("XML node \"trees\" in stage number " + index + " is missing");

                foreach (XmlNode nodeTree in nodeTrees.ChildNodes)
                {
                    var subTrees = nodeTree.ChildNodes.Cast<XmlNode>().Where(x => x.Name == "_");
                    if (subTrees.Count() > 1) throw new NotSupportedException("Haar cascades using sub-trees are not supported");

                    foreach (XmlNode subTree in subTrees)
                    {
                        var nodeSubThreshold = subTree.SelectSingleNode("threshold");
                        if (nodeSubThreshold == null) throw new XmlException("Threshold is missing in tree");

                        var nodeLeftVal = subTree.SelectSingleNode("left_val");
                        if (nodeLeftVal == null) throw new XmlException("Left value is missing in tree");

                        var nodeRightVal = subTree.SelectSingleNode("right_val");
                        if (nodeRightVal == null) throw new XmlException("Right value is missing in tree");

                        var nodeFeature = subTree.SelectSingleNode("feature");
                        if (nodeFeature == null) throw new XmlException("Feature is missing in tree");

                        var nodeTilted = nodeFeature.SelectSingleNode("tilted");
                        if (nodeTilted != null && nodeTilted.InnerText != "0") throw new NotSupportedException("Tilted nodes are not supported");

                        List<HaarRectangle> rectList = new List<HaarRectangle>();
                        var nodeRects = nodeFeature.SelectSingleNode("rects");
                        if (nodeRects != null)
                        {
                            foreach (XmlNode nodeRect in nodeRects.ChildNodes)
                            {
                                string value = nodeRect.InnerText;
                                if (value == null) continue;
                                string[] parts = value.Split(new[] { ' ', '.', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length != 5) throw new XmlException("Rectangle definition \"" + value + "\" is not valid or recognized!");

                                rectList.Add(new HaarRectangle(Int32.Parse(parts[0], CultureInfo.InvariantCulture), Int32.Parse(parts[1], CultureInfo.InvariantCulture),
                                    Int32.Parse(parts[2], CultureInfo.InvariantCulture), Int32.Parse(parts[3], CultureInfo.InvariantCulture), Int32.Parse(parts[4], CultureInfo.InvariantCulture)));
                            }
                        }
                        if (rectList.Count < 2 || rectList.Count > 3) throw new NotSupportedException("Only 2 or 3 rectangles per feature are supported, but I found " + rectList.Count);

                        stageNodes.AddHaarFeature(Double.Parse(nodeSubThreshold.InnerText.Trim(), CultureInfo.InvariantCulture),
                            Double.Parse(nodeLeftVal.InnerText.Trim(), CultureInfo.InvariantCulture),
                            Double.Parse(nodeRightVal.InnerText.Trim(), CultureInfo.InvariantCulture), rectList.ToArray());
                    }
                }
                stageNodeCounts.Add(stageNodes.Count - index); index = stageNodes.Count;
            }

            DetectorData data = new DetectorData();
            data.WindowSize = windowSize;
            data.StageNodes = stageNodes;
            data.StageNodesCount = stageNodeCounts;
            data.StageThresholds = stageThresholds.Select(x => (float)x).ToList();

            return data;
        }

        /// <summary>
        /// Creates the nodes needed to classify faces
        /// </summary>
        /// <returns></returns>
        protected static DetectorData CreateFaceDetector()
        {
            int index = 0;
            List<HaarFeatureNode> stageNodes = new List<HaarFeatureNode>();
            List<int> stageNodeCounts = new List<int>();
            List<double> stageThresholds = new List<double>();

            #region Hardcoded transcription of haarcascade_frontalface_alt.xml based on code by Mario Klingemann
            // Stage 00
            stageThresholds.Add(0.822689414024353);
            stageNodes.AddHaarFeature(0.004014195874333382, 0.0337941907346249, 0.8378106951713562, new HaarRectangle(3, 7, 14, 4, -1), new HaarRectangle(3, 9, 14, 2, 2));
            stageNodes.AddHaarFeature(0.0151513395830989, 0.1514132022857666, 0.7488812208175659, new HaarRectangle(1, 2, 18, 4, -1), new HaarRectangle(7, 2, 6, 4, 3));
            stageNodes.AddHaarFeature(0.004210993181914091, 0.0900492817163467, 0.6374819874763489, new HaarRectangle(1, 7, 15, 9, -1), new HaarRectangle(1, 10, 15, 3, 3));
            stageNodeCounts.Add(stageNodes.Count - index); index = stageNodes.Count;

            // Stage 01
            stageThresholds.Add(6.956608772277832);
            stageNodes.AddHaarFeature(0.0016227109590545297, 0.0693085864186287, 0.7110946178436279, new HaarRectangle(5, 6, 2, 6, -1), new HaarRectangle(5, 9, 2, 3, 2));
            stageNodes.AddHaarFeature(0.002290664939209819, 0.1795803010463715, 0.6668692231178284, new HaarRectangle(7, 5, 6, 3, -1), new HaarRectangle(9, 5, 2, 3, 3));
            stageNodes.AddHaarFeature(0.005002570804208517, 0.1693672984838486, 0.6554006934165955, new HaarRectangle(4, 0, 12, 9, -1), new HaarRectangle(4, 3, 12, 3, 3));
            stageNodes.AddHaarFeature(0.007965989410877228, 0.5866332054138184, 0.0914145186543465, new HaarRectangle(6, 9, 10, 8, -1), new HaarRectangle(6, 13, 10, 4, 2));
            stageNodes.AddHaarFeature(-0.003522701095789671, 0.1413166970014572, 0.6031895875930786, new HaarRectangle(3, 6, 14, 8, -1), new HaarRectangle(3, 10, 14, 4, 2));
            stageNodes.AddHaarFeature(0.0366676896810532, 0.3675672113895416, 0.7920318245887756, new HaarRectangle(14, 1, 6, 10, -1), new HaarRectangle(14, 1, 3, 10, 2));
            stageNodes.AddHaarFeature(0.009336147457361221, 0.6161385774612427, 0.2088509947061539, new HaarRectangle(7, 8, 5, 12, -1), new HaarRectangle(7, 12, 5, 4, 3));
            stageNodes.AddHaarFeature(0.008696131408214569, 0.2836230993270874, 0.6360273957252502, new HaarRectangle(1, 1, 18, 3, -1), new HaarRectangle(7, 1, 6, 3, 3));
            stageNodes.AddHaarFeature(0.0011488880263641477, 0.2223580926656723, 0.5800700783729553, new HaarRectangle(1, 8, 17, 2, -1), new HaarRectangle(1, 9, 17, 1, 2));
            stageNodes.AddHaarFeature(-0.002148468978703022, 0.2406464070081711, 0.5787054896354675, new HaarRectangle(16, 6, 4, 2, -1), new HaarRectangle(16, 7, 4, 1, 2));
            stageNodes.AddHaarFeature(0.0021219060290604834, 0.5559654831886292, 0.136223703622818, new HaarRectangle(5, 17, 2, 2, -1), new HaarRectangle(5, 18, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.0939491465687752, 0.8502737283706665, 0.4717740118503571, new HaarRectangle(14, 2, 6, 12, -1), new HaarRectangle(14, 2, 3, 12, 2));
            stageNodes.AddHaarFeature(0.0013777789426967502, 0.5993673801422119, 0.2834529876708984, new HaarRectangle(4, 0, 4, 12, -1), new HaarRectangle(4, 0, 2, 6, 2), new HaarRectangle(6, 6, 2, 6, 2));
            stageNodes.AddHaarFeature(0.0730631574988365, 0.4341886043548584, 0.7060034275054932, new HaarRectangle(2, 11, 18, 8, -1), new HaarRectangle(8, 11, 6, 8, 3));
            stageNodes.AddHaarFeature(0.00036767389974556863, 0.3027887940406799, 0.6051574945449829, new HaarRectangle(5, 7, 10, 2, -1), new HaarRectangle(5, 8, 10, 1, 2));
            stageNodes.AddHaarFeature(-0.0060479710809886456, 0.17984339594841, 0.5675256848335266, new HaarRectangle(15, 11, 5, 3, -1), new HaarRectangle(15, 12, 5, 1, 3));
            stageNodeCounts.Add(stageNodes.Count - index); index = stageNodes.Count;

            // Stage 02
            stageThresholds.Add(9.498542785644531);
            stageNodes.AddHaarFeature(-0.0165106896311045, 0.6644225120544434, 0.1424857974052429, new HaarRectangle(5, 3, 10, 9, -1), new HaarRectangle(5, 6, 10, 3, 3));
            stageNodes.AddHaarFeature(0.002705249935388565, 0.6325352191925049, 0.1288477033376694, new HaarRectangle(9, 4, 2, 14, -1), new HaarRectangle(9, 11, 2, 7, 2));
            stageNodes.AddHaarFeature(0.002806986914947629, 0.1240288019180298, 0.6193193197250366, new HaarRectangle(3, 5, 4, 12, -1), new HaarRectangle(3, 9, 4, 4, 3));
            stageNodes.AddHaarFeature(-0.0015402400167658925, 0.1432143002748489, 0.5670015811920166, new HaarRectangle(4, 5, 12, 5, -1), new HaarRectangle(8, 5, 4, 5, 3));
            stageNodes.AddHaarFeature(-0.0005638627917505801, 0.1657433062791824, 0.5905207991600037, new HaarRectangle(5, 6, 10, 8, -1), new HaarRectangle(5, 10, 10, 4, 2));
            stageNodes.AddHaarFeature(0.0019253729842603209, 0.2695507109165192, 0.5738824009895325, new HaarRectangle(8, 0, 6, 9, -1), new HaarRectangle(8, 3, 6, 3, 3));
            stageNodes.AddHaarFeature(-0.005021484103053808, 0.1893538981676102, 0.5782774090766907, new HaarRectangle(9, 12, 1, 8, -1), new HaarRectangle(9, 16, 1, 4, 2));
            stageNodes.AddHaarFeature(0.002636542078107595, 0.2309329062700272, 0.5695425868034363, new HaarRectangle(0, 7, 20, 6, -1), new HaarRectangle(0, 9, 20, 2, 3));
            stageNodes.AddHaarFeature(-0.0015127769438549876, 0.2759602069854736, 0.5956642031669617, new HaarRectangle(7, 0, 6, 17, -1), new HaarRectangle(9, 0, 2, 17, 3));
            stageNodes.AddHaarFeature(-0.0101574398577213, 0.1732538044452667, 0.5522047281265259, new HaarRectangle(9, 0, 6, 4, -1), new HaarRectangle(11, 0, 2, 4, 3));
            stageNodes.AddHaarFeature(-0.011953660286963, 0.1339409947395325, 0.5559014081954956, new HaarRectangle(5, 1, 6, 4, -1), new HaarRectangle(7, 1, 2, 4, 3));
            stageNodes.AddHaarFeature(0.004885949194431305, 0.3628703951835632, 0.6188849210739136, new HaarRectangle(12, 1, 6, 16, -1), new HaarRectangle(14, 1, 2, 16, 3));
            stageNodes.AddHaarFeature(-0.0801329165697098, 0.0912110507488251, 0.5475944876670837, new HaarRectangle(0, 5, 18, 8, -1), new HaarRectangle(0, 5, 9, 4, 2), new HaarRectangle(9, 9, 9, 4, 2));
            stageNodes.AddHaarFeature(0.0010643280111253262, 0.3715142905712128, 0.5711399912834167, new HaarRectangle(8, 15, 10, 4, -1), new HaarRectangle(13, 15, 5, 2, 2), new HaarRectangle(8, 17, 5, 2, 2));
            stageNodes.AddHaarFeature(-0.0013419450260698795, 0.5953313708305359, 0.331809788942337, new HaarRectangle(3, 1, 4, 8, -1), new HaarRectangle(3, 1, 2, 4, 2), new HaarRectangle(5, 5, 2, 4, 2));
            stageNodes.AddHaarFeature(-0.0546011403203011, 0.1844065934419632, 0.5602846145629883, new HaarRectangle(3, 6, 14, 10, -1), new HaarRectangle(10, 6, 7, 5, 2), new HaarRectangle(3, 11, 7, 5, 2));
            stageNodes.AddHaarFeature(0.0029071690514683723, 0.3594244122505188, 0.6131715178489685, new HaarRectangle(2, 1, 6, 16, -1), new HaarRectangle(4, 1, 2, 16, 3));
            stageNodes.AddHaarFeature(0.0007471871795132757, 0.5994353294372559, 0.3459562957286835, new HaarRectangle(0, 18, 20, 2, -1), new HaarRectangle(0, 19, 20, 1, 2));
            stageNodes.AddHaarFeature(0.004301380831748247, 0.4172652065753937, 0.6990845203399658, new HaarRectangle(8, 13, 4, 3, -1), new HaarRectangle(8, 14, 4, 1, 3));
            stageNodes.AddHaarFeature(0.004501757211983204, 0.4509715139865875, 0.7801457047462463, new HaarRectangle(9, 14, 2, 3, -1), new HaarRectangle(9, 15, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0241385009139776, 0.5438212752342224, 0.1319826990365982, new HaarRectangle(0, 12, 9, 6, -1), new HaarRectangle(0, 14, 9, 2, 3));
            stageNodeCounts.Add(stageNodes.Count - index); index = stageNodes.Count;

            // Stage 03
            stageThresholds.Add(18.4129695892334);
            stageNodes.AddHaarFeature(0.001921223010867834, 0.1415266990661621, 0.6199870705604553, new HaarRectangle(5, 7, 3, 4, -1), new HaarRectangle(5, 9, 3, 2, 2));
            stageNodes.AddHaarFeature(-0.00012748669541906563, 0.6191074252128601, 0.1884928941726685, new HaarRectangle(9, 3, 2, 16, -1), new HaarRectangle(9, 11, 2, 8, 2));
            stageNodes.AddHaarFeature(0.0005140993162058294, 0.1487396955490112, 0.5857927799224854, new HaarRectangle(3, 6, 13, 8, -1), new HaarRectangle(3, 10, 13, 4, 2));
            stageNodes.AddHaarFeature(0.004187860991805792, 0.2746909856796265, 0.6359239816665649, new HaarRectangle(12, 3, 8, 2, -1), new HaarRectangle(12, 3, 4, 2, 2));
            stageNodes.AddHaarFeature(0.005101571790874005, 0.5870851278305054, 0.2175628989934921, new HaarRectangle(8, 8, 4, 12, -1), new HaarRectangle(8, 12, 4, 4, 3));
            stageNodes.AddHaarFeature(-0.002144844038411975, 0.5880944728851318, 0.2979590892791748, new HaarRectangle(11, 3, 8, 6, -1), new HaarRectangle(15, 3, 4, 3, 2), new HaarRectangle(11, 6, 4, 3, 2));
            stageNodes.AddHaarFeature(-0.0028977119363844395, 0.2373327016830444, 0.5876647233963013, new HaarRectangle(7, 1, 6, 19, -1), new HaarRectangle(9, 1, 2, 19, 3));
            stageNodes.AddHaarFeature(-0.0216106791049242, 0.1220654994249344, 0.5194202065467834, new HaarRectangle(9, 0, 6, 4, -1), new HaarRectangle(11, 0, 2, 4, 3));
            stageNodes.AddHaarFeature(-0.004629931878298521, 0.263123095035553, 0.5817409157752991, new HaarRectangle(3, 1, 9, 3, -1), new HaarRectangle(6, 1, 3, 3, 3));
            stageNodes.AddHaarFeature(0.000593937118537724, 0.363862007856369, 0.5698544979095459, new HaarRectangle(8, 15, 10, 4, -1), new HaarRectangle(13, 15, 5, 2, 2), new HaarRectangle(8, 17, 5, 2, 2));
            stageNodes.AddHaarFeature(0.0538786612451077, 0.4303531050682068, 0.7559366226196289, new HaarRectangle(0, 3, 6, 10, -1), new HaarRectangle(3, 3, 3, 10, 2));
            stageNodes.AddHaarFeature(0.0018887349870055912, 0.2122603058815002, 0.561342716217041, new HaarRectangle(3, 4, 15, 15, -1), new HaarRectangle(3, 9, 15, 5, 3));
            stageNodes.AddHaarFeature(-0.0023635339457541704, 0.563184916973114, 0.2642767131328583, new HaarRectangle(6, 5, 8, 6, -1), new HaarRectangle(6, 7, 8, 2, 3));
            stageNodes.AddHaarFeature(0.0240177996456623, 0.5797107815742493, 0.2751705944538117, new HaarRectangle(4, 4, 12, 10, -1), new HaarRectangle(10, 4, 6, 5, 2), new HaarRectangle(4, 9, 6, 5, 2));
            stageNodes.AddHaarFeature(0.00020543030404951423, 0.2705242037773132, 0.575256884098053, new HaarRectangle(6, 4, 4, 4, -1), new HaarRectangle(8, 4, 2, 4, 2));
            stageNodes.AddHaarFeature(0.0008479019743390381, 0.5435624718666077, 0.2334876954555512, new HaarRectangle(15, 11, 1, 2, -1), new HaarRectangle(15, 12, 1, 1, 2));
            stageNodes.AddHaarFeature(0.001409132964909077, 0.5319424867630005, 0.2063155025243759, new HaarRectangle(3, 11, 2, 2, -1), new HaarRectangle(3, 12, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0014642629539594054, 0.5418980717658997, 0.3068861067295075, new HaarRectangle(16, 11, 1, 3, -1), new HaarRectangle(16, 12, 1, 1, 3));
            stageNodes.AddHaarFeature(0.0016352549428120255, 0.3695372939109802, 0.6112868189811707, new HaarRectangle(3, 15, 6, 4, -1), new HaarRectangle(3, 15, 3, 2, 2), new HaarRectangle(6, 17, 3, 2, 2));
            stageNodes.AddHaarFeature(0.0008317275205627083, 0.3565036952495575, 0.6025236248970032, new HaarRectangle(6, 7, 8, 2, -1), new HaarRectangle(6, 8, 8, 1, 2));
            stageNodes.AddHaarFeature(-0.0020998890977352858, 0.1913982033729553, 0.5362827181816101, new HaarRectangle(3, 11, 1, 3, -1), new HaarRectangle(3, 12, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.0007421398186124861, 0.3835555016994476, 0.552931010723114, new HaarRectangle(6, 0, 12, 2, -1), new HaarRectangle(6, 1, 12, 1, 2));
            stageNodes.AddHaarFeature(0.0014642629539594054, 0.5418980717658997, 0.3068861067295075, new HaarRectangle(16, 11, 1, 3, -1), new HaarRectangle(16, 12, 1, 1, 3));
            stageNodes.AddHaarFeature(0.0016352549428120255, 0.3695372939109802, 0.6112868189811707, new HaarRectangle(3, 15, 6, 4, -1), new HaarRectangle(3, 15, 3, 2, 2), new HaarRectangle(6, 17, 3, 2, 2));
            stageNodes.AddHaarFeature(0.0008317275205627083, 0.3565036952495575, 0.6025236248970032, new HaarRectangle(6, 7, 8, 2, -1), new HaarRectangle(6, 8, 8, 1, 2));
            stageNodes.AddHaarFeature(-0.0020998890977352858, 0.1913982033729553, 0.5362827181816101, new HaarRectangle(3, 11, 1, 3, -1), new HaarRectangle(3, 12, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.0007421398186124861, 0.3835555016994476, 0.552931010723114, new HaarRectangle(6, 0, 12, 2, -1), new HaarRectangle(6, 1, 12, 1, 2));
            stageNodes.AddHaarFeature(0.0032655049581080675, 0.4312896132469177, 0.7101895809173584, new HaarRectangle(9, 14, 2, 3, -1), new HaarRectangle(9, 15, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0008913499186746776, 0.3984830975532532, 0.6391963958740234, new HaarRectangle(7, 15, 6, 2, -1), new HaarRectangle(7, 16, 6, 1, 2));
            stageNodes.AddHaarFeature(-0.0152841797098517, 0.2366732954978943, 0.5433713793754578, new HaarRectangle(0, 5, 4, 6, -1), new HaarRectangle(0, 7, 4, 2, 3));
            stageNodes.AddHaarFeature(0.004838141147047281, 0.5817500948905945, 0.3239189088344574, new HaarRectangle(4, 12, 12, 2, -1), new HaarRectangle(8, 12, 4, 2, 3));
            stageNodes.AddHaarFeature(-0.0009109317907132208, 0.5540593862533569, 0.2911868989467621, new HaarRectangle(6, 3, 1, 9, -1), new HaarRectangle(6, 6, 1, 3, 3));
            stageNodes.AddHaarFeature(-0.006127506028860807, 0.1775255054235458, 0.5196629166603088, new HaarRectangle(10, 17, 3, 2, -1), new HaarRectangle(11, 17, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.000445762590970844, 0.3024170100688934, 0.5533593893051147, new HaarRectangle(9, 9, 2, 2, -1), new HaarRectangle(9, 10, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0226465407758951, 0.4414930939674377, 0.6975377202033997, new HaarRectangle(7, 6, 6, 4, -1), new HaarRectangle(9, 6, 2, 4, 3));
            stageNodes.AddHaarFeature(-0.0018804960418492556, 0.2791394889354706, 0.5497952103614807, new HaarRectangle(7, 17, 3, 2, -1), new HaarRectangle(8, 17, 1, 2, 3));
            stageNodes.AddHaarFeature(0.007088910788297653, 0.5263199210166931, 0.2385547012090683, new HaarRectangle(10, 17, 3, 3, -1), new HaarRectangle(11, 17, 1, 3, 3));
            stageNodes.AddHaarFeature(0.0017318050377070904, 0.4319379031658173, 0.6983600854873657, new HaarRectangle(8, 12, 3, 2, -1), new HaarRectangle(8, 13, 3, 1, 2));
            stageNodes.AddHaarFeature(-0.006848270073533058, 0.3082042932510376, 0.5390920042991638, new HaarRectangle(9, 3, 6, 2, -1), new HaarRectangle(11, 3, 2, 2, 3));
            stageNodes.AddHaarFeature(-0.000015062530110299122, 0.552192211151123, 0.3120366036891937, new HaarRectangle(3, 11, 14, 4, -1), new HaarRectangle(3, 13, 14, 2, 2));
            stageNodes.AddHaarFeature(0.0294755697250366, 0.5401322841644287, 0.1770603060722351, new HaarRectangle(1, 10, 18, 4, -1), new HaarRectangle(10, 10, 9, 2, 2), new HaarRectangle(1, 12, 9, 2, 2));
            stageNodes.AddHaarFeature(0.008138732984662056, 0.5178617835044861, 0.121101900935173, new HaarRectangle(0, 10, 3, 3, -1), new HaarRectangle(0, 11, 3, 1, 3));
            stageNodes.AddHaarFeature(0.0209429506212473, 0.5290294289588928, 0.3311221897602081, new HaarRectangle(9, 1, 6, 6, -1), new HaarRectangle(11, 1, 2, 6, 3));
            stageNodes.AddHaarFeature(-0.009566552937030792, 0.7471994161605835, 0.4451968967914581, new HaarRectangle(8, 7, 3, 6, -1), new HaarRectangle(9, 7, 1, 6, 3));
            stageNodeCounts.Add(stageNodes.Count - index); index = stageNodes.Count;

            stageThresholds.Add(15.324139595031738);
            stageNodes.AddHaarFeature(-0.00028206960996612906, 0.2064086049795151, 0.6076732277870178, new HaarRectangle(1, 0, 18, 9, -1), new HaarRectangle(1, 3, 18, 3, 3));
            stageNodes.AddHaarFeature(0.00167906004935503, 0.5851997137069702, 0.1255383938550949, new HaarRectangle(12, 10, 2, 6, -1), new HaarRectangle(12, 13, 2, 3, 2));
            stageNodes.AddHaarFeature(0.0006982791237533092, 0.094018429517746, 0.5728961229324341, new HaarRectangle(0, 5, 19, 8, -1), new HaarRectangle(0, 9, 19, 4, 2));
            stageNodes.AddHaarFeature(0.0007895901217125357, 0.1781987994909287, 0.5694308876991272, new HaarRectangle(7, 0, 6, 9, -1), new HaarRectangle(9, 0, 2, 9, 3));
            stageNodes.AddHaarFeature(-0.002856049919500947, 0.1638399064540863, 0.5788664817810059, new HaarRectangle(5, 3, 6, 1, -1), new HaarRectangle(7, 3, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0038122469559311862, 0.2085440009832382, 0.5508564710617065, new HaarRectangle(11, 3, 6, 1, -1), new HaarRectangle(13, 3, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0015896620461717248, 0.5702760815620422, 0.1857215017080307, new HaarRectangle(5, 10, 4, 6, -1), new HaarRectangle(5, 13, 4, 3, 2));
            stageNodes.AddHaarFeature(0.0100783398374915, 0.5116943120956421, 0.2189770042896271, new HaarRectangle(11, 3, 6, 1, -1), new HaarRectangle(13, 3, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0635263025760651, 0.7131379842758179, 0.4043813049793243, new HaarRectangle(4, 4, 12, 6, -1), new HaarRectangle(4, 6, 12, 2, 3));
            stageNodes.AddHaarFeature(-0.009103149175643921, 0.2567181885242462, 0.54639732837677, new HaarRectangle(15, 12, 2, 6, -1), new HaarRectangle(15, 14, 2, 2, 3));
            stageNodes.AddHaarFeature(-0.002403500024229288, 0.1700665950775147, 0.559097409248352, new HaarRectangle(9, 3, 2, 2, -1), new HaarRectangle(10, 3, 1, 2, 2));
            stageNodes.AddHaarFeature(0.001522636041045189, 0.5410556793212891, 0.2619054019451141, new HaarRectangle(9, 3, 3, 1, -1), new HaarRectangle(10, 3, 1, 1, 3));
            stageNodes.AddHaarFeature(0.0179974399507046, 0.3732436895370483, 0.6535220742225647, new HaarRectangle(1, 1, 4, 14, -1), new HaarRectangle(3, 1, 2, 14, 2));
            stageNodes.AddHaarFeature(-0.00645381910726428, 0.2626481950283051, 0.5537446141242981, new HaarRectangle(9, 0, 4, 4, -1), new HaarRectangle(11, 0, 2, 2, 2), new HaarRectangle(9, 2, 2, 2, 2));
            stageNodes.AddHaarFeature(-0.0118807600811124, 0.2003753930330277, 0.5544745922088623, new HaarRectangle(7, 5, 1, 14, -1), new HaarRectangle(7, 12, 1, 7, 2));
            stageNodes.AddHaarFeature(0.0012713660253211856, 0.5591902732849121, 0.303197592496872, new HaarRectangle(19, 0, 1, 4, -1), new HaarRectangle(19, 2, 1, 2, 2));
            stageNodes.AddHaarFeature(0.0011376109905540943, 0.2730407118797302, 0.5646508932113647, new HaarRectangle(5, 5, 6, 4, -1), new HaarRectangle(8, 5, 3, 4, 2));
            stageNodes.AddHaarFeature(-0.00426519988104701, 0.1405909061431885, 0.5461820960044861, new HaarRectangle(9, 18, 3, 2, -1), new HaarRectangle(10, 18, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.0029602861031889915, 0.1795035004615784, 0.5459290146827698, new HaarRectangle(8, 18, 3, 2, -1), new HaarRectangle(9, 18, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.008844822645187378, 0.5736783146858215, 0.280921995639801, new HaarRectangle(4, 5, 12, 6, -1), new HaarRectangle(4, 7, 12, 2, 3));
            stageNodes.AddHaarFeature(-0.006643068976700306, 0.2370675951242447, 0.5503826141357422, new HaarRectangle(3, 12, 2, 6, -1), new HaarRectangle(3, 14, 2, 2, 3));
            stageNodes.AddHaarFeature(0.003999780863523483, 0.5608199834823608, 0.3304282128810883, new HaarRectangle(10, 8, 2, 12, -1), new HaarRectangle(10, 12, 2, 4, 3));
            stageNodes.AddHaarFeature(-0.004122172016650438, 0.1640105992555618, 0.5378993153572083, new HaarRectangle(7, 18, 3, 2, -1), new HaarRectangle(8, 18, 1, 2, 3));
            stageNodes.AddHaarFeature(0.0156249096617103, 0.5227649211883545, 0.2288603931665421, new HaarRectangle(9, 0, 6, 2, -1), new HaarRectangle(11, 0, 2, 2, 3));
            stageNodes.AddHaarFeature(-0.0103564197197557, 0.7016193866729736, 0.4252927899360657, new HaarRectangle(5, 11, 9, 3, -1), new HaarRectangle(5, 12, 9, 1, 3));
            stageNodes.AddHaarFeature(-0.008796080946922302, 0.2767347097396851, 0.5355830192565918, new HaarRectangle(9, 0, 6, 2, -1), new HaarRectangle(11, 0, 2, 2, 3));
            stageNodes.AddHaarFeature(0.1622693985700607, 0.434224009513855, 0.744257926940918, new HaarRectangle(1, 1, 18, 5, -1), new HaarRectangle(7, 1, 6, 5, 3));
            stageNodes.AddHaarFeature(0.0045542530715465546, 0.5726485848426819, 0.2582125067710877, new HaarRectangle(8, 0, 4, 4, -1), new HaarRectangle(10, 0, 2, 2, 2), new HaarRectangle(8, 2, 2, 2, 2));
            stageNodes.AddHaarFeature(-0.002130920998752117, 0.2106848061084747, 0.5361018776893616, new HaarRectangle(3, 12, 1, 3, -1), new HaarRectangle(3, 13, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.0132084200158715, 0.7593790888786316, 0.4552468061447144, new HaarRectangle(8, 14, 5, 3, -1), new HaarRectangle(8, 15, 5, 1, 3));
            stageNodes.AddHaarFeature(-0.0659966766834259, 0.125247597694397, 0.5344039797782898, new HaarRectangle(5, 4, 10, 12, -1), new HaarRectangle(5, 4, 5, 6, 2), new HaarRectangle(10, 10, 5, 6, 2));
            stageNodes.AddHaarFeature(0.007914265617728233, 0.3315384089946747, 0.5601043105125427, new HaarRectangle(9, 6, 9, 12, -1), new HaarRectangle(9, 10, 9, 4, 3));
            stageNodes.AddHaarFeature(0.0208942797034979, 0.5506049990653992, 0.2768838107585907, new HaarRectangle(2, 2, 12, 14, -1), new HaarRectangle(2, 2, 6, 7, 2), new HaarRectangle(8, 9, 6, 7, 2));
            stageNodeCounts.Add(stageNodes.Count - index); index = stageNodes.Count;

            stageThresholds.Add(21.010639190673828);
            stageNodes.AddHaarFeature(0.0011961159761995075, 0.1762690991163254, 0.6156241297721863, new HaarRectangle(4, 7, 12, 2, -1), new HaarRectangle(8, 7, 4, 2, 3));
            stageNodes.AddHaarFeature(-0.0018679830245673656, 0.6118106842041016, 0.1832399964332581, new HaarRectangle(7, 4, 6, 4, -1), new HaarRectangle(7, 6, 6, 2, 2));
            stageNodes.AddHaarFeature(-0.00019579799845814705, 0.0990442633628845, 0.5723816156387329, new HaarRectangle(4, 5, 11, 8, -1), new HaarRectangle(4, 9, 11, 4, 2));
            stageNodes.AddHaarFeature(-0.0008025565766729414, 0.5579879879951477, 0.2377282977104187, new HaarRectangle(3, 10, 16, 4, -1), new HaarRectangle(3, 12, 16, 2, 2));
            stageNodes.AddHaarFeature(-0.0024510810617357492, 0.2231457978487015, 0.5858935117721558, new HaarRectangle(0, 0, 16, 2, -1), new HaarRectangle(0, 1, 16, 1, 2));
            stageNodes.AddHaarFeature(0.0005036185029894114, 0.2653993964195252, 0.5794103741645813, new HaarRectangle(7, 5, 6, 2, -1), new HaarRectangle(9, 5, 2, 2, 3));
            stageNodes.AddHaarFeature(0.0040293349884450436, 0.5803827047348022, 0.2484865039587021, new HaarRectangle(3, 2, 6, 10, -1), new HaarRectangle(3, 2, 3, 5, 2), new HaarRectangle(6, 7, 3, 5, 2));
            stageNodes.AddHaarFeature(-0.0144517095759511, 0.1830351948738098, 0.5484204888343811, new HaarRectangle(10, 5, 8, 15, -1), new HaarRectangle(10, 10, 8, 5, 3));
            stageNodes.AddHaarFeature(0.0020380979403853416, 0.3363558948040009, 0.6051092743873596, new HaarRectangle(3, 14, 8, 6, -1), new HaarRectangle(3, 14, 4, 3, 2), new HaarRectangle(7, 17, 4, 3, 2));
            stageNodes.AddHaarFeature(-0.0016155190533027053, 0.2286642044782639, 0.5441246032714844, new HaarRectangle(14, 2, 2, 2, -1), new HaarRectangle(14, 3, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0033458340913057327, 0.5625913143157959, 0.2392338067293167, new HaarRectangle(1, 10, 7, 6, -1), new HaarRectangle(1, 13, 7, 3, 2));
            stageNodes.AddHaarFeature(0.0016379579901695251, 0.3906993865966797, 0.5964621901512146, new HaarRectangle(15, 4, 4, 3, -1), new HaarRectangle(15, 4, 2, 3, 2));
            stageNodes.AddHaarFeature(0.0302512105554342, 0.524848222732544, 0.1575746983289719, new HaarRectangle(2, 9, 14, 6, -1), new HaarRectangle(2, 9, 7, 3, 2), new HaarRectangle(9, 12, 7, 3, 2));
            stageNodes.AddHaarFeature(0.037251990288496, 0.4194310903549194, 0.6748418807983398, new HaarRectangle(5, 7, 10, 4, -1), new HaarRectangle(5, 9, 10, 2, 2));
            stageNodes.AddHaarFeature(-0.0251097902655602, 0.1882549971342087, 0.5473451018333435, new HaarRectangle(6, 9, 8, 8, -1), new HaarRectangle(6, 9, 4, 4, 2), new HaarRectangle(10, 13, 4, 4, 2));
            stageNodes.AddHaarFeature(-0.005309905856847763, 0.133997306227684, 0.5227110981941223, new HaarRectangle(14, 1, 3, 2, -1), new HaarRectangle(14, 2, 3, 1, 2));
            stageNodes.AddHaarFeature(0.0012086479691788554, 0.3762088119983673, 0.6109635829925537, new HaarRectangle(1, 4, 4, 2, -1), new HaarRectangle(3, 4, 2, 2, 2));
            stageNodes.AddHaarFeature(-0.0219076797366142, 0.266314297914505, 0.5404006838798523, new HaarRectangle(11, 10, 2, 8, -1), new HaarRectangle(11, 14, 2, 4, 2));
            stageNodes.AddHaarFeature(0.0054116579703986645, 0.5363578796386719, 0.2232273072004318, new HaarRectangle(0, 0, 5, 3, -1), new HaarRectangle(0, 1, 5, 1, 3));
            stageNodes.AddHaarFeature(0.069946326315403, 0.5358232855796814, 0.2453698068857193, new HaarRectangle(2, 5, 18, 8, -1), new HaarRectangle(11, 5, 9, 4, 2), new HaarRectangle(2, 9, 9, 4, 2));
            stageNodes.AddHaarFeature(0.00034520021290518343, 0.2409671992063522, 0.5376930236816406, new HaarRectangle(6, 6, 1, 6, -1), new HaarRectangle(6, 9, 1, 3, 2));
            stageNodes.AddHaarFeature(0.0012627709656953812, 0.5425856709480286, 0.3155693113803864, new HaarRectangle(19, 1, 1, 3, -1), new HaarRectangle(19, 2, 1, 1, 3));
            stageNodes.AddHaarFeature(0.0227195098996162, 0.4158405959606171, 0.6597865223884583, new HaarRectangle(7, 6, 6, 6, -1), new HaarRectangle(9, 6, 2, 6, 3));
            stageNodes.AddHaarFeature(-0.0018111000536009667, 0.2811253070831299, 0.5505244731903076, new HaarRectangle(19, 1, 1, 3, -1), new HaarRectangle(19, 2, 1, 1, 3));
            stageNodes.AddHaarFeature(0.0033469670452177525, 0.526002824306488, 0.1891465038061142, new HaarRectangle(3, 13, 2, 3, -1), new HaarRectangle(3, 14, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0004079175123479218, 0.5673509240150452, 0.3344210088253021, new HaarRectangle(8, 4, 8, 12, -1), new HaarRectangle(12, 4, 4, 6, 2), new HaarRectangle(8, 10, 4, 6, 2));
            stageNodes.AddHaarFeature(0.0127347996458411, 0.5343592166900635, 0.2395612001419067, new HaarRectangle(5, 2, 6, 3, -1), new HaarRectangle(7, 2, 2, 3, 3));
            stageNodes.AddHaarFeature(-0.007311972789466381, 0.6010890007019043, 0.4022207856178284, new HaarRectangle(6, 1, 9, 10, -1), new HaarRectangle(6, 6, 9, 5, 2));
            stageNodes.AddHaarFeature(-0.0569487512111664, 0.8199151158332825, 0.4543190896511078, new HaarRectangle(0, 4, 6, 12, -1), new HaarRectangle(2, 4, 2, 12, 3));
            stageNodes.AddHaarFeature(-0.005011659115552902, 0.2200281023979187, 0.5357710719108582, new HaarRectangle(15, 13, 2, 3, -1), new HaarRectangle(15, 14, 2, 1, 3));
            stageNodes.AddHaarFeature(0.006033436860889196, 0.4413081109523773, 0.7181751132011414, new HaarRectangle(7, 14, 5, 3, -1), new HaarRectangle(7, 15, 5, 1, 3));
            stageNodes.AddHaarFeature(0.0039437441155314445, 0.547886073589325, 0.2791733145713806, new HaarRectangle(15, 13, 3, 3, -1), new HaarRectangle(15, 14, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.0036591119132936, 0.635786771774292, 0.3989723920822144, new HaarRectangle(6, 14, 8, 3, -1), new HaarRectangle(6, 15, 8, 1, 3));
            stageNodes.AddHaarFeature(-0.003845618106424809, 0.3493686020374298, 0.5300664901733398, new HaarRectangle(15, 13, 3, 3, -1), new HaarRectangle(15, 14, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.007192626129835844, 0.1119614988565445, 0.5229672789573669, new HaarRectangle(2, 13, 3, 3, -1), new HaarRectangle(2, 14, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.0527989417314529, 0.2387102991342545, 0.54534512758255, new HaarRectangle(4, 7, 12, 12, -1), new HaarRectangle(10, 7, 6, 6, 2), new HaarRectangle(4, 13, 6, 6, 2));
            stageNodes.AddHaarFeature(-0.007953766733407974, 0.7586917877197266, 0.4439376890659332, new HaarRectangle(9, 7, 2, 6, -1), new HaarRectangle(10, 7, 1, 6, 2));
            stageNodes.AddHaarFeature(-0.0027344180271029472, 0.2565476894378662, 0.5489321947097778, new HaarRectangle(8, 9, 5, 2, -1), new HaarRectangle(8, 10, 5, 1, 2));
            stageNodes.AddHaarFeature(-0.0018507939530536532, 0.6734347939491272, 0.4252474904060364, new HaarRectangle(8, 6, 3, 4, -1), new HaarRectangle(9, 6, 1, 4, 3));
            stageNodes.AddHaarFeature(0.0159189198166132, 0.548835277557373, 0.2292661964893341, new HaarRectangle(9, 6, 2, 8, -1), new HaarRectangle(9, 10, 2, 4, 2));
            stageNodes.AddHaarFeature(-0.001268767984583974, 0.6104331016540527, 0.4022389948368073, new HaarRectangle(7, 7, 3, 6, -1), new HaarRectangle(8, 7, 1, 6, 3));
            stageNodes.AddHaarFeature(0.006288391072303057, 0.5310853123664856, 0.1536193042993546, new HaarRectangle(11, 3, 3, 3, -1), new HaarRectangle(12, 3, 1, 3, 3));
            stageNodes.AddHaarFeature(-0.0062259892001748085, 0.1729111969470978, 0.524160623550415, new HaarRectangle(5, 4, 6, 1, -1), new HaarRectangle(7, 4, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0121325999498367, 0.659775972366333, 0.4325182139873505, new HaarRectangle(5, 6, 10, 3, -1), new HaarRectangle(5, 7, 10, 1, 3));
            stageNodeCounts.Add(stageNodes.Count - index); index = stageNodes.Count;

            stageThresholds.Add(23.918790817260742);
            stageNodes.AddHaarFeature(-0.0039184908382594585, 0.6103435158729553, 0.1469330936670303, new HaarRectangle(7, 3, 6, 9, -1), new HaarRectangle(7, 6, 6, 3, 3));
            stageNodes.AddHaarFeature(0.0015971299726516008, 0.2632363140583038, 0.5896466970443726, new HaarRectangle(6, 7, 9, 1, -1), new HaarRectangle(9, 7, 3, 1, 3));
            stageNodes.AddHaarFeature(0.0177801102399826, 0.587287425994873, 0.1760361939668655, new HaarRectangle(2, 8, 16, 8, -1), new HaarRectangle(2, 12, 16, 4, 2));
            stageNodes.AddHaarFeature(0.0006533476989716291, 0.1567801982164383, 0.5596066117286682, new HaarRectangle(14, 6, 2, 6, -1), new HaarRectangle(14, 9, 2, 3, 2));
            stageNodes.AddHaarFeature(-0.00028353091329336166, 0.1913153976202011, 0.5732036232948303, new HaarRectangle(1, 5, 6, 15, -1), new HaarRectangle(1, 10, 6, 5, 3));
            stageNodes.AddHaarFeature(0.0016104689566418529, 0.2914913892745972, 0.5623080730438232, new HaarRectangle(10, 0, 6, 9, -1), new HaarRectangle(10, 3, 6, 3, 3));
            stageNodes.AddHaarFeature(-0.0977506190538406, 0.194347694516182, 0.5648233294487, new HaarRectangle(6, 6, 7, 14, -1), new HaarRectangle(6, 13, 7, 7, 2));
            stageNodes.AddHaarFeature(0.0005518235848285258, 0.3134616911411285, 0.5504639744758606, new HaarRectangle(13, 7, 3, 6, -1), new HaarRectangle(13, 9, 3, 2, 3));
            stageNodes.AddHaarFeature(-0.0128582203760743, 0.253648191690445, 0.5760142803192139, new HaarRectangle(1, 8, 15, 4, -1), new HaarRectangle(6, 8, 5, 4, 3));
            stageNodes.AddHaarFeature(0.004153023939579725, 0.5767722129821777, 0.36597740650177, new HaarRectangle(11, 2, 3, 10, -1), new HaarRectangle(11, 7, 3, 5, 2));
            stageNodes.AddHaarFeature(0.0017092459602281451, 0.2843191027641296, 0.5918939113616943, new HaarRectangle(3, 7, 4, 6, -1), new HaarRectangle(3, 9, 4, 2, 3));
            stageNodes.AddHaarFeature(0.007521735969930886, 0.4052427113056183, 0.6183109283447266, new HaarRectangle(13, 3, 6, 10, -1), new HaarRectangle(15, 3, 2, 10, 3));
            stageNodes.AddHaarFeature(0.0022479810286313295, 0.578375518321991, 0.3135401010513306, new HaarRectangle(5, 7, 8, 10, -1), new HaarRectangle(5, 7, 4, 5, 2), new HaarRectangle(9, 12, 4, 5, 2));
            stageNodes.AddHaarFeature(0.0520062111318111, 0.5541312098503113, 0.1916636973619461, new HaarRectangle(4, 4, 12, 12, -1), new HaarRectangle(10, 4, 6, 6, 2), new HaarRectangle(4, 10, 6, 6, 2));
            stageNodes.AddHaarFeature(0.0120855299755931, 0.4032655954360962, 0.6644591093063354, new HaarRectangle(1, 4, 6, 9, -1), new HaarRectangle(3, 4, 2, 9, 3));
            stageNodes.AddHaarFeature(0.00001468782011215808, 0.3535977900028229, 0.5709382891654968, new HaarRectangle(11, 3, 2, 5, -1), new HaarRectangle(11, 3, 1, 5, 2));
            stageNodes.AddHaarFeature(0.000007139518857002258, 0.3037444949150085, 0.5610269904136658, new HaarRectangle(7, 3, 2, 5, -1), new HaarRectangle(8, 3, 1, 5, 2));
            stageNodes.AddHaarFeature(-0.0046001640148460865, 0.7181087136268616, 0.4580326080322266, new HaarRectangle(10, 14, 2, 3, -1), new HaarRectangle(10, 15, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0020058949012309313, 0.5621951818466187, 0.2953684031963348, new HaarRectangle(5, 12, 6, 2, -1), new HaarRectangle(8, 12, 3, 2, 2));
            stageNodes.AddHaarFeature(0.004505027085542679, 0.4615387916564941, 0.7619017958641052, new HaarRectangle(9, 14, 2, 3, -1), new HaarRectangle(9, 15, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0117468303069472, 0.5343837141990662, 0.1772529035806656, new HaarRectangle(4, 11, 12, 6, -1), new HaarRectangle(4, 14, 12, 3, 2));
            stageNodes.AddHaarFeature(-0.0583163388073444, 0.1686245948076248, 0.5340772271156311, new HaarRectangle(11, 11, 5, 9, -1), new HaarRectangle(11, 14, 5, 3, 3));
            stageNodes.AddHaarFeature(0.00023629379575140777, 0.3792056143283844, 0.6026803851127625, new HaarRectangle(6, 15, 3, 2, -1), new HaarRectangle(6, 16, 3, 1, 2));
            stageNodes.AddHaarFeature(-0.007815618067979813, 0.151286706328392, 0.5324323773384094, new HaarRectangle(11, 0, 3, 5, -1), new HaarRectangle(12, 0, 1, 5, 3));
            stageNodes.AddHaarFeature(-0.0108761601150036, 0.2081822007894516, 0.5319945216178894, new HaarRectangle(5, 5, 6, 7, -1), new HaarRectangle(8, 5, 3, 7, 2));
            stageNodes.AddHaarFeature(-0.0027745519764721394, 0.4098246991634369, 0.5210328102111816, new HaarRectangle(13, 0, 1, 9, -1), new HaarRectangle(13, 3, 1, 3, 3));
            stageNodes.AddHaarFeature(-0.0007827638182789088, 0.5693274140357971, 0.3478842079639435, new HaarRectangle(3, 2, 4, 8, -1), new HaarRectangle(3, 2, 2, 4, 2), new HaarRectangle(5, 6, 2, 4, 2));
            stageNodes.AddHaarFeature(0.0138704096898437, 0.5326750874519348, 0.2257698029279709, new HaarRectangle(13, 12, 4, 6, -1), new HaarRectangle(13, 14, 4, 2, 3));
            stageNodes.AddHaarFeature(-0.0236749108880758, 0.1551305055618286, 0.5200707912445068, new HaarRectangle(3, 12, 4, 6, -1), new HaarRectangle(3, 14, 4, 2, 3));
            stageNodes.AddHaarFeature(-0.000014879409718560057, 0.5500566959381104, 0.3820176124572754, new HaarRectangle(13, 11, 3, 4, -1), new HaarRectangle(13, 13, 3, 2, 2));
            stageNodes.AddHaarFeature(0.00361906411126256, 0.4238683879375458, 0.6639748215675354, new HaarRectangle(4, 4, 4, 3, -1), new HaarRectangle(4, 5, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.0198171101510525, 0.2150038033723831, 0.5382357835769653, new HaarRectangle(7, 5, 11, 8, -1), new HaarRectangle(7, 9, 11, 4, 2));
            stageNodes.AddHaarFeature(-0.0038154039066284895, 0.6675711274147034, 0.4215297102928162, new HaarRectangle(7, 8, 3, 4, -1), new HaarRectangle(8, 8, 1, 4, 3));
            stageNodes.AddHaarFeature(-0.0049775829538702965, 0.2267289012670517, 0.5386328101158142, new HaarRectangle(9, 1, 6, 1, -1), new HaarRectangle(11, 1, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0236749108880758, 0.1551305055618286, 0.5200707912445068, new HaarRectangle(3, 12, 4, 6, -1), new HaarRectangle(3, 14, 4, 2, 3));
            stageNodes.AddHaarFeature(-0.000014879409718560057, 0.5500566959381104, 0.3820176124572754, new HaarRectangle(13, 11, 3, 4, -1), new HaarRectangle(13, 13, 3, 2, 2));
            stageNodes.AddHaarFeature(0.00361906411126256, 0.4238683879375458, 0.6639748215675354, new HaarRectangle(4, 4, 4, 3, -1), new HaarRectangle(4, 5, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.0198171101510525, 0.2150038033723831, 0.5382357835769653, new HaarRectangle(7, 5, 11, 8, -1), new HaarRectangle(7, 9, 11, 4, 2));
            stageNodes.AddHaarFeature(-0.0038154039066284895, 0.6675711274147034, 0.4215297102928162, new HaarRectangle(7, 8, 3, 4, -1), new HaarRectangle(8, 8, 1, 4, 3));
            stageNodes.AddHaarFeature(-0.0049775829538702965, 0.2267289012670517, 0.5386328101158142, new HaarRectangle(9, 1, 6, 1, -1), new HaarRectangle(11, 1, 2, 1, 3));
            stageNodes.AddHaarFeature(0.002244102070108056, 0.4308691024780273, 0.6855735778808594, new HaarRectangle(5, 5, 3, 3, -1), new HaarRectangle(5, 6, 3, 1, 3));
            stageNodes.AddHaarFeature(0.0122824599966407, 0.5836614966392517, 0.3467479050159454, new HaarRectangle(0, 9, 20, 6, -1), new HaarRectangle(10, 9, 10, 3, 2), new HaarRectangle(0, 12, 10, 3, 2));
            stageNodes.AddHaarFeature(-0.002854869933798909, 0.7016944885253906, 0.4311453998088837, new HaarRectangle(8, 6, 3, 5, -1), new HaarRectangle(9, 6, 1, 5, 3));
            stageNodes.AddHaarFeature(-0.003787566907703877, 0.2895345091819763, 0.5224946141242981, new HaarRectangle(11, 0, 1, 3, -1), new HaarRectangle(11, 1, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.0012201230274513364, 0.2975570857524872, 0.5481644868850708, new HaarRectangle(4, 2, 4, 2, -1), new HaarRectangle(4, 3, 4, 1, 2));
            stageNodes.AddHaarFeature(0.010160599835217, 0.4888817965984345, 0.8182697892189026, new HaarRectangle(12, 6, 4, 3, -1), new HaarRectangle(12, 7, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.0161745697259903, 0.1481492966413498, 0.5239992737770081, new HaarRectangle(5, 0, 6, 4, -1), new HaarRectangle(7, 0, 2, 4, 3));
            stageNodes.AddHaarFeature(0.0192924607545137, 0.4786309897899628, 0.7378190755844116, new HaarRectangle(9, 7, 3, 8, -1), new HaarRectangle(10, 7, 1, 8, 3));
            stageNodes.AddHaarFeature(-0.003247953951358795, 0.7374222874641418, 0.4470643997192383, new HaarRectangle(9, 7, 2, 2, -1), new HaarRectangle(10, 7, 1, 2, 2));
            stageNodes.AddHaarFeature(-0.009380348026752472, 0.3489154875278473, 0.5537996292114258, new HaarRectangle(6, 7, 14, 4, -1), new HaarRectangle(13, 7, 7, 2, 2), new HaarRectangle(6, 9, 7, 2, 2));
            stageNodes.AddHaarFeature(-0.0126061299815774, 0.2379686981439591, 0.5315443277359009, new HaarRectangle(0, 5, 3, 6, -1), new HaarRectangle(0, 7, 3, 2, 3));
            stageNodes.AddHaarFeature(-0.0256219301372766, 0.1964688003063202, 0.5138769745826721, new HaarRectangle(13, 11, 3, 4, -1), new HaarRectangle(13, 13, 3, 2, 2));
            stageNodes.AddHaarFeature(-0.0000757414964027703, 0.5590522885322571, 0.3365853130817413, new HaarRectangle(4, 11, 3, 4, -1), new HaarRectangle(4, 13, 3, 2, 2));
            stageNodes.AddHaarFeature(-0.0892108827829361, 0.0634046569466591, 0.516263484954834, new HaarRectangle(5, 9, 12, 8, -1), new HaarRectangle(11, 9, 6, 4, 2), new HaarRectangle(5, 13, 6, 4, 2));
            stageNodes.AddHaarFeature(-0.002767048077657819, 0.732346773147583, 0.4490706026554108, new HaarRectangle(9, 12, 1, 3, -1), new HaarRectangle(9, 13, 1, 1, 3));
            stageNodes.AddHaarFeature(0.0002715257869567722, 0.411483496427536, 0.5985518097877502, new HaarRectangle(10, 15, 2, 4, -1), new HaarRectangle(10, 17, 2, 2, 2));
            stageNodeCounts.Add(stageNodes.Count - index); index = stageNodes.Count;

            stageThresholds.Add(24.52787971496582);
            stageNodes.AddHaarFeature(0.001478621968999505, 0.266354501247406, 0.6643316745758057, new HaarRectangle(7, 7, 6, 1, -1), new HaarRectangle(9, 7, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.001874165958724916, 0.6143848896026611, 0.2518512904644013, new HaarRectangle(12, 3, 6, 6, -1), new HaarRectangle(15, 3, 3, 3, 2), new HaarRectangle(12, 6, 3, 3, 2));
            stageNodes.AddHaarFeature(-0.001715100952424109, 0.5766341090202332, 0.2397463023662567, new HaarRectangle(0, 4, 10, 6, -1), new HaarRectangle(0, 6, 10, 2, 3));
            stageNodes.AddHaarFeature(-0.0018939269939437509, 0.5682045817375183, 0.2529144883155823, new HaarRectangle(8, 3, 8, 14, -1), new HaarRectangle(12, 3, 4, 7, 2), new HaarRectangle(8, 10, 4, 7, 2));
            stageNodes.AddHaarFeature(-0.005300605203956366, 0.1640675961971283, 0.5556079745292664, new HaarRectangle(4, 4, 7, 15, -1), new HaarRectangle(4, 9, 7, 5, 3));
            stageNodes.AddHaarFeature(-0.0466625317931175, 0.6123154163360596, 0.4762830138206482, new HaarRectangle(12, 2, 6, 8, -1), new HaarRectangle(15, 2, 3, 4, 2), new HaarRectangle(12, 6, 3, 4, 2));
            stageNodes.AddHaarFeature(-0.0007943133241496981, 0.5707858800888062, 0.2839404046535492, new HaarRectangle(2, 2, 6, 8, -1), new HaarRectangle(2, 2, 3, 4, 2), new HaarRectangle(5, 6, 3, 4, 2));
            stageNodes.AddHaarFeature(0.0148916700854898, 0.4089672863483429, 0.6006367206573486, new HaarRectangle(2, 13, 18, 7, -1), new HaarRectangle(8, 13, 6, 7, 3));
            stageNodes.AddHaarFeature(-0.0012046529445797205, 0.5712450742721558, 0.2705289125442505, new HaarRectangle(4, 3, 8, 14, -1), new HaarRectangle(4, 3, 4, 7, 2), new HaarRectangle(8, 10, 4, 7, 2));
            stageNodes.AddHaarFeature(0.006061938125640154, 0.526250422000885, 0.3262225985527039, new HaarRectangle(18, 1, 2, 6, -1), new HaarRectangle(18, 3, 2, 2, 3));
            stageNodes.AddHaarFeature(-0.0025286648888140917, 0.6853830814361572, 0.4199256896972656, new HaarRectangle(9, 11, 2, 3, -1), new HaarRectangle(9, 12, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.005901021882891655, 0.3266282081604004, 0.5434812903404236, new HaarRectangle(18, 1, 2, 6, -1), new HaarRectangle(18, 3, 2, 2, 3));
            stageNodes.AddHaarFeature(0.005670276004821062, 0.5468410849571228, 0.2319003939628601, new HaarRectangle(0, 1, 2, 6, -1), new HaarRectangle(0, 3, 2, 2, 3));
            stageNodes.AddHaarFeature(-0.003030410036444664, 0.557066798210144, 0.2708238065242767, new HaarRectangle(1, 5, 18, 6, -1), new HaarRectangle(1, 7, 18, 2, 3));
            stageNodes.AddHaarFeature(0.002980364952236414, 0.3700568974018097, 0.5890625715255737, new HaarRectangle(0, 2, 6, 7, -1), new HaarRectangle(3, 2, 3, 7, 2));
            stageNodes.AddHaarFeature(-0.0758405104279518, 0.2140070050954819, 0.5419948101043701, new HaarRectangle(7, 3, 6, 14, -1), new HaarRectangle(7, 10, 6, 7, 2));
            stageNodes.AddHaarFeature(0.0192625392228365, 0.5526772141456604, 0.2726590037345886, new HaarRectangle(3, 7, 13, 10, -1), new HaarRectangle(3, 12, 13, 5, 2));
            stageNodes.AddHaarFeature(0.00018888259364757687, 0.3958011865615845, 0.6017209887504578, new HaarRectangle(11, 15, 2, 2, -1), new HaarRectangle(11, 16, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0293695498257875, 0.5241373777389526, 0.1435758024454117, new HaarRectangle(2, 11, 16, 4, -1), new HaarRectangle(2, 11, 8, 2, 2), new HaarRectangle(10, 13, 8, 2, 2));
            stageNodes.AddHaarFeature(0.0010417619487270713, 0.3385409116744995, 0.5929983258247375, new HaarRectangle(13, 7, 6, 4, -1), new HaarRectangle(16, 7, 3, 2, 2), new HaarRectangle(13, 9, 3, 2, 2));
            stageNodes.AddHaarFeature(0.0026125640142709017, 0.5485377907752991, 0.3021597862243652, new HaarRectangle(6, 10, 3, 9, -1), new HaarRectangle(6, 13, 3, 3, 3));
            stageNodes.AddHaarFeature(0.0009697746718302369, 0.3375276029109955, 0.553203284740448, new HaarRectangle(14, 6, 1, 6, -1), new HaarRectangle(14, 9, 1, 3, 2));
            stageNodes.AddHaarFeature(0.0005951265920884907, 0.563174307346344, 0.3359399139881134, new HaarRectangle(5, 10, 4, 1, -1), new HaarRectangle(7, 10, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.1015655994415283, 0.0637350380420685, 0.5230425000190735, new HaarRectangle(3, 8, 15, 5, -1), new HaarRectangle(8, 8, 5, 5, 3));
            stageNodes.AddHaarFeature(0.0361566990613937, 0.5136963129043579, 0.1029528975486755, new HaarRectangle(1, 6, 5, 4, -1), new HaarRectangle(1, 8, 5, 2, 2));
            stageNodes.AddHaarFeature(0.003462414024397731, 0.3879320025444031, 0.5558289289474487, new HaarRectangle(3, 1, 17, 6, -1), new HaarRectangle(3, 3, 17, 2, 3));
            stageNodes.AddHaarFeature(0.0195549800992012, 0.5250086784362793, 0.1875859946012497, new HaarRectangle(6, 7, 8, 2, -1), new HaarRectangle(10, 7, 4, 2, 2));
            stageNodes.AddHaarFeature(-0.0023121440317481756, 0.667202889919281, 0.4679641127586365, new HaarRectangle(9, 7, 3, 2, -1), new HaarRectangle(10, 7, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.0018605289515107872, 0.7163379192352295, 0.4334670901298523, new HaarRectangle(8, 7, 3, 2, -1), new HaarRectangle(9, 7, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.0009402636205777526, 0.302136093378067, 0.5650203227996826, new HaarRectangle(8, 9, 4, 2, -1), new HaarRectangle(8, 10, 4, 1, 2));
            stageNodes.AddHaarFeature(-0.005241833161562681, 0.1820009052753449, 0.5250256061553955, new HaarRectangle(8, 8, 4, 3, -1), new HaarRectangle(8, 9, 4, 1, 3));
            stageNodes.AddHaarFeature(0.00011729019752237946, 0.3389188051223755, 0.544597327709198, new HaarRectangle(9, 5, 6, 4, -1), new HaarRectangle(9, 5, 3, 4, 2));
            stageNodes.AddHaarFeature(0.0011878840159624815, 0.4085349142551422, 0.6253563165664673, new HaarRectangle(8, 13, 4, 3, -1), new HaarRectangle(8, 14, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.0108813596889377, 0.3378399014472961, 0.5700082778930664, new HaarRectangle(4, 7, 12, 6, -1), new HaarRectangle(10, 7, 6, 3, 2), new HaarRectangle(4, 10, 6, 3, 2));
            stageNodes.AddHaarFeature(0.0017354859737679362, 0.4204635918140411, 0.6523038744926453, new HaarRectangle(8, 14, 4, 3, -1), new HaarRectangle(8, 15, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.00651190523058176, 0.2595216035842896, 0.5428143739700317, new HaarRectangle(9, 7, 3, 3, -1), new HaarRectangle(9, 8, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.0012136430013924835, 0.6165143847465515, 0.3977893888950348, new HaarRectangle(7, 4, 3, 8, -1), new HaarRectangle(8, 4, 1, 8, 3));
            stageNodes.AddHaarFeature(-0.010354240424931, 0.1628028005361557, 0.5219504833221436, new HaarRectangle(10, 0, 3, 6, -1), new HaarRectangle(11, 0, 1, 6, 3));
            stageNodes.AddHaarFeature(0.0005585883045569062, 0.3199650943279266, 0.5503574013710022, new HaarRectangle(6, 3, 4, 8, -1), new HaarRectangle(8, 3, 2, 8, 2));
            stageNodes.AddHaarFeature(0.0152996499091387, 0.4103994071483612, 0.6122388243675232, new HaarRectangle(14, 3, 6, 13, -1), new HaarRectangle(14, 3, 3, 13, 2));
            stageNodes.AddHaarFeature(-0.021588210016489, 0.103491298854351, 0.519738495349884, new HaarRectangle(8, 13, 3, 6, -1), new HaarRectangle(8, 16, 3, 3, 2));
            stageNodes.AddHaarFeature(-0.1283462941646576, 0.8493865132331848, 0.4893102943897247, new HaarRectangle(14, 3, 6, 13, -1), new HaarRectangle(14, 3, 3, 13, 2));
            stageNodes.AddHaarFeature(-0.0022927189711481333, 0.3130157887935638, 0.5471575260162354, new HaarRectangle(0, 7, 10, 4, -1), new HaarRectangle(0, 7, 5, 2, 2), new HaarRectangle(5, 9, 5, 2, 2));
            stageNodes.AddHaarFeature(0.0799151062965393, 0.4856320917606354, 0.6073989272117615, new HaarRectangle(14, 3, 6, 13, -1), new HaarRectangle(14, 3, 3, 13, 2));
            stageNodes.AddHaarFeature(-0.0794410929083824, 0.8394674062728882, 0.462453305721283, new HaarRectangle(0, 3, 6, 13, -1), new HaarRectangle(3, 3, 3, 13, 2));
            stageNodes.AddHaarFeature(-0.00528000108897686, 0.1881695985794067, 0.5306698083877563, new HaarRectangle(9, 1, 4, 1, -1), new HaarRectangle(9, 1, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0010463109938427806, 0.5271229147911072, 0.2583065927028656, new HaarRectangle(8, 0, 2, 1, -1), new HaarRectangle(9, 0, 1, 1, 2));
            stageNodes.AddHaarFeature(0.00026317298761568964, 0.4235304892063141, 0.5735440850257874, new HaarRectangle(10, 16, 4, 4, -1), new HaarRectangle(12, 16, 2, 2, 2), new HaarRectangle(10, 18, 2, 2, 2));
            stageNodes.AddHaarFeature(-0.003617316018790006, 0.6934396028518677, 0.4495444893836975, new HaarRectangle(9, 6, 2, 3, -1), new HaarRectangle(10, 6, 1, 3, 2));
            stageNodes.AddHaarFeature(0.0114218797534704, 0.590092122554779, 0.4138193130493164, new HaarRectangle(4, 5, 12, 2, -1), new HaarRectangle(8, 5, 4, 2, 3));
            stageNodes.AddHaarFeature(-0.0019963278900831938, 0.6466382741928101, 0.4327239990234375, new HaarRectangle(8, 7, 3, 5, -1), new HaarRectangle(9, 7, 1, 5, 3));
            stageNodeCounts.Add(stageNodes.Count - index); index = stageNodes.Count;

            stageThresholds.Add(27.153350830078125);
            stageNodes.AddHaarFeature(-0.00996912457048893, 0.6142324209213257, 0.2482212036848068, new HaarRectangle(6, 4, 8, 6, -1), new HaarRectangle(6, 6, 8, 2, 3));
            stageNodes.AddHaarFeature(0.0007307305932044983, 0.5704951882362366, 0.2321965992450714, new HaarRectangle(9, 5, 2, 12, -1), new HaarRectangle(9, 11, 2, 6, 2));
            stageNodes.AddHaarFeature(0.0006404530140571296, 0.2112251967191696, 0.5814933180809021, new HaarRectangle(4, 6, 6, 8, -1), new HaarRectangle(4, 10, 6, 4, 2));
            stageNodes.AddHaarFeature(0.004542401991784573, 0.2950482070446014, 0.586631178855896, new HaarRectangle(12, 2, 8, 5, -1), new HaarRectangle(12, 2, 4, 5, 2));
            stageNodes.AddHaarFeature(0.00009247744310414419, 0.2990990877151489, 0.5791326761245728, new HaarRectangle(0, 8, 18, 3, -1), new HaarRectangle(0, 9, 18, 1, 3));
            stageNodes.AddHaarFeature(-0.008660314604640007, 0.2813029885292053, 0.5635542273521423, new HaarRectangle(8, 12, 4, 8, -1), new HaarRectangle(8, 16, 4, 4, 2));
            stageNodes.AddHaarFeature(0.008051581680774689, 0.3535369038581848, 0.6054757237434387, new HaarRectangle(0, 2, 8, 5, -1), new HaarRectangle(4, 2, 4, 5, 2));
            stageNodes.AddHaarFeature(0.00043835240649059415, 0.5596532225608826, 0.2731510996818543, new HaarRectangle(13, 11, 3, 4, -1), new HaarRectangle(13, 13, 3, 2, 2));
            stageNodes.AddHaarFeature(-0.00009816897363634781, 0.5978031754493713, 0.3638561069965363, new HaarRectangle(5, 11, 6, 1, -1), new HaarRectangle(7, 11, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0011298790341243148, 0.2755252122879028, 0.5432729125022888, new HaarRectangle(11, 3, 3, 1, -1), new HaarRectangle(12, 3, 1, 1, 3));
            stageNodes.AddHaarFeature(0.006435615010559559, 0.4305641949176788, 0.7069833278656006, new HaarRectangle(7, 13, 5, 3, -1), new HaarRectangle(7, 14, 5, 1, 3));
            stageNodes.AddHaarFeature(-0.0568293295800686, 0.2495242953300476, 0.5294997096061707, new HaarRectangle(11, 11, 7, 6, -1), new HaarRectangle(11, 14, 7, 3, 2));
            stageNodes.AddHaarFeature(0.004066816996783018, 0.5478553175926208, 0.2497723996639252, new HaarRectangle(2, 11, 7, 6, -1), new HaarRectangle(2, 14, 7, 3, 2));
            stageNodes.AddHaarFeature(0.0000481647984997835, 0.3938601016998291, 0.5706356167793274, new HaarRectangle(12, 14, 2, 6, -1), new HaarRectangle(12, 16, 2, 2, 3));
            stageNodes.AddHaarFeature(0.00617950176820159, 0.440760612487793, 0.7394766807556152, new HaarRectangle(8, 14, 3, 3, -1), new HaarRectangle(8, 15, 3, 1, 3));
            stageNodes.AddHaarFeature(0.006498575210571289, 0.5445243120193481, 0.2479152977466583, new HaarRectangle(11, 0, 3, 5, -1), new HaarRectangle(12, 0, 1, 5, 3));
            stageNodes.AddHaarFeature(-0.0010211090557277205, 0.2544766962528229, 0.5338971018791199, new HaarRectangle(6, 1, 4, 9, -1), new HaarRectangle(8, 1, 2, 9, 2));
            stageNodes.AddHaarFeature(-0.005424752831459045, 0.2718858122825623, 0.5324069261550903, new HaarRectangle(10, 3, 6, 1, -1), new HaarRectangle(12, 3, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0010559899965301156, 0.3178288042545319, 0.553450882434845, new HaarRectangle(8, 8, 3, 4, -1), new HaarRectangle(8, 10, 3, 2, 2));
            stageNodes.AddHaarFeature(0.0006646580877713859, 0.4284219145774841, 0.6558194160461426, new HaarRectangle(8, 12, 4, 2, -1), new HaarRectangle(8, 13, 4, 1, 2));
            stageNodes.AddHaarFeature(-0.00027524109464138746, 0.5902860760688782, 0.3810262978076935, new HaarRectangle(5, 18, 4, 2, -1), new HaarRectangle(5, 19, 4, 1, 2));
            stageNodes.AddHaarFeature(0.004229320213198662, 0.381648987531662, 0.5709385871887207, new HaarRectangle(2, 1, 18, 6, -1), new HaarRectangle(2, 3, 18, 2, 3));
            stageNodes.AddHaarFeature(-0.0032868210691958666, 0.1747743934392929, 0.5259544253349304, new HaarRectangle(6, 0, 3, 2, -1), new HaarRectangle(7, 0, 1, 2, 3));
            stageNodes.AddHaarFeature(0.0001561187964398414, 0.3601722121238709, 0.5725612044334412, new HaarRectangle(13, 8, 6, 2, -1), new HaarRectangle(16, 8, 3, 1, 2), new HaarRectangle(13, 9, 3, 1, 2));
            stageNodes.AddHaarFeature(-0.000007362138148891972, 0.540185809135437, 0.3044497072696686, new HaarRectangle(6, 10, 3, 6, -1), new HaarRectangle(6, 13, 3, 3, 2));
            stageNodes.AddHaarFeature(-0.014767250046134, 0.3220770061016083, 0.5573434829711914, new HaarRectangle(0, 13, 20, 4, -1), new HaarRectangle(10, 13, 10, 2, 2), new HaarRectangle(0, 15, 10, 2, 2));
            stageNodes.AddHaarFeature(0.0244895908981562, 0.4301528036594391, 0.6518812775611877, new HaarRectangle(7, 7, 6, 5, -1), new HaarRectangle(9, 7, 2, 5, 3));
            stageNodes.AddHaarFeature(-0.00037652091123163695, 0.356458306312561, 0.5598236918449402, new HaarRectangle(11, 0, 2, 2, -1), new HaarRectangle(11, 1, 2, 1, 2));
            stageNodes.AddHaarFeature(0.00000736576885174145, 0.3490782976150513, 0.556189775466919, new HaarRectangle(1, 8, 6, 2, -1), new HaarRectangle(1, 8, 3, 1, 2), new HaarRectangle(4, 9, 3, 1, 2));
            stageNodes.AddHaarFeature(-0.0150999398902059, 0.1776272058486939, 0.5335299968719482, new HaarRectangle(0, 2, 20, 2, -1), new HaarRectangle(10, 2, 10, 1, 2), new HaarRectangle(0, 3, 10, 1, 2));
            stageNodes.AddHaarFeature(-0.0038316650316119194, 0.6149687767028809, 0.4221394062042236, new HaarRectangle(7, 14, 5, 3, -1), new HaarRectangle(7, 15, 5, 1, 3));
            stageNodes.AddHaarFeature(0.0169254001230001, 0.5413014888763428, 0.2166585028171539, new HaarRectangle(7, 13, 6, 6, -1), new HaarRectangle(10, 13, 3, 3, 2), new HaarRectangle(7, 16, 3, 3, 2));
            stageNodes.AddHaarFeature(-0.003047785023227334, 0.6449490785598755, 0.4354617893695831, new HaarRectangle(9, 12, 2, 3, -1), new HaarRectangle(9, 13, 2, 1, 3));
            stageNodes.AddHaarFeature(0.003214058931916952, 0.5400155186653137, 0.3523217141628265, new HaarRectangle(16, 11, 1, 6, -1), new HaarRectangle(16, 13, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.004002320114523172, 0.2774524092674255, 0.5338417291641235, new HaarRectangle(3, 11, 1, 6, -1), new HaarRectangle(3, 13, 1, 2, 3));
            stageNodes.AddHaarFeature(0.007418212946504355, 0.567673921585083, 0.3702817857265472, new HaarRectangle(4, 4, 14, 12, -1), new HaarRectangle(11, 4, 7, 6, 2), new HaarRectangle(4, 10, 7, 6, 2));
            stageNodes.AddHaarFeature(-0.008876458741724491, 0.7749221920967102, 0.4583688974380493, new HaarRectangle(5, 4, 3, 3, -1), new HaarRectangle(5, 5, 3, 1, 3));
            stageNodes.AddHaarFeature(0.002731173997744918, 0.5338721871376038, 0.3996661007404327, new HaarRectangle(12, 3, 3, 3, -1), new HaarRectangle(13, 3, 1, 3, 3));
            stageNodes.AddHaarFeature(-0.0025082379579544067, 0.5611963272094727, 0.377749890089035, new HaarRectangle(6, 6, 8, 3, -1), new HaarRectangle(6, 7, 8, 1, 3));
            stageNodes.AddHaarFeature(-0.008054107427597046, 0.291522890329361, 0.5179182887077332, new HaarRectangle(12, 3, 3, 3, -1), new HaarRectangle(13, 3, 1, 3, 3));
            stageNodes.AddHaarFeature(-0.0009793881326913834, 0.5536432862281799, 0.3700192868709564, new HaarRectangle(3, 1, 4, 10, -1), new HaarRectangle(3, 1, 2, 5, 2), new HaarRectangle(5, 6, 2, 5, 2));
            stageNodes.AddHaarFeature(-0.005874590948224068, 0.3754391074180603, 0.5679376125335693, new HaarRectangle(5, 7, 10, 2, -1), new HaarRectangle(5, 7, 5, 2, 2));
            stageNodes.AddHaarFeature(-0.00449367193505168, 0.7019699215888977, 0.4480949938297272, new HaarRectangle(8, 7, 3, 3, -1), new HaarRectangle(9, 7, 1, 3, 3));
            stageNodes.AddHaarFeature(-0.00543892290443182, 0.2310364991426468, 0.5313386917114258, new HaarRectangle(15, 12, 2, 3, -1), new HaarRectangle(15, 13, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0007509464048780501, 0.5864868760108948, 0.4129343032836914, new HaarRectangle(7, 8, 3, 4, -1), new HaarRectangle(8, 8, 1, 4, 3));
            stageNodes.AddHaarFeature(0.000014528800420521293, 0.3732407093048096, 0.5619621276855469, new HaarRectangle(13, 4, 1, 12, -1), new HaarRectangle(13, 10, 1, 6, 2));
            stageNodes.AddHaarFeature(0.0407580696046352, 0.5312091112136841, 0.2720521986484528, new HaarRectangle(4, 5, 12, 12, -1), new HaarRectangle(4, 5, 6, 6, 2), new HaarRectangle(10, 11, 6, 6, 2));
            stageNodes.AddHaarFeature(0.006650593131780624, 0.4710015952587128, 0.6693493723869324, new HaarRectangle(7, 14, 7, 3, -1), new HaarRectangle(7, 15, 7, 1, 3));
            stageNodes.AddHaarFeature(0.0045759351924061775, 0.5167819261550903, 0.1637275964021683, new HaarRectangle(3, 12, 2, 3, -1), new HaarRectangle(3, 13, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0065269311890006065, 0.5397608876228333, 0.2938531935214996, new HaarRectangle(3, 2, 14, 2, -1), new HaarRectangle(10, 2, 7, 1, 2), new HaarRectangle(3, 3, 7, 1, 2));
            stageNodes.AddHaarFeature(-0.0136603796854615, 0.7086488008499146, 0.453220009803772, new HaarRectangle(0, 1, 3, 10, -1), new HaarRectangle(1, 1, 1, 10, 3));
            stageNodes.AddHaarFeature(0.0273588690906763, 0.5206481218338013, 0.3589231967926025, new HaarRectangle(9, 0, 6, 5, -1), new HaarRectangle(11, 0, 2, 5, 3));
            stageNodes.AddHaarFeature(0.0006219755159690976, 0.3507075905799866, 0.5441123247146606, new HaarRectangle(5, 7, 6, 2, -1), new HaarRectangle(8, 7, 3, 2, 2));
            stageNodes.AddHaarFeature(-0.0033077080734074116, 0.5859522819519043, 0.402489185333252, new HaarRectangle(7, 1, 6, 10, -1), new HaarRectangle(7, 6, 6, 5, 2));
            stageNodes.AddHaarFeature(-0.0106311095878482, 0.6743267178535461, 0.4422602951526642, new HaarRectangle(1, 1, 18, 3, -1), new HaarRectangle(7, 1, 6, 3, 3));
            stageNodes.AddHaarFeature(0.0194416493177414, 0.5282716155052185, 0.1797904968261719, new HaarRectangle(16, 3, 3, 6, -1), new HaarRectangle(16, 5, 3, 2, 3));
            stageNodeCounts.Add(stageNodes.Count - index); index = stageNodes.Count;

            stageThresholds.Add(34.55411148071289);
            stageNodes.AddHaarFeature(-0.005505216773599386, 0.5914731025695801, 0.2626559138298035, new HaarRectangle(6, 3, 7, 6, -1), new HaarRectangle(6, 6, 7, 3, 2));
            stageNodes.AddHaarFeature(0.001956227933987975, 0.2312581986188889, 0.5741627216339111, new HaarRectangle(4, 7, 12, 2, -1), new HaarRectangle(8, 7, 4, 2, 3));
            stageNodes.AddHaarFeature(-0.008892478421330452, 0.1656530052423477, 0.5626654028892517, new HaarRectangle(0, 4, 17, 10, -1), new HaarRectangle(0, 9, 17, 5, 2));
            stageNodes.AddHaarFeature(0.0836383774876595, 0.5423449873924255, 0.1957294940948486, new HaarRectangle(3, 4, 15, 16, -1), new HaarRectangle(3, 12, 15, 8, 2));
            stageNodes.AddHaarFeature(0.0012282270472496748, 0.3417904078960419, 0.5992503762245178, new HaarRectangle(7, 15, 6, 4, -1), new HaarRectangle(7, 17, 6, 2, 2));
            stageNodes.AddHaarFeature(0.0057629169896245, 0.3719581961631775, 0.6079903841018677, new HaarRectangle(15, 2, 4, 9, -1), new HaarRectangle(15, 2, 2, 9, 2));
            stageNodes.AddHaarFeature(-0.0016417410224676132, 0.2577486038208008, 0.5576915740966797, new HaarRectangle(2, 3, 3, 2, -1), new HaarRectangle(2, 4, 3, 1, 2));
            stageNodes.AddHaarFeature(0.0034113149158656597, 0.2950749099254608, 0.5514171719551086, new HaarRectangle(13, 6, 7, 9, -1), new HaarRectangle(13, 9, 7, 3, 3));
            stageNodes.AddHaarFeature(-0.0110693201422691, 0.7569358944892883, 0.4477078914642334, new HaarRectangle(8, 11, 4, 3, -1), new HaarRectangle(8, 12, 4, 1, 3));
            stageNodes.AddHaarFeature(0.0348659716546535, 0.5583708882331848, 0.2669621109962463, new HaarRectangle(0, 2, 20, 6, -1), new HaarRectangle(10, 2, 10, 3, 2), new HaarRectangle(0, 5, 10, 3, 2));
            stageNodes.AddHaarFeature(0.0006570109981112182, 0.5627313256263733, 0.2988890111446381, new HaarRectangle(3, 2, 6, 10, -1), new HaarRectangle(3, 2, 3, 5, 2), new HaarRectangle(6, 7, 3, 5, 2));
            stageNodes.AddHaarFeature(-0.0243391301482916, 0.2771185040473938, 0.5108863115310669, new HaarRectangle(13, 10, 3, 4, -1), new HaarRectangle(13, 12, 3, 2, 2));
            stageNodes.AddHaarFeature(0.0005943520227447152, 0.5580651760101318, 0.3120341897010803, new HaarRectangle(4, 10, 3, 4, -1), new HaarRectangle(4, 12, 3, 2, 2));
            stageNodes.AddHaarFeature(0.0022971509024500847, 0.3330250084400177, 0.5679075717926025, new HaarRectangle(7, 5, 6, 3, -1), new HaarRectangle(9, 5, 2, 3, 3));
            stageNodes.AddHaarFeature(-0.0037801829166710377, 0.2990534901618958, 0.5344808101654053, new HaarRectangle(7, 6, 6, 8, -1), new HaarRectangle(7, 10, 6, 4, 2));
            stageNodes.AddHaarFeature(-0.13420669734478, 0.1463858932256699, 0.5392568111419678, new HaarRectangle(0, 11, 20, 6, -1), new HaarRectangle(0, 14, 20, 3, 2));
            stageNodes.AddHaarFeature(0.0007522454834543167, 0.3746953904628754, 0.5692734718322754, new HaarRectangle(4, 13, 4, 6, -1), new HaarRectangle(4, 13, 2, 3, 2), new HaarRectangle(6, 16, 2, 3, 2));
            stageNodes.AddHaarFeature(-0.040545541793108, 0.2754747867584229, 0.5484297871589661, new HaarRectangle(6, 0, 8, 12, -1), new HaarRectangle(10, 0, 4, 6, 2), new HaarRectangle(6, 6, 4, 6, 2));
            stageNodes.AddHaarFeature(0.0012572970008477569, 0.3744584023952484, 0.5756075978279114, new HaarRectangle(2, 0, 15, 2, -1), new HaarRectangle(2, 1, 15, 1, 2));
            stageNodes.AddHaarFeature(-0.007424994837492704, 0.7513859272003174, 0.4728231132030487, new HaarRectangle(9, 12, 2, 3, -1), new HaarRectangle(9, 13, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0005090812919661403, 0.540489673614502, 0.2932321131229401, new HaarRectangle(3, 12, 1, 2, -1), new HaarRectangle(3, 13, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.001280845026485622, 0.6169779896736145, 0.4273349046707153, new HaarRectangle(9, 11, 2, 3, -1), new HaarRectangle(9, 12, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0018348860321566465, 0.2048496007919312, 0.5206472277641296, new HaarRectangle(7, 3, 3, 1, -1), new HaarRectangle(8, 3, 1, 1, 3));
            stageNodes.AddHaarFeature(0.0274848695844412, 0.5252984762191772, 0.1675522029399872, new HaarRectangle(17, 7, 3, 6, -1), new HaarRectangle(17, 9, 3, 2, 3));
            stageNodes.AddHaarFeature(0.0022372419480234385, 0.5267782807350159, 0.2777658104896545, new HaarRectangle(7, 2, 3, 2, -1), new HaarRectangle(8, 2, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.008863529190421104, 0.69545578956604, 0.4812048971652985, new HaarRectangle(11, 4, 5, 3, -1), new HaarRectangle(11, 5, 5, 1, 3));
            stageNodes.AddHaarFeature(0.004175397101789713, 0.4291887879371643, 0.6349195837974548, new HaarRectangle(4, 4, 5, 3, -1), new HaarRectangle(4, 5, 5, 1, 3));
            stageNodes.AddHaarFeature(-0.0017098189564421773, 0.2930536866188049, 0.5361248850822449, new HaarRectangle(19, 3, 1, 2, -1), new HaarRectangle(19, 4, 1, 1, 2));
            stageNodes.AddHaarFeature(0.006532854866236448, 0.4495325088500977, 0.7409694194793701, new HaarRectangle(5, 5, 4, 3, -1), new HaarRectangle(5, 6, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.009537290781736374, 0.3149119913578033, 0.5416501760482788, new HaarRectangle(17, 7, 3, 6, -1), new HaarRectangle(17, 9, 3, 2, 3));
            stageNodes.AddHaarFeature(0.0253109894692898, 0.5121892094612122, 0.1311707943677902, new HaarRectangle(0, 7, 3, 6, -1), new HaarRectangle(0, 9, 3, 2, 3));
            stageNodes.AddHaarFeature(0.0364609695971012, 0.5175911784172058, 0.2591339945793152, new HaarRectangle(14, 2, 6, 9, -1), new HaarRectangle(14, 5, 6, 3, 3));
            stageNodes.AddHaarFeature(0.0208543296903372, 0.5137140154838562, 0.1582316011190414, new HaarRectangle(0, 4, 5, 6, -1), new HaarRectangle(0, 6, 5, 2, 3));
            stageNodes.AddHaarFeature(-0.0008720774785615504, 0.5574309825897217, 0.439897894859314, new HaarRectangle(10, 5, 6, 2, -1), new HaarRectangle(12, 5, 2, 2, 3));
            stageNodes.AddHaarFeature(-0.000015227000403683633, 0.5548940896987915, 0.3708069920539856, new HaarRectangle(4, 5, 6, 2, -1), new HaarRectangle(6, 5, 2, 2, 3));
            stageNodes.AddHaarFeature(-0.0008431650931015611, 0.3387419879436493, 0.5554211139678955, new HaarRectangle(8, 1, 4, 6, -1), new HaarRectangle(8, 3, 4, 2, 3));
            stageNodes.AddHaarFeature(0.0036037859972566366, 0.5358061790466309, 0.3411171138286591, new HaarRectangle(0, 2, 3, 6, -1), new HaarRectangle(0, 4, 3, 2, 3));
            stageNodes.AddHaarFeature(-0.006805789191275835, 0.6125202775001526, 0.4345862865447998, new HaarRectangle(6, 6, 8, 3, -1), new HaarRectangle(6, 7, 8, 1, 3));
            stageNodes.AddHaarFeature(-0.0470216609537601, 0.2358165979385376, 0.519373893737793, new HaarRectangle(0, 1, 5, 9, -1), new HaarRectangle(0, 4, 5, 3, 3));
            stageNodes.AddHaarFeature(-0.0369541086256504, 0.7323111295700073, 0.4760943949222565, new HaarRectangle(16, 0, 4, 15, -1), new HaarRectangle(16, 0, 2, 15, 2));
            stageNodes.AddHaarFeature(0.0010439479956403377, 0.5419455170631409, 0.3411330878734589, new HaarRectangle(1, 10, 3, 2, -1), new HaarRectangle(1, 11, 3, 1, 2));
            stageNodes.AddHaarFeature(-0.00021050689974799752, 0.2821694016456604, 0.5554947257041931, new HaarRectangle(14, 4, 1, 10, -1), new HaarRectangle(14, 9, 1, 5, 2));
            stageNodes.AddHaarFeature(-0.0808315873146057, 0.9129930138587952, 0.4697434902191162, new HaarRectangle(0, 1, 4, 12, -1), new HaarRectangle(2, 1, 2, 12, 2));
            stageNodes.AddHaarFeature(-0.00036579059087671345, 0.6022670269012451, 0.3978292942047119, new HaarRectangle(11, 11, 4, 2, -1), new HaarRectangle(11, 11, 2, 2, 2));
            stageNodes.AddHaarFeature(-0.00012545920617412776, 0.5613213181495667, 0.384553998708725, new HaarRectangle(5, 11, 4, 2, -1), new HaarRectangle(7, 11, 2, 2, 2));
            stageNodes.AddHaarFeature(-0.0687864869832993, 0.2261611968278885, 0.5300496816635132, new HaarRectangle(3, 8, 15, 5, -1), new HaarRectangle(8, 8, 5, 5, 3));
            stageNodes.AddHaarFeature(0.0124157899990678, 0.4075691998004913, 0.5828812122344971, new HaarRectangle(0, 0, 6, 10, -1), new HaarRectangle(3, 0, 3, 10, 2));
            stageNodes.AddHaarFeature(-0.004717481788247824, 0.2827253937721252, 0.5267757773399353, new HaarRectangle(11, 4, 3, 2, -1), new HaarRectangle(12, 4, 1, 2, 3));
            stageNodes.AddHaarFeature(0.0381368584930897, 0.5074741244316101, 0.1023615971207619, new HaarRectangle(8, 12, 3, 8, -1), new HaarRectangle(8, 16, 3, 4, 2));
            stageNodes.AddHaarFeature(-0.0028168049175292253, 0.6169006824493408, 0.4359692931175232, new HaarRectangle(8, 14, 5, 3, -1), new HaarRectangle(8, 15, 5, 1, 3));
            stageNodes.AddHaarFeature(0.008130360394716263, 0.4524433016777039, 0.76060950756073, new HaarRectangle(7, 14, 4, 3, -1), new HaarRectangle(7, 15, 4, 1, 3));
            stageNodes.AddHaarFeature(0.006005601957440376, 0.5240408778190613, 0.185971200466156, new HaarRectangle(11, 4, 3, 2, -1), new HaarRectangle(12, 4, 1, 2, 3));
            stageNodes.AddHaarFeature(0.0191393196582794, 0.5209379196166992, 0.2332071959972382, new HaarRectangle(3, 15, 14, 4, -1), new HaarRectangle(3, 15, 7, 2, 2), new HaarRectangle(10, 17, 7, 2, 2));
            stageNodes.AddHaarFeature(0.0164457596838474, 0.5450702905654907, 0.3264234960079193, new HaarRectangle(2, 2, 16, 4, -1), new HaarRectangle(10, 2, 8, 2, 2), new HaarRectangle(2, 4, 8, 2, 2));
            stageNodes.AddHaarFeature(-0.0373568907380104, 0.6999046802520752, 0.4533241987228394, new HaarRectangle(0, 8, 6, 12, -1), new HaarRectangle(3, 8, 3, 12, 2));
            stageNodes.AddHaarFeature(-0.0197279006242752, 0.2653664946556091, 0.54128098487854, new HaarRectangle(5, 7, 10, 2, -1), new HaarRectangle(5, 7, 5, 2, 2));
            stageNodes.AddHaarFeature(0.0066972579807043076, 0.4480566084384918, 0.7138652205467224, new HaarRectangle(9, 7, 2, 5, -1), new HaarRectangle(10, 7, 1, 5, 2));
            stageNodes.AddHaarFeature(0.0007445752853527665, 0.4231350123882294, 0.5471320152282715, new HaarRectangle(13, 7, 6, 4, -1), new HaarRectangle(16, 7, 3, 2, 2), new HaarRectangle(13, 9, 3, 2, 2));
            stageNodes.AddHaarFeature(0.0011790640419349074, 0.5341702103614807, 0.3130455017089844, new HaarRectangle(0, 13, 8, 2, -1), new HaarRectangle(0, 14, 8, 1, 2));
            stageNodes.AddHaarFeature(0.0349806100130081, 0.5118659734725952, 0.343053013086319, new HaarRectangle(13, 7, 6, 4, -1), new HaarRectangle(16, 7, 3, 2, 2), new HaarRectangle(13, 9, 3, 2, 2));
            stageNodes.AddHaarFeature(0.0005685979267582297, 0.3532187044620514, 0.5468639731407166, new HaarRectangle(1, 7, 6, 4, -1), new HaarRectangle(1, 7, 3, 2, 2), new HaarRectangle(4, 9, 3, 2, 2));
            stageNodes.AddHaarFeature(-0.0113406497985125, 0.2842353880405426, 0.5348700881004333, new HaarRectangle(12, 6, 1, 12, -1), new HaarRectangle(12, 12, 1, 6, 2));
            stageNodes.AddHaarFeature(-0.00662281084805727, 0.6883640289306641, 0.4492664933204651, new HaarRectangle(9, 5, 2, 6, -1), new HaarRectangle(10, 5, 1, 6, 2));
            stageNodes.AddHaarFeature(-0.008016033098101616, 0.1709893941879273, 0.5224308967590332, new HaarRectangle(14, 12, 2, 3, -1), new HaarRectangle(14, 13, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0014206819469109178, 0.5290846228599548, 0.299338310956955, new HaarRectangle(4, 12, 2, 3, -1), new HaarRectangle(4, 13, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0027801711112260814, 0.6498854160308838, 0.4460499882698059, new HaarRectangle(8, 12, 4, 3, -1), new HaarRectangle(8, 13, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.001474758959375322, 0.3260438144207001, 0.5388113260269165, new HaarRectangle(5, 2, 2, 4, -1), new HaarRectangle(5, 2, 1, 2, 2), new HaarRectangle(6, 4, 1, 2, 2));
            stageNodes.AddHaarFeature(-0.0238303393125534, 0.7528941035270691, 0.4801219999790192, new HaarRectangle(5, 5, 11, 3, -1), new HaarRectangle(5, 6, 11, 1, 3));
            stageNodes.AddHaarFeature(0.00693697901442647, 0.5335165858268738, 0.3261427879333496, new HaarRectangle(7, 6, 4, 12, -1), new HaarRectangle(7, 12, 4, 6, 2));
            stageNodes.AddHaarFeature(0.008280625566840172, 0.458039402961731, 0.5737829804420471, new HaarRectangle(12, 13, 8, 5, -1), new HaarRectangle(12, 13, 4, 5, 2));
            stageNodes.AddHaarFeature(-0.0104395002126694, 0.2592320144176483, 0.5233827829360962, new HaarRectangle(7, 6, 1, 12, -1), new HaarRectangle(7, 12, 1, 6, 2));
            stageNodeCounts.Add(stageNodes.Count - index); index = stageNodes.Count;

            stageThresholds.Add(39.1072883605957);
            stageNodes.AddHaarFeature(0.0072006587870419025, 0.325888603925705, 0.6849808096885681, new HaarRectangle(1, 2, 6, 3, -1), new HaarRectangle(4, 2, 3, 3, 2));
            stageNodes.AddHaarFeature(-0.002859358908608556, 0.5838881134986877, 0.2537829875946045, new HaarRectangle(9, 5, 6, 10, -1), new HaarRectangle(12, 5, 3, 5, 2), new HaarRectangle(9, 10, 3, 5, 2));
            stageNodes.AddHaarFeature(0.0006858052802272141, 0.5708081722259521, 0.2812424004077911, new HaarRectangle(5, 5, 8, 12, -1), new HaarRectangle(5, 5, 4, 6, 2), new HaarRectangle(9, 11, 4, 6, 2));
            stageNodes.AddHaarFeature(0.007958019152283669, 0.2501051127910614, 0.5544260740280151, new HaarRectangle(0, 7, 20, 6, -1), new HaarRectangle(0, 9, 20, 2, 3));
            stageNodes.AddHaarFeature(-0.0012124150525778532, 0.2385368049144745, 0.5433350205421448, new HaarRectangle(4, 2, 2, 2, -1), new HaarRectangle(4, 3, 2, 1, 2));
            stageNodes.AddHaarFeature(0.00794261321425438, 0.3955070972442627, 0.6220757961273193, new HaarRectangle(4, 18, 12, 2, -1), new HaarRectangle(8, 18, 4, 2, 3));
            stageNodes.AddHaarFeature(0.0024630590341985226, 0.5639708042144775, 0.2992357909679413, new HaarRectangle(7, 4, 4, 16, -1), new HaarRectangle(7, 12, 4, 8, 2));
            stageNodes.AddHaarFeature(-0.006039659958332777, 0.218651294708252, 0.541167676448822, new HaarRectangle(7, 6, 7, 8, -1), new HaarRectangle(7, 10, 7, 4, 2));
            stageNodes.AddHaarFeature(-0.0012988339876756072, 0.23507060110569, 0.5364584922790527, new HaarRectangle(6, 3, 3, 1, -1), new HaarRectangle(7, 3, 1, 1, 3));
            stageNodes.AddHaarFeature(0.00022299369447864592, 0.380411297082901, 0.572960615158081, new HaarRectangle(11, 15, 2, 4, -1), new HaarRectangle(11, 17, 2, 2, 2));
            stageNodes.AddHaarFeature(0.0014654280385002494, 0.2510167956352234, 0.5258268713951111, new HaarRectangle(3, 5, 4, 8, -1), new HaarRectangle(3, 9, 4, 4, 2));
            stageNodes.AddHaarFeature(-0.0008121004211716354, 0.5992823839187622, 0.3851158916950226, new HaarRectangle(7, 1, 6, 12, -1), new HaarRectangle(7, 7, 6, 6, 2));
            stageNodes.AddHaarFeature(-0.0013836020370945334, 0.5681396126747131, 0.3636586964130402, new HaarRectangle(4, 6, 6, 2, -1), new HaarRectangle(6, 6, 2, 2, 3));
            stageNodes.AddHaarFeature(-0.0279364492744207, 0.1491317003965378, 0.5377560257911682, new HaarRectangle(16, 4, 4, 6, -1), new HaarRectangle(16, 6, 4, 2, 3));
            stageNodes.AddHaarFeature(-0.0004691955109592527, 0.3692429959774017, 0.5572484731674194, new HaarRectangle(3, 3, 5, 2, -1), new HaarRectangle(3, 4, 5, 1, 2));
            stageNodes.AddHaarFeature(-0.004982965998351574, 0.6758509278297424, 0.4532504081726074, new HaarRectangle(9, 11, 2, 3, -1), new HaarRectangle(9, 12, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0018815309740602968, 0.5368022918701172, 0.2932539880275726, new HaarRectangle(2, 16, 4, 2, -1), new HaarRectangle(2, 17, 4, 1, 2));
            stageNodes.AddHaarFeature(-0.0190675500780344, 0.1649377048015595, 0.5330067276954651, new HaarRectangle(7, 13, 6, 6, -1), new HaarRectangle(10, 13, 3, 3, 2), new HaarRectangle(7, 16, 3, 3, 2));
            stageNodes.AddHaarFeature(-0.0046906559728085995, 0.1963925957679749, 0.5119361877441406, new HaarRectangle(7, 0, 3, 4, -1), new HaarRectangle(8, 0, 1, 4, 3));
            stageNodes.AddHaarFeature(0.005977713968604803, 0.467117190361023, 0.7008398175239563, new HaarRectangle(8, 15, 4, 3, -1), new HaarRectangle(8, 16, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.0333031304180622, 0.1155416965484619, 0.5104162096977234, new HaarRectangle(0, 4, 4, 6, -1), new HaarRectangle(0, 6, 4, 2, 3));
            stageNodes.AddHaarFeature(0.0907441079616547, 0.5149660110473633, 0.1306173056364059, new HaarRectangle(5, 6, 12, 3, -1), new HaarRectangle(9, 6, 4, 3, 3));
            stageNodes.AddHaarFeature(0.0009355589863844216, 0.3605481088161469, 0.543985903263092, new HaarRectangle(7, 6, 6, 14, -1), new HaarRectangle(9, 6, 2, 14, 3));
            stageNodes.AddHaarFeature(0.0149016501381993, 0.4886212050914764, 0.7687569856643677, new HaarRectangle(9, 7, 3, 3, -1), new HaarRectangle(10, 7, 1, 3, 3));
            stageNodes.AddHaarFeature(0.0006159411859698594, 0.5356813073158264, 0.3240939080715179, new HaarRectangle(6, 12, 2, 4, -1), new HaarRectangle(6, 14, 2, 2, 2));
            stageNodes.AddHaarFeature(-0.0506709888577461, 0.1848621964454651, 0.5230404138565063, new HaarRectangle(10, 12, 7, 6, -1), new HaarRectangle(10, 14, 7, 2, 3));
            stageNodes.AddHaarFeature(0.0006866574985906482, 0.3840579986572266, 0.5517945885658264, new HaarRectangle(1, 0, 15, 2, -1), new HaarRectangle(1, 1, 15, 1, 2));
            stageNodes.AddHaarFeature(0.008371243253350258, 0.4288564026355743, 0.6131753921508789, new HaarRectangle(14, 0, 6, 6, -1), new HaarRectangle(14, 0, 3, 6, 2));
            stageNodes.AddHaarFeature(-0.0012953069526702166, 0.2913674116134644, 0.528073787689209, new HaarRectangle(5, 3, 3, 1, -1), new HaarRectangle(6, 3, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.0419416800141335, 0.7554799914360046, 0.4856030941009522, new HaarRectangle(14, 0, 6, 6, -1), new HaarRectangle(14, 0, 3, 6, 2));
            stageNodes.AddHaarFeature(-0.0235293805599213, 0.2838279902935028, 0.5256081223487854, new HaarRectangle(0, 3, 20, 10, -1), new HaarRectangle(0, 8, 20, 5, 2));
            stageNodes.AddHaarFeature(0.0408574491739273, 0.4870935082435608, 0.6277297139167786, new HaarRectangle(14, 0, 6, 6, -1), new HaarRectangle(14, 0, 3, 6, 2));
            stageNodes.AddHaarFeature(-0.0254068691283464, 0.7099707722663879, 0.4575029015541077, new HaarRectangle(0, 0, 6, 6, -1), new HaarRectangle(3, 0, 3, 6, 2));
            stageNodes.AddHaarFeature(-0.0004141544050071389, 0.4030886888504028, 0.5469412207603455, new HaarRectangle(19, 15, 1, 2, -1), new HaarRectangle(19, 16, 1, 1, 2));
            stageNodes.AddHaarFeature(0.0218241196125746, 0.4502024054527283, 0.6768701076507568, new HaarRectangle(0, 2, 4, 8, -1), new HaarRectangle(2, 2, 2, 8, 2));
            stageNodes.AddHaarFeature(0.0141140399500728, 0.5442860722541809, 0.3791700005531311, new HaarRectangle(2, 1, 18, 4, -1), new HaarRectangle(11, 1, 9, 2, 2), new HaarRectangle(2, 3, 9, 2, 2));
            stageNodes.AddHaarFeature(0.00006721459067193791, 0.4200463891029358, 0.5873476266860962, new HaarRectangle(8, 12, 1, 2, -1), new HaarRectangle(8, 13, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.00794176384806633, 0.3792561888694763, 0.5585265755653381, new HaarRectangle(5, 2, 10, 6, -1), new HaarRectangle(10, 2, 5, 3, 2), new HaarRectangle(5, 5, 5, 3, 2));
            stageNodes.AddHaarFeature(-0.007214440964162351, 0.7253103852272034, 0.4603548943996429, new HaarRectangle(9, 7, 2, 4, -1), new HaarRectangle(10, 7, 1, 4, 2));
            stageNodes.AddHaarFeature(0.002581733977422118, 0.4693301916122437, 0.5900238752365112, new HaarRectangle(9, 7, 3, 3, -1), new HaarRectangle(10, 7, 1, 3, 3));
            stageNodes.AddHaarFeature(0.1340931951999664, 0.5149213075637817, 0.1808844953775406, new HaarRectangle(4, 5, 12, 8, -1), new HaarRectangle(8, 5, 4, 8, 3));
            stageNodes.AddHaarFeature(0.0022962710354477167, 0.5399743914604187, 0.3717867136001587, new HaarRectangle(15, 15, 4, 3, -1), new HaarRectangle(15, 16, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.002157584996894002, 0.2408495992422104, 0.5148863792419434, new HaarRectangle(8, 18, 3, 1, -1), new HaarRectangle(9, 18, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.004919618833810091, 0.6573588252067566, 0.4738740026950836, new HaarRectangle(9, 13, 4, 3, -1), new HaarRectangle(9, 14, 4, 1, 3));
            stageNodes.AddHaarFeature(0.0016267469618469477, 0.4192821979522705, 0.6303114295005798, new HaarRectangle(7, 13, 4, 3, -1), new HaarRectangle(7, 14, 4, 1, 3));
            stageNodes.AddHaarFeature(0.00033413388882763684, 0.5540298223495483, 0.3702101111412048, new HaarRectangle(19, 15, 1, 2, -1), new HaarRectangle(19, 16, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0266980808228254, 0.1710917949676514, 0.5101410746574402, new HaarRectangle(0, 15, 8, 4, -1), new HaarRectangle(0, 17, 8, 2, 2));
            stageNodes.AddHaarFeature(-0.0305618792772293, 0.1904218047857285, 0.5168793797492981, new HaarRectangle(9, 3, 6, 4, -1), new HaarRectangle(11, 3, 2, 4, 3));
            stageNodes.AddHaarFeature(0.002851154888048768, 0.4447506964206696, 0.6313853859901428, new HaarRectangle(8, 14, 4, 3, -1), new HaarRectangle(8, 15, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.0362114794552326, 0.2490727007389069, 0.5377349257469177, new HaarRectangle(3, 14, 14, 6, -1), new HaarRectangle(3, 16, 14, 2, 3));
            stageNodes.AddHaarFeature(-0.002411518944427371, 0.5381243228912354, 0.3664236962795258, new HaarRectangle(6, 3, 6, 6, -1), new HaarRectangle(6, 6, 6, 3, 2));
            stageNodes.AddHaarFeature(-0.0007725320174358785, 0.5530232191085815, 0.3541550040245056, new HaarRectangle(5, 11, 10, 6, -1), new HaarRectangle(5, 14, 10, 3, 2));
            stageNodes.AddHaarFeature(0.0002948172914329916, 0.4132699072360992, 0.5667243003845215, new HaarRectangle(3, 10, 3, 4, -1), new HaarRectangle(4, 10, 1, 4, 3));
            stageNodes.AddHaarFeature(-0.006233456078916788, 0.0987872332334518, 0.5198668837547302, new HaarRectangle(13, 9, 2, 2, -1), new HaarRectangle(13, 9, 1, 2, 2));
            stageNodes.AddHaarFeature(-0.0262747295200825, 0.0911274924874306, 0.5028107166290283, new HaarRectangle(5, 3, 6, 4, -1), new HaarRectangle(7, 3, 2, 4, 3));
            stageNodes.AddHaarFeature(0.005321226082742214, 0.4726648926734924, 0.6222720742225647, new HaarRectangle(9, 7, 3, 3, -1), new HaarRectangle(10, 7, 1, 3, 3));
            stageNodes.AddHaarFeature(-0.004112905822694302, 0.2157457023859024, 0.5137804746627808, new HaarRectangle(2, 12, 2, 3, -1), new HaarRectangle(2, 13, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0032457809429615736, 0.5410770773887634, 0.3721776902675629, new HaarRectangle(9, 8, 3, 12, -1), new HaarRectangle(9, 12, 3, 4, 3));
            stageNodes.AddHaarFeature(-0.0163597092032433, 0.7787874937057495, 0.4685291945934296, new HaarRectangle(3, 14, 4, 6, -1), new HaarRectangle(3, 14, 2, 3, 2), new HaarRectangle(5, 17, 2, 3, 2));
            stageNodes.AddHaarFeature(0.00032166109303943813, 0.5478987097740173, 0.4240373969078064, new HaarRectangle(16, 15, 2, 2, -1), new HaarRectangle(16, 16, 2, 1, 2));
            stageNodes.AddHaarFeature(0.000644524407107383, 0.5330560803413391, 0.3501324951648712, new HaarRectangle(2, 15, 2, 2, -1), new HaarRectangle(2, 16, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.0078909732401371, 0.6923521161079407, 0.4726569056510925, new HaarRectangle(8, 12, 4, 3, -1), new HaarRectangle(8, 13, 4, 1, 3));
            stageNodes.AddHaarFeature(0.048336211591959, 0.50559002161026, 0.0757492035627365, new HaarRectangle(0, 7, 20, 1, -1), new HaarRectangle(10, 7, 10, 1, 2));
            stageNodes.AddHaarFeature(-0.000751781277358532, 0.3783741891384125, 0.5538573861122131, new HaarRectangle(7, 6, 8, 3, -1), new HaarRectangle(7, 6, 4, 3, 2));
            stageNodes.AddHaarFeature(-0.002495391061529517, 0.3081651031970978, 0.5359612107276917, new HaarRectangle(5, 7, 8, 2, -1), new HaarRectangle(9, 7, 4, 2, 2));
            stageNodes.AddHaarFeature(-0.0022385010961443186, 0.663395881652832, 0.4649342894554138, new HaarRectangle(9, 7, 3, 5, -1), new HaarRectangle(10, 7, 1, 5, 3));
            stageNodes.AddHaarFeature(-0.0017988430336117744, 0.6596844792366028, 0.4347187876701355, new HaarRectangle(8, 7, 3, 5, -1), new HaarRectangle(9, 7, 1, 5, 3));
            stageNodes.AddHaarFeature(0.008786091580986977, 0.523183286190033, 0.2315579950809479, new HaarRectangle(11, 1, 3, 5, -1), new HaarRectangle(12, 1, 1, 5, 3));
            stageNodes.AddHaarFeature(0.003671538084745407, 0.520425021648407, 0.2977376878261566, new HaarRectangle(6, 2, 3, 6, -1), new HaarRectangle(7, 2, 1, 6, 3));
            stageNodes.AddHaarFeature(-0.0353364497423172, 0.7238878011703491, 0.4861505031585693, new HaarRectangle(14, 14, 6, 5, -1), new HaarRectangle(14, 14, 3, 5, 2));
            stageNodes.AddHaarFeature(-0.0006918924045749009, 0.3105022013187408, 0.5229824781417847, new HaarRectangle(9, 8, 2, 2, -1), new HaarRectangle(9, 9, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.003394610946998, 0.3138968050479889, 0.5210173726081848, new HaarRectangle(10, 7, 1, 3, -1), new HaarRectangle(10, 8, 1, 1, 3));
            stageNodes.AddHaarFeature(0.0009856928372755647, 0.4536580145359039, 0.6585097908973694, new HaarRectangle(6, 6, 2, 2, -1), new HaarRectangle(6, 6, 1, 1, 2), new HaarRectangle(7, 7, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0501631014049053, 0.1804454028606415, 0.5198916792869568, new HaarRectangle(2, 11, 18, 4, -1), new HaarRectangle(11, 11, 9, 2, 2), new HaarRectangle(2, 13, 9, 2, 2));
            stageNodes.AddHaarFeature(-0.0022367259953171015, 0.7255702018737793, 0.4651359021663666, new HaarRectangle(6, 6, 2, 2, -1), new HaarRectangle(6, 6, 1, 1, 2), new HaarRectangle(7, 7, 1, 1, 2));
            stageNodes.AddHaarFeature(0.0007432628772221506, 0.4412921071052551, 0.5898545980453491, new HaarRectangle(0, 15, 20, 2, -1), new HaarRectangle(0, 16, 20, 1, 2));
            stageNodes.AddHaarFeature(-0.0009348518215119839, 0.3500052988529205, 0.5366017818450928, new HaarRectangle(4, 14, 2, 3, -1), new HaarRectangle(4, 15, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0174979399889708, 0.4912194907665253, 0.8315284848213196, new HaarRectangle(8, 14, 4, 3, -1), new HaarRectangle(8, 15, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.0015200000489130616, 0.3570275902748108, 0.537056028842926, new HaarRectangle(8, 7, 2, 3, -1), new HaarRectangle(8, 8, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0007800394087098539, 0.4353772103786469, 0.5967335104942322, new HaarRectangle(9, 10, 2, 3, -1), new HaarRectangle(9, 11, 2, 1, 3));
            stageNodeCounts.Add(stageNodes.Count - index); index = stageNodes.Count;

            stageThresholds.Add(50.61048126220703);
            stageNodes.AddHaarFeature(-0.00999455526471138, 0.6162583231925964, 0.3054533004760742, new HaarRectangle(5, 4, 10, 4, -1), new HaarRectangle(5, 6, 10, 2, 2));
            stageNodes.AddHaarFeature(-0.0011085229925811293, 0.5818294882774353, 0.3155578076839447, new HaarRectangle(9, 7, 6, 4, -1), new HaarRectangle(12, 7, 3, 2, 2), new HaarRectangle(9, 9, 3, 2, 2));
            stageNodes.AddHaarFeature(0.001036438043229282, 0.2552052140235901, 0.5692911744117737, new HaarRectangle(4, 7, 3, 6, -1), new HaarRectangle(4, 9, 3, 2, 3));
            stageNodes.AddHaarFeature(0.000682113110087812, 0.3685089945793152, 0.5934931039810181, new HaarRectangle(11, 15, 4, 4, -1), new HaarRectangle(13, 15, 2, 2, 2), new HaarRectangle(11, 17, 2, 2, 2));
            stageNodes.AddHaarFeature(-0.0006805734010413289, 0.2332392036914825, 0.5474792122840881, new HaarRectangle(7, 8, 4, 2, -1), new HaarRectangle(7, 9, 4, 1, 2));
            stageNodes.AddHaarFeature(0.0002606878988444805, 0.325745701789856, 0.5667545795440674, new HaarRectangle(13, 1, 4, 3, -1), new HaarRectangle(13, 1, 2, 3, 2));
            stageNodes.AddHaarFeature(0.0005160737200640142, 0.3744716942310333, 0.5845472812652588, new HaarRectangle(5, 15, 4, 4, -1), new HaarRectangle(5, 15, 2, 2, 2), new HaarRectangle(7, 17, 2, 2, 2));
            stageNodes.AddHaarFeature(0.0008500752155669034, 0.3420371115207672, 0.5522807240486145, new HaarRectangle(9, 5, 4, 7, -1), new HaarRectangle(9, 5, 2, 7, 2));
            stageNodes.AddHaarFeature(-0.0018607829697430136, 0.2804419994354248, 0.5375424027442932, new HaarRectangle(5, 6, 8, 3, -1), new HaarRectangle(9, 6, 4, 3, 2));
            stageNodes.AddHaarFeature(-0.001503397012129426, 0.2579050958156586, 0.5498952269554138, new HaarRectangle(9, 9, 2, 2, -1), new HaarRectangle(9, 10, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0023478909861296415, 0.4175156056880951, 0.6313710808753967, new HaarRectangle(7, 15, 5, 3, -1), new HaarRectangle(7, 16, 5, 1, 3));
            stageNodes.AddHaarFeature(-0.00028880240279249847, 0.5865169763565063, 0.4052666127681732, new HaarRectangle(11, 10, 4, 3, -1), new HaarRectangle(11, 10, 2, 3, 2));
            stageNodes.AddHaarFeature(0.008940547704696655, 0.5211141109466553, 0.231865406036377, new HaarRectangle(6, 9, 8, 10, -1), new HaarRectangle(6, 14, 8, 5, 2));
            stageNodes.AddHaarFeature(-0.0193277392536402, 0.2753432989120483, 0.5241525769233704, new HaarRectangle(10, 11, 6, 2, -1), new HaarRectangle(10, 11, 3, 2, 2));
            stageNodes.AddHaarFeature(-0.0002020206011366099, 0.5722978711128235, 0.3677195906639099, new HaarRectangle(4, 11, 6, 2, -1), new HaarRectangle(7, 11, 3, 2, 2));
            stageNodes.AddHaarFeature(0.002117906929925084, 0.4466108083724976, 0.5542430877685547, new HaarRectangle(11, 3, 8, 1, -1), new HaarRectangle(11, 3, 4, 1, 2));
            stageNodes.AddHaarFeature(-0.0017743760254234076, 0.2813253104686737, 0.5300959944725037, new HaarRectangle(6, 3, 3, 2, -1), new HaarRectangle(7, 3, 1, 2, 3));
            stageNodes.AddHaarFeature(0.004223445896059275, 0.439970999956131, 0.5795428156852722, new HaarRectangle(14, 5, 6, 5, -1), new HaarRectangle(14, 5, 3, 5, 2));
            stageNodes.AddHaarFeature(-0.0143752200528979, 0.2981117963790894, 0.5292059183120728, new HaarRectangle(7, 5, 2, 12, -1), new HaarRectangle(7, 11, 2, 6, 2));
            stageNodes.AddHaarFeature(-0.0153491804376245, 0.7705215215682983, 0.4748171865940094, new HaarRectangle(8, 11, 4, 3, -1), new HaarRectangle(8, 12, 4, 1, 3));
            stageNodes.AddHaarFeature(0.000015152279956964776, 0.3718844056129456, 0.5576897263526917, new HaarRectangle(4, 1, 2, 3, -1), new HaarRectangle(5, 1, 1, 3, 2));
            stageNodes.AddHaarFeature(-0.009129391983151436, 0.3615196049213409, 0.5286766886711121, new HaarRectangle(18, 3, 2, 6, -1), new HaarRectangle(18, 5, 2, 2, 3));
            stageNodes.AddHaarFeature(0.0022512159775942564, 0.5364704728126526, 0.3486298024654388, new HaarRectangle(0, 3, 2, 6, -1), new HaarRectangle(0, 5, 2, 2, 3));
            stageNodes.AddHaarFeature(-0.0049696918576955795, 0.6927651762962341, 0.4676836133003235, new HaarRectangle(9, 12, 2, 3, -1), new HaarRectangle(9, 13, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0128290103748441, 0.7712153792381287, 0.4660735130310059, new HaarRectangle(7, 13, 4, 3, -1), new HaarRectangle(7, 14, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.009366006590425968, 0.3374983966350555, 0.5351287722587585, new HaarRectangle(18, 0, 2, 6, -1), new HaarRectangle(18, 2, 2, 2, 3));
            stageNodes.AddHaarFeature(0.0032452319283038378, 0.5325189828872681, 0.3289610147476196, new HaarRectangle(0, 0, 2, 6, -1), new HaarRectangle(0, 2, 2, 2, 3));
            stageNodes.AddHaarFeature(-0.0117235602810979, 0.6837652921676636, 0.4754300117492676, new HaarRectangle(8, 14, 6, 3, -1), new HaarRectangle(8, 15, 6, 1, 3));
            stageNodes.AddHaarFeature(0.00002925794069597032, 0.357208788394928, 0.5360502004623413, new HaarRectangle(7, 4, 2, 4, -1), new HaarRectangle(8, 4, 1, 4, 2));
            stageNodes.AddHaarFeature(-0.000022244219508138485, 0.5541427135467529, 0.3552064001560211, new HaarRectangle(8, 5, 4, 6, -1), new HaarRectangle(8, 7, 4, 2, 3));
            stageNodes.AddHaarFeature(0.005088150966912508, 0.5070844292640686, 0.1256462037563324, new HaarRectangle(6, 4, 2, 2, -1), new HaarRectangle(7, 4, 1, 2, 2));
            stageNodes.AddHaarFeature(0.0274296794086695, 0.5269560217857361, 0.1625818014144898, new HaarRectangle(3, 14, 14, 4, -1), new HaarRectangle(10, 14, 7, 2, 2), new HaarRectangle(3, 16, 7, 2, 2));
            stageNodes.AddHaarFeature(-0.00641428679227829, 0.7145588994026184, 0.4584197103977203, new HaarRectangle(6, 15, 6, 2, -1), new HaarRectangle(6, 15, 3, 1, 2), new HaarRectangle(9, 16, 3, 1, 2));
            stageNodes.AddHaarFeature(0.003347995923832059, 0.5398612022399902, 0.3494696915149689, new HaarRectangle(14, 15, 6, 2, -1), new HaarRectangle(14, 16, 6, 1, 2));
            stageNodes.AddHaarFeature(-0.0826354920864105, 0.2439192980527878, 0.5160226225852966, new HaarRectangle(2, 12, 12, 8, -1), new HaarRectangle(2, 16, 12, 4, 2));
            stageNodes.AddHaarFeature(0.0010261740535497663, 0.3886891901493073, 0.5767908096313477, new HaarRectangle(7, 7, 7, 2, -1), new HaarRectangle(7, 8, 7, 1, 2));
            stageNodes.AddHaarFeature(-0.0016307090409100056, 0.3389458060264587, 0.5347700715065002, new HaarRectangle(0, 2, 18, 2, -1), new HaarRectangle(0, 3, 18, 1, 2));
            stageNodes.AddHaarFeature(0.0024546680506318808, 0.4601413905620575, 0.638724684715271, new HaarRectangle(9, 6, 2, 5, -1), new HaarRectangle(9, 6, 1, 5, 2));
            stageNodes.AddHaarFeature(-0.0009947651997208595, 0.5769879221916199, 0.4120396077632904, new HaarRectangle(7, 5, 3, 8, -1), new HaarRectangle(8, 5, 1, 8, 3));
            stageNodes.AddHaarFeature(0.0154091902077198, 0.4878709018230438, 0.7089822292327881, new HaarRectangle(9, 6, 3, 4, -1), new HaarRectangle(10, 6, 1, 4, 3));
            stageNodes.AddHaarFeature(0.0011784400558099153, 0.5263553261756897, 0.2895244956016541, new HaarRectangle(4, 13, 3, 2, -1), new HaarRectangle(4, 14, 3, 1, 2));
            stageNodes.AddHaarFeature(-0.0277019198983908, 0.149882897734642, 0.5219606757164001, new HaarRectangle(9, 4, 6, 3, -1), new HaarRectangle(11, 4, 2, 3, 3));
            stageNodes.AddHaarFeature(-0.0295053999871016, 0.024893319234252, 0.4999816119670868, new HaarRectangle(5, 4, 6, 3, -1), new HaarRectangle(7, 4, 2, 3, 3));
            stageNodes.AddHaarFeature(0.0004515943001024425, 0.5464622974395752, 0.4029662907123566, new HaarRectangle(14, 11, 5, 2, -1), new HaarRectangle(14, 12, 5, 1, 2));
            stageNodes.AddHaarFeature(0.007177263963967562, 0.4271056950092316, 0.5866296887397766, new HaarRectangle(1, 2, 6, 9, -1), new HaarRectangle(3, 2, 2, 9, 3));
            stageNodes.AddHaarFeature(-0.0741820484399796, 0.6874179244041443, 0.4919027984142304, new HaarRectangle(14, 6, 6, 13, -1), new HaarRectangle(14, 6, 3, 13, 2));
            stageNodes.AddHaarFeature(-0.0172541607171297, 0.3370676040649414, 0.534873902797699, new HaarRectangle(3, 6, 14, 8, -1), new HaarRectangle(3, 6, 7, 4, 2), new HaarRectangle(10, 10, 7, 4, 2));
            stageNodes.AddHaarFeature(0.0148515598848462, 0.4626792967319489, 0.6129904985427856, new HaarRectangle(16, 0, 4, 11, -1), new HaarRectangle(16, 0, 2, 11, 2));
            stageNodes.AddHaarFeature(0.0100020002573729, 0.5346122980117798, 0.3423453867435455, new HaarRectangle(3, 4, 12, 12, -1), new HaarRectangle(3, 4, 6, 6, 2), new HaarRectangle(9, 10, 6, 6, 2));
            stageNodes.AddHaarFeature(0.0020138120744377375, 0.4643830060958862, 0.5824304223060608, new HaarRectangle(11, 4, 5, 3, -1), new HaarRectangle(11, 5, 5, 1, 3));
            stageNodes.AddHaarFeature(0.0015135470312088728, 0.5196396112442017, 0.2856149971485138, new HaarRectangle(4, 11, 4, 2, -1), new HaarRectangle(4, 12, 4, 1, 2));
            stageNodes.AddHaarFeature(0.003138143103569746, 0.4838162958621979, 0.5958529710769653, new HaarRectangle(10, 7, 2, 2, -1), new HaarRectangle(10, 7, 1, 2, 2));
            stageNodes.AddHaarFeature(-0.005145044066011906, 0.8920302987098694, 0.4741412103176117, new HaarRectangle(8, 7, 2, 2, -1), new HaarRectangle(9, 7, 1, 2, 2));
            stageNodes.AddHaarFeature(-0.004473670851439238, 0.2033942937850952, 0.5337278842926025, new HaarRectangle(9, 17, 3, 2, -1), new HaarRectangle(10, 17, 1, 2, 3));
            stageNodes.AddHaarFeature(0.0019628470763564114, 0.457163393497467, 0.6725863218307495, new HaarRectangle(5, 6, 3, 3, -1), new HaarRectangle(5, 7, 3, 1, 3));
            stageNodes.AddHaarFeature(0.005426045041531324, 0.5271108150482178, 0.2845670878887177, new HaarRectangle(10, 0, 3, 3, -1), new HaarRectangle(11, 0, 1, 3, 3));
            stageNodes.AddHaarFeature(0.0004961146041750908, 0.4138312935829163, 0.5718597769737244, new HaarRectangle(5, 6, 6, 2, -1), new HaarRectangle(5, 6, 3, 1, 2), new HaarRectangle(8, 7, 3, 1, 2));
            stageNodes.AddHaarFeature(0.009372878819704056, 0.5225151181221008, 0.2804847061634064, new HaarRectangle(12, 16, 4, 3, -1), new HaarRectangle(12, 17, 4, 1, 3));
            stageNodes.AddHaarFeature(0.0006050089723430574, 0.523676872253418, 0.3314523994922638, new HaarRectangle(3, 12, 3, 2, -1), new HaarRectangle(3, 13, 3, 1, 2));
            stageNodes.AddHaarFeature(0.0005679255118593574, 0.4531059861183167, 0.6276971101760864, new HaarRectangle(9, 12, 3, 2, -1), new HaarRectangle(9, 13, 3, 1, 2));
            stageNodes.AddHaarFeature(0.0246443394571543, 0.5130851864814758, 0.2017143964767456, new HaarRectangle(1, 11, 16, 4, -1), new HaarRectangle(1, 11, 8, 2, 2), new HaarRectangle(9, 13, 8, 2, 2));
            stageNodes.AddHaarFeature(-0.0102904504165053, 0.7786595225334167, 0.4876641035079956, new HaarRectangle(12, 4, 3, 3, -1), new HaarRectangle(12, 5, 3, 1, 3));
            stageNodes.AddHaarFeature(0.002062941901385784, 0.4288598895072937, 0.5881264209747314, new HaarRectangle(4, 4, 5, 3, -1), new HaarRectangle(4, 5, 5, 1, 3));
            stageNodes.AddHaarFeature(-0.005051948130130768, 0.3523977994918823, 0.5286008715629578, new HaarRectangle(12, 16, 4, 3, -1), new HaarRectangle(12, 17, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.0057692620903253555, 0.6841086149215698, 0.4588094055652618, new HaarRectangle(5, 4, 3, 3, -1), new HaarRectangle(5, 5, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.00045789941214025026, 0.356552004814148, 0.5485978126525879, new HaarRectangle(9, 0, 2, 2, -1), new HaarRectangle(9, 1, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.0007591883768327534, 0.336879312992096, 0.5254197120666504, new HaarRectangle(8, 9, 4, 2, -1), new HaarRectangle(8, 10, 4, 1, 2));
            stageNodes.AddHaarFeature(-0.001773725962266326, 0.3422161042690277, 0.5454015135765076, new HaarRectangle(8, 8, 4, 3, -1), new HaarRectangle(8, 9, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.008561046794056892, 0.6533612012863159, 0.4485856890678406, new HaarRectangle(0, 13, 6, 3, -1), new HaarRectangle(2, 13, 2, 3, 3));
            stageNodes.AddHaarFeature(0.0017277270089834929, 0.5307580232620239, 0.3925352990627289, new HaarRectangle(16, 14, 3, 2, -1), new HaarRectangle(16, 15, 3, 1, 2));
            stageNodes.AddHaarFeature(-0.0281996093690395, 0.685745894908905, 0.4588584005832672, new HaarRectangle(1, 18, 18, 2, -1), new HaarRectangle(7, 18, 6, 2, 3));
            stageNodes.AddHaarFeature(-0.001778110978193581, 0.4037851095199585, 0.5369856953620911, new HaarRectangle(16, 14, 3, 2, -1), new HaarRectangle(16, 15, 3, 1, 2));
            stageNodes.AddHaarFeature(0.00033177141449414194, 0.539979875087738, 0.3705750107765198, new HaarRectangle(1, 14, 3, 2, -1), new HaarRectangle(1, 15, 3, 1, 2));
            stageNodes.AddHaarFeature(0.0026385399978607893, 0.4665437042713165, 0.6452730894088745, new HaarRectangle(7, 14, 6, 3, -1), new HaarRectangle(7, 15, 6, 1, 3));
            stageNodes.AddHaarFeature(-0.0021183069329708815, 0.5914781093597412, 0.4064677059650421, new HaarRectangle(5, 14, 8, 3, -1), new HaarRectangle(5, 15, 8, 1, 3));
            stageNodes.AddHaarFeature(-0.0147732896730304, 0.3642038106918335, 0.5294762849807739, new HaarRectangle(10, 6, 4, 14, -1), new HaarRectangle(10, 6, 2, 14, 2));
            stageNodes.AddHaarFeature(-0.0168154407292604, 0.2664231956005096, 0.5144972801208496, new HaarRectangle(6, 6, 4, 14, -1), new HaarRectangle(8, 6, 2, 14, 2));
            stageNodes.AddHaarFeature(-0.006337014026939869, 0.6779531240463257, 0.4852097928524017, new HaarRectangle(13, 5, 2, 3, -1), new HaarRectangle(13, 6, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.00004456004899111576, 0.5613964796066284, 0.4153054058551788, new HaarRectangle(7, 16, 6, 1, -1), new HaarRectangle(9, 16, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0010240620467811825, 0.5964478254318237, 0.4566304087638855, new HaarRectangle(9, 12, 3, 3, -1), new HaarRectangle(9, 13, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.00231616897508502, 0.2976115047931671, 0.5188159942626953, new HaarRectangle(7, 0, 3, 3, -1), new HaarRectangle(8, 0, 1, 3, 3));
            stageNodes.AddHaarFeature(0.5321757197380066, 0.5187839269638062, 0.220263198018074, new HaarRectangle(4, 0, 16, 18, -1), new HaarRectangle(4, 9, 16, 9, 2));
            stageNodes.AddHaarFeature(-0.1664305031299591, 0.1866022944450378, 0.5060343146324158, new HaarRectangle(1, 1, 16, 14, -1), new HaarRectangle(1, 8, 16, 7, 2));
            stageNodes.AddHaarFeature(0.112535297870636, 0.5212125182151794, 0.1185022965073586, new HaarRectangle(3, 9, 15, 4, -1), new HaarRectangle(8, 9, 5, 4, 3));
            stageNodes.AddHaarFeature(0.009304686449468136, 0.4589937031269074, 0.6826149225234985, new HaarRectangle(6, 12, 7, 3, -1), new HaarRectangle(6, 13, 7, 1, 3));
            stageNodes.AddHaarFeature(-0.004625509958714247, 0.3079940974712372, 0.5225008726119995, new HaarRectangle(14, 15, 2, 3, -1), new HaarRectangle(14, 16, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.1111646965146065, 0.2101044058799744, 0.5080801844596863, new HaarRectangle(2, 3, 16, 14, -1), new HaarRectangle(2, 3, 8, 7, 2), new HaarRectangle(10, 10, 8, 7, 2));
            stageNodes.AddHaarFeature(-0.0108884396031499, 0.5765355229377747, 0.4790464043617249, new HaarRectangle(16, 2, 4, 18, -1), new HaarRectangle(18, 2, 2, 9, 2), new HaarRectangle(16, 11, 2, 9, 2));
            stageNodes.AddHaarFeature(0.005856430158019066, 0.5065100193023682, 0.1563598960638046, new HaarRectangle(4, 15, 2, 3, -1), new HaarRectangle(4, 16, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0548543892800808, 0.49669149518013, 0.7230510711669922, new HaarRectangle(16, 2, 4, 18, -1), new HaarRectangle(18, 2, 2, 9, 2), new HaarRectangle(16, 11, 2, 9, 2));
            stageNodes.AddHaarFeature(-0.0111973397433758, 0.2194979041814804, 0.5098798274993896, new HaarRectangle(1, 1, 8, 3, -1), new HaarRectangle(1, 2, 8, 1, 3));
            stageNodes.AddHaarFeature(0.004406907130032778, 0.4778401851654053, 0.6770902872085571, new HaarRectangle(8, 11, 4, 3, -1), new HaarRectangle(8, 12, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.0636652931571007, 0.1936362981796265, 0.5081024169921875, new HaarRectangle(5, 11, 5, 9, -1), new HaarRectangle(5, 14, 5, 3, 3));
            stageNodes.AddHaarFeature(-0.009808149188756943, 0.599906325340271, 0.4810341000556946, new HaarRectangle(16, 0, 4, 11, -1), new HaarRectangle(16, 0, 2, 11, 2));
            stageNodes.AddHaarFeature(-0.0021717099007219076, 0.3338333964347839, 0.5235472917556763, new HaarRectangle(7, 0, 6, 1, -1), new HaarRectangle(9, 0, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0133155202493072, 0.6617069840431213, 0.4919213056564331, new HaarRectangle(16, 3, 3, 7, -1), new HaarRectangle(17, 3, 1, 7, 3));
            stageNodes.AddHaarFeature(0.002544207964092493, 0.4488744139671326, 0.6082184910774231, new HaarRectangle(1, 3, 3, 7, -1), new HaarRectangle(2, 3, 1, 7, 3));
            stageNodes.AddHaarFeature(0.0120378397405148, 0.540939211845398, 0.3292432129383087, new HaarRectangle(7, 8, 6, 12, -1), new HaarRectangle(7, 12, 6, 4, 3));
            stageNodes.AddHaarFeature(-0.0207010507583618, 0.6819120049476624, 0.4594995975494385, new HaarRectangle(0, 0, 4, 11, -1), new HaarRectangle(2, 0, 2, 11, 2));
            stageNodes.AddHaarFeature(0.0276082791388035, 0.4630792140960693, 0.5767282843589783, new HaarRectangle(14, 0, 6, 20, -1), new HaarRectangle(14, 0, 3, 20, 2));
            stageNodes.AddHaarFeature(0.0012370620388537643, 0.5165379047393799, 0.2635016143321991, new HaarRectangle(0, 3, 1, 2, -1), new HaarRectangle(0, 4, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.037669338285923, 0.2536393105983734, 0.5278980135917664, new HaarRectangle(5, 5, 10, 8, -1), new HaarRectangle(10, 5, 5, 4, 2), new HaarRectangle(5, 9, 5, 4, 2));
            stageNodes.AddHaarFeature(-0.00180572597309947, 0.3985156118869782, 0.5517500042915344, new HaarRectangle(4, 7, 12, 4, -1), new HaarRectangle(4, 7, 6, 2, 2), new HaarRectangle(10, 9, 6, 2, 2));
            stageNodeCounts.Add(stageNodes.Count - index); index = stageNodes.Count;

            stageThresholds.Add(54.62007141113281);
            stageNodes.AddHaarFeature(0.004429902881383896, 0.2891018092632294, 0.633522629737854, new HaarRectangle(2, 1, 6, 4, -1), new HaarRectangle(5, 1, 3, 4, 2));
            stageNodes.AddHaarFeature(-0.0023813319858163595, 0.621178925037384, 0.3477487862110138, new HaarRectangle(9, 7, 6, 4, -1), new HaarRectangle(12, 7, 3, 2, 2), new HaarRectangle(9, 9, 3, 2, 2));
            stageNodes.AddHaarFeature(0.0022915711160749197, 0.2254412025213242, 0.5582118034362793, new HaarRectangle(5, 6, 2, 6, -1), new HaarRectangle(5, 9, 2, 3, 2));
            stageNodes.AddHaarFeature(0.0009945794008672237, 0.3711710870265961, 0.5930070877075195, new HaarRectangle(9, 16, 6, 4, -1), new HaarRectangle(12, 16, 3, 2, 2), new HaarRectangle(9, 18, 3, 2, 2));
            stageNodes.AddHaarFeature(0.0007716466789133847, 0.565172016620636, 0.334799587726593, new HaarRectangle(9, 4, 2, 12, -1), new HaarRectangle(9, 10, 2, 6, 2));
            stageNodes.AddHaarFeature(-0.001138641033321619, 0.3069126009941101, 0.5508630871772766, new HaarRectangle(7, 1, 6, 18, -1), new HaarRectangle(9, 1, 2, 18, 3));
            stageNodes.AddHaarFeature(-0.0001640303962631151, 0.576282799243927, 0.3699047863483429, new HaarRectangle(4, 12, 12, 2, -1), new HaarRectangle(8, 12, 4, 2, 3));
            stageNodes.AddHaarFeature(0.000029793529392918572, 0.2644244134426117, 0.5437911152839661, new HaarRectangle(8, 8, 6, 2, -1), new HaarRectangle(8, 9, 6, 1, 2));
            stageNodes.AddHaarFeature(0.008577490225434303, 0.5051138997077942, 0.1795724928379059, new HaarRectangle(8, 0, 3, 6, -1), new HaarRectangle(9, 0, 1, 6, 3));
            stageNodes.AddHaarFeature(-0.0002603268949314952, 0.5826969146728516, 0.4446826875209808, new HaarRectangle(11, 18, 3, 2, -1), new HaarRectangle(11, 19, 3, 1, 2));
            stageNodes.AddHaarFeature(-0.006140463054180145, 0.3113852143287659, 0.5346971750259399, new HaarRectangle(1, 1, 17, 4, -1), new HaarRectangle(1, 3, 17, 2, 2));
            stageNodes.AddHaarFeature(-0.0230869501829147, 0.32779461145401, 0.533119797706604, new HaarRectangle(11, 8, 4, 12, -1), new HaarRectangle(11, 8, 2, 12, 2));
            stageNodes.AddHaarFeature(-0.0142436502501369, 0.7381709814071655, 0.4588063061237335, new HaarRectangle(8, 14, 4, 3, -1), new HaarRectangle(8, 15, 4, 1, 3));
            stageNodes.AddHaarFeature(0.0194871295243502, 0.5256630778312683, 0.2274471968412399, new HaarRectangle(12, 3, 2, 17, -1), new HaarRectangle(12, 3, 1, 17, 2));
            stageNodes.AddHaarFeature(-0.0009668110869824886, 0.5511230826377869, 0.3815006911754608, new HaarRectangle(4, 7, 6, 1, -1), new HaarRectangle(6, 7, 2, 1, 3));
            stageNodes.AddHaarFeature(0.003147470997646451, 0.5425636768341064, 0.2543726861476898, new HaarRectangle(18, 3, 2, 3, -1), new HaarRectangle(18, 4, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.00018026070029009136, 0.5380191802978516, 0.3406304121017456, new HaarRectangle(8, 4, 3, 4, -1), new HaarRectangle(8, 6, 3, 2, 2));
            stageNodes.AddHaarFeature(-0.006026626098901033, 0.3035801947116852, 0.54205721616745, new HaarRectangle(4, 5, 12, 10, -1), new HaarRectangle(4, 10, 12, 5, 2));
            stageNodes.AddHaarFeature(0.00044462960795499384, 0.3990997076034546, 0.5660110116004944, new HaarRectangle(5, 18, 4, 2, -1), new HaarRectangle(7, 18, 2, 2, 2));
            stageNodes.AddHaarFeature(0.0022609760053455834, 0.5562806725502014, 0.3940688073635101, new HaarRectangle(17, 2, 3, 6, -1), new HaarRectangle(17, 4, 3, 2, 3));
            stageNodes.AddHaarFeature(0.0511330589652061, 0.4609653949737549, 0.7118561863899231, new HaarRectangle(7, 7, 6, 6, -1), new HaarRectangle(9, 7, 2, 6, 3));
            stageNodes.AddHaarFeature(-0.0177863091230392, 0.2316166013479233, 0.5322144031524658, new HaarRectangle(17, 2, 3, 6, -1), new HaarRectangle(17, 4, 3, 2, 3));
            stageNodes.AddHaarFeature(-0.004967962857335806, 0.233077198266983, 0.5122029185295105, new HaarRectangle(8, 0, 3, 4, -1), new HaarRectangle(9, 0, 1, 4, 3));
            stageNodes.AddHaarFeature(0.002066768938675523, 0.4657444059848785, 0.6455488204956055, new HaarRectangle(9, 14, 2, 3, -1), new HaarRectangle(9, 15, 2, 1, 3));
            stageNodes.AddHaarFeature(0.007441376801580191, 0.5154392123222351, 0.236163392663002, new HaarRectangle(0, 12, 6, 3, -1), new HaarRectangle(0, 13, 6, 1, 3));
            stageNodes.AddHaarFeature(-0.003627727972343564, 0.6219773292541504, 0.4476661086082459, new HaarRectangle(8, 14, 4, 3, -1), new HaarRectangle(8, 15, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.005353075917810202, 0.1837355047464371, 0.5102208256721497, new HaarRectangle(3, 12, 2, 3, -1), new HaarRectangle(3, 13, 2, 1, 3));
            stageNodes.AddHaarFeature(0.1453091949224472, 0.5145987272262573, 0.1535930931568146, new HaarRectangle(5, 6, 12, 7, -1), new HaarRectangle(9, 6, 4, 7, 3));
            stageNodes.AddHaarFeature(0.0024394490756094456, 0.5343660116195679, 0.3624661862850189, new HaarRectangle(0, 2, 3, 6, -1), new HaarRectangle(0, 4, 3, 2, 3));
            stageNodes.AddHaarFeature(-0.003128339070826769, 0.6215007901191711, 0.4845592081546783, new HaarRectangle(14, 6, 1, 3, -1), new HaarRectangle(14, 7, 1, 1, 3));
            stageNodes.AddHaarFeature(0.0017940260004252195, 0.4299261868000031, 0.5824198126792908, new HaarRectangle(2, 0, 3, 14, -1), new HaarRectangle(3, 0, 1, 14, 3));
            stageNodes.AddHaarFeature(0.0362538211047649, 0.5260334014892578, 0.1439467966556549, new HaarRectangle(12, 14, 5, 6, -1), new HaarRectangle(12, 16, 5, 2, 3));
            stageNodes.AddHaarFeature(-0.005174672231078148, 0.350653886795044, 0.5287045240402222, new HaarRectangle(4, 14, 5, 6, -1), new HaarRectangle(4, 16, 5, 2, 3));
            stageNodes.AddHaarFeature(0.0006538329762406647, 0.4809640944004059, 0.6122040152549744, new HaarRectangle(11, 10, 2, 2, -1), new HaarRectangle(12, 10, 1, 1, 2), new HaarRectangle(11, 11, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0264802295714617, 0.1139362007379532, 0.5045586228370667, new HaarRectangle(5, 0, 3, 14, -1), new HaarRectangle(6, 0, 1, 14, 3));
            stageNodes.AddHaarFeature(-0.0030440660193562508, 0.6352095007896423, 0.4794734120368958, new HaarRectangle(10, 15, 2, 3, -1), new HaarRectangle(10, 16, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0036993520334362984, 0.5131118297576904, 0.2498510926961899, new HaarRectangle(0, 2, 2, 3, -1), new HaarRectangle(0, 3, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.00036762931267730897, 0.54213947057724, 0.3709532022476196, new HaarRectangle(5, 11, 12, 6, -1), new HaarRectangle(5, 14, 12, 3, 2));
            stageNodes.AddHaarFeature(-0.041382260620594, 0.1894959956407547, 0.5081691741943359, new HaarRectangle(6, 11, 3, 9, -1), new HaarRectangle(6, 14, 3, 3, 3));
            stageNodes.AddHaarFeature(-0.0010532729793339968, 0.645436704158783, 0.4783608913421631, new HaarRectangle(11, 10, 2, 2, -1), new HaarRectangle(12, 10, 1, 1, 2), new HaarRectangle(11, 11, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0021648600231856108, 0.6215031147003174, 0.449982613325119, new HaarRectangle(5, 6, 1, 3, -1), new HaarRectangle(5, 7, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.0005674774874933064, 0.3712610900402069, 0.5419334769248962, new HaarRectangle(4, 9, 13, 3, -1), new HaarRectangle(4, 10, 13, 1, 3));
            stageNodes.AddHaarFeature(0.173758402466774, 0.5023643970489502, 0.1215742006897926, new HaarRectangle(1, 7, 15, 6, -1), new HaarRectangle(6, 7, 5, 6, 3));
            stageNodes.AddHaarFeature(-0.0029049699660390615, 0.3240267932415009, 0.5381883978843689, new HaarRectangle(4, 5, 12, 6, -1), new HaarRectangle(8, 5, 4, 6, 3));
            stageNodes.AddHaarFeature(0.0012299539521336555, 0.4165507853031158, 0.5703486204147339, new HaarRectangle(8, 10, 4, 3, -1), new HaarRectangle(8, 11, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.0005432923790067434, 0.3854042887687683, 0.554754912853241, new HaarRectangle(15, 14, 1, 3, -1), new HaarRectangle(15, 15, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.008329725824296474, 0.2204494029283524, 0.5097082853317261, new HaarRectangle(1, 11, 5, 3, -1), new HaarRectangle(1, 12, 5, 1, 3));
            stageNodes.AddHaarFeature(-0.00010417630255687982, 0.560706615447998, 0.4303036034107208, new HaarRectangle(7, 1, 7, 12, -1), new HaarRectangle(7, 7, 7, 6, 2));
            stageNodes.AddHaarFeature(0.0312047004699707, 0.4621657133102417, 0.6982004046440125, new HaarRectangle(0, 1, 6, 10, -1), new HaarRectangle(0, 1, 3, 5, 2), new HaarRectangle(3, 6, 3, 5, 2));
            stageNodes.AddHaarFeature(0.007894350215792656, 0.5269594192504883, 0.226906806230545, new HaarRectangle(16, 1, 4, 3, -1), new HaarRectangle(16, 2, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.004364531021565199, 0.6359223127365112, 0.4537956118583679, new HaarRectangle(5, 5, 2, 3, -1), new HaarRectangle(5, 6, 2, 1, 3));
            stageNodes.AddHaarFeature(0.007679305970668792, 0.5274767875671387, 0.274048388004303, new HaarRectangle(12, 2, 3, 5, -1), new HaarRectangle(13, 2, 1, 5, 3));
            stageNodes.AddHaarFeature(-0.0254311393946409, 0.2038519978523254, 0.5071732997894287, new HaarRectangle(0, 3, 4, 6, -1), new HaarRectangle(0, 5, 4, 2, 3));
            stageNodes.AddHaarFeature(0.0008200060110539198, 0.4587455093860626, 0.6119868159294128, new HaarRectangle(8, 12, 4, 2, -1), new HaarRectangle(8, 13, 4, 1, 2));
            stageNodes.AddHaarFeature(0.002928460016846657, 0.5071274042129517, 0.2028204947710037, new HaarRectangle(8, 18, 3, 1, -1), new HaarRectangle(9, 18, 1, 1, 3));
            stageNodes.AddHaarFeature(0.00004525647091213614, 0.4812104105949402, 0.5430821776390076, new HaarRectangle(11, 10, 2, 2, -1), new HaarRectangle(12, 10, 1, 1, 2), new HaarRectangle(11, 11, 1, 1, 2));
            stageNodes.AddHaarFeature(0.0013158309739083052, 0.4625813961029053, 0.6779323220252991, new HaarRectangle(7, 10, 2, 2, -1), new HaarRectangle(7, 10, 1, 1, 2), new HaarRectangle(8, 11, 1, 1, 2));
            stageNodes.AddHaarFeature(0.0015870389761403203, 0.5386291742324829, 0.3431465029716492, new HaarRectangle(11, 11, 4, 4, -1), new HaarRectangle(11, 13, 4, 2, 2));
            stageNodes.AddHaarFeature(-0.0215396601706743, 0.025942500680685, 0.5003222823143005, new HaarRectangle(8, 12, 3, 8, -1), new HaarRectangle(9, 12, 1, 8, 3));
            stageNodes.AddHaarFeature(0.014334480278194, 0.5202844738960266, 0.1590632945299149, new HaarRectangle(13, 0, 6, 3, -1), new HaarRectangle(13, 1, 6, 1, 3));
            stageNodes.AddHaarFeature(-0.008388138376176357, 0.728248119354248, 0.4648044109344482, new HaarRectangle(8, 8, 3, 4, -1), new HaarRectangle(9, 8, 1, 4, 3));
            stageNodes.AddHaarFeature(0.00919068418443203, 0.556235671043396, 0.3923191130161285, new HaarRectangle(5, 7, 10, 10, -1), new HaarRectangle(10, 7, 5, 5, 2), new HaarRectangle(5, 12, 5, 5, 2));
            stageNodes.AddHaarFeature(-0.005845305975526571, 0.6803392767906189, 0.4629127979278565, new HaarRectangle(3, 18, 8, 2, -1), new HaarRectangle(3, 18, 4, 1, 2), new HaarRectangle(7, 19, 4, 1, 2));
            stageNodes.AddHaarFeature(-0.0547077991068363, 0.2561671137809753, 0.5206125974655151, new HaarRectangle(10, 2, 6, 8, -1), new HaarRectangle(12, 2, 2, 8, 3));
            stageNodes.AddHaarFeature(0.009114277549088001, 0.518962025642395, 0.3053877055644989, new HaarRectangle(4, 2, 6, 8, -1), new HaarRectangle(6, 2, 2, 8, 3));
            stageNodes.AddHaarFeature(-0.0155750000849366, 0.1295074969530106, 0.5169094800949097, new HaarRectangle(11, 0, 3, 7, -1), new HaarRectangle(12, 0, 1, 7, 3));
            stageNodes.AddHaarFeature(-0.0001205060034408234, 0.5735098123550415, 0.4230825006961823, new HaarRectangle(7, 11, 2, 1, -1), new HaarRectangle(8, 11, 1, 1, 2));
            stageNodes.AddHaarFeature(0.0012273970060050488, 0.5289878249168396, 0.4079791903495789, new HaarRectangle(15, 14, 1, 3, -1), new HaarRectangle(15, 15, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.0012186600361019373, 0.6575639843940735, 0.4574409127235413, new HaarRectangle(7, 15, 2, 2, -1), new HaarRectangle(7, 15, 1, 1, 2), new HaarRectangle(8, 16, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0033256649039685726, 0.3628047108650208, 0.5195019841194153, new HaarRectangle(15, 14, 1, 3, -1), new HaarRectangle(15, 15, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.0132883097976446, 0.1284265965223312, 0.504348874092102, new HaarRectangle(6, 0, 3, 7, -1), new HaarRectangle(7, 0, 1, 7, 3));
            stageNodes.AddHaarFeature(-0.0033839771058410406, 0.6292240023612976, 0.475750595331192, new HaarRectangle(18, 1, 2, 7, -1), new HaarRectangle(18, 1, 1, 7, 2));
            stageNodes.AddHaarFeature(-0.2195422053337097, 0.148773193359375, 0.5065013766288757, new HaarRectangle(2, 0, 8, 20, -1), new HaarRectangle(2, 10, 8, 10, 2));
            stageNodes.AddHaarFeature(0.004911170806735754, 0.425610214471817, 0.5665838718414307, new HaarRectangle(3, 0, 15, 6, -1), new HaarRectangle(3, 2, 15, 2, 3));
            stageNodes.AddHaarFeature(-0.00018744950648397207, 0.4004144072532654, 0.5586857199668884, new HaarRectangle(4, 3, 12, 2, -1), new HaarRectangle(4, 4, 12, 1, 2));
            stageNodes.AddHaarFeature(-0.00521786417812109, 0.6009116172790527, 0.4812706112861633, new HaarRectangle(16, 0, 4, 5, -1), new HaarRectangle(16, 0, 2, 5, 2));
            stageNodes.AddHaarFeature(-0.0011111519997939467, 0.3514933884143829, 0.5287089943885803, new HaarRectangle(7, 0, 3, 4, -1), new HaarRectangle(8, 0, 1, 4, 3));
            stageNodes.AddHaarFeature(0.004403640050441027, 0.4642275869846344, 0.5924085974693298, new HaarRectangle(16, 0, 4, 5, -1), new HaarRectangle(16, 0, 2, 5, 2));
            stageNodes.AddHaarFeature(0.1229949966073036, 0.5025529265403748, 0.0691524818539619, new HaarRectangle(1, 7, 6, 13, -1), new HaarRectangle(3, 7, 2, 13, 3));
            stageNodes.AddHaarFeature(-0.0123135102912784, 0.5884591937065125, 0.4934012889862061, new HaarRectangle(16, 0, 4, 5, -1), new HaarRectangle(16, 0, 2, 5, 2));
            stageNodes.AddHaarFeature(0.004147103987634182, 0.4372239112854004, 0.589347779750824, new HaarRectangle(0, 0, 4, 5, -1), new HaarRectangle(2, 0, 2, 5, 2));
            stageNodes.AddHaarFeature(-0.003550264984369278, 0.4327551126480103, 0.5396270155906677, new HaarRectangle(14, 12, 3, 6, -1), new HaarRectangle(14, 14, 3, 2, 3));
            stageNodes.AddHaarFeature(-0.0192242693156004, 0.1913134008646011, 0.5068330764770508, new HaarRectangle(3, 12, 3, 6, -1), new HaarRectangle(3, 14, 3, 2, 3));
            stageNodes.AddHaarFeature(0.0014395059552043676, 0.5308178067207336, 0.424353301525116, new HaarRectangle(16, 1, 4, 3, -1), new HaarRectangle(16, 2, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.00677519990131259, 0.6365395784378052, 0.4540086090564728, new HaarRectangle(8, 7, 2, 10, -1), new HaarRectangle(8, 7, 1, 5, 2), new HaarRectangle(9, 12, 1, 5, 2));
            stageNodes.AddHaarFeature(0.007011963054537773, 0.5189834237098694, 0.302619993686676, new HaarRectangle(11, 11, 4, 4, -1), new HaarRectangle(11, 13, 4, 2, 2));
            stageNodes.AddHaarFeature(0.005401465110480785, 0.5105062127113342, 0.2557682991027832, new HaarRectangle(0, 1, 4, 3, -1), new HaarRectangle(0, 2, 4, 1, 3));
            stageNodes.AddHaarFeature(0.0009027498890645802, 0.4696914851665497, 0.5861827731132507, new HaarRectangle(13, 4, 1, 3, -1), new HaarRectangle(13, 5, 1, 1, 3));
            stageNodes.AddHaarFeature(0.0114744501188397, 0.5053645968437195, 0.152717798948288, new HaarRectangle(7, 15, 3, 5, -1), new HaarRectangle(8, 15, 1, 5, 3));
            stageNodes.AddHaarFeature(-0.006702343001961708, 0.6508980989456177, 0.4890604019165039, new HaarRectangle(9, 7, 3, 5, -1), new HaarRectangle(10, 7, 1, 5, 3));
            stageNodes.AddHaarFeature(-0.0020462959073483944, 0.6241816878318787, 0.4514600038528442, new HaarRectangle(8, 7, 3, 5, -1), new HaarRectangle(9, 7, 1, 5, 3));
            stageNodes.AddHaarFeature(-0.009995156899094582, 0.3432781100273132, 0.5400953888893127, new HaarRectangle(10, 6, 4, 14, -1), new HaarRectangle(10, 6, 2, 14, 2));
            stageNodes.AddHaarFeature(-0.0357007086277008, 0.1878059059381485, 0.5074077844619751, new HaarRectangle(0, 5, 5, 6, -1), new HaarRectangle(0, 7, 5, 2, 3));
            stageNodes.AddHaarFeature(0.0004558456130325794, 0.3805277049541473, 0.5402569770812988, new HaarRectangle(9, 5, 6, 4, -1), new HaarRectangle(9, 5, 3, 4, 2));
            stageNodes.AddHaarFeature(-0.0542606003582478, 0.6843714714050293, 0.4595097005367279, new HaarRectangle(0, 0, 18, 10, -1), new HaarRectangle(6, 0, 6, 10, 3));
            stageNodes.AddHaarFeature(0.0060600461438298225, 0.5502905249595642, 0.450052797794342, new HaarRectangle(10, 6, 4, 14, -1), new HaarRectangle(10, 6, 2, 14, 2));
            stageNodes.AddHaarFeature(-0.006479183211922646, 0.3368858098983765, 0.5310757160186768, new HaarRectangle(6, 6, 4, 14, -1), new HaarRectangle(8, 6, 2, 14, 2));
            stageNodes.AddHaarFeature(-0.0014939469983801246, 0.6487640142440796, 0.4756175875663757, new HaarRectangle(13, 4, 1, 3, -1), new HaarRectangle(13, 5, 1, 1, 3));
            stageNodes.AddHaarFeature(0.000014610530342906712, 0.403457909822464, 0.5451064109802246, new HaarRectangle(5, 1, 2, 3, -1), new HaarRectangle(6, 1, 1, 3, 2));
            stageNodes.AddHaarFeature(-0.007232193835079671, 0.6386873722076416, 0.4824739992618561, new HaarRectangle(18, 1, 2, 18, -1), new HaarRectangle(19, 1, 1, 9, 2), new HaarRectangle(18, 10, 1, 9, 2));
            stageNodes.AddHaarFeature(-0.004064581822603941, 0.2986421883106232, 0.5157335996627808, new HaarRectangle(2, 1, 4, 3, -1), new HaarRectangle(2, 2, 4, 1, 3));
            stageNodes.AddHaarFeature(0.0304630808532238, 0.5022199749946594, 0.7159956097602844, new HaarRectangle(18, 1, 2, 18, -1), new HaarRectangle(19, 1, 1, 9, 2), new HaarRectangle(18, 10, 1, 9, 2));
            stageNodes.AddHaarFeature(-0.008054491132497787, 0.6492452025413513, 0.4619275033473969, new HaarRectangle(1, 14, 4, 6, -1), new HaarRectangle(1, 14, 2, 3, 2), new HaarRectangle(3, 17, 2, 3, 2));
            stageNodes.AddHaarFeature(0.0395051389932632, 0.5150570869445801, 0.2450613975524902, new HaarRectangle(10, 11, 7, 6, -1), new HaarRectangle(10, 13, 7, 2, 3));
            stageNodes.AddHaarFeature(0.008453020825982094, 0.4573669135570526, 0.6394037008285522, new HaarRectangle(0, 10, 6, 10, -1), new HaarRectangle(0, 10, 3, 5, 2), new HaarRectangle(3, 15, 3, 5, 2));
            stageNodes.AddHaarFeature(-0.0011688120430335402, 0.3865512013435364, 0.548366129398346, new HaarRectangle(11, 0, 3, 4, -1), new HaarRectangle(12, 0, 1, 4, 3));
            stageNodes.AddHaarFeature(0.002807067008689046, 0.5128579139709473, 0.2701480090618134, new HaarRectangle(5, 10, 5, 6, -1), new HaarRectangle(5, 13, 5, 3, 2));
            stageNodes.AddHaarFeature(0.000473652093205601, 0.4051581919193268, 0.5387461185455322, new HaarRectangle(14, 6, 1, 8, -1), new HaarRectangle(14, 10, 1, 4, 2));
            stageNodes.AddHaarFeature(0.0117410803213716, 0.5295950174331665, 0.3719413876533508, new HaarRectangle(1, 7, 18, 6, -1), new HaarRectangle(1, 7, 9, 3, 2), new HaarRectangle(10, 10, 9, 3, 2));
            stageNodes.AddHaarFeature(0.003183323889970779, 0.4789406955242157, 0.6895126104354858, new HaarRectangle(9, 7, 2, 2, -1), new HaarRectangle(9, 7, 1, 2, 2));
            stageNodes.AddHaarFeature(0.0007024150108918548, 0.5384489297866821, 0.3918080925941467, new HaarRectangle(5, 9, 4, 5, -1), new HaarRectangle(7, 9, 2, 5, 2));
            stageNodeCounts.Add(stageNodes.Count - index); index = stageNodes.Count;

            stageThresholds.Add(50.16973114013672);
            stageNodes.AddHaarFeature(0.0170599296689034, 0.3948527872562408, 0.7142534852027893, new HaarRectangle(7, 6, 6, 3, -1), new HaarRectangle(9, 6, 2, 3, 3));
            stageNodes.AddHaarFeature(0.0218408405780792, 0.3370316028594971, 0.6090016961097717, new HaarRectangle(1, 0, 18, 4, -1), new HaarRectangle(7, 0, 6, 4, 3));
            stageNodes.AddHaarFeature(0.00024520049919374287, 0.3500576019287109, 0.5987902283668518, new HaarRectangle(7, 15, 2, 4, -1), new HaarRectangle(7, 17, 2, 2, 2));
            stageNodes.AddHaarFeature(0.008327260613441467, 0.3267528116703033, 0.5697240829467773, new HaarRectangle(1, 0, 19, 9, -1), new HaarRectangle(1, 3, 19, 3, 3));
            stageNodes.AddHaarFeature(0.0005714829894714057, 0.3044599890708923, 0.5531656742095947, new HaarRectangle(3, 7, 3, 6, -1), new HaarRectangle(3, 9, 3, 2, 3));
            stageNodes.AddHaarFeature(0.0006737398798577487, 0.3650012016296387, 0.567263126373291, new HaarRectangle(13, 7, 4, 4, -1), new HaarRectangle(15, 7, 2, 2, 2), new HaarRectangle(13, 9, 2, 2, 2));
            stageNodes.AddHaarFeature(0.00003468159047770314, 0.3313541114330292, 0.5388727188110352, new HaarRectangle(3, 7, 4, 4, -1), new HaarRectangle(3, 7, 2, 2, 2), new HaarRectangle(5, 9, 2, 2, 2));
            stageNodes.AddHaarFeature(-0.005856339819729328, 0.2697942852973938, 0.5498778820037842, new HaarRectangle(9, 6, 10, 8, -1), new HaarRectangle(9, 10, 10, 4, 2));
            stageNodes.AddHaarFeature(0.00851022731512785, 0.5269358158111572, 0.2762879133224487, new HaarRectangle(3, 8, 14, 12, -1), new HaarRectangle(3, 14, 14, 6, 2));
            stageNodes.AddHaarFeature(-0.0698172077536583, 0.2909603118896484, 0.5259246826171875, new HaarRectangle(6, 5, 10, 12, -1), new HaarRectangle(11, 5, 5, 6, 2), new HaarRectangle(6, 11, 5, 6, 2));
            stageNodes.AddHaarFeature(-0.0008611367084085941, 0.5892577171325684, 0.4073697924613953, new HaarRectangle(9, 11, 2, 3, -1), new HaarRectangle(9, 12, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0009714924963191152, 0.3523564040660858, 0.5415862202644348, new HaarRectangle(9, 5, 6, 5, -1), new HaarRectangle(9, 5, 3, 5, 2));
            stageNodes.AddHaarFeature(-0.00001472749045206001, 0.5423017740249634, 0.3503156006336212, new HaarRectangle(9, 4, 2, 4, -1), new HaarRectangle(9, 6, 2, 2, 2));
            stageNodes.AddHaarFeature(0.0484202913939953, 0.51939457654953, 0.3411195874214172, new HaarRectangle(9, 5, 6, 5, -1), new HaarRectangle(9, 5, 3, 5, 2));
            stageNodes.AddHaarFeature(0.0013257140526548028, 0.315776914358139, 0.5335376262664795, new HaarRectangle(5, 5, 6, 5, -1), new HaarRectangle(8, 5, 3, 5, 2));
            stageNodes.AddHaarFeature(0.00001492214960308047, 0.4451299905776978, 0.5536553859710693, new HaarRectangle(11, 2, 6, 1, -1), new HaarRectangle(13, 2, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.002717339899390936, 0.3031741976737976, 0.5248088836669922, new HaarRectangle(3, 2, 6, 1, -1), new HaarRectangle(5, 2, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0029219500720500946, 0.4781453013420105, 0.6606041789054871, new HaarRectangle(13, 5, 2, 3, -1), new HaarRectangle(13, 6, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0019804988987743855, 0.3186308145523071, 0.5287625193595886, new HaarRectangle(0, 10, 1, 4, -1), new HaarRectangle(0, 12, 1, 2, 2));
            stageNodes.AddHaarFeature(-0.004001210909336805, 0.6413596868515015, 0.4749928116798401, new HaarRectangle(13, 5, 2, 3, -1), new HaarRectangle(13, 6, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.004349199123680592, 0.1507498025894165, 0.5098996758460999, new HaarRectangle(8, 18, 3, 2, -1), new HaarRectangle(9, 18, 1, 2, 3));
            stageNodes.AddHaarFeature(0.0013490889687091112, 0.4316158890724182, 0.5881167054176331, new HaarRectangle(6, 15, 9, 2, -1), new HaarRectangle(6, 16, 9, 1, 2));
            stageNodes.AddHaarFeature(0.0185970701277256, 0.4735553860664368, 0.9089794158935548, new HaarRectangle(8, 14, 4, 3, -1), new HaarRectangle(8, 15, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.001856237999163568, 0.3553189039230347, 0.5577837228775024, new HaarRectangle(18, 4, 2, 4, -1), new HaarRectangle(18, 6, 2, 2, 2));
            stageNodes.AddHaarFeature(0.002294043079018593, 0.4500094950199127, 0.6580877900123596, new HaarRectangle(5, 5, 2, 3, -1), new HaarRectangle(5, 6, 2, 1, 3));
            stageNodes.AddHaarFeature(0.00029982850537635386, 0.5629242062568665, 0.3975878953933716, new HaarRectangle(15, 16, 3, 2, -1), new HaarRectangle(15, 17, 3, 1, 2));
            stageNodes.AddHaarFeature(0.0035455459728837013, 0.5381547212600708, 0.3605485856533051, new HaarRectangle(0, 0, 3, 9, -1), new HaarRectangle(0, 3, 3, 3, 3));
            stageNodes.AddHaarFeature(0.009610472247004509, 0.5255997180938721, 0.1796745955944061, new HaarRectangle(9, 7, 3, 3, -1), new HaarRectangle(9, 8, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.0062783220782876015, 0.227285698056221, 0.5114030241966248, new HaarRectangle(8, 7, 3, 3, -1), new HaarRectangle(8, 8, 3, 1, 3));
            stageNodes.AddHaarFeature(0.0034598479978740215, 0.4626308083534241, 0.6608219146728516, new HaarRectangle(9, 5, 2, 6, -1), new HaarRectangle(9, 5, 1, 6, 2));
            stageNodes.AddHaarFeature(-0.0013112019514665008, 0.6317539811134338, 0.4436857998371124, new HaarRectangle(8, 6, 3, 4, -1), new HaarRectangle(9, 6, 1, 4, 3));
            stageNodes.AddHaarFeature(0.002687617903575301, 0.5421109795570374, 0.4054022133350372, new HaarRectangle(7, 6, 8, 12, -1), new HaarRectangle(11, 6, 4, 6, 2), new HaarRectangle(7, 12, 4, 6, 2));
            stageNodes.AddHaarFeature(0.003911816980689764, 0.5358477830886841, 0.3273454904556274, new HaarRectangle(5, 6, 8, 12, -1), new HaarRectangle(5, 6, 4, 6, 2), new HaarRectangle(9, 12, 4, 6, 2));
            stageNodes.AddHaarFeature(-0.014206450432539, 0.7793576717376709, 0.4975781142711639, new HaarRectangle(12, 4, 3, 3, -1), new HaarRectangle(12, 5, 3, 1, 3));
            stageNodes.AddHaarFeature(0.0007170552853494883, 0.5297319889068604, 0.3560903966426849, new HaarRectangle(2, 16, 3, 2, -1), new HaarRectangle(2, 17, 3, 1, 2));
            stageNodes.AddHaarFeature(0.001663501956500113, 0.467809408903122, 0.5816481709480286, new HaarRectangle(12, 4, 3, 3, -1), new HaarRectangle(12, 5, 3, 1, 3));
            stageNodes.AddHaarFeature(0.0033686188980937004, 0.5276734232902527, 0.3446420133113861, new HaarRectangle(2, 12, 6, 6, -1), new HaarRectangle(2, 14, 6, 2, 3));
            stageNodes.AddHaarFeature(0.0127995302900672, 0.4834679961204529, 0.7472159266471863, new HaarRectangle(7, 13, 6, 3, -1), new HaarRectangle(7, 14, 6, 1, 3));
            stageNodes.AddHaarFeature(0.0033901201095432043, 0.4511859118938446, 0.6401721239089966, new HaarRectangle(6, 14, 6, 3, -1), new HaarRectangle(6, 15, 6, 1, 3));
            stageNodes.AddHaarFeature(0.004707077983766794, 0.533565878868103, 0.355522096157074, new HaarRectangle(14, 15, 5, 3, -1), new HaarRectangle(14, 16, 5, 1, 3));
            stageNodes.AddHaarFeature(0.0014819339849054811, 0.4250707030296326, 0.5772724151611328, new HaarRectangle(5, 4, 3, 3, -1), new HaarRectangle(5, 5, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.0069995759986341, 0.3003320097923279, 0.5292900204658508, new HaarRectangle(14, 15, 5, 3, -1), new HaarRectangle(14, 16, 5, 1, 3));
            stageNodes.AddHaarFeature(0.0159390103071928, 0.5067319273948669, 0.1675581932067871, new HaarRectangle(5, 3, 6, 2, -1), new HaarRectangle(7, 3, 2, 2, 3));
            stageNodes.AddHaarFeature(0.00763773499056697, 0.4795069992542267, 0.7085601091384888, new HaarRectangle(8, 15, 4, 3, -1), new HaarRectangle(8, 16, 4, 1, 3));
            stageNodes.AddHaarFeature(0.006733404006808996, 0.5133113265037537, 0.2162470072507858, new HaarRectangle(1, 15, 5, 3, -1), new HaarRectangle(1, 16, 5, 1, 3));
            stageNodes.AddHaarFeature(-0.012858809903264, 0.1938841938972473, 0.525137186050415, new HaarRectangle(8, 13, 4, 6, -1), new HaarRectangle(10, 13, 2, 3, 2), new HaarRectangle(8, 16, 2, 3, 2));
            stageNodes.AddHaarFeature(-0.0006227080011740327, 0.5686538219451904, 0.419786810874939, new HaarRectangle(7, 8, 3, 3, -1), new HaarRectangle(8, 8, 1, 3, 3));
            stageNodes.AddHaarFeature(-0.0005265168147161603, 0.4224168956279755, 0.5429695844650269, new HaarRectangle(12, 0, 5, 4, -1), new HaarRectangle(12, 2, 5, 2, 2));
            stageNodes.AddHaarFeature(0.0110750999301672, 0.5113775134086609, 0.2514517903327942, new HaarRectangle(0, 2, 20, 2, -1), new HaarRectangle(0, 2, 10, 1, 2), new HaarRectangle(10, 3, 10, 1, 2));
            stageNodes.AddHaarFeature(-0.0367282517254353, 0.7194662094116211, 0.4849618971347809, new HaarRectangle(1, 0, 18, 4, -1), new HaarRectangle(7, 0, 6, 4, 3));
            stageNodes.AddHaarFeature(-0.00028207109426148236, 0.3840261995792389, 0.539444625377655, new HaarRectangle(4, 3, 6, 1, -1), new HaarRectangle(6, 3, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0027489690110087395, 0.593708872795105, 0.4569182097911835, new HaarRectangle(4, 18, 13, 2, -1), new HaarRectangle(4, 19, 13, 1, 2));
            stageNodes.AddHaarFeature(0.0100475195795298, 0.5138576030731201, 0.2802298069000244, new HaarRectangle(2, 10, 3, 6, -1), new HaarRectangle(2, 12, 3, 2, 3));
            stageNodes.AddHaarFeature(-0.008149784058332443, 0.6090037226676941, 0.4636121094226837, new HaarRectangle(14, 12, 6, 8, -1), new HaarRectangle(17, 12, 3, 4, 2), new HaarRectangle(14, 16, 3, 4, 2));
            stageNodes.AddHaarFeature(-0.006883388850837946, 0.3458611071109772, 0.5254660248756409, new HaarRectangle(4, 13, 10, 6, -1), new HaarRectangle(4, 13, 5, 3, 2), new HaarRectangle(9, 16, 5, 3, 2));
            stageNodes.AddHaarFeature(-0.0000140393603942357, 0.5693104267120361, 0.4082083106040955, new HaarRectangle(14, 12, 1, 2, -1), new HaarRectangle(14, 13, 1, 1, 2));
            stageNodes.AddHaarFeature(0.001549841952510178, 0.4350537061691284, 0.5806517004966736, new HaarRectangle(8, 13, 4, 3, -1), new HaarRectangle(8, 14, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.006784149911254644, 0.1468873023986816, 0.5182775259017944, new HaarRectangle(14, 12, 2, 2, -1), new HaarRectangle(14, 13, 2, 1, 2));
            stageNodes.AddHaarFeature(0.00021705629478674385, 0.5293524265289307, 0.345617413520813, new HaarRectangle(4, 12, 2, 2, -1), new HaarRectangle(4, 13, 2, 1, 2));
            stageNodes.AddHaarFeature(0.00031198898795992136, 0.4652450978755951, 0.5942413806915283, new HaarRectangle(8, 12, 9, 2, -1), new HaarRectangle(8, 13, 9, 1, 2));
            stageNodes.AddHaarFeature(0.005450753029435873, 0.4653508961200714, 0.7024846076965332, new HaarRectangle(9, 14, 2, 3, -1), new HaarRectangle(9, 15, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.00025818689027801156, 0.5497295260429382, 0.3768967092037201, new HaarRectangle(11, 10, 3, 6, -1), new HaarRectangle(11, 13, 3, 3, 2));
            stageNodes.AddHaarFeature(-0.0174425393342972, 0.3919087946414948, 0.5457497835159302, new HaarRectangle(5, 6, 9, 12, -1), new HaarRectangle(5, 12, 9, 6, 2));
            stageNodes.AddHaarFeature(-0.045343529433012, 0.1631357073783875, 0.5154908895492554, new HaarRectangle(11, 10, 3, 6, -1), new HaarRectangle(11, 13, 3, 3, 2));
            stageNodes.AddHaarFeature(0.0019190689781680703, 0.514589786529541, 0.2791895866394043, new HaarRectangle(6, 10, 3, 6, -1), new HaarRectangle(6, 13, 3, 3, 2));
            stageNodes.AddHaarFeature(-0.006017786916345358, 0.6517636179924011, 0.4756332933902741, new HaarRectangle(5, 4, 11, 3, -1), new HaarRectangle(5, 5, 11, 1, 3));
            stageNodes.AddHaarFeature(-0.004072073847055435, 0.5514652729034424, 0.4092685878276825, new HaarRectangle(7, 1, 5, 10, -1), new HaarRectangle(7, 6, 5, 5, 2));
            stageNodes.AddHaarFeature(0.0003985505900345742, 0.316524088382721, 0.5285550951957703, new HaarRectangle(2, 8, 18, 2, -1), new HaarRectangle(2, 9, 18, 1, 2));
            stageNodes.AddHaarFeature(-0.0065418570302426815, 0.6853377819061279, 0.4652808904647827, new HaarRectangle(7, 17, 5, 3, -1), new HaarRectangle(7, 18, 5, 1, 3));
            stageNodes.AddHaarFeature(0.003484508953988552, 0.5484588146209717, 0.4502759873867035, new HaarRectangle(5, 9, 12, 1, -1), new HaarRectangle(9, 9, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.0136967804282904, 0.6395779848098755, 0.4572555124759674, new HaarRectangle(0, 14, 6, 6, -1), new HaarRectangle(0, 14, 3, 3, 2), new HaarRectangle(3, 17, 3, 3, 2));
            stageNodes.AddHaarFeature(-0.017347140237689, 0.2751072943210602, 0.5181614756584167, new HaarRectangle(5, 9, 12, 1, -1), new HaarRectangle(9, 9, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.004088542889803648, 0.3325636088848114, 0.5194984078407288, new HaarRectangle(3, 9, 12, 1, -1), new HaarRectangle(7, 9, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.009468790143728256, 0.5942280888557434, 0.485181987285614, new HaarRectangle(14, 10, 6, 7, -1), new HaarRectangle(14, 10, 3, 7, 2));
            stageNodes.AddHaarFeature(0.0017084840219467878, 0.4167110919952393, 0.5519806146621704, new HaarRectangle(1, 0, 16, 2, -1), new HaarRectangle(1, 1, 16, 1, 2));
            stageNodes.AddHaarFeature(0.009480909444391727, 0.5433894991874695, 0.4208514988422394, new HaarRectangle(10, 9, 10, 9, -1), new HaarRectangle(10, 12, 10, 3, 3));
            stageNodes.AddHaarFeature(-0.004738965071737766, 0.6407189965248108, 0.4560655057430267, new HaarRectangle(0, 1, 10, 2, -1), new HaarRectangle(5, 1, 5, 2, 2));
            stageNodes.AddHaarFeature(0.006576105020940304, 0.5214555263519287, 0.2258227020502091, new HaarRectangle(17, 3, 2, 3, -1), new HaarRectangle(17, 4, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0021690549328923225, 0.3151527941226959, 0.5156704783439636, new HaarRectangle(1, 3, 2, 3, -1), new HaarRectangle(1, 4, 2, 1, 3));
            stageNodes.AddHaarFeature(0.014660170301795, 0.4870837032794952, 0.668994128704071, new HaarRectangle(9, 7, 3, 6, -1), new HaarRectangle(10, 7, 1, 6, 3));
            stageNodes.AddHaarFeature(0.00017231999663636088, 0.3569748997688294, 0.5251078009605408, new HaarRectangle(6, 5, 4, 3, -1), new HaarRectangle(8, 5, 2, 3, 2));
            stageNodes.AddHaarFeature(-0.0218037609010935, 0.8825920820236206, 0.496632993221283, new HaarRectangle(7, 5, 6, 6, -1), new HaarRectangle(9, 5, 2, 6, 3));
            stageNodes.AddHaarFeature(-0.0947361066937447, 0.1446162015199661, 0.5061113834381104, new HaarRectangle(3, 4, 12, 12, -1), new HaarRectangle(3, 4, 6, 6, 2), new HaarRectangle(9, 10, 6, 6, 2));
            stageNodes.AddHaarFeature(0.0055825551971793175, 0.5396478772163391, 0.4238066077232361, new HaarRectangle(9, 2, 6, 15, -1), new HaarRectangle(11, 2, 2, 15, 3));
            stageNodes.AddHaarFeature(0.0019517090404406192, 0.4170410931110382, 0.5497786998748779, new HaarRectangle(2, 2, 6, 17, -1), new HaarRectangle(4, 2, 2, 17, 3));
            stageNodes.AddHaarFeature(0.0121499001979828, 0.4698367118835449, 0.5664274096488953, new HaarRectangle(14, 10, 6, 7, -1), new HaarRectangle(14, 10, 3, 7, 2));
            stageNodes.AddHaarFeature(-0.007516962010413408, 0.6267772912979126, 0.4463135898113251, new HaarRectangle(0, 10, 6, 7, -1), new HaarRectangle(3, 10, 3, 7, 2));
            stageNodes.AddHaarFeature(-0.0716679096221924, 0.3097011148929596, 0.5221003293991089, new HaarRectangle(9, 2, 6, 15, -1), new HaarRectangle(11, 2, 2, 15, 3));
            stageNodes.AddHaarFeature(-0.0882924199104309, 0.0811238884925842, 0.5006365180015564, new HaarRectangle(5, 2, 6, 15, -1), new HaarRectangle(7, 2, 2, 15, 3));
            stageNodes.AddHaarFeature(0.0310630798339844, 0.5155503749847412, 0.1282255947589874, new HaarRectangle(17, 9, 3, 6, -1), new HaarRectangle(17, 11, 3, 2, 3));
            stageNodes.AddHaarFeature(0.0466218404471874, 0.4699777960777283, 0.736396074295044, new HaarRectangle(6, 7, 6, 6, -1), new HaarRectangle(8, 7, 2, 6, 3));
            stageNodes.AddHaarFeature(-0.0121894897893071, 0.3920530080795288, 0.5518996715545654, new HaarRectangle(1, 10, 18, 6, -1), new HaarRectangle(10, 10, 9, 3, 2), new HaarRectangle(1, 13, 9, 3, 2));
            stageNodes.AddHaarFeature(0.0130161102861166, 0.5260658264160156, 0.3685136139392853, new HaarRectangle(0, 9, 10, 9, -1), new HaarRectangle(0, 12, 10, 3, 3));
            stageNodes.AddHaarFeature(-0.003495289944112301, 0.6339294910430908, 0.4716280996799469, new HaarRectangle(8, 15, 4, 3, -1), new HaarRectangle(8, 16, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.00004401503974804655, 0.5333027243614197, 0.3776184916496277, new HaarRectangle(5, 12, 3, 4, -1), new HaarRectangle(5, 14, 3, 2, 2));
            stageNodes.AddHaarFeature(-0.1096649020910263, 0.1765342056751251, 0.5198346972465515, new HaarRectangle(3, 3, 16, 12, -1), new HaarRectangle(3, 9, 16, 6, 2));
            stageNodes.AddHaarFeature(-0.0009027955820783973, 0.5324159860610962, 0.3838908076286316, new HaarRectangle(1, 1, 12, 12, -1), new HaarRectangle(1, 1, 6, 6, 2), new HaarRectangle(7, 7, 6, 6, 2));
            stageNodes.AddHaarFeature(0.0007112664170563221, 0.4647929966449738, 0.5755224227905273, new HaarRectangle(10, 4, 2, 4, -1), new HaarRectangle(11, 4, 1, 2, 2), new HaarRectangle(10, 6, 1, 2, 2));
            stageNodes.AddHaarFeature(-0.003125027986243367, 0.323670893907547, 0.5166770815849304, new HaarRectangle(0, 9, 10, 2, -1), new HaarRectangle(0, 9, 5, 1, 2), new HaarRectangle(5, 10, 5, 1, 2));
            stageNodes.AddHaarFeature(0.002414467977359891, 0.4787439107894898, 0.6459717750549316, new HaarRectangle(9, 11, 3, 3, -1), new HaarRectangle(9, 12, 3, 1, 3));
            stageNodes.AddHaarFeature(0.00044391240226104856, 0.4409308135509491, 0.6010255813598633, new HaarRectangle(3, 12, 9, 2, -1), new HaarRectangle(3, 13, 9, 1, 2));
            stageNodes.AddHaarFeature(-0.0002261118934256956, 0.4038113951683044, 0.5493255853652954, new HaarRectangle(9, 9, 2, 2, -1), new HaarRectangle(9, 10, 2, 1, 2));
            stageNodeCounts.Add(stageNodes.Count - index); index = stageNodes.Count;

            stageThresholds.Add(66.6691207885742);
            stageNodes.AddHaarFeature(-0.0469012893736362, 0.660017192363739, 0.3743801116943359, new HaarRectangle(3, 4, 13, 6, -1), new HaarRectangle(3, 6, 13, 2, 3));
            stageNodes.AddHaarFeature(-0.001456834957934916, 0.578399121761322, 0.3437797129154205, new HaarRectangle(9, 7, 6, 4, -1), new HaarRectangle(12, 7, 3, 2, 2), new HaarRectangle(9, 9, 3, 2, 2));
            stageNodes.AddHaarFeature(0.005559836979955435, 0.3622266948223114, 0.5908216238021851, new HaarRectangle(1, 0, 6, 8, -1), new HaarRectangle(4, 0, 3, 8, 2));
            stageNodes.AddHaarFeature(0.0007317048730328678, 0.550041913986206, 0.2873558104038239, new HaarRectangle(9, 5, 2, 12, -1), new HaarRectangle(9, 11, 2, 6, 2));
            stageNodes.AddHaarFeature(0.001331800944171846, 0.267316997051239, 0.5431019067764282, new HaarRectangle(4, 4, 3, 10, -1), new HaarRectangle(4, 9, 3, 5, 2));
            stageNodes.AddHaarFeature(0.00024347059661522508, 0.3855027854442596, 0.574138879776001, new HaarRectangle(6, 17, 8, 3, -1), new HaarRectangle(6, 18, 8, 1, 3));
            stageNodes.AddHaarFeature(-0.0030512469820678234, 0.5503209829330444, 0.3462845087051392, new HaarRectangle(0, 5, 10, 6, -1), new HaarRectangle(0, 7, 10, 2, 3));
            stageNodes.AddHaarFeature(-0.0006865719915367663, 0.3291221857070923, 0.5429509282112122, new HaarRectangle(13, 2, 3, 2, -1), new HaarRectangle(13, 3, 3, 1, 2));
            stageNodes.AddHaarFeature(0.001466820016503334, 0.3588382005691528, 0.5351811051368713, new HaarRectangle(7, 5, 4, 5, -1), new HaarRectangle(9, 5, 2, 5, 2));
            stageNodes.AddHaarFeature(0.0003202187072020024, 0.429684191942215, 0.5700234174728394, new HaarRectangle(12, 14, 3, 6, -1), new HaarRectangle(12, 16, 3, 2, 3));
            stageNodes.AddHaarFeature(0.0007412218837998809, 0.5282164812088013, 0.3366870880126953, new HaarRectangle(1, 11, 8, 2, -1), new HaarRectangle(1, 12, 8, 1, 2));
            stageNodes.AddHaarFeature(0.0038330298848450184, 0.4559567868709564, 0.6257336139678955, new HaarRectangle(7, 13, 6, 3, -1), new HaarRectangle(7, 14, 6, 1, 3));
            stageNodes.AddHaarFeature(-0.0154564399272203, 0.2350116968154907, 0.512945294380188, new HaarRectangle(0, 5, 3, 6, -1), new HaarRectangle(0, 7, 3, 2, 3));
            stageNodes.AddHaarFeature(0.002679677912965417, 0.5329415202140808, 0.4155062139034271, new HaarRectangle(13, 2, 3, 2, -1), new HaarRectangle(13, 3, 3, 1, 2));
            stageNodes.AddHaarFeature(0.0028296569362282753, 0.4273087978363037, 0.5804538130760193, new HaarRectangle(4, 14, 4, 6, -1), new HaarRectangle(4, 14, 2, 3, 2), new HaarRectangle(6, 17, 2, 3, 2));
            stageNodes.AddHaarFeature(-0.0039444249123334885, 0.2912611961364746, 0.5202686190605164, new HaarRectangle(13, 2, 3, 2, -1), new HaarRectangle(13, 3, 3, 1, 2));
            stageNodes.AddHaarFeature(0.002717955969274044, 0.5307688117027283, 0.3585677146911621, new HaarRectangle(8, 2, 4, 12, -1), new HaarRectangle(8, 6, 4, 4, 3));
            stageNodes.AddHaarFeature(0.005907762795686722, 0.470377504825592, 0.5941585898399353, new HaarRectangle(14, 0, 6, 8, -1), new HaarRectangle(17, 0, 3, 4, 2), new HaarRectangle(14, 4, 3, 4, 2));
            stageNodes.AddHaarFeature(-0.004224034957587719, 0.2141567021608353, 0.5088796019554138, new HaarRectangle(7, 17, 3, 2, -1), new HaarRectangle(8, 17, 1, 2, 3));
            stageNodes.AddHaarFeature(0.0040725888684391975, 0.4766413867473602, 0.6841061115264893, new HaarRectangle(8, 12, 4, 2, -1), new HaarRectangle(8, 13, 4, 1, 2));
            stageNodes.AddHaarFeature(0.0101495301350951, 0.5360798835754395, 0.3748497068881989, new HaarRectangle(6, 0, 8, 12, -1), new HaarRectangle(6, 0, 4, 6, 2), new HaarRectangle(10, 6, 4, 6, 2));
            stageNodes.AddHaarFeature(-0.00018864999583456665, 0.5720130205154419, 0.3853805065155029, new HaarRectangle(14, 0, 2, 10, -1), new HaarRectangle(15, 0, 1, 5, 2), new HaarRectangle(14, 5, 1, 5, 2));
            stageNodes.AddHaarFeature(-0.0048864358104765415, 0.3693122863769531, 0.5340958833694458, new HaarRectangle(5, 3, 8, 6, -1), new HaarRectangle(5, 3, 4, 3, 2), new HaarRectangle(9, 6, 4, 3, 2));
            stageNodes.AddHaarFeature(0.0261584799736738, 0.4962374866008759, 0.6059989929199219, new HaarRectangle(14, 0, 6, 10, -1), new HaarRectangle(17, 0, 3, 5, 2), new HaarRectangle(14, 5, 3, 5, 2));
            stageNodes.AddHaarFeature(0.0004856075975112617, 0.4438945949077606, 0.6012468934059143, new HaarRectangle(9, 14, 1, 2, -1), new HaarRectangle(9, 15, 1, 1, 2));
            stageNodes.AddHaarFeature(0.0112687097862363, 0.5244250297546387, 0.1840388029813767, new HaarRectangle(15, 10, 4, 3, -1), new HaarRectangle(15, 11, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.0028114619199186563, 0.6060283780097961, 0.4409897029399872, new HaarRectangle(8, 14, 2, 3, -1), new HaarRectangle(8, 15, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.005611272994428873, 0.3891170918941498, 0.5589237213134766, new HaarRectangle(3, 13, 14, 4, -1), new HaarRectangle(10, 13, 7, 2, 2), new HaarRectangle(3, 15, 7, 2, 2));
            stageNodes.AddHaarFeature(0.008568009361624718, 0.5069345831871033, 0.2062619030475617, new HaarRectangle(1, 10, 4, 3, -1), new HaarRectangle(1, 11, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.00038172779022715986, 0.5882201790809631, 0.41926109790802, new HaarRectangle(9, 11, 6, 1, -1), new HaarRectangle(11, 11, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.00017680290329735726, 0.5533605813980103, 0.400336891412735, new HaarRectangle(5, 11, 6, 1, -1), new HaarRectangle(7, 11, 2, 1, 3));
            stageNodes.AddHaarFeature(0.006511253770440817, 0.3310146927833557, 0.5444191098213196, new HaarRectangle(3, 5, 16, 15, -1), new HaarRectangle(3, 10, 16, 5, 3));
            stageNodes.AddHaarFeature(-0.00006594868318643421, 0.5433831810951233, 0.3944905996322632, new HaarRectangle(6, 12, 4, 2, -1), new HaarRectangle(8, 12, 2, 2, 2));
            stageNodes.AddHaarFeature(0.006993905175477266, 0.5600358247756958, 0.4192714095115662, new HaarRectangle(4, 4, 12, 10, -1), new HaarRectangle(10, 4, 6, 5, 2), new HaarRectangle(4, 9, 6, 5, 2));
            stageNodes.AddHaarFeature(-0.0046744439750909805, 0.6685466766357422, 0.4604960978031158, new HaarRectangle(8, 6, 3, 4, -1), new HaarRectangle(9, 6, 1, 4, 3));
            stageNodes.AddHaarFeature(0.0115898502990603, 0.5357121229171753, 0.2926830053329468, new HaarRectangle(8, 12, 4, 8, -1), new HaarRectangle(10, 12, 2, 4, 2), new HaarRectangle(8, 16, 2, 4, 2));
            stageNodes.AddHaarFeature(0.013007840141654, 0.4679817855358124, 0.730746328830719, new HaarRectangle(8, 14, 4, 3, -1), new HaarRectangle(8, 15, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.0011008579749614, 0.3937501013278961, 0.5415065288543701, new HaarRectangle(12, 2, 3, 2, -1), new HaarRectangle(13, 2, 1, 2, 3));
            stageNodes.AddHaarFeature(0.0006047264905646443, 0.4242376089096069, 0.5604041218757629, new HaarRectangle(8, 15, 3, 2, -1), new HaarRectangle(8, 16, 3, 1, 2));
            stageNodes.AddHaarFeature(-0.0144948400557041, 0.3631210029125214, 0.5293182730674744, new HaarRectangle(6, 0, 9, 14, -1), new HaarRectangle(9, 0, 3, 14, 3));
            stageNodes.AddHaarFeature(-0.005305694881826639, 0.686045229434967, 0.4621821045875549, new HaarRectangle(9, 6, 2, 3, -1), new HaarRectangle(10, 6, 1, 3, 2));
            stageNodes.AddHaarFeature(-0.00081829127157107, 0.3944096863269806, 0.542043924331665, new HaarRectangle(10, 8, 2, 3, -1), new HaarRectangle(10, 9, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0190775208175182, 0.1962621957063675, 0.5037891864776611, new HaarRectangle(0, 9, 4, 6, -1), new HaarRectangle(0, 11, 4, 2, 3));
            stageNodes.AddHaarFeature(0.00035549470339901745, 0.4086259007453919, 0.5613973140716553, new HaarRectangle(6, 0, 8, 2, -1), new HaarRectangle(6, 1, 8, 1, 2));
            stageNodes.AddHaarFeature(0.0019679730758070946, 0.448912113904953, 0.5926123261451721, new HaarRectangle(6, 14, 7, 3, -1), new HaarRectangle(6, 15, 7, 1, 3));
            stageNodes.AddHaarFeature(0.006918914150446653, 0.5335925817489624, 0.3728385865688324, new HaarRectangle(8, 10, 8, 9, -1), new HaarRectangle(8, 13, 8, 3, 3));
            stageNodes.AddHaarFeature(0.002987277926877141, 0.5111321210861206, 0.2975643873214722, new HaarRectangle(5, 2, 3, 2, -1), new HaarRectangle(6, 2, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.006226461846381426, 0.5541489720344543, 0.4824537932872772, new HaarRectangle(14, 1, 6, 8, -1), new HaarRectangle(17, 1, 3, 4, 2), new HaarRectangle(14, 5, 3, 4, 2));
            stageNodes.AddHaarFeature(0.013353300280869, 0.4586423933506012, 0.6414797902107239, new HaarRectangle(0, 1, 6, 8, -1), new HaarRectangle(0, 1, 3, 4, 2), new HaarRectangle(3, 5, 3, 4, 2));
            stageNodes.AddHaarFeature(0.0335052385926247, 0.5392425060272217, 0.3429994881153107, new HaarRectangle(1, 2, 18, 6, -1), new HaarRectangle(10, 2, 9, 3, 2), new HaarRectangle(1, 5, 9, 3, 2));
            stageNodes.AddHaarFeature(-0.0025294460356235504, 0.1703713983297348, 0.5013315081596375, new HaarRectangle(9, 3, 2, 1, -1), new HaarRectangle(10, 3, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.001280162949115038, 0.5305461883544922, 0.4697405099868774, new HaarRectangle(13, 2, 4, 6, -1), new HaarRectangle(15, 2, 2, 3, 2), new HaarRectangle(13, 5, 2, 3, 2));
            stageNodes.AddHaarFeature(0.007068738806992769, 0.4615545868873596, 0.643650472164154, new HaarRectangle(5, 4, 3, 3, -1), new HaarRectangle(5, 5, 3, 1, 3));
            stageNodes.AddHaarFeature(0.0009688049904070796, 0.4833599030971527, 0.6043894290924072, new HaarRectangle(13, 5, 1, 3, -1), new HaarRectangle(13, 6, 1, 1, 3));
            stageNodes.AddHaarFeature(0.003964765928685665, 0.5187637209892273, 0.323181688785553, new HaarRectangle(2, 16, 5, 3, -1), new HaarRectangle(2, 17, 5, 1, 3));
            stageNodes.AddHaarFeature(-0.022057730704546, 0.4079256951808929, 0.520098090171814, new HaarRectangle(13, 2, 4, 6, -1), new HaarRectangle(15, 2, 2, 3, 2), new HaarRectangle(13, 5, 2, 3, 2));
            stageNodes.AddHaarFeature(-0.0006690631271339953, 0.533160924911499, 0.3815600872039795, new HaarRectangle(3, 2, 4, 6, -1), new HaarRectangle(3, 2, 2, 3, 2), new HaarRectangle(5, 5, 2, 3, 2));
            stageNodes.AddHaarFeature(-0.0006700932863168418, 0.5655422210693359, 0.4688901901245117, new HaarRectangle(13, 5, 1, 2, -1), new HaarRectangle(13, 6, 1, 1, 2));
            stageNodes.AddHaarFeature(0.000742845528293401, 0.4534381031990051, 0.6287400126457214, new HaarRectangle(5, 5, 2, 2, -1), new HaarRectangle(5, 6, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0022227810695767403, 0.5350633263587952, 0.3303655982017517, new HaarRectangle(13, 9, 2, 2, -1), new HaarRectangle(13, 9, 1, 2, 2));
            stageNodes.AddHaarFeature(-0.005413052160292864, 0.1113687008619309, 0.500543475151062, new HaarRectangle(5, 9, 2, 2, -1), new HaarRectangle(6, 9, 1, 2, 2));
            stageNodes.AddHaarFeature(-0.000014520040167553816, 0.5628737807273865, 0.4325133860111237, new HaarRectangle(13, 17, 3, 2, -1), new HaarRectangle(13, 18, 3, 1, 2));
            stageNodes.AddHaarFeature(0.00023369169502984732, 0.4165835082530975, 0.5447791218757629, new HaarRectangle(6, 16, 4, 4, -1), new HaarRectangle(6, 16, 2, 2, 2), new HaarRectangle(8, 18, 2, 2, 2));
            stageNodes.AddHaarFeature(0.004289454780519009, 0.4860391020774841, 0.6778649091720581, new HaarRectangle(9, 16, 2, 3, -1), new HaarRectangle(9, 17, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0059103150852024555, 0.52623051404953, 0.3612113893032074, new HaarRectangle(0, 13, 9, 6, -1), new HaarRectangle(0, 15, 9, 2, 3));
            stageNodes.AddHaarFeature(0.0129005396738648, 0.5319377183914185, 0.32502880692482, new HaarRectangle(9, 14, 2, 6, -1), new HaarRectangle(9, 17, 2, 3, 2));
            stageNodes.AddHaarFeature(0.004698297940194607, 0.461824506521225, 0.6665925979614258, new HaarRectangle(9, 15, 2, 3, -1), new HaarRectangle(9, 16, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0104398597031832, 0.550567090511322, 0.3883604109287262, new HaarRectangle(1, 10, 18, 6, -1), new HaarRectangle(1, 12, 18, 2, 3));
            stageNodes.AddHaarFeature(0.0030443191062659025, 0.4697853028774262, 0.7301844954490662, new HaarRectangle(8, 11, 4, 2, -1), new HaarRectangle(8, 12, 4, 1, 2));
            stageNodes.AddHaarFeature(-0.0006159375188872218, 0.3830839097499847, 0.5464984178543091, new HaarRectangle(7, 9, 6, 2, -1), new HaarRectangle(7, 10, 6, 1, 2));
            stageNodes.AddHaarFeature(-0.0034247159492224455, 0.256630003452301, 0.5089530944824219, new HaarRectangle(8, 8, 2, 3, -1), new HaarRectangle(8, 9, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.009353856556117535, 0.6469966173171997, 0.49407958984375, new HaarRectangle(17, 5, 3, 4, -1), new HaarRectangle(18, 5, 1, 4, 3));
            stageNodes.AddHaarFeature(0.0523389987647533, 0.4745982885360718, 0.787877082824707, new HaarRectangle(1, 19, 18, 1, -1), new HaarRectangle(7, 19, 6, 1, 3));
            stageNodes.AddHaarFeature(0.0035765620414167643, 0.5306664705276489, 0.2748498022556305, new HaarRectangle(9, 0, 3, 2, -1), new HaarRectangle(10, 0, 1, 2, 3));
            stageNodes.AddHaarFeature(0.0007155531784519553, 0.541312575340271, 0.4041908979415894, new HaarRectangle(1, 8, 1, 6, -1), new HaarRectangle(1, 10, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.0105166798457503, 0.6158512234687805, 0.4815283119678497, new HaarRectangle(12, 17, 8, 3, -1), new HaarRectangle(12, 17, 4, 3, 2));
            stageNodes.AddHaarFeature(0.007734792772680522, 0.4695805907249451, 0.7028980851173401, new HaarRectangle(0, 5, 3, 4, -1), new HaarRectangle(1, 5, 1, 4, 3));
            stageNodes.AddHaarFeature(-0.004322677850723267, 0.2849566042423248, 0.5304684042930603, new HaarRectangle(9, 7, 2, 3, -1), new HaarRectangle(9, 8, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0025534399319440126, 0.7056984901428223, 0.4688892066478729, new HaarRectangle(7, 11, 2, 2, -1), new HaarRectangle(7, 11, 1, 1, 2), new HaarRectangle(8, 12, 1, 1, 2));
            stageNodes.AddHaarFeature(0.00010268510231981054, 0.3902932107448578, 0.5573464035987854, new HaarRectangle(11, 3, 2, 5, -1), new HaarRectangle(11, 3, 1, 5, 2));
            stageNodes.AddHaarFeature(0.000007139518857002258, 0.368423193693161, 0.526398777961731, new HaarRectangle(7, 3, 2, 5, -1), new HaarRectangle(8, 3, 1, 5, 2));
            stageNodes.AddHaarFeature(-0.0016711989883333445, 0.3849175870418549, 0.5387271046638489, new HaarRectangle(15, 13, 2, 3, -1), new HaarRectangle(15, 14, 2, 1, 3));
            stageNodes.AddHaarFeature(0.004926044959574938, 0.4729771912097931, 0.7447251081466675, new HaarRectangle(5, 6, 2, 3, -1), new HaarRectangle(5, 7, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0043908702209591866, 0.4809181094169617, 0.5591921806335449, new HaarRectangle(4, 19, 15, 1, -1), new HaarRectangle(9, 19, 5, 1, 3));
            stageNodes.AddHaarFeature(-0.0177936293184757, 0.6903678178787231, 0.4676927030086517, new HaarRectangle(1, 19, 15, 1, -1), new HaarRectangle(6, 19, 5, 1, 3));
            stageNodes.AddHaarFeature(0.002046966925263405, 0.5370690226554871, 0.3308162093162537, new HaarRectangle(15, 13, 2, 3, -1), new HaarRectangle(15, 14, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0298914890736341, 0.5139865279197693, 0.3309059143066406, new HaarRectangle(5, 0, 4, 15, -1), new HaarRectangle(7, 0, 2, 15, 2));
            stageNodes.AddHaarFeature(0.0015494900289922953, 0.466023713350296, 0.6078342795372009, new HaarRectangle(9, 6, 2, 5, -1), new HaarRectangle(9, 6, 1, 5, 2));
            stageNodes.AddHaarFeature(0.0014956969534978273, 0.4404835999011993, 0.5863919854164124, new HaarRectangle(9, 5, 2, 7, -1), new HaarRectangle(10, 5, 1, 7, 2));
            stageNodes.AddHaarFeature(0.0009588592802174388, 0.5435971021652222, 0.4208523035049439, new HaarRectangle(16, 11, 3, 3, -1), new HaarRectangle(16, 12, 3, 1, 3));
            stageNodes.AddHaarFeature(0.0004964370164088905, 0.5370578169822693, 0.4000622034072876, new HaarRectangle(1, 11, 3, 3, -1), new HaarRectangle(1, 12, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.00272808107547462, 0.5659412741661072, 0.4259642958641052, new HaarRectangle(6, 6, 8, 3, -1), new HaarRectangle(6, 7, 8, 1, 3));
            stageNodes.AddHaarFeature(0.0023026480339467525, 0.5161657929420471, 0.3350869119167328, new HaarRectangle(0, 15, 6, 2, -1), new HaarRectangle(0, 16, 6, 1, 2));
            stageNodes.AddHaarFeature(0.2515163123607636, 0.4869661927223206, 0.714730978012085, new HaarRectangle(1, 0, 18, 6, -1), new HaarRectangle(7, 0, 6, 6, 3));
            stageNodes.AddHaarFeature(-0.004632802214473486, 0.27274489402771, 0.5083789825439453, new HaarRectangle(6, 0, 3, 4, -1), new HaarRectangle(7, 0, 1, 4, 3));
            stageNodes.AddHaarFeature(-0.0404344908893108, 0.6851438879966736, 0.5021767020225525, new HaarRectangle(14, 10, 4, 10, -1), new HaarRectangle(16, 10, 2, 5, 2), new HaarRectangle(14, 15, 2, 5, 2));
            stageNodes.AddHaarFeature(0.000014972220014897177, 0.428446501493454, 0.5522555112838745, new HaarRectangle(3, 2, 3, 2, -1), new HaarRectangle(4, 2, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.00024050309730228037, 0.4226118922233582, 0.5390074849128723, new HaarRectangle(11, 2, 2, 2, -1), new HaarRectangle(11, 3, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0236578397452831, 0.4744631946086884, 0.7504366040229797, new HaarRectangle(2, 10, 4, 10, -1), new HaarRectangle(2, 10, 2, 5, 2), new HaarRectangle(4, 15, 2, 5, 2));
            stageNodes.AddHaarFeature(-0.00814491044729948, 0.424505889415741, 0.5538362860679626, new HaarRectangle(0, 13, 20, 6, -1), new HaarRectangle(10, 13, 10, 3, 2), new HaarRectangle(0, 16, 10, 3, 2));
            stageNodes.AddHaarFeature(-0.003699213033542037, 0.5952357053756714, 0.4529713094234467, new HaarRectangle(0, 5, 2, 15, -1), new HaarRectangle(1, 5, 1, 15, 2));
            stageNodes.AddHaarFeature(-0.0067718601785600185, 0.4137794077396393, 0.5473399758338928, new HaarRectangle(1, 7, 18, 4, -1), new HaarRectangle(10, 7, 9, 2, 2), new HaarRectangle(1, 9, 9, 2, 2));
            stageNodes.AddHaarFeature(0.004266953095793724, 0.4484114944934845, 0.5797994136810303, new HaarRectangle(0, 0, 2, 17, -1), new HaarRectangle(1, 0, 1, 17, 2));
            stageNodes.AddHaarFeature(0.0017791989957913756, 0.5624858736991882, 0.4432444870471954, new HaarRectangle(2, 6, 16, 6, -1), new HaarRectangle(10, 6, 8, 3, 2), new HaarRectangle(2, 9, 8, 3, 2));
            stageNodes.AddHaarFeature(0.0016774770338088274, 0.4637751877307892, 0.63642418384552, new HaarRectangle(8, 14, 1, 3, -1), new HaarRectangle(8, 15, 1, 1, 3));
            stageNodes.AddHaarFeature(0.0011732629500329494, 0.4544503092765808, 0.5914415717124939, new HaarRectangle(8, 15, 4, 2, -1), new HaarRectangle(8, 16, 4, 1, 2));
            stageNodes.AddHaarFeature(0.000869981711730361, 0.5334752798080444, 0.3885917961597443, new HaarRectangle(5, 2, 8, 2, -1), new HaarRectangle(5, 2, 4, 1, 2), new HaarRectangle(9, 3, 4, 1, 2));
            stageNodes.AddHaarFeature(0.0007637834060005844, 0.5398585200309753, 0.374494194984436, new HaarRectangle(6, 11, 8, 6, -1), new HaarRectangle(6, 14, 8, 3, 2));
            stageNodes.AddHaarFeature(0.00015684569370932877, 0.4317873120307922, 0.5614616274833679, new HaarRectangle(9, 13, 2, 2, -1), new HaarRectangle(9, 14, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.0215113703161478, 0.1785925030708313, 0.5185542702674866, new HaarRectangle(18, 4, 2, 6, -1), new HaarRectangle(18, 6, 2, 2, 3));
            stageNodes.AddHaarFeature(0.00013081369979772717, 0.4342499077320099, 0.5682849884033203, new HaarRectangle(9, 12, 2, 2, -1), new HaarRectangle(9, 13, 2, 1, 2));
            stageNodes.AddHaarFeature(0.021992040798068, 0.5161716938018799, 0.2379394024610519, new HaarRectangle(18, 4, 2, 6, -1), new HaarRectangle(18, 6, 2, 2, 3));
            stageNodes.AddHaarFeature(-0.000801365007646382, 0.598676323890686, 0.4466426968574524, new HaarRectangle(9, 13, 1, 3, -1), new HaarRectangle(9, 14, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.008273609913885593, 0.410821795463562, 0.5251057147979736, new HaarRectangle(18, 4, 2, 6, -1), new HaarRectangle(18, 6, 2, 2, 3));
            stageNodes.AddHaarFeature(0.0036831789184361696, 0.5173814296722412, 0.339751809835434, new HaarRectangle(0, 4, 2, 6, -1), new HaarRectangle(0, 6, 2, 2, 3));
            stageNodes.AddHaarFeature(-0.007952568121254444, 0.6888983249664307, 0.4845924079418182, new HaarRectangle(9, 12, 3, 3, -1), new HaarRectangle(9, 13, 3, 1, 3));
            stageNodes.AddHaarFeature(0.0015382299898192286, 0.5178567171096802, 0.3454113900661469, new HaarRectangle(3, 13, 2, 3, -1), new HaarRectangle(3, 14, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0140435304492712, 0.1678421050310135, 0.518866777420044, new HaarRectangle(13, 13, 4, 3, -1), new HaarRectangle(13, 14, 4, 1, 3));
            stageNodes.AddHaarFeature(0.0014315890148282051, 0.436825692653656, 0.5655773878097534, new HaarRectangle(5, 4, 3, 3, -1), new HaarRectangle(5, 5, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.0340142287313938, 0.7802296280860901, 0.4959217011928558, new HaarRectangle(5, 2, 10, 6, -1), new HaarRectangle(5, 4, 10, 2, 3));
            stageNodes.AddHaarFeature(-0.0120272999629378, 0.1585101038217545, 0.503223180770874, new HaarRectangle(3, 13, 4, 3, -1), new HaarRectangle(3, 14, 4, 1, 3));
            stageNodes.AddHaarFeature(0.1331661939620972, 0.5163304805755615, 0.2755128145217896, new HaarRectangle(3, 7, 15, 5, -1), new HaarRectangle(8, 7, 5, 5, 3));
            stageNodes.AddHaarFeature(-0.0015221949433907866, 0.372831791639328, 0.5214552283287048, new HaarRectangle(3, 7, 12, 2, -1), new HaarRectangle(7, 7, 4, 2, 3));
            stageNodes.AddHaarFeature(-0.000939292716793716, 0.5838379263877869, 0.4511165022850037, new HaarRectangle(10, 3, 3, 9, -1), new HaarRectangle(11, 3, 1, 9, 3));
            stageNodes.AddHaarFeature(0.0277197398245335, 0.4728286862373352, 0.7331544756889343, new HaarRectangle(8, 6, 4, 6, -1), new HaarRectangle(10, 6, 2, 6, 2));
            stageNodes.AddHaarFeature(0.003103015013039112, 0.5302202105522156, 0.4101563096046448, new HaarRectangle(9, 7, 4, 3, -1), new HaarRectangle(9, 8, 4, 1, 3));
            stageNodes.AddHaarFeature(0.0778612196445465, 0.4998334050178528, 0.127296194434166, new HaarRectangle(0, 9, 4, 9, -1), new HaarRectangle(2, 9, 2, 9, 2));
            stageNodes.AddHaarFeature(-0.0158549398183823, 0.0508333593606949, 0.5165656208992004, new HaarRectangle(9, 13, 3, 5, -1), new HaarRectangle(10, 13, 1, 5, 3));
            stageNodes.AddHaarFeature(-0.00497253006324172, 0.6798133850097656, 0.4684231877326965, new HaarRectangle(7, 7, 6, 3, -1), new HaarRectangle(9, 7, 2, 3, 3));
            stageNodes.AddHaarFeature(-0.0009767650626599789, 0.6010771989822388, 0.4788931906223297, new HaarRectangle(9, 7, 3, 5, -1), new HaarRectangle(10, 7, 1, 5, 3));
            stageNodes.AddHaarFeature(-0.0024647710379213095, 0.3393397927284241, 0.5220503807067871, new HaarRectangle(5, 7, 8, 2, -1), new HaarRectangle(9, 7, 4, 2, 2));
            stageNodes.AddHaarFeature(-0.006793770007789135, 0.4365136921405792, 0.5239663124084473, new HaarRectangle(5, 9, 12, 2, -1), new HaarRectangle(9, 9, 4, 2, 3));
            stageNodes.AddHaarFeature(0.0326080210506916, 0.505272388458252, 0.2425214946269989, new HaarRectangle(5, 6, 10, 3, -1), new HaarRectangle(10, 6, 5, 3, 2));
            stageNodes.AddHaarFeature(-0.0005851442110724747, 0.5733973979949951, 0.4758574068546295, new HaarRectangle(10, 12, 3, 1, -1), new HaarRectangle(11, 12, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.0296326000243425, 0.3892289102077484, 0.5263597965240479, new HaarRectangle(0, 1, 11, 15, -1), new HaarRectangle(0, 6, 11, 5, 3));
            stageNodeCounts.Add(stageNodes.Count - index); index = stageNodes.Count;

            stageThresholds.Add(67.69892120361328);
            stageNodes.AddHaarFeature(0.0465508513152599, 0.3276950120925903, 0.6240522861480713, new HaarRectangle(1, 0, 18, 6, -1), new HaarRectangle(7, 0, 6, 6, 3));
            stageNodes.AddHaarFeature(0.007953712716698647, 0.4256485104560852, 0.6942939162254333, new HaarRectangle(7, 7, 6, 1, -1), new HaarRectangle(9, 7, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0006822156137786806, 0.3711487054824829, 0.59007328748703, new HaarRectangle(5, 16, 6, 4, -1), new HaarRectangle(5, 16, 3, 2, 2), new HaarRectangle(8, 18, 3, 2, 2));
            stageNodes.AddHaarFeature(-0.00019348249770700932, 0.2041133940219879, 0.53005450963974, new HaarRectangle(6, 5, 9, 8, -1), new HaarRectangle(6, 9, 9, 4, 2));
            stageNodes.AddHaarFeature(-0.0002671050897333771, 0.5416126251220703, 0.3103179037570953, new HaarRectangle(5, 10, 2, 6, -1), new HaarRectangle(5, 13, 2, 3, 2));
            stageNodes.AddHaarFeature(0.0027818060480058193, 0.5277832746505737, 0.3467069864273071, new HaarRectangle(7, 6, 8, 10, -1), new HaarRectangle(11, 6, 4, 5, 2), new HaarRectangle(7, 11, 4, 5, 2));
            stageNodes.AddHaarFeature(-0.000467790785478428, 0.5308231115341187, 0.3294492065906525, new HaarRectangle(5, 6, 8, 10, -1), new HaarRectangle(5, 6, 4, 5, 2), new HaarRectangle(9, 11, 4, 5, 2));
            stageNodes.AddHaarFeature(-0.000030335160772665404, 0.577387273311615, 0.3852097094058991, new HaarRectangle(9, 5, 2, 2, -1), new HaarRectangle(9, 6, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0007803800981491805, 0.4317438900470734, 0.6150057911872864, new HaarRectangle(5, 12, 8, 2, -1), new HaarRectangle(5, 13, 8, 1, 2));
            stageNodes.AddHaarFeature(-0.004255385138094425, 0.2933903932571411, 0.5324292778968811, new HaarRectangle(10, 2, 8, 2, -1), new HaarRectangle(10, 3, 8, 1, 2));
            stageNodes.AddHaarFeature(-0.0002473561035003513, 0.5468844771385193, 0.3843030035495758, new HaarRectangle(4, 0, 2, 10, -1), new HaarRectangle(4, 0, 1, 5, 2), new HaarRectangle(5, 5, 1, 5, 2));
            stageNodes.AddHaarFeature(-0.00014724259381182492, 0.4281542897224426, 0.5755587220191956, new HaarRectangle(9, 10, 2, 2, -1), new HaarRectangle(9, 11, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0011864770203828812, 0.374730110168457, 0.5471466183662415, new HaarRectangle(2, 8, 15, 3, -1), new HaarRectangle(2, 9, 15, 1, 3));
            stageNodes.AddHaarFeature(0.0023936580400913954, 0.4537783861160278, 0.6111528873443604, new HaarRectangle(8, 13, 4, 3, -1), new HaarRectangle(8, 14, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.0015390539774671197, 0.2971341907978058, 0.518953800201416, new HaarRectangle(7, 2, 3, 2, -1), new HaarRectangle(8, 2, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.007196879014372826, 0.6699066758155823, 0.4726476967334747, new HaarRectangle(7, 13, 6, 3, -1), new HaarRectangle(7, 14, 6, 1, 3));
            stageNodes.AddHaarFeature(-0.0004149978922214359, 0.3384954035282135, 0.5260317921638489, new HaarRectangle(9, 9, 2, 2, -1), new HaarRectangle(9, 10, 2, 1, 2));
            stageNodes.AddHaarFeature(0.004435983020812273, 0.539912223815918, 0.3920140862464905, new HaarRectangle(17, 2, 3, 6, -1), new HaarRectangle(17, 4, 3, 2, 3));
            stageNodes.AddHaarFeature(0.0026606200262904167, 0.4482578039169312, 0.6119617819786072, new HaarRectangle(1, 5, 3, 4, -1), new HaarRectangle(2, 5, 1, 4, 3));
            stageNodes.AddHaarFeature(-0.0015287200221791863, 0.3711237907409668, 0.5340266227722168, new HaarRectangle(14, 8, 4, 6, -1), new HaarRectangle(14, 10, 4, 2, 3));
            stageNodes.AddHaarFeature(-0.0047397250309586525, 0.603108823299408, 0.4455145001411438, new HaarRectangle(1, 4, 3, 8, -1), new HaarRectangle(2, 4, 1, 8, 3));
            stageNodes.AddHaarFeature(-0.0148291299119592, 0.2838754057884216, 0.5341861844062805, new HaarRectangle(8, 13, 4, 6, -1), new HaarRectangle(8, 16, 4, 3, 2));
            stageNodes.AddHaarFeature(0.0009227555710822344, 0.5209547281265259, 0.3361653983592987, new HaarRectangle(3, 14, 2, 2, -1), new HaarRectangle(3, 15, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0835298076272011, 0.5119969844818115, 0.0811644494533539, new HaarRectangle(14, 8, 4, 6, -1), new HaarRectangle(14, 10, 4, 2, 3));
            stageNodes.AddHaarFeature(-0.0007563314866274595, 0.331712007522583, 0.5189831256866455, new HaarRectangle(2, 8, 4, 6, -1), new HaarRectangle(2, 10, 4, 2, 3));
            stageNodes.AddHaarFeature(0.009840385988354683, 0.524759829044342, 0.233495905995369, new HaarRectangle(10, 14, 1, 6, -1), new HaarRectangle(10, 17, 1, 3, 2));
            stageNodes.AddHaarFeature(-0.001595383044332266, 0.5750094056129456, 0.4295622110366821, new HaarRectangle(7, 5, 3, 6, -1), new HaarRectangle(8, 5, 1, 6, 3));
            stageNodes.AddHaarFeature(0.000034766020689858124, 0.4342445135116577, 0.5564029216766357, new HaarRectangle(11, 2, 2, 6, -1), new HaarRectangle(12, 2, 1, 3, 2), new HaarRectangle(11, 5, 1, 3, 2));
            stageNodes.AddHaarFeature(0.0298629105091095, 0.4579147100448608, 0.6579188108444214, new HaarRectangle(6, 6, 6, 5, -1), new HaarRectangle(8, 6, 2, 5, 3));
            stageNodes.AddHaarFeature(0.0113255903124809, 0.5274311900138855, 0.3673888146877289, new HaarRectangle(17, 1, 3, 6, -1), new HaarRectangle(17, 3, 3, 2, 3));
            stageNodes.AddHaarFeature(-0.008782864548265934, 0.7100368738174438, 0.4642167091369629, new HaarRectangle(8, 7, 3, 5, -1), new HaarRectangle(9, 7, 1, 5, 3));
            stageNodes.AddHaarFeature(0.004363995976746082, 0.5279216170310974, 0.2705877125263214, new HaarRectangle(9, 18, 3, 2, -1), new HaarRectangle(10, 18, 1, 2, 3));
            stageNodes.AddHaarFeature(0.004180472809821367, 0.5072525143623352, 0.2449083030223846, new HaarRectangle(8, 18, 3, 2, -1), new HaarRectangle(9, 18, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.0004566851130221039, 0.4283105134963989, 0.5548691153526306, new HaarRectangle(12, 3, 5, 2, -1), new HaarRectangle(12, 4, 5, 1, 2));
            stageNodes.AddHaarFeature(-0.003714036894962192, 0.5519387722015381, 0.4103653132915497, new HaarRectangle(7, 1, 5, 12, -1), new HaarRectangle(7, 7, 5, 6, 2));
            stageNodes.AddHaarFeature(-0.025304289534688, 0.6867002248764038, 0.48698890209198, new HaarRectangle(1, 0, 18, 4, -1), new HaarRectangle(7, 0, 6, 4, 3));
            stageNodes.AddHaarFeature(-0.0003445408074185252, 0.3728874027729034, 0.528769314289093, new HaarRectangle(4, 2, 2, 2, -1), new HaarRectangle(4, 3, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.0008393523166887462, 0.6060152053833008, 0.4616062045097351, new HaarRectangle(11, 14, 4, 2, -1), new HaarRectangle(13, 14, 2, 1, 2), new HaarRectangle(11, 15, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0172800496220589, 0.5049635767936707, 0.1819823980331421, new HaarRectangle(0, 2, 3, 6, -1), new HaarRectangle(0, 4, 3, 2, 3));
            stageNodes.AddHaarFeature(-0.006359507795423269, 0.1631239950656891, 0.5232778787612915, new HaarRectangle(9, 7, 2, 3, -1), new HaarRectangle(9, 8, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0010298109846189618, 0.446327805519104, 0.6176549196243286, new HaarRectangle(5, 5, 1, 3, -1), new HaarRectangle(5, 6, 1, 1, 3));
            stageNodes.AddHaarFeature(0.0010117109632119536, 0.5473384857177734, 0.4300698935985565, new HaarRectangle(10, 10, 6, 1, -1), new HaarRectangle(10, 10, 3, 1, 2));
            stageNodes.AddHaarFeature(-0.010308800265193, 0.1166985034942627, 0.5000867247581482, new HaarRectangle(4, 10, 6, 1, -1), new HaarRectangle(7, 10, 3, 1, 2));
            stageNodes.AddHaarFeature(0.005468201823532581, 0.4769287109375, 0.6719213724136353, new HaarRectangle(9, 17, 3, 3, -1), new HaarRectangle(9, 18, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.0009169646073132753, 0.3471089899539948, 0.5178164839744568, new HaarRectangle(4, 14, 1, 3, -1), new HaarRectangle(4, 15, 1, 1, 3));
            stageNodes.AddHaarFeature(0.002392282010987401, 0.4785236120223999, 0.6216310858726501, new HaarRectangle(12, 5, 3, 3, -1), new HaarRectangle(12, 6, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.007557381875813008, 0.5814796090126038, 0.4410085082054138, new HaarRectangle(4, 5, 12, 3, -1), new HaarRectangle(4, 6, 12, 1, 3));
            stageNodes.AddHaarFeature(-0.0007702403236180544, 0.387800008058548, 0.546572208404541, new HaarRectangle(9, 8, 2, 3, -1), new HaarRectangle(9, 9, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.00871259905397892, 0.1660051047801971, 0.4995836019515991, new HaarRectangle(4, 9, 3, 3, -1), new HaarRectangle(5, 9, 1, 3, 3));
            stageNodes.AddHaarFeature(-0.0103063201531768, 0.4093391001224518, 0.5274233818054199, new HaarRectangle(6, 0, 9, 17, -1), new HaarRectangle(9, 0, 3, 17, 3));
            stageNodes.AddHaarFeature(-0.002094097901135683, 0.6206194758415222, 0.4572280049324036, new HaarRectangle(9, 12, 1, 3, -1), new HaarRectangle(9, 13, 1, 1, 3));
            stageNodes.AddHaarFeature(0.006809905171394348, 0.5567759275436401, 0.4155600070953369, new HaarRectangle(9, 5, 2, 15, -1), new HaarRectangle(9, 10, 2, 5, 3));
            stageNodes.AddHaarFeature(-0.0010746059706434608, 0.5638927817344666, 0.4353024959564209, new HaarRectangle(8, 14, 2, 3, -1), new HaarRectangle(8, 15, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0021550289820879698, 0.4826265871524811, 0.6749758124351501, new HaarRectangle(10, 14, 1, 3, -1), new HaarRectangle(10, 15, 1, 1, 3));
            stageNodes.AddHaarFeature(0.0317423194646835, 0.5048379898071289, 0.188324898481369, new HaarRectangle(7, 1, 6, 5, -1), new HaarRectangle(9, 1, 2, 5, 3));
            stageNodes.AddHaarFeature(-0.0783827230334282, 0.2369548976421356, 0.5260158181190491, new HaarRectangle(0, 0, 20, 2, -1), new HaarRectangle(0, 0, 10, 2, 2));
            stageNodes.AddHaarFeature(0.005741511937230825, 0.5048828721046448, 0.2776469886302948, new HaarRectangle(2, 13, 5, 3, -1), new HaarRectangle(2, 14, 5, 1, 3));
            stageNodes.AddHaarFeature(-0.0029014600440859795, 0.6238604784011841, 0.4693317115306854, new HaarRectangle(9, 11, 2, 3, -1), new HaarRectangle(9, 12, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0026427931152284145, 0.3314141929149628, 0.5169777274131775, new HaarRectangle(2, 5, 9, 15, -1), new HaarRectangle(2, 10, 9, 5, 3));
            stageNodes.AddHaarFeature(-0.1094966009259224, 0.2380045056343079, 0.5183441042900085, new HaarRectangle(5, 0, 12, 10, -1), new HaarRectangle(11, 0, 6, 5, 2), new HaarRectangle(5, 5, 6, 5, 2));
            stageNodes.AddHaarFeature(0.00007407591328956187, 0.406963586807251, 0.5362150073051453, new HaarRectangle(5, 1, 2, 3, -1), new HaarRectangle(6, 1, 1, 3, 2));
            stageNodes.AddHaarFeature(-0.0005059380200691521, 0.5506706237792969, 0.437459409236908, new HaarRectangle(10, 7, 6, 1, -1), new HaarRectangle(12, 7, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0008213177789002658, 0.5525709986686707, 0.4209375977516174, new HaarRectangle(3, 1, 2, 10, -1), new HaarRectangle(3, 1, 1, 5, 2), new HaarRectangle(4, 6, 1, 5, 2));
            stageNodes.AddHaarFeature(-0.000060276539443293586, 0.5455474853515625, 0.4748266041278839, new HaarRectangle(13, 7, 2, 1, -1), new HaarRectangle(13, 7, 1, 1, 2));
            stageNodes.AddHaarFeature(0.006806514225900173, 0.5157995820045471, 0.3424577116966248, new HaarRectangle(4, 13, 4, 6, -1), new HaarRectangle(4, 15, 4, 2, 3));
            stageNodes.AddHaarFeature(0.0017202789895236492, 0.5013207793235779, 0.6331263780593872, new HaarRectangle(13, 7, 2, 1, -1), new HaarRectangle(13, 7, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0001301692973356694, 0.5539718270301819, 0.4226869940757752, new HaarRectangle(5, 7, 2, 1, -1), new HaarRectangle(6, 7, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.004801638890057802, 0.4425095021724701, 0.5430780053138733, new HaarRectangle(2, 12, 18, 4, -1), new HaarRectangle(11, 12, 9, 2, 2), new HaarRectangle(2, 14, 9, 2, 2));
            stageNodes.AddHaarFeature(-0.002539931097999215, 0.7145782113075256, 0.4697605073451996, new HaarRectangle(5, 7, 2, 2, -1), new HaarRectangle(5, 7, 1, 1, 2), new HaarRectangle(6, 8, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0014278929447755218, 0.4070445001125336, 0.539960503578186, new HaarRectangle(16, 3, 4, 2, -1), new HaarRectangle(16, 4, 4, 1, 2));
            stageNodes.AddHaarFeature(-0.0251425504684448, 0.7884690761566162, 0.4747352004051209, new HaarRectangle(0, 2, 2, 18, -1), new HaarRectangle(0, 2, 1, 9, 2), new HaarRectangle(1, 11, 1, 9, 2));
            stageNodes.AddHaarFeature(-0.003889960935339331, 0.4296191930770874, 0.5577110052108765, new HaarRectangle(1, 2, 18, 4, -1), new HaarRectangle(10, 2, 9, 2, 2), new HaarRectangle(1, 4, 9, 2, 2));
            stageNodes.AddHaarFeature(0.004394745919853449, 0.4693162143230438, 0.702394425868988, new HaarRectangle(9, 14, 1, 3, -1), new HaarRectangle(9, 15, 1, 1, 3));
            stageNodes.AddHaarFeature(0.0246784202754498, 0.5242322087287903, 0.3812510073184967, new HaarRectangle(2, 12, 18, 4, -1), new HaarRectangle(11, 12, 9, 2, 2), new HaarRectangle(2, 14, 9, 2, 2));
            stageNodes.AddHaarFeature(0.0380476787686348, 0.5011739730834961, 0.1687828004360199, new HaarRectangle(0, 12, 18, 4, -1), new HaarRectangle(0, 12, 9, 2, 2), new HaarRectangle(9, 14, 9, 2, 2));
            stageNodes.AddHaarFeature(0.007942486554384232, 0.4828582108020783, 0.6369568109512329, new HaarRectangle(11, 4, 5, 3, -1), new HaarRectangle(11, 5, 5, 1, 3));
            stageNodes.AddHaarFeature(-0.0015110049862414598, 0.5906485915184021, 0.4487667977809906, new HaarRectangle(6, 4, 7, 3, -1), new HaarRectangle(6, 5, 7, 1, 3));
            stageNodes.AddHaarFeature(0.0064201741479337215, 0.5241097807884216, 0.2990570068359375, new HaarRectangle(13, 17, 3, 3, -1), new HaarRectangle(13, 18, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.0029802159406244755, 0.3041465878486633, 0.5078489780426025, new HaarRectangle(8, 1, 3, 4, -1), new HaarRectangle(9, 1, 1, 4, 3));
            stageNodes.AddHaarFeature(-0.0007458007894456388, 0.4128139019012451, 0.5256826281547546, new HaarRectangle(11, 4, 2, 4, -1), new HaarRectangle(11, 4, 1, 4, 2));
            stageNodes.AddHaarFeature(-0.0104709500446916, 0.5808395147323608, 0.4494296014308929, new HaarRectangle(0, 17, 9, 3, -1), new HaarRectangle(3, 17, 3, 3, 3));
            stageNodes.AddHaarFeature(0.009336920455098152, 0.524655282497406, 0.265894889831543, new HaarRectangle(11, 0, 2, 8, -1), new HaarRectangle(12, 0, 1, 4, 2), new HaarRectangle(11, 4, 1, 4, 2));
            stageNodes.AddHaarFeature(0.0279369000345469, 0.4674955010414124, 0.7087256908416748, new HaarRectangle(0, 8, 6, 12, -1), new HaarRectangle(0, 8, 3, 6, 2), new HaarRectangle(3, 14, 3, 6, 2));
            stageNodes.AddHaarFeature(0.007427767850458622, 0.5409486889839172, 0.3758518099784851, new HaarRectangle(10, 7, 4, 12, -1), new HaarRectangle(10, 13, 4, 6, 2));
            stageNodes.AddHaarFeature(-0.0235845092684031, 0.3758639991283417, 0.5238550901412964, new HaarRectangle(5, 3, 8, 14, -1), new HaarRectangle(5, 10, 8, 7, 2));
            stageNodes.AddHaarFeature(0.0011452640173956752, 0.4329578876495361, 0.5804247260093689, new HaarRectangle(14, 10, 6, 1, -1), new HaarRectangle(14, 10, 3, 1, 2));
            stageNodes.AddHaarFeature(-0.00043468660442158574, 0.5280618071556091, 0.3873069882392883, new HaarRectangle(0, 4, 10, 4, -1), new HaarRectangle(0, 6, 10, 2, 2));
            stageNodes.AddHaarFeature(0.0106485402211547, 0.4902113080024719, 0.5681251883506775, new HaarRectangle(10, 0, 5, 8, -1), new HaarRectangle(10, 4, 5, 4, 2));
            stageNodes.AddHaarFeature(-0.0003941805043723434, 0.5570880174636841, 0.4318251013755798, new HaarRectangle(8, 1, 4, 8, -1), new HaarRectangle(8, 1, 2, 4, 2), new HaarRectangle(10, 5, 2, 4, 2));
            stageNodes.AddHaarFeature(-0.00013270479394122958, 0.5658439993858337, 0.4343554973602295, new HaarRectangle(9, 11, 6, 1, -1), new HaarRectangle(11, 11, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.002012551063671708, 0.6056739091873169, 0.4537523984909058, new HaarRectangle(8, 9, 3, 4, -1), new HaarRectangle(9, 9, 1, 4, 3));
            stageNodes.AddHaarFeature(0.0024854319635778666, 0.5390477180480957, 0.4138010144233704, new HaarRectangle(18, 4, 2, 6, -1), new HaarRectangle(18, 6, 2, 2, 3));
            stageNodes.AddHaarFeature(0.0018237880431115627, 0.4354828894138336, 0.5717188715934753, new HaarRectangle(8, 8, 3, 4, -1), new HaarRectangle(9, 8, 1, 4, 3));
            stageNodes.AddHaarFeature(-0.0166566595435143, 0.3010913133621216, 0.521612286567688, new HaarRectangle(7, 1, 13, 3, -1), new HaarRectangle(7, 2, 13, 1, 3));
            stageNodes.AddHaarFeature(0.0008034955826587974, 0.5300151109695435, 0.3818396925926209, new HaarRectangle(7, 13, 6, 1, -1), new HaarRectangle(9, 13, 2, 1, 3));
            stageNodes.AddHaarFeature(0.003417037893086672, 0.5328028798103333, 0.4241400063037872, new HaarRectangle(12, 11, 3, 6, -1), new HaarRectangle(12, 13, 3, 2, 3));
            stageNodes.AddHaarFeature(-0.00036222729249857366, 0.5491728186607361, 0.418697714805603, new HaarRectangle(5, 11, 6, 1, -1), new HaarRectangle(7, 11, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.1163002029061317, 0.1440722048282623, 0.522645115852356, new HaarRectangle(1, 4, 18, 10, -1), new HaarRectangle(10, 4, 9, 5, 2), new HaarRectangle(1, 9, 9, 5, 2));
            stageNodes.AddHaarFeature(-0.0146950101479888, 0.7747725248336792, 0.4715717136859894, new HaarRectangle(8, 6, 4, 9, -1), new HaarRectangle(8, 9, 4, 3, 3));
            stageNodes.AddHaarFeature(0.0021972130052745342, 0.5355433821678162, 0.3315644860267639, new HaarRectangle(8, 6, 4, 3, -1), new HaarRectangle(8, 7, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.0004696520918514579, 0.5767235159873962, 0.4458136856555939, new HaarRectangle(8, 7, 3, 3, -1), new HaarRectangle(9, 7, 1, 3, 3));
            stageNodes.AddHaarFeature(0.006514499895274639, 0.5215674042701721, 0.3647888898849487, new HaarRectangle(14, 15, 4, 3, -1), new HaarRectangle(14, 16, 4, 1, 3));
            stageNodes.AddHaarFeature(0.0213000606745481, 0.4994204938411713, 0.1567950993776321, new HaarRectangle(5, 10, 3, 10, -1), new HaarRectangle(6, 10, 1, 10, 3));
            stageNodes.AddHaarFeature(0.0031881409231573343, 0.4742200076580048, 0.6287270188331604, new HaarRectangle(8, 15, 4, 3, -1), new HaarRectangle(8, 16, 4, 1, 3));
            stageNodes.AddHaarFeature(0.0009001977741718293, 0.5347954034805298, 0.394375205039978, new HaarRectangle(0, 8, 1, 6, -1), new HaarRectangle(0, 10, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.005177227780222893, 0.6727191805839539, 0.5013138055801392, new HaarRectangle(10, 15, 1, 3, -1), new HaarRectangle(10, 16, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.004376464989036322, 0.3106675148010254, 0.5128793120384216, new HaarRectangle(2, 15, 4, 3, -1), new HaarRectangle(2, 16, 4, 1, 3));
            stageNodes.AddHaarFeature(0.002629996044561267, 0.488631010055542, 0.5755215883255005, new HaarRectangle(18, 3, 2, 8, -1), new HaarRectangle(19, 3, 1, 4, 2), new HaarRectangle(18, 7, 1, 4, 2));
            stageNodes.AddHaarFeature(-0.002045868895947933, 0.6025794148445129, 0.4558076858520508, new HaarRectangle(0, 3, 2, 8, -1), new HaarRectangle(0, 3, 1, 4, 2), new HaarRectangle(1, 7, 1, 4, 2));
            stageNodes.AddHaarFeature(0.0694827064871788, 0.5240747928619385, 0.2185259014368057, new HaarRectangle(3, 7, 14, 10, -1), new HaarRectangle(10, 7, 7, 5, 2), new HaarRectangle(3, 12, 7, 5, 2));
            stageNodes.AddHaarFeature(0.0240489393472672, 0.501186728477478, 0.2090622037649155, new HaarRectangle(0, 7, 19, 3, -1), new HaarRectangle(0, 8, 19, 1, 3));
            stageNodes.AddHaarFeature(0.003109534038230777, 0.4866712093353272, 0.7108548283576965, new HaarRectangle(12, 6, 3, 3, -1), new HaarRectangle(12, 7, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.00125032605137676, 0.3407891094684601, 0.5156195163726807, new HaarRectangle(0, 6, 1, 3, -1), new HaarRectangle(0, 7, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.0010281190043315291, 0.557557225227356, 0.443943202495575, new HaarRectangle(12, 6, 3, 3, -1), new HaarRectangle(12, 7, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.008889362215995789, 0.6402000784873962, 0.4620442092418671, new HaarRectangle(5, 6, 3, 3, -1), new HaarRectangle(5, 7, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.0006109480164013803, 0.3766441941261292, 0.5448899865150452, new HaarRectangle(8, 2, 4, 2, -1), new HaarRectangle(8, 3, 4, 1, 2));
            stageNodes.AddHaarFeature(-0.005768635775893927, 0.3318648934364319, 0.5133677124977112, new HaarRectangle(6, 3, 4, 12, -1), new HaarRectangle(8, 3, 2, 12, 2));
            stageNodes.AddHaarFeature(0.0018506490159779787, 0.4903570115566254, 0.6406934857368469, new HaarRectangle(13, 6, 2, 3, -1), new HaarRectangle(13, 7, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0997994691133499, 0.1536051034927368, 0.5015562176704407, new HaarRectangle(0, 10, 20, 4, -1), new HaarRectangle(0, 12, 20, 2, 2));
            stageNodes.AddHaarFeature(-0.3512834906578064, 0.0588231310248375, 0.5174378752708435, new HaarRectangle(2, 0, 17, 14, -1), new HaarRectangle(2, 7, 17, 7, 2));
            stageNodes.AddHaarFeature(-0.0452445708215237, 0.6961488723754883, 0.4677872955799103, new HaarRectangle(0, 0, 6, 10, -1), new HaarRectangle(0, 0, 3, 5, 2), new HaarRectangle(3, 5, 3, 5, 2));
            stageNodes.AddHaarFeature(0.0714815780520439, 0.5167986154556274, 0.1038092970848084, new HaarRectangle(14, 6, 6, 4, -1), new HaarRectangle(14, 6, 3, 4, 2));
            stageNodes.AddHaarFeature(0.0021895780228078365, 0.4273078143596649, 0.5532060861587524, new HaarRectangle(0, 6, 6, 4, -1), new HaarRectangle(3, 6, 3, 4, 2));
            stageNodes.AddHaarFeature(-0.0005924265133216977, 0.46389439702034, 0.5276389122009277, new HaarRectangle(13, 2, 7, 2, -1), new HaarRectangle(13, 3, 7, 1, 2));
            stageNodes.AddHaarFeature(0.0016788389766588807, 0.530164897441864, 0.3932034969329834, new HaarRectangle(0, 2, 7, 2, -1), new HaarRectangle(0, 3, 7, 1, 2));
            stageNodes.AddHaarFeature(-0.0022163488902151585, 0.5630694031715393, 0.4757033884525299, new HaarRectangle(6, 11, 14, 2, -1), new HaarRectangle(13, 11, 7, 1, 2), new HaarRectangle(6, 12, 7, 1, 2));
            stageNodes.AddHaarFeature(0.00011568699846975504, 0.4307535886764526, 0.5535702705383301, new HaarRectangle(8, 5, 2, 2, -1), new HaarRectangle(8, 5, 1, 1, 2), new HaarRectangle(9, 6, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.007201728876680136, 0.144488200545311, 0.5193064212799072, new HaarRectangle(13, 9, 2, 3, -1), new HaarRectangle(13, 9, 1, 3, 2));
            stageNodes.AddHaarFeature(0.0008908127201721072, 0.4384432137012482, 0.5593621134757996, new HaarRectangle(1, 1, 3, 12, -1), new HaarRectangle(2, 1, 1, 12, 3));
            stageNodes.AddHaarFeature(0.00019605009583756328, 0.5340415835380554, 0.4705956876277924, new HaarRectangle(17, 4, 1, 3, -1), new HaarRectangle(17, 5, 1, 1, 3));
            stageNodes.AddHaarFeature(0.0005202214233577251, 0.5213856101036072, 0.3810079097747803, new HaarRectangle(2, 4, 1, 3, -1), new HaarRectangle(2, 5, 1, 1, 3));
            stageNodes.AddHaarFeature(0.0009458857239224016, 0.4769414961338043, 0.6130738854408264, new HaarRectangle(14, 5, 1, 3, -1), new HaarRectangle(14, 6, 1, 1, 3));
            stageNodes.AddHaarFeature(0.0000916984718060121, 0.4245009124279022, 0.5429363250732422, new HaarRectangle(7, 16, 2, 3, -1), new HaarRectangle(7, 17, 2, 1, 3));
            stageNodes.AddHaarFeature(0.002183320000767708, 0.5457730889320374, 0.419107586145401, new HaarRectangle(8, 13, 4, 6, -1), new HaarRectangle(10, 13, 2, 3, 2), new HaarRectangle(8, 16, 2, 3, 2));
            stageNodes.AddHaarFeature(-0.0008603967144154013, 0.5764588713645935, 0.4471659958362579, new HaarRectangle(5, 5, 1, 3, -1), new HaarRectangle(5, 6, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.0132362395524979, 0.6372823119163513, 0.4695009887218475, new HaarRectangle(16, 0, 4, 20, -1), new HaarRectangle(16, 0, 2, 20, 2));
            stageNodes.AddHaarFeature(0.0004337670106906444, 0.5317873954772949, 0.394582986831665, new HaarRectangle(5, 1, 2, 6, -1), new HaarRectangle(5, 1, 1, 3, 2), new HaarRectangle(6, 4, 1, 3, 2));
            stageNodeCounts.Add(stageNodes.Count - index); index = stageNodes.Count;

            stageThresholds.Add(69.22987365722656);
            stageNodes.AddHaarFeature(-0.024847149848938, 0.6555516719818115, 0.3873311877250671, new HaarRectangle(5, 4, 10, 4, -1), new HaarRectangle(5, 6, 10, 2, 2));
            stageNodes.AddHaarFeature(0.006134861148893834, 0.374807208776474, 0.5973997712135315, new HaarRectangle(15, 2, 4, 12, -1), new HaarRectangle(15, 2, 2, 12, 2));
            stageNodes.AddHaarFeature(0.006449849810451269, 0.542549192905426, 0.2548811137676239, new HaarRectangle(7, 6, 4, 12, -1), new HaarRectangle(7, 12, 4, 6, 2));
            stageNodes.AddHaarFeature(0.0006349121103994548, 0.2462442070245743, 0.5387253761291504, new HaarRectangle(14, 5, 1, 8, -1), new HaarRectangle(14, 9, 1, 4, 2));
            stageNodes.AddHaarFeature(0.0014023890253156424, 0.5594322085380554, 0.3528657853603363, new HaarRectangle(1, 4, 14, 10, -1), new HaarRectangle(1, 4, 7, 5, 2), new HaarRectangle(8, 9, 7, 5, 2));
            stageNodes.AddHaarFeature(0.0003004400059580803, 0.3958503901958466, 0.576593816280365, new HaarRectangle(11, 6, 6, 14, -1), new HaarRectangle(14, 6, 3, 7, 2), new HaarRectangle(11, 13, 3, 7, 2));
            stageNodes.AddHaarFeature(0.00010042409849120304, 0.3698996901512146, 0.5534998178482056, new HaarRectangle(3, 6, 6, 14, -1), new HaarRectangle(3, 6, 3, 7, 2), new HaarRectangle(6, 13, 3, 7, 2));
            stageNodes.AddHaarFeature(-0.005084149073809385, 0.3711090981960297, 0.5547800064086914, new HaarRectangle(4, 9, 15, 2, -1), new HaarRectangle(9, 9, 5, 2, 3));
            stageNodes.AddHaarFeature(-0.0195372607558966, 0.7492755055427551, 0.4579297006130219, new HaarRectangle(7, 14, 6, 3, -1), new HaarRectangle(7, 15, 6, 1, 3));
            stageNodes.AddHaarFeature(-0.000007453274065483128, 0.5649787187576294, 0.390406996011734, new HaarRectangle(6, 3, 14, 4, -1), new HaarRectangle(13, 3, 7, 2, 2), new HaarRectangle(6, 5, 7, 2, 2));
            stageNodes.AddHaarFeature(-0.0036079459823668, 0.3381088078022003, 0.5267801284790039, new HaarRectangle(1, 9, 15, 2, -1), new HaarRectangle(6, 9, 5, 2, 3));
            stageNodes.AddHaarFeature(0.0020697501022368674, 0.5519291162490845, 0.3714388906955719, new HaarRectangle(6, 11, 8, 9, -1), new HaarRectangle(6, 14, 8, 3, 3));
            stageNodes.AddHaarFeature(-0.0004646384040825069, 0.5608214735984802, 0.4113566875457764, new HaarRectangle(7, 4, 3, 8, -1), new HaarRectangle(8, 4, 1, 8, 3));
            stageNodes.AddHaarFeature(0.0007549045258201659, 0.3559206128120422, 0.532935619354248, new HaarRectangle(14, 6, 2, 6, -1), new HaarRectangle(14, 9, 2, 3, 2));
            stageNodes.AddHaarFeature(-0.0009832223877310753, 0.5414795875549316, 0.3763205111026764, new HaarRectangle(5, 7, 6, 4, -1), new HaarRectangle(5, 7, 3, 2, 2), new HaarRectangle(8, 9, 3, 2, 2));
            stageNodes.AddHaarFeature(-0.0199406407773495, 0.634790301322937, 0.4705299139022827, new HaarRectangle(1, 1, 18, 19, -1), new HaarRectangle(7, 1, 6, 19, 3));
            stageNodes.AddHaarFeature(0.00376803008839488, 0.3913489878177643, 0.5563716292381287, new HaarRectangle(1, 2, 6, 5, -1), new HaarRectangle(4, 2, 3, 5, 2));
            stageNodes.AddHaarFeature(-0.009452850557863712, 0.2554892897605896, 0.5215116739273071, new HaarRectangle(12, 17, 6, 2, -1), new HaarRectangle(12, 18, 6, 1, 2));
            stageNodes.AddHaarFeature(0.002956084907054901, 0.5174679160118103, 0.3063920140266419, new HaarRectangle(2, 17, 6, 2, -1), new HaarRectangle(2, 18, 6, 1, 2));
            stageNodes.AddHaarFeature(0.009107873775064945, 0.5388448238372803, 0.2885963022708893, new HaarRectangle(17, 3, 3, 6, -1), new HaarRectangle(17, 5, 3, 2, 3));
            stageNodes.AddHaarFeature(0.0018219229532405736, 0.4336043000221252, 0.58521968126297, new HaarRectangle(8, 17, 3, 3, -1), new HaarRectangle(8, 18, 3, 1, 3));
            stageNodes.AddHaarFeature(0.0146887395530939, 0.5287361741065979, 0.2870005965232849, new HaarRectangle(10, 13, 2, 6, -1), new HaarRectangle(10, 16, 2, 3, 2));
            stageNodes.AddHaarFeature(-0.0143879903480411, 0.701944887638092, 0.4647370874881744, new HaarRectangle(7, 13, 6, 3, -1), new HaarRectangle(7, 14, 6, 1, 3));
            stageNodes.AddHaarFeature(-0.0189866498112679, 0.2986552119255066, 0.5247011780738831, new HaarRectangle(17, 3, 3, 6, -1), new HaarRectangle(17, 5, 3, 2, 3));
            stageNodes.AddHaarFeature(0.0011527639580890536, 0.4323473870754242, 0.593166172504425, new HaarRectangle(8, 13, 2, 3, -1), new HaarRectangle(8, 14, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0109336702153087, 0.5286864042282104, 0.3130319118499756, new HaarRectangle(9, 3, 6, 2, -1), new HaarRectangle(11, 3, 2, 2, 3));
            stageNodes.AddHaarFeature(-0.0149327302351594, 0.2658419013023377, 0.508407711982727, new HaarRectangle(0, 3, 3, 6, -1), new HaarRectangle(0, 5, 3, 2, 3));
            stageNodes.AddHaarFeature(-0.0002997053961735219, 0.5463526844978333, 0.374072402715683, new HaarRectangle(8, 5, 4, 6, -1), new HaarRectangle(8, 7, 4, 2, 3));
            stageNodes.AddHaarFeature(0.004167762119323015, 0.4703496992588043, 0.7435721755027771, new HaarRectangle(5, 5, 3, 2, -1), new HaarRectangle(5, 6, 3, 1, 2));
            stageNodes.AddHaarFeature(-0.00639053201302886, 0.2069258987903595, 0.5280538201332092, new HaarRectangle(10, 1, 3, 4, -1), new HaarRectangle(11, 1, 1, 4, 3));
            stageNodes.AddHaarFeature(0.004502960946410894, 0.518264889717102, 0.348354309797287, new HaarRectangle(1, 2, 5, 9, -1), new HaarRectangle(1, 5, 5, 3, 3));
            stageNodes.AddHaarFeature(-0.009204036556184292, 0.680377721786499, 0.4932360053062439, new HaarRectangle(13, 6, 2, 3, -1), new HaarRectangle(13, 7, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0813272595405579, 0.5058398842811584, 0.2253051996231079, new HaarRectangle(0, 6, 14, 3, -1), new HaarRectangle(7, 6, 7, 3, 2));
            stageNodes.AddHaarFeature(-0.150792807340622, 0.2963424921035767, 0.5264679789543152, new HaarRectangle(2, 11, 18, 8, -1), new HaarRectangle(2, 15, 18, 4, 2));
            stageNodes.AddHaarFeature(0.0033179009333252907, 0.4655495882034302, 0.7072932124137878, new HaarRectangle(5, 6, 2, 3, -1), new HaarRectangle(5, 7, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0007740280125290155, 0.4780347943305969, 0.5668237805366516, new HaarRectangle(10, 6, 4, 2, -1), new HaarRectangle(12, 6, 2, 1, 2), new HaarRectangle(10, 7, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0006819954141974449, 0.4286996126174927, 0.5722156763076782, new HaarRectangle(6, 6, 4, 2, -1), new HaarRectangle(6, 6, 2, 1, 2), new HaarRectangle(8, 7, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0053671570494771, 0.5299307107925415, 0.3114621937274933, new HaarRectangle(10, 1, 3, 4, -1), new HaarRectangle(11, 1, 1, 4, 3));
            stageNodes.AddHaarFeature(0.00009701866656541824, 0.3674638867378235, 0.5269461870193481, new HaarRectangle(7, 1, 2, 7, -1), new HaarRectangle(8, 1, 1, 7, 2));
            stageNodes.AddHaarFeature(-0.1253408938646317, 0.2351492047309876, 0.5245791077613831, new HaarRectangle(4, 2, 15, 14, -1), new HaarRectangle(4, 9, 15, 7, 2));
            stageNodes.AddHaarFeature(-0.005251626949757338, 0.7115936875343323, 0.4693767130374908, new HaarRectangle(8, 7, 3, 2, -1), new HaarRectangle(9, 7, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.007834210991859436, 0.4462651014328003, 0.5409085750579834, new HaarRectangle(2, 3, 18, 4, -1), new HaarRectangle(11, 3, 9, 2, 2), new HaarRectangle(2, 5, 9, 2, 2));
            stageNodes.AddHaarFeature(-0.001131006982177496, 0.5945618748664856, 0.4417662024497986, new HaarRectangle(9, 7, 2, 2, -1), new HaarRectangle(10, 7, 1, 2, 2));
            stageNodes.AddHaarFeature(0.0017601120052859187, 0.5353249907493591, 0.3973453044891357, new HaarRectangle(13, 9, 2, 3, -1), new HaarRectangle(13, 9, 1, 3, 2));
            stageNodes.AddHaarFeature(-0.00081581249833107, 0.3760268092155457, 0.5264726877212524, new HaarRectangle(5, 2, 6, 2, -1), new HaarRectangle(7, 2, 2, 2, 3));
            stageNodes.AddHaarFeature(-0.0038687589112669234, 0.6309912800788879, 0.4749819934368134, new HaarRectangle(9, 5, 2, 7, -1), new HaarRectangle(9, 5, 1, 7, 2));
            stageNodes.AddHaarFeature(0.0015207129763439298, 0.5230181813240051, 0.3361223936080933, new HaarRectangle(5, 9, 2, 3, -1), new HaarRectangle(6, 9, 1, 3, 2));
            stageNodes.AddHaarFeature(0.545867383480072, 0.5167139768600464, 0.1172635033726692, new HaarRectangle(6, 0, 14, 18, -1), new HaarRectangle(6, 9, 14, 9, 2));
            stageNodes.AddHaarFeature(0.0156501904129982, 0.4979439079761505, 0.1393294930458069, new HaarRectangle(2, 16, 6, 3, -1), new HaarRectangle(2, 17, 6, 1, 3));
            stageNodes.AddHaarFeature(-0.0117318602278829, 0.7129650712013245, 0.4921196103096008, new HaarRectangle(9, 7, 3, 6, -1), new HaarRectangle(10, 7, 1, 6, 3));
            stageNodes.AddHaarFeature(-0.006176512222737074, 0.2288102954626083, 0.5049701929092407, new HaarRectangle(7, 8, 4, 3, -1), new HaarRectangle(7, 9, 4, 1, 3));
            stageNodes.AddHaarFeature(0.0022457661107182503, 0.4632433950901032, 0.6048725843429565, new HaarRectangle(7, 12, 6, 3, -1), new HaarRectangle(7, 13, 6, 1, 3));
            stageNodes.AddHaarFeature(-0.005191586911678314, 0.6467421054840088, 0.4602192938327789, new HaarRectangle(9, 12, 2, 3, -1), new HaarRectangle(9, 13, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0238278806209564, 0.1482000946998596, 0.5226079225540161, new HaarRectangle(7, 12, 6, 2, -1), new HaarRectangle(9, 12, 2, 2, 3));
            stageNodes.AddHaarFeature(0.0010284580057486892, 0.5135489106178284, 0.3375957012176514, new HaarRectangle(5, 11, 4, 6, -1), new HaarRectangle(5, 14, 4, 3, 2));
            stageNodes.AddHaarFeature(-0.0100788502022624, 0.2740561068058014, 0.5303567051887512, new HaarRectangle(11, 12, 7, 2, -1), new HaarRectangle(11, 13, 7, 1, 2));
            stageNodes.AddHaarFeature(0.002616893034428358, 0.533267080783844, 0.3972454071044922, new HaarRectangle(6, 10, 8, 6, -1), new HaarRectangle(6, 10, 4, 3, 2), new HaarRectangle(10, 13, 4, 3, 2));
            stageNodes.AddHaarFeature(0.000543853675480932, 0.5365604162216187, 0.4063411951065064, new HaarRectangle(11, 10, 3, 4, -1), new HaarRectangle(11, 12, 3, 2, 2));
            stageNodes.AddHaarFeature(0.005351051222532988, 0.4653759002685547, 0.6889045834541321, new HaarRectangle(9, 16, 2, 3, -1), new HaarRectangle(9, 17, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0015274790348485112, 0.5449501276016235, 0.3624723851680756, new HaarRectangle(13, 3, 1, 9, -1), new HaarRectangle(13, 6, 1, 3, 3));
            stageNodes.AddHaarFeature(-0.0806244164705276, 0.1656087040901184, 0.5000287294387817, new HaarRectangle(1, 13, 14, 6, -1), new HaarRectangle(1, 15, 14, 2, 3));
            stageNodes.AddHaarFeature(0.0221920292824507, 0.5132731199264526, 0.2002808004617691, new HaarRectangle(13, 6, 1, 6, -1), new HaarRectangle(13, 9, 1, 3, 2));
            stageNodes.AddHaarFeature(0.007310063112527133, 0.4617947936058044, 0.6366536021232605, new HaarRectangle(0, 4, 3, 8, -1), new HaarRectangle(1, 4, 1, 8, 3));
            stageNodes.AddHaarFeature(-0.006406307220458984, 0.5916250944137573, 0.4867860972881317, new HaarRectangle(18, 0, 2, 18, -1), new HaarRectangle(18, 0, 1, 18, 2));
            stageNodes.AddHaarFeature(-0.0007641504053026439, 0.388840913772583, 0.5315797924995422, new HaarRectangle(2, 3, 6, 2, -1), new HaarRectangle(2, 4, 6, 1, 2));
            stageNodes.AddHaarFeature(0.0007673448999412359, 0.4159064888954163, 0.5605279803276062, new HaarRectangle(9, 0, 8, 6, -1), new HaarRectangle(9, 2, 8, 2, 3));
            stageNodes.AddHaarFeature(0.0006147450185380876, 0.3089022040367127, 0.5120148062705994, new HaarRectangle(6, 6, 1, 6, -1), new HaarRectangle(6, 9, 1, 3, 2));
            stageNodes.AddHaarFeature(-0.005010527092963457, 0.3972199857234955, 0.5207306146621704, new HaarRectangle(14, 8, 6, 3, -1), new HaarRectangle(14, 9, 6, 1, 3));
            stageNodes.AddHaarFeature(-0.008690913207828999, 0.6257408261299133, 0.4608575999736786, new HaarRectangle(0, 0, 2, 18, -1), new HaarRectangle(1, 0, 1, 18, 2));
            stageNodes.AddHaarFeature(-0.016391459852457, 0.2085209935903549, 0.5242266058921814, new HaarRectangle(1, 18, 18, 2, -1), new HaarRectangle(10, 18, 9, 1, 2), new HaarRectangle(1, 19, 9, 1, 2));
            stageNodes.AddHaarFeature(0.00040973909199237823, 0.5222427248954773, 0.3780320882797241, new HaarRectangle(3, 15, 2, 2, -1), new HaarRectangle(3, 16, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.0025242289993911986, 0.5803927183151245, 0.4611890017986298, new HaarRectangle(8, 14, 5, 3, -1), new HaarRectangle(8, 15, 5, 1, 3));
            stageNodes.AddHaarFeature(0.0005094531225040555, 0.4401271939277649, 0.5846015810966492, new HaarRectangle(8, 14, 2, 3, -1), new HaarRectangle(8, 15, 2, 1, 3));
            stageNodes.AddHaarFeature(0.001965641975402832, 0.5322325229644775, 0.4184590876102448, new HaarRectangle(12, 3, 3, 3, -1), new HaarRectangle(13, 3, 1, 3, 3));
            stageNodes.AddHaarFeature(0.0005629889783449471, 0.3741844892501831, 0.5234565734863281, new HaarRectangle(7, 5, 6, 2, -1), new HaarRectangle(9, 5, 2, 2, 3));
            stageNodes.AddHaarFeature(-0.0006794679793529212, 0.4631041884422302, 0.5356478095054626, new HaarRectangle(15, 5, 5, 2, -1), new HaarRectangle(15, 6, 5, 1, 2));
            stageNodes.AddHaarFeature(0.007285634987056256, 0.5044670104980469, 0.2377564013004303, new HaarRectangle(0, 5, 5, 2, -1), new HaarRectangle(0, 6, 5, 1, 2));
            stageNodes.AddHaarFeature(-0.0174594894051552, 0.7289121150970459, 0.5050435066223145, new HaarRectangle(17, 14, 1, 6, -1), new HaarRectangle(17, 17, 1, 3, 2));
            stageNodes.AddHaarFeature(-0.0254217498004436, 0.6667134761810303, 0.4678100049495697, new HaarRectangle(2, 9, 9, 3, -1), new HaarRectangle(5, 9, 3, 3, 3));
            stageNodes.AddHaarFeature(-0.0015647639520466328, 0.4391759037971497, 0.532362699508667, new HaarRectangle(12, 3, 3, 3, -1), new HaarRectangle(13, 3, 1, 3, 3));
            stageNodes.AddHaarFeature(0.0114443600177765, 0.4346440136432648, 0.5680012106895447, new HaarRectangle(0, 0, 4, 18, -1), new HaarRectangle(2, 0, 2, 18, 2));
            stageNodes.AddHaarFeature(-0.0006735255010426044, 0.44771409034729, 0.5296812057495117, new HaarRectangle(17, 6, 1, 3, -1), new HaarRectangle(17, 7, 1, 1, 3));
            stageNodes.AddHaarFeature(0.009319420903921127, 0.4740200042724609, 0.7462607026100159, new HaarRectangle(2, 14, 1, 6, -1), new HaarRectangle(2, 17, 1, 3, 2));
            stageNodes.AddHaarFeature(0.00013328490604180845, 0.536506175994873, 0.475213497877121, new HaarRectangle(19, 8, 1, 2, -1), new HaarRectangle(19, 9, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.007881579920649529, 0.1752219051122665, 0.5015255212783813, new HaarRectangle(5, 3, 3, 3, -1), new HaarRectangle(6, 3, 1, 3, 3));
            stageNodes.AddHaarFeature(-0.005798568017780781, 0.7271236777305603, 0.4896200895309448, new HaarRectangle(9, 16, 2, 3, -1), new HaarRectangle(9, 17, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0003892249951604754, 0.4003908932209015, 0.5344941020011902, new HaarRectangle(2, 6, 1, 3, -1), new HaarRectangle(2, 7, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.0019288610201328993, 0.5605612993240356, 0.4803955852985382, new HaarRectangle(12, 4, 8, 2, -1), new HaarRectangle(16, 4, 4, 1, 2), new HaarRectangle(12, 5, 4, 1, 2));
            stageNodes.AddHaarFeature(0.008421415463089943, 0.4753246903419495, 0.7623608708381653, new HaarRectangle(0, 4, 8, 2, -1), new HaarRectangle(0, 4, 4, 1, 2), new HaarRectangle(4, 5, 4, 1, 2));
            stageNodes.AddHaarFeature(0.008165587671101093, 0.5393261909484863, 0.419164389371872, new HaarRectangle(2, 16, 18, 4, -1), new HaarRectangle(2, 18, 18, 2, 2));
            stageNodes.AddHaarFeature(0.00048280550981871784, 0.4240800142288208, 0.5399821996688843, new HaarRectangle(7, 15, 2, 4, -1), new HaarRectangle(7, 17, 2, 2, 2));
            stageNodes.AddHaarFeature(-0.002718663075938821, 0.4244599938392639, 0.5424923896789551, new HaarRectangle(4, 0, 14, 3, -1), new HaarRectangle(4, 1, 14, 1, 3));
            stageNodes.AddHaarFeature(-0.0125072300434113, 0.5895841717720032, 0.4550411105155945, new HaarRectangle(0, 0, 4, 20, -1), new HaarRectangle(2, 0, 2, 20, 2));
            stageNodes.AddHaarFeature(-0.0242865197360516, 0.2647134959697723, 0.518917977809906, new HaarRectangle(12, 4, 4, 8, -1), new HaarRectangle(14, 4, 2, 4, 2), new HaarRectangle(12, 8, 2, 4, 2));
            stageNodes.AddHaarFeature(-0.0029676330741494894, 0.734768271446228, 0.4749749898910523, new HaarRectangle(6, 7, 2, 2, -1), new HaarRectangle(6, 7, 1, 1, 2), new HaarRectangle(7, 8, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0125289997085929, 0.2756049931049347, 0.5177599787712097, new HaarRectangle(10, 6, 2, 3, -1), new HaarRectangle(10, 7, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0010104000102728603, 0.3510560989379883, 0.5144724249839783, new HaarRectangle(8, 7, 3, 2, -1), new HaarRectangle(8, 8, 3, 1, 2));
            stageNodes.AddHaarFeature(-0.0021348530426621437, 0.5637925863265991, 0.466731995344162, new HaarRectangle(8, 2, 6, 12, -1), new HaarRectangle(8, 8, 6, 6, 2));
            stageNodes.AddHaarFeature(0.0195642597973347, 0.4614573121070862, 0.6137639880180359, new HaarRectangle(4, 0, 11, 12, -1), new HaarRectangle(4, 4, 11, 4, 3));
            stageNodes.AddHaarFeature(-0.0971463471651077, 0.2998378872871399, 0.5193555951118469, new HaarRectangle(14, 9, 6, 11, -1), new HaarRectangle(16, 9, 2, 11, 3));
            stageNodes.AddHaarFeature(0.00450145686045289, 0.5077884793281555, 0.3045755922794342, new HaarRectangle(0, 14, 4, 3, -1), new HaarRectangle(0, 15, 4, 1, 3));
            stageNodes.AddHaarFeature(0.006370697170495987, 0.486101895570755, 0.6887500882148743, new HaarRectangle(9, 10, 2, 3, -1), new HaarRectangle(9, 11, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.009072152897715569, 0.1673395931720734, 0.5017563104629517, new HaarRectangle(5, 11, 3, 2, -1), new HaarRectangle(5, 12, 3, 1, 2));
            stageNodes.AddHaarFeature(-0.005353720858693123, 0.2692756950855255, 0.524263322353363, new HaarRectangle(9, 15, 3, 3, -1), new HaarRectangle(10, 15, 1, 3, 3));
            stageNodes.AddHaarFeature(-0.0109328404068947, 0.7183864116668701, 0.4736028909683228, new HaarRectangle(8, 8, 3, 4, -1), new HaarRectangle(9, 8, 1, 4, 3));
            stageNodes.AddHaarFeature(0.008235607296228409, 0.5223966836929321, 0.2389862984418869, new HaarRectangle(9, 15, 3, 3, -1), new HaarRectangle(10, 15, 1, 3, 3));
            stageNodes.AddHaarFeature(-0.0010038160253316164, 0.5719355940818787, 0.4433943033218384, new HaarRectangle(7, 7, 3, 2, -1), new HaarRectangle(8, 7, 1, 2, 3));
            stageNodes.AddHaarFeature(0.004085912834852934, 0.5472841858863831, 0.4148836135864258, new HaarRectangle(2, 10, 16, 4, -1), new HaarRectangle(10, 10, 8, 2, 2), new HaarRectangle(2, 12, 8, 2, 2));
            stageNodes.AddHaarFeature(0.1548541933298111, 0.4973812103271484, 0.0610615983605385, new HaarRectangle(2, 3, 4, 17, -1), new HaarRectangle(4, 3, 2, 17, 2));
            stageNodes.AddHaarFeature(0.00020897459762636572, 0.4709174036979675, 0.542388916015625, new HaarRectangle(15, 13, 2, 7, -1), new HaarRectangle(15, 13, 1, 7, 2));
            stageNodes.AddHaarFeature(0.0003331699117552489, 0.4089626967906952, 0.5300992131233215, new HaarRectangle(2, 2, 6, 1, -1), new HaarRectangle(5, 2, 3, 1, 2));
            stageNodes.AddHaarFeature(-0.0108134001493454, 0.6104369759559631, 0.4957334101200104, new HaarRectangle(5, 2, 12, 4, -1), new HaarRectangle(9, 2, 4, 4, 3));
            stageNodes.AddHaarFeature(0.0456560105085373, 0.5069689154624939, 0.2866660058498383, new HaarRectangle(6, 0, 8, 12, -1), new HaarRectangle(6, 0, 4, 6, 2), new HaarRectangle(10, 6, 4, 6, 2));
            stageNodes.AddHaarFeature(0.0012569549726322291, 0.484691709280014, 0.631817102432251, new HaarRectangle(13, 7, 2, 2, -1), new HaarRectangle(14, 7, 1, 1, 2), new HaarRectangle(13, 8, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.120150700211525, 0.0605261400341988, 0.4980959892272949, new HaarRectangle(0, 12, 20, 6, -1), new HaarRectangle(0, 14, 20, 2, 3));
            stageNodes.AddHaarFeature(-0.00010533799650147556, 0.5363109707832336, 0.4708042144775391, new HaarRectangle(14, 7, 2, 3, -1), new HaarRectangle(14, 7, 1, 3, 2));
            stageNodes.AddHaarFeature(-0.2070319056510925, 0.059660330414772, 0.497909814119339, new HaarRectangle(0, 8, 9, 12, -1), new HaarRectangle(3, 8, 3, 12, 3));
            stageNodes.AddHaarFeature(0.00012909180077258495, 0.4712977111339569, 0.5377997756004333, new HaarRectangle(3, 0, 16, 2, -1), new HaarRectangle(3, 0, 8, 2, 2));
            stageNodes.AddHaarFeature(0.000388185289921239, 0.4363538026809692, 0.5534191131591797, new HaarRectangle(6, 15, 3, 3, -1), new HaarRectangle(6, 16, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.0029243610333651304, 0.5811185836791992, 0.4825215935707092, new HaarRectangle(8, 15, 6, 3, -1), new HaarRectangle(8, 16, 6, 1, 3));
            stageNodes.AddHaarFeature(0.0008388233254663646, 0.5311700105667114, 0.403813898563385, new HaarRectangle(0, 10, 1, 6, -1), new HaarRectangle(0, 12, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.0019061550265178084, 0.3770701885223389, 0.526001513004303, new HaarRectangle(10, 9, 4, 3, -1), new HaarRectangle(10, 10, 4, 1, 3));
            stageNodes.AddHaarFeature(0.00895143486559391, 0.4766167998313904, 0.7682183980941772, new HaarRectangle(9, 15, 2, 3, -1), new HaarRectangle(9, 16, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0130834598094225, 0.5264462828636169, 0.3062222003936768, new HaarRectangle(5, 7, 10, 1, -1), new HaarRectangle(5, 7, 5, 1, 2));
            stageNodes.AddHaarFeature(-0.2115933001041412, 0.6737198233604431, 0.4695810079574585, new HaarRectangle(4, 0, 12, 19, -1), new HaarRectangle(10, 0, 6, 19, 2));
            stageNodes.AddHaarFeature(0.0031493250280618668, 0.5644835233688354, 0.4386953115463257, new HaarRectangle(0, 6, 20, 6, -1), new HaarRectangle(10, 6, 10, 3, 2), new HaarRectangle(0, 9, 10, 3, 2));
            stageNodes.AddHaarFeature(0.00039754100725986063, 0.4526061117649078, 0.5895630121231079, new HaarRectangle(3, 6, 2, 2, -1), new HaarRectangle(3, 6, 1, 1, 2), new HaarRectangle(4, 7, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0013814480043947697, 0.6070582270622253, 0.4942413866519928, new HaarRectangle(15, 6, 2, 2, -1), new HaarRectangle(16, 6, 1, 1, 2), new HaarRectangle(15, 7, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0005812218878418207, 0.5998213291168213, 0.4508252143859863, new HaarRectangle(3, 6, 2, 2, -1), new HaarRectangle(3, 6, 1, 1, 2), new HaarRectangle(4, 7, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.002390532987192273, 0.420558899641037, 0.5223848223686218, new HaarRectangle(14, 4, 1, 12, -1), new HaarRectangle(14, 10, 1, 6, 2));
            stageNodes.AddHaarFeature(0.0272689294070005, 0.5206447243690491, 0.3563301861286163, new HaarRectangle(2, 5, 16, 10, -1), new HaarRectangle(2, 5, 8, 5, 2), new HaarRectangle(10, 10, 8, 5, 2));
            stageNodes.AddHaarFeature(-0.0037658358924090862, 0.3144704103469849, 0.5218814015388489, new HaarRectangle(9, 17, 3, 2, -1), new HaarRectangle(10, 17, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.0014903489500284195, 0.338019609451294, 0.5124437212944031, new HaarRectangle(1, 4, 2, 2, -1), new HaarRectangle(1, 5, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.0174282304942608, 0.5829960703849792, 0.4919725954532623, new HaarRectangle(5, 0, 15, 5, -1), new HaarRectangle(10, 0, 5, 5, 3));
            stageNodes.AddHaarFeature(-0.0152780301868916, 0.6163144707679749, 0.4617887139320374, new HaarRectangle(0, 0, 15, 5, -1), new HaarRectangle(5, 0, 5, 5, 3));
            stageNodes.AddHaarFeature(0.0319956094026566, 0.5166357159614563, 0.171276405453682, new HaarRectangle(11, 2, 2, 17, -1), new HaarRectangle(11, 2, 1, 17, 2));
            stageNodes.AddHaarFeature(-0.003825671039521694, 0.3408012092113495, 0.5131387710571289, new HaarRectangle(7, 2, 2, 17, -1), new HaarRectangle(8, 2, 1, 17, 2));
            stageNodes.AddHaarFeature(-0.00851864367723465, 0.6105518937110901, 0.4997941851615906, new HaarRectangle(15, 11, 2, 9, -1), new HaarRectangle(15, 11, 1, 9, 2));
            stageNodes.AddHaarFeature(0.0009064162150025368, 0.4327270984649658, 0.5582311153411865, new HaarRectangle(3, 11, 2, 9, -1), new HaarRectangle(4, 11, 1, 9, 2));
            stageNodes.AddHaarFeature(0.0103448498994112, 0.4855653047561646, 0.5452420115470886, new HaarRectangle(5, 16, 14, 4, -1), new HaarRectangle(5, 16, 7, 4, 2));
            stageNodeCounts.Add(stageNodes.Count - index); index = stageNodes.Count;

            stageThresholds.Add(79.24907684326172);
            stageNodes.AddHaarFeature(0.007898182608187199, 0.333252489566803, 0.5946462154388428, new HaarRectangle(1, 4, 18, 1, -1), new HaarRectangle(7, 4, 6, 1, 3));
            stageNodes.AddHaarFeature(0.0016170160379260778, 0.3490641117095947, 0.5577868819236755, new HaarRectangle(13, 7, 6, 4, -1), new HaarRectangle(16, 7, 3, 2, 2), new HaarRectangle(13, 9, 3, 2, 2));
            stageNodes.AddHaarFeature(-0.0005544974119402468, 0.5542566180229187, 0.3291530013084412, new HaarRectangle(9, 8, 2, 12, -1), new HaarRectangle(9, 12, 2, 4, 3));
            stageNodes.AddHaarFeature(0.0015428980113938453, 0.3612579107284546, 0.5545979142189026, new HaarRectangle(12, 1, 6, 6, -1), new HaarRectangle(12, 3, 6, 2, 3));
            stageNodes.AddHaarFeature(-0.0010329450014978647, 0.3530139029026032, 0.5576140284538269, new HaarRectangle(5, 2, 6, 6, -1), new HaarRectangle(5, 2, 3, 3, 2), new HaarRectangle(8, 5, 3, 3, 2));
            stageNodes.AddHaarFeature(0.0007769815856590867, 0.3916778862476349, 0.5645321011543274, new HaarRectangle(9, 16, 6, 4, -1), new HaarRectangle(12, 16, 3, 2, 2), new HaarRectangle(9, 18, 3, 2, 2));
            stageNodes.AddHaarFeature(0.143203005194664, 0.4667482078075409, 0.7023633122444153, new HaarRectangle(1, 2, 18, 3, -1), new HaarRectangle(7, 2, 6, 3, 3));
            stageNodes.AddHaarFeature(-0.007386649027466774, 0.3073684871196747, 0.5289257764816284, new HaarRectangle(7, 4, 9, 10, -1), new HaarRectangle(7, 9, 9, 5, 2));
            stageNodes.AddHaarFeature(-0.0006293674232438207, 0.562211811542511, 0.4037049114704132, new HaarRectangle(5, 9, 4, 4, -1), new HaarRectangle(7, 9, 2, 4, 2));
            stageNodes.AddHaarFeature(0.000788935285527259, 0.5267661213874817, 0.3557874858379364, new HaarRectangle(11, 10, 3, 6, -1), new HaarRectangle(11, 13, 3, 3, 2));
            stageNodes.AddHaarFeature(-0.0122280502691865, 0.6668320894241333, 0.4625549912452698, new HaarRectangle(7, 11, 5, 3, -1), new HaarRectangle(7, 12, 5, 1, 3));
            stageNodes.AddHaarFeature(0.0035420239437371492, 0.5521438121795654, 0.3869673013687134, new HaarRectangle(7, 11, 6, 6, -1), new HaarRectangle(10, 11, 3, 3, 2), new HaarRectangle(7, 14, 3, 3, 2));
            stageNodes.AddHaarFeature(-0.0010585320414975286, 0.3628678023815155, 0.5320926904678345, new HaarRectangle(0, 0, 10, 9, -1), new HaarRectangle(0, 3, 10, 3, 3));
            stageNodes.AddHaarFeature(0.000014935660146875309, 0.4632444977760315, 0.5363323092460632, new HaarRectangle(13, 14, 1, 6, -1), new HaarRectangle(13, 16, 1, 2, 3));
            stageNodes.AddHaarFeature(0.005253770854324102, 0.5132231712341309, 0.3265708982944489, new HaarRectangle(0, 2, 3, 6, -1), new HaarRectangle(0, 4, 3, 2, 3));
            stageNodes.AddHaarFeature(-0.008233802393078804, 0.6693689823150635, 0.4774140119552612, new HaarRectangle(8, 14, 4, 3, -1), new HaarRectangle(8, 15, 4, 1, 3));
            stageNodes.AddHaarFeature(0.00002186681012972258, 0.405386209487915, 0.5457931160926819, new HaarRectangle(6, 14, 1, 6, -1), new HaarRectangle(6, 16, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.003815022995695472, 0.645499587059021, 0.4793178141117096, new HaarRectangle(9, 15, 2, 3, -1), new HaarRectangle(9, 16, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0011105879675596952, 0.5270407199859619, 0.3529678881168366, new HaarRectangle(6, 4, 3, 3, -1), new HaarRectangle(7, 4, 1, 3, 3));
            stageNodes.AddHaarFeature(-0.005770768970251083, 0.3803547024726868, 0.5352957844734192, new HaarRectangle(9, 0, 11, 3, -1), new HaarRectangle(9, 1, 11, 1, 3));
            stageNodes.AddHaarFeature(-0.003015833906829357, 0.533940315246582, 0.3887133002281189, new HaarRectangle(0, 6, 20, 3, -1), new HaarRectangle(0, 7, 20, 1, 3));
            stageNodes.AddHaarFeature(-0.0008545368909835817, 0.3564616143703461, 0.5273603796958923, new HaarRectangle(10, 1, 1, 2, -1), new HaarRectangle(10, 2, 1, 1, 2));
            stageNodes.AddHaarFeature(0.0110505102202296, 0.4671907126903534, 0.6849737763404846, new HaarRectangle(9, 6, 2, 6, -1), new HaarRectangle(10, 6, 1, 6, 2));
            stageNodes.AddHaarFeature(0.0426058396697044, 0.51514732837677, 0.0702200904488564, new HaarRectangle(5, 8, 12, 1, -1), new HaarRectangle(9, 8, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.0030781750101596117, 0.3041661083698273, 0.5152602195739746, new HaarRectangle(3, 8, 12, 1, -1), new HaarRectangle(7, 8, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.005481572821736336, 0.6430295705795288, 0.4897229969501495, new HaarRectangle(9, 7, 3, 5, -1), new HaarRectangle(10, 7, 1, 5, 3));
            stageNodes.AddHaarFeature(0.003188186092302203, 0.5307493209838867, 0.3826209902763367, new HaarRectangle(3, 9, 6, 2, -1), new HaarRectangle(6, 9, 3, 2, 2));
            stageNodes.AddHaarFeature(0.00035947180003859103, 0.4650047123432159, 0.5421904921531677, new HaarRectangle(12, 9, 3, 3, -1), new HaarRectangle(12, 10, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.004070503171533346, 0.2849679887294769, 0.5079116225242615, new HaarRectangle(7, 0, 6, 1, -1), new HaarRectangle(9, 0, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0145941702648997, 0.2971645891666412, 0.5128461718559265, new HaarRectangle(12, 9, 3, 3, -1), new HaarRectangle(12, 10, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.00011947689927183092, 0.563109815120697, 0.4343082010746002, new HaarRectangle(7, 10, 2, 1, -1), new HaarRectangle(8, 10, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0006934464909136295, 0.4403578042984009, 0.5359959006309509, new HaarRectangle(6, 4, 9, 13, -1), new HaarRectangle(9, 4, 3, 13, 3));
            stageNodes.AddHaarFeature(0.000014834799912932796, 0.3421008884906769, 0.5164697766304016, new HaarRectangle(6, 8, 4, 2, -1), new HaarRectangle(6, 9, 4, 1, 2));
            stageNodes.AddHaarFeature(0.009029698558151722, 0.4639343023300171, 0.6114075183868408, new HaarRectangle(16, 2, 4, 6, -1), new HaarRectangle(16, 2, 2, 6, 2));
            stageNodes.AddHaarFeature(-0.008064081892371178, 0.2820158898830414, 0.5075494050979614, new HaarRectangle(0, 17, 6, 3, -1), new HaarRectangle(0, 18, 6, 1, 3));
            stageNodes.AddHaarFeature(0.0260621197521687, 0.5208905935287476, 0.2688778042793274, new HaarRectangle(10, 10, 3, 10, -1), new HaarRectangle(10, 15, 3, 5, 2));
            stageNodes.AddHaarFeature(0.0173146594315767, 0.4663713872432709, 0.6738539934158325, new HaarRectangle(8, 7, 3, 5, -1), new HaarRectangle(9, 7, 1, 5, 3));
            stageNodes.AddHaarFeature(0.0226666405797005, 0.5209349989891052, 0.2212723940610886, new HaarRectangle(10, 4, 4, 3, -1), new HaarRectangle(10, 4, 2, 3, 2));
            stageNodes.AddHaarFeature(-0.002196592977270484, 0.6063101291656494, 0.4538190066814423, new HaarRectangle(8, 4, 3, 8, -1), new HaarRectangle(9, 4, 1, 8, 3));
            stageNodes.AddHaarFeature(-0.009528247639536858, 0.4635204970836639, 0.5247430801391602, new HaarRectangle(6, 6, 9, 13, -1), new HaarRectangle(9, 6, 3, 13, 3));
            stageNodes.AddHaarFeature(0.00809436198323965, 0.5289440155029297, 0.3913882076740265, new HaarRectangle(6, 0, 8, 12, -1), new HaarRectangle(6, 0, 4, 6, 2), new HaarRectangle(10, 6, 4, 6, 2));
            stageNodes.AddHaarFeature(-0.0728773325681686, 0.7752001881599426, 0.4990234971046448, new HaarRectangle(14, 2, 6, 8, -1), new HaarRectangle(16, 2, 2, 8, 3));
            stageNodes.AddHaarFeature(-0.006900952197611332, 0.2428039014339447, 0.5048090219497681, new HaarRectangle(6, 0, 3, 6, -1), new HaarRectangle(7, 0, 1, 6, 3));
            stageNodes.AddHaarFeature(-0.0113082397729158, 0.5734364986419678, 0.4842376112937927, new HaarRectangle(14, 2, 6, 8, -1), new HaarRectangle(16, 2, 2, 8, 3));
            stageNodes.AddHaarFeature(0.0596132017672062, 0.5029836297035217, 0.2524977028369904, new HaarRectangle(0, 5, 6, 6, -1), new HaarRectangle(0, 8, 6, 3, 2));
            stageNodes.AddHaarFeature(-0.0028624620754271746, 0.6073045134544373, 0.4898459911346436, new HaarRectangle(9, 12, 6, 2, -1), new HaarRectangle(12, 12, 3, 1, 2), new HaarRectangle(9, 13, 3, 1, 2));
            stageNodes.AddHaarFeature(0.00447814492508769, 0.5015289187431335, 0.2220316976308823, new HaarRectangle(8, 17, 3, 2, -1), new HaarRectangle(9, 17, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.0017513240454718473, 0.6614428758621216, 0.4933868944644928, new HaarRectangle(11, 6, 2, 2, -1), new HaarRectangle(12, 6, 1, 1, 2), new HaarRectangle(11, 7, 1, 1, 2));
            stageNodes.AddHaarFeature(0.0401634201407433, 0.5180878043174744, 0.3741044998168945, new HaarRectangle(1, 9, 18, 2, -1), new HaarRectangle(7, 9, 6, 2, 3));
            stageNodes.AddHaarFeature(0.0003476894926279783, 0.4720416963100433, 0.5818032026290894, new HaarRectangle(11, 6, 2, 2, -1), new HaarRectangle(12, 6, 1, 1, 2), new HaarRectangle(11, 7, 1, 1, 2));
            stageNodes.AddHaarFeature(0.00265516503714025, 0.3805010914802551, 0.5221335887908936, new HaarRectangle(3, 4, 12, 8, -1), new HaarRectangle(7, 4, 4, 8, 3));
            stageNodes.AddHaarFeature(-0.008770627900958061, 0.294416606426239, 0.5231295228004456, new HaarRectangle(13, 11, 5, 3, -1), new HaarRectangle(13, 12, 5, 1, 3));
            stageNodes.AddHaarFeature(-0.005512209143489599, 0.7346177101135254, 0.4722816944122315, new HaarRectangle(9, 10, 2, 3, -1), new HaarRectangle(9, 11, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0006867204210720956, 0.5452876091003418, 0.424241304397583, new HaarRectangle(14, 7, 2, 3, -1), new HaarRectangle(14, 7, 1, 3, 2));
            stageNodes.AddHaarFeature(0.0005601966986432672, 0.439886212348938, 0.5601285099983215, new HaarRectangle(5, 4, 1, 3, -1), new HaarRectangle(5, 5, 1, 1, 3));
            stageNodes.AddHaarFeature(0.0024143769405782223, 0.4741686880588532, 0.6136621832847595, new HaarRectangle(13, 4, 2, 3, -1), new HaarRectangle(13, 5, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.00156809005420655, 0.604455292224884, 0.4516409933567047, new HaarRectangle(5, 4, 2, 3, -1), new HaarRectangle(5, 5, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.003682749113067985, 0.2452459037303925, 0.5294982194900513, new HaarRectangle(9, 8, 2, 3, -1), new HaarRectangle(9, 9, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.000294091907562688, 0.3732838034629822, 0.5251451134681702, new HaarRectangle(8, 9, 2, 2, -1), new HaarRectangle(8, 10, 2, 1, 2));
            stageNodes.AddHaarFeature(0.00042847759323194623, 0.5498809814453125, 0.4065535068511963, new HaarRectangle(15, 14, 1, 4, -1), new HaarRectangle(15, 16, 1, 2, 2));
            stageNodes.AddHaarFeature(-0.004881707020103931, 0.2139908969402313, 0.4999957084655762, new HaarRectangle(3, 12, 2, 2, -1), new HaarRectangle(3, 13, 2, 1, 2));
            stageNodes.AddHaarFeature(0.00027272020815871656, 0.465028703212738, 0.581342875957489, new HaarRectangle(12, 15, 2, 2, -1), new HaarRectangle(13, 15, 1, 1, 2), new HaarRectangle(12, 16, 1, 1, 2));
            stageNodes.AddHaarFeature(0.00020947199664078653, 0.4387486875057221, 0.5572792887687683, new HaarRectangle(9, 13, 2, 2, -1), new HaarRectangle(9, 14, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0485011897981167, 0.5244972705841064, 0.3212889134883881, new HaarRectangle(4, 11, 14, 9, -1), new HaarRectangle(4, 14, 14, 3, 3));
            stageNodes.AddHaarFeature(-0.004516641143709421, 0.605681300163269, 0.4545882046222687, new HaarRectangle(7, 13, 4, 3, -1), new HaarRectangle(7, 14, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.0122916800901294, 0.2040929049253464, 0.5152214169502258, new HaarRectangle(15, 14, 1, 4, -1), new HaarRectangle(15, 16, 1, 2, 2));
            stageNodes.AddHaarFeature(0.00048549679922871297, 0.5237604975700378, 0.3739503026008606, new HaarRectangle(4, 14, 1, 4, -1), new HaarRectangle(4, 16, 1, 2, 2));
            stageNodes.AddHaarFeature(0.0305560491979122, 0.4960533976554871, 0.5938246250152588, new HaarRectangle(14, 0, 6, 13, -1), new HaarRectangle(16, 0, 2, 13, 3));
            stageNodes.AddHaarFeature(-0.00015105320198927075, 0.5351303815841675, 0.4145204126834869, new HaarRectangle(4, 1, 2, 12, -1), new HaarRectangle(4, 1, 1, 6, 2), new HaarRectangle(5, 7, 1, 6, 2));
            stageNodes.AddHaarFeature(0.0024937440175563097, 0.4693366885185242, 0.5514941215515137, new HaarRectangle(11, 14, 6, 6, -1), new HaarRectangle(14, 14, 3, 3, 2), new HaarRectangle(11, 17, 3, 3, 2));
            stageNodes.AddHaarFeature(-0.012382130138576, 0.6791396737098694, 0.4681667983531952, new HaarRectangle(3, 14, 6, 6, -1), new HaarRectangle(3, 14, 3, 3, 2), new HaarRectangle(6, 17, 3, 3, 2));
            stageNodes.AddHaarFeature(-0.005133346188813448, 0.3608739078044891, 0.5229160189628601, new HaarRectangle(14, 17, 3, 2, -1), new HaarRectangle(14, 18, 3, 1, 2));
            stageNodes.AddHaarFeature(0.0005191927775740623, 0.5300073027610779, 0.3633613884449005, new HaarRectangle(3, 17, 3, 2, -1), new HaarRectangle(3, 18, 3, 1, 2));
            stageNodes.AddHaarFeature(0.1506042033433914, 0.515731692314148, 0.2211782038211823, new HaarRectangle(14, 0, 6, 13, -1), new HaarRectangle(16, 0, 2, 13, 3));
            stageNodes.AddHaarFeature(0.007714414969086646, 0.4410496950149536, 0.5776609182357788, new HaarRectangle(0, 0, 6, 13, -1), new HaarRectangle(2, 0, 2, 13, 3));
            stageNodes.AddHaarFeature(0.009444352239370346, 0.5401855111122131, 0.375665009021759, new HaarRectangle(10, 10, 7, 6, -1), new HaarRectangle(10, 12, 7, 2, 3));
            stageNodes.AddHaarFeature(0.00025006249779835343, 0.4368270933628082, 0.5607374906539917, new HaarRectangle(6, 15, 2, 2, -1), new HaarRectangle(6, 15, 1, 1, 2), new HaarRectangle(7, 16, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.003307715058326721, 0.4244799017906189, 0.551823079586029, new HaarRectangle(6, 11, 8, 6, -1), new HaarRectangle(10, 11, 4, 3, 2), new HaarRectangle(6, 14, 4, 3, 2));
            stageNodes.AddHaarFeature(0.0007404891075566411, 0.4496962130069733, 0.5900576710700989, new HaarRectangle(7, 6, 2, 2, -1), new HaarRectangle(7, 6, 1, 1, 2), new HaarRectangle(8, 7, 1, 1, 2));
            stageNodes.AddHaarFeature(0.0440920516848564, 0.5293493270874023, 0.3156355023384094, new HaarRectangle(2, 2, 16, 6, -1), new HaarRectangle(10, 2, 8, 3, 2), new HaarRectangle(2, 5, 8, 3, 2));
            stageNodes.AddHaarFeature(0.0033639909233897924, 0.4483296871185303, 0.5848662257194519, new HaarRectangle(5, 4, 3, 3, -1), new HaarRectangle(5, 5, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.003976007923483849, 0.4559507071971893, 0.5483639240264893, new HaarRectangle(11, 7, 3, 10, -1), new HaarRectangle(11, 12, 3, 5, 2));
            stageNodes.AddHaarFeature(0.0027716930489987135, 0.534178614616394, 0.3792484104633331, new HaarRectangle(6, 7, 3, 10, -1), new HaarRectangle(6, 12, 3, 5, 2));
            stageNodes.AddHaarFeature(-0.00024123019829858097, 0.5667188763618469, 0.4576973021030426, new HaarRectangle(10, 7, 3, 2, -1), new HaarRectangle(11, 7, 1, 2, 3));
            stageNodes.AddHaarFeature(0.0004942566738463938, 0.4421244859695435, 0.5628787279129028, new HaarRectangle(8, 12, 4, 2, -1), new HaarRectangle(8, 13, 4, 1, 2));
            stageNodes.AddHaarFeature(-0.0003887646889779717, 0.4288370907306671, 0.5391063094139099, new HaarRectangle(10, 1, 1, 3, -1), new HaarRectangle(10, 2, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.0500488989055157, 0.6899513006210327, 0.4703742861747742, new HaarRectangle(1, 2, 4, 18, -1), new HaarRectangle(1, 2, 2, 9, 2), new HaarRectangle(3, 11, 2, 9, 2));
            stageNodes.AddHaarFeature(-0.0366354808211327, 0.2217779010534287, 0.5191826224327087, new HaarRectangle(12, 4, 4, 12, -1), new HaarRectangle(12, 10, 4, 6, 2));
            stageNodes.AddHaarFeature(0.0024273579474538565, 0.5136224031448364, 0.3497397899627686, new HaarRectangle(0, 0, 1, 6, -1), new HaarRectangle(0, 2, 1, 2, 3));
            stageNodes.AddHaarFeature(0.001955803018063307, 0.4826192855834961, 0.640838086605072, new HaarRectangle(9, 11, 2, 3, -1), new HaarRectangle(9, 12, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0017494610510766506, 0.3922835886478424, 0.5272685289382935, new HaarRectangle(8, 7, 4, 3, -1), new HaarRectangle(8, 8, 4, 1, 3));
            stageNodes.AddHaarFeature(0.0139550799503922, 0.507820188999176, 0.8416504859924316, new HaarRectangle(10, 7, 3, 2, -1), new HaarRectangle(11, 7, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.00021896739781368524, 0.5520489811897278, 0.4314234852790833, new HaarRectangle(7, 7, 3, 2, -1), new HaarRectangle(8, 7, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.0015131309628486631, 0.3934605121612549, 0.5382571220397949, new HaarRectangle(9, 4, 6, 1, -1), new HaarRectangle(11, 4, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.004362280014902353, 0.7370628714561462, 0.4736475944519043, new HaarRectangle(8, 7, 2, 3, -1), new HaarRectangle(9, 7, 1, 3, 2));
            stageNodes.AddHaarFeature(0.0651605874300003, 0.5159279704093933, 0.328159511089325, new HaarRectangle(12, 7, 8, 6, -1), new HaarRectangle(16, 7, 4, 3, 2), new HaarRectangle(12, 10, 4, 3, 2));
            stageNodes.AddHaarFeature(-0.0023567399475723505, 0.3672826886177063, 0.5172886252403259, new HaarRectangle(0, 7, 8, 6, -1), new HaarRectangle(0, 7, 4, 3, 2), new HaarRectangle(4, 10, 4, 3, 2));
            stageNodes.AddHaarFeature(0.0151466596871614, 0.5031493902206421, 0.6687604188919067, new HaarRectangle(18, 2, 2, 10, -1), new HaarRectangle(19, 2, 1, 5, 2), new HaarRectangle(18, 7, 1, 5, 2));
            stageNodes.AddHaarFeature(-0.0228509604930878, 0.676751971244812, 0.4709596931934357, new HaarRectangle(0, 2, 6, 4, -1), new HaarRectangle(3, 2, 3, 4, 2));
            stageNodes.AddHaarFeature(0.004886765033006668, 0.5257998108863831, 0.4059878885746002, new HaarRectangle(9, 4, 6, 1, -1), new HaarRectangle(11, 4, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0017619599821045997, 0.4696272909641266, 0.6688278913497925, new HaarRectangle(7, 15, 2, 2, -1), new HaarRectangle(7, 15, 1, 1, 2), new HaarRectangle(8, 16, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0012942519970238209, 0.4320712983608246, 0.5344281792640686, new HaarRectangle(11, 13, 1, 6, -1), new HaarRectangle(11, 16, 1, 3, 2));
            stageNodes.AddHaarFeature(0.0109299495816231, 0.4997706115245819, 0.1637486070394516, new HaarRectangle(8, 13, 1, 6, -1), new HaarRectangle(8, 16, 1, 3, 2));
            stageNodes.AddHaarFeature(0.000029958489903947335, 0.4282417893409729, 0.5633224248886108, new HaarRectangle(14, 3, 2, 1, -1), new HaarRectangle(14, 3, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0065884361974895, 0.677212119102478, 0.4700526893138886, new HaarRectangle(8, 15, 2, 3, -1), new HaarRectangle(8, 16, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0032527779694646597, 0.531339704990387, 0.4536148905754089, new HaarRectangle(12, 15, 7, 4, -1), new HaarRectangle(12, 17, 7, 2, 2));
            stageNodes.AddHaarFeature(-0.00404357397928834, 0.5660061836242676, 0.4413388967514038, new HaarRectangle(4, 14, 12, 3, -1), new HaarRectangle(4, 15, 12, 1, 3));
            stageNodes.AddHaarFeature(-0.0012523540062829852, 0.3731913864612579, 0.5356451869010925, new HaarRectangle(10, 3, 3, 2, -1), new HaarRectangle(11, 3, 1, 2, 3));
            stageNodes.AddHaarFeature(0.00019246719602961093, 0.5189986228942871, 0.3738811016082764, new HaarRectangle(4, 12, 2, 2, -1), new HaarRectangle(4, 13, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.038589671254158, 0.2956373989582062, 0.51888108253479, new HaarRectangle(10, 11, 4, 6, -1), new HaarRectangle(10, 14, 4, 3, 2));
            stageNodes.AddHaarFeature(0.0001548987056594342, 0.4347135126590729, 0.5509533286094666, new HaarRectangle(7, 13, 2, 2, -1), new HaarRectangle(7, 13, 1, 1, 2), new HaarRectangle(8, 14, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0337638482451439, 0.3230330049991608, 0.5195475816726685, new HaarRectangle(4, 11, 14, 4, -1), new HaarRectangle(11, 11, 7, 2, 2), new HaarRectangle(4, 13, 7, 2, 2));
            stageNodes.AddHaarFeature(-0.008265706710517406, 0.5975489020347595, 0.4552114009857178, new HaarRectangle(1, 18, 18, 2, -1), new HaarRectangle(7, 18, 6, 2, 3));
            stageNodes.AddHaarFeature(0.000014481440302915873, 0.4745678007602692, 0.5497426986694336, new HaarRectangle(11, 18, 2, 2, -1), new HaarRectangle(12, 18, 1, 1, 2), new HaarRectangle(11, 19, 1, 1, 2));
            stageNodes.AddHaarFeature(0.000014951299817766994, 0.4324473142623901, 0.5480644106864929, new HaarRectangle(7, 18, 2, 2, -1), new HaarRectangle(7, 18, 1, 1, 2), new HaarRectangle(8, 19, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.018741799518466, 0.1580052971839905, 0.517853319644928, new HaarRectangle(12, 18, 8, 2, -1), new HaarRectangle(12, 19, 8, 1, 2));
            stageNodes.AddHaarFeature(0.001757223973982036, 0.4517636895179749, 0.5773764252662659, new HaarRectangle(7, 14, 6, 2, -1), new HaarRectangle(7, 15, 6, 1, 2));
            stageNodes.AddHaarFeature(-0.0031391119118779898, 0.4149647951126099, 0.5460842251777649, new HaarRectangle(8, 12, 4, 8, -1), new HaarRectangle(10, 12, 2, 4, 2), new HaarRectangle(8, 16, 2, 4, 2));
            stageNodes.AddHaarFeature(0.00006665677938144654, 0.4039090871810913, 0.5293084979057312, new HaarRectangle(4, 9, 3, 3, -1), new HaarRectangle(4, 10, 3, 1, 3));
            stageNodes.AddHaarFeature(0.006774342153221369, 0.4767651855945587, 0.612195611000061, new HaarRectangle(7, 10, 6, 2, -1), new HaarRectangle(9, 10, 2, 2, 3));
            stageNodes.AddHaarFeature(-0.007386816199868917, 0.3586258888244629, 0.5187280774116516, new HaarRectangle(5, 0, 4, 15, -1), new HaarRectangle(7, 0, 2, 15, 2));
            stageNodes.AddHaarFeature(0.0140409301966429, 0.4712139964103699, 0.5576155781745911, new HaarRectangle(8, 6, 12, 14, -1), new HaarRectangle(12, 6, 4, 14, 3));
            stageNodes.AddHaarFeature(-0.005525832995772362, 0.2661027014255524, 0.5039281249046326, new HaarRectangle(5, 16, 3, 3, -1), new HaarRectangle(5, 17, 3, 1, 3));
            stageNodes.AddHaarFeature(0.3868423998355866, 0.5144339799880981, 0.2525899112224579, new HaarRectangle(8, 1, 12, 19, -1), new HaarRectangle(12, 1, 4, 19, 3));
            stageNodes.AddHaarFeature(0.0001145924034062773, 0.4284994900226593, 0.5423371195793152, new HaarRectangle(3, 0, 3, 2, -1), new HaarRectangle(3, 1, 3, 1, 2));
            stageNodes.AddHaarFeature(-0.0184675697237253, 0.3885835111141205, 0.5213062167167664, new HaarRectangle(10, 12, 4, 5, -1), new HaarRectangle(10, 12, 2, 5, 2));
            stageNodes.AddHaarFeature(-0.0004590701137203723, 0.541256308555603, 0.4235909879207611, new HaarRectangle(6, 12, 4, 5, -1), new HaarRectangle(8, 12, 2, 5, 2));
            stageNodes.AddHaarFeature(0.0012527540093287826, 0.4899305105209351, 0.6624091267585754, new HaarRectangle(11, 11, 2, 2, -1), new HaarRectangle(12, 11, 1, 1, 2), new HaarRectangle(11, 12, 1, 1, 2));
            stageNodes.AddHaarFeature(0.001491060946136713, 0.5286778211593628, 0.4040051996707916, new HaarRectangle(0, 2, 3, 6, -1), new HaarRectangle(0, 4, 3, 2, 3));
            stageNodes.AddHaarFeature(-0.0007543556275777519, 0.6032990217208862, 0.4795120060443878, new HaarRectangle(11, 11, 2, 2, -1), new HaarRectangle(12, 11, 1, 1, 2), new HaarRectangle(11, 12, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0069478838704526424, 0.408440113067627, 0.5373504161834717, new HaarRectangle(7, 6, 4, 10, -1), new HaarRectangle(7, 11, 4, 5, 2));
            stageNodes.AddHaarFeature(0.0002809292054735124, 0.4846062958240509, 0.5759382247924805, new HaarRectangle(11, 11, 2, 2, -1), new HaarRectangle(12, 11, 1, 1, 2), new HaarRectangle(11, 12, 1, 1, 2));
            stageNodes.AddHaarFeature(0.0009607371757738293, 0.5164741277694702, 0.3554979860782623, new HaarRectangle(2, 13, 5, 2, -1), new HaarRectangle(2, 14, 5, 1, 2));
            stageNodes.AddHaarFeature(-0.0002688392996788025, 0.5677582025527954, 0.4731765985488892, new HaarRectangle(11, 11, 2, 2, -1), new HaarRectangle(12, 11, 1, 1, 2), new HaarRectangle(11, 12, 1, 1, 2));
            stageNodes.AddHaarFeature(0.002159937052056194, 0.4731487035751343, 0.7070567011833191, new HaarRectangle(7, 11, 2, 2, -1), new HaarRectangle(7, 11, 1, 1, 2), new HaarRectangle(8, 12, 1, 1, 2));
            stageNodes.AddHaarFeature(0.005623530130833387, 0.5240243077278137, 0.2781791985034943, new HaarRectangle(14, 13, 3, 3, -1), new HaarRectangle(14, 14, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.005024399142712355, 0.2837013900279999, 0.5062304139137268, new HaarRectangle(3, 13, 3, 3, -1), new HaarRectangle(3, 14, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.009761163964867592, 0.7400717735290527, 0.4934569001197815, new HaarRectangle(9, 14, 2, 3, -1), new HaarRectangle(9, 15, 2, 1, 3));
            stageNodes.AddHaarFeature(0.004151510074734688, 0.5119131207466125, 0.3407008051872253, new HaarRectangle(8, 7, 3, 3, -1), new HaarRectangle(8, 8, 3, 1, 3));
            stageNodes.AddHaarFeature(0.006246508099138737, 0.4923788011074066, 0.6579058766365051, new HaarRectangle(13, 5, 3, 3, -1), new HaarRectangle(13, 6, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.007059747818857431, 0.2434711009263992, 0.503284215927124, new HaarRectangle(0, 9, 5, 3, -1), new HaarRectangle(0, 10, 5, 1, 3));
            stageNodes.AddHaarFeature(-0.0020587709732353687, 0.590031087398529, 0.469508707523346, new HaarRectangle(13, 5, 3, 3, -1), new HaarRectangle(13, 6, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.0024146060459315777, 0.3647317886352539, 0.5189201831817627, new HaarRectangle(9, 12, 2, 8, -1), new HaarRectangle(9, 12, 1, 4, 2), new HaarRectangle(10, 16, 1, 4, 2));
            stageNodes.AddHaarFeature(-0.0014817609917372463, 0.6034948229789734, 0.4940128028392792, new HaarRectangle(11, 7, 2, 2, -1), new HaarRectangle(12, 7, 1, 1, 2), new HaarRectangle(11, 8, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0063016400672495365, 0.5818989872932434, 0.4560427963733673, new HaarRectangle(0, 16, 6, 4, -1), new HaarRectangle(3, 16, 3, 4, 2));
            stageNodes.AddHaarFeature(0.00347634288482368, 0.5217475891113281, 0.3483993113040924, new HaarRectangle(10, 6, 2, 3, -1), new HaarRectangle(10, 7, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0222508702427149, 0.2360700070858002, 0.5032082796096802, new HaarRectangle(9, 5, 2, 6, -1), new HaarRectangle(9, 7, 2, 2, 3));
            stageNodes.AddHaarFeature(-0.030612550675869, 0.6499186754226685, 0.4914919137954712, new HaarRectangle(12, 15, 8, 4, -1), new HaarRectangle(12, 15, 4, 4, 2));
            stageNodes.AddHaarFeature(0.013057479634881, 0.4413323104381561, 0.5683764219284058, new HaarRectangle(0, 14, 8, 6, -1), new HaarRectangle(4, 14, 4, 6, 2));
            stageNodes.AddHaarFeature(-0.0006009574281051755, 0.4359731078147888, 0.5333483219146729, new HaarRectangle(9, 0, 3, 2, -1), new HaarRectangle(10, 0, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.0004151425091549754, 0.550406277179718, 0.4326060116291046, new HaarRectangle(4, 15, 4, 2, -1), new HaarRectangle(6, 15, 2, 2, 2));
            stageNodes.AddHaarFeature(-0.013776290230453, 0.4064112901687622, 0.5201548933982849, new HaarRectangle(12, 7, 3, 13, -1), new HaarRectangle(13, 7, 1, 13, 3));
            stageNodes.AddHaarFeature(-0.0322965085506439, 0.0473519712686539, 0.4977194964885712, new HaarRectangle(5, 7, 3, 13, -1), new HaarRectangle(6, 7, 1, 13, 3));
            stageNodes.AddHaarFeature(0.0535569787025452, 0.4881733059883118, 0.666693925857544, new HaarRectangle(9, 6, 3, 9, -1), new HaarRectangle(9, 9, 3, 3, 3));
            stageNodes.AddHaarFeature(0.008188954554498196, 0.5400037169456482, 0.4240820109844208, new HaarRectangle(4, 4, 7, 12, -1), new HaarRectangle(4, 10, 7, 6, 2));
            stageNodes.AddHaarFeature(0.0002105532039422542, 0.4802047908306122, 0.5563852787017822, new HaarRectangle(12, 12, 2, 2, -1), new HaarRectangle(13, 12, 1, 1, 2), new HaarRectangle(12, 13, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.00243827304802835, 0.7387793064117432, 0.4773685038089752, new HaarRectangle(6, 12, 2, 2, -1), new HaarRectangle(6, 12, 1, 1, 2), new HaarRectangle(7, 13, 1, 1, 2));
            stageNodes.AddHaarFeature(0.003283557016402483, 0.5288546085357666, 0.3171291947364807, new HaarRectangle(8, 9, 4, 2, -1), new HaarRectangle(10, 9, 2, 1, 2), new HaarRectangle(8, 10, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0023729570675641294, 0.4750812947750092, 0.7060170769691467, new HaarRectangle(3, 6, 2, 2, -1), new HaarRectangle(3, 6, 1, 1, 2), new HaarRectangle(4, 7, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0014541699783876538, 0.3811730146408081, 0.533073902130127, new HaarRectangle(16, 6, 3, 2, -1), new HaarRectangle(16, 7, 3, 1, 2));
            stageNodeCounts.Add(stageNodes.Count - index); index = stageNodes.Count;

            stageThresholds.Add(87.69602966308594);
            stageNodes.AddHaarFeature(0.0557552389800549, 0.4019156992435455, 0.6806036829948425, new HaarRectangle(0, 7, 19, 4, -1), new HaarRectangle(0, 9, 19, 2, 2));
            stageNodes.AddHaarFeature(0.002473024884238839, 0.3351148962974548, 0.5965719819068909, new HaarRectangle(10, 2, 10, 1, -1), new HaarRectangle(10, 2, 5, 1, 2));
            stageNodes.AddHaarFeature(-0.00035031698644161224, 0.5557708144187927, 0.3482286930084229, new HaarRectangle(9, 4, 2, 12, -1), new HaarRectangle(9, 10, 2, 6, 2));
            stageNodes.AddHaarFeature(0.0005416763015091419, 0.426085889339447, 0.5693380832672119, new HaarRectangle(12, 18, 4, 1, -1), new HaarRectangle(12, 18, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0007719367858953774, 0.3494240045547485, 0.5433688759803772, new HaarRectangle(1, 7, 6, 4, -1), new HaarRectangle(1, 7, 3, 2, 2), new HaarRectangle(4, 9, 3, 2, 2));
            stageNodes.AddHaarFeature(-0.0015999219613149762, 0.4028499126434326, 0.5484359264373779, new HaarRectangle(12, 0, 6, 13, -1), new HaarRectangle(14, 0, 2, 13, 3));
            stageNodes.AddHaarFeature(-0.00011832080053864048, 0.3806901872158051, 0.5425465106964111, new HaarRectangle(2, 0, 6, 13, -1), new HaarRectangle(4, 0, 2, 13, 3));
            stageNodes.AddHaarFeature(0.0003290903114248067, 0.262010008096695, 0.5429521799087524, new HaarRectangle(10, 5, 8, 8, -1), new HaarRectangle(10, 9, 8, 4, 2));
            stageNodes.AddHaarFeature(0.0002951810893137008, 0.379976898431778, 0.5399264097213745, new HaarRectangle(8, 3, 2, 5, -1), new HaarRectangle(9, 3, 1, 5, 2));
            stageNodes.AddHaarFeature(0.00009046671038959175, 0.4433645009994507, 0.5440226197242737, new HaarRectangle(8, 4, 9, 1, -1), new HaarRectangle(11, 4, 3, 1, 3));
            stageNodes.AddHaarFeature(0.000015007190086180344, 0.3719654977321625, 0.5409119725227356, new HaarRectangle(3, 4, 9, 1, -1), new HaarRectangle(6, 4, 3, 1, 3));
            stageNodes.AddHaarFeature(0.1393561065196991, 0.552539587020874, 0.4479042887687683, new HaarRectangle(1, 0, 18, 10, -1), new HaarRectangle(7, 0, 6, 10, 3));
            stageNodes.AddHaarFeature(0.001646199030801654, 0.4264501035213471, 0.5772169828414917, new HaarRectangle(7, 17, 5, 3, -1), new HaarRectangle(7, 18, 5, 1, 3));
            stageNodes.AddHaarFeature(0.0004998443182557821, 0.4359526038169861, 0.5685871243476868, new HaarRectangle(7, 11, 6, 1, -1), new HaarRectangle(9, 11, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.001097128028050065, 0.3390136957168579, 0.5205408930778503, new HaarRectangle(2, 2, 3, 2, -1), new HaarRectangle(2, 3, 3, 1, 2));
            stageNodes.AddHaarFeature(0.0006691989256069064, 0.4557456076145172, 0.598065972328186, new HaarRectangle(8, 12, 4, 2, -1), new HaarRectangle(8, 13, 4, 1, 2));
            stageNodes.AddHaarFeature(0.0008647104259580375, 0.5134841203689575, 0.2944033145904541, new HaarRectangle(6, 10, 3, 6, -1), new HaarRectangle(6, 13, 3, 3, 2));
            stageNodes.AddHaarFeature(-0.0002718259929679334, 0.3906578123569489, 0.5377181172370911, new HaarRectangle(11, 4, 2, 4, -1), new HaarRectangle(11, 4, 1, 4, 2));
            stageNodes.AddHaarFeature(0.00003024949910468422, 0.3679609894752502, 0.5225688815116882, new HaarRectangle(7, 4, 2, 4, -1), new HaarRectangle(8, 4, 1, 4, 2));
            stageNodes.AddHaarFeature(-0.008522589690983295, 0.7293102145195007, 0.4892365038394928, new HaarRectangle(9, 6, 2, 4, -1), new HaarRectangle(9, 6, 1, 4, 2));
            stageNodes.AddHaarFeature(0.0016705560265108943, 0.43453249335289, 0.5696138143539429, new HaarRectangle(6, 13, 8, 3, -1), new HaarRectangle(6, 14, 8, 1, 3));
            stageNodes.AddHaarFeature(-0.0071433838456869125, 0.2591280043125153, 0.5225623846054077, new HaarRectangle(9, 15, 3, 4, -1), new HaarRectangle(10, 15, 1, 4, 3));
            stageNodes.AddHaarFeature(-0.0163193698972464, 0.6922279000282288, 0.4651575982570648, new HaarRectangle(9, 2, 2, 17, -1), new HaarRectangle(10, 2, 1, 17, 2));
            stageNodes.AddHaarFeature(0.004803426098078489, 0.5352262854576111, 0.3286302983760834, new HaarRectangle(7, 0, 6, 1, -1), new HaarRectangle(9, 0, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0075421929359436035, 0.2040544003248215, 0.5034546256065369, new HaarRectangle(8, 15, 3, 4, -1), new HaarRectangle(9, 15, 1, 4, 3));
            stageNodes.AddHaarFeature(-0.0143631100654602, 0.6804888844490051, 0.4889059066772461, new HaarRectangle(7, 13, 7, 3, -1), new HaarRectangle(7, 14, 7, 1, 3));
            stageNodes.AddHaarFeature(0.0008906358852982522, 0.5310695767402649, 0.3895480930805206, new HaarRectangle(8, 16, 3, 3, -1), new HaarRectangle(9, 16, 1, 3, 3));
            stageNodes.AddHaarFeature(-0.004406019113957882, 0.5741562843322754, 0.4372426867485046, new HaarRectangle(6, 2, 8, 10, -1), new HaarRectangle(6, 7, 8, 5, 2));
            stageNodes.AddHaarFeature(-0.0001886254030978307, 0.2831785976886749, 0.5098205208778381, new HaarRectangle(2, 5, 8, 8, -1), new HaarRectangle(2, 9, 8, 4, 2));
            stageNodes.AddHaarFeature(-0.0037979281041771178, 0.3372507989406586, 0.5246580243110657, new HaarRectangle(14, 16, 2, 2, -1), new HaarRectangle(14, 17, 2, 1, 2));
            stageNodes.AddHaarFeature(0.00014627049677073956, 0.5306674242019653, 0.391171008348465, new HaarRectangle(4, 16, 2, 2, -1), new HaarRectangle(4, 17, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.000049164638767251745, 0.5462496280670166, 0.3942720890045166, new HaarRectangle(10, 11, 4, 6, -1), new HaarRectangle(10, 14, 4, 3, 2));
            stageNodes.AddHaarFeature(-0.0335825011134148, 0.2157824039459229, 0.5048211812973022, new HaarRectangle(6, 11, 4, 6, -1), new HaarRectangle(6, 14, 4, 3, 2));
            stageNodes.AddHaarFeature(-0.0035339309833943844, 0.6465312242507935, 0.4872696995735169, new HaarRectangle(10, 14, 1, 3, -1), new HaarRectangle(10, 15, 1, 1, 3));
            stageNodes.AddHaarFeature(0.005014411173760891, 0.4617668092250824, 0.6248074769973755, new HaarRectangle(8, 14, 4, 3, -1), new HaarRectangle(8, 15, 4, 1, 3));
            stageNodes.AddHaarFeature(0.0188173707574606, 0.5220689177513123, 0.2000052034854889, new HaarRectangle(10, 0, 4, 6, -1), new HaarRectangle(12, 0, 2, 3, 2), new HaarRectangle(10, 3, 2, 3, 2));
            stageNodes.AddHaarFeature(-0.001343433978036046, 0.4014537930488586, 0.53016197681427, new HaarRectangle(0, 3, 20, 2, -1), new HaarRectangle(0, 4, 20, 1, 2));
            stageNodes.AddHaarFeature(0.0017557960236445067, 0.4794039130210877, 0.5653169751167297, new HaarRectangle(12, 0, 8, 2, -1), new HaarRectangle(16, 0, 4, 1, 2), new HaarRectangle(12, 1, 4, 1, 2));
            stageNodes.AddHaarFeature(-0.0956374630331993, 0.2034195065498352, 0.5006706714630127, new HaarRectangle(2, 12, 10, 8, -1), new HaarRectangle(2, 16, 10, 4, 2));
            stageNodes.AddHaarFeature(-0.0222412291914225, 0.7672473192214966, 0.5046340227127075, new HaarRectangle(17, 7, 2, 10, -1), new HaarRectangle(18, 7, 1, 5, 2), new HaarRectangle(17, 12, 1, 5, 2));
            stageNodes.AddHaarFeature(-0.0155758196488023, 0.7490342259407043, 0.4755851030349731, new HaarRectangle(1, 7, 2, 10, -1), new HaarRectangle(1, 7, 1, 5, 2), new HaarRectangle(2, 12, 1, 5, 2));
            stageNodes.AddHaarFeature(0.005359911825507879, 0.5365303754806519, 0.4004670977592468, new HaarRectangle(15, 10, 3, 6, -1), new HaarRectangle(15, 12, 3, 2, 3));
            stageNodes.AddHaarFeature(-0.0217634998261929, 0.0740154981613159, 0.4964174926280975, new HaarRectangle(4, 4, 6, 2, -1), new HaarRectangle(6, 4, 2, 2, 3));
            stageNodes.AddHaarFeature(-0.165615901350975, 0.2859103083610535, 0.5218086242675781, new HaarRectangle(0, 5, 20, 6, -1), new HaarRectangle(0, 7, 20, 2, 3));
            stageNodes.AddHaarFeature(0.0001646132004680112, 0.4191615879535675, 0.5380793213844299, new HaarRectangle(0, 0, 8, 2, -1), new HaarRectangle(0, 0, 4, 1, 2), new HaarRectangle(4, 1, 4, 1, 2));
            stageNodes.AddHaarFeature(-0.008907750248908997, 0.6273192763328552, 0.4877404868602753, new HaarRectangle(1, 0, 18, 4, -1), new HaarRectangle(7, 0, 6, 4, 3));
            stageNodes.AddHaarFeature(0.0008634644909761846, 0.5159940719604492, 0.3671025931835175, new HaarRectangle(1, 13, 6, 2, -1), new HaarRectangle(1, 14, 6, 1, 2));
            stageNodes.AddHaarFeature(-0.0013751760125160215, 0.5884376764297485, 0.4579083919525147, new HaarRectangle(10, 8, 3, 4, -1), new HaarRectangle(11, 8, 1, 4, 3));
            stageNodes.AddHaarFeature(-0.0014081239933148026, 0.3560509979724884, 0.5139945149421692, new HaarRectangle(6, 1, 6, 1, -1), new HaarRectangle(8, 1, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.003934288863092661, 0.5994288921356201, 0.466427206993103, new HaarRectangle(8, 14, 4, 3, -1), new HaarRectangle(8, 15, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.0319669283926487, 0.3345462083816528, 0.5144183039665222, new HaarRectangle(1, 6, 18, 2, -1), new HaarRectangle(10, 6, 9, 2, 2));
            stageNodes.AddHaarFeature(-0.000015089280168467669, 0.5582656264305115, 0.441405713558197, new HaarRectangle(15, 11, 1, 2, -1), new HaarRectangle(15, 12, 1, 1, 2));
            stageNodes.AddHaarFeature(0.0005199447041377425, 0.4623680114746094, 0.6168993711471558, new HaarRectangle(6, 5, 1, 2, -1), new HaarRectangle(6, 6, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0034220460802316666, 0.6557074785232544, 0.4974805116653442, new HaarRectangle(13, 4, 1, 3, -1), new HaarRectangle(13, 5, 1, 1, 3));
            stageNodes.AddHaarFeature(0.00017723299970384687, 0.5269501805305481, 0.3901908099651337, new HaarRectangle(2, 15, 1, 2, -1), new HaarRectangle(2, 16, 1, 1, 2));
            stageNodes.AddHaarFeature(0.0015716759953647852, 0.4633373022079468, 0.5790457725524902, new HaarRectangle(12, 4, 4, 3, -1), new HaarRectangle(12, 5, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.00890413299202919, 0.2689608037471771, 0.5053591132164001, new HaarRectangle(0, 0, 7, 3, -1), new HaarRectangle(0, 1, 7, 1, 3));
            stageNodes.AddHaarFeature(0.000406775187002495, 0.5456603169441223, 0.4329898953437805, new HaarRectangle(9, 12, 6, 2, -1), new HaarRectangle(9, 12, 3, 2, 2));
            stageNodes.AddHaarFeature(0.0067604780197143555, 0.4648993909358978, 0.6689761877059937, new HaarRectangle(5, 4, 2, 3, -1), new HaarRectangle(5, 5, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0029100088868290186, 0.5309703946113586, 0.3377839922904968, new HaarRectangle(18, 4, 2, 3, -1), new HaarRectangle(18, 5, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0013885459629818795, 0.4074738919734955, 0.5349133014678955, new HaarRectangle(3, 0, 8, 6, -1), new HaarRectangle(3, 2, 8, 2, 3));
            stageNodes.AddHaarFeature(-0.0767642632126808, 0.1992176026105881, 0.522824227809906, new HaarRectangle(0, 2, 20, 6, -1), new HaarRectangle(10, 2, 10, 3, 2), new HaarRectangle(0, 5, 10, 3, 2));
            stageNodes.AddHaarFeature(-0.00022688310127705336, 0.5438501834869385, 0.4253072142601013, new HaarRectangle(4, 7, 2, 4, -1), new HaarRectangle(5, 7, 1, 4, 2));
            stageNodes.AddHaarFeature(-0.006309415213763714, 0.4259178936481476, 0.5378909707069397, new HaarRectangle(3, 10, 15, 2, -1), new HaarRectangle(8, 10, 5, 2, 3));
            stageNodes.AddHaarFeature(-0.1100727990269661, 0.6904156804084778, 0.4721749126911163, new HaarRectangle(3, 0, 12, 11, -1), new HaarRectangle(9, 0, 6, 11, 2));
            stageNodes.AddHaarFeature(0.0002861965913325548, 0.4524914920330048, 0.5548306107521057, new HaarRectangle(13, 0, 2, 6, -1), new HaarRectangle(13, 0, 1, 6, 2));
            stageNodes.AddHaarFeature(0.00002942532955785282, 0.5370373725891113, 0.4236463904380798, new HaarRectangle(0, 19, 2, 1, -1), new HaarRectangle(1, 19, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0248865708708763, 0.6423557996749878, 0.4969303905963898, new HaarRectangle(16, 10, 4, 10, -1), new HaarRectangle(18, 10, 2, 5, 2), new HaarRectangle(16, 15, 2, 5, 2));
            stageNodes.AddHaarFeature(0.0331488512456417, 0.4988475143909454, 0.1613811999559403, new HaarRectangle(4, 8, 10, 3, -1), new HaarRectangle(4, 9, 10, 1, 3));
            stageNodes.AddHaarFeature(0.0007849169196560979, 0.541602611541748, 0.4223009049892426, new HaarRectangle(14, 12, 3, 3, -1), new HaarRectangle(14, 13, 3, 1, 3));
            stageNodes.AddHaarFeature(0.004708718974143267, 0.4576328992843628, 0.6027557849884033, new HaarRectangle(0, 10, 4, 10, -1), new HaarRectangle(0, 10, 2, 5, 2), new HaarRectangle(2, 15, 2, 5, 2));
            stageNodes.AddHaarFeature(0.0024144479539245367, 0.530897319316864, 0.4422498941421509, new HaarRectangle(18, 3, 2, 6, -1), new HaarRectangle(18, 5, 2, 2, 3));
            stageNodes.AddHaarFeature(0.0019523180089890957, 0.4705634117126465, 0.666332483291626, new HaarRectangle(6, 6, 1, 3, -1), new HaarRectangle(6, 7, 1, 1, 3));
            stageNodes.AddHaarFeature(0.0013031980488449335, 0.4406126141548157, 0.5526962280273438, new HaarRectangle(7, 7, 7, 2, -1), new HaarRectangle(7, 8, 7, 1, 2));
            stageNodes.AddHaarFeature(0.004473549779504538, 0.5129023790359497, 0.3301498889923096, new HaarRectangle(0, 3, 2, 6, -1), new HaarRectangle(0, 5, 2, 2, 3));
            stageNodes.AddHaarFeature(-0.002665286883711815, 0.3135471045970917, 0.5175036191940308, new HaarRectangle(11, 1, 3, 1, -1), new HaarRectangle(12, 1, 1, 1, 3));
            stageNodes.AddHaarFeature(0.0001366677024634555, 0.4119370877742767, 0.530687689781189, new HaarRectangle(5, 0, 2, 6, -1), new HaarRectangle(6, 0, 1, 6, 2));
            stageNodes.AddHaarFeature(-0.0171264503151178, 0.6177806258201599, 0.4836578965187073, new HaarRectangle(1, 1, 18, 14, -1), new HaarRectangle(7, 1, 6, 14, 3));
            stageNodes.AddHaarFeature(-0.0002660143072716892, 0.3654330968856812, 0.5169736742973328, new HaarRectangle(4, 6, 8, 3, -1), new HaarRectangle(8, 6, 4, 3, 2));
            stageNodes.AddHaarFeature(-0.022932380437851, 0.349091500043869, 0.5163992047309875, new HaarRectangle(9, 12, 6, 2, -1), new HaarRectangle(9, 12, 3, 2, 2));
            stageNodes.AddHaarFeature(0.0023316550068557262, 0.5166299939155579, 0.3709389865398407, new HaarRectangle(5, 12, 6, 2, -1), new HaarRectangle(8, 12, 3, 2, 2));
            stageNodes.AddHaarFeature(0.016925660893321, 0.501473605632782, 0.8053988218307495, new HaarRectangle(10, 7, 3, 5, -1), new HaarRectangle(11, 7, 1, 5, 3));
            stageNodes.AddHaarFeature(-0.008985882624983788, 0.6470788717269897, 0.465702086687088, new HaarRectangle(7, 7, 3, 5, -1), new HaarRectangle(8, 7, 1, 5, 3));
            stageNodes.AddHaarFeature(-0.0118746999651194, 0.3246378898620606, 0.5258755087852478, new HaarRectangle(13, 0, 3, 10, -1), new HaarRectangle(14, 0, 1, 10, 3));
            stageNodes.AddHaarFeature(0.00019350569345988333, 0.5191941857337952, 0.3839643895626068, new HaarRectangle(4, 11, 3, 2, -1), new HaarRectangle(4, 12, 3, 1, 2));
            stageNodes.AddHaarFeature(0.005871349014341831, 0.4918133914470673, 0.6187043190002441, new HaarRectangle(17, 3, 3, 6, -1), new HaarRectangle(18, 3, 1, 6, 3));
            stageNodes.AddHaarFeature(-0.2483879029750824, 0.1836802959442139, 0.4988150000572205, new HaarRectangle(1, 8, 18, 10, -1), new HaarRectangle(1, 13, 18, 5, 2));
            stageNodes.AddHaarFeature(0.0122560001909733, 0.5227053761482239, 0.3632029891014099, new HaarRectangle(13, 0, 3, 10, -1), new HaarRectangle(14, 0, 1, 10, 3));
            stageNodes.AddHaarFeature(0.0008399017970077692, 0.4490250051021576, 0.5774148106575012, new HaarRectangle(9, 14, 2, 3, -1), new HaarRectangle(9, 15, 2, 1, 3));
            stageNodes.AddHaarFeature(0.002540736924856901, 0.4804787039756775, 0.5858299136161804, new HaarRectangle(16, 3, 3, 7, -1), new HaarRectangle(17, 3, 1, 7, 3));
            stageNodes.AddHaarFeature(-0.0148224299773574, 0.2521049976348877, 0.5023537278175354, new HaarRectangle(4, 0, 3, 10, -1), new HaarRectangle(5, 0, 1, 10, 3));
            stageNodes.AddHaarFeature(-0.005797395948320627, 0.5996695756912231, 0.4853715002536774, new HaarRectangle(16, 3, 3, 7, -1), new HaarRectangle(17, 3, 1, 7, 3));
            stageNodes.AddHaarFeature(0.000726621481589973, 0.5153716802597046, 0.3671779930591583, new HaarRectangle(0, 9, 1, 2, -1), new HaarRectangle(0, 10, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0172325801104307, 0.6621719002723694, 0.4994656145572662, new HaarRectangle(18, 1, 2, 10, -1), new HaarRectangle(18, 1, 1, 10, 2));
            stageNodes.AddHaarFeature(0.007862408645451069, 0.4633395075798035, 0.6256101727485657, new HaarRectangle(0, 1, 2, 10, -1), new HaarRectangle(1, 1, 1, 10, 2));
            stageNodes.AddHaarFeature(-0.004734362009912729, 0.3615573048591614, 0.5281885266304016, new HaarRectangle(10, 16, 3, 4, -1), new HaarRectangle(11, 16, 1, 4, 3));
            stageNodes.AddHaarFeature(0.000830484787002206, 0.4442889094352722, 0.5550957918167114, new HaarRectangle(2, 8, 3, 3, -1), new HaarRectangle(3, 8, 1, 3, 3));
            stageNodes.AddHaarFeature(0.00766021991148591, 0.5162935256958008, 0.2613354921340942, new HaarRectangle(11, 0, 2, 6, -1), new HaarRectangle(12, 0, 1, 3, 2), new HaarRectangle(11, 3, 1, 3, 2));
            stageNodes.AddHaarFeature(-0.004104837775230408, 0.2789632081985474, 0.5019031763076782, new HaarRectangle(7, 0, 2, 6, -1), new HaarRectangle(7, 0, 1, 3, 2), new HaarRectangle(8, 3, 1, 3, 2));
            stageNodes.AddHaarFeature(0.004851257894188166, 0.4968984127044678, 0.5661668181419373, new HaarRectangle(16, 3, 3, 7, -1), new HaarRectangle(17, 3, 1, 7, 3));
            stageNodes.AddHaarFeature(0.0009989645332098007, 0.4445607960224152, 0.5551813244819641, new HaarRectangle(1, 3, 3, 7, -1), new HaarRectangle(2, 3, 1, 7, 3));
            stageNodes.AddHaarFeature(-0.2702363133430481, 0.0293882098048925, 0.515131413936615, new HaarRectangle(14, 1, 6, 16, -1), new HaarRectangle(16, 1, 2, 16, 3));
            stageNodes.AddHaarFeature(-0.0130906803533435, 0.5699399709701538, 0.4447459876537323, new HaarRectangle(0, 1, 6, 16, -1), new HaarRectangle(2, 1, 2, 16, 3));
            stageNodes.AddHaarFeature(-0.009434279054403305, 0.4305466115474701, 0.5487895011901855, new HaarRectangle(2, 0, 16, 8, -1), new HaarRectangle(10, 0, 8, 4, 2), new HaarRectangle(2, 4, 8, 4, 2));
            stageNodes.AddHaarFeature(-0.0015482039889320731, 0.3680317103862763, 0.512808084487915, new HaarRectangle(6, 8, 5, 3, -1), new HaarRectangle(6, 9, 5, 1, 3));
            stageNodes.AddHaarFeature(0.005374613218009472, 0.4838916957378388, 0.6101555824279785, new HaarRectangle(9, 7, 3, 3, -1), new HaarRectangle(10, 7, 1, 3, 3));
            stageNodes.AddHaarFeature(0.0015786769799888134, 0.5325223207473755, 0.4118548035621643, new HaarRectangle(8, 8, 4, 3, -1), new HaarRectangle(8, 9, 4, 1, 3));
            stageNodes.AddHaarFeature(0.0036856050137430434, 0.4810948073863983, 0.6252303123474121, new HaarRectangle(9, 6, 2, 4, -1), new HaarRectangle(9, 6, 1, 4, 2));
            stageNodes.AddHaarFeature(0.009388701990246773, 0.520022988319397, 0.3629410862922669, new HaarRectangle(0, 7, 15, 1, -1), new HaarRectangle(5, 7, 5, 1, 3));
            stageNodes.AddHaarFeature(0.0127926301211119, 0.4961709976196289, 0.673801600933075, new HaarRectangle(8, 2, 7, 9, -1), new HaarRectangle(8, 5, 7, 3, 3));
            stageNodes.AddHaarFeature(-0.0033661040943115954, 0.4060279130935669, 0.5283598899841309, new HaarRectangle(1, 7, 16, 4, -1), new HaarRectangle(1, 7, 8, 2, 2), new HaarRectangle(9, 9, 8, 2, 2));
            stageNodes.AddHaarFeature(0.00039771420415490866, 0.4674113988876343, 0.5900775194168091, new HaarRectangle(6, 12, 8, 2, -1), new HaarRectangle(6, 13, 8, 1, 2));
            stageNodes.AddHaarFeature(0.0014868030557408929, 0.4519116878509522, 0.6082053780555725, new HaarRectangle(8, 11, 3, 3, -1), new HaarRectangle(8, 12, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.0886867493391037, 0.2807899117469788, 0.5180991888046265, new HaarRectangle(4, 5, 14, 10, -1), new HaarRectangle(11, 5, 7, 5, 2), new HaarRectangle(4, 10, 7, 5, 2));
            stageNodes.AddHaarFeature(-0.00007429611287079751, 0.5295584201812744, 0.408762514591217, new HaarRectangle(4, 12, 3, 2, -1), new HaarRectangle(4, 13, 3, 1, 2));
            stageNodes.AddHaarFeature(-0.000014932939848222304, 0.5461400151252747, 0.4538542926311493, new HaarRectangle(9, 11, 6, 1, -1), new HaarRectangle(11, 11, 2, 1, 3));
            stageNodes.AddHaarFeature(0.005916223861277103, 0.5329161286354065, 0.4192134141921997, new HaarRectangle(4, 9, 7, 6, -1), new HaarRectangle(4, 11, 7, 2, 3));
            stageNodes.AddHaarFeature(0.001114164013415575, 0.4512017965316773, 0.5706217288970947, new HaarRectangle(7, 10, 6, 3, -1), new HaarRectangle(7, 11, 6, 1, 3));
            stageNodes.AddHaarFeature(0.00008924936264520511, 0.4577805995941162, 0.5897638201713562, new HaarRectangle(9, 11, 2, 2, -1), new HaarRectangle(9, 12, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0025319510605186224, 0.5299603939056396, 0.3357639014720917, new HaarRectangle(0, 5, 20, 6, -1), new HaarRectangle(0, 7, 20, 2, 3));
            stageNodes.AddHaarFeature(0.0124262003228068, 0.4959059059619904, 0.1346601992845535, new HaarRectangle(6, 4, 6, 1, -1), new HaarRectangle(8, 4, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0283357501029968, 0.5117079019546509, 0.0006104363710619509, new HaarRectangle(9, 11, 6, 1, -1), new HaarRectangle(11, 11, 2, 1, 3));
            stageNodes.AddHaarFeature(0.006616588216274977, 0.4736349880695343, 0.7011628150939941, new HaarRectangle(5, 11, 6, 1, -1), new HaarRectangle(7, 11, 2, 1, 3));
            stageNodes.AddHaarFeature(0.008046876639127731, 0.5216417908668518, 0.3282819986343384, new HaarRectangle(10, 16, 3, 4, -1), new HaarRectangle(11, 16, 1, 4, 3));
            stageNodes.AddHaarFeature(-0.001119398046284914, 0.5809860825538635, 0.4563739001750946, new HaarRectangle(8, 7, 3, 3, -1), new HaarRectangle(9, 7, 1, 3, 3));
            stageNodes.AddHaarFeature(0.0132775902748108, 0.5398362278938293, 0.4103901088237763, new HaarRectangle(2, 12, 16, 8, -1), new HaarRectangle(2, 16, 16, 4, 2));
            stageNodes.AddHaarFeature(0.0004879473999608308, 0.424928605556488, 0.5410590767860413, new HaarRectangle(0, 15, 15, 2, -1), new HaarRectangle(0, 16, 15, 1, 2));
            stageNodes.AddHaarFeature(0.0112431701272726, 0.526996374130249, 0.3438215851783752, new HaarRectangle(15, 4, 5, 6, -1), new HaarRectangle(15, 6, 5, 2, 3));
            stageNodes.AddHaarFeature(-0.0008989666821435094, 0.5633075833320618, 0.4456613063812256, new HaarRectangle(9, 5, 2, 4, -1), new HaarRectangle(10, 5, 1, 4, 2));
            stageNodes.AddHaarFeature(0.006667715962976217, 0.5312889218330383, 0.4362679123878479, new HaarRectangle(8, 10, 9, 6, -1), new HaarRectangle(8, 12, 9, 2, 3));
            stageNodes.AddHaarFeature(0.0289472993463278, 0.4701794981956482, 0.657579779624939, new HaarRectangle(2, 19, 15, 1, -1), new HaarRectangle(7, 19, 5, 1, 3));
            stageNodes.AddHaarFeature(-0.0234000496566296, 0, 0.5137398838996887, new HaarRectangle(10, 16, 3, 4, -1), new HaarRectangle(11, 16, 1, 4, 3));
            stageNodes.AddHaarFeature(-0.0891170501708984, 0.0237452797591686, 0.4942430853843689, new HaarRectangle(0, 15, 20, 4, -1), new HaarRectangle(0, 17, 20, 2, 2));
            stageNodes.AddHaarFeature(-0.0140546001493931, 0.3127323091030121, 0.511751115322113, new HaarRectangle(10, 16, 3, 4, -1), new HaarRectangle(11, 16, 1, 4, 3));
            stageNodes.AddHaarFeature(0.008123939856886864, 0.50090491771698, 0.2520025968551636, new HaarRectangle(7, 16, 3, 4, -1), new HaarRectangle(8, 16, 1, 4, 3));
            stageNodes.AddHaarFeature(-0.004996465053409338, 0.6387143731117249, 0.4927811920642853, new HaarRectangle(9, 16, 3, 3, -1), new HaarRectangle(9, 17, 3, 1, 3));
            stageNodes.AddHaarFeature(0.0031253970228135586, 0.5136849880218506, 0.3680452108383179, new HaarRectangle(8, 11, 4, 6, -1), new HaarRectangle(8, 14, 4, 3, 2));
            stageNodes.AddHaarFeature(0.006766964215785265, 0.5509843826293945, 0.4363631904125214, new HaarRectangle(9, 6, 2, 12, -1), new HaarRectangle(9, 10, 2, 4, 3));
            stageNodes.AddHaarFeature(-0.002371144015341997, 0.6162335276603699, 0.4586946964263916, new HaarRectangle(8, 17, 4, 3, -1), new HaarRectangle(8, 18, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.005352279171347618, 0.6185457706451416, 0.4920490980148315, new HaarRectangle(9, 18, 8, 2, -1), new HaarRectangle(13, 18, 4, 1, 2), new HaarRectangle(9, 19, 4, 1, 2));
            stageNodes.AddHaarFeature(-0.0159688591957092, 0.1382617950439453, 0.4983252882957459, new HaarRectangle(1, 18, 8, 2, -1), new HaarRectangle(1, 19, 8, 1, 2));
            stageNodes.AddHaarFeature(0.004767606034874916, 0.4688057899475098, 0.5490046143531799, new HaarRectangle(13, 5, 6, 15, -1), new HaarRectangle(15, 5, 2, 15, 3));
            stageNodes.AddHaarFeature(-0.002471469109877944, 0.2368514984846115, 0.5003952980041504, new HaarRectangle(9, 8, 2, 2, -1), new HaarRectangle(9, 9, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.0007103378884494305, 0.5856394171714783, 0.4721533060073853, new HaarRectangle(9, 5, 2, 3, -1), new HaarRectangle(9, 5, 1, 3, 2));
            stageNodes.AddHaarFeature(-0.1411755979061127, 0.0869000628590584, 0.4961591064929962, new HaarRectangle(1, 5, 6, 15, -1), new HaarRectangle(3, 5, 2, 15, 3));
            stageNodes.AddHaarFeature(0.1065180972218514, 0.5138837099075317, 0.1741005033254623, new HaarRectangle(4, 1, 14, 8, -1), new HaarRectangle(11, 1, 7, 4, 2), new HaarRectangle(4, 5, 7, 4, 2));
            stageNodes.AddHaarFeature(-0.0527447499334812, 0.7353636026382446, 0.4772881865501404, new HaarRectangle(2, 4, 4, 16, -1), new HaarRectangle(2, 4, 2, 8, 2), new HaarRectangle(4, 12, 2, 8, 2));
            stageNodes.AddHaarFeature(-0.00474317604675889, 0.3884406089782715, 0.5292701721191406, new HaarRectangle(12, 4, 3, 12, -1), new HaarRectangle(12, 10, 3, 6, 2));
            stageNodes.AddHaarFeature(0.0009967676596716046, 0.5223492980003357, 0.4003424048423767, new HaarRectangle(4, 5, 10, 12, -1), new HaarRectangle(4, 5, 5, 6, 2), new HaarRectangle(9, 11, 5, 6, 2));
            stageNodes.AddHaarFeature(0.00802841316908598, 0.4959106147289276, 0.7212964296340942, new HaarRectangle(9, 14, 2, 3, -1), new HaarRectangle(9, 15, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0008602585876360536, 0.4444884061813355, 0.55384761095047, new HaarRectangle(5, 4, 2, 3, -1), new HaarRectangle(5, 5, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0009319150121882558, 0.539837121963501, 0.4163244068622589, new HaarRectangle(12, 2, 4, 10, -1), new HaarRectangle(14, 2, 2, 5, 2), new HaarRectangle(12, 7, 2, 5, 2));
            stageNodes.AddHaarFeature(-0.0025082060601562266, 0.5854265093803406, 0.456250011920929, new HaarRectangle(6, 4, 7, 3, -1), new HaarRectangle(6, 5, 7, 1, 3));
            stageNodes.AddHaarFeature(-0.0021378761157393456, 0.4608069062232971, 0.5280259251594543, new HaarRectangle(2, 0, 18, 2, -1), new HaarRectangle(11, 0, 9, 1, 2), new HaarRectangle(2, 1, 9, 1, 2));
            stageNodes.AddHaarFeature(-0.002154604997485876, 0.3791126906871796, 0.5255997180938721, new HaarRectangle(0, 0, 18, 2, -1), new HaarRectangle(0, 0, 9, 1, 2), new HaarRectangle(9, 1, 9, 1, 2));
            stageNodes.AddHaarFeature(-0.007621400989592075, 0.5998609066009521, 0.4952073991298676, new HaarRectangle(13, 13, 4, 6, -1), new HaarRectangle(15, 13, 2, 3, 2), new HaarRectangle(13, 16, 2, 3, 2));
            stageNodes.AddHaarFeature(0.002205536002293229, 0.4484206140041351, 0.5588530898094177, new HaarRectangle(3, 13, 4, 6, -1), new HaarRectangle(3, 13, 2, 3, 2), new HaarRectangle(5, 16, 2, 3, 2));
            stageNodes.AddHaarFeature(0.0012586950324475763, 0.5450747013092041, 0.4423840939998627, new HaarRectangle(10, 12, 2, 6, -1), new HaarRectangle(10, 15, 2, 3, 2));
            stageNodes.AddHaarFeature(-0.005092672072350979, 0.4118275046348572, 0.5263035893440247, new HaarRectangle(5, 9, 10, 10, -1), new HaarRectangle(5, 9, 5, 5, 2), new HaarRectangle(10, 14, 5, 5, 2));
            stageNodes.AddHaarFeature(-0.0025095739401876926, 0.5787907838821411, 0.4998494982719421, new HaarRectangle(11, 4, 4, 2, -1), new HaarRectangle(13, 4, 2, 1, 2), new HaarRectangle(11, 5, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.0773275569081306, 0.8397865891456604, 0.481112003326416, new HaarRectangle(7, 12, 6, 8, -1), new HaarRectangle(10, 12, 3, 8, 2));
            stageNodes.AddHaarFeature(-0.041485819965601, 0.240861102938652, 0.5176993012428284, new HaarRectangle(12, 2, 4, 10, -1), new HaarRectangle(14, 2, 2, 5, 2), new HaarRectangle(12, 7, 2, 5, 2));
            stageNodes.AddHaarFeature(0.0001035566965583712, 0.4355360865592957, 0.5417054295539856, new HaarRectangle(8, 11, 2, 1, -1), new HaarRectangle(9, 11, 1, 1, 2));
            stageNodes.AddHaarFeature(0.0013255809899419546, 0.5453971028327942, 0.4894095063209534, new HaarRectangle(10, 5, 1, 12, -1), new HaarRectangle(10, 9, 1, 4, 3));
            stageNodes.AddHaarFeature(-0.00805987324565649, 0.5771024227142334, 0.4577918946743012, new HaarRectangle(0, 11, 6, 9, -1), new HaarRectangle(3, 11, 3, 9, 2));
            stageNodes.AddHaarFeature(0.019058620557189, 0.5169867873191833, 0.3400475084781647, new HaarRectangle(12, 2, 4, 10, -1), new HaarRectangle(14, 2, 2, 5, 2), new HaarRectangle(12, 7, 2, 5, 2));
            stageNodes.AddHaarFeature(-0.0350578911602497, 0.2203243970870972, 0.5000503063201904, new HaarRectangle(4, 2, 4, 10, -1), new HaarRectangle(4, 2, 2, 5, 2), new HaarRectangle(6, 7, 2, 5, 2));
            stageNodes.AddHaarFeature(0.005729605909436941, 0.5043408274650574, 0.6597570776939392, new HaarRectangle(11, 4, 4, 2, -1), new HaarRectangle(13, 4, 2, 1, 2), new HaarRectangle(11, 5, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.0116483299061656, 0.2186284959316254, 0.4996652901172638, new HaarRectangle(0, 14, 6, 3, -1), new HaarRectangle(0, 15, 6, 1, 3));
            stageNodes.AddHaarFeature(0.0014544479781761763, 0.5007681846618652, 0.5503727793693542, new HaarRectangle(11, 4, 4, 2, -1), new HaarRectangle(13, 4, 2, 1, 2), new HaarRectangle(11, 5, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.00025030909455381334, 0.4129841029644013, 0.524167001247406, new HaarRectangle(6, 1, 3, 2, -1), new HaarRectangle(7, 1, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.000829072727356106, 0.541286826133728, 0.4974496066570282, new HaarRectangle(11, 4, 4, 2, -1), new HaarRectangle(13, 4, 2, 1, 2), new HaarRectangle(11, 5, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0010862209601327777, 0.460552990436554, 0.5879228711128235, new HaarRectangle(5, 4, 4, 2, -1), new HaarRectangle(5, 4, 2, 1, 2), new HaarRectangle(7, 5, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0002000050008064136, 0.5278854966163635, 0.4705209136009216, new HaarRectangle(13, 0, 2, 12, -1), new HaarRectangle(14, 0, 1, 6, 2), new HaarRectangle(13, 6, 1, 6, 2));
            stageNodes.AddHaarFeature(0.0029212920926511288, 0.5129609704017639, 0.375553697347641, new HaarRectangle(6, 0, 3, 10, -1), new HaarRectangle(7, 0, 1, 10, 3));
            stageNodes.AddHaarFeature(0.0253874007612467, 0.4822691977024078, 0.5790768265724182, new HaarRectangle(3, 0, 17, 8, -1), new HaarRectangle(3, 4, 17, 4, 2));
            stageNodes.AddHaarFeature(-0.00319684692658484, 0.5248395204544067, 0.3962840139865875, new HaarRectangle(0, 4, 20, 4, -1), new HaarRectangle(0, 6, 20, 2, 2));
            stageNodeCounts.Add(stageNodes.Count - index); index = stageNodes.Count;

            stageThresholds.Add(90.25334930419922);
            stageNodes.AddHaarFeature(0.005803173873573542, 0.3498983979225159, 0.596198320388794, new HaarRectangle(0, 3, 8, 2, -1), new HaarRectangle(4, 3, 4, 2, 2));
            stageNodes.AddHaarFeature(-0.009000306949019432, 0.6816636919975281, 0.4478552043437958, new HaarRectangle(8, 11, 4, 3, -1), new HaarRectangle(8, 12, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.00115496595390141, 0.5585706233978271, 0.3578251004219055, new HaarRectangle(5, 7, 6, 4, -1), new HaarRectangle(5, 7, 3, 2, 2), new HaarRectangle(8, 9, 3, 2, 2));
            stageNodes.AddHaarFeature(-0.0011069850297644734, 0.5365036129951477, 0.3050428032875061, new HaarRectangle(8, 3, 4, 9, -1), new HaarRectangle(8, 6, 4, 3, 3));
            stageNodes.AddHaarFeature(0.00010308309720130636, 0.363909512758255, 0.5344635844230652, new HaarRectangle(8, 15, 1, 4, -1), new HaarRectangle(8, 17, 1, 2, 2));
            stageNodes.AddHaarFeature(-0.005098483990877867, 0.2859157025814056, 0.5504264831542969, new HaarRectangle(4, 5, 12, 7, -1), new HaarRectangle(8, 5, 4, 7, 3));
            stageNodes.AddHaarFeature(0.0008257220033556223, 0.5236523747444153, 0.3476041853427887, new HaarRectangle(4, 2, 4, 10, -1), new HaarRectangle(4, 2, 2, 5, 2), new HaarRectangle(6, 7, 2, 5, 2));
            stageNodes.AddHaarFeature(0.009978332556784153, 0.4750322103500366, 0.621964693069458, new HaarRectangle(3, 0, 17, 2, -1), new HaarRectangle(3, 1, 17, 1, 2));
            stageNodes.AddHaarFeature(-0.0374025292694569, 0.334337592124939, 0.527806282043457, new HaarRectangle(2, 2, 16, 15, -1), new HaarRectangle(2, 7, 16, 5, 3));
            stageNodes.AddHaarFeature(0.0048548257909715176, 0.5192180871963501, 0.3700444102287293, new HaarRectangle(15, 2, 5, 2, -1), new HaarRectangle(15, 3, 5, 1, 2));
            stageNodes.AddHaarFeature(-0.0018664470408111813, 0.2929843962192535, 0.5091944932937622, new HaarRectangle(9, 3, 2, 2, -1), new HaarRectangle(10, 3, 1, 2, 2));
            stageNodes.AddHaarFeature(0.0168888904154301, 0.3686845898628235, 0.5431225895881653, new HaarRectangle(4, 5, 16, 15, -1), new HaarRectangle(4, 10, 16, 5, 3));
            stageNodes.AddHaarFeature(-0.005837262142449617, 0.3632183969020844, 0.5221335887908936, new HaarRectangle(7, 13, 5, 6, -1), new HaarRectangle(7, 16, 5, 3, 2));
            stageNodes.AddHaarFeature(-0.00147137395106256, 0.5870683789253235, 0.4700650870800018, new HaarRectangle(10, 7, 3, 2, -1), new HaarRectangle(11, 7, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.0011522950371727347, 0.3195894956588745, 0.5140954256057739, new HaarRectangle(8, 3, 3, 1, -1), new HaarRectangle(9, 3, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.004256030078977346, 0.6301859021186829, 0.4814921021461487, new HaarRectangle(9, 16, 3, 3, -1), new HaarRectangle(9, 17, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.006737829186022282, 0.1977048069238663, 0.5025808215141296, new HaarRectangle(0, 2, 5, 2, -1), new HaarRectangle(0, 3, 5, 1, 2));
            stageNodes.AddHaarFeature(0.0113826701417565, 0.495413213968277, 0.6867045760154724, new HaarRectangle(12, 5, 4, 3, -1), new HaarRectangle(12, 6, 4, 1, 3));
            stageNodes.AddHaarFeature(0.005179470870643854, 0.5164427757263184, 0.3350647985935211, new HaarRectangle(1, 7, 12, 1, -1), new HaarRectangle(5, 7, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.1174378991127014, 0.2315246015787125, 0.5234413743019104, new HaarRectangle(7, 5, 6, 14, -1), new HaarRectangle(7, 12, 6, 7, 2));
            stageNodes.AddHaarFeature(0.0287034492939711, 0.4664297103881836, 0.6722521185874939, new HaarRectangle(0, 0, 8, 10, -1), new HaarRectangle(0, 0, 4, 5, 2), new HaarRectangle(4, 5, 4, 5, 2));
            stageNodes.AddHaarFeature(0.004823103081434965, 0.5220875144004822, 0.2723532915115356, new HaarRectangle(9, 1, 3, 2, -1), new HaarRectangle(10, 1, 1, 2, 3));
            stageNodes.AddHaarFeature(0.0026798530016094446, 0.5079277157783508, 0.2906948924064636, new HaarRectangle(8, 1, 3, 2, -1), new HaarRectangle(9, 1, 1, 2, 3));
            stageNodes.AddHaarFeature(0.008050408214330673, 0.4885950982570648, 0.6395021080970764, new HaarRectangle(12, 4, 3, 3, -1), new HaarRectangle(12, 5, 3, 1, 3));
            stageNodes.AddHaarFeature(0.004805495962500572, 0.5197256803512573, 0.365666389465332, new HaarRectangle(7, 4, 6, 16, -1), new HaarRectangle(7, 12, 6, 8, 2));
            stageNodes.AddHaarFeature(-0.0022420159075409174, 0.6153467893600464, 0.4763701856136322, new HaarRectangle(12, 4, 3, 3, -1), new HaarRectangle(12, 5, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.0137577103450894, 0.2637344896793366, 0.5030903220176697, new HaarRectangle(2, 3, 2, 6, -1), new HaarRectangle(2, 5, 2, 2, 3));
            stageNodes.AddHaarFeature(-0.1033829972147942, 0.2287521958351135, 0.5182461142539978, new HaarRectangle(14, 2, 6, 9, -1), new HaarRectangle(14, 5, 6, 3, 3));
            stageNodes.AddHaarFeature(-0.009443208575248718, 0.6953303813934326, 0.4694949090480804, new HaarRectangle(5, 4, 3, 3, -1), new HaarRectangle(5, 5, 3, 1, 3));
            stageNodes.AddHaarFeature(0.0008027118165045976, 0.5450655221939087, 0.4268783926963806, new HaarRectangle(9, 17, 3, 2, -1), new HaarRectangle(10, 17, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.004194566980004311, 0.6091387867927551, 0.4571642875671387, new HaarRectangle(5, 5, 2, 3, -1), new HaarRectangle(5, 6, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0109422104433179, 0.5241063237190247, 0.3284547030925751, new HaarRectangle(13, 11, 3, 6, -1), new HaarRectangle(13, 13, 3, 2, 3));
            stageNodes.AddHaarFeature(-0.0005784106906503439, 0.5387929081916809, 0.4179368913173676, new HaarRectangle(3, 14, 2, 6, -1), new HaarRectangle(3, 17, 2, 3, 2));
            stageNodes.AddHaarFeature(-0.002088862005621195, 0.4292691051959992, 0.5301715731620789, new HaarRectangle(14, 3, 6, 2, -1), new HaarRectangle(14, 4, 6, 1, 2));
            stageNodes.AddHaarFeature(0.0032383969519287348, 0.379234790802002, 0.5220744013786316, new HaarRectangle(0, 8, 16, 2, -1), new HaarRectangle(0, 9, 16, 1, 2));
            stageNodes.AddHaarFeature(0.004907502792775631, 0.5237283110618591, 0.4126757979393005, new HaarRectangle(14, 3, 6, 2, -1), new HaarRectangle(14, 4, 6, 1, 2));
            stageNodes.AddHaarFeature(-0.0322779417037964, 0.1947655975818634, 0.4994502067565918, new HaarRectangle(0, 0, 5, 6, -1), new HaarRectangle(0, 2, 5, 2, 3));
            stageNodes.AddHaarFeature(-0.008971123024821281, 0.6011285185813904, 0.4929032027721405, new HaarRectangle(12, 5, 4, 3, -1), new HaarRectangle(12, 6, 4, 1, 3));
            stageNodes.AddHaarFeature(0.0153210898861289, 0.5009753704071045, 0.2039822041988373, new HaarRectangle(4, 11, 3, 6, -1), new HaarRectangle(4, 13, 3, 2, 3));
            stageNodes.AddHaarFeature(0.002085556974634528, 0.4862189888954163, 0.5721694827079773, new HaarRectangle(12, 5, 4, 3, -1), new HaarRectangle(12, 6, 4, 1, 3));
            stageNodes.AddHaarFeature(0.005061502102762461, 0.5000218749046326, 0.1801805943250656, new HaarRectangle(9, 5, 1, 3, -1), new HaarRectangle(9, 6, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.003717475105077028, 0.5530117154121399, 0.4897592961788178, new HaarRectangle(12, 5, 4, 3, -1), new HaarRectangle(12, 6, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.0121705001220107, 0.4178605973720551, 0.5383723974227905, new HaarRectangle(6, 6, 8, 12, -1), new HaarRectangle(6, 12, 8, 6, 2));
            stageNodes.AddHaarFeature(0.004624839872121811, 0.4997169971466065, 0.5761327147483826, new HaarRectangle(12, 5, 4, 3, -1), new HaarRectangle(12, 6, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.0002104042941937223, 0.5331807136535645, 0.4097681045532227, new HaarRectangle(5, 12, 9, 2, -1), new HaarRectangle(8, 12, 3, 2, 3));
            stageNodes.AddHaarFeature(-0.0146417804062366, 0.5755925178527832, 0.5051776170730591, new HaarRectangle(12, 5, 4, 3, -1), new HaarRectangle(12, 6, 4, 1, 3));
            stageNodes.AddHaarFeature(0.00331994891166687, 0.4576976895332336, 0.6031805872917175, new HaarRectangle(4, 5, 4, 3, -1), new HaarRectangle(4, 6, 4, 1, 3));
            stageNodes.AddHaarFeature(0.003723687957972288, 0.4380396902561188, 0.541588306427002, new HaarRectangle(6, 6, 9, 2, -1), new HaarRectangle(9, 6, 3, 2, 3));
            stageNodes.AddHaarFeature(0.0008295116131193936, 0.5163031816482544, 0.3702219128608704, new HaarRectangle(4, 11, 1, 3, -1), new HaarRectangle(4, 12, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.0114084901288152, 0.6072946786880493, 0.4862565100193024, new HaarRectangle(14, 12, 6, 6, -1), new HaarRectangle(14, 12, 3, 6, 2));
            stageNodes.AddHaarFeature(-0.004532012157142162, 0.3292475938796997, 0.5088962912559509, new HaarRectangle(7, 0, 3, 7, -1), new HaarRectangle(8, 0, 1, 7, 3));
            stageNodes.AddHaarFeature(0.00512760179117322, 0.4829767942428589, 0.6122708916664124, new HaarRectangle(9, 8, 3, 3, -1), new HaarRectangle(10, 8, 1, 3, 3));
            stageNodes.AddHaarFeature(0.00985831581056118, 0.4660679996013641, 0.6556177139282227, new HaarRectangle(8, 8, 3, 3, -1), new HaarRectangle(9, 8, 1, 3, 3));
            stageNodes.AddHaarFeature(0.036985918879509, 0.5204849243164063, 0.1690472066402435, new HaarRectangle(5, 10, 11, 3, -1), new HaarRectangle(5, 11, 11, 1, 3));
            stageNodes.AddHaarFeature(0.004649116192013025, 0.5167322158813477, 0.3725225031375885, new HaarRectangle(5, 7, 10, 1, -1), new HaarRectangle(10, 7, 5, 1, 2));
            stageNodes.AddHaarFeature(-0.004266470205038786, 0.6406493186950684, 0.4987342953681946, new HaarRectangle(9, 7, 3, 2, -1), new HaarRectangle(10, 7, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.0004795659042429179, 0.5897293090820313, 0.4464873969554901, new HaarRectangle(8, 7, 3, 2, -1), new HaarRectangle(9, 7, 1, 2, 3));
            stageNodes.AddHaarFeature(0.0036827160511165857, 0.5441560745239258, 0.347266286611557, new HaarRectangle(11, 9, 4, 2, -1), new HaarRectangle(11, 9, 2, 2, 2));
            stageNodes.AddHaarFeature(-0.0100598800927401, 0.2143162935972214, 0.500482976436615, new HaarRectangle(5, 9, 4, 2, -1), new HaarRectangle(7, 9, 2, 2, 2));
            stageNodes.AddHaarFeature(-0.0003036184061784297, 0.538642406463623, 0.4590323865413666, new HaarRectangle(14, 10, 2, 4, -1), new HaarRectangle(14, 12, 2, 2, 2));
            stageNodes.AddHaarFeature(-0.001454547978937626, 0.5751184225082397, 0.4497095048427582, new HaarRectangle(7, 7, 3, 2, -1), new HaarRectangle(8, 7, 1, 2, 3));
            stageNodes.AddHaarFeature(0.0016515209572389722, 0.5421937704086304, 0.4238520860671997, new HaarRectangle(14, 17, 6, 3, -1), new HaarRectangle(14, 18, 6, 1, 3));
            stageNodes.AddHaarFeature(-0.007846863940358162, 0.4077920913696289, 0.5258157253265381, new HaarRectangle(4, 5, 12, 12, -1), new HaarRectangle(4, 5, 6, 6, 2), new HaarRectangle(10, 11, 6, 6, 2));
            stageNodes.AddHaarFeature(-0.005125985015183687, 0.422927588224411, 0.5479453206062317, new HaarRectangle(6, 9, 8, 8, -1), new HaarRectangle(10, 9, 4, 4, 2), new HaarRectangle(6, 13, 4, 4, 2));
            stageNodes.AddHaarFeature(-0.0368909612298012, 0.6596375703811646, 0.4674678146839142, new HaarRectangle(0, 4, 15, 4, -1), new HaarRectangle(5, 4, 5, 4, 3));
            stageNodes.AddHaarFeature(0.0002403563994448632, 0.4251135885715485, 0.5573202967643738, new HaarRectangle(13, 2, 4, 1, -1), new HaarRectangle(13, 2, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.000015150169929256664, 0.5259246826171875, 0.4074114859104157, new HaarRectangle(4, 12, 2, 2, -1), new HaarRectangle(4, 13, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0022108471021056175, 0.4671722948551178, 0.5886352062225342, new HaarRectangle(8, 13, 4, 3, -1), new HaarRectangle(8, 14, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.0011568620102480054, 0.5711066126823425, 0.4487161934375763, new HaarRectangle(9, 13, 2, 3, -1), new HaarRectangle(9, 14, 2, 1, 3));
            stageNodes.AddHaarFeature(0.004999629221856594, 0.5264198184013367, 0.2898327112197876, new HaarRectangle(13, 11, 2, 3, -1), new HaarRectangle(13, 12, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0014656189596280456, 0.3891738057136536, 0.5197871923446655, new HaarRectangle(7, 12, 4, 4, -1), new HaarRectangle(7, 12, 2, 2, 2), new HaarRectangle(9, 14, 2, 2, 2));
            stageNodes.AddHaarFeature(-0.0011975039960816505, 0.5795872807502747, 0.4927955865859985, new HaarRectangle(10, 11, 2, 2, -1), new HaarRectangle(11, 11, 1, 1, 2), new HaarRectangle(10, 12, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0044954330660402775, 0.2377603054046631, 0.5012555122375488, new HaarRectangle(8, 17, 3, 2, -1), new HaarRectangle(9, 17, 1, 2, 3));
            stageNodes.AddHaarFeature(0.00014997160178609192, 0.4876626133918762, 0.5617607831954956, new HaarRectangle(10, 11, 2, 2, -1), new HaarRectangle(11, 11, 1, 1, 2), new HaarRectangle(10, 12, 1, 1, 2));
            stageNodes.AddHaarFeature(0.002639150945469737, 0.516808807849884, 0.3765509128570557, new HaarRectangle(0, 17, 6, 3, -1), new HaarRectangle(0, 18, 6, 1, 3));
            stageNodes.AddHaarFeature(-0.0002936813107226044, 0.5446649193763733, 0.4874630868434906, new HaarRectangle(10, 11, 2, 2, -1), new HaarRectangle(11, 11, 1, 1, 2), new HaarRectangle(10, 12, 1, 1, 2));
            stageNodes.AddHaarFeature(0.0014211760135367513, 0.4687897861003876, 0.669133186340332, new HaarRectangle(8, 11, 2, 2, -1), new HaarRectangle(8, 11, 1, 1, 2), new HaarRectangle(9, 12, 1, 1, 2));
            stageNodes.AddHaarFeature(0.0794276371598244, 0.5193443894386292, 0.273294597864151, new HaarRectangle(12, 5, 8, 4, -1), new HaarRectangle(12, 5, 4, 4, 2));
            stageNodes.AddHaarFeature(0.0799375027418137, 0.4971731007099152, 0.1782083958387375, new HaarRectangle(0, 5, 8, 4, -1), new HaarRectangle(4, 5, 4, 4, 2));
            stageNodes.AddHaarFeature(0.0110892597585917, 0.5165994763374329, 0.3209475874900818, new HaarRectangle(13, 2, 4, 1, -1), new HaarRectangle(13, 2, 2, 1, 2));
            stageNodes.AddHaarFeature(0.00016560709627810866, 0.4058471918106079, 0.5307276248931885, new HaarRectangle(3, 2, 4, 1, -1), new HaarRectangle(5, 2, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.0053354292176663876, 0.3445056974887848, 0.5158129930496216, new HaarRectangle(10, 0, 4, 2, -1), new HaarRectangle(12, 0, 2, 1, 2), new HaarRectangle(10, 1, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0011287260567769408, 0.4594863057136536, 0.6075533032417297, new HaarRectangle(7, 12, 3, 1, -1), new HaarRectangle(8, 12, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.0219692196696997, 0.1680400967597961, 0.5228595733642578, new HaarRectangle(8, 11, 4, 8, -1), new HaarRectangle(10, 11, 2, 4, 2), new HaarRectangle(8, 15, 2, 4, 2));
            stageNodes.AddHaarFeature(-0.0002177532005589455, 0.3861596882343292, 0.5215672850608826, new HaarRectangle(9, 9, 2, 2, -1), new HaarRectangle(9, 10, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0002020014944719151, 0.5517979264259338, 0.4363039135932922, new HaarRectangle(3, 18, 15, 2, -1), new HaarRectangle(3, 19, 15, 1, 2));
            stageNodes.AddHaarFeature(-0.0217331498861313, 0.7999460101127625, 0.4789851009845734, new HaarRectangle(2, 6, 2, 12, -1), new HaarRectangle(2, 6, 1, 6, 2), new HaarRectangle(3, 12, 1, 6, 2));
            stageNodes.AddHaarFeature(-0.0008439993252977728, 0.4085975885391235, 0.5374773144721985, new HaarRectangle(9, 8, 2, 3, -1), new HaarRectangle(9, 9, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.00043895249837078154, 0.5470405220985413, 0.4366143047809601, new HaarRectangle(7, 10, 3, 2, -1), new HaarRectangle(8, 10, 1, 2, 3));
            stageNodes.AddHaarFeature(0.0015092400135472417, 0.4988996982574463, 0.5842149257659912, new HaarRectangle(11, 11, 3, 1, -1), new HaarRectangle(12, 11, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.003554783994331956, 0.6753690242767334, 0.4721005856990814, new HaarRectangle(6, 11, 3, 1, -1), new HaarRectangle(7, 11, 1, 1, 3));
            stageNodes.AddHaarFeature(0.0004819140012841672, 0.541585385799408, 0.4357109069824219, new HaarRectangle(9, 2, 4, 2, -1), new HaarRectangle(11, 2, 2, 1, 2), new HaarRectangle(9, 3, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.00602643983438611, 0.2258509993553162, 0.499188095331192, new HaarRectangle(4, 12, 2, 3, -1), new HaarRectangle(4, 13, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0116681400686502, 0.625655472278595, 0.4927498996257782, new HaarRectangle(2, 1, 18, 3, -1), new HaarRectangle(8, 1, 6, 3, 3));
            stageNodes.AddHaarFeature(-0.0028718370012938976, 0.3947784900665283, 0.524580180644989, new HaarRectangle(5, 1, 4, 14, -1), new HaarRectangle(7, 1, 2, 14, 2));
            stageNodes.AddHaarFeature(0.0170511696487665, 0.4752511084079742, 0.5794224143028259, new HaarRectangle(8, 16, 12, 3, -1), new HaarRectangle(8, 16, 6, 3, 2));
            stageNodes.AddHaarFeature(-0.0133520802482963, 0.6041104793548584, 0.4544535875320435, new HaarRectangle(1, 17, 18, 3, -1), new HaarRectangle(7, 17, 6, 3, 3));
            stageNodes.AddHaarFeature(-0.0003930180100724101, 0.4258275926113129, 0.5544905066490173, new HaarRectangle(9, 14, 2, 6, -1), new HaarRectangle(9, 17, 2, 3, 2));
            stageNodes.AddHaarFeature(0.0030483349692076445, 0.5233420133590698, 0.3780272901058197, new HaarRectangle(9, 12, 1, 8, -1), new HaarRectangle(9, 16, 1, 4, 2));
            stageNodes.AddHaarFeature(-0.00435792887583375, 0.6371889114379883, 0.4838674068450928, new HaarRectangle(9, 14, 2, 3, -1), new HaarRectangle(9, 15, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0056661018170416355, 0.5374705791473389, 0.4163666069507599, new HaarRectangle(9, 6, 2, 12, -1), new HaarRectangle(9, 10, 2, 4, 3));
            stageNodes.AddHaarFeature(0.00006067733920644969, 0.4638795852661133, 0.5311625003814697, new HaarRectangle(12, 9, 3, 3, -1), new HaarRectangle(12, 10, 3, 1, 3));
            stageNodes.AddHaarFeature(0.0367381609976292, 0.4688656032085419, 0.6466524004936218, new HaarRectangle(0, 1, 4, 8, -1), new HaarRectangle(2, 1, 2, 8, 2));
            stageNodes.AddHaarFeature(0.008652813732624054, 0.5204318761825562, 0.2188657969236374, new HaarRectangle(9, 1, 6, 2, -1), new HaarRectangle(12, 1, 3, 1, 2), new HaarRectangle(9, 2, 3, 1, 2));
            stageNodes.AddHaarFeature(-0.1537135988473892, 0.1630371958017349, 0.4958840012550354, new HaarRectangle(1, 3, 12, 14, -1), new HaarRectangle(1, 10, 12, 7, 2));
            stageNodes.AddHaarFeature(-0.00041560421232134104, 0.577445924282074, 0.4696458876132965, new HaarRectangle(8, 12, 4, 2, -1), new HaarRectangle(10, 12, 2, 1, 2), new HaarRectangle(8, 13, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.0012640169588848948, 0.3977175951004028, 0.5217198133468628, new HaarRectangle(1, 9, 10, 2, -1), new HaarRectangle(1, 9, 5, 1, 2), new HaarRectangle(6, 10, 5, 1, 2));
            stageNodes.AddHaarFeature(-0.003547334112226963, 0.6046528220176697, 0.480831503868103, new HaarRectangle(8, 15, 4, 3, -1), new HaarRectangle(8, 16, 4, 1, 3));
            stageNodes.AddHaarFeature(0.000030019069527043033, 0.3996723890304565, 0.5228201150894165, new HaarRectangle(6, 8, 8, 3, -1), new HaarRectangle(6, 9, 8, 1, 3));
            stageNodes.AddHaarFeature(0.00131130195222795, 0.4712158143520355, 0.5765997767448425, new HaarRectangle(9, 15, 5, 3, -1), new HaarRectangle(9, 16, 5, 1, 3));
            stageNodes.AddHaarFeature(-0.0013374709524214268, 0.4109584987163544, 0.5253170132637024, new HaarRectangle(8, 7, 4, 3, -1), new HaarRectangle(8, 8, 4, 1, 3));
            stageNodes.AddHaarFeature(0.0208767093718052, 0.5202993750572205, 0.1757981926202774, new HaarRectangle(7, 7, 6, 2, -1), new HaarRectangle(7, 8, 6, 1, 2));
            stageNodes.AddHaarFeature(-0.007549794856458902, 0.6566609740257263, 0.4694975018501282, new HaarRectangle(5, 7, 8, 2, -1), new HaarRectangle(5, 7, 4, 1, 2), new HaarRectangle(9, 8, 4, 1, 2));
            stageNodes.AddHaarFeature(0.0241885501891375, 0.5128673911094666, 0.3370220959186554, new HaarRectangle(12, 9, 3, 3, -1), new HaarRectangle(12, 10, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.0029358828905969854, 0.658078670501709, 0.4694541096687317, new HaarRectangle(4, 7, 4, 2, -1), new HaarRectangle(4, 8, 4, 1, 2));
            stageNodes.AddHaarFeature(0.0575579293072224, 0.5146445035934448, 0.2775259912014008, new HaarRectangle(14, 2, 6, 9, -1), new HaarRectangle(14, 5, 6, 3, 3));
            stageNodes.AddHaarFeature(-0.0011343370424583554, 0.3836601972579956, 0.5192667245864868, new HaarRectangle(4, 9, 3, 3, -1), new HaarRectangle(5, 9, 1, 3, 3));
            stageNodes.AddHaarFeature(0.0168169997632504, 0.5085592865943909, 0.6177260875701904, new HaarRectangle(12, 9, 3, 3, -1), new HaarRectangle(12, 10, 3, 1, 3));
            stageNodes.AddHaarFeature(0.005053517874330282, 0.5138763189315796, 0.3684791922569275, new HaarRectangle(0, 2, 6, 9, -1), new HaarRectangle(0, 5, 6, 3, 3));
            stageNodes.AddHaarFeature(-0.004587471019476652, 0.5989655256271362, 0.4835202097892761, new HaarRectangle(17, 3, 3, 6, -1), new HaarRectangle(18, 3, 1, 6, 3));
            stageNodes.AddHaarFeature(0.001688246033154428, 0.4509486854076386, 0.5723056793212891, new HaarRectangle(0, 3, 3, 6, -1), new HaarRectangle(1, 3, 1, 6, 3));
            stageNodes.AddHaarFeature(-0.0016554000321775677, 0.3496770858764648, 0.5243319272994995, new HaarRectangle(17, 14, 1, 2, -1), new HaarRectangle(17, 15, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0193738006055355, 0.1120536997914314, 0.496871292591095, new HaarRectangle(4, 9, 4, 3, -1), new HaarRectangle(6, 9, 2, 3, 2));
            stageNodes.AddHaarFeature(0.0103744501248002, 0.5148196816444397, 0.4395213127136231, new HaarRectangle(12, 9, 3, 3, -1), new HaarRectangle(12, 10, 3, 1, 3));
            stageNodes.AddHaarFeature(0.00014973050565458834, 0.4084999859333038, 0.526988685131073, new HaarRectangle(5, 9, 3, 3, -1), new HaarRectangle(5, 10, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.042981930077076, 0.6394104957580566, 0.501850426197052, new HaarRectangle(9, 5, 6, 8, -1), new HaarRectangle(12, 5, 3, 4, 2), new HaarRectangle(9, 9, 3, 4, 2));
            stageNodes.AddHaarFeature(0.008306593634188175, 0.470755398273468, 0.6698353290557861, new HaarRectangle(5, 5, 6, 8, -1), new HaarRectangle(5, 5, 3, 4, 2), new HaarRectangle(8, 9, 3, 4, 2));
            stageNodes.AddHaarFeature(-0.0041285790503025055, 0.4541369080543518, 0.5323647260665894, new HaarRectangle(16, 1, 4, 6, -1), new HaarRectangle(16, 4, 4, 3, 2));
            stageNodes.AddHaarFeature(0.0017399420030415058, 0.433396190404892, 0.5439866185188293, new HaarRectangle(1, 0, 6, 20, -1), new HaarRectangle(3, 0, 2, 20, 3));
            stageNodes.AddHaarFeature(0.00011739750334527344, 0.4579687118530273, 0.5543426275253296, new HaarRectangle(12, 11, 3, 2, -1), new HaarRectangle(13, 11, 1, 2, 3));
            stageNodes.AddHaarFeature(0.00018585780344437808, 0.4324643909931183, 0.5426754951477051, new HaarRectangle(5, 11, 3, 2, -1), new HaarRectangle(6, 11, 1, 2, 3));
            stageNodes.AddHaarFeature(0.005558769218623638, 0.525722086429596, 0.3550611138343811, new HaarRectangle(9, 4, 6, 1, -1), new HaarRectangle(11, 4, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.007985156029462814, 0.6043018102645874, 0.4630635976791382, new HaarRectangle(0, 0, 8, 3, -1), new HaarRectangle(4, 0, 4, 3, 2));
            stageNodes.AddHaarFeature(0.0006059412262402475, 0.4598254859447479, 0.55331951379776, new HaarRectangle(15, 0, 2, 5, -1), new HaarRectangle(15, 0, 1, 5, 2));
            stageNodes.AddHaarFeature(-0.0002298304025316611, 0.4130752086639404, 0.5322461128234863, new HaarRectangle(4, 1, 3, 2, -1), new HaarRectangle(5, 1, 1, 2, 3));
            stageNodes.AddHaarFeature(0.0004374021082185209, 0.4043039977550507, 0.5409289002418518, new HaarRectangle(7, 0, 6, 15, -1), new HaarRectangle(9, 0, 2, 15, 3));
            stageNodes.AddHaarFeature(0.0002948202018160373, 0.4494963884353638, 0.5628852248191833, new HaarRectangle(6, 11, 3, 1, -1), new HaarRectangle(7, 11, 1, 1, 3));
            stageNodes.AddHaarFeature(0.0103126596659422, 0.5177510976791382, 0.2704316973686218, new HaarRectangle(12, 0, 3, 4, -1), new HaarRectangle(13, 0, 1, 4, 3));
            stageNodes.AddHaarFeature(-0.00772411096841097, 0.1988019049167633, 0.4980553984642029, new HaarRectangle(5, 4, 6, 1, -1), new HaarRectangle(7, 4, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.004679720848798752, 0.6644750237464905, 0.5018296241760254, new HaarRectangle(12, 7, 3, 2, -1), new HaarRectangle(12, 8, 3, 1, 2));
            stageNodes.AddHaarFeature(-0.005075545981526375, 0.3898304998874664, 0.5185269117355347, new HaarRectangle(0, 1, 4, 6, -1), new HaarRectangle(0, 4, 4, 3, 2));
            stageNodes.AddHaarFeature(0.00224797404371202, 0.4801808893680573, 0.5660336017608643, new HaarRectangle(12, 7, 3, 2, -1), new HaarRectangle(12, 8, 3, 1, 2));
            stageNodes.AddHaarFeature(0.0008332700817845762, 0.5210919976234436, 0.3957188129425049, new HaarRectangle(2, 16, 3, 3, -1), new HaarRectangle(2, 17, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.0412793308496475, 0.6154541969299316, 0.5007054209709167, new HaarRectangle(13, 8, 6, 10, -1), new HaarRectangle(16, 8, 3, 5, 2), new HaarRectangle(13, 13, 3, 5, 2));
            stageNodes.AddHaarFeature(-0.0005093018990010023, 0.3975942134857178, 0.5228403806686401, new HaarRectangle(0, 9, 5, 2, -1), new HaarRectangle(0, 10, 5, 1, 2));
            stageNodes.AddHaarFeature(0.0012568780221045015, 0.4979138076305389, 0.5939183235168457, new HaarRectangle(12, 11, 2, 2, -1), new HaarRectangle(13, 11, 1, 1, 2), new HaarRectangle(12, 12, 1, 1, 2));
            stageNodes.AddHaarFeature(0.008004849776625633, 0.4984497129917145, 0.1633366048336029, new HaarRectangle(3, 15, 3, 3, -1), new HaarRectangle(3, 16, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.0011879300000146031, 0.5904964804649353, 0.4942624866962433, new HaarRectangle(12, 7, 3, 2, -1), new HaarRectangle(12, 8, 3, 1, 2));
            stageNodes.AddHaarFeature(0.0006194895249791443, 0.4199557900428772, 0.5328726172447205, new HaarRectangle(5, 7, 3, 2, -1), new HaarRectangle(5, 8, 3, 1, 2));
            stageNodes.AddHaarFeature(0.006682985927909613, 0.5418602824211121, 0.490588903427124, new HaarRectangle(9, 5, 9, 9, -1), new HaarRectangle(9, 8, 9, 3, 3));
            stageNodes.AddHaarFeature(-0.003706234041601419, 0.3725939095020294, 0.5138000249862671, new HaarRectangle(5, 0, 3, 7, -1), new HaarRectangle(6, 0, 1, 7, 3));
            stageNodes.AddHaarFeature(-0.0397394113242626, 0.6478961110115051, 0.5050346851348877, new HaarRectangle(5, 2, 12, 5, -1), new HaarRectangle(9, 2, 4, 5, 3));
            stageNodes.AddHaarFeature(0.0014085009461268785, 0.4682339131832123, 0.6377884149551392, new HaarRectangle(6, 11, 2, 2, -1), new HaarRectangle(6, 11, 1, 1, 2), new HaarRectangle(7, 12, 1, 1, 2));
            stageNodes.AddHaarFeature(0.0003932268882635981, 0.5458530187606812, 0.415048211812973, new HaarRectangle(15, 15, 3, 2, -1), new HaarRectangle(15, 16, 3, 1, 2));
            stageNodes.AddHaarFeature(-0.0018979819724336269, 0.3690159916877747, 0.5149704217910767, new HaarRectangle(2, 15, 3, 2, -1), new HaarRectangle(2, 16, 3, 1, 2));
            stageNodes.AddHaarFeature(-0.0139704402536154, 0.6050562858581543, 0.4811357855796814, new HaarRectangle(14, 12, 6, 8, -1), new HaarRectangle(17, 12, 3, 4, 2), new HaarRectangle(14, 16, 3, 4, 2));
            stageNodes.AddHaarFeature(-0.1010081991553307, 0.2017080038785934, 0.4992361962795258, new HaarRectangle(2, 8, 15, 6, -1), new HaarRectangle(7, 8, 5, 6, 3));
            stageNodes.AddHaarFeature(-0.0173469204455614, 0.5713148713111877, 0.4899486005306244, new HaarRectangle(2, 2, 18, 17, -1), new HaarRectangle(8, 2, 6, 17, 3));
            stageNodes.AddHaarFeature(0.000156197595060803, 0.4215388894081116, 0.5392642021179199, new HaarRectangle(5, 1, 4, 1, -1), new HaarRectangle(7, 1, 2, 1, 2));
            stageNodes.AddHaarFeature(0.1343892961740494, 0.5136151909828186, 0.3767612874507904, new HaarRectangle(5, 2, 12, 5, -1), new HaarRectangle(9, 2, 4, 5, 3));
            stageNodes.AddHaarFeature(-0.0245822407305241, 0.7027357816696167, 0.4747906923294067, new HaarRectangle(3, 2, 12, 5, -1), new HaarRectangle(7, 2, 4, 5, 3));
            stageNodes.AddHaarFeature(-0.0038553720805794, 0.4317409098148346, 0.5427716970443726, new HaarRectangle(4, 9, 12, 4, -1), new HaarRectangle(10, 9, 6, 2, 2), new HaarRectangle(4, 11, 6, 2, 2));
            stageNodes.AddHaarFeature(-0.002316524973139167, 0.594269871711731, 0.4618647992610931, new HaarRectangle(5, 15, 6, 2, -1), new HaarRectangle(5, 15, 3, 1, 2), new HaarRectangle(8, 16, 3, 1, 2));
            stageNodes.AddHaarFeature(-0.004851812031120062, 0.6191568970680237, 0.4884895086288452, new HaarRectangle(10, 14, 2, 3, -1), new HaarRectangle(10, 15, 2, 1, 3));
            stageNodes.AddHaarFeature(0.002469993894919753, 0.5256664752960205, 0.4017199873924255, new HaarRectangle(0, 13, 20, 2, -1), new HaarRectangle(0, 13, 10, 1, 2), new HaarRectangle(10, 14, 10, 1, 2));
            stageNodes.AddHaarFeature(0.0454969592392445, 0.5237867832183838, 0.2685773968696594, new HaarRectangle(4, 9, 12, 8, -1), new HaarRectangle(10, 9, 6, 4, 2), new HaarRectangle(4, 13, 6, 4, 2));
            stageNodes.AddHaarFeature(-0.0203195996582508, 0.213044598698616, 0.4979738891124725, new HaarRectangle(8, 13, 3, 6, -1), new HaarRectangle(8, 16, 3, 3, 2));
            stageNodes.AddHaarFeature(0.0002699499891605228, 0.481404185295105, 0.5543122291564941, new HaarRectangle(10, 12, 2, 2, -1), new HaarRectangle(10, 13, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.0018232699949294329, 0.6482579708099365, 0.4709989130496979, new HaarRectangle(9, 12, 2, 2, -1), new HaarRectangle(9, 12, 1, 1, 2), new HaarRectangle(10, 13, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.006301579065620899, 0.4581927955150604, 0.5306236147880554, new HaarRectangle(4, 11, 14, 4, -1), new HaarRectangle(11, 11, 7, 2, 2), new HaarRectangle(4, 13, 7, 2, 2));
            stageNodes.AddHaarFeature(-0.0002413949987385422, 0.5232086777687073, 0.4051763117313385, new HaarRectangle(8, 5, 4, 2, -1), new HaarRectangle(8, 6, 4, 1, 2));
            stageNodes.AddHaarFeature(-0.001033036969602108, 0.5556201934814453, 0.4789193868637085, new HaarRectangle(10, 10, 6, 3, -1), new HaarRectangle(12, 10, 2, 3, 3));
            stageNodes.AddHaarFeature(0.00018041160365100953, 0.5229442715644836, 0.4011810123920441, new HaarRectangle(2, 14, 1, 2, -1), new HaarRectangle(2, 15, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0614078603684902, 0.62986820936203, 0.5010703206062317, new HaarRectangle(13, 8, 6, 12, -1), new HaarRectangle(16, 8, 3, 6, 2), new HaarRectangle(13, 14, 3, 6, 2));
            stageNodes.AddHaarFeature(-0.0695439130067825, 0.7228280901908875, 0.4773184061050415, new HaarRectangle(1, 8, 6, 12, -1), new HaarRectangle(1, 8, 3, 6, 2), new HaarRectangle(4, 14, 3, 6, 2));
            stageNodes.AddHaarFeature(-0.0705426633358002, 0.2269513010978699, 0.5182529091835022, new HaarRectangle(10, 0, 6, 10, -1), new HaarRectangle(12, 0, 2, 10, 3));
            stageNodes.AddHaarFeature(0.0024423799477517605, 0.5237097144126892, 0.4098151028156281, new HaarRectangle(5, 11, 8, 4, -1), new HaarRectangle(5, 11, 4, 2, 2), new HaarRectangle(9, 13, 4, 2, 2));
            stageNodes.AddHaarFeature(0.0015494349645450711, 0.4773750901222229, 0.5468043088912964, new HaarRectangle(10, 16, 8, 4, -1), new HaarRectangle(14, 16, 4, 2, 2), new HaarRectangle(10, 18, 4, 2, 2));
            stageNodes.AddHaarFeature(-0.0239142198115587, 0.7146975994110107, 0.4783824980258942, new HaarRectangle(7, 7, 6, 6, -1), new HaarRectangle(9, 7, 2, 6, 3));
            stageNodes.AddHaarFeature(-0.0124536901712418, 0.2635296881198883, 0.5241122841835022, new HaarRectangle(10, 2, 4, 10, -1), new HaarRectangle(10, 2, 2, 10, 2));
            stageNodes.AddHaarFeature(-0.00020760179904755205, 0.3623757064342499, 0.5113608837127686, new HaarRectangle(6, 1, 4, 9, -1), new HaarRectangle(8, 1, 2, 9, 2));
            stageNodes.AddHaarFeature(0.000029781080229440704, 0.4705932140350342, 0.5432801842689514, new HaarRectangle(12, 19, 2, 1, -1), new HaarRectangle(12, 19, 1, 1, 2));
            stageNodeCounts.Add(stageNodes.Count - index); index = stageNodes.Count;

            stageThresholds.Add(104.74919891357422);
            stageNodes.AddHaarFeature(0.0117727499455214, 0.3860518932342529, 0.6421167254447937, new HaarRectangle(1, 2, 4, 9, -1), new HaarRectangle(3, 2, 2, 9, 2));
            stageNodes.AddHaarFeature(0.0270375702530146, 0.4385654926300049, 0.675403892993927, new HaarRectangle(7, 5, 6, 4, -1), new HaarRectangle(9, 5, 2, 4, 3));
            stageNodes.AddHaarFeature(-0.00003641950024757534, 0.5487101078033447, 0.34233158826828, new HaarRectangle(9, 4, 2, 4, -1), new HaarRectangle(9, 6, 2, 2, 2));
            stageNodes.AddHaarFeature(0.001999540952965617, 0.3230532109737396, 0.5400317907333374, new HaarRectangle(14, 5, 2, 8, -1), new HaarRectangle(14, 9, 2, 4, 2));
            stageNodes.AddHaarFeature(0.0045278300531208515, 0.5091639757156372, 0.2935043871402741, new HaarRectangle(7, 6, 5, 12, -1), new HaarRectangle(7, 12, 5, 6, 2));
            stageNodes.AddHaarFeature(0.00047890920541249216, 0.4178153872489929, 0.5344064235687256, new HaarRectangle(14, 6, 2, 6, -1), new HaarRectangle(14, 9, 2, 3, 2));
            stageNodes.AddHaarFeature(0.001172092044726014, 0.2899182140827179, 0.5132070779800415, new HaarRectangle(4, 6, 2, 6, -1), new HaarRectangle(4, 9, 2, 3, 2));
            stageNodes.AddHaarFeature(0.0009530570241622627, 0.428012490272522, 0.5560845136642456, new HaarRectangle(8, 15, 10, 4, -1), new HaarRectangle(13, 15, 5, 2, 2), new HaarRectangle(8, 17, 5, 2, 2));
            stageNodes.AddHaarFeature(0.000015099150004971308, 0.4044871926307678, 0.5404760241508484, new HaarRectangle(6, 18, 2, 2, -1), new HaarRectangle(7, 18, 1, 2, 2));
            stageNodes.AddHaarFeature(-0.0006081790197640657, 0.4271768927574158, 0.5503466129302979, new HaarRectangle(11, 3, 6, 2, -1), new HaarRectangle(11, 4, 6, 1, 2));
            stageNodes.AddHaarFeature(0.003322452073916793, 0.3962723910808563, 0.5369734764099121, new HaarRectangle(2, 0, 16, 6, -1), new HaarRectangle(2, 2, 16, 2, 3));
            stageNodes.AddHaarFeature(-0.00110374903306365, 0.4727177917957306, 0.5237749814987183, new HaarRectangle(11, 3, 6, 2, -1), new HaarRectangle(11, 4, 6, 1, 2));
            stageNodes.AddHaarFeature(-0.001435026992112398, 0.5603008270263672, 0.4223509132862091, new HaarRectangle(4, 11, 10, 3, -1), new HaarRectangle(4, 12, 10, 1, 3));
            stageNodes.AddHaarFeature(0.00207673991099, 0.5225917100906372, 0.4732725918292999, new HaarRectangle(11, 3, 6, 2, -1), new HaarRectangle(11, 4, 6, 1, 2));
            stageNodes.AddHaarFeature(-0.00016412809782195836, 0.3999075889587402, 0.5432739853858948, new HaarRectangle(3, 3, 6, 2, -1), new HaarRectangle(3, 4, 6, 1, 2));
            stageNodes.AddHaarFeature(0.008830243721604347, 0.4678385853767395, 0.6027327179908752, new HaarRectangle(16, 0, 4, 7, -1), new HaarRectangle(16, 0, 2, 7, 2));
            stageNodes.AddHaarFeature(-0.0105520701035857, 0.3493967056274414, 0.5213974714279175, new HaarRectangle(0, 14, 9, 6, -1), new HaarRectangle(0, 16, 9, 2, 3));
            stageNodes.AddHaarFeature(-0.00227316003292799, 0.6185818910598755, 0.4749062955379486, new HaarRectangle(9, 16, 3, 3, -1), new HaarRectangle(9, 17, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.0008478633244521915, 0.5285341143608093, 0.3843482136726379, new HaarRectangle(4, 6, 6, 2, -1), new HaarRectangle(6, 6, 2, 2, 3));
            stageNodes.AddHaarFeature(0.0012081359745934606, 0.536064088344574, 0.3447335958480835, new HaarRectangle(15, 11, 1, 3, -1), new HaarRectangle(15, 12, 1, 1, 3));
            stageNodes.AddHaarFeature(0.002651273040100932, 0.4558292031288147, 0.6193962097167969, new HaarRectangle(5, 5, 2, 3, -1), new HaarRectangle(5, 6, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0011012479662895205, 0.368023008108139, 0.5327628254890442, new HaarRectangle(10, 9, 2, 2, -1), new HaarRectangle(10, 10, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0004956151824444532, 0.396059513092041, 0.5274940729141235, new HaarRectangle(3, 1, 4, 3, -1), new HaarRectangle(5, 1, 2, 3, 2));
            stageNodes.AddHaarFeature(-0.0439017713069916, 0.7020444869995117, 0.4992839097976685, new HaarRectangle(16, 0, 4, 7, -1), new HaarRectangle(16, 0, 2, 7, 2));
            stageNodes.AddHaarFeature(0.0346903502941132, 0.5049164295196533, 0.276660293340683, new HaarRectangle(0, 0, 20, 1, -1), new HaarRectangle(10, 0, 10, 1, 2));
            stageNodes.AddHaarFeature(-0.002744219033047557, 0.2672632932662964, 0.5274971127510071, new HaarRectangle(15, 11, 1, 3, -1), new HaarRectangle(15, 12, 1, 1, 3));
            stageNodes.AddHaarFeature(0.0033316588960587974, 0.4579482972621918, 0.6001101732254028, new HaarRectangle(0, 4, 3, 4, -1), new HaarRectangle(1, 4, 1, 4, 3));
            stageNodes.AddHaarFeature(-0.0200445707887411, 0.3171594142913818, 0.523571789264679, new HaarRectangle(16, 3, 3, 6, -1), new HaarRectangle(16, 5, 3, 2, 3));
            stageNodes.AddHaarFeature(0.0013492030557245016, 0.5265362858772278, 0.4034324884414673, new HaarRectangle(1, 3, 3, 6, -1), new HaarRectangle(1, 5, 3, 2, 3));
            stageNodes.AddHaarFeature(0.0029702018946409225, 0.5332456827163696, 0.4571984112262726, new HaarRectangle(6, 2, 12, 6, -1), new HaarRectangle(12, 2, 6, 3, 2), new HaarRectangle(6, 5, 6, 3, 2));
            stageNodes.AddHaarFeature(0.006303998176008463, 0.4593310952186585, 0.6034635901451111, new HaarRectangle(8, 10, 4, 3, -1), new HaarRectangle(8, 11, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.0129365902394056, 0.4437963962554932, 0.5372971296310425, new HaarRectangle(4, 2, 14, 6, -1), new HaarRectangle(11, 2, 7, 3, 2), new HaarRectangle(4, 5, 7, 3, 2));
            stageNodes.AddHaarFeature(0.004014872945845127, 0.4680323898792267, 0.6437833905220032, new HaarRectangle(9, 11, 2, 3, -1), new HaarRectangle(9, 12, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.002640167949721217, 0.3709631860256195, 0.5314332842826843, new HaarRectangle(15, 13, 2, 3, -1), new HaarRectangle(15, 14, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0139184398576617, 0.4723555147647858, 0.713080883026123, new HaarRectangle(8, 12, 4, 3, -1), new HaarRectangle(8, 13, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.00045087869511917233, 0.4492394030094147, 0.5370404124259949, new HaarRectangle(15, 11, 1, 3, -1), new HaarRectangle(15, 12, 1, 1, 3));
            stageNodes.AddHaarFeature(0.00025384349282830954, 0.4406864047050476, 0.5514402985572815, new HaarRectangle(7, 13, 5, 2, -1), new HaarRectangle(7, 14, 5, 1, 2));
            stageNodes.AddHaarFeature(0.002271000063046813, 0.4682416915893555, 0.5967984199523926, new HaarRectangle(7, 12, 6, 3, -1), new HaarRectangle(7, 13, 6, 1, 3));
            stageNodes.AddHaarFeature(0.002412077970802784, 0.5079392194747925, 0.3018598854541779, new HaarRectangle(5, 11, 4, 4, -1), new HaarRectangle(5, 13, 4, 2, 2));
            stageNodes.AddHaarFeature(-0.00003602567085181363, 0.560103714466095, 0.4471096992492676, new HaarRectangle(11, 4, 3, 3, -1), new HaarRectangle(12, 4, 1, 3, 3));
            stageNodes.AddHaarFeature(-0.0074905529618263245, 0.2207535058259964, 0.4989944100379944, new HaarRectangle(6, 4, 3, 3, -1), new HaarRectangle(7, 4, 1, 3, 3));
            stageNodes.AddHaarFeature(-0.017513120546937, 0.6531215906143188, 0.5017648935317993, new HaarRectangle(16, 5, 3, 6, -1), new HaarRectangle(17, 5, 1, 6, 3));
            stageNodes.AddHaarFeature(0.1428163051605225, 0.4967963099479675, 0.1482062041759491, new HaarRectangle(3, 6, 12, 7, -1), new HaarRectangle(7, 6, 4, 7, 3));
            stageNodes.AddHaarFeature(0.005534526892006396, 0.4898946881294251, 0.5954223871231079, new HaarRectangle(16, 5, 3, 6, -1), new HaarRectangle(17, 5, 1, 6, 3));
            stageNodes.AddHaarFeature(-0.0009632359142415226, 0.3927116990089417, 0.519607424736023, new HaarRectangle(3, 13, 2, 3, -1), new HaarRectangle(3, 14, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0020370010752230883, 0.5613325238227844, 0.4884858131408691, new HaarRectangle(16, 5, 3, 6, -1), new HaarRectangle(17, 5, 1, 6, 3));
            stageNodes.AddHaarFeature(0.0016614829655736685, 0.4472880065441132, 0.5578880906105042, new HaarRectangle(1, 5, 3, 6, -1), new HaarRectangle(2, 5, 1, 6, 3));
            stageNodes.AddHaarFeature(-0.0031188090797513723, 0.3840532898902893, 0.5397477746009827, new HaarRectangle(1, 9, 18, 1, -1), new HaarRectangle(7, 9, 6, 1, 3));
            stageNodes.AddHaarFeature(-0.006400061771273613, 0.5843983888626099, 0.4533218145370483, new HaarRectangle(0, 9, 8, 7, -1), new HaarRectangle(4, 9, 4, 7, 2));
            stageNodes.AddHaarFeature(0.0003131960111204535, 0.5439221858978271, 0.4234727919101715, new HaarRectangle(12, 11, 8, 2, -1), new HaarRectangle(12, 12, 8, 1, 2));
            stageNodes.AddHaarFeature(-0.0182220991700888, 0.1288464963436127, 0.4958404898643494, new HaarRectangle(0, 11, 8, 2, -1), new HaarRectangle(0, 12, 8, 1, 2));
            stageNodes.AddHaarFeature(0.008796924725174904, 0.49512979388237, 0.7153480052947998, new HaarRectangle(9, 13, 2, 3, -1), new HaarRectangle(9, 14, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.004239507019519806, 0.3946599960327148, 0.5194936990737915, new HaarRectangle(4, 10, 12, 4, -1), new HaarRectangle(4, 10, 6, 2, 2), new HaarRectangle(10, 12, 6, 2, 2));
            stageNodes.AddHaarFeature(0.009708627127110958, 0.4897503852844238, 0.6064900159835815, new HaarRectangle(9, 3, 3, 7, -1), new HaarRectangle(10, 3, 1, 7, 3));
            stageNodes.AddHaarFeature(-0.003993417136371136, 0.3245440125465393, 0.5060828924179077, new HaarRectangle(7, 2, 3, 5, -1), new HaarRectangle(8, 2, 1, 5, 3));
            stageNodes.AddHaarFeature(-0.0167850591242313, 0.1581953018903732, 0.5203778743743896, new HaarRectangle(9, 12, 4, 6, -1), new HaarRectangle(11, 12, 2, 3, 2), new HaarRectangle(9, 15, 2, 3, 2));
            stageNodes.AddHaarFeature(0.018272090703249, 0.4680935144424439, 0.6626979112625122, new HaarRectangle(8, 7, 3, 6, -1), new HaarRectangle(9, 7, 1, 6, 3));
            stageNodes.AddHaarFeature(0.00568728381767869, 0.5211697816848755, 0.3512184917926788, new HaarRectangle(15, 4, 4, 2, -1), new HaarRectangle(15, 5, 4, 1, 2));
            stageNodes.AddHaarFeature(-0.0010739039862528443, 0.5768386125564575, 0.4529845118522644, new HaarRectangle(8, 7, 3, 3, -1), new HaarRectangle(9, 7, 1, 3, 3));
            stageNodes.AddHaarFeature(-0.00370938703417778, 0.4507763087749481, 0.5313581228256226, new HaarRectangle(14, 2, 6, 4, -1), new HaarRectangle(14, 4, 6, 2, 2));
            stageNodes.AddHaarFeature(-0.0002111070934915915, 0.5460820198059082, 0.4333376884460449, new HaarRectangle(7, 16, 6, 1, -1), new HaarRectangle(9, 16, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0010670139454305172, 0.5371856093406677, 0.4078390896320343, new HaarRectangle(15, 13, 2, 3, -1), new HaarRectangle(15, 14, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0035943021066486835, 0.4471287131309509, 0.5643836259841919, new HaarRectangle(8, 7, 3, 10, -1), new HaarRectangle(9, 7, 1, 10, 3));
            stageNodes.AddHaarFeature(-0.005177603103220463, 0.4499393105506897, 0.5280330181121826, new HaarRectangle(11, 10, 2, 6, -1), new HaarRectangle(11, 12, 2, 2, 3));
            stageNodes.AddHaarFeature(-0.00025414369883947074, 0.5516173243522644, 0.4407708048820496, new HaarRectangle(6, 10, 4, 1, -1), new HaarRectangle(8, 10, 2, 1, 2));
            stageNodes.AddHaarFeature(0.006352256052196026, 0.5194190144538879, 0.2465227991342545, new HaarRectangle(10, 9, 2, 2, -1), new HaarRectangle(10, 10, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.0004420508048497141, 0.3830705881118774, 0.5139682292938232, new HaarRectangle(8, 9, 2, 2, -1), new HaarRectangle(8, 10, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0007448872784152627, 0.4891090989112854, 0.5974786877632141, new HaarRectangle(12, 7, 2, 2, -1), new HaarRectangle(13, 7, 1, 1, 2), new HaarRectangle(12, 8, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0035116379149258137, 0.7413681745529175, 0.4768764972686768, new HaarRectangle(5, 7, 2, 2, -1), new HaarRectangle(5, 7, 1, 1, 2), new HaarRectangle(6, 8, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0125409103929996, 0.3648819029331207, 0.5252826809883118, new HaarRectangle(13, 0, 3, 14, -1), new HaarRectangle(14, 0, 1, 14, 3));
            stageNodes.AddHaarFeature(0.009493185207247734, 0.5100492835044861, 0.362958699464798, new HaarRectangle(4, 0, 3, 14, -1), new HaarRectangle(5, 0, 1, 14, 3));
            stageNodes.AddHaarFeature(0.0129611501470208, 0.5232442021369934, 0.4333561062812805, new HaarRectangle(13, 4, 3, 14, -1), new HaarRectangle(14, 4, 1, 14, 3));
            stageNodes.AddHaarFeature(0.004720944911241531, 0.4648149013519287, 0.6331052780151367, new HaarRectangle(9, 14, 2, 3, -1), new HaarRectangle(9, 15, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0023119079414755106, 0.5930309891700745, 0.4531058073043823, new HaarRectangle(8, 14, 4, 3, -1), new HaarRectangle(8, 15, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.002826229901984334, 0.3870477974414825, 0.5257101058959961, new HaarRectangle(4, 2, 3, 16, -1), new HaarRectangle(5, 2, 1, 16, 3));
            stageNodes.AddHaarFeature(-0.0014311339473351836, 0.552250325679779, 0.4561854898929596, new HaarRectangle(7, 2, 8, 10, -1), new HaarRectangle(7, 7, 8, 5, 2));
            stageNodes.AddHaarFeature(0.0019378310535103085, 0.4546220898628235, 0.5736966729164124, new HaarRectangle(6, 14, 7, 3, -1), new HaarRectangle(6, 15, 7, 1, 3));
            stageNodes.AddHaarFeature(0.00026343559147790074, 0.5345739126205444, 0.4571875035762787, new HaarRectangle(9, 2, 10, 12, -1), new HaarRectangle(14, 2, 5, 6, 2), new HaarRectangle(9, 8, 5, 6, 2));
            stageNodes.AddHaarFeature(0.0007825752254575491, 0.3967815935611725, 0.5220187902450562, new HaarRectangle(6, 7, 8, 2, -1), new HaarRectangle(6, 8, 8, 1, 2));
            stageNodes.AddHaarFeature(-0.0195504408329725, 0.282964289188385, 0.5243508219718933, new HaarRectangle(8, 13, 4, 6, -1), new HaarRectangle(8, 16, 4, 3, 2));
            stageNodes.AddHaarFeature(0.00043914958951063454, 0.4590066969394684, 0.589909017086029, new HaarRectangle(6, 6, 1, 3, -1), new HaarRectangle(6, 7, 1, 1, 3));
            stageNodes.AddHaarFeature(0.0214520003646612, 0.523141086101532, 0.2855378985404968, new HaarRectangle(16, 2, 4, 6, -1), new HaarRectangle(16, 4, 4, 2, 3));
            stageNodes.AddHaarFeature(0.0005897358059883118, 0.4397256970405579, 0.550642192363739, new HaarRectangle(6, 6, 4, 2, -1), new HaarRectangle(6, 6, 2, 1, 2), new HaarRectangle(8, 7, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.0261576101183891, 0.3135079145431519, 0.5189175009727478, new HaarRectangle(16, 2, 4, 6, -1), new HaarRectangle(16, 4, 4, 2, 3));
            stageNodes.AddHaarFeature(-0.0139598604291677, 0.3213272988796234, 0.5040717720985413, new HaarRectangle(0, 2, 4, 6, -1), new HaarRectangle(0, 4, 4, 2, 3));
            stageNodes.AddHaarFeature(-0.006369901821017265, 0.6387544870376587, 0.4849506914615631, new HaarRectangle(9, 6, 2, 6, -1), new HaarRectangle(9, 6, 1, 6, 2));
            stageNodes.AddHaarFeature(-0.008561382070183754, 0.2759132087230682, 0.5032019019126892, new HaarRectangle(3, 4, 6, 10, -1), new HaarRectangle(3, 9, 6, 5, 2));
            stageNodes.AddHaarFeature(0.000966229010373354, 0.4685640931129456, 0.5834879279136658, new HaarRectangle(9, 5, 2, 6, -1), new HaarRectangle(9, 5, 1, 6, 2));
            stageNodes.AddHaarFeature(0.0007655026856809855, 0.5175207257270813, 0.389642208814621, new HaarRectangle(3, 13, 2, 3, -1), new HaarRectangle(3, 14, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.008183334022760391, 0.2069136947393417, 0.5208122134208679, new HaarRectangle(13, 13, 3, 2, -1), new HaarRectangle(13, 14, 3, 1, 2));
            stageNodes.AddHaarFeature(-0.009397693909704685, 0.6134091019630432, 0.4641222953796387, new HaarRectangle(2, 16, 10, 4, -1), new HaarRectangle(2, 16, 5, 2, 2), new HaarRectangle(7, 18, 5, 2, 2));
            stageNodes.AddHaarFeature(0.004802898038178682, 0.5454108119010925, 0.439521998167038, new HaarRectangle(5, 6, 10, 6, -1), new HaarRectangle(10, 6, 5, 3, 2), new HaarRectangle(5, 9, 5, 3, 2));
            stageNodes.AddHaarFeature(-0.003568056970834732, 0.6344485282897949, 0.4681093990802765, new HaarRectangle(7, 14, 1, 3, -1), new HaarRectangle(7, 15, 1, 1, 3));
            stageNodes.AddHaarFeature(0.0040733120404183865, 0.5292683243751526, 0.4015620052814484, new HaarRectangle(14, 16, 6, 3, -1), new HaarRectangle(14, 17, 6, 1, 3));
            stageNodes.AddHaarFeature(0.0012568129459396005, 0.4392988085746765, 0.5452824831008911, new HaarRectangle(5, 4, 3, 3, -1), new HaarRectangle(5, 5, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.0029065010603517294, 0.5898832082748413, 0.4863379895687103, new HaarRectangle(7, 4, 10, 3, -1), new HaarRectangle(7, 5, 10, 1, 3));
            stageNodes.AddHaarFeature(-0.00244093406945467, 0.4069364964962006, 0.5247421860694885, new HaarRectangle(0, 4, 5, 4, -1), new HaarRectangle(0, 6, 5, 2, 2));
            stageNodes.AddHaarFeature(0.0248307008296251, 0.5182725787162781, 0.3682524859905243, new HaarRectangle(13, 11, 3, 9, -1), new HaarRectangle(13, 14, 3, 3, 3));
            stageNodes.AddHaarFeature(-0.0488540083169937, 0.1307577937841415, 0.496128112077713, new HaarRectangle(4, 11, 3, 9, -1), new HaarRectangle(4, 14, 3, 3, 3));
            stageNodes.AddHaarFeature(-0.0016110379947349427, 0.6421005725860596, 0.4872662127017975, new HaarRectangle(9, 7, 2, 1, -1), new HaarRectangle(9, 7, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0970094799995422, 0.0477693490684032, 0.495098888874054, new HaarRectangle(5, 0, 6, 17, -1), new HaarRectangle(7, 0, 2, 17, 3));
            stageNodes.AddHaarFeature(0.0011209240183234217, 0.4616267085075378, 0.5354745984077454, new HaarRectangle(10, 3, 6, 3, -1), new HaarRectangle(10, 3, 3, 3, 2));
            stageNodes.AddHaarFeature(-0.001306409016251564, 0.626185417175293, 0.4638805985450745, new HaarRectangle(2, 2, 15, 4, -1), new HaarRectangle(7, 2, 5, 4, 3));
            stageNodes.AddHaarFeature(0.000457716203527525, 0.5384417772293091, 0.4646640121936798, new HaarRectangle(8, 2, 8, 2, -1), new HaarRectangle(12, 2, 4, 1, 2), new HaarRectangle(8, 3, 4, 1, 2));
            stageNodes.AddHaarFeature(-0.0006314995116554201, 0.3804047107696533, 0.51302570104599, new HaarRectangle(8, 1, 3, 6, -1), new HaarRectangle(8, 3, 3, 2, 3));
            stageNodes.AddHaarFeature(0.0001450597046641633, 0.4554310142993927, 0.5664461851119995, new HaarRectangle(9, 17, 2, 2, -1), new HaarRectangle(9, 18, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.0164745505899191, 0.6596958041191101, 0.4715859889984131, new HaarRectangle(0, 0, 2, 14, -1), new HaarRectangle(1, 0, 1, 14, 2));
            stageNodes.AddHaarFeature(0.0133695797994733, 0.519546627998352, 0.3035964965820313, new HaarRectangle(12, 0, 7, 3, -1), new HaarRectangle(12, 1, 7, 1, 3));
            stageNodes.AddHaarFeature(0.00010271780047332868, 0.522917628288269, 0.4107066094875336, new HaarRectangle(1, 14, 1, 2, -1), new HaarRectangle(1, 15, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0055311559699475765, 0.6352887749671936, 0.4960907101631165, new HaarRectangle(14, 12, 2, 8, -1), new HaarRectangle(15, 12, 1, 4, 2), new HaarRectangle(14, 16, 1, 4, 2));
            stageNodes.AddHaarFeature(-0.0026187049224972725, 0.3824546039104462, 0.5140984058380127, new HaarRectangle(1, 0, 7, 3, -1), new HaarRectangle(1, 1, 7, 1, 3));
            stageNodes.AddHaarFeature(0.005083426833152771, 0.4950439929962158, 0.6220818758010864, new HaarRectangle(14, 12, 2, 8, -1), new HaarRectangle(15, 12, 1, 4, 2), new HaarRectangle(14, 16, 1, 4, 2));
            stageNodes.AddHaarFeature(0.0798181593418121, 0.4952335953712463, 0.1322475969791412, new HaarRectangle(6, 0, 8, 12, -1), new HaarRectangle(6, 0, 4, 6, 2), new HaarRectangle(10, 6, 4, 6, 2));
            stageNodes.AddHaarFeature(-0.0992265865206718, 0.7542728781700134, 0.5008416771888733, new HaarRectangle(6, 1, 8, 9, -1), new HaarRectangle(6, 4, 8, 3, 3));
            stageNodes.AddHaarFeature(-0.0006517401780001819, 0.3699302971363068, 0.5130121111869812, new HaarRectangle(5, 2, 2, 2, -1), new HaarRectangle(5, 3, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.018996849656105, 0.6689178943634033, 0.4921202957630158, new HaarRectangle(13, 14, 6, 6, -1), new HaarRectangle(16, 14, 3, 3, 2), new HaarRectangle(13, 17, 3, 3, 2));
            stageNodes.AddHaarFeature(0.0173468999564648, 0.4983300864696503, 0.1859198063611984, new HaarRectangle(0, 17, 20, 2, -1), new HaarRectangle(0, 17, 10, 1, 2), new HaarRectangle(10, 18, 10, 1, 2));
            stageNodes.AddHaarFeature(0.0005508210160769522, 0.4574424028396606, 0.5522121787071228, new HaarRectangle(10, 3, 2, 6, -1), new HaarRectangle(11, 3, 1, 3, 2), new HaarRectangle(10, 6, 1, 3, 2));
            stageNodes.AddHaarFeature(0.002005605027079582, 0.5131744742393494, 0.3856469988822937, new HaarRectangle(5, 12, 6, 2, -1), new HaarRectangle(8, 12, 3, 2, 2));
            stageNodes.AddHaarFeature(-0.007768819108605386, 0.4361700117588043, 0.5434309244155884, new HaarRectangle(10, 7, 6, 13, -1), new HaarRectangle(10, 7, 3, 13, 2));
            stageNodes.AddHaarFeature(0.0508782789111137, 0.4682720899581909, 0.6840639710426331, new HaarRectangle(5, 15, 10, 5, -1), new HaarRectangle(10, 15, 5, 5, 2));
            stageNodes.AddHaarFeature(-0.0022901780903339386, 0.4329245090484619, 0.5306099057197571, new HaarRectangle(10, 4, 4, 10, -1), new HaarRectangle(10, 4, 2, 10, 2));
            stageNodes.AddHaarFeature(-0.00015715380141045898, 0.5370057225227356, 0.4378164112567902, new HaarRectangle(5, 7, 2, 1, -1), new HaarRectangle(6, 7, 1, 1, 2));
            stageNodes.AddHaarFeature(0.1051924005150795, 0.5137274265289307, 0.0673614665865898, new HaarRectangle(10, 3, 6, 7, -1), new HaarRectangle(10, 3, 3, 7, 2));
            stageNodes.AddHaarFeature(0.002719891956076026, 0.4112060964107513, 0.5255665183067322, new HaarRectangle(4, 3, 6, 7, -1), new HaarRectangle(7, 3, 3, 7, 2));
            stageNodes.AddHaarFeature(0.0483377799391747, 0.5404623746871948, 0.4438967108726502, new HaarRectangle(1, 7, 18, 5, -1), new HaarRectangle(7, 7, 6, 5, 3));
            stageNodes.AddHaarFeature(0.0009570376132614911, 0.4355969130992889, 0.5399510860443115, new HaarRectangle(3, 17, 4, 3, -1), new HaarRectangle(5, 17, 2, 3, 2));
            stageNodes.AddHaarFeature(-0.0253712590783834, 0.5995175242424011, 0.5031024813652039, new HaarRectangle(8, 14, 12, 6, -1), new HaarRectangle(14, 14, 6, 3, 2), new HaarRectangle(8, 17, 6, 3, 2));
            stageNodes.AddHaarFeature(0.0524579510092735, 0.4950287938117981, 0.1398351043462753, new HaarRectangle(0, 13, 20, 4, -1), new HaarRectangle(0, 13, 10, 2, 2), new HaarRectangle(10, 15, 10, 2, 2));
            stageNodes.AddHaarFeature(-0.0123656298965216, 0.639729917049408, 0.496410608291626, new HaarRectangle(4, 5, 14, 2, -1), new HaarRectangle(11, 5, 7, 1, 2), new HaarRectangle(4, 6, 7, 1, 2));
            stageNodes.AddHaarFeature(-0.1458971947431564, 0.1001669988036156, 0.494632214307785, new HaarRectangle(1, 2, 10, 12, -1), new HaarRectangle(1, 2, 5, 6, 2), new HaarRectangle(6, 8, 5, 6, 2));
            stageNodes.AddHaarFeature(-0.0159086007624865, 0.3312329947948456, 0.5208340883255005, new HaarRectangle(6, 1, 14, 3, -1), new HaarRectangle(6, 2, 14, 1, 3));
            stageNodes.AddHaarFeature(0.00039486068999394774, 0.4406363964080811, 0.5426102876663208, new HaarRectangle(8, 16, 2, 3, -1), new HaarRectangle(8, 17, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0052454001270234585, 0.2799589931964874, 0.5189967155456543, new HaarRectangle(9, 17, 3, 2, -1), new HaarRectangle(10, 17, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.005042179953306913, 0.6987580060958862, 0.4752142131328583, new HaarRectangle(5, 15, 4, 2, -1), new HaarRectangle(5, 15, 2, 1, 2), new HaarRectangle(7, 16, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0029812189750373363, 0.4983288943767548, 0.6307479739189148, new HaarRectangle(10, 15, 1, 3, -1), new HaarRectangle(10, 16, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.007288430817425251, 0.298233300447464, 0.5026869773864746, new HaarRectangle(8, 16, 4, 4, -1), new HaarRectangle(8, 16, 2, 2, 2), new HaarRectangle(10, 18, 2, 2, 2));
            stageNodes.AddHaarFeature(0.0015094350092113018, 0.5308442115783691, 0.3832970857620239, new HaarRectangle(6, 11, 8, 6, -1), new HaarRectangle(6, 14, 8, 3, 2));
            stageNodes.AddHaarFeature(-0.009334079921245575, 0.2037964016199112, 0.4969817101955414, new HaarRectangle(2, 13, 5, 2, -1), new HaarRectangle(2, 14, 5, 1, 2));
            stageNodes.AddHaarFeature(0.0286671407520771, 0.5025696754455566, 0.6928027272224426, new HaarRectangle(13, 14, 6, 6, -1), new HaarRectangle(16, 14, 3, 3, 2), new HaarRectangle(13, 17, 3, 3, 2));
            stageNodes.AddHaarFeature(0.1701968014240265, 0.4960052967071533, 0.1476442962884903, new HaarRectangle(1, 9, 18, 4, -1), new HaarRectangle(7, 9, 6, 4, 3));
            stageNodes.AddHaarFeature(-0.003261447884142399, 0.5603063702583313, 0.4826056063175201, new HaarRectangle(13, 14, 6, 6, -1), new HaarRectangle(16, 14, 3, 3, 2), new HaarRectangle(13, 17, 3, 3, 2));
            stageNodes.AddHaarFeature(0.0005576927796937525, 0.5205562114715576, 0.4129633009433746, new HaarRectangle(0, 2, 1, 6, -1), new HaarRectangle(0, 4, 1, 2, 3));
            stageNodes.AddHaarFeature(0.3625833988189697, 0.5221652984619141, 0.3768612146377564, new HaarRectangle(5, 0, 15, 20, -1), new HaarRectangle(5, 10, 15, 10, 2));
            stageNodes.AddHaarFeature(-0.0116151301190257, 0.6022682785987854, 0.4637489914894104, new HaarRectangle(1, 14, 6, 6, -1), new HaarRectangle(1, 14, 3, 3, 2), new HaarRectangle(4, 17, 3, 3, 2));
            stageNodes.AddHaarFeature(-0.004079519771039486, 0.4070447087287903, 0.5337479114532471, new HaarRectangle(8, 14, 4, 6, -1), new HaarRectangle(10, 14, 2, 3, 2), new HaarRectangle(8, 17, 2, 3, 2));
            stageNodes.AddHaarFeature(0.0005720430053770542, 0.4601835012435913, 0.5900393128395081, new HaarRectangle(7, 11, 2, 1, -1), new HaarRectangle(8, 11, 1, 1, 2));
            stageNodes.AddHaarFeature(0.000675433489959687, 0.5398252010345459, 0.4345428943634033, new HaarRectangle(9, 17, 3, 2, -1), new HaarRectangle(10, 17, 1, 2, 3));
            stageNodes.AddHaarFeature(0.0006329569732770324, 0.5201563239097595, 0.4051358997821808, new HaarRectangle(8, 17, 3, 2, -1), new HaarRectangle(9, 17, 1, 2, 3));
            stageNodes.AddHaarFeature(0.00124353205319494, 0.4642387926578522, 0.5547441244125366, new HaarRectangle(12, 14, 4, 6, -1), new HaarRectangle(14, 14, 2, 3, 2), new HaarRectangle(12, 17, 2, 3, 2));
            stageNodes.AddHaarFeature(-0.004736385773867369, 0.6198567152023315, 0.4672552049160004, new HaarRectangle(4, 14, 4, 6, -1), new HaarRectangle(4, 14, 2, 3, 2), new HaarRectangle(6, 17, 2, 3, 2));
            stageNodes.AddHaarFeature(-0.006465846206992865, 0.6837332844734192, 0.5019000768661499, new HaarRectangle(13, 14, 2, 6, -1), new HaarRectangle(14, 14, 1, 3, 2), new HaarRectangle(13, 17, 1, 3, 2));
            stageNodes.AddHaarFeature(0.000350173213519156, 0.4344803094863892, 0.5363622903823853, new HaarRectangle(5, 14, 2, 6, -1), new HaarRectangle(5, 14, 1, 3, 2), new HaarRectangle(6, 17, 1, 3, 2));
            stageNodes.AddHaarFeature(0.00015754920605104417, 0.4760079085826874, 0.5732020735740662, new HaarRectangle(7, 0, 6, 12, -1), new HaarRectangle(7, 4, 6, 4, 3));
            stageNodes.AddHaarFeature(0.009977436624467373, 0.5090985894203186, 0.3635039925575256, new HaarRectangle(0, 7, 12, 2, -1), new HaarRectangle(4, 7, 4, 2, 3));
            stageNodes.AddHaarFeature(-0.0004146452993154526, 0.5570064783096313, 0.4593802094459534, new HaarRectangle(10, 3, 3, 13, -1), new HaarRectangle(11, 3, 1, 13, 3));
            stageNodes.AddHaarFeature(-0.00035888899583369493, 0.5356845855712891, 0.4339134991168976, new HaarRectangle(7, 3, 3, 13, -1), new HaarRectangle(8, 3, 1, 13, 3));
            stageNodes.AddHaarFeature(0.0004046325047966093, 0.4439803063869476, 0.5436776876449585, new HaarRectangle(10, 8, 6, 3, -1), new HaarRectangle(10, 9, 6, 1, 3));
            stageNodes.AddHaarFeature(-0.0008218478760682046, 0.4042294919490814, 0.5176299214363098, new HaarRectangle(3, 11, 3, 2, -1), new HaarRectangle(4, 11, 1, 2, 3));
            stageNodes.AddHaarFeature(0.005946741905063391, 0.4927651882171631, 0.5633779764175415, new HaarRectangle(13, 12, 6, 8, -1), new HaarRectangle(16, 12, 3, 4, 2), new HaarRectangle(13, 16, 3, 4, 2));
            stageNodes.AddHaarFeature(-0.0217533893883228, 0.8006293773651123, 0.480084091424942, new HaarRectangle(7, 6, 6, 5, -1), new HaarRectangle(9, 6, 2, 5, 3));
            stageNodes.AddHaarFeature(-0.0145403798669577, 0.3946054875850678, 0.5182222723960876, new HaarRectangle(17, 11, 2, 7, -1), new HaarRectangle(17, 11, 1, 7, 2));
            stageNodes.AddHaarFeature(-0.0405107699334621, 0.0213249903172255, 0.4935792982578278, new HaarRectangle(3, 13, 8, 2, -1), new HaarRectangle(7, 13, 4, 2, 2));
            stageNodes.AddHaarFeature(-0.0005845826817676425, 0.4012795984745026, 0.5314025282859802, new HaarRectangle(6, 9, 8, 3, -1), new HaarRectangle(6, 10, 8, 1, 3));
            stageNodes.AddHaarFeature(0.005515180062502623, 0.4642418920993805, 0.5896260738372803, new HaarRectangle(4, 3, 4, 3, -1), new HaarRectangle(4, 4, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.006062622182071209, 0.6502159237861633, 0.5016477704048157, new HaarRectangle(11, 3, 4, 3, -1), new HaarRectangle(11, 4, 4, 1, 3));
            stageNodes.AddHaarFeature(0.0945358425378799, 0.5264708995819092, 0.4126827120780945, new HaarRectangle(1, 4, 17, 12, -1), new HaarRectangle(1, 8, 17, 4, 3));
            stageNodes.AddHaarFeature(0.004731505177915096, 0.4879199862480164, 0.5892447829246521, new HaarRectangle(11, 3, 4, 3, -1), new HaarRectangle(11, 4, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.0005257147131487727, 0.391728013753891, 0.5189412832260132, new HaarRectangle(4, 8, 6, 3, -1), new HaarRectangle(4, 9, 6, 1, 3));
            stageNodes.AddHaarFeature(-0.0025464049540460114, 0.5837599039077759, 0.498570591211319, new HaarRectangle(12, 3, 5, 3, -1), new HaarRectangle(12, 4, 5, 1, 3));
            stageNodes.AddHaarFeature(-0.0260756891220808, 0.1261983960866928, 0.4955821931362152, new HaarRectangle(1, 11, 2, 7, -1), new HaarRectangle(2, 11, 1, 7, 2));
            stageNodes.AddHaarFeature(-0.00547797093167901, 0.5722513794898987, 0.5010265707969666, new HaarRectangle(15, 12, 2, 8, -1), new HaarRectangle(16, 12, 1, 4, 2), new HaarRectangle(15, 16, 1, 4, 2));
            stageNodes.AddHaarFeature(0.005133774131536484, 0.527326226234436, 0.4226376116275787, new HaarRectangle(4, 8, 11, 3, -1), new HaarRectangle(4, 9, 11, 1, 3));
            stageNodes.AddHaarFeature(0.000479449809063226, 0.4450066983699799, 0.5819587111473083, new HaarRectangle(9, 13, 6, 2, -1), new HaarRectangle(12, 13, 3, 1, 2), new HaarRectangle(9, 14, 3, 1, 2));
            stageNodes.AddHaarFeature(-0.0021114079281687737, 0.5757653117179871, 0.451171487569809, new HaarRectangle(6, 13, 4, 3, -1), new HaarRectangle(6, 14, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.0131799904629588, 0.1884381026029587, 0.5160734057426453, new HaarRectangle(9, 12, 3, 3, -1), new HaarRectangle(10, 12, 1, 3, 3));
            stageNodes.AddHaarFeature(-0.004796809982508421, 0.6589789986610413, 0.4736118912696838, new HaarRectangle(5, 3, 3, 3, -1), new HaarRectangle(5, 4, 3, 1, 3));
            stageNodes.AddHaarFeature(0.0067483168095350266, 0.5259429812431335, 0.3356395065784454, new HaarRectangle(9, 4, 2, 3, -1), new HaarRectangle(9, 5, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0014623369788751006, 0.5355271100997925, 0.4264092147350311, new HaarRectangle(0, 2, 16, 3, -1), new HaarRectangle(0, 3, 16, 1, 3));
            stageNodes.AddHaarFeature(0.004764515906572342, 0.5034406781196594, 0.5786827802658081, new HaarRectangle(15, 12, 2, 8, -1), new HaarRectangle(16, 12, 1, 4, 2), new HaarRectangle(15, 16, 1, 4, 2));
            stageNodes.AddHaarFeature(0.0068066660314798355, 0.475660502910614, 0.6677829027175903, new HaarRectangle(3, 12, 2, 8, -1), new HaarRectangle(3, 12, 1, 4, 2), new HaarRectangle(4, 16, 1, 4, 2));
            stageNodes.AddHaarFeature(0.0036608621012419462, 0.5369611978530884, 0.4311546981334686, new HaarRectangle(14, 13, 3, 6, -1), new HaarRectangle(14, 15, 3, 2, 3));
            stageNodes.AddHaarFeature(0.0214496403932571, 0.4968641996383667, 0.1888816058635712, new HaarRectangle(3, 13, 3, 6, -1), new HaarRectangle(3, 15, 3, 2, 3));
            stageNodes.AddHaarFeature(0.004167890176177025, 0.4930733144283295, 0.5815368890762329, new HaarRectangle(6, 5, 10, 2, -1), new HaarRectangle(11, 5, 5, 1, 2), new HaarRectangle(6, 6, 5, 1, 2));
            stageNodes.AddHaarFeature(0.008646756410598755, 0.5205205082893372, 0.4132595062255859, new HaarRectangle(2, 14, 14, 6, -1), new HaarRectangle(2, 17, 14, 3, 2));
            stageNodes.AddHaarFeature(-0.0003611407882999629, 0.5483555197715759, 0.4800927937030792, new HaarRectangle(10, 14, 1, 3, -1), new HaarRectangle(10, 15, 1, 1, 3));
            stageNodes.AddHaarFeature(0.00108087295666337, 0.4689902067184448, 0.6041421294212341, new HaarRectangle(4, 16, 2, 2, -1), new HaarRectangle(4, 16, 1, 1, 2), new HaarRectangle(5, 17, 1, 1, 2));
            stageNodes.AddHaarFeature(0.005771995987743139, 0.5171142220497131, 0.3053277134895325, new HaarRectangle(10, 6, 2, 3, -1), new HaarRectangle(10, 7, 2, 1, 3));
            stageNodes.AddHaarFeature(0.001572077046148479, 0.5219978094100952, 0.4178803861141205, new HaarRectangle(0, 17, 20, 2, -1), new HaarRectangle(0, 17, 10, 1, 2), new HaarRectangle(10, 18, 10, 1, 2));
            stageNodes.AddHaarFeature(-0.0019307859474793077, 0.5860369801521301, 0.4812920093536377, new HaarRectangle(13, 6, 1, 3, -1), new HaarRectangle(13, 7, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.007892627269029617, 0.1749276965856552, 0.497173398733139, new HaarRectangle(8, 13, 3, 2, -1), new HaarRectangle(9, 13, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.002222467912361026, 0.434258908033371, 0.521284818649292, new HaarRectangle(12, 2, 3, 3, -1), new HaarRectangle(13, 2, 1, 3, 3));
            stageNodes.AddHaarFeature(0.001901198993436992, 0.4765186905860901, 0.689205527305603, new HaarRectangle(3, 18, 2, 2, -1), new HaarRectangle(3, 18, 1, 1, 2), new HaarRectangle(4, 19, 1, 1, 2));
            stageNodes.AddHaarFeature(0.0027576119173318148, 0.5262191295623779, 0.4337486028671265, new HaarRectangle(9, 16, 3, 4, -1), new HaarRectangle(10, 16, 1, 4, 3));
            stageNodes.AddHaarFeature(0.005178744904696941, 0.4804069101810455, 0.7843729257583618, new HaarRectangle(6, 6, 1, 3, -1), new HaarRectangle(6, 7, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.0009027334162965418, 0.412084698677063, 0.5353423953056335, new HaarRectangle(13, 1, 5, 2, -1), new HaarRectangle(13, 2, 5, 1, 2));
            stageNodes.AddHaarFeature(0.005179795902222395, 0.4740372896194458, 0.6425960063934326, new HaarRectangle(7, 14, 6, 2, -1), new HaarRectangle(7, 14, 3, 1, 2), new HaarRectangle(10, 15, 3, 1, 2));
            stageNodes.AddHaarFeature(-0.0101140001788735, 0.2468792051076889, 0.5175017714500427, new HaarRectangle(11, 3, 3, 4, -1), new HaarRectangle(12, 3, 1, 4, 3));
            stageNodes.AddHaarFeature(-0.0186170600354671, 0.5756294131278992, 0.4628978967666626, new HaarRectangle(1, 13, 12, 6, -1), new HaarRectangle(5, 13, 4, 6, 3));
            stageNodes.AddHaarFeature(0.0059225959703326225, 0.5169625878334045, 0.3214271068572998, new HaarRectangle(14, 11, 5, 2, -1), new HaarRectangle(14, 12, 5, 1, 2));
            stageNodes.AddHaarFeature(-0.006294507998973131, 0.3872014880180359, 0.5141636729240417, new HaarRectangle(2, 15, 14, 4, -1), new HaarRectangle(2, 15, 7, 2, 2), new HaarRectangle(9, 17, 7, 2, 2));
            stageNodes.AddHaarFeature(0.0065353019163012505, 0.4853048920631409, 0.6310489773750305, new HaarRectangle(3, 7, 14, 2, -1), new HaarRectangle(10, 7, 7, 1, 2), new HaarRectangle(3, 8, 7, 1, 2));
            stageNodes.AddHaarFeature(0.0010878399480134249, 0.5117315053939819, 0.3723258972167969, new HaarRectangle(1, 11, 4, 2, -1), new HaarRectangle(1, 12, 4, 1, 2));
            stageNodes.AddHaarFeature(-0.0225422400981188, 0.5692740082740784, 0.4887112975120544, new HaarRectangle(14, 0, 6, 14, -1), new HaarRectangle(16, 0, 2, 14, 3));
            stageNodes.AddHaarFeature(-0.003006566083058715, 0.2556012868881226, 0.5003992915153503, new HaarRectangle(4, 11, 1, 3, -1), new HaarRectangle(4, 12, 1, 1, 3));
            stageNodes.AddHaarFeature(0.007474127225577832, 0.4810872972011566, 0.5675926804542542, new HaarRectangle(14, 0, 6, 14, -1), new HaarRectangle(16, 0, 2, 14, 3));
            stageNodes.AddHaarFeature(0.0261623207479715, 0.4971194863319397, 0.1777237057685852, new HaarRectangle(1, 10, 3, 7, -1), new HaarRectangle(2, 10, 1, 7, 3));
            stageNodes.AddHaarFeature(0.0009435273823328317, 0.4940010905265808, 0.549125075340271, new HaarRectangle(8, 12, 9, 2, -1), new HaarRectangle(8, 13, 9, 1, 2));
            stageNodes.AddHaarFeature(0.0333632417023182, 0.5007612109184265, 0.2790724039077759, new HaarRectangle(0, 6, 20, 1, -1), new HaarRectangle(10, 6, 10, 1, 2));
            stageNodes.AddHaarFeature(-0.0151186501607299, 0.7059578895568848, 0.4973031878471375, new HaarRectangle(8, 4, 4, 4, -1), new HaarRectangle(8, 4, 2, 4, 2));
            stageNodes.AddHaarFeature(0.0009864894673228264, 0.5128620266914368, 0.3776761889457703, new HaarRectangle(0, 0, 2, 2, -1), new HaarRectangle(0, 1, 2, 1, 2));
            stageNodeCounts.Add(stageNodes.Count - index); index = stageNodes.Count;

            stageThresholds.Add(105.76110076904297);
            stageNodes.AddHaarFeature(-0.0951507985591888, 0.6470757126808167, 0.4017286896705627, new HaarRectangle(5, 3, 10, 9, -1), new HaarRectangle(5, 6, 10, 3, 3));
            stageNodes.AddHaarFeature(0.006270234007388353, 0.399982213973999, 0.574644923210144, new HaarRectangle(15, 2, 4, 10, -1), new HaarRectangle(15, 2, 2, 10, 2));
            stageNodes.AddHaarFeature(0.000300180894555524, 0.355877012014389, 0.5538809895515442, new HaarRectangle(8, 2, 2, 7, -1), new HaarRectangle(9, 2, 1, 7, 2));
            stageNodes.AddHaarFeature(0.0011757409665733576, 0.425653487443924, 0.5382617712020874, new HaarRectangle(7, 4, 12, 1, -1), new HaarRectangle(11, 4, 4, 1, 3));
            stageNodes.AddHaarFeature(0.00004423526843311265, 0.3682908117771149, 0.5589926838874817, new HaarRectangle(3, 4, 9, 1, -1), new HaarRectangle(6, 4, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.000029936920327600092, 0.5452470183372498, 0.4020367860794067, new HaarRectangle(15, 10, 1, 4, -1), new HaarRectangle(15, 12, 1, 2, 2));
            stageNodes.AddHaarFeature(0.003007319988682866, 0.5239058136940002, 0.3317843973636627, new HaarRectangle(4, 10, 6, 4, -1), new HaarRectangle(7, 10, 3, 4, 2));
            stageNodes.AddHaarFeature(-0.0105138896033168, 0.4320689141750336, 0.5307983756065369, new HaarRectangle(15, 9, 1, 6, -1), new HaarRectangle(15, 12, 1, 3, 2));
            stageNodes.AddHaarFeature(0.008347682654857635, 0.4504637122154236, 0.6453298926353455, new HaarRectangle(7, 17, 6, 3, -1), new HaarRectangle(7, 18, 6, 1, 3));
            stageNodes.AddHaarFeature(-0.0031492270063608885, 0.4313425123691559, 0.5370525121688843, new HaarRectangle(14, 3, 2, 16, -1), new HaarRectangle(15, 3, 1, 8, 2), new HaarRectangle(14, 11, 1, 8, 2));
            stageNodes.AddHaarFeature(-0.00001443564997316571, 0.5326603055000305, 0.381797194480896, new HaarRectangle(4, 9, 1, 6, -1), new HaarRectangle(4, 12, 1, 3, 2));
            stageNodes.AddHaarFeature(-0.00042855090578086674, 0.430516391992569, 0.5382009744644165, new HaarRectangle(12, 1, 5, 2, -1), new HaarRectangle(12, 2, 5, 1, 2));
            stageNodes.AddHaarFeature(0.00015062429883982986, 0.4235970973968506, 0.5544965267181396, new HaarRectangle(6, 18, 4, 2, -1), new HaarRectangle(6, 18, 2, 1, 2), new HaarRectangle(8, 19, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0715598315000534, 0.5303059816360474, 0.2678802907466888, new HaarRectangle(2, 4, 16, 10, -1), new HaarRectangle(10, 4, 8, 5, 2), new HaarRectangle(2, 9, 8, 5, 2));
            stageNodes.AddHaarFeature(0.0008409518050029874, 0.3557108938694, 0.5205433964729309, new HaarRectangle(6, 5, 1, 10, -1), new HaarRectangle(6, 10, 1, 5, 2));
            stageNodes.AddHaarFeature(0.0629865005612373, 0.5225362777709961, 0.2861376106739044, new HaarRectangle(4, 8, 15, 2, -1), new HaarRectangle(9, 8, 5, 2, 3));
            stageNodes.AddHaarFeature(-0.0033798629883676767, 0.3624185919761658, 0.5201697945594788, new HaarRectangle(1, 8, 15, 2, -1), new HaarRectangle(6, 8, 5, 2, 3));
            stageNodes.AddHaarFeature(-0.00011810739670181648, 0.547447681427002, 0.3959893882274628, new HaarRectangle(9, 5, 3, 6, -1), new HaarRectangle(9, 7, 3, 2, 3));
            stageNodes.AddHaarFeature(-0.0005450560129247606, 0.3740422129631043, 0.5215715765953064, new HaarRectangle(5, 7, 8, 2, -1), new HaarRectangle(9, 7, 4, 2, 2));
            stageNodes.AddHaarFeature(-0.0018454910023137927, 0.5893052220344543, 0.4584448933601379, new HaarRectangle(9, 11, 2, 3, -1), new HaarRectangle(9, 12, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.00043832371011376386, 0.4084582030773163, 0.5385351181030273, new HaarRectangle(1, 0, 16, 3, -1), new HaarRectangle(1, 1, 16, 1, 3));
            stageNodes.AddHaarFeature(-0.002400083001703024, 0.377745509147644, 0.5293580293655396, new HaarRectangle(11, 2, 7, 2, -1), new HaarRectangle(11, 3, 7, 1, 2));
            stageNodes.AddHaarFeature(-0.0987957417964935, 0.2963612079620361, 0.5070089101791382, new HaarRectangle(5, 1, 10, 18, -1), new HaarRectangle(5, 7, 10, 6, 3));
            stageNodes.AddHaarFeature(0.0031798239797353745, 0.4877632856369019, 0.6726443767547607, new HaarRectangle(17, 4, 3, 2, -1), new HaarRectangle(18, 4, 1, 2, 3));
            stageNodes.AddHaarFeature(0.00032406419632025063, 0.4366911053657532, 0.5561109781265259, new HaarRectangle(8, 13, 1, 3, -1), new HaarRectangle(8, 14, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.0325472503900528, 0.31281578540802, 0.5308616161346436, new HaarRectangle(3, 14, 14, 6, -1), new HaarRectangle(3, 16, 14, 2, 3));
            stageNodes.AddHaarFeature(-0.007756113074719906, 0.6560224890708923, 0.4639872014522553, new HaarRectangle(0, 2, 3, 4, -1), new HaarRectangle(1, 2, 1, 4, 3));
            stageNodes.AddHaarFeature(0.0160272493958473, 0.5172680020332336, 0.3141897916793823, new HaarRectangle(12, 1, 5, 2, -1), new HaarRectangle(12, 2, 5, 1, 2));
            stageNodes.AddHaarFeature(0.00000710023505234858, 0.4084446132183075, 0.5336294770240784, new HaarRectangle(3, 1, 5, 2, -1), new HaarRectangle(3, 2, 5, 1, 2));
            stageNodes.AddHaarFeature(0.007342280820012093, 0.4966922104358673, 0.660346508026123, new HaarRectangle(10, 13, 2, 3, -1), new HaarRectangle(10, 14, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0016970280557870865, 0.5908237099647522, 0.4500182867050171, new HaarRectangle(8, 13, 2, 3, -1), new HaarRectangle(8, 14, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0024118260480463505, 0.5315160751342773, 0.3599720895290375, new HaarRectangle(14, 12, 2, 3, -1), new HaarRectangle(14, 13, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.005530093796551228, 0.2334040999412537, 0.4996814131736755, new HaarRectangle(7, 2, 2, 3, -1), new HaarRectangle(7, 3, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0026478730142116547, 0.5880935788154602, 0.4684734046459198, new HaarRectangle(5, 6, 10, 4, -1), new HaarRectangle(10, 6, 5, 2, 2), new HaarRectangle(5, 8, 5, 2, 2));
            stageNodes.AddHaarFeature(0.0112956296652555, 0.4983777105808258, 0.1884590983390808, new HaarRectangle(9, 13, 1, 6, -1), new HaarRectangle(9, 16, 1, 3, 2));
            stageNodes.AddHaarFeature(-0.000669528788421303, 0.5872138142585754, 0.4799019992351532, new HaarRectangle(10, 12, 2, 2, -1), new HaarRectangle(11, 12, 1, 1, 2), new HaarRectangle(10, 13, 1, 1, 2));
            stageNodes.AddHaarFeature(0.0014410680159926414, 0.5131189227104187, 0.350101113319397, new HaarRectangle(4, 12, 2, 3, -1), new HaarRectangle(4, 13, 2, 1, 3));
            stageNodes.AddHaarFeature(0.002463787095621228, 0.5339372158050537, 0.4117639064788818, new HaarRectangle(14, 4, 6, 6, -1), new HaarRectangle(14, 6, 6, 2, 3));
            stageNodes.AddHaarFeature(0.0003311451873742044, 0.4313383102416992, 0.5398246049880981, new HaarRectangle(8, 17, 2, 3, -1), new HaarRectangle(8, 18, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.0335572697222233, 0.26753368973732, 0.5179154872894287, new HaarRectangle(16, 4, 4, 6, -1), new HaarRectangle(16, 6, 4, 2, 3));
            stageNodes.AddHaarFeature(0.0185394193977118, 0.4973869919776917, 0.2317177057266235, new HaarRectangle(0, 4, 4, 6, -1), new HaarRectangle(0, 6, 4, 2, 3));
            stageNodes.AddHaarFeature(-0.00029698139405809343, 0.552970826625824, 0.4643664062023163, new HaarRectangle(14, 6, 2, 3, -1), new HaarRectangle(14, 6, 1, 3, 2));
            stageNodes.AddHaarFeature(-0.00045577259152196353, 0.5629584193229675, 0.4469191133975983, new HaarRectangle(4, 9, 8, 1, -1), new HaarRectangle(8, 9, 4, 1, 2));
            stageNodes.AddHaarFeature(-0.0101589802652597, 0.6706212759017944, 0.4925918877124786, new HaarRectangle(8, 12, 4, 3, -1), new HaarRectangle(8, 13, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.000022413829356082715, 0.5239421725273132, 0.3912901878356934, new HaarRectangle(5, 12, 10, 6, -1), new HaarRectangle(5, 14, 10, 2, 3));
            stageNodes.AddHaarFeature(0.00007203496352303773, 0.4799438118934631, 0.5501788854598999, new HaarRectangle(11, 12, 1, 2, -1), new HaarRectangle(11, 13, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.006926720961928368, 0.6930009722709656, 0.4698084890842438, new HaarRectangle(8, 15, 4, 2, -1), new HaarRectangle(8, 16, 4, 1, 2));
            stageNodes.AddHaarFeature(-0.007699783891439439, 0.409962385892868, 0.5480883121490479, new HaarRectangle(6, 9, 8, 8, -1), new HaarRectangle(10, 9, 4, 4, 2), new HaarRectangle(6, 13, 4, 4, 2));
            stageNodes.AddHaarFeature(-0.007313054986298085, 0.3283475935459137, 0.5057886242866516, new HaarRectangle(7, 12, 4, 6, -1), new HaarRectangle(7, 12, 2, 3, 2), new HaarRectangle(9, 15, 2, 3, 2));
            stageNodes.AddHaarFeature(0.0019650589674711227, 0.4978047013282776, 0.6398249864578247, new HaarRectangle(10, 11, 3, 1, -1), new HaarRectangle(11, 11, 1, 1, 3));
            stageNodes.AddHaarFeature(0.007164760027080774, 0.4661160111427307, 0.6222137212753296, new HaarRectangle(9, 7, 2, 10, -1), new HaarRectangle(9, 7, 1, 5, 2), new HaarRectangle(10, 12, 1, 5, 2));
            stageNodes.AddHaarFeature(-0.0240786392241716, 0.2334644943475723, 0.5222162008285522, new HaarRectangle(8, 0, 6, 6, -1), new HaarRectangle(10, 0, 2, 6, 3));
            stageNodes.AddHaarFeature(-0.0210279691964388, 0.1183653995394707, 0.4938226044178009, new HaarRectangle(3, 11, 2, 6, -1), new HaarRectangle(3, 13, 2, 2, 3));
            stageNodes.AddHaarFeature(0.00036017020465806127, 0.5325019955635071, 0.4116711020469666, new HaarRectangle(16, 12, 1, 2, -1), new HaarRectangle(16, 13, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0172197297215462, 0.6278762221336365, 0.4664269089698792, new HaarRectangle(1, 14, 6, 6, -1), new HaarRectangle(1, 14, 3, 3, 2), new HaarRectangle(4, 17, 3, 3, 2));
            stageNodes.AddHaarFeature(-0.007867214269936085, 0.3403415083885193, 0.5249736905097961, new HaarRectangle(13, 1, 3, 6, -1), new HaarRectangle(14, 1, 1, 6, 3));
            stageNodes.AddHaarFeature(-0.000447773898486048, 0.3610411882400513, 0.5086259245872498, new HaarRectangle(8, 8, 2, 2, -1), new HaarRectangle(8, 9, 2, 1, 2));
            stageNodes.AddHaarFeature(0.005548601038753986, 0.4884265959262848, 0.6203498244285583, new HaarRectangle(9, 9, 3, 3, -1), new HaarRectangle(10, 9, 1, 3, 3));
            stageNodes.AddHaarFeature(-0.00694611482322216, 0.262593001127243, 0.5011097192764282, new HaarRectangle(8, 7, 3, 3, -1), new HaarRectangle(8, 8, 3, 1, 3));
            stageNodes.AddHaarFeature(0.00013569870498031378, 0.4340794980525971, 0.5628312230110168, new HaarRectangle(14, 0, 2, 3, -1), new HaarRectangle(14, 0, 1, 3, 2));
            stageNodes.AddHaarFeature(-0.0458802506327629, 0.6507998704910278, 0.4696274995803833, new HaarRectangle(1, 0, 18, 9, -1), new HaarRectangle(7, 0, 6, 9, 3));
            stageNodes.AddHaarFeature(-0.0215825606137514, 0.3826502859592438, 0.5287616848945618, new HaarRectangle(11, 5, 4, 15, -1), new HaarRectangle(11, 5, 2, 15, 2));
            stageNodes.AddHaarFeature(-0.0202095396816731, 0.3233368098735809, 0.5074477195739746, new HaarRectangle(5, 5, 4, 15, -1), new HaarRectangle(7, 5, 2, 15, 2));
            stageNodes.AddHaarFeature(0.005849671084433794, 0.5177603960037231, 0.4489670991897583, new HaarRectangle(14, 0, 2, 3, -1), new HaarRectangle(14, 0, 1, 3, 2));
            stageNodes.AddHaarFeature(-0.00005747637987951748, 0.4020850956439972, 0.5246363878250122, new HaarRectangle(4, 0, 2, 3, -1), new HaarRectangle(5, 0, 1, 3, 2));
            stageNodes.AddHaarFeature(-0.001151310047134757, 0.6315072178840637, 0.490515410900116, new HaarRectangle(11, 12, 2, 2, -1), new HaarRectangle(12, 12, 1, 1, 2), new HaarRectangle(11, 13, 1, 1, 2));
            stageNodes.AddHaarFeature(0.0019862831104546785, 0.4702459871768951, 0.6497151255607605, new HaarRectangle(7, 12, 2, 2, -1), new HaarRectangle(7, 12, 1, 1, 2), new HaarRectangle(8, 13, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.005271951202303171, 0.3650383949279785, 0.5227652788162231, new HaarRectangle(12, 0, 3, 4, -1), new HaarRectangle(13, 0, 1, 4, 3));
            stageNodes.AddHaarFeature(0.0012662699446082115, 0.5166100859642029, 0.387761801481247, new HaarRectangle(4, 11, 3, 3, -1), new HaarRectangle(4, 12, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.006291944067925215, 0.737589418888092, 0.5023847818374634, new HaarRectangle(12, 7, 4, 2, -1), new HaarRectangle(12, 8, 4, 1, 2));
            stageNodes.AddHaarFeature(0.000673601112794131, 0.4423226118087769, 0.5495585799217224, new HaarRectangle(8, 10, 3, 2, -1), new HaarRectangle(9, 10, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.0010523450328037145, 0.5976396203041077, 0.4859583079814911, new HaarRectangle(9, 9, 3, 2, -1), new HaarRectangle(10, 9, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.0004421623889356851, 0.5955939292907715, 0.4398930966854096, new HaarRectangle(8, 9, 3, 2, -1), new HaarRectangle(9, 9, 1, 2, 3));
            stageNodes.AddHaarFeature(0.0011747940443456173, 0.5349888205528259, 0.4605058133602142, new HaarRectangle(12, 0, 3, 4, -1), new HaarRectangle(13, 0, 1, 4, 3));
            stageNodes.AddHaarFeature(0.005245743785053492, 0.5049191117286682, 0.2941577136516571, new HaarRectangle(5, 0, 3, 4, -1), new HaarRectangle(6, 0, 1, 4, 3));
            stageNodes.AddHaarFeature(-0.0245397202670574, 0.2550177872180939, 0.5218586921691895, new HaarRectangle(4, 14, 12, 4, -1), new HaarRectangle(10, 14, 6, 2, 2), new HaarRectangle(4, 16, 6, 2, 2));
            stageNodes.AddHaarFeature(0.0007379304151982069, 0.4424861073493958, 0.5490816235542297, new HaarRectangle(8, 13, 2, 3, -1), new HaarRectangle(8, 14, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0014233799884095788, 0.5319514274597168, 0.4081355929374695, new HaarRectangle(10, 10, 3, 8, -1), new HaarRectangle(10, 14, 3, 4, 2));
            stageNodes.AddHaarFeature(-0.0024149110540747643, 0.4087659120559692, 0.5238950252532959, new HaarRectangle(8, 10, 4, 8, -1), new HaarRectangle(8, 10, 2, 4, 2), new HaarRectangle(10, 14, 2, 4, 2));
            stageNodes.AddHaarFeature(-0.00121652998495847, 0.567457914352417, 0.4908052980899811, new HaarRectangle(10, 8, 3, 1, -1), new HaarRectangle(11, 8, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.0012438809499144554, 0.4129425883293152, 0.5256118178367615, new HaarRectangle(9, 12, 1, 6, -1), new HaarRectangle(9, 15, 1, 3, 2));
            stageNodes.AddHaarFeature(0.006194273941218853, 0.5060194134712219, 0.7313653230667114, new HaarRectangle(10, 8, 3, 1, -1), new HaarRectangle(11, 8, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.0016607169527560472, 0.5979632139205933, 0.4596369862556458, new HaarRectangle(7, 8, 3, 1, -1), new HaarRectangle(8, 8, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.0273162592202425, 0.4174365103244782, 0.5308842062950134, new HaarRectangle(5, 2, 15, 14, -1), new HaarRectangle(5, 9, 15, 7, 2));
            stageNodes.AddHaarFeature(-0.00158455700147897, 0.56158047914505, 0.4519486129283905, new HaarRectangle(2, 1, 2, 10, -1), new HaarRectangle(2, 1, 1, 5, 2), new HaarRectangle(3, 6, 1, 5, 2));
            stageNodes.AddHaarFeature(-0.00155147397890687, 0.4076187014579773, 0.5360785126686096, new HaarRectangle(14, 14, 2, 3, -1), new HaarRectangle(14, 15, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0003844655875582248, 0.4347293972969055, 0.5430442094802856, new HaarRectangle(2, 7, 3, 3, -1), new HaarRectangle(3, 7, 1, 3, 3));
            stageNodes.AddHaarFeature(-0.0146722598001361, 0.1659304946660996, 0.5146093964576721, new HaarRectangle(17, 4, 3, 3, -1), new HaarRectangle(17, 5, 3, 1, 3));
            stageNodes.AddHaarFeature(0.008160888217389584, 0.4961819052696228, 0.1884745955467224, new HaarRectangle(0, 4, 3, 3, -1), new HaarRectangle(0, 5, 3, 1, 3));
            stageNodes.AddHaarFeature(0.0011121659772470591, 0.4868263900279999, 0.6093816161155701, new HaarRectangle(13, 5, 6, 2, -1), new HaarRectangle(16, 5, 3, 1, 2), new HaarRectangle(13, 6, 3, 1, 2));
            stageNodes.AddHaarFeature(-0.007260377053171397, 0.6284325122833252, 0.4690375924110413, new HaarRectangle(4, 19, 12, 1, -1), new HaarRectangle(8, 19, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.0002404643018962815, 0.5575000047683716, 0.4046044051647186, new HaarRectangle(12, 12, 2, 4, -1), new HaarRectangle(12, 14, 2, 2, 2));
            stageNodes.AddHaarFeature(-0.00023348190006799996, 0.4115762114524841, 0.5252848267555237, new HaarRectangle(3, 15, 1, 3, -1), new HaarRectangle(3, 16, 1, 1, 3));
            stageNodes.AddHaarFeature(0.005573648028075695, 0.4730072915554047, 0.5690100789070129, new HaarRectangle(11, 16, 6, 4, -1), new HaarRectangle(11, 16, 3, 4, 2));
            stageNodes.AddHaarFeature(0.0306237693876028, 0.4971886873245239, 0.1740095019340515, new HaarRectangle(2, 10, 3, 10, -1), new HaarRectangle(3, 10, 1, 10, 3));
            stageNodes.AddHaarFeature(0.000920747988857329, 0.5372117757797241, 0.4354872107505798, new HaarRectangle(12, 8, 2, 4, -1), new HaarRectangle(12, 8, 1, 4, 2));
            stageNodes.AddHaarFeature(-0.000043550739064812653, 0.5366883873939514, 0.4347316920757294, new HaarRectangle(6, 8, 2, 4, -1), new HaarRectangle(7, 8, 1, 4, 2));
            stageNodes.AddHaarFeature(-0.006645271088927984, 0.3435518145561218, 0.516053318977356, new HaarRectangle(10, 14, 2, 3, -1), new HaarRectangle(10, 14, 1, 3, 2));
            stageNodes.AddHaarFeature(0.0432219989597797, 0.4766792058944702, 0.7293652892112732, new HaarRectangle(5, 1, 10, 3, -1), new HaarRectangle(10, 1, 5, 3, 2));
            stageNodes.AddHaarFeature(0.0022331769578158855, 0.5029315948486328, 0.5633171200752258, new HaarRectangle(10, 7, 3, 2, -1), new HaarRectangle(11, 7, 1, 2, 3));
            stageNodes.AddHaarFeature(0.0031829739455133677, 0.4016092121601105, 0.5192136764526367, new HaarRectangle(5, 6, 9, 2, -1), new HaarRectangle(8, 6, 3, 2, 3));
            stageNodes.AddHaarFeature(-0.00018027749320026487, 0.4088315963745117, 0.5417919754981995, new HaarRectangle(9, 8, 2, 2, -1), new HaarRectangle(9, 9, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.0052934689447283745, 0.407567709684372, 0.5243561863899231, new HaarRectangle(2, 11, 16, 6, -1), new HaarRectangle(2, 11, 8, 3, 2), new HaarRectangle(10, 14, 8, 3, 2));
            stageNodes.AddHaarFeature(0.0012750959722325206, 0.4913282990455627, 0.6387010812759399, new HaarRectangle(12, 7, 2, 2, -1), new HaarRectangle(13, 7, 1, 1, 2), new HaarRectangle(12, 8, 1, 1, 2));
            stageNodes.AddHaarFeature(0.004338532220572233, 0.5031672120094299, 0.2947346866130829, new HaarRectangle(9, 5, 2, 3, -1), new HaarRectangle(9, 6, 2, 1, 3));
            stageNodes.AddHaarFeature(0.00852507445961237, 0.4949789047241211, 0.6308869123458862, new HaarRectangle(9, 7, 3, 2, -1), new HaarRectangle(10, 7, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.0009426635224372149, 0.5328366756439209, 0.4285649955272675, new HaarRectangle(5, 1, 8, 12, -1), new HaarRectangle(5, 7, 8, 6, 2));
            stageNodes.AddHaarFeature(0.0013609660090878606, 0.4991525113582611, 0.5941501259803772, new HaarRectangle(13, 5, 2, 2, -1), new HaarRectangle(13, 6, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0004478250921238214, 0.4573504030704498, 0.5854480862617493, new HaarRectangle(5, 5, 2, 2, -1), new HaarRectangle(5, 6, 2, 1, 2));
            stageNodes.AddHaarFeature(0.001336005050688982, 0.4604358971118927, 0.584905207157135, new HaarRectangle(12, 4, 3, 3, -1), new HaarRectangle(12, 5, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.0006096754805184901, 0.3969388902187347, 0.522942304611206, new HaarRectangle(4, 14, 2, 3, -1), new HaarRectangle(4, 15, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.002365678083151579, 0.5808320045471191, 0.4898357093334198, new HaarRectangle(12, 4, 3, 3, -1), new HaarRectangle(12, 5, 3, 1, 3));
            stageNodes.AddHaarFeature(0.001073434017598629, 0.435121089220047, 0.5470039248466492, new HaarRectangle(5, 4, 3, 3, -1), new HaarRectangle(5, 5, 3, 1, 3));
            stageNodes.AddHaarFeature(0.0021923359017819166, 0.535506010055542, 0.3842903971672058, new HaarRectangle(9, 14, 2, 6, -1), new HaarRectangle(10, 14, 1, 3, 2), new HaarRectangle(9, 17, 1, 3, 2));
            stageNodes.AddHaarFeature(0.005496861878782511, 0.5018138885498047, 0.2827191948890686, new HaarRectangle(8, 14, 3, 2, -1), new HaarRectangle(9, 14, 1, 2, 3));
            stageNodes.AddHaarFeature(-0.0753688216209412, 0.1225076019763947, 0.5148826837539673, new HaarRectangle(9, 5, 6, 6, -1), new HaarRectangle(11, 5, 2, 6, 3));
            stageNodes.AddHaarFeature(0.0251344703137875, 0.4731766879558563, 0.702544629573822, new HaarRectangle(5, 5, 6, 6, -1), new HaarRectangle(7, 5, 2, 6, 3));
            stageNodes.AddHaarFeature(-0.00002935859993158374, 0.5430532097816467, 0.465608686208725, new HaarRectangle(13, 13, 1, 2, -1), new HaarRectangle(13, 14, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0005835591000504792, 0.4031040072441101, 0.5190119743347168, new HaarRectangle(0, 2, 10, 2, -1), new HaarRectangle(0, 3, 10, 1, 2));
            stageNodes.AddHaarFeature(-0.0026639450807124376, 0.4308126866817474, 0.5161771178245544, new HaarRectangle(13, 13, 1, 2, -1), new HaarRectangle(13, 14, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0013804089976474645, 0.621982991695404, 0.4695515930652618, new HaarRectangle(5, 7, 2, 2, -1), new HaarRectangle(5, 7, 1, 1, 2), new HaarRectangle(6, 8, 1, 1, 2));
            stageNodes.AddHaarFeature(0.001231321948580444, 0.5379363894462585, 0.4425831139087677, new HaarRectangle(13, 5, 2, 7, -1), new HaarRectangle(13, 5, 1, 7, 2));
            stageNodes.AddHaarFeature(-0.000014644179827882908, 0.5281640291213989, 0.4222503006458283, new HaarRectangle(6, 13, 1, 2, -1), new HaarRectangle(6, 14, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0128188095986843, 0.2582092881202698, 0.5179932713508606, new HaarRectangle(11, 0, 3, 7, -1), new HaarRectangle(12, 0, 1, 7, 3));
            stageNodes.AddHaarFeature(0.0228521898388863, 0.4778693020343781, 0.7609264254570007, new HaarRectangle(0, 3, 2, 16, -1), new HaarRectangle(0, 3, 1, 8, 2), new HaarRectangle(1, 11, 1, 8, 2));
            stageNodes.AddHaarFeature(0.0008230597013607621, 0.5340992212295532, 0.4671724140644074, new HaarRectangle(11, 0, 3, 7, -1), new HaarRectangle(12, 0, 1, 7, 3));
            stageNodes.AddHaarFeature(0.0127701200544834, 0.4965761005878449, 0.1472366005182266, new HaarRectangle(6, 0, 3, 7, -1), new HaarRectangle(7, 0, 1, 7, 3));
            stageNodes.AddHaarFeature(-0.0500515103340149, 0.641499400138855, 0.5016592144966125, new HaarRectangle(11, 16, 8, 4, -1), new HaarRectangle(11, 16, 4, 4, 2));
            stageNodes.AddHaarFeature(0.0157752707600594, 0.4522320032119751, 0.5685362219810486, new HaarRectangle(1, 16, 8, 4, -1), new HaarRectangle(5, 16, 4, 4, 2));
            stageNodes.AddHaarFeature(-0.0185016207396984, 0.2764748930931091, 0.5137959122657776, new HaarRectangle(13, 5, 2, 7, -1), new HaarRectangle(13, 5, 1, 7, 2));
            stageNodes.AddHaarFeature(0.002462625037878752, 0.5141941905021667, 0.3795408010482788, new HaarRectangle(5, 5, 2, 7, -1), new HaarRectangle(6, 5, 1, 7, 2));
            stageNodes.AddHaarFeature(0.0629161670804024, 0.5060648918151855, 0.658043384552002, new HaarRectangle(18, 6, 2, 14, -1), new HaarRectangle(18, 13, 2, 7, 2));
            stageNodes.AddHaarFeature(-0.000021648500478477217, 0.5195388197898865, 0.401988685131073, new HaarRectangle(6, 10, 3, 4, -1), new HaarRectangle(6, 12, 3, 2, 2));
            stageNodes.AddHaarFeature(0.0021180990152060986, 0.4962365031242371, 0.5954458713531494, new HaarRectangle(14, 7, 1, 2, -1), new HaarRectangle(14, 8, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0166348908096552, 0.3757933080196381, 0.517544686794281, new HaarRectangle(0, 1, 18, 6, -1), new HaarRectangle(0, 1, 9, 3, 2), new HaarRectangle(9, 4, 9, 3, 2));
            stageNodes.AddHaarFeature(-0.002889947034418583, 0.6624013781547546, 0.5057178735733032, new HaarRectangle(14, 7, 1, 2, -1), new HaarRectangle(14, 8, 1, 1, 2));
            stageNodes.AddHaarFeature(0.076783262193203, 0.4795796871185303, 0.8047714829444885, new HaarRectangle(0, 6, 2, 14, -1), new HaarRectangle(0, 13, 2, 7, 2));
            stageNodes.AddHaarFeature(0.003917067777365446, 0.4937882125377655, 0.5719941854476929, new HaarRectangle(17, 0, 3, 12, -1), new HaarRectangle(18, 0, 1, 12, 3));
            stageNodes.AddHaarFeature(-0.0726706013083458, 0.0538945607841015, 0.4943903982639313, new HaarRectangle(0, 6, 18, 3, -1), new HaarRectangle(0, 7, 18, 1, 3));
            stageNodes.AddHaarFeature(0.5403950214385986, 0.5129774212837219, 0.1143338978290558, new HaarRectangle(6, 0, 14, 16, -1), new HaarRectangle(6, 8, 14, 8, 2));
            stageNodes.AddHaarFeature(0.0029510019812732935, 0.4528343975543976, 0.5698574185371399, new HaarRectangle(0, 0, 3, 12, -1), new HaarRectangle(1, 0, 1, 12, 3));
            stageNodes.AddHaarFeature(0.0034508369863033295, 0.5357726812362671, 0.4218730926513672, new HaarRectangle(13, 0, 3, 7, -1), new HaarRectangle(14, 0, 1, 7, 3));
            stageNodes.AddHaarFeature(-0.00042077939724549646, 0.5916172862052917, 0.4637925922870636, new HaarRectangle(5, 7, 1, 2, -1), new HaarRectangle(5, 8, 1, 1, 2));
            stageNodes.AddHaarFeature(0.0033051050268113613, 0.5273385047912598, 0.438204288482666, new HaarRectangle(14, 4, 6, 6, -1), new HaarRectangle(14, 6, 6, 2, 3));
            stageNodes.AddHaarFeature(0.0004773506079800427, 0.4046528041362763, 0.5181884765625, new HaarRectangle(5, 7, 7, 2, -1), new HaarRectangle(5, 8, 7, 1, 2));
            stageNodes.AddHaarFeature(-0.0259285103529692, 0.7452235817909241, 0.5089386105537415, new HaarRectangle(8, 6, 6, 9, -1), new HaarRectangle(8, 9, 6, 3, 3));
            stageNodes.AddHaarFeature(-0.002972979098558426, 0.3295435905456543, 0.5058795213699341, new HaarRectangle(5, 4, 6, 1, -1), new HaarRectangle(7, 4, 2, 1, 3));
            stageNodes.AddHaarFeature(0.005850832909345627, 0.4857144057750702, 0.5793024897575378, new HaarRectangle(13, 0, 6, 4, -1), new HaarRectangle(16, 0, 3, 2, 2), new HaarRectangle(13, 2, 3, 2, 2));
            stageNodes.AddHaarFeature(-0.0459675192832947, 0.4312731027603149, 0.5380653142929077, new HaarRectangle(1, 2, 18, 12, -1), new HaarRectangle(1, 6, 18, 4, 3));
            stageNodes.AddHaarFeature(0.1558596044778824, 0.5196170210838318, 0.1684713959693909, new HaarRectangle(3, 2, 17, 12, -1), new HaarRectangle(3, 6, 17, 4, 3));
            stageNodes.AddHaarFeature(0.0151648297905922, 0.4735757112503052, 0.6735026836395264, new HaarRectangle(5, 14, 7, 3, -1), new HaarRectangle(5, 15, 7, 1, 3));
            stageNodes.AddHaarFeature(-0.0010604249546304343, 0.5822926759719849, 0.4775702953338623, new HaarRectangle(10, 14, 1, 3, -1), new HaarRectangle(10, 15, 1, 1, 3));
            stageNodes.AddHaarFeature(0.006647629197686911, 0.4999198913574219, 0.231953501701355, new HaarRectangle(3, 14, 3, 3, -1), new HaarRectangle(3, 15, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.0122311301529408, 0.4750893115997315, 0.5262982249259949, new HaarRectangle(14, 4, 6, 6, -1), new HaarRectangle(14, 6, 6, 2, 3));
            stageNodes.AddHaarFeature(0.005652888212352991, 0.5069767832756042, 0.3561818897724152, new HaarRectangle(0, 4, 6, 6, -1), new HaarRectangle(0, 6, 6, 2, 3));
            stageNodes.AddHaarFeature(0.0012977829901501536, 0.4875693917274475, 0.5619062781333923, new HaarRectangle(12, 5, 4, 3, -1), new HaarRectangle(12, 6, 4, 1, 3));
            stageNodes.AddHaarFeature(0.0107815898954868, 0.4750770032405853, 0.6782308220863342, new HaarRectangle(4, 5, 4, 3, -1), new HaarRectangle(4, 6, 4, 1, 3));
            stageNodes.AddHaarFeature(0.002865477930754423, 0.5305461883544922, 0.4290736019611359, new HaarRectangle(18, 0, 2, 6, -1), new HaarRectangle(18, 2, 2, 2, 3));
            stageNodes.AddHaarFeature(0.0028663428965955973, 0.4518479108810425, 0.5539351105690002, new HaarRectangle(8, 1, 4, 9, -1), new HaarRectangle(10, 1, 2, 9, 2));
            stageNodes.AddHaarFeature(-0.005198332015424967, 0.4149119853973389, 0.5434188842773438, new HaarRectangle(6, 6, 8, 2, -1), new HaarRectangle(6, 6, 4, 2, 2));
            stageNodes.AddHaarFeature(0.005373999010771513, 0.471789687871933, 0.6507657170295715, new HaarRectangle(6, 5, 4, 2, -1), new HaarRectangle(6, 5, 2, 1, 2), new HaarRectangle(8, 6, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.0146415298804641, 0.2172164022922516, 0.5161777138710022, new HaarRectangle(10, 5, 2, 3, -1), new HaarRectangle(10, 6, 2, 1, 3));
            stageNodes.AddHaarFeature(-0.000015042580344015732, 0.533738374710083, 0.4298836886882782, new HaarRectangle(9, 5, 1, 3, -1), new HaarRectangle(9, 6, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.0001187566012958996, 0.4604594111442566, 0.5582447052001953, new HaarRectangle(9, 10, 2, 2, -1), new HaarRectangle(9, 11, 2, 1, 2));
            stageNodes.AddHaarFeature(0.0169955305755138, 0.4945895075798035, 0.0738800764083862, new HaarRectangle(0, 8, 4, 3, -1), new HaarRectangle(0, 9, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.0350959412753582, 0.70055091381073, 0.4977591037750244, new HaarRectangle(6, 0, 8, 6, -1), new HaarRectangle(6, 3, 8, 3, 2));
            stageNodes.AddHaarFeature(0.0024217350874096155, 0.4466265141963959, 0.5477694272994995, new HaarRectangle(1, 0, 6, 4, -1), new HaarRectangle(1, 0, 3, 2, 2), new HaarRectangle(4, 2, 3, 2, 2));
            stageNodes.AddHaarFeature(-0.0009634033776819706, 0.4714098870754242, 0.5313338041305542, new HaarRectangle(13, 0, 3, 7, -1), new HaarRectangle(14, 0, 1, 7, 3));
            stageNodes.AddHaarFeature(0.00016391130338888615, 0.4331546127796173, 0.5342242121696472, new HaarRectangle(9, 16, 2, 2, -1), new HaarRectangle(9, 17, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.0211414601653814, 0.2644700109958649, 0.5204498767852783, new HaarRectangle(11, 4, 6, 10, -1), new HaarRectangle(11, 9, 6, 5, 2));
            stageNodes.AddHaarFeature(0.0008777520270086826, 0.5208349823951721, 0.4152742922306061, new HaarRectangle(0, 10, 19, 2, -1), new HaarRectangle(0, 11, 19, 1, 2));
            stageNodes.AddHaarFeature(-0.0279439203441143, 0.6344125270843506, 0.5018811821937561, new HaarRectangle(9, 5, 8, 9, -1), new HaarRectangle(9, 8, 8, 3, 3));
            stageNodes.AddHaarFeature(0.006729737855494022, 0.5050438046455383, 0.3500863909721375, new HaarRectangle(4, 0, 3, 7, -1), new HaarRectangle(5, 0, 1, 7, 3));
            stageNodes.AddHaarFeature(0.0232810396701097, 0.4966318011283875, 0.6968677043914795, new HaarRectangle(8, 6, 4, 12, -1), new HaarRectangle(10, 6, 2, 6, 2), new HaarRectangle(8, 12, 2, 6, 2));
            stageNodes.AddHaarFeature(-0.0116449799388647, 0.3300260007381439, 0.5049629807472229, new HaarRectangle(0, 2, 6, 4, -1), new HaarRectangle(0, 4, 6, 2, 2));
            stageNodes.AddHaarFeature(0.0157643090933561, 0.4991598129272461, 0.7321153879165649, new HaarRectangle(8, 15, 4, 3, -1), new HaarRectangle(8, 16, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.001361147966235876, 0.3911735117435455, 0.5160670876502991, new HaarRectangle(8, 0, 3, 7, -1), new HaarRectangle(9, 0, 1, 7, 3));
            stageNodes.AddHaarFeature(-0.0008152233785949649, 0.5628911256790161, 0.49497190117836, new HaarRectangle(9, 5, 3, 4, -1), new HaarRectangle(10, 5, 1, 4, 3));
            stageNodes.AddHaarFeature(-0.0006006627227179706, 0.585359513759613, 0.4550595879554749, new HaarRectangle(8, 5, 3, 4, -1), new HaarRectangle(9, 5, 1, 4, 3));
            stageNodes.AddHaarFeature(0.0004971551825292408, 0.4271470010280609, 0.5443599224090576, new HaarRectangle(7, 6, 6, 1, -1), new HaarRectangle(9, 6, 2, 1, 3));
            stageNodes.AddHaarFeature(0.0023475370835512877, 0.5143110752105713, 0.3887656927108765, new HaarRectangle(7, 14, 4, 4, -1), new HaarRectangle(7, 14, 2, 2, 2), new HaarRectangle(9, 16, 2, 2, 2));
            stageNodes.AddHaarFeature(-0.008926156908273697, 0.6044502258300781, 0.497172087430954, new HaarRectangle(13, 14, 4, 6, -1), new HaarRectangle(15, 14, 2, 3, 2), new HaarRectangle(13, 17, 2, 3, 2));
            stageNodes.AddHaarFeature(-0.013919910416007, 0.2583160996437073, 0.5000367760658264, new HaarRectangle(7, 8, 1, 8, -1), new HaarRectangle(7, 12, 1, 4, 2));
            stageNodes.AddHaarFeature(0.00102099496871233, 0.4857374131679535, 0.5560358166694641, new HaarRectangle(16, 0, 2, 8, -1), new HaarRectangle(17, 0, 1, 4, 2), new HaarRectangle(16, 4, 1, 4, 2));
            stageNodes.AddHaarFeature(-0.0027441629208624363, 0.5936884880065918, 0.464577704668045, new HaarRectangle(2, 0, 2, 8, -1), new HaarRectangle(2, 0, 1, 4, 2), new HaarRectangle(3, 4, 1, 4, 2));
            stageNodes.AddHaarFeature(-0.0162001308053732, 0.3163014948368073, 0.5193495154380798, new HaarRectangle(6, 1, 14, 3, -1), new HaarRectangle(6, 2, 14, 1, 3));
            stageNodes.AddHaarFeature(0.004333198070526123, 0.5061224102973938, 0.3458878993988037, new HaarRectangle(7, 9, 3, 10, -1), new HaarRectangle(7, 14, 3, 5, 2));
            stageNodes.AddHaarFeature(0.0005849793087691069, 0.4779017865657806, 0.5870177745819092, new HaarRectangle(9, 14, 2, 2, -1), new HaarRectangle(9, 15, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.0022466450463980436, 0.4297851026058197, 0.5374773144721985, new HaarRectangle(7, 7, 6, 8, -1), new HaarRectangle(7, 11, 6, 4, 2));
            stageNodes.AddHaarFeature(0.0023146099410951138, 0.5438671708106995, 0.4640969932079315, new HaarRectangle(9, 7, 3, 6, -1), new HaarRectangle(9, 10, 3, 3, 2));
            stageNodes.AddHaarFeature(0.008767912164330482, 0.472689300775528, 0.6771789789199829, new HaarRectangle(7, 13, 3, 3, -1), new HaarRectangle(7, 14, 3, 1, 3));
            stageNodes.AddHaarFeature(-0.00022448020172305405, 0.4229173064231873, 0.5428048968315125, new HaarRectangle(9, 9, 2, 2, -1), new HaarRectangle(9, 10, 2, 1, 2));
            stageNodes.AddHaarFeature(-0.007433602120727301, 0.6098880767822266, 0.4683673977851868, new HaarRectangle(0, 1, 18, 2, -1), new HaarRectangle(6, 1, 6, 2, 3));
            stageNodes.AddHaarFeature(-0.0023189240600913763, 0.5689436793327332, 0.4424242079257965, new HaarRectangle(7, 1, 6, 14, -1), new HaarRectangle(7, 8, 6, 7, 2));
            stageNodes.AddHaarFeature(-0.0021042178850620985, 0.3762221038341522, 0.5187087059020996, new HaarRectangle(1, 9, 18, 1, -1), new HaarRectangle(7, 9, 6, 1, 3));
            stageNodes.AddHaarFeature(0.000460348412161693, 0.4699405133724213, 0.5771207213401794, new HaarRectangle(9, 7, 2, 2, -1), new HaarRectangle(9, 7, 1, 2, 2));
            stageNodes.AddHaarFeature(0.0010547629790380595, 0.4465216994285584, 0.5601701736450195, new HaarRectangle(9, 3, 2, 9, -1), new HaarRectangle(10, 3, 1, 9, 2));
            stageNodes.AddHaarFeature(0.0008714881842024624, 0.544980525970459, 0.3914709091186523, new HaarRectangle(18, 14, 2, 3, -1), new HaarRectangle(18, 15, 2, 1, 3));
            stageNodes.AddHaarFeature(0.00033364820410497487, 0.4564009010791779, 0.5645738840103149, new HaarRectangle(7, 11, 3, 1, -1), new HaarRectangle(8, 11, 1, 1, 3));
            stageNodes.AddHaarFeature(-0.001485325046814978, 0.5747377872467041, 0.4692778885364533, new HaarRectangle(10, 8, 3, 4, -1), new HaarRectangle(11, 8, 1, 4, 3));
            stageNodes.AddHaarFeature(0.0030251620337367058, 0.5166196823120117, 0.3762814104557037, new HaarRectangle(7, 14, 3, 6, -1), new HaarRectangle(8, 14, 1, 6, 3));
            stageNodes.AddHaarFeature(0.005028074141591787, 0.5002111792564392, 0.6151527166366577, new HaarRectangle(10, 8, 3, 4, -1), new HaarRectangle(11, 8, 1, 4, 3));
            stageNodes.AddHaarFeature(-0.0005816451157443225, 0.5394598245620728, 0.4390751123428345, new HaarRectangle(7, 8, 3, 4, -1), new HaarRectangle(8, 8, 1, 4, 3));
            stageNodes.AddHaarFeature(0.0451415292918682, 0.5188326835632324, 0.206303596496582, new HaarRectangle(7, 9, 6, 9, -1), new HaarRectangle(7, 12, 6, 3, 3));
            stageNodes.AddHaarFeature(-0.001079562003724277, 0.3904685080051422, 0.5137907266616821, new HaarRectangle(0, 14, 2, 3, -1), new HaarRectangle(0, 15, 2, 1, 3));
            stageNodes.AddHaarFeature(0.00015995999274309725, 0.4895322918891907, 0.5427504181861877, new HaarRectangle(11, 12, 1, 2, -1), new HaarRectangle(11, 13, 1, 1, 2));
            stageNodes.AddHaarFeature(-0.0193592701107264, 0.6975228786468506, 0.4773507118225098, new HaarRectangle(4, 3, 8, 3, -1), new HaarRectangle(8, 3, 4, 3, 2));
            stageNodes.AddHaarFeature(0.207255095243454, 0.5233635902404785, 0.3034991919994354, new HaarRectangle(0, 4, 20, 6, -1), new HaarRectangle(0, 4, 10, 6, 2));
            stageNodes.AddHaarFeature(-0.00041953290929086506, 0.5419396758079529, 0.4460186064243317, new HaarRectangle(9, 14, 1, 3, -1), new HaarRectangle(9, 15, 1, 1, 3));
            stageNodes.AddHaarFeature(0.0022582069505006075, 0.4815764129161835, 0.6027408838272095, new HaarRectangle(8, 14, 4, 3, -1), new HaarRectangle(8, 15, 4, 1, 3));
            stageNodes.AddHaarFeature(-0.0067811207845807076, 0.3980278968811035, 0.5183305740356445, new HaarRectangle(0, 15, 14, 4, -1), new HaarRectangle(0, 17, 14, 2, 2));
            stageNodes.AddHaarFeature(0.0111543098464608, 0.543123185634613, 0.4188759922981262, new HaarRectangle(1, 14, 18, 6, -1), new HaarRectangle(1, 17, 18, 3, 2));
            stageNodes.AddHaarFeature(0.0431624315679073, 0.4738228023052216, 0.6522961258888245, new HaarRectangle(0, 0, 10, 6, -1), new HaarRectangle(0, 0, 5, 3, 2), new HaarRectangle(5, 3, 5, 3, 2));
            stageNodeCounts.Add(stageNodes.Count - index); index = stageNodes.Count;

            #endregion

            DetectorData data = new DetectorData();
            data.StageNodes = stageNodes;
            data.StageNodesCount = stageNodeCounts;
            data.StageThresholds = stageThresholds.Select(x => (float)x).ToList();
            data.WindowSize = new Size(20, 20);

            return data;
        }

        /// <summary>
        /// Free resources being used
        /// </summary>
        public void Dispose()
        {
            try { if (_ownIntegralImage) { _integralImage.Dispose(); _integralImage = null; } }
            catch { }
            try { if (_ownIntegral2Image) { _integral2Image.Dispose(); _integral2Image = null; } }
            catch { }
            try { if (_ownResultRectangles) { _resultRectangles.Dispose(); _resultRectangles = null; } }
            catch { }
        }
    }
}
