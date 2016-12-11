using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace PerformanceTest
{
    static class Program
    {
        static void Main(string[] args)
        {
            //RunBenchmarkDotNet(3, Environment.ProcessorCount * 2);
            RunBenchmarkConcurrent(3, Environment.ProcessorCount * 4);

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static void RunBenchmarkDotNet(int launchCount, int targetCount)
        {
            var config = ManualConfig.Create(DefaultConfig.Instance)
                .With(RankColumn.Arabic)
                .With(CharacteristicColumn.AllColumns).With(StatisticColumn.AllStatistics)
                //.With(StatisticColumn.P0, StatisticColumn.P25, StatisticColumn.P50, StatisticColumn.P67, StatisticColumn.P80, StatisticColumn.P85, StatisticColumn.P90, StatisticColumn.P95, StatisticColumn.P100)
                .With(Job.Clr.With(Platform.X64).WithGcServer(true).WithLaunchCount(launchCount).WithWarmupCount(Math.Max(1, targetCount / 5)).WithTargetCount(targetCount).WithUnrollFactor(1).WithInvocationCount(3));

            BenchmarkRunner.Run<RequestsBenchmark>(config);
        }

        static void RunBenchmarkConcurrent(int launchCount, int targetCount)
        {
            var methods = new RequestsBenchmark();
            var benchmark = new ConcurrentBenchmark();
            benchmark.BeforeEachLaunch(() => methods.BeforeEach());
            benchmark.Enqueue($"{nameof(methods.NewWebClient)}", () => { methods.NewWebClient(); return Task.CompletedTask; });
            benchmark.Enqueue($"{nameof(methods.PoolWebClient)}", () => { methods.PoolWebClient(); return Task.CompletedTask; });
            benchmark.Enqueue($"{nameof(methods.SharedWebRequestHttpClient)}", () => methods.SharedWebRequestHttpClient());
            benchmark.Enqueue($"{nameof(methods.SharedHttpRequestHttpClient)}", () => methods.SharedHttpRequestHttpClient());
            benchmark.Enqueue($"{nameof(methods.PoolWebRequestHttpClient)}", () => methods.PoolWebRequestHttpClient());
            benchmark.Enqueue($"{nameof(methods.PoolHttpRequestHttpClient)}", () => methods.PoolHttpRequestHttpClient());
            benchmark.Enqueue($"{nameof(methods.NewWebRequestHttpClient)}", () => methods.NewWebRequestHttpClient());
            benchmark.Enqueue($"{nameof(methods.NewHttpRequestHttpClient)}", () => methods.NewHttpRequestHttpClient());
            benchmark.Run(launchCount, targetCount);
        }

        //static void Start()
        //{
        //    const int count = 50;

        //    //var sharedClient = new HttpClient(new WebRequestHandler());
        //    //Console.WriteLine("Shared HttpClient");
        //    //RunTest(count,
        //    //    async () =>
        //    //    {
        //    //        using (var response = await sharedClient.GetAsync("http://www.google.com").ConfigureAwait(false))
        //    //        using (var content = response.Content)
        //    //        {
        //    //            response.EnsureSuccessStatusCode();
        //    //            return await content.ReadAsStringAsync().ConfigureAwait(false);
        //    //        }
        //    //    });
        //    //var shared2Client = new HttpClient(new HttpClientHandler());
        //    //Console.WriteLine("Shared HttpClient");
        //    //RunTest(count,
        //    //    async () =>
        //    //    {
        //    //        using (var response = await shared2Client.GetAsync("http://www.google.com").ConfigureAwait(false))
        //    //        using (var content = response.Content)
        //    //        {
        //    //            response.EnsureSuccessStatusCode();
        //    //            return await content.ReadAsStringAsync().ConfigureAwait(false);
        //    //        }
        //    //    });
        //    //Console.WriteLine("ObjectPool");
        //    //var objectPool = new ObjectPool<HttpClient>(() => new HttpClient());
        //    //RunTest(count,
        //    //    async () =>
        //    //    {
        //    //        var client = objectPool.Resolve();
        //    //        using (var response = await client.GetAsync("http://www.google.com").ConfigureAwait(false))
        //    //        using (var content = response.Content)
        //    //        {
        //    //            response.EnsureSuccessStatusCode();
        //    //            objectPool.Release(client);
        //    //            return await content.ReadAsStringAsync().ConfigureAwait(false);
        //    //        }
        //    //    });

        //    //Console.WriteLine("New HttpClient");
        //    //RunTest(count,
        //    //    async () =>
        //    //    {
        //    //        using (var client = new HttpClient(new WebRequestHandler { AllowAutoRedirect = false }))
        //    //        using (var response = await client.GetAsync("http://www.google.com").ConfigureAwait(false))
        //    //        using (var content = response.Content)
        //    //        {
        //    //            response.EnsureSuccessStatusCode();
        //    //            return await content.ReadAsStringAsync().ConfigureAwait(false);
        //    //        }
        //    //    });
        //    //Console.WriteLine("WebClient");
        //    //RunTest(count, () => Task.FromResult<object>(new WebClient().DownloadString("http://www.google.com")));

        //    //var sharedClient = new HttpClient();
        //    //Console.WriteLine("Shared client and Shared service");
        //    //RunTest(count,
        //    //    async () =>
        //    //    {
        //    //        using (var response = await sharedClient.GetAsync("http://web20161208102028.azurewebsites.net/SharedRemote/Awaiter").ConfigureAwait(false))
        //    //        using (var content = response.Content)
        //    //        {
        //    //            response.EnsureSuccessStatusCode();
        //    //            return await content.ReadAsStringAsync().ConfigureAwait(false);
        //    //        }
        //    //    });
        //    //Console.WriteLine("Shared client and New service");
        //    //RunTest(count,
        //    //    async () =>
        //    //    {
        //    //        using (var response = await sharedClient.GetAsync("http://web20161208102028.azurewebsites.net/Remote/Awaiter").ConfigureAwait(false))
        //    //        using (var content = response.Content)
        //    //        {
        //    //            response.EnsureSuccessStatusCode();
        //    //            return await content.ReadAsStringAsync().ConfigureAwait(false);
        //    //        }
        //    //    });
        //    //Console.WriteLine("New client and Shared service");
        //    //RunTest(count,
        //    //    async () =>
        //    //    {
        //    //        using (var client = new HttpClient())
        //    //        using (var response = await client.GetAsync("http://web20161208102028.azurewebsites.net/SharedRemote/Awaiter").ConfigureAwait(false))
        //    //        using (var content = response.Content)
        //    //        {
        //    //            response.EnsureSuccessStatusCode();
        //    //            return await content.ReadAsStringAsync().ConfigureAwait(false);
        //    //        }
        //    //    });
        //    //Console.WriteLine("New client and New service");
        //    //RunTest(count,
        //    //    async () =>
        //    //    {
        //    //        using (var client = new HttpClient())
        //    //        using (var response = await client.GetAsync("http://web20161208102028.azurewebsites.net/Remote/Awaiter").ConfigureAwait(false))
        //    //        using (var content = response.Content)
        //    //        {
        //    //            response.EnsureSuccessStatusCode();
        //    //            return await content.ReadAsStringAsync().ConfigureAwait(false);
        //    //        }
        //    //    });
        //    //Console.WriteLine("WebClient and Shared service");
        //    //RunTest(count, () => Task.FromResult<object>(new WebClient().DownloadString("http://web20161208102028.azurewebsites.net/SharedRemote/Awaiter")));
        //    //Console.WriteLine("WebClient and New service");
        //    //RunTest(count, () => Task.FromResult<object>(new WebClient().DownloadString("http://web20161208102028.azurewebsites.net/Remote/Awaiter")));
        //}
    }
}
