using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using Flurl;
using Flurl.Http;
using Flurl.Http.Configuration;
using Flurl.Http.Xml;
using Flurl.Util;
using Newtonsoft.Json;
using QRCoder;
using WeChatBot.Net.Enums;
using WeChatBot.Net.Extensions;
using WeChatBot.Net.Helper;
using WeChatBot.Net.Model;
using WeChatBot.Net.Util;

namespace WeChatBot.Net
{
    public class Client
    {
        protected string UUID;
        public bool Debug;
        protected string BaseUri;
        private string _redirectUri;

        private string wxsid;
        private string skey;
        private string pass_ticket;
        private int uin;

        /// <summary>
        /// Choose output to console or show as png
        /// </summary>
        public QRCodeOutputType QRCodeOutputType { get; set; }

        private dynamic device_id;
        protected BaseRequest _baseRequest;
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
        private static readonly HttpClientContainer HttpClientContainer = new HttpClientContainer();

        public Client()
        {
            //r = redis.StrictRedis(host = "localhost", port = 6379, db = 0)
            Debug = false;
            UUID = "";
            BaseUri = "";
            _redirectUri = "";
            wxsid = "";
            skey = "";
            pass_ticket = "";
            device_id = "e" + "123456789012345";
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

            var result = await WaitingUserProcessing();

            if (result != HttpStatusCode.OK)
            {
                Logger.Error("Web WeChat login failed. failed code={result}");
                return;
            }

            if (await Login())
            {
                Logger.Info("Web WeChat login succeed .");
            }
            else
            {
                Logger.Error("Web WeChat login failed .");
                return;
            }

            if (await Init())
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

        private async Task<bool> Init()
        {
            var url = BaseUri + "/webwxinit?r={NowUnix()}&lang=en_US&pass_ticket={pass_ticket}";
            var dic =  await url.WithClient(HttpClientContainer.GetClient())
                                .PostJsonAsync(new { BaseRequest = _baseRequest })
                                .ReceiveJson<dynamic>();
            this.sync_key = dic.SyncKey;
            this.my_account = dic.User;
            sync_key_str = string.Join("|", ((List<dynamic>) sync_key.List).Select(x => x.Key + x.Val));
            return dic.BaseResponse.Ret == 0;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task<bool> Login()
        {
            if (len(this._redirectUri) < 4)
            {
                Logger.Error(@"Login failed due to network problem, please try again.");
                return false;
            }

            var ret = await _redirectUri.GetXmlAsync<error>();

            if (new[] { ret.skey, ret.wxsid, ret.pass_ticket }.Any(string.IsNullOrEmpty) ||
                ret.wxuin <= 0)
            {
                return false;
            }

            this._baseRequest = new BaseRequest
            {
                DeviceID = device_id,
                Sid = ret.wxsid,
                Skey = ret.skey,
                Uin = ret.wxuin
            };
            return true;
        }


        private int len(string redirectUri)
        {
            return redirectUri.Length;
        }

        /// <summary>
        /// Wait for user interaction to scan the qrcode and confirm login on phone
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        ///  http comet:
        ///  	tip=1, 等待用户扫描二维码,
        ///  		201: scaned
        ///  		408: timeout
        ///  	tip=0, 等待用户确认登录,
        ///  		200: confirmed
        /// </remarks>
        private async Task<HttpStatusCode> WaitingUserProcessing()
        {
            var tip = 1;

            var tryLaterSecs = 1;
            var maxRetryTimes = 10;

            var retryTime = maxRetryTimes;
            while (retryTime > 0)
            {
                var url = $@"https://login.weixin.qq.com/cgi-bin/mmwebwx-bin/login?tip={tip}&uuid={UUID}&_={NowUnix()}";

                var response = await url.WithClient(HttpClientContainer.GetClient())
                                        .WithHeader("Connection", "keep-alive")
                                        .WithHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.2785.116 Safari/537.36")
                                        .WithHeader("Accept", "*/*")
                                        .WithHeader("Accept-Encoding", "gzip, deflate, sdch, br")
                                        .WithHeader("Accept-Language", "zh-CN,zh;q=0.8,en-US;q=0.6,en;q=0.4,zh-TW;q=0.2,ja;q=0.2,pt;q=0.2")
                                        .GetAsync();
                if (response.StatusCode == HttpStatusCode.Created)
                {
                    Logger.Info("Please confirm to login .");
                    tip = 0;
                }
                else if (response.StatusCode == HttpStatusCode.OK)  //# 确认登录成功
                {
                    var p = Regex.Match(@"window.redirect_uri=""(\S+?)"";", await response.Content.ReadAsStringAsync());

                    var redirectUri = p.Groups[1] + "&fun=new";
                    this._redirectUri = redirectUri;
                    this.BaseUri = redirectUri.Substring(0, redirectUri.LastIndexOf("/", StringComparison.Ordinal));

                    return response.StatusCode;
                }
                else if (response.StatusCode == HttpStatusCode.RequestTimeout)
                {
                    Logger.Error($@"WeChat login timeout. retry in {tryLaterSecs} secs later...");

                    tip = 1;  //# 重置
                    retryTime -= 1;
                    Thread.Sleep(tryLaterSecs);
                }
                else
                {
                    Logger.Error($@"WeChat login exception return_code={response.StatusCode}. retry in {tryLaterSecs} secs later...");

                    tip = 1;
                    retryTime -= 1;
                    Thread.Sleep(tryLaterSecs);
                }
            }

            return HttpStatusCode.NotFound;
        }

        /// <summary>
        ///     获取当前账户的所有相关账号(包括联系人、公众号、群聊、特殊账号)
        /// </summary>
        private void GetContact()
        {
            var url = BaseUri + $@"/webwxgetcontact?pass_ticket={pass_ticket}&skey={skey}&r={NowUnix()}";
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
            var url = BaseUri + $@"/webwxbatchgetcontact?type=ex&r={NowUnix()}&pass_ticket={pass_ticket}";


            dynamic list = new ExpandoObject();
            foreach (var group in group_list)
            {
                list.add(new { UserName = group["UserName"], EncryChatRoomId = "" });
            }

            var @params = new
            {
                BaseRequest = _baseRequest,
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
        public async Task<Tuple<bool, string>> GetUuid()
        {
            var url = @"https://login.weixin.qq.com/jslogin";
            var @params = new
            {
                appid = "wx782c26e4c19acffb",
                fun = "new",
                lang = "zh_CN",
                _ = NowUnix() * 1000 + Random.Next(1, 999)
            };

            var data = await url.SetQueryParams(@params)
                                .WithClient(HttpClientContainer.GetClient())
                                .WithHeader("Cache-Control", "no-cache, must-revalidate")
                                .GetStringAsync().ConfigureAwait(false);

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
