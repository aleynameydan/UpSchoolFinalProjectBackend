using Domain.Entities;
using OfficeOpenXml;

namespace Application.Common.Models;

public class ExcelModel
{
    public void ExportToExcel(List<Product> productList, string recipientEmail, string subject, string body)
    {
        //creating new excel package

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using (ExcelPackage package = new ExcelPackage())
        {
            //creating new worksheet

            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Products");
            
            //adding row headers
            worksheet.Cells[1, 1].Value = "Name";
            worksheet.Cells[1, 2].Value = "Price";
            worksheet.Cells[1, 3].Value = "Sale Price";
            worksheet.Cells[1, 4].Value = "IsOnSale";
            worksheet.Cells[1, 5].Value = "Picture";
            
            //adding products to Products Excel File
            for (int i = 0; i < productList.Count; i++)
            {
                Product product = productList[i];
                worksheet.Cells[i + 2, 1].Value = product.Name;
                worksheet.Cells[i + 2, 2].Value = product.Price;
                worksheet.Cells[i + 2, 3].Value = product.SalePrice;
                worksheet.Cells[i + 2, 4].Value = product.IsOnSale;
                worksheet.Cells[i + 2, 5].Value = product.Picture;
            }
            
            //saving the file

            string filePath =
                @"/Users/aleynameydan/Desktop/UpSchool-FullStack-Development-Bootcamp/Project/Application/Common/Models/ExcelFiles";

            FileInfo fileInfo = new FileInfo(filePath);
            
            package.SaveAs(fileInfo);
            
            //sending excel file by mail

            MailSender mailSender = new MailSender();
            
            mailSender.SendEmailWithExcelAttachment(recipientEmail, subject, body, filePath);

        }
    }
}