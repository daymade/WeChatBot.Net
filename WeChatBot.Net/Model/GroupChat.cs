using System.Collections.Generic;

namespace WeChatBot.Net.Model
{
    public class GroupChat
    {
        /// <summary>
        /// �洢Ⱥ�ĵ�EncryChatRoomId����ȡȺ�ڳ�Աͷ��ʱ��Ҫ�õ�
        /// </summary>
        public string EncryChatRoomId { get; set; }
        public List<Member> Members { get; set; }
        public string Gid { get; set; }
    }
}