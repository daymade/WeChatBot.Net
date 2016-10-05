using System.Collections.Generic;

namespace WeChatBot.Net.Model
{
    public class GroupChat
    {
        /// <summary>
        /// 存储群聊的EncryChatRoomId，获取群内成员头像时需要用到
        /// </summary>
        public string EncryChatRoomId { get; set; }
        public List<Member> Members { get; set; }
        public string Gid { get; set; }
    }
}