using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChannelChance.Common;

namespace ChannelChance.Controls
{
    /// <summary>
    /// Interaction logic for ImageAnimControl.xaml
    /// </summary>
    public partial class ImageAnimControl : UserControl, IDisposable
    {
        public event Action<int> PlayRightNextPage;
        public event Action<int> PlayLeftNextPage;

        #region Fields

        private List<BitmapImage> _bitmapImages;
        private int _currentIndex = 0;
        private int _nextIndex = 0;
        private bool _ischanging = false;
        private bool _isRight;
        private int _leftImgIndex = 0;
        private int _rightImgIndex = 0;
        public int[] LeftCount;//左边触发移动个数
        public int[] RightCount;//右边触发移动个数
        private int _maxCount;
        private int _midCount;
        #endregion

        public ImageAnimControl()
        {
            InitializeComponent();

            _bitmapImages = new List<BitmapImage>();

            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        #region Events
        void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            if (_ischanging)
            {
                var count = _nextIndex - _currentIndex;

                if (count == 0)
                {
                    if (_leftImgIndex == 0)
                    {
                        _ischanging = false;
                        if (PlayLeftNextPage != null)
                            PlayLeftNextPage.Invoke(1);
                        return;
                    }
                    if (_rightImgIndex == RightCount.Length)
                    {
                        _ischanging = false;
                        if (PlayRightNextPage != null)
                            PlayRightNextPage.Invoke(1);
                        return;
                    }
                    return;
                }

                _isRight = count > 0;
                if (_isRight)
                {
                    _currentIndex++;
                    var currentImage = _bitmapImages[_currentIndex];
                    ElementImage.Source = currentImage;
                }
                else
                {
                    _currentIndex--;
                    var currentImage = _bitmapImages[_currentIndex];
                    ElementImage.Source = currentImage;
                }
            }
        }
        #endregion

        #region Methods

        public void Initial(string imageDir)
        {
            LoadDatas(imageDir);
            _maxCount = _bitmapImages.Count;
            _midCount = (int)Math.Ceiling(_maxCount / 2.0);
            ElementImage.Source = _bitmapImages[_midCount];
            _nextIndex = _currentIndex = _midCount;
            _leftImgIndex = LeftCount.Length;
        }

        /// <summary>
        /// 右方向变化
        /// </summary>
        /// <param name="index"></param>
        public void ChangeRightIndex(int index)
        {
            Console.WriteLine("ChangeRightIndex:" + index);
            if (index > 0)
            {
                var count = _rightImgIndex + index;
                if (count >= RightCount.Length)
                {
                    _rightImgIndex = RightCount.Length;
                    return;
                }
                _rightImgIndex = count;
                if (RightCount.Length > _rightImgIndex)
                {
                    _nextIndex += RightCount[_rightImgIndex];
                }
            }
            else
            {
                var count = _rightImgIndex + index;
                if (count < 0)
                {
                    _rightImgIndex = 0;
                    return;
                }
                if (RightCount.Length > _rightImgIndex)
                {
                    _nextIndex -= RightCount[_rightImgIndex];
                    _rightImgIndex = _rightImgIndex + index;
                }

            }
            _ischanging = true;
        }
        /// <summary>
        /// 左方向变化
        /// </summary>
        /// <param name="index"></param>
        public void ChangeLeftIndex(int index)
        {
            if (index > 0)
            {
                var count = _leftImgIndex - index;
                if (count < 0)
                {
                    _leftImgIndex = 0;
                    return;
                }
                _leftImgIndex = count;
                if (LeftCount.Length > _leftImgIndex)
                {
                    _nextIndex -= LeftCount[_leftImgIndex];
                }

            }
            else
            {
                var count = _leftImgIndex - index;
                if (count >= LeftCount.Length)
                {
                    _leftImgIndex = LeftCount.Length;
                    return;
                }
                _leftImgIndex = count;
                if (LeftCount.Length > _leftImgIndex)
                {
                    _nextIndex += LeftCount[_leftImgIndex];
                }

            }
            _ischanging = true;
        }

        /// <summary>
        /// 手举起来或者放下
        /// </summary>
        public void HandUpAndDown(HandDirection hand)
        {
            _ischanging = true;
            switch (hand)
            {
                case HandDirection.R:
                    _rightImgIndex = 0;
                    break;
                case HandDirection.L:
                    _leftImgIndex = LeftCount.Length;
                    break;
            }
            _nextIndex = _midCount;
        }

        /// <summary>
        /// 加载数据
        /// </summary>
        private void LoadDatas(string dirName)
        {
            var path = System.IO.Path.Combine(Environment.CurrentDirectory, dirName);
            var bitmapImages = ImageHelper.LoadImages(path);
            _bitmapImages.AddRange(bitmapImages);
        }

        public void Reset()
        {
            _leftImgIndex = LeftCount.Length;
            _rightImgIndex = 0;
            _nextIndex = _midCount;
            ElementImage.Source = _bitmapImages[_midCount];
        }


        public void Dispose()
        {
            CompositionTarget.Rendering -= CompositionTarget_Rendering;
            _bitmapImages = null;
        }

        #endregion

    }


}
