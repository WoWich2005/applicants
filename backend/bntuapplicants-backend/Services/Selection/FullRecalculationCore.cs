namespace bntuapplicants_backend.Services.Selection
{
    public static class FullRecalculationCore
    {
        public sealed record Application(int ApplicantId, int CategoryId, int SelectionPriority);

        public sealed record Category(int Id, int CompetitionListId, int Quota, int Priority, int CriteriaGroupId);

        public sealed record Snapshot(
            IReadOnlyList<Application> Applications,
            IReadOnlyList<Category> Categories,
            IReadOnlyDictionary<int, int> CompetitionListPlans,
            // groupId -> criteriaIds in priority ASC
            IReadOnlyDictionary<int, IReadOnlyList<int>> CriteriaByGroup,
            // applicantId -> (criteriaId -> value)
            IReadOnlyDictionary<int, IReadOnlyDictionary<int, int>> Scores,
            // criteriaId -> true if higher_is_better, false if lower_is_better
            IReadOnlyDictionary<int, bool> CriteriaHigherIsBetter
        );

        public sealed record Selection(int ApplicantId, int CategoryId);

        public static IReadOnlyList<Selection> Run(Snapshot snapshot)
        {
            var categoryMap = snapshot.Categories.ToDictionary(c => c.Id);

            // Prefilter: drop applications where applicant is missing any criteria score for that category's group
            var validApplications = snapshot.Applications
                .Where(app =>
                {
                    if (!categoryMap.TryGetValue(app.CategoryId, out var cat)) return false;
                    if (!snapshot.CriteriaByGroup.TryGetValue(cat.CriteriaGroupId, out var criteria)) return true;
                    if (criteria.Count == 0) return true;
                    if (!snapshot.Scores.TryGetValue(app.ApplicantId, out var scores)) return false;
                    return criteria.All(cid => scores.ContainsKey(cid));
                })
                .ToList();

            // Build per-applicant preference list sorted by selectionPriority ASC
            var prefs = validApplications
                .GroupBy(a => a.ApplicantId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(a => a.SelectionPriority).ToArray()
                );

            var nextIdx = prefs.Keys.ToDictionary(id => id, _ => 0);

            // Per-category pool (SortedSet ordered by strength DESC via CategoryComparer)
            var catPool = snapshot.Categories.ToDictionary(c => c.Id, _ => new SortedSet<Candidate>(CategoryComparer.Instance));

            var clTotal = snapshot.Categories
                .GroupBy(c => c.CompetitionListId)
                .ToDictionary(g => g.Key, _ => 0);

            // Per-CL categories sorted by priority DESC (least protected first), tiebreak by id ASC
            var clCats = snapshot.Categories
                .GroupBy(c => c.CompetitionListId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(c => c.Priority).ThenBy(c => c.Id).Select(c => c.Id).ToList()
                );

            var freeQueue = new Queue<int>(prefs.Keys);

            while (freeQueue.Count > 0)
            {
                int applicantId = freeQueue.Dequeue();

                if (!nextIdx.TryGetValue(applicantId, out int idx) || idx >= prefs[applicantId].Length)
                    continue;

                var app = prefs[applicantId][idx];
                nextIdx[applicantId] = idx + 1;

                if (!categoryMap.TryGetValue(app.CategoryId, out var cat)) continue;

                int clId = cat.CompetitionListId;
                var scoreVector = BuildScoreVector(applicantId, cat.CriteriaGroupId, snapshot);
                var candidate = new Candidate(applicantId, app.SelectionPriority, scoreVector);

                catPool[app.CategoryId].Add(candidate);
                clTotal[clId]++;

                // Phase 1: quota check
                while (catPool[app.CategoryId].Count > cat.Quota)
                {
                    var worst = catPool[app.CategoryId].Max!;
                    catPool[app.CategoryId].Remove(worst);
                    clTotal[clId]--;
                    freeQueue.Enqueue(worst.ApplicantId);
                }

                // Phase 2: plan check
                if (snapshot.CompetitionListPlans.TryGetValue(clId, out int plan))
                {
                    while (clTotal[clId] > plan)
                    {
                        // Find least-protected non-empty category (highest priority number)
                        int? victimCatId = null;
                        if (clCats.TryGetValue(clId, out var catList))
                        {
                            foreach (int cId in catList)
                            {
                                if (catPool[cId].Count > 0)
                                {
                                    victimCatId = cId;
                                    break;
                                }
                            }
                        }
                        if (victimCatId == null) break;

                        var worst = catPool[victimCatId.Value].Max!;
                        catPool[victimCatId.Value].Remove(worst);
                        clTotal[clId]--;
                        freeQueue.Enqueue(worst.ApplicantId);
                    }
                }
            }

            var result = new List<Selection>();
            foreach (var (catId, pool) in catPool)
                foreach (var candidate in pool)
                    result.Add(new Selection(candidate.ApplicantId, catId));

            return result;
        }

        private static int[] BuildScoreVector(
            int applicantId,
            int criteriaGroupId,
            Snapshot snapshot)
        {
            if (!snapshot.CriteriaByGroup.TryGetValue(criteriaGroupId, out var criteriaIds) || criteriaIds.Count == 0)
                return [];

            if (!snapshot.Scores.TryGetValue(applicantId, out var scores))
                return new int[criteriaIds.Count];

            return [.. criteriaIds.Select(cid =>
            {
                int v = scores.TryGetValue(cid, out int val) ? val : 0;
                // Negate lower_is_better values so CategoryComparer (always DESC) works correctly:
                // lower raw value → higher negated value → ranked first
                bool higherIsBetter = !snapshot.CriteriaHigherIsBetter.TryGetValue(cid, out bool hib) || hib;
                return higherIsBetter ? v : -v;
            })];
        }
    }
}
