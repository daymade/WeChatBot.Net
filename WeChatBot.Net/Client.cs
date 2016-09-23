using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using WeChatBot.Net.Util.Extensions;

namespace WeChatBot.Net
{
    public class Client
    {
        private dynamic DEBUG;
        private string uuid;
        private string base_uri;
        private string redirect_uri;
        private string uin;
        private string sid;
        private string skey;
        private string pass_ticket;
        private string device_id;
        private dynamic base_request;
        private string sync_key_str;
        private dynamic sync_key;
        private string sync_host;
        private dynamic r;
        private dynamic temp_pwd;
        private dynamic session;
        private dynamic conf;
        private dynamic my_account;
        private dynamic member_list;
        private dynamic group_members;
        private dynamic account_info;
        private dynamic contact_list;
        private dynamic public_list;
        private dynamic group_list;
        private dynamic special_list;
        private dynamic encry_chat_room_id_list;
        private int file_index;

        private readonly string[] _specialUsers =
        {
            "newsapp",
            "fmessage",
            "filehelper",
            "weibo",
            "qqmail",
            "fmessage",
            "tmessage",
            "qmessage",
            "qqsync",
            "floatbottle",
            "lbsapp",
            "shakeapp",
            "medianote",
            "qqfriend",
            "readerapp",
            "blogapp",
            "facebookapp",
            "masssendapp",
            "meishiapp",
            "feedsapp",
            "voip",
            "blogappweixin",
            "weixin",
            "brandsessionholder",
            "weixinreminder",
            "wxid_novlwrv3lqwv11",
            "gh_22b87fa7cb3c",
            "officialaccounts",
            "notification_messages",
            "wxid_novlwrv3lqwv11",
            "gh_22b87fa7cb3c",
            "wxitil",
            "userexperience_alarm",
            "notification_messages"
        };

        public Client()
        {
            //r = redis.StrictRedis(host = "localhost", port = 6379, db = 0)
            DEBUG = false;
            uuid = "";
            base_uri = "";
            redirect_uri = "";
            uin = "";
            sid = "";
            skey = "";
            pass_ticket = "";
            device_id = "e" + "123456789012345";
            base_request = new ExpandoObject();
            sync_key_str = "";
            sync_key = new List<int>();
            sync_host = "";

            //文件缓存目录
            var temppwd = Path.Combine(Directory.GetCurrentDirectory(), "temp");
            if (!Directory.Exists(temppwd))
            {
                Directory.CreateDirectory(temppwd);
            }

            //session = SafeSession()
            //session.headers.update({ "User-Agent": "Mozilla/5.0 (X11; Linux i686; U;) Gecko/20070322 Kazehakase/0.4.5"})
            //conf = { "qr": "png"}

            my_account = new ExpandoObject(); //当前账户

            //所有相关账号: 联系人, 公众号, 群组, 特殊账号
            member_list = new ExpandoObject();

            //所有群组的成员, {"group_id1": [member1, member2, ...], ...}
            group_members = new ExpandoObject();

            //所有账户, {"group_member":{"id":{"type":"group_member", "info":{}}, ...}, "normal_member":{"id":{}, ...}}
            account_info = new ExpandoObject();
            //{ "group_member": { }, "normal_member": { } }

            contact_list = new ExpandoObject(); //联系人列表
            public_list = new ExpandoObject(); //公众账号列表
            group_list = new ExpandoObject(); //群聊列表
            special_list = new ExpandoObject(); //特殊账号列表
            encry_chat_room_id_list = new ExpandoObject(); //存储群聊的EncryChatRoomId，获取群内成员头像时需要用到
            file_index = 0;
        }

        /// <summary>
        ///     获取当前账户的所有相关账号(包括联系人、公众号、群聊、特殊账号)
        /// </summary>
        private void GetContact()
        {
            var url = base_uri + $@"/webwxgetcontact?pass_ticket={pass_ticket}&skey={skey}&r={GetRTime()}";
            r = session.post(url, new {data = "{}"});
            r.encoding = "utf-8";
            var dic = json.loads(r.text);
            member_list = dic["MemberList"];

            contact_list = new List<dynamic>();
            public_list = new List<dynamic>();
            special_list = new List<dynamic>();
            group_list = new List<dynamic>();


            foreach (var contact in member_list)
            {
                if (contact["VerifyFlag"] & 8 != 0) //# 公众号
                {
                    public_list.append(contact);
                    account_info["normal_member"][contact["UserName"]] = new
                                                                         {
                                                                             type = "public",
                                                                             info = contact
                                                                         };
                }
                else if (Enumerable.Contains(_specialUsers, contact["UserName"])) //# 特殊账户
                {
                    special_list.append(contact);
                    account_info["normal_member"][contact["UserName"]] = new
                                                                         {
                                                                             type = "special",
                                                                             info = contact
                                                                         };
                }
                else if (contact["UserName"].find("@@") != -1) //# 群聊
                {
                    group_list.append(contact);
                    account_info["normal_member"][contact["UserName"]] = new
                                                                         {
                                                                             type = "group",
                                                                             info = contact
                                                                         };
                }
                else if (contact["UserName"] == my_account["UserName"]) //# 自己
                {
                    account_info["normal_member"][contact["UserName"]] = new
                                                                         {
                                                                             type = "",
                                                                             @this = "",
                                                                             info = "",
                                                                             contact = ""
                                                                         };
                }
                else
                {
                    contact_list.append(contact);
                    account_info["normal_member"][contact["UserName"]] = new
                                                                         {
                                                                             type = "contact",
                                                                             info = contact
                                                                         };
                }
            }

            batch_get_group_members();


            foreach (var group in group_members)
            {
                foreach (var member in group_members[group])
                {
                    if (account_info.contains(member["UserName"]))
                    {
                        account_info["group_member"][member["UserName"]] = new {type = "group_member", info = member, group};
                    }
                }
            }
        }

        /// <summary>
        ///     批量获取所有群聊成员信息
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        void batch_get_group_members()
        {
            var url = base_uri + $@"/webwxbatchgetcontact?type=ex&r={GetRTime()}&pass_ticket={pass_ticket}";


            dynamic list = new ExpandoObject();
            foreach (var group in group_list)
            {
                list.add(new {UserName = group["UserName"], EncryChatRoomId = ""});
            }

            var @params = new
                          {
                              BaseRequest = base_request,
                              group_list.Count,
                              List = list
                          };

            dynamic r = session.post(url, new {data = json.dumps(@params)});
            r.encoding = "utf-8";
            var dic = json.loads(r.text);
            dynamic group_members = new ExpandoObject();
            dynamic encry_chat_room_id = new ExpandoObject();
            foreach (var group in dic["ContactList"])
            {
                var gid = group.UserName;
                var members = group["MemberList"];
                group_members[gid] = members;
                encry_chat_room_id[gid] = group["EncryChatRoomId"];
            }

            this.group_members = group_members;
            encry_chat_room_id_list = encry_chat_room_id;
        }

        private long GetRTime()
        {
            return DateTime.Now.ToUnixTime();
        }
    }

    public class json
    {
        public static Dictionary<object, object> loads(dynamic json)
        {
            return new Dictionary<object, object>();
        }

        public static string dumps(object obj)
        {
            return "";
        }
    }
}
