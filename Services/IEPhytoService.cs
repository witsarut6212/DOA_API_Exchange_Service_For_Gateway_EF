using DOA_API_Exchange_Service_For_Gateway.Models.Requests;

namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    public interface IEPhytoService
    {
        Task<bool> SubmitEPhytoAsync(EPhytoRequest request, string source);
        Task<bool> IsDocumentExists(string docId, string docType, string docStatus);
    }
}
