using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ChannelChance.Common
{
    public  class HandOption
    {
        private static HandOption _Instance = null;
        private HandOption()
        {
            SetInit();
        }
        static HandOption()
        {
            _Instance = new HandOption();
        }
        public static HandOption Instance
        {
            get
            {
                return _Instance;
            }
        }
        public void SetInit()
        {
            Page1State = HandDirection.None;
            Page2State = HandDirection.None;
            Page3State = HandDirection.None;
        }
        public  HandDirection Page1State { get; set; }
        public  HandDirection Page2State { get; set; }
        public  HandDirection Page3State { get; set; }

        public Tuple<Uri, Uri> Page1Img
        {
            get
            {
                string img1 = Page1State == HandDirection.L ? "dimension_IA.png" : "dimension_IB.png";
                string img2 = Page1State == HandDirection.L ? "wallpaper-IA.png" : "wallpaper-IB.png";
                return GetImg(img1, img2);
            }
        }

        public Tuple<Uri, Uri> Page2Img
        {
            get
            {
                string img1 = Page2State == HandDirection.L ? "dimension_IIA.png" : "dimension_IIB.png";
                string img2 = Page2State == HandDirection.L ? "wallpaper-IIA.png" : "wallpaper-IIB.png";
                return GetImg(img1, img2);
            }
        }

        public Tuple<Uri, Uri> Page3Img
        {
            get
            {
                string img1 = Page3State == HandDirection.L ? "dimension_IIIA.png" : "dimension_IIIB.png";
                string img2 = Page3State == HandDirection.L ? "wallpaper-IIIA.png" : "wallpaper-IIIB.png";
                return GetImg(img1, img2);
            }
        }

        public Tuple<Uri, Uri> Page4Img
        {
            get
            {
                string img1 = SeesawManager.Instance.HandDirection == HandDirection.L ? "dimension_IVA.png" : "dimension_IVB.png";
                string img2 = SeesawManager.Instance.HandDirection == HandDirection.L ? "wallpaper-IVA.png" : "wallpaper-IVB.png";
                return GetImg(img1, img2);
            }
        }

        public string Page1Video
        {
            get
            {
                string video = Page1State == HandDirection.L ? "I_B.mp4" : "I_A.mp4";
                return GetVideo(video);
            }
        }

        public string Page2Video
        {
            get
            {
                string video = Page2State == HandDirection.L ? "II_B.mp4" : "II_A.mp4";
                return GetVideo(video);
            }
        }

        public string Page3Video
        {
            get
            {
                string video = Page3State == HandDirection.L ? "III_B.mp4" : "III_A.mp4";
                return GetVideo(video);
            }
        }

        private Tuple<Uri,Uri> GetImg(string img1,string img2)
        {
            img1 = Path.Combine(Environment.CurrentDirectory, "Dimension", img1);
            img2 = Path.Combine(Environment.CurrentDirectory, "wallpaper", img2);
            return new Tuple<Uri, Uri>(new Uri(img1), new Uri(img2));
        }

        private string GetVideo(string video)
        {
            return Path.Combine(Appconfig.VideoPath, video);
        }
    }
}