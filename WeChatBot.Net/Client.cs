using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Flurl;
using Flurl.Http;
using Flurl.Http.Xml;
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

        /// <summary>
        ///     所有账户, {"group_member":{"id":{"type":"group_member", "info":{}}, ...}, "normal_member":{"id":{}, ...}}
        /// </summary>
        protected AccountInfo AccountInfo = new AccountInfo();

        /// <summary>
        ///     所有群组的成员, {"group_id1": [member1, member2, ...], ...}
        /// </summary>
        protected List<GroupChat> GroupChats = new List<GroupChat>();

        /// <summary>
        ///     所有相关账号: 联系人, 公众号, 群组, 特殊账号
        /// </summary>
        protected List<Member> MemberList = new List<Member>();

        /// <summary>
        ///     联系人列表
        /// </summary>
        protected List<Member> ContactList = new List<Member>();

        /// <summary>
        ///     公众账号列表
        /// </summary>
        protected List<Member> PublicList = new List<Member>();

        /// <summary>
        ///     群聊列表
        /// </summary>
        protected List<Member> GroupList = new List<Member>();

        /// <summary>
        ///     特殊账号列表
        /// </summary>
        protected List<Member> SpecialList = new List<Member>();

        private dynamic file_index;

        private static readonly Random Random = new Random();
        private static readonly Logger Logger = new Logger();
        private static readonly FileManager FileManager = new FileManager();
        private static readonly QRCodeHelper QRCodeHelper = new QRCodeHelper();
        private static readonly HttpClientContainer HttpClientContainer = new HttpClientContainer();
        private readonly GlobalConstant _globalConstant = new GlobalConstant();
        private readonly Settings _settings;

        public static readonly IMapper Mapper;

        static Client()
        {
            var config = new MapperConfiguration(cfg =>
                                                 {
                                                     cfg.CreateMap<Member, NormalMemberInfo>();
                                                     cfg.CreateMap<Member, GroupMemberInfo>();
                                                 });
            Mapper = config.CreateMapper();
        }

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

            Logger.Info($@"Get {ContactList.Count()} contacts");

            await ProcessMessage();
        }

        /// <summary>
        ///     Get an unique token within current session, generated by server
        /// </summary>
        /// <returns>isSuccess and the uuid</returns>
        /// <remarks>
        ///     If failed, uuid will be empty string
        /// </remarks>
        public async Task<Tuple<bool, string>> GetUuid()
        {
            var url = @"https://login.weixin.qq.com/jslogin";
            var @params = new
                          {
                              appid = "wx782c26e4c19acffb",
                              fun = "new",
                              lang = "zh_CN",
                              _ = NowUnixShifted()
                          };

            var data = await url.SetQueryParams(@params)
                                .WithFlurlClient()
                                .EnableCookies()
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

        /// <summary>
        ///     Wait for user interaction to scan the qrcode and confirm login on phone
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        ///     http comet:
        ///     tip=1, 等待用户扫描二维码,
        ///     201: scaned
        ///     408: timeout
        ///     tip=0, 等待用户确认登录,
        ///     200: confirmed
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

                var response = await url.WithFlurlClient()
                                        .GetStringAsync();

                var code = Regex.Match(response, @"window.code=(\d+);").Groups[1].Value;

                if (code == HttpStatusCode.Created.ToString("d"))
                {
                    Logger.Info("Please confirm to login .");
                    tip = 0;
                }
                else if (code == HttpStatusCode.OK.ToString("d")) //# 确认登录成功
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

                    tip = 1; //# 重置
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
            if (_redirectUri.Length < 4)
            {
                Logger.Error(@"Login failed due to network problem, please try again.");
                return false;
            }

            var response = await _redirectUri.WithFlurlClient()
                                             .GetXmlAsync<LoginResponse>();

            if (new[] {response.skey, response.wxsid, response.pass_ticket}.Any(string.IsNullOrEmpty) ||
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

            var initResponse = await url.WithFlurlClient()
                                        .PostJsonAsync(new {BaseRequest})
                                        .ReceiveJson<InitResponse>();

            MyAccount = initResponse.User;
            SyncKey = initResponse.SyncKey;
            SyncKeyAsString = string.Join("|", SyncKey.List.Select(x => $@"{x.Key}_{x.Val}"));

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
            var statusNotifyResponse = await url.WithFlurlClient()
                                                .PostJsonAsync(data)
                                                .ReceiveJson<StatusNotifyResponse>();

            return statusNotifyResponse.BaseResponse.Ret == 0;
        }

        /// <summary>
        ///     All related accounts for current account (including contacts, public accounts, group chattings, special accounts)
        /// </summary>
        private async Task<bool> GetContact()
        {
            var url = _baseUri + $@"/webwxgetcontact?pass_ticket={_passTicket}&skey={_skey}&r={NowUnix()}";
            var response = await url.WithFlurlClient()
                                    .PostJsonAsync(new {})
                                    .ReceiveJson<GetContactResponse>();

            MemberList = response.MemberList;

            ContactList = new List<Member>();
            PublicList = new List<Member>();
            SpecialList = new List<Member>();
            GroupList = new List<Member>();

            foreach (var contact in MemberList)
            {
                if (IsPublickAccount(contact)) //# 公众号
                {
                    PublicList.Add(contact);
                    AddNormalContact(contact, MemberType.Public);
                }
                else if (_globalConstant.SpecialUsers.Contains(contact.UserName)) //# 特殊账户
                {
                    SpecialList.Add(contact);
                    AddNormalContact(contact, MemberType.Special);
                }
                else if (contact.UserName.StartsWith("@@")) //# 群聊
                {
                    GroupList.Add(contact);
                    AddNormalContact(contact, MemberType.Group);
                }
                else if (contact.UserName == MyAccount.UserName) //# 自己
                {
                    AddNormalContact(contact, MemberType.Self);
                }
                else
                {
                    ContactList.Add(contact);
                    AddNormalContact(contact, MemberType.Contact);
                }
            }

            await BatchGetGroupMembers();

            //TODO what if a contact name been when contact exists in two group
            foreach (var group in GroupChats)
            {
                foreach (var member in group.Members)
                {
                    if (AccountInfo.GroupMembers.All(x => x.GroupMemberInfo.UserName != member.UserName))
                    {
                        AccountInfo.GroupMembers.Add(new GroupMember()
                                                     {
                                                         GroupMemberInfo = Mapper.Map<GroupMemberInfo>(member),
                                                         GroupName = group.Gid,
                                                         Type = MemberType.GroupMember
                                                     });
                    }
                }
            }

            return true;
        }

        async Task<bool> BatchGetGroupMembers()
        {
            var url = _baseUri + $@"/webwxbatchgetcontact?type=ex&r={NowUnix()}&pass_ticket={_passTicket}";

            var @params = new
                          {
                              BaseRequest,
                              GroupList.Count,
                              List = GroupList.Select(group => new {group.UserName, EncryChatRoomId = ""}).ToList()
                          };

            var result = await url.WithFlurlClient()
                                  .PostJsonAsync(@params)
                                  .ReceiveJson<BatchGetGroupMembersReponse>();

            var groupChats = result.ContactList.Select(x => new GroupChat()
                                                            {
                                                                EncryChatRoomId = x.EncryChatRoomId,
                                                                Gid = x.UserName,
                                                                Members = x.MemberList.ToList()
                                                            }).ToList();

            GroupChats = groupChats;

            return true;
        }

        private async Task ProcessMessage()
        {
            Logger.Info(@"Start to process messages .");
            throw new NotImplementedException();
        }

        private void AddNormalContact(Member contact, MemberType type)
        {
            AccountInfo.NormalMembers.Add(new NormalMember()
                                          {
                                              Info = Mapper.Map<NormalMemberInfo>(contact),
                                              Type = type
                                          });
        }

        private static bool IsPublickAccount(Member contact)
        {
            return (contact.VerifyFlag & 8) != 0;
        }

        #region Utils

        private double NowUnixShifted()
        {
            return NowUnix() * 1e4 + Random.Next(1, 9999);
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

        #endregion
    }
}
