using bntuapplicants_backend.Models;
using OfficeOpenXml;

namespace bntuapplicants_backend.Services
{
    public class ExcelExportService
    {
        public async Task<byte[]> GenerateAdmissionReportAsync(
            int facultyId,
            List<Specialty> facultySpecialties,
            Dictionary<int, List<(string Name, int Points)>> applicantsBySpecialty)
        {
            ExcelPackage.License.SetNonCommercialPersonal("My Name");
            using (var excelPackage = new ExcelPackage())
            {
                foreach (var specialty in facultySpecialties)
                {
                    var worksheet = excelPackage.Workbook.Worksheets.Add(specialty.Name);

                    worksheet.Cells[1, 1].Value = "№";
                    worksheet.Cells[1, 2].Value = "ФИО абитуриента";
                    worksheet.Cells[1, 3].Value = "Сумма баллов";

                    if (applicantsBySpecialty.TryGetValue(specialty.Id, out var applicants) && applicants.Any())
                    {
                        for (int i = 0; i < applicants.Count; i++)
                        {
                            worksheet.Cells[i + 2, 1].Value = i + 1;
                            worksheet.Cells[i + 2, 2].Value = applicants[i].Name;
                            worksheet.Cells[i + 2, 3].Value = applicants[i].Points;
                        }
                    }
                    else
                    {
                        worksheet.Cells[2, 1].Value = "Нет зачисленных абитуриентов";
                        worksheet.Cells[2, 1, 2, 3].Merge = true;
                    }

                    worksheet.Cells[1, 1, 1, 3].Style.Font.Bold = true;
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                }

                if(facultySpecialties.Count == 0)
                {
                    excelPackage.Workbook.Worksheets.Add("Пусто");
                }

                return await excelPackage.GetAsByteArrayAsync();
            }
        }
    }
}
