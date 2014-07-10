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
        //状态
        RightHandsUP,
        LeftHandsUP,

        //具体move
        RightHandsMove,
        LeftHandsMove,
        RightHandsMoveY,
        LeftHandsMoveY,

        //双臂伸平
        Fly
    }
}
