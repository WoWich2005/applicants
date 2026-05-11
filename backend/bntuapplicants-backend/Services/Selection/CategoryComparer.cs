namespace bntuapplicants_backend.Services.Selection
{
    public sealed record Candidate(int ApplicantId, int SelectionPriority, int[] ScoreVector);

    public sealed class CategoryComparer : IComparer<Candidate>
    {
        public static readonly CategoryComparer Instance = new();

        private CategoryComparer() { }

        // Returns negative when a > b (stronger), so SortedSet orders strongest first.
        public int Compare(Candidate? a, Candidate? b)
        {
            if (a is null && b is null) return 0;
            if (a is null) return 1;
            if (b is null) return -1;

            // Score vector DESC (lexicographic)
            int len = Math.Min(a.ScoreVector.Length, b.ScoreVector.Length);
            for (int i = 0; i < len; i++)
            {
                int cmp = b.ScoreVector[i].CompareTo(a.ScoreVector[i]);
                if (cmp != 0) return cmp;
            }
            if (a.ScoreVector.Length != b.ScoreVector.Length)
                return b.ScoreVector.Length.CompareTo(a.ScoreVector.Length);

            // Tiebreaker: selectionPriority ASC (lower = better)
            int spCmp = a.SelectionPriority.CompareTo(b.SelectionPriority);
            if (spCmp != 0) return spCmp;

            // Final tiebreaker to ensure uniqueness in SortedSet
            return a.ApplicantId.CompareTo(b.ApplicantId);
        }
    }
}
