using System.IO;
using DinkToPdf;
using DinkToPdf.Contracts;

namespace CSDL.Services
{
    public class CertificateService
    {
        private readonly IConverter _converter;

        public CertificateService(IConverter converter)
        {
            _converter = converter;
        }

        public byte[] GenerateCertificate(string fullName, string eventName, string location, string date, string bloodType)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "CertificateTemplate.html");
            if (!System.IO.File.Exists(filePath))
            {
                throw new FileNotFoundException("Không tìm thấy file template tại: " + filePath);
            }
            var htmlContent = System.IO.File.ReadAllText(filePath)
       .Replace("{FULL_NAME}", fullName)
       .Replace("{EVENT_NAME}", eventName)
       .Replace("{LOCATION}", location)
       .Replace("{DATE}", date)
       .Replace("{BLOOD_TYPE}", bloodType)
       .Replace("{ISSUE_DATE}", DateTime.Now.ToString("dd/MM/yyyy"));

            Console.WriteLine("HTML đã thay thế xong:");
            Console.WriteLine(htmlContent);

            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = { PaperSize = PaperKind.A4 },
                Objects = { new ObjectSettings { HtmlContent = htmlContent } }
            };

            return _converter.Convert(doc);
        }
    }
}
