using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using NUnit.Framework;
using WeChatBot.Net.Enums;
using WeChatBot.Net.Extensions;

namespace WeChatBot.Net.Tests
{
    [TestFixture()]
    public class ClientTests
    {
        [Test()]
        public async Task GetUuidTest_Normal_ShouldReturnTrue()
        {
            var q = await new Client().GetUuid();
            Assert.IsTrue(q.Item1);
        }

        [Test()]
        [Ignore("manual run this for debug")]
        public async Task RunTest_Normal_ShouldOuputQRCode()
        {
            var client = new Client(new Settings()
                                    {
                                        Debug = true,
                                        QRCodeOutputType = QRCodeOutputType.TTY
                                    });
            await client.Run();

            Assert.Pass();
        }

        [Test]
        public async Task LoginTest_Https_ShouldReturn400()
        {
            var url = @"https://login.weixin.qq.com/cgi-bin/mmwebwx-bin/login?tip=1&uuid=YenIkE0Lsw==&_=1474877000";
            var q = await url.GetAsync();
            Assert.That(q.IsSuccessStatusCode);
        }

        [Test]
        public async Task LoginTest_HttpsWithFlurlClient_ShouldReturn400()
        {
            var url = @"https://login.weixin.qq.com/cgi-bin/mmwebwx-bin/login?tip=1&uuid=YenIkE0Lsw==&_=1474877000";
            var q = await url.WithClient(new FlurlClient()).GetAsync();
            Assert.That(q.IsSuccessStatusCode);
        }

        /// <summary>
        ///     AutoDispose = false fix this problem
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task LoginTest_HttpsWithFlurlClientTwice_ShouldReturn400()
        {
            var url = @"https://login.weixin.qq.com/cgi-bin/mmwebwx-bin/login?tip=1&uuid=YenIkE0Lsw==&_=1474877000";
            var flurlClient = new FlurlClient() {AutoDispose = false};
            {
                var q = await url.WithClient(flurlClient).GetAsyncSafe();
                Assert.That(q.IsSuccessStatusCode);
                await q.Content.ReadAsStringAsync();
            }
            {
                var q = await url.WithClient(flurlClient).GetAsyncSafe();
                Assert.That(q.IsSuccessStatusCode);
            }
        }

        [Category("TestHttpClient")]
        [Test]
        public async Task LoginTest_HttpsWithHttpClient_ShouldReturn400()
        {
            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(TimeSpan.FromSeconds(5));

                var url = @"https://login.weixin.qq.com/cgi-bin/mmwebwx-bin/login?tip=1&uuid=YenIkE0Lsw==&_=1474877000";

                using (var client = new HttpClient())
                {
                    var q = await client.GetAsync(url, cts.Token);
                    Assert.That(q.IsSuccessStatusCode);
                }
            }
        }

        [Category("TestHttpClient")]
        [Test]
        public async Task LoginTest_HttpsWithHttpClientTwice_ShouldReturn400()
        {
            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(TimeSpan.FromSeconds(5));

                var url = @"https://login.weixin.qq.com/cgi-bin/mmwebwx-bin/login?tip=1&uuid=YenIkE0Lsw==&_=1474877000"; //a new httpclient will fail

                //url = @"https://www.baidu.com"; //uncomment to use this url, r1 r2 r3 both success

                var h = new HttpClientHandler()
                        {
                            AllowAutoRedirect = true,
                            Proxy = new WebProxy("http://127.0.0.1:8888"),
                            UseProxy = true
                        };

                using (var client = new HttpClient(h))
                {
                    client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
                    client.DefaultRequestHeaders.Add("Connection", "keep-alive");
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux i686; U;) Gecko/20070322 Kazehakase/0.4.5");

                    var q = await client.GetAsync(url, cts.Token); //first request r1 //success
                    var q2 = await client.GetAsync(url, cts.Token); //second request r2 //sucess

                    Assert.That(q.IsSuccessStatusCode);
                    Assert.That(q2.IsSuccessStatusCode);
                }

                Console.WriteLine("fuck httpclient");

                var h2 = new HttpClientHandler()
                         {
                             AllowAutoRedirect = true,
                             Proxy = new WebProxy("http://127.0.0.1:8888"),
                             UseProxy = true
                         };
                using (var client = new HttpClient(h2))
                {
                    client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
                    client.DefaultRequestHeaders.Add("Connection", "keep-alive");
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux i686; U;) Gecko/20070322 Kazehakase/0.4.5");

                    var q = await client.GetAsync(url, cts.Token); //third request r3 //failed
                    Assert.That(q.IsSuccessStatusCode);
                }
            }
        }
    }
}
