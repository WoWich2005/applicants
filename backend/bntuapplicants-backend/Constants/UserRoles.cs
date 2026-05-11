namespace bntuapplicants_backend.Constants
{
    public static class UserRoles
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string DataAdministrator = "DataAdministrator";
        public const string Auditor = "Auditor";
        public const string AdmissionsOperator = "AdmissionsOperator";
        public const string DataViewer = "DataViewer";

        public const string WriteStructure = $"{SuperAdmin},{DataAdministrator}";
        public const string WriteApplicants = $"{SuperAdmin},{DataAdministrator},{Auditor},{AdmissionsOperator}";
        public const string WriteAudit = $"{SuperAdmin},{DataAdministrator},{Auditor}";
    }
}
