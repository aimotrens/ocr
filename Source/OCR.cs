using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace Ocr;

internal class Ocr
{
    public async Task<string[]> AnalyzePdf(Stream pdf)
    {
        byte[] pdfData;
        using (MemoryStream ms = new())
        {
            await pdf.CopyToAsync(ms);
            pdfData = ms.ToArray();
        }

        var images = ExtractBitmaps(pdfData);
        var extractedTexts = ExtractPageTexts(pdfData);
        var texts = new string[images.Length];

        for (var i = 0; i < images.Length; i++)
        {
            texts[i] = extractedTexts[i] + "\r\n\r\n".PadRight(25, '-') + "\r\n\r\n";

            if (images[i] != null)
            {
                using var imgMs = new MemoryStream(images[i]);
                imgMs.Position = 0;
                texts[i] += await AnalyzeImage(imgMs);
            }
        }

        return texts;
    }

    public async Task<string> AnalyzeImage(Stream data)
    {
        var tesseract = new Process();
        tesseract.StartInfo = new ProcessStartInfo
        {
            FileName = "tesseract",
            Arguments = "-l deu - -",
            RedirectStandardInput = true,
            RedirectStandardOutput = true
        };

        try
        {
            tesseract.Start();

            await data.CopyToAsync(tesseract.StandardInput.BaseStream);
            tesseract.StandardInput.Close();

            return await tesseract.StandardOutput.ReadToEndAsync();
        }
        finally
        {
            tesseract.Kill();
        }
    }

    private static byte[][] ExtractBitmaps(byte[] pdfData)
    {
        using var ms = new MemoryStream(pdfData);
        using var reader = new PdfReader(ms);
        using var doc = new PdfDocument(reader);
        
        var extractor = new ImageExtractor();
        var proc = new PdfCanvasProcessor(extractor);
        for (var i = 1; i <= doc.GetNumberOfPages(); i++)
            proc.ProcessPageContent(doc.GetPage(i));

        return extractor.Images.ToArray();
    }

    private static string[] ExtractPageTexts(byte[] pdfData)
    {
        using var ms = new MemoryStream(pdfData);
        using var reader = new PdfReader(ms);
        using var doc = new PdfDocument(reader);

        var texts = new string[doc.GetNumberOfPages()];

        for (var i = 1; i <= texts.Length; i++)
            texts[i - 1] = PdfTextExtractor.GetTextFromPage(doc.GetPage(i), new SimpleTextExtractionStrategy());

        return texts;
    }

    private class ImageExtractor : IEventListener
    {
        public List<byte[]> Images { get; } = new();

        public void EventOccurred(IEventData data, EventType type)
        {
            if (data is ImageRenderInfo imageData)
                try
                {
                    var imageObject = imageData.GetImage();
                    if (imageObject != null)
                        //File.WriteAllBytes(string.Format(format, index++, imageObject.IdentifyImageFileExtension()), imageObject.GetImageBytes());
                        Images.Add(imageObject.GetImageBytes());
                    else
                        Console.WriteLine("Image could not be read.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Image could not be read: {0}.", ex.Message);
                }
        }

        public ICollection<EventType> GetSupportedEvents()
        {
            return null;
        }
    }
}