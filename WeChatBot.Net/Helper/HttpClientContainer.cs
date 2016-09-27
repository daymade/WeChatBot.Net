using System.Net;
using System.Net.Http;
using Flurl.Http;

namespace WeChatBot.Net.Helper
{
    public class HttpClientContainer
    {
        internal static readonly FlurlClient FlurlClient;

        static HttpClientContainer()
        {
            FlurlClient = new FlurlClient()
                          {
                              AutoDispose = false
                          };

            FlurlClient.EnableCookies();
            FlurlClient.ConfigureHttpClient(x =>
                                            {
                                                var clientHandler = new HttpClientHandler()
                                                                    {
                                                                        UseProxy = true,
                                                                        Proxy = new WebProxy("http://127.0.0.1:8888"),
                                                                        AllowAutoRedirect = true
                                                                    };
                                                x = new HttpClient(clientHandler);
                                                x.DefaultRequestHeaders.Add("Connection", "keep-alive");
                                                x.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux i686; U;) Gecko/20070322 Kazehakase/0.4.5");
                                            });
        }

        public FlurlClient GetClient()
        {
            return FlurlClient.Clone();
        }
    }
}
