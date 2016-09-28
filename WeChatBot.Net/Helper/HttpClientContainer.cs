using System;
using System.Net;
using System.Net.Http;
using Flurl;
using Flurl.Http;
using Flurl.Http.Configuration;

namespace WeChatBot.Net.Helper
{
    public class HttpClientContainer
    {
        protected static readonly FlurlClient FlurlClient;
        private static readonly Logger Logger = new Logger();

        static HttpClientContainer()
        {
            FlurlClient = new FlurlClient().EnableCookies()
                                           .ConfigureClient(x =>
                                                            {
                                                                x.AfterCall = call =>
                                                                              {
                                                                                  Logger.Info($@" call.Duration: {call.Duration}ms");
                                                                              };
                                                                x.HttpClientFactory = new CustomHttpClientFactory();
                                                            })
                                           .ConfigureHttpClient(x =>
                                                                {
                                                                    x.DefaultRequestHeaders.Add("Connection", "keep-alive");
                                                                    x.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux i686; U;) Gecko/20070322 Kazehakase/0.4.5");
                                                                    x.DefaultRequestHeaders.ExpectContinue = false;
                                                                });
        }

        public FlurlClient GetClient()
        {
            return FlurlClient.Clone();
        }
    }

    public class CustomHttpClientFactory : DefaultHttpClientFactory
    {
        public override HttpMessageHandler CreateMessageHandler()
        {
            var clientHandler = new HttpClientHandler()
            {
                UseProxy = true,
                Proxy = new WebProxy("http://127.0.0.1:8888"),
                AllowAutoRedirect = true,
                UseCookies = true
            };
            return clientHandler;
        }
    }
}
