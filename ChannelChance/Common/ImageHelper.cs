﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace ChannelChance.Common
{
    public class ImageHelper
    {
        public static List<BitmapImage> LoadImages(string dir)
        {
            List<BitmapImage> bitmapImages = new List<BitmapImage>();
            if (Directory.Exists(dir))
            {
                var files = Directory.GetFiles(dir).OrderBy(x => x);
                foreach (var file in files)
                {
                    var bitmapImage = LoadImage(file);
                    bitmapImages.Add(bitmapImage);
                }
            }
            return bitmapImages;
        }
        public static List<string> LoadImageNames(string dir)
        {
            var bitmapImages = new List<string>();
            if (Directory.Exists(dir))
            {
                var files = Directory.GetFiles(dir).OrderBy(x => x);
                bitmapImages.AddRange(files);
            }
            return bitmapImages;
        }
        public static BitmapImage LoadImage(string path)
        {
            BitmapImage bitmapImage = null;
            if (File.Exists(path))
            {
                bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption=BitmapCacheOption.OnLoad;
                bitmapImage.DecodePixelWidth = 1024;
                bitmapImage.DecodePixelHeight = 576;
                bitmapImage.UriSource = new Uri(path);
                bitmapImage.EndInit();
                bitmapImage.Freeze();
            }
            return bitmapImage;
        }
    }
}
