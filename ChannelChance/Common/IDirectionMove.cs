using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChannelChance.Common
{
    public interface IDirectionMove
    {
        /// <summary>
        /// 左手移动
        /// </summary>
        /// <param name="count"></param>
        void LeftHandMove(int count);
        /// <summary>
        /// 右手移动
        /// </summary>
        /// <param name="count"></param>
        void RightHandMove(int count);

        /// <summary>
        /// 左手举起来
        /// </summary>
        /// <param name="count"></param>
        void LeftHandUp(int count);

        /// <summary>
        /// 右手举起来
        /// </summary>
        /// <param name="count"></param>
        void RightHandUp(int count);
        /// <summary>
        /// 左手举起后上下移动
        /// </summary>
        /// <param name="count"></param>
        void LeftHandsMoveY(int count);
        /// <summary>
        /// 右手举起后上下移动
        /// </summary>
        /// <param name="count"></param>
        void RightHandsMoveY(int count);

        bool IsMediaPlaying { get; }
    }
}
