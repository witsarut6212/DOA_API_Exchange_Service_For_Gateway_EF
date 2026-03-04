using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    public class LogService : ILogService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        private static readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        public LogService(IWebHostEnvironment env, IConfiguration configuration)
        {
            _env = env;
            _configuration = configuration;
        }

        public async Task LogExceptionAsync(Exception exception, string instance)
        {
            var serviceTitle = _configuration["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";

            var entry = new ExceptionLogEntry
            {
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Service = serviceTitle, // กลับไปใช้ชื่อ Service หลัก (Title) ตามต้องการ
                LogType = "exception",
                ExceptionType = exception.GetType().Name,
                ExceptionMessage = exception.Message,
                InnerException = GetFullInnerExceptionMessage(exception),
                StackTrace = GetShortenedStackTrace(exception.StackTrace), // กรอง Stack Trace ให้สั้นและไล่ง่าย
                Instance = instance
            };

            await WriteLogEntryAsync(entry);
        }

        private string GetShortenedStackTrace(string? stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace)) return string.Empty;

            var lines = stackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            
            // กรองเอาเฉพาะบรรทัดที่มีชื่อโปรเจกต์ของเรา เพื่อให้ไล่ Code ง่ายขึ้น
            var projectLines = lines
                .Where(l => l.Contains("DOA_API_Exchange_Service_For_Gateway"))
                .ToList();

            if (!projectLines.Any())
            {
                // ถ้าไม่เจอชื่อโปรเจกต์เลย (เป็น Error นอกเหนือความคาดหมาย) ให้เอาบรรทัดแรกมาแสดง
                return lines.FirstOrDefault()?.Trim() ?? string.Empty;
            }

            return string.Join(Environment.NewLine, projectLines.Select(l => l.Trim()));
        }

        private string? GetFullInnerExceptionMessage(Exception ex)
        {
            if (ex.InnerException == null) return null;
            
            var messages = new List<string>();
            var inner = ex.InnerException;
            while (inner != null)
            {
                messages.Add(inner.Message);
                inner = inner.InnerException;
            }
            return string.Join(" --> ", messages);
        }

        private async Task WriteLogEntryAsync(ExceptionLogEntry entry)
        {
            try
            {
                string logFilePath = GetLogFilePath();
                string logDirectory = Path.GetDirectoryName(logFilePath)!;

                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                string jsonLine = JsonSerializer.Serialize(entry, _jsonOptions);

                await _fileLock.WaitAsync();
                try
                {
                    await File.AppendAllTextAsync(logFilePath, jsonLine + Environment.NewLine + Environment.NewLine, Encoding.UTF8);
                }
                finally
                {
                    _fileLock.Release();
                }
            }
            catch
            {
            }
        }

        private string GetLogFilePath()
        {
            string storagePath = _configuration["Configuration:StoragePath"] ?? "Storage";
            DateTime now = DateTime.Now;

            string logDirectory = Path.Combine(
                _env.ContentRootPath,
                storagePath,
                "Log",
                now.ToString("yyyy"),
                now.ToString("MM")
            );

            string logFileName = $"log-{now:yyyy-MM-dd}.json";

            return Path.Combine(logDirectory, logFileName);
        }
    }

    internal class ExceptionLogEntry
    {
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = string.Empty;

        [JsonPropertyName("service")]
        public string Service { get; set; } = string.Empty;

        [JsonPropertyName("log_type")]
        public string LogType { get; set; } = "exception";

        [JsonPropertyName("exception_type")]
        public string ExceptionType { get; set; } = string.Empty;

        [JsonPropertyName("exception_message")]
        public string ExceptionMessage { get; set; } = string.Empty;

        [JsonPropertyName("inner_exception")] // เพิ่มฟิลด์ใหม่
        public string? InnerException { get; set; }

        [JsonPropertyName("stack_trace")]
        public string StackTrace { get; set; } = string.Empty;

        [JsonPropertyName("instance")]
        public string Instance { get; set; } = string.Empty;
    }
}
