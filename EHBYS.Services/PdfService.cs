using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;

namespace EHBYS.Services;

public static class PdfService
{
    public static void CreateStatement(string path, string memberName, string parcelNo, decimal debt, decimal payment)
    {
        using var writer = new PdfWriter(path);
        using var pdf = new PdfDocument(writer);
        using var doc = new Document(pdf);

        doc.Add(new Paragraph("EGE HOBI BAHCELERI KOOPERATIFI"));
        doc.Add(new Paragraph("RESMI HESAP EXTRESI"));
        doc.Add(new Paragraph("-----------------------------"));
        doc.Add(new Paragraph("Uye: " + memberName));
        doc.Add(new Paragraph("Parsel: " + parcelNo));
        doc.Add(new Paragraph("Borc: " + debt + " TL"));
        doc.Add(new Paragraph("Odeme: " + payment + " TL"));
        doc.Add(new Paragraph("Kalan: " + (debt - payment) + " TL"));
        doc.Add(new Paragraph("-----------------------------"));
        doc.Add(new Paragraph("Olusturma Tarihi: " + DateTime.Now));
    }
}
