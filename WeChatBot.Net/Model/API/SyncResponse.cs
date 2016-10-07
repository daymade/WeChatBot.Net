using WeChatBot.Net.Model.API.Base;

namespace WeChatBot.Net.Model.API
{
    public class SyncResponse
    {
        public BaseResponse BaseResponse { get; set; }
        public int AddMsgCount { get; set; }
        public object[] AddMsgList { get; set; }
        public int ModContactCount { get; set; }
        public object[] ModContactList { get; set; }
        public int DelContactCount { get; set; }
        public object[] DelContactList { get; set; }
        public int ModChatRoomMemberCount { get; set; }
        public object[] ModChatRoomMemberList { get; set; }
        public Profile Profile { get; set; }
        public int ContinueFlag { get; set; }
        public Synckey SyncKey { get; set; }
        public string SKey { get; set; }
        public Synckey SyncCheckKey { get; set; }
    }

    public class Profile
    {
        public int BitFlag { get; set; }
        public Username UserName { get; set; }
        public Nickname NickName { get; set; }
        public int BindUin { get; set; }
        public Bindemail BindEmail { get; set; }
        public Bindmobile BindMobile { get; set; }
        public int Status { get; set; }
        public int Sex { get; set; }
        public int PersonalCard { get; set; }
        public string Alias { get; set; }
        public int HeadImgUpdateFlag { get; set; }
        public string HeadImgUrl { get; set; }
        public string Signature { get; set; }

        public class Username
        {
            public string Buff { get; set; }
        }

        public class Nickname
        {
            public string Buff { get; set; }
        }

        public class Bindemail
        {
            public string Buff { get; set; }
        }

        public class Bindmobile
        {
            public string Buff { get; set; }
        }
    }
}