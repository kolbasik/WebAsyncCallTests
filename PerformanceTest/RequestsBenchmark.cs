using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace PerformanceTest
{
    public class RequestsBenchmark
    {
        private readonly string Url = "http://web20161208102028.azurewebsites.net/Delay/Result?max=5000";
        //private readonly string Url = "http://ya.ru";
        //private readonly string Url = "http://www.msn.com/";
        //private readonly string Url = "https://www.amazon.com/";
        //private readonly string Url = "https://azure.microsoft.com/en-gb/";

        private Lazy<HttpClient> sharedWebRequestClient;
        private Lazy<HttpClient> sharedHttpRequestClient;
        private ObjectPool<HttpClient> poolWebRequestClient;
        private ObjectPool<HttpClient> poolHttpRequestClient;
        private ObjectPool<WebClient> poolWebClient;

        [Setup]
        public void BeforeEach()
        {
            sharedWebRequestClient = new Lazy<HttpClient>(() => new HttpClient(new WebRequestHandler()));
            sharedHttpRequestClient = new Lazy<HttpClient>(() => new HttpClient(new HttpClientHandler()));
            poolWebRequestClient = new ObjectPool<HttpClient>(() => new HttpClient(new WebRequestHandler()));
            poolHttpRequestClient = new ObjectPool<HttpClient>(() => new HttpClient(new HttpClientHandler()));
            poolWebClient = new ObjectPool<WebClient>(() => new WebClient());
        }

        [Benchmark(Baseline = true), MTAThread]
        public string NewWebClient()
        {
            return new WebClient().DownloadString(Url);
        }

        [Benchmark, MTAThread]
        public string PoolWebClient()
        {
            var webClient = poolWebClient.Resolve();
            var result = webClient.DownloadString(Url);
            poolWebClient.Release(webClient);
            return result;
        }

        [Benchmark, MTAThread]
        public async Task<string> SharedWebRequestHttpClient()
        {
            var client = sharedWebRequestClient.Value;
            using (var response = await client.GetAsync(Url).ConfigureAwait(false))
            using (var content = response.Content)
            {
                response.EnsureSuccessStatusCode();
                return await content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }

        [Benchmark, MTAThread]
        public async Task<string> SharedHttpRequestHttpClient()
        {
            var client = sharedHttpRequestClient.Value;
            using (var response = await client.GetAsync(Url).ConfigureAwait(false))
            using (var content = response.Content)
            {
                response.EnsureSuccessStatusCode();
                return await content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }

        [Benchmark, MTAThread]
        public async Task<string> PoolWebRequestHttpClient()
        {
            string result;
            var client = poolWebRequestClient.Resolve();
            using (var response = await client.GetAsync(Url).ConfigureAwait(false))
            using (var content = response.Content)
            {
                response.EnsureSuccessStatusCode();
                result = await content.ReadAsStringAsync().ConfigureAwait(false);
            }
            poolWebRequestClient.Release(client);
            return result;
        }

        [Benchmark, MTAThread]
        public async Task<string> PoolHttpRequestHttpClient()
        {
            string result;
            var client = poolHttpRequestClient.Resolve();
            using (var response = await client.GetAsync(Url).ConfigureAwait(false))
            using (var content = response.Content)
            {
                response.EnsureSuccessStatusCode();
                result = await content.ReadAsStringAsync().ConfigureAwait(false);
            }
            poolHttpRequestClient.Release(client);
            return result;
        }

        [Benchmark, MTAThread]
        public async Task<string> NewWebRequestHttpClient()
        {
            using (var client = new HttpClient(new WebRequestHandler()))
            using (var response = await client.GetAsync(Url).ConfigureAwait(false))
            using (var content = response.Content)
            {
                response.EnsureSuccessStatusCode();
                return await content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }

        [Benchmark, MTAThread]
        public async Task<string> NewHttpRequestHttpClient()
        {
            using (var client = new HttpClient(new HttpClientHandler()))
            using (var response = await client.GetAsync(Url).ConfigureAwait(false))
            using (var content = response.Content)
            {
                response.EnsureSuccessStatusCode();
                return await content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }
    }
}