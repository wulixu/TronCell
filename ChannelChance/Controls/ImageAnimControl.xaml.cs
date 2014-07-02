using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ChannelChance.Common;

namespace ChannelChance.Controls
{
    /// <summary>
    /// Interaction logic for ImageAnimControl.xaml
    /// </summary>
    public partial class ImageAnimControl : UserControl
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
        private int[] _leftCount = new int[4] { 6, 6, 6, 6 };
        private int[] _rightCount = new int[4] { 6, 6, 6, 6 };
        private int _maxCount;
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

                _isRight = count > 0;

                if (count == 0)
                {
                    _ischanging = false;
                    if (_leftImgIndex > _maxCount)
                    {
                        if (PlayLeftNextPage != null)
                            PlayLeftNextPage.Invoke(1);
                    }
                    if (_rightImgIndex > _maxCount)
                    {
                        if (PlayRightNextPage != null)
                            PlayRightNextPage.Invoke(1);
                    }

                    return;
                }

                if (_isRight)
                {
                    var currentImage = _bitmapImages[_currentIndex];
                    ElementImage.Source = currentImage;
                    _currentIndex++;
                }
                else
                {
                    var currentImage = _bitmapImages[_currentIndex];
                    ElementImage.Source = currentImage;
                    _currentIndex--;
                }
            }
        }
        #endregion

        #region Methods

        public void Initial(string imageDir)
        {
            LoadDatas(imageDir);
        }

        /// <summary>
        /// 右方向变化
        /// </summary>
        /// <param name="index"></param>
        public void ChangeRightIndex(int index)
        {
            if (index > 0)
            {
                _rightImgIndex = _rightImgIndex + index;
                if (_rightCount.Contains(_rightImgIndex))
                    _nextIndex += _rightCount[_rightImgIndex];
            }
            else
            {
                if (_rightCount.Contains(_rightImgIndex))
                    _nextIndex -= _rightCount[_rightImgIndex];
                _rightImgIndex = _rightImgIndex + index;
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
                _leftImgIndex = _leftImgIndex + index;
                if (_leftCount.Contains(_leftImgIndex))
                    _nextIndex += _leftCount[_rightImgIndex];
            }
            else
            {
                if (_leftCount.Contains(_leftImgIndex))
                    _nextIndex -= _leftCount[_rightImgIndex];
                _leftImgIndex = _leftImgIndex + index;
            }
            _ischanging = true;
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

        #endregion
    }
}
