using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Ocr.Controllers;

[ApiController]
[Route("[controller]")]
public class OcrController : ControllerBase
{
    private readonly ILogger<OcrController> _logger;

    public OcrController(ILogger<OcrController> logger)
    {
        _logger = logger;
    }

    [HttpPost("analyze_pdf")]
    public async Task<string[]> AnalyzePdf()
    {
        Ocr ocr = new();
        return await ocr.AnalyzePdf(Request.Body);
    }

    [HttpPost("analyze_image")]
    public async Task<string> AnalyzeImage()
    {
        Ocr ocr = new();
        return await ocr.AnalyzeImage(Request.Body);
    }
}