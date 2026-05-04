using bntuapplicants_backend.Models;

namespace bntuapplicants_backend.Data.Interfaces
{
    public interface IResultRepository
    {
        Task<Dictionary<int, List<(string Name, int Points)>>> GetByFacultyIdAsync(int facultyId);
    }
}
