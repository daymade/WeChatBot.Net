using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Http.Configuration;
using NUnit.Framework;
using WeChatBot.Net.Enums;
using WeChatBot.Net.Extensions;

namespace WeChatBot.Net.Tests
{
    [TestFixture()]
    public class ClientTests
    {
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

        [Test()]
        public async Task GetUuidTest_Normal_ShouldReturnTrue()
        {
            var q = await new Client().GetUuid();
            Assert.IsTrue(q.HasValue);
        }

        [Test]
        public async Task LoginTest_HttpsWithFlurlClient_ShouldReturn400()
        {
            var url = @"https://login.weixin.qq.com/cgi-bin/mmwebwx-bin/login?tip=1&uuid=YenIkE0Lsw==&_=1474877000";
            var q = await url.WithClient(new FlurlClient()).GetAsync();
            Assert.That(q.IsSuccessStatusCode);
        }

        [Category("TestHttpClient")]
        [Test]
        public async Task LoginTest_HttpsWithHttpClient_ShouldReturn400()
        {
            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(TimeSpan.FromSeconds(3));

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
        public void LoginTest_NewFlurlClientTwice_ShouldReturn400()
        {
            Assert.ThrowsAsync<TaskCanceledException>(async () =>
                                                {
                                                    using (var cts = new CancellationTokenSource())
                                                    {
                                                        cts.CancelAfter(TimeSpan.FromSeconds(3));

                                                        var url = @"https://login.weixin.qq.com/cgi-bin/mmwebwx-bin/login?tip=1&uuid=YenIkE0Lsw==&_=1474877000";

                                                        using (var client = new HttpClient())
                                                        {
                                                            var q = await client.GetAsync(url, cts.Token);
                                                            Assert.That(q.IsSuccessStatusCode);
                                                        }

                                                        var handler = new HttpClientHandler()
                                                                      {
                                                                      };
                                                        using (var client = new HttpClient(handler))
                                                        {
                                                            var q = await client.GetAsync(url, cts.Token);
                                                            Assert.That(q.IsSuccessStatusCode);
                                                        }
                                                    }
                                                });
        }

        [Category("TestHttpClient")]
        [Test]
        public async Task LoginTest_NewFlurlClientTwice2_ShouldReturn400()
        {
            var url = @"https://login.weixin.qq.com/cgi-bin/mmwebwx-bin/login?tip=1&uuid=YenIkE0Lsw==&_=1474877000";
            //ServicePointManager.SetTcpKeepAlive(false, 1,1);
            //System.Net.ServicePointManager.Expect100Continue = false;
            //ServicePoint sp = ServicePointManager.FindServicePoint(new Uri(url));
            //sp.MaxIdleTime = 1;
            //await Task.Delay(TimeSpan.FromMilliseconds(2));

            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var client = new HttpClient();

            {
                var q = await client.GetAsync(url);
                Assert.That(q.IsSuccessStatusCode);
            }
            Console.WriteLine($"fuck httpclient");

            {
                client = new HttpClient();

                var q = await client.GetAsync(url);
                Assert.That(q.IsSuccessStatusCode);
            }
        }
    }
}
