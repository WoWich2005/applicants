namespace bntuapplicants_backend.Dtos.Responses
{
    public class PagedResponse<T>
    {
        public List<T> Items { get; set; } = [];
        public int Total { get; set; }
    }
}
