using NUnit.Framework;
using WeChatBot.Net.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Flurl.Http;

namespace WeChatBot.Net.Extensions.Tests
{
    [TestFixture()]
    public class FlurlClientExtensionsTests
    {
        /// <summary>
        /// Exception: This instance has already started one or more requests. Properties can only be modified before sending the first request.
        /// </summary>
        /// <returns></returns>
        [Test()]
        public async Task WithNewHttpClientTest()
        {
            var url = @"https://login.weixin.qq.com/cgi-bin/mmwebwx-bin/login?tip=1&uuid=YenIkE0Lsw==&_=1474877000";
            var flurlClient = new FlurlClient() { AutoDispose = false };

            {
                var q = await url.WithClient(flurlClient).WithTimeout(TimeSpan.FromSeconds(5)).GetAsync();
                Assert.That(q.IsSuccessStatusCode);
                await q.Content.ReadAsStringAsync();
            }
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                                                                   {
                                                                       {
                                                                           var q = await url.WithClient(flurlClient).WithTimeout(TimeSpan.FromSeconds(5)).GetAsync();
                                                                           Assert.That(q.IsSuccessStatusCode);
                                                                       }
                                                                   });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Test()]
        public async Task WithNewHttpClientTest2()
        {
            var url = @"https://login.weixin.qq.com/cgi-bin/mmwebwx-bin/login?tip=1&uuid=YenIkE0Lsw==&_=1474877000";
            var flurlClient = new FlurlClient() { AutoDispose = false };

            {
                var q = await url.WithClient(flurlClient).WithTimeout(TimeSpan.FromSeconds(5)).GetAsync();
                Assert.That(q.IsSuccessStatusCode);
                await q.Content.ReadAsStringAsync();
            }
            Console.WriteLine($"fuck httpclient");
            {
                

                var q = await url.WithClient(flurlClient).WithNewHttpClient().GetAsync();

                //var q2 = await url.WithClient(flurlClient).WithNewHttpClient().WithTimeout(TimeSpan.FromSeconds(5)).GetAsync();
                Assert.That(q.IsSuccessStatusCode);
            }
        }
    }
}