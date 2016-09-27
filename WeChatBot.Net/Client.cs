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
using WeChatBot.Net.Model.API;
using WeChatBot.Net.Model.API.Base;
using WeChatBot.Net.Util;

namespace WeChatBot.Net
{
    public class Client
    {
        private string _baseUri;
        private string _redirectUri;

        private string _wxsid;
        private string _skey;
        private string _passTicket;
        private int _wxuin;

        protected string UUID;
        protected User MyAccount;
        protected Synckey SyncKey;
        protected string SyncKeyAsString;

        protected BaseRequest BaseRequest
        {
            get
            {
                return new BaseRequest()
                {
                    Skey = _skey,
                    DeviceID = device_id,
                    Sid = _wxsid,
                    Uin = _wxuin
                };
            }
        }

        public Settings Settings
        {
            get { return _settings; }
        }


        private dynamic device_id;
        private dynamic sync_host;
        private dynamic r;
        private dynamic session;
        private dynamic configuration;
        private dynamic member_list;
        private dynamic group_members;
        private dynamic account_info;
        private dynamic contact_list;
        private dynamic public_list;
        private dynamic group_list;
        private dynamic special_list;
        private dynamic encry_chat_room_id_list;
        private dynamic file_index;

        private static readonly Random Random = new Random();
        private static readonly Logger Logger = new Logger();
        private static readonly FileManager FileManager = new FileManager();
        private static readonly QRCodeHelper QRCodeHelper = new QRCodeHelper();
        private static readonly HttpClientContainer HttpClientContainer = new HttpClientContainer();
        private readonly GlobalConstant _globalConstant = new GlobalConstant();
        private readonly Settings _settings;

        public Client() : this(new Settings())
        {
            
        }

        public Client(Settings settings)
        {
            _settings = settings;

            //r = redis.StrictRedis(host = "localhost", port = 6379, db = 0)
            device_id = "e" + "123456789012345";
            sync_host = "";

            //configuration = { "qr": "png"}

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

            await QRCodeHelper.ShowQRCode(UUID, _settings.QRCodeOutputType);

            Logger.Info("Please use WeChat to scan the QR code .");

            var actions = new List<Func<Task<bool>>>()
                          {
                              WaitingUserProcessing,
                              Login,
                              Init,
                              StatusNotify,
                              GetContact
                          };
            foreach (var action in actions)
            {
                await ThrowIfFailed(Log(action));
            }

            Logger.Info($@"Get {contact_list.Count()} contacts");

            await ProcessMessage();
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
        private async Task<bool> WaitingUserProcessing()
        {
            var tip = 1;

            var tryLaterSecs = 1;
            var maxRetryTimes = 10;

            var retryTime = maxRetryTimes;
            while (retryTime > 0)
            {
                var url = $@"https://login.weixin.qq.com/cgi-bin/mmwebwx-bin/login?tip={tip}&uuid={UUID}&_={NowUnix()}";

                var response = await url.WithClient(HttpClientContainer.GetClient())
                                        .GetAsync()
                                        .ReceiveString();

                var code = Regex.Match(response, @"window.code=(\d+);").Groups[1].Value;

                if (code == HttpStatusCode.Created.ToString("d"))
                {
                    Logger.Info("Please confirm to login .");
                    tip = 0;
                }
                else if (code == HttpStatusCode.OK.ToString("d"))  //# 确认登录成功
                {
                    var redirectUri = Regex.Match(response, @"window.redirect_uri=""(\S+?)"";", RegexOptions.Multiline | RegexOptions.IgnoreCase).Groups[1].Value;
                    if (string.IsNullOrEmpty(redirectUri))
                    {
                        Logger.Error("redirect_uri not found");
                        return false;
                    }

                    var redirectUrl = redirectUri + "&fun=new";

                    _redirectUri = redirectUrl;
                    _baseUri = redirectUri.Substring(0, redirectUri.LastIndexOf("/", StringComparison.Ordinal));

                    return true;
                }
                else if (code == HttpStatusCode.RequestTimeout.ToString("d"))
                {
                    Logger.Info($@"WeChat login timeout. retry in {tryLaterSecs} secs later...");

                    tip = 1;  //# 重置
                    retryTime -= 1;
                    Thread.Sleep(tryLaterSecs);
                }
                else
                {
                    Logger.Info($@"WeChat login exception return_code={code}. retry in {tryLaterSecs} secs later...");

                    tip = 1;
                    retryTime -= 1;
                    Thread.Sleep(tryLaterSecs);
                }
            }

            Logger.Error($"Web WeChat login failed. ");
            return false;
        }

        private async Task<bool> Login()
        {
            if (len(_redirectUri) < 4)
            {
                Logger.Error(@"Login failed due to network problem, please try again.");
                return false;
            }

            var response = await _redirectUri.GetXmlAsync<LoginResponse>();

            if (new[] { response.skey, response.wxsid, response.pass_ticket }.Any(string.IsNullOrEmpty) ||
                response.wxuin <= 0)
            {
                return false;
            }

            _skey = response.skey;
            _wxsid = response.wxsid;
            _passTicket = response.pass_ticket;
            _wxuin = response.wxuin;

            return true;
        }

        private async Task<bool> Init()
        {
            var url = _baseUri + $"/webwxinit?r={NowUnix()}&lang=en_US&pass_ticket={_passTicket}";

            var initResponse = await url.WithClient(HttpClientContainer.GetClient())
                               .PostJsonAsync(new { BaseRequest })
                               .ReceiveJson<InitResponse>();

            MyAccount = initResponse.User;
            SyncKey = initResponse.SyncKey;
            SyncKeyAsString = string.Join("|", SyncKey.List.Select(x => x.Key + x.Val));

            return initResponse.BaseResponse.Ret == 0;
        }

        private async Task<bool> StatusNotify()
        {
            var url = _baseUri + $@"/webwxstatusnotify?lang=zh_CN&pass_ticket={_passTicket}";

            var data = new
                       {
                           BaseRequest,
                           Code = 3,
                           FromUserName = MyAccount.UserName,
                           ToUserName = MyAccount.UserName,
                           ClientMsgId = NowUnix()
                       };
            var statusNotifyResponse = await url.WithClient(HttpClientContainer.GetClient())
                                                .PostJsonAsync(data)
                                                .ReceiveJson<StatusNotifyResponse>();
            
            return statusNotifyResponse.BaseResponse.Ret == 0;
        }

        /// <summary>
        ///    All related accounts for current account (including contacts, public accounts, group chattings, special accounts) 
        /// </summary>
        private async Task<bool> GetContact()
        {
            return true;
            var url = _baseUri + $@"/webwxgetcontact?pass_ticket={_passTicket}&skey={_skey}&r={NowUnix()}";
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
                else if (Enumerable.Contains(_globalConstant.SpecialUsers, contact["UserName"])) //# 特殊账户
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
                else if (contact["UserName"] == MyAccount.UserName) //# 自己
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

            await batch_get_group_members();

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

            return true;
        }

        private async Task ProcessMessage()
        {
            Logger.Info(@"Start to process messages .");
            throw new NotImplementedException();
        }


        private int len(string redirectUri)
        {
            return redirectUri.Length;
        }

        

       

        /// <summary>
        ///     批量获取所有群聊成员信息
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        async Task batch_get_group_members()
        {
            var url = _baseUri + $@"/webwxbatchgetcontact?type=ex&r={NowUnix()}&pass_ticket={_passTicket}";


            dynamic list = new ExpandoObject();
            foreach (var group in group_list)
            {
                list.add(new { UserName = group["UserName"], EncryChatRoomId = "" });
            }

            var @params = new
            {
                BaseRequest = BaseRequest,
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

        private async Task<bool> Log(Func<Task<bool>> action)
        {
            var result = await action();

            var resultDescripitn = result ? "succeed" : "failed";
            Logger.Info($@"Web WeChat {action.Method.Name} {resultDescripitn} .");

            return result;
        }

        private async Task<bool> ThrowIfFailed(Task<bool> action)
        {
            if (!await action)
            {
                throw new Exception(action.GetType().Name);
            }
            return true;
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
