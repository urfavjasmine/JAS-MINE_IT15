using JAS_MINE_IT15.Data;
using Microsoft.AspNetCore.Mvc;

namespace JAS_MINE_IT15.Controllers
{
    [Route("seed")]
    public class SeedController : Controller
    {
        private readonly IServiceProvider _services;

        public SeedController(IServiceProvider services)
        {
            _services = services;
        }

        [HttpGet("run")]
        public async Task<IActionResult> Run()
        {
            await IdentitySeeder.SeedRoles(_services);
            await IdentitySeeder.SeedSuperAdmin(_services);
            await IdentitySeeder.SeedDefaultUsers(_services);

            return Content("Seeding done. Check AspNetUsers table now.");
        }
    }
}
