using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using WeChatBot.Net.Helper;

namespace WeChatBot.Net.Extensions
{
    public static class FlurlClientExtensions
    {
        /// <summary>
        /// Fluently specify that an existing FlurlClient should be used to call the Url, rather than creating a new one.
        /// Enables re-using the underlying HttpClient.
        /// </summary>
        /// <param name="url">The URL.</param>
        public static FlurlClient WithFlurlClient(this string url)
        {
            return ((Url)url).WithFlurlClient();
        }

        /// <summary>
        /// Fluently specify that an existing FlurlClient should be used to call the Url, rather than creating a new one.
        /// Enables re-using the underlying HttpClient.
        /// </summary>
        /// <param name="url">The URL.</param>
        public static FlurlClient WithFlurlClient(this Url url)
        {
            return url.WithClient(HttpClientContainer.GetClient());
        }

        public static async Task<HttpResponseMessage> GetAsyncSafe(this FlurlClient client)
        {
            return await Action(client, async (flurlClient, cts) => await flurlClient.GetAsync(cts));
        }
        
        private static async Task<HttpResponseMessage> Action(FlurlClient client,
                                                              Func<FlurlClient, CancellationToken, Task<HttpResponseMessage>> action,
                                                              int times = 3)
        {
            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                {
                    return await action(client, cts.Token);
                }
            }
            catch (Exception ex)
            {
                var newClient = client.Clone().ConfigureHttpClient(x => x = new HttpClient());
                if (times-- <= 0)
                {
                    Console.Error.WriteLine(ex);
                    throw;
                }
                return await Action(newClient, action, times);
            }
        }
    }
}
