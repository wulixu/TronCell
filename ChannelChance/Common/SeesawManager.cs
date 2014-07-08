using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChannelChance.Common
{
    public class SeesawManager
    {
        public const int MaxCount = 3;
        private static SeesawManager _Instance = null;
        private HandDirection _HandDirection = HandDirection.None;
        public HandDirection HandDirection
        {
            get { return _HandDirection; }
            set { _HandDirection = value; }
        }
        private uint _LeftHandTimes = 0;
        public uint LeftHandTimes
        {
            get { return _LeftHandTimes; }
            set { _LeftHandTimes = value; }
        }
        private uint _RightHandTimes = 0;
        public uint RightHandTimes
        {
            get { return _RightHandTimes; }
            set { _RightHandTimes = value; }
        }

        private SeesawManager()
        {
            _HandDirection = HandDirection.None;
            _LeftHandTimes = 0;
            _RightHandTimes = 0;
        }

        static SeesawManager()
        {
            if (_Instance == null)
                _Instance = new SeesawManager();
        }

        public static SeesawManager Instance
        {
            get
            {
                return _Instance;
            }
        }

        public string MP4Path
        {
            get
            {
                return (_HandDirection == HandDirection.None || _LeftHandTimes > MaxCount || _RightHandTimes > MaxCount) ? string.Empty : Path.Combine(Appconfig.VideoPath, string.Format("IV_L{0}_R{1}_{2}.mp4", _LeftHandTimes, _RightHandTimes, _HandDirection.ToString()));
            }
        }

        public bool CanPlayMP4
        {
            get
            {
                return _RightHandTimes <= MaxCount && _LeftHandTimes <= MaxCount;
            }
        }

        public bool IsFinish
        {
            get
            {
                return _LeftHandTimes >= MaxCount || _RightHandTimes >= MaxCount;
            }
        }
        
        public string CurrentImg
        {
            get
            {
                return Path.Combine(Environment.CurrentDirectory, Appconfig.Seesawimages, string.Format("IV_L{0}_R{1}_{2}.png", _LeftHandTimes, _RightHandTimes, _HandDirection.ToString()));
            }
        }
        public string CurrentImgName
        {
            get { return string.Format("IV_L{0}_R{1}_{2}", _LeftHandTimes, _RightHandTimes, _HandDirection.ToString()); }
        }
    }

    public enum HandDirection
    {
        None,
        R,//右手
        L//左手
    }
}