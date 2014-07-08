using KinectChannel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChannelChance.Kinect
{
    public abstract class GestureDetectorBase
    {
        /// <summary>
        /// 人所在的最近距离
        /// </summary>
        public float PlayerZDistance { get; set; }
        //动作持续最长时间ms
        public int MinTimeDuration { get; set; }
        /// <summary>
        /// 动作持续最短时间ms
        /// </summary>
        public int MaxTimeDuration { get; set; }
        /// <summary>
        /// 动作持续总共行程
        /// </summary>
        public float GestureDistance { get; set; }

        /// <summary>
        /// 动作阈值
        /// </summary>
        public float GestureGateDistance { get; set; }


        /// <summary>
        /// 是否识别动作
        /// </summary>
        /// <param name="PlayerJoints"></param>
        /// <returns></returns>
        public virtual bool GetstureDetected( KinectPlayer player)
        {
            return false;
        }

        public GestureDetectorBase()
        {
            this.MinTimeDuration = 0;
            this.MaxTimeDuration = 300;
            this.PlayerZDistance = 1.3f;
            this.GestureGateDistance = 0.06f;
        }

        protected void Get2Joints(List<PlayerJoints> PlayerJoints, out PlayerJoints startJoints, out PlayerJoints nowJoints)
        {
            startJoints = null;
            nowJoints = null;
            foreach (var item in PlayerJoints)
            {

                if (DateTime.Now.Subtract(item.TimeStamp).TotalMilliseconds < this.MaxTimeDuration)
                {
                    startJoints = item;
                    break;
                }
            }
            if (PlayerJoints.Count > 0)
            {
                nowJoints = PlayerJoints[PlayerJoints.Count - 1];
            }
        }
    }
}
