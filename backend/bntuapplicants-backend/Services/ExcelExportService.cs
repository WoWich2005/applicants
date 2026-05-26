using bntuapplicants_backend.Dtos.Responses;
using ClosedXML.Excel;

namespace bntuapplicants_backend.Services
{
    public class ExcelExportService
    {
        public Task<byte[]> GenerateCompetitionListReportAsync(CompetitionListResultDto cl)
        {
            using var workbook = new XLWorkbook();

            if (cl.Categories.Count == 0)
            {
                workbook.Worksheets.Add("Пусто");
                return Task.FromResult(ToByteArray(workbook));
            }

            foreach (var cat in cl.Categories)
            {
                string sheetName = SanitizeSheetName(cat.Name);
                var ws = workbook.Worksheets.Add(sheetName);

                ws.Cell(1, 1).Value = "ID";
                ws.Cell(1, 2).Value = "Идентификатор";
                ws.Cell(1, 3).Value = "ФИО";
                for (int i = 0; i < cat.Criteria.Count; i++)
                    ws.Cell(1, 4 + i).Value = cat.Criteria[i].Name;
                int admittedToCol = 4 + cat.Criteria.Count;
                ws.Cell(1, admittedToCol).Value = "Зачислен в";

                int totalCols = admittedToCol;
                ws.Range(1, 1, 1, totalCols).Style.Font.Bold = true;

                int row = 2;

                // Section: admitted
                var admittedHeader = ws.Range(row, 1, row, totalCols);
                admittedHeader.Merge();
                admittedHeader.FirstCell().Value = "Зачислены";
                admittedHeader.Style.Font.Bold = true;
                admittedHeader.Style.Fill.PatternType = XLFillPatternValues.Solid;
                admittedHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#C6E0B4");
                row++;

                foreach (var a in cat.Admitted)
                    row = WriteApplicantRow(ws, row, a, cat.Criteria, admittedToCol);

                row++; // empty row

                // Section: not admitted
                var notAdmittedHeader = ws.Range(row, 1, row, totalCols);
                notAdmittedHeader.Merge();
                notAdmittedHeader.FirstCell().Value = "Подавали заявку, но не прошли";
                notAdmittedHeader.Style.Font.Bold = true;
                notAdmittedHeader.Style.Fill.PatternType = XLFillPatternValues.Solid;
                notAdmittedHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFC7CE");
                row++;

                foreach (var a in cat.NotAdmitted)
                    row = WriteApplicantRow(ws, row, a, cat.Criteria, admittedToCol, a.AdmittedTo ?? "Никуда");

                ws.Columns().AdjustToContents();
            }

            return Task.FromResult(ToByteArray(workbook));
        }

        // Excel sheet names may not contain: \ / * ? [ ] :
        private static string SanitizeSheetName(string name)
        {
            var sanitized = string.Concat(name.Select(c => @"\/*?[]:".Contains(c) ? '_' : c));
            return sanitized.Length > 31 ? sanitized[..31] : sanitized;
        }

        private static byte[] ToByteArray(XLWorkbook workbook)
        {
            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        private static int WriteApplicantRow(IXLWorksheet ws, int row, ApplicantResultDto a, List<CriterionInfoDto> criteria, int admittedToCol, string? admittedTo = null)
        {
            ws.Cell(row, 1).Value = a.Id;
            ws.Cell(row, 2).Value = a.ExternalId;
            ws.Cell(row, 3).Value = a.Name;
            for (int i = 0; i < criteria.Count; i++)
                ws.Cell(row, 4 + i).Value = a.Scores.TryGetValue(criteria[i].Id, out int v) ? v : 0;
            if (admittedTo != null)
                ws.Cell(row, admittedToCol).Value = admittedTo;
            return row + 1;
        }
    }
}
