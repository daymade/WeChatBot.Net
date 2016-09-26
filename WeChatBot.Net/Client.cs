using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using QRCoder;
using WeChatBot.Net.Enums;
using WeChatBot.Net.Helper;
using WeChatBot.Net.Util;
using WeChatBot.Net.Util.Extensions;

namespace WeChatBot.Net
{
    public class Client
    {
        protected string UUID;

        /// <summary>
        /// 
        /// </summary>
        public QRCodeOutputType QRCodeOutputType { get; set; }

        public bool Debug;
        private dynamic base_uri;
        private dynamic redirect_uri;
        private dynamic uin;
        private dynamic sid;
        private dynamic skey;
        private dynamic pass_ticket;
        private dynamic device_id;
        private dynamic base_request;
        private dynamic sync_key_str;
        private dynamic sync_key;
        private dynamic sync_host;
        private dynamic r;
        private dynamic session;
        private dynamic configuration;
        private dynamic my_account;
        private dynamic member_list;
        private dynamic group_members;
        private dynamic account_info;
        private dynamic contact_list;
        private dynamic public_list;
        private dynamic group_list;
        private dynamic special_list;
        private dynamic encry_chat_room_id_list;
        private dynamic file_index;

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

        private static readonly Random Random = new Random();
        private static readonly Logger Logger = new Logger();
        private static readonly FileManager FileManager = new FileManager();
        private static readonly QRCodeHelper QRCodeHelper = new QRCodeHelper();

        public Client()
        {
            //r = redis.StrictRedis(host = "localhost", port = 6379, db = 0)
            Debug = false;
            UUID = "";
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


            //session = SafeSession()
            //session.headers.update({ "User-Agent": "Mozilla/5.0 (X11; Linux i686; U;) Gecko/20070322 Kazehakase/0.4.5"})
            //configuration = { "qr": "png"}

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

        public async Task Run()
        {
            var getUuidSuccess = await GetUuid();
            if (!getUuidSuccess.Item1)
            {
                Logger.Error("Failed to get login token. ");
            }

            UUID = getUuidSuccess.Item2;

            await QRCodeHelper.ShowQRCode(this.UUID);

            Logger.Info("Please use WeChat to scan the QR code .");

            var result = wait4login();

            if (result != "SUCCESS")
            {
                Logger.Error("Web WeChat login failed. failed code={result}");
                return;
            }

            if (login())
            {
                Logger.Info("Web WeChat login succeed .");
            }
            else
            {
                Logger.Error("Web WeChat login failed .");
                return;
            }
            if (init())
            {
                Logger.Info("Web WeChat init succeed .");
            }
            else
            {
                Logger.Error("Web WeChat init failed .");
                return;
            }

            status_notify();
            get_contact();

            Logger.Info($@"Get {contact_list.Count()} contacts");
            Logger.Info(@"Start to process messages .");
            proc_msg();
        }

        private void get_contact()
        {
            throw new NotImplementedException();
        }

        private void status_notify()
        {
            throw new NotImplementedException();
        }

        private void proc_msg()
        {
            throw new NotImplementedException();
        }

        private bool init()
        {
            throw new NotImplementedException();
        }

        private bool login()
        {
            throw new NotImplementedException();
        }

        private object wait4login()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     获取当前账户的所有相关账号(包括联系人、公众号、群聊、特殊账号)
        /// </summary>
        private void GetContact()
        {
            var url = base_uri + $@"/webwxgetcontact?pass_ticket={pass_ticket}&skey={skey}&r={NowUnix()}";
            r = session.post(url, new { data = "{}" });
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
                        account_info["group_member"][member["UserName"]] = new { type = "group_member", info = member, group };
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
            var url = base_uri + $@"/webwxbatchgetcontact?type=ex&r={NowUnix()}&pass_ticket={pass_ticket}";


            dynamic list = new ExpandoObject();
            foreach (var group in group_list)
            {
                list.add(new { UserName = group["UserName"], EncryChatRoomId = "" });
            }

            var @params = new
            {
                BaseRequest = base_request,
                group_list.Count,
                List = list
            };

            dynamic r = session.post(url, new { data = json.dumps(@params) });
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

        /// <summary>
        /// Get an unique token within current session, generated by server
        /// </summary>
        /// <returns>isSuccess and the uuid</returns>
        /// <remarks>
        /// If failed, uuid will be empty string
        /// </remarks>
        public async Task<Tuple<bool,string>> GetUuid()
        {
            var url = @"https://login.weixin.qq.com/jslogin";
            var @params = new
            {
                appid = "wx782c26e4c19acffb",
                fun = "new",
                lang = "zh_CN",
                _ = NowUnix() * 1000 + Random.Next(1, 999)
            };

            var data = await url.SetQueryParams(@params).GetStringAsync();

            var match = Regex.Match(data, @"window.QRLogin.code = (?<code>\d+); window.QRLogin.uuid = ""(?<uuid>\S+?)""");
            if (!match.Success)
            {
                return new Tuple<bool, string>(false, "");
            }

            var code = match.Groups["code"].Value;
            var uuid = match.Groups["uuid"].Value;

            return new Tuple<bool, string>(code == "200", uuid);
        }


        private long NowUnix()
        {
            return DateTime.Now.ToUnixTime();
        }
    }

    /// <summary>
    /// temp stub
    /// </summary>
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
