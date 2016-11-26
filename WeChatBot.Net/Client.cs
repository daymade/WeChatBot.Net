using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
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
        //FIX 1102
        private string _baseHost;
        private string _redirectUri;

        private string _wxsid;
        private string _skey;
        private string _passTicket;
        private long _wxuin;

        protected string UUID;
        protected User MyAccount;
        protected Synckey SyncKey;

        protected string SyncKeyAsString
        {
            get
            {
                return string.Join("|", SyncKey.List.Select(x => $@"{x.Key}_{x.Val}"));
            }
        }

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

        private string device_id;
        private string sync_host;
        //private dynamic r;
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
                              GetContact,
                              TestSyncCheck
                          };
            foreach (var action in actions)
            {
                await ThrowIfFailed(Log(action));
            }

            Logger.Info(@"Start to process messages .");

            await Task.WhenAll(CreatePollingTimerAsync(() => true,
                                                       TimeSpan.FromSeconds(30),
                                                       async () => await ProcessMessage()),
                               CreatePollingTimerAsync(() => true,
                                                       TimeSpan.FromSeconds(30),
                                                       async () => await Schedule()));
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
                                .WithHeader("Cache-Control", "no-cache, must-revalidate")
                                .GetStringAsync();

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

                var extractCode = Regex.Match(response, @"window.code=(\d+);");
                if (!extractCode.Success)
                {
                    Logger.Error($"response : {response}");
                    return false;
                }

                var code = extractCode.Groups[1].Value;
                if (code == HttpStatusCode.Created.ToString("d"))
                {
                    Logger.Info("Please confirm to login .");
                    tip = 0;
                }
                else if (code == HttpStatusCode.OK.ToString("d")) //# 确认登录成功
                {
                    var extractUri = Regex.Match(response, @"window.redirect_uri=""(\S+?)"";", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                    if (!extractUri.Success)
                    {
                        Logger.Error($"response : {response}");
                        return false;
                    }

                    var redirectUri = extractUri.Groups[1].Value;
                    if (string.IsNullOrEmpty(redirectUri))
                    {
                        Logger.Error("redirect_uri not found");
                        return false;
                    }

                    var redirectUrl = redirectUri + "&fun=new";

                    _redirectUri = redirectUrl;
                    _baseUri = redirectUri.Substring(0, redirectUri.LastIndexOf("/", StringComparison.Ordinal));

                    var tempHost = _baseUri.Substring(8);
                    _baseHost = tempHost.Substring(0, tempHost.IndexOf("/", StringComparison.Ordinal));
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

            var initResponse = await url.WithFlurlClient()
                                        .PostJsonAsync(new { BaseRequest })
                                        .ReceiveJson<InitResponse>();

            MyAccount = initResponse.User;
            SyncKey = initResponse.SyncKey;

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
                                    .PostJsonAsync(new { })
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

            Logger.Info($@"Get {ContactList.Count} contacts");

            return true;
        }

        async Task<bool> BatchGetGroupMembers()
        {
            var url = _baseUri + $@"/webwxbatchgetcontact?type=ex&r={NowUnix()}&pass_ticket={_passTicket}";

            var @params = new
            {
                BaseRequest,
                GroupList.Count,
                List = GroupList.Select(group => new { group.UserName, EncryChatRoomId = "" }).ToList()
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

        /// <summary>Starts an asynchronous polling timer, similar to a thread timer.</summary>
        /// <param name="condition">The condition which indicates that polling should continue.</param>
        /// <param name="pollInterval">Similar to a timer tick, this determines the polling frequency.</param>
        /// <param name="pollingAction">The action to be executed after each poll interval, as long as the condition
        /// evaluates to true. This works like a timer event handler.</param>
        /// <param name="afterAction">An optional action to be executed after the condition evaluates to false.</param>
        protected async Task CreatePollingTimerAsync(Func<bool> condition, TimeSpan pollInterval, Action pollingAction, Action afterAction = null)
        {
            Debug.Assert(condition != null, "condition != null");
            while (condition())
            {
                await Task.Delay(pollInterval);

                if (condition())
                {
                    pollingAction();
                }
            }

            afterAction?.Invoke();
        }

        private async Task ProcessMessage()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await this.SyncCheck();

                var retcode = result?.Item1;
                var selector = result?.Item2;

                if (Settings.Debug)
                {
                    Logger.Info($@"[DEBUG] sync_check: {retcode}, {selector}");
                }

                if (retcode == 1100 ||
                    retcode == 1101 ||
                    (retcode == 0 && selector == 0))
                {
                    return;
                }

                if (retcode == 0)
                {
                    var r = await Sync();
                    if (r != null)
                    {
                        await this.HandleMessage(r);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($@"Except in proc_msg: {ex}");
            }
            finally
            {
                var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                if (elapsedMilliseconds < 800)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(1000 - elapsedMilliseconds));
                }
            }
        }

        private async Task<SyncResponse> Sync()
        {
            var url = $@"{_baseUri}/webwxsync?sid={_wxsid}key={_skey}&lang=en_US&pass_ticket={_passTicket}";
            var @params = new
            {
                BaseRequest = BaseRequest,
                SyncKey = SyncKey,
                rr = ~NowUnix()
            };
            try
            {
                //bug can not change http timeout 
                var r = await url.WithFlurlClient()
                                 .PostJsonAsync(@params)
                                 .ReceiveJson<SyncResponse>();

                if (r.BaseResponse.Ret == 0)
                {
                    SyncKey = r.SyncKey;
                    return r;
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Error($@"Sync exception: {ex}");
                return null;
            }
        }


        /// <summary>
        /// 
        ///  处理原始微信消息的内部函数
        ///  msg_type_id:
        ///      0->Init
        ///      1->Self
        ///      2->FileHelper
        ///      3->Group
        ///      4->Contact
        ///      5->Public
        ///      6->Special
        ///      99->Unknown
        ///  :param r: 原始微信消息
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task HandleMessage(SyncResponse response)
        {
            foreach (dynamic msg in response.AddMsgList)
            {
                var msg_type_id = 99;
                var user = new Message.User() {id = msg.FromUserName, name = "unknown"};

                if (msg.MsgType == 51) //init message
                {
                    msg_type_id = 0;
                    user.name = "system";
                }
                else if (msg.MsgType == 37) //friend request
                {
                    msg_type_id = 37;
                    //
                    // content = msg["Content"]
                    // username = content[content.index("fromusername="): content.index("encryptusername")]
                    // username = username[username.index(""") + 1: username.rindex(""")]
                    // print u"[Friend Request]"
                    // print u"       Nickname：" + msg["RecommendInfo"]["NickName"]
                    // print u"       附加消息："+msg["RecommendInfo"]["Content"]
                    // # print u"Ticket："+msg["RecommendInfo"]["Ticket"] # Ticket添加好友时要用
                    // print u"       微信号："+username #未设置微信号的 腾讯会自动生成一段微信ID 但是无法通过搜索 搜索到此人
                    //
                    continue;
                }
                else if (msg.FromUserName == MyAccount.UserName) //# Self
                {
                    msg_type_id = 1;
                    user.name = "this";
                }
                else if (msg["ToUserName"] == "filehelper") //# File Helper
                {
                    msg_type_id = 2;
                    user.name = "file_helper";
                }
                else
                {
                    var contactName = this.GetContactName(user.id);

                    var contactPreferName = contactName.HasValue? contactName.Value : "unknown";

                    if (msg.FromUserName.StartWith("@@")) //# Group
                    {
                        msg_type_id = 3;
                        user.name = contactPreferName;
                    }
                    else if (this.IsContact(msg.FromUserName)) //# Contact
                    {
                        msg_type_id = 4;
                        user.name = contactPreferName;
                    }
                    else if (this.IsPublic(msg.FromUserName)) //# Public
                    {
                        msg_type_id = 5;
                        user.name = contactPreferName;
                    }
                    else if (this.IsSpecial(msg.FromUserName)) //# Special
                    {
                        msg_type_id = 6;
                        user.name = contactPreferName;
                    }
                }

                if (string.IsNullOrEmpty(user.name))
                {
                    user.name = "unknown";
                }
                user.name = HttpUtility.HtmlDecode(user.name);

                if (Settings.Debug &&
                    msg_type_id != 0)
                {
                    Logger.Info($@"[MSG] user.name:");
                }

                var content = this.extract_msg_content(msg_type_id, msg);
                var message = new Message()
                              {
                                  msg_type_id = msg_type_id,
                                  msg_id = msg["MsgId"],
                                  content = content,
                                  to_user_id = msg["ToUserName"],
                                  user = user
                              };
                handle_msg_all(message);
            }
        }

        private ContactName GetContactName(string uid)
        {
            var normalMember = AccountInfo.NormalMembers.FirstOrDefault(x=>x.Info.UserName == uid);
            if (normalMember == null)
            {
                return null;
            }

            var info = normalMember.Info;
            var contactName = new ContactName()
                              {
                                  RemarkName = info?.RemarkName,
                                  Nickname = info?.NickName,
                                  DisplayName = info?.DisplayName
                              };
            return contactName;
        }

        /// <summary>
        /// content_type_id:
        ///     0 -> Text
        ///     1 -> Location
        ///     3 -> Image
        ///     4 -> Voice
        ///     5 -> Recommend
        ///     6 -> Animation
        ///     7 -> Share
        ///     8 -> Video
        ///     9 -> VideoCall
        ///     10 -> Redraw
        ///     11 -> Empty
        ///     99 -> Unknown
        /// :param msg_type_id: 消息类型id
        /// :param msg: 消息结构体
        /// :return: 解析的消息
        /// 
        /// </summary>
        /// <param name="msgTypeId"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        private MessageContent extract_msg_content(int msgTypeId, object msg)
        {
            throw new NotImplementedException();
        }

        private void handle_msg_all(Message message)
        {
            throw new NotImplementedException();
        }

        private async Task Schedule()
        {
            throw new NotImplementedException($"{nameof(Schedule)} NotImplemented");
        }

        private async Task<bool> TestSyncCheck()
        {
            foreach (var hostPrefix in new[] { "webpush", "webpush2" })
            {
                sync_host = $@"{hostPrefix}.{_baseHost}";
                var response = await SyncCheck();
                if (response?.Item1 == 0)
                {
                    return true;
                }
                Logger.Error($@"SyncCheck: {response} ");
            }
            return false;
        }

        private async Task<Tuple<int, int>> SyncCheck()
        {
            var @params = new
            {
                r = NowUnix(),
                sid = _wxsid,
                uin = _wxuin,
                skey = _skey,
                deviceid = device_id,
                synckey = SyncKeyAsString,
                _ = NowUnix(),
            };
            
            var url = $@"https://{sync_host}/cgi-bin/mmwebwx-bin/synccheck?".SetQueryParams(@params);
            try
            {
                var data = await url.WithFlurlClient()
                                    .GetStringAsync();

                var pm = Regex.Match(data, @"window\.synccheck={retcode:""(\d+)"",selector:""(\d+)""}");
                if (!pm.Success)
                {
                    return null;
                }

                var retcode = int.Parse(pm.Groups[1].Value);
                var selector = int.Parse(pm.Groups[2].Value);
                return new Tuple<int, int>(retcode, selector);
            }
            catch (Exception ex)
            {
                Logger.Error($@"exception: {ex.Message}");
                return null;
            }
        }

        #region Helpers
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
        private bool IsSpecial(string fromUserName)
        {
            return SpecialList.Any(x => x.UserName == fromUserName);
        }

        private bool IsPublic(string fromUserName)
        {
            return PublicList.Any(x => x.UserName == fromUserName);
        }

        private bool IsContact(string fromUserName)
        {
            return ContactList.Any(x => x.UserName == fromUserName);
        }
        #endregion Helpers

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
            Logger.Info($@"{action.Method.Name} {resultDescripitn} .");

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

        #endregion Utils
    }

    internal class MessageContent
    {
    }
}