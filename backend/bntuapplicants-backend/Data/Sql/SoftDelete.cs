namespace bntuapplicants_backend.Data.Sql
{
    public static class SoftDelete
    {
        public const string NotSoftDeleted = @"NOT EXISTS (
                SELECT 1 FROM applicant_deletion_requests dr
                WHERE dr.applicant_id = Applicants.id AND dr.status IN ('pending','confirmed'))";
    }
}
