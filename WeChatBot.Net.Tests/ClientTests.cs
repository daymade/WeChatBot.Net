using NUnit.Framework;
using WeChatBot.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using Newtonsoft.Json;
using WeChatBot.Net.Enums;

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
            var client = new Client
            {
                Debug = true,
                QRCodeOutputType = QRCodeOutputType.TTY
            };
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

                var url = @"https://login.weixin.qq.com/cgi-bin/mmwebwx-bin/login?tip=1&uuid=YenIkE0Lsw==&_=1474877000";

                //url = @"https://www.baidu.com";

                ServicePointManager.Expect100Continue = false;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls;

                Console.WriteLine(ServicePointManager.Expect100Continue);
                Console.WriteLine(ServicePointManager.SecurityProtocol);
                Console.WriteLine(ServicePointManager.ServerCertificateValidationCallback);
                
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
                    client.DefaultRequestHeaders.Add("Connection", "keep-alive");
                    //client.DefaultRequestHeaders.Add("Connection", "close");

                    for (int i = 0; i < 2; i++)
                    {
                        var q = await client.GetAsync(url, cts.Token);
                        Assert.That(q.IsSuccessStatusCode);
                    }
                }

                //Console.WriteLine("fuck httpclient");

                //using (var client = new HttpClient())
                //{
                //    client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
                //    client.DefaultRequestHeaders.Add("Connection", "close");
                //    var q = await client.GetAsync(url, cts.Token);
                //    Assert.That(q.IsSuccessStatusCode);
                //}
            }
        }

        [Category("TestHttpClient")]
        [Test]
        public async Task LoginTest_HttpsWithWebClientTwice_ShouldReturn400()
        {
            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(TimeSpan.FromSeconds(5));

                var url = @"https://login.weixin.qq.com/cgi-bin/mmwebwx-bin/login?tip=1&uuid=YenIkE0Lsw==&_=1474877000";

                // Initialize an HttpWebRequest for the current URL.
                var webReq = (HttpWebRequest)WebRequest.Create(url);

                // Send the request to the Internet resource and wait for
                // the response.                
                var response = await webReq.GetResponseAsync();

                // Initialize an HttpWebRequest for the current URL.
                var webReq1 = (HttpWebRequest)WebRequest.Create(url);

                // Send the request to the Internet resource and wait for
                // the response.                
                var response2 = await webReq1.GetResponseAsync();
            }
        }
    }
}