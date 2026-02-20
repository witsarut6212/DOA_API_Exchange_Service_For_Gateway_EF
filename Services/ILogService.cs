namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    public interface ILogService
    {
        Task LogExceptionAsync(Exception exception, string instance, string? requestId = null);
    }
}
