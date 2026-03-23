using Newtonsoft.Json;

namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    public interface IPreSaveLogger
    {
        Task LogAsync(string? docId, string apiName, string messageId, object data);
    }

    public class PreSaveLogger : IPreSaveLogger
    {
        private readonly ILogger<PreSaveLogger> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private static readonly object _lock = new();

        public PreSaveLogger(ILogger<PreSaveLogger> logger, IConfiguration configuration, IWebHostEnvironment env)
        {
            _logger = logger;
            _configuration = configuration;
            _env = env;
        }

        public async Task LogAsync(string? docId, string apiName, string messageId, object data)
        {
            try
            {
                if (!_env.IsDevelopment()) return; // Bypass logging entirely if not in development mode
                
                var storageBase = _configuration["Configuration:StoragePath"] ?? "Storage";
                var logDir = Path.Combine(Directory.GetCurrentDirectory(), storageBase, "PreSave");
                Directory.CreateDirectory(logDir);

                var date = DateTime.Now.ToString("yyyy-MM-dd");
                var safeDocId = string.IsNullOrWhiteSpace(docId) ? "UnknownDocId" : string.Join("_", docId.Split(Path.GetInvalidFileNameChars()));
                var fileName = $"{safeDocId}_{date}.log";
                var filePath = Path.Combine(logDir, fileName);

                var logEntry = new
                {
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    MessageId = messageId,
                    ApiName = apiName,
                    Data = data
                };

                var json = JsonConvert.SerializeObject(logEntry, Formatting.Indented);
                var line = json + Environment.NewLine + "---" + Environment.NewLine;

                await File.AppendAllTextAsync(filePath, line);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PreSaveLogger failed for {ApiName}, MessageId: {MessageId}", apiName, messageId);
            }
        }
    }
}
