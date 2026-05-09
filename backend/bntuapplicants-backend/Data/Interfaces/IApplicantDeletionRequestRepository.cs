using bntuapplicants_backend.Dtos.Responses;

namespace bntuapplicants_backend.Data.Interfaces
{
    public interface IApplicantDeletionRequestRepository
    {
        Task<bool> RequestAsync(int applicantId, int? requestedByUserId);
        Task<PagedResponse<PendingDeletionDto>> GetPagedAsync(int page, int pageSize, string? search, string? requestedBySearch = null, string? status = null, string? idSearch = null);
        Task<bool> ConfirmAsync(int applicantId);
        Task<bool> RejectAsync(int applicantId);
    }
}
