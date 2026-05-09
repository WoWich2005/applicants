namespace bntuapplicants_backend.Dtos.Responses
{
    public class AlertsSummaryDto
    {
        public int UnvalidatedCount { get; set; }
        public int MissingCriteriaCount { get; set; }
        public int InvalidPrioritiesCount { get; set; }
        public int IncompleteCount { get; set; }
        public int InvalidCriteriaGroupsCount { get; set; }
        public int InvalidAdmissionCategoriesCount { get; set; }
        public int PendingDeletionsCount { get; set; }
    }
}
