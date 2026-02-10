using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace DOA_API_Exchange_Service_For_Gateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConnectionTestController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ConnectionTestController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("test-mysql")]
        public async Task<IActionResult> TestMySqlConnection()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // ทดสอบ Query ง่ายๆ
                using var command = new MySqlCommand("SELECT DATABASE(), VERSION();", connection);
                using var reader = await command.ExecuteReaderAsync();
                
                string? databaseName = null;
                string? version = null;
                
                if (await reader.ReadAsync())
                {
                    databaseName = reader.GetString(0);
                    version = reader.GetString(1);
                }

                return Ok(new
                {
                    status = "Success",
                    message = "เชื่อมต่อ MySQL สำเร็จ!",
                    database = databaseName,
                    mysqlVersion = version,
                    connectionString = connectionString?.Replace(_configuration.GetConnectionString("DefaultConnection")?.Split("Pwd=")[1]?.Split(";")[0] ?? "", "***") // ซ่อนรหัสผ่าน
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "Error",
                    message = "ไม่สามารถเชื่อมต่อ MySQL ได้",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpGet("list-tables")]
        public async Task<IActionResult> ListTables()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new MySqlCommand("SHOW TABLES;", connection);
                using var reader = await command.ExecuteReaderAsync();
                
                var tables = new List<string>();
                while (await reader.ReadAsync())
                {
                    tables.Add(reader.GetString(0));
                }

                return Ok(new
                {
                    status = "Success",
                    tableCount = tables.Count,
                    tables = tables
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "Error",
                    message = "ไม่สามารถดึงรายชื่อตารางได้",
                    error = ex.Message
                });
            }
        }
    }
}
