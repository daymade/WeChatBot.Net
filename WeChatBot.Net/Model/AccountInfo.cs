using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeChatBot.Net.Model
{
    public class AccountInfo
    {
        public List<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();
        public List<NormalMember> NormalMembers { get; set; } = new List<NormalMember>();
    }

    public class GroupMember
    {
        public GroupMemberInfo GroupMemberInfo { get; set; }
        public string GroupName { get; set; }
        public MemberType Type { get; set; }
    }

    public class GroupMemberInfo
    {
        public string UserName { get; set; }
        public string RemarkPYQuanPin { get; set; }
        public string DisplayName { get; set; }
        public string KeyWord { get; set; }
        public string PYInitial { get; set; }
        public int Uin { get; set; }
        public int MemberStatus { get; set; }
        public string PYQuanPin { get; set; }
        public string RemarkPYInitial { get; set; }
        public string NickName { get; set; }
        public int AttrStatus { get; set; }
    }

    public class NormalMember
    {
        public NormalMemberInfo Info { get; set; }
        public MemberType Type { get; set; }
    }

    public class NormalMemberInfo
    {
        public string UserName { get; set; }
        public string City { get; set; }
        public string DisplayName { get; set; }
        public int UniFriend { get; set; }
        public object[] MemberList { get; set; }
        public string PYQuanPin { get; set; }
        public string RemarkPYInitial { get; set; }
        public int Sex { get; set; }
        public int AppAccountFlag { get; set; }
        public int VerifyFlag { get; set; }
        public string Province { get; set; }
        public string KeyWord { get; set; }
        public string RemarkName { get; set; }
        public string PYInitial { get; set; }
        public int ChatRoomId { get; set; }
        public int HideInputBarFlag { get; set; }
        public string EncryChatRoomId { get; set; }
        public int AttrStatus { get; set; }
        public int SnsFlag { get; set; }
        public int MemberCount { get; set; }
        public int OwnerUin { get; set; }
        public string Alias { get; set; }
        public string Signature { get; set; }
        public int ContactFlag { get; set; }
        public string NickName { get; set; }
        public string RemarkPYQuanPin { get; set; }
        public string HeadImgUrl { get; set; }
        public int Uin { get; set; }
        public int StarFriend { get; set; }
        public int Statues { get; set; }
    }
}
