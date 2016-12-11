using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Web.Controllers
{
    public sealed class SharedRemoteController : Controller
    {
        private static readonly HttpClient Client;

        static SharedRemoteController()
        {
            Client = new HttpClient { BaseAddress = new Uri("http://www.google.com") };
        }

        public ActionResult Result()
        {
            var result = ExecuteAsync(async () =>
            {
                using (var response = await Client.GetAsync("/").ConfigureAwait(false))
                using (var content = response.Content)
                {
                    response.EnsureSuccessStatusCode();
                    return Tuple.Create(response.StatusCode, response.ReasonPhrase, content.Headers.ContentType.MediaType);
                }
            }).Result;
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Awaiter()
        {
            var result = ExecuteAsync(async () =>
            {
                using (var response = await Client.GetAsync("/").ConfigureAwait(false))
                using (var content = response.Content)
                {
                    response.EnsureSuccessStatusCode();
                    return Tuple.Create(response.StatusCode, response.ReasonPhrase, content.Headers.ContentType.MediaType);
                }
            }).GetAwaiter().GetResult();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Factory()
        {
            var result = Task.Factory.StartNew(state => ExecuteAsync(
                async () =>
                {
                    using (var response = await Client.GetAsync("/").ConfigureAwait(false))
                    using (var content = response.Content)
                    {
                        response.EnsureSuccessStatusCode();
                        return Tuple.Create(response.StatusCode, response.ReasonPhrase, content.Headers.ContentType.MediaType);
                    }
                }), null, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default).Unwrap().GetAwaiter().GetResult();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [NonAction]
        private static async Task<Tuple<T, TimeSpan>> ExecuteAsync<T>(Func<Task<T>> execute)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await execute().ConfigureAwait(false);
            return Tuple.Create(result, stopwatch.Elapsed);
        }
    }
}