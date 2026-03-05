using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DOA_API_Exchange_Service_For_Gateway.Data;
using DOA_API_Exchange_Service_For_Gateway.Models.Entities;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;
using System;
using System.Threading.Tasks;
using DOA_API_Exchange_Service_For_Gateway.Helpers;

namespace DOA_API_Exchange_Service_For_Gateway.Controllers
{
    [ApiController]
    [Route("api-doa-gw/application")]
    // [Route("api-doa-gw/v1.0/application")] // You can change to this later using your prefix settings
    public class ApplicationController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ApplicationController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] ApplicationRegisterRequest request)
        {
            var title = "API Exchange Service For Gateway";

            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseWriter.CreateError(title, "Invalid request body.", 400));
            }

            // 1. Check for Duplicate AppName or AppNickName
            var existingApp = await _context.ApplicationExternals
                .FirstOrDefaultAsync(a => a.AppName == request.AppName || a.AppNickName == request.AppNickName);

            if (existingApp != null)
            {
                var duplicateWarning = existingApp.AppName == request.AppName ? "AppName" : "AppNickName";
                return Conflict(ResponseWriter.CreateError(title, $"{duplicateWarning} is already registered.", 409));
            }

            // 2. Create the new Application External Record
            var newApplication = new ApplicationExternal
            {
                AppRoleId = 0, // Default value
                CliendId = Guid.NewGuid().ToString(), // Generate UUID v4
                CallbackUrl = request.CallbackUrl,
                HostUrl = request.HostUrl,
                AppName = request.AppName,
                AppNickName = request.AppNickName,
                CreatedAt = DateTime.Now
            };

            // 3. Save to Database
            await _context.ApplicationExternals.AddAsync(newApplication);
            await _context.SaveChangesAsync();

            // 4. Return Output
            var response = new ApplicationRegisterResponse
            {
                AppName = newApplication.AppName,
                AppNickName = newApplication.AppNickName,
                ClientId = newApplication.CliendId // Map กลับเป็น ClientId ใน JSON response
            };

            return Ok(ResponseWriter.CreateSuccess(title, response, "Application registered successfully."));
        }
    }
}
