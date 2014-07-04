using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChannelChance.Kinect
{
    public class KinectGestureEventArgs : EventArgs
    {
        public float Distance { get; set; }

        public int ActionStep { get; set; }
        public KinectGestureType GestureType { get; set; }

    }

    public enum KinectGestureType
    {
        RightHandsUP,
        LeftHandsUP,
        RightHandsMove,
        LeftHandsMove
    }
}
