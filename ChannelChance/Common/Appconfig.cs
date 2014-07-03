using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChannelChance.Common
{
    public abstract class Appconfig
    {
        public static readonly string ApplicationPath = Environment.CurrentDirectory;

        public static readonly string VideoPath = Path.Combine(ApplicationPath, "video");

        public static readonly string I_B_MP4 = Path.Combine(VideoPath, "I_B.mp4");

        public static readonly string I_A_MP4 = Path.Combine(VideoPath, "I_A.mp4");

        public static readonly string II_B_MP4 = Path.Combine(VideoPath, "II_B.mp4");

        public static readonly string II_A_MP4 = Path.Combine(VideoPath, "II_A.mp4");

        public static readonly string III_B_MP4 = Path.Combine(VideoPath, "III_B.mp4");

        public static readonly string III_A_MP4 = Path.Combine(VideoPath, "III_A.mp4");


        public static readonly string CutImagesDirName = "CutImages";
        public static readonly string TiltImagesDirName = "TiltImages";
        public static readonly string GroundImagesDirName = "GroundImages";
        public static readonly string Seesawimages = "Seesawimages";
    }
}