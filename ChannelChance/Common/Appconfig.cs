﻿using System;
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

        public static readonly string Mp3Path = Path.Combine(ApplicationPath, "mp3");

        public static readonly string I_B_MP4 = Path.Combine(Mp3Path, "I_B.mp4");

        public static readonly string I_A_MP4 = Path.Combine(Mp3Path, "I_A.mp4");

        public static readonly string II_B_MP4 = Path.Combine(Mp3Path, "II_B.mp4");

        public static readonly string II_A_MP4 = Path.Combine(Mp3Path, "II_A.mp4");
    }
}