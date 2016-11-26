using System.Net;
using System.Net.Http;
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
            FlurlClient = new FlurlClient()
            {
                AutoDispose = false
            }
                          .EnableCookies()
                          .ConfigureClient(x =>
                                           {
                                               x.AfterCall = call =>
                                                             {
                                                                 Logger.Info($@"Call.Duration: {call.Duration?.TotalMilliseconds}ms");
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

        public static FlurlClient GetClient()
        {
            var flurlClient = FlurlClient.Clone();

            return flurlClient;
        }
    }

    public class CustomHttpClientFactory : DefaultHttpClientFactory
    {
        public override HttpMessageHandler CreateMessageHandler()
        {
            var clientHandler = new HttpClientHandler()
            {
                AllowAutoRedirect = true,
                UseCookies = true
            };

            //TODO inject setting
            if (new Settings().UseFiddlerProxy)
            {
                clientHandler.UseProxy = true;
                clientHandler.Proxy = new WebProxy("http://127.0.0.1:8888");
            }

            return clientHandler;
        }
    }
}
