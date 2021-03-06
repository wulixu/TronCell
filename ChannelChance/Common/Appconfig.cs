﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChannelChance.Common
{
    public static class Appconfig
    {
        public static readonly string ApplicationPath = Environment.CurrentDirectory;

        public static readonly string VideoPath = Path.Combine(ApplicationPath, "video");

        public static readonly string V_A_MP4 = Path.Combine(VideoPath, "V_A.mp4");

        public static readonly string V_B_MP4 = Path.Combine(VideoPath, "V_B.mp4");

        public static readonly string TowDimensionPic = Path.Combine(ApplicationPath, "twodimension.jpg");

        public static readonly string MP3 = Path.Combine(ApplicationPath, "mp3", "loop.mp3");
        public static readonly string CutImagesDirName = "CutImages";
        public static readonly string TiltImagesDirName = "TiltImages";
        public static readonly string GroundImagesDirName = "GroundImages";
        public static readonly string Seesawimages = "Seesawimages";
        public static readonly int AutoPlayInterval = 3;
        public static readonly double Brightness = Convert.ToDouble(ConfigurationManager.AppSettings["Brightness"]);
        public static readonly double Contrast = Convert.ToDouble(ConfigurationManager.AppSettings["Contrast"]);

        public static int ToRorL(int count)
        {
            var b = count > 0;
            return b ? 1 : -1;
        }
        public static Dictionary<string, double[]> AnimaEllipsePositions =null;
        public static double[]  GetAnimaEllipsePositions(string key)
        {
            double delta = 47;double[] returnV=null;
            if (AnimaEllipsePositions == null)
            {
                AnimaEllipsePositions = new Dictionary<string, double[]>();
                AnimaEllipsePositions.Add("Default", new double[] { 605, 1190 });
                AnimaEllipsePositions.Add("IV_L0_R1_R", new double[] { 525 - delta, 1235 - delta });
                AnimaEllipsePositions.Add("IV_L0_R2_R", new double[] { 390 - delta, 1175 - delta });
                AnimaEllipsePositions.Add("IV_L1_R0_L", new double[] { 670 - delta, 1370 - delta });
                AnimaEllipsePositions.Add("IV_L1_R1_L", new double[] { 540 - delta, 1370 - delta });
                AnimaEllipsePositions.Add("IV_L1_R1_R", new double[] { 540 - delta, 1370 - delta });
                AnimaEllipsePositions.Add("IV_L1_R2_L", new double[] { 410 - delta, 1315 - delta });
                AnimaEllipsePositions.Add("IV_L1_R2_R", new double[] { 410 - delta, 1315 - delta });
                AnimaEllipsePositions.Add("IV_L2_R0_L", new double[] { 730 - delta, 1505 - delta });
                AnimaEllipsePositions.Add("IV_L2_R1_L", new double[] { 585 - delta, 1505 - delta });
                AnimaEllipsePositions.Add("IV_L2_R1_R", new double[] { 585 - delta, 1505 - delta });
                AnimaEllipsePositions.Add("IV_L2_R2_L", new double[] { 450 - delta, 1435 - delta });
                AnimaEllipsePositions.Add("IV_L2_R2_R", new double[] { 450 - delta, 1435 - delta });
            }
            try
            {
                returnV = AnimaEllipsePositions[key];
            }
            catch
            {
                returnV = null;
            }
            return returnV;
        }
    }
}