using System;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Web.Controllers
{
    public sealed class DelayController : AsyncController
    {
        private static readonly Random Rnd = new Random(Guid.NewGuid().GetHashCode());

        public async Task<ActionResult> ResultAsync(int max = 5000)
        {
            var delay = TimeSpan.FromMilliseconds(Rnd.Next(0, max));
            await Task.Delay(delay);
            return Json(delay, JsonRequestBehavior.AllowGet);
        }
    }
}