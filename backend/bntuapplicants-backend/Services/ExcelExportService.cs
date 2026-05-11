using bntuapplicants_backend.Dtos.Responses;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace bntuapplicants_backend.Services
{
    public class ExcelExportService
    {
        public async Task<byte[]> GenerateCompetitionListReportAsync(CompetitionListResultDto cl)
        {
            ExcelPackage.License.SetNonCommercialPersonal("My Name");
            using var package = new ExcelPackage();

            if (cl.Categories.Count == 0)
            {
                package.Workbook.Worksheets.Add("Пусто");
                return await package.GetAsByteArrayAsync();
            }

            foreach (var cat in cl.Categories)
            {
                string sheetName = cat.Name.Length > 31 ? cat.Name[..31] : cat.Name;
                var ws = package.Workbook.Worksheets.Add(sheetName);

                // Build column headers: id | externalid | ФИО | criteria... | Зачислен в
                ws.Cells[1, 1].Value = "ID";
                ws.Cells[1, 2].Value = "Идентификатор";
                ws.Cells[1, 3].Value = "ФИО";
                for (int i = 0; i < cat.Criteria.Count; i++)
                    ws.Cells[1, 4 + i].Value = cat.Criteria[i].Name;
                int admittedToCol = 4 + cat.Criteria.Count;
                ws.Cells[1, admittedToCol].Value = "Зачислен в";

                int totalCols = admittedToCol;
                ws.Cells[1, 1, 1, totalCols].Style.Font.Bold = true;

                int row = 2;

                // Section: admitted
                ws.Cells[row, 1].Value = "Зачислены";
                ws.Cells[row, 1, row, totalCols].Merge = true;
                ws.Cells[row, 1].Style.Font.Bold = true;
                ws.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(198, 224, 180));
                row++;

                foreach (var a in cat.Admitted)
                    row = WriteApplicantRow(ws, row, a, cat.Criteria, admittedToCol);

                row++; // empty row

                // Section: not admitted
                ws.Cells[row, 1].Value = "Подавали заявку, но не прошли";
                ws.Cells[row, 1, row, totalCols].Merge = true;
                ws.Cells[row, 1].Style.Font.Bold = true;
                ws.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 199, 206));
                row++;

                foreach (var a in cat.NotAdmitted)
                    row = WriteApplicantRow(ws, row, a, cat.Criteria, admittedToCol, a.AdmittedTo ?? "Никуда");

                ws.Cells[ws.Dimension.Address].AutoFitColumns();
            }

            return await package.GetAsByteArrayAsync();
        }

        private static int WriteApplicantRow(ExcelWorksheet ws, int row, ApplicantResultDto a, List<CriterionInfoDto> criteria, int admittedToCol, string? admittedTo = null)
        {
            ws.Cells[row, 1].Value = a.Id;
            ws.Cells[row, 2].Value = a.ExternalId;
            ws.Cells[row, 3].Value = a.Name;
            for (int i = 0; i < criteria.Count; i++)
                ws.Cells[row, 4 + i].Value = a.Scores.TryGetValue(criteria[i].Id, out int v) ? v : 0;
            if (admittedTo != null)
                ws.Cells[row, admittedToCol].Value = admittedTo;
            return row + 1;
        }
    }
}
