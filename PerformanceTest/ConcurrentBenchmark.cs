using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace PerformanceTest
{
    public sealed class ConcurrentBenchmark
    {
        private readonly Dictionary<string, Func<Task>> tests = new Dictionary<string, Func<Task>>(StringComparer.OrdinalIgnoreCase);
        private readonly List<Action> beforeEachLaunch = new List<Action>();

        public void Enqueue(string name, Func<Task> test)
        {
            tests.Add(name, test);
        }

        public void BeforeEachLaunch(Action action)
        {
            beforeEachLaunch.Add(action);
        }

        public void Run(int launchCount, int targetCount)
        {
            var resultsSet = new List<List<TestResult>>();
            for (var i = 0; i < launchCount; i++)
            {
                beforeEachLaunch.ForEach(action => action());
                resultsSet.Add(Run(targetCount));
            }
            if (resultsSet.Count > 1)
            {
                Console.WriteLine();
                Console.WriteLine("--- LAUNCH SUMMARY ---");
                var results = resultsSet.SelectMany(x => x).GroupBy(x => x.Name).Select(x => new TestResult
                {
                    Name = x.Key,
                    Min = x.OrderBy(y => y.Min).ElementAt(x.Count() / 2).Min,
                    Avg = x.OrderBy(y => y.Avg).ElementAt(x.Count() / 2).Avg,
                    Max = x.OrderBy(y => y.Max).ElementAt(x.Count() / 2).Max,
                    Total = x.OrderBy(y => y.Total).ElementAt(x.Count() / 2).Total,
                    Tcp = x.OrderBy(y => y.Tcp).ElementAt(x.Count() / 2).Tcp
                }).ToList();
                foreach (var rank in Rank(results).OrderBy(x => x.Order))
                {
                    Console.WriteLine($"{rank.Order}: {rank.Name} => {rank.Min}+{rank.Avg}+{rank.Max}+{rank.Total}+{rank.Tcp} ; {rank.Result}");
                }
            }
        }

        public List<TestResult> Run(int targetCount)
        {
            var results = new List<TestResult>();

            Console.WriteLine();
            Console.WriteLine("--- TARGET START ---");

            foreach (var test in tests)
            {
                Console.WriteLine($"{test.Key}...");
                var result = Run(targetCount, test.Key, test.Value);
                Console.WriteLine($"{result} Threads: [{string.Join(",", result.Threads)}]");
                results.Add(result);
                Thread.Sleep(500);
            }

            Console.WriteLine();
            Console.WriteLine("--- TARGET SUMMARY ---");
            foreach (var rank in Rank(results).OrderBy(x => x.Order))
            {
                Console.WriteLine($"{rank.Order}: {rank.Name} => {rank.Min}+{rank.Avg}+{rank.Max}+{rank.Total}+{rank.Tcp} ; {rank.Result}");
            }

            return results;
        }

        public static TestResult Run(int targetCount, string name, Func<Task> execute)
        {
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            var oldTcpConnection = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections().Count(tcp => tcp.RemoteEndPoint.Port == 80);

            var totaltime = Stopwatch.StartNew();
            var tasks = ParallelEnumerable.Range(0, targetCount)
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                .Select(
                    x =>
                    {
                        var stopwatch = Stopwatch.StartNew();
                        execute().GetAwaiter().GetResult();
                        stopwatch.Stop();
                        return Tuple.Create(stopwatch.Elapsed, Thread.CurrentThread.ManagedThreadId, Thread.CurrentThread.Name);
                    })
                .ToList();

            totaltime.Stop();

            var newTcpConnection = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections().Count(tcp => tcp.RemoteEndPoint.Port == 80);

            var result = new TestResult
            {
                Name = name,
                Total = totaltime.Elapsed.TotalMilliseconds,
                Min = tasks.Min(x => x.Item1.TotalMilliseconds),
                Avg = tasks.Average(x => x.Item1.TotalMilliseconds),
                Max = tasks.Max(x => x.Item1.TotalMilliseconds),
                Tcp = Math.Max(newTcpConnection - oldTcpConnection, 0),
                Threads = tasks.Select(x => x.Item2).Distinct().OrderBy(x => x).ToArray()
            };

            return result;
        }

        public static IEnumerable<TestRank> Rank(List<TestResult> results)
        {
            var rankMin = results.OrderBy(x => x.Min).Select((x, i) => Tuple.Create(x, i)).ToDictionary(x => x.Item1, x => x.Item2);
            var rankAvg = results.OrderBy(x => x.Avg).Select((x, i) => Tuple.Create(x, i)).ToDictionary(x => x.Item1, x => x.Item2);
            var rankMax = results.OrderBy(x => x.Max).Select((x, i) => Tuple.Create(x, i)).ToDictionary(x => x.Item1, x => x.Item2);
            var rankTotal = results.OrderBy(x => x.Total).Select((x, i) => Tuple.Create(x, i)).ToDictionary(x => x.Item1, x => x.Item2);
            var rankTcp = results.OrderBy(x => x.Tcp).Select((x, i) => Tuple.Create(x, i)).ToDictionary(x => x.Item1, x => x.Item2);
            var ranks = results.Select(x => new TestRank(x) { Min = rankMin[x], Avg = rankAvg[x], Max = rankMax[x], Total = rankTotal[x], Tcp = rankTcp[x] }).ToList();
            return ranks;
        }

        public sealed class TestResult
        {
            public string Name { get; set; }
            public double Min { get; set; }
            public double Avg { get; set; }
            public double Max { get; set; }
            public double Total { get; set; }
            public int Tcp { get; set; }
            public int[] Threads { get; set; }

            public override string ToString() => $"Min: {Min:F1}, Avg: {Avg:F1}, Max: {Max:F1}, Total: {Total:F1}, Tcp: {Tcp}";
        }

        public sealed class TestRank
        {
            public TestRank(TestResult result)
            {
                Name = result.Name;
                Result = result;
            }

            public string Name { get; }
            public TestResult Result { get; }
            public int Order => Min + Avg + Max + Total + Tcp;
            public int Min { get; set; }
            public int Avg { get; set; }
            public int Max { get; set; }
            public int Total { get; set; }
            public int Tcp { get; set; }
        }
    }
}