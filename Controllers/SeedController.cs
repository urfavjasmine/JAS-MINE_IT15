using JAS_MINE_IT15.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JAS_MINE_IT15.Controllers
{
    [Route("seed")]
    [Authorize(Roles = "super_admin")]
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
            return Content("Seeding done (roles only). No default user credentials were created.");
        }
    }
}
