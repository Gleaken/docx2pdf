using Docx2Pdf.Execptions;
using Microsoft.AspNetCore.Mvc;

namespace Docx2Pdf.Controllers;

[ApiController]
[Route("[controller]")]
public class ConvertController : ControllerBase
{
    private readonly ILogger<ConvertController> _logger;
    private readonly AppSettings _appSettings;

    public ConvertController(ILogger<ConvertController> logger, AppSettings appSettings)
    {
        _logger = logger;
        _appSettings = appSettings;
    }

    [HttpPost]
    public async Task<IActionResult> Post(IFormFile file)
    {
        try
        {
            ValidateRequest(file);
            var path = await SaveFile(file);
            await ConvertDocx(path);

            var content = GetPdfContent(path);
            return content;
        }
        catch (Exception ex)
        {
            switch (ex)
            {
                case FileFormatException:
                case FileSizeException:
                    _logger.LogWarning("{exception}", ex);
                    return BadRequest(ex.Message);
                default:
                    _logger.LogError("{exception}", ex);
                    throw;
            }
        }
    }

    private FileStreamResult GetPdfContent(string path)
    {
        var pdfPath = path.Replace("docx", "pdf");
        var content = new FileStreamResult(new FileStream(pdfPath, FileMode.Open), "application/pdf");
        
        if (!_appSettings.DontDeletePdfFiles)
            DeleteFiles(path, pdfPath);
        
        _logger.LogInformation("Successfully converted file {path} to {pdfPath}", path, pdfPath);
        return content;
    }

    private async Task<string> SaveFile(IFormFile file)
    {
        var path = Path.Combine(_appSettings.WorkingDirectory, file.FileName.ToLower());
        _logger.LogInformation("Received file: {uploadedFile}", file.FileName);
        await using var stream = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(stream);
        return path;
    }

    private void ValidateRequest(IFormFile file)
    {
        if (!file.FileName.ToLower().EndsWith(".docx"))
            throw new FileFormatException();
        if (file.Length > _appSettings.MaxFileSize)
            throw new FileSizeException(file.Length, _appSettings.MaxFileSize);
    }

    private void DeleteFiles(string path, string pdfPath)
    {
        System.IO.File.Delete(path);
        _logger.LogTrace("Deleted file {path}", path);
        System.IO.File.Delete(pdfPath);
        _logger.LogTrace("Deleted file {pdfPath}", pdfPath);
    }

    private async Task ConvertDocx(string path)
    {
        using var proc = new System.Diagnostics.Process();
        proc.StartInfo.FileName = "/bin/bash";
        proc.StartInfo.Arguments = "-c \" " + $" libreoffice --headless --convert-to pdf {path}" +
                                   (string.IsNullOrEmpty(_appSettings.WorkingDirectory)
                                       ? "\""
                                       : $" --outdir {_appSettings.WorkingDirectory}\"");
        proc.StartInfo.RedirectStandardOutput = true;
        proc.StartInfo.RedirectStandardError = true;
        proc.Start();
        
        var result = await proc.StandardOutput.ReadToEndAsync();
        var errorMessage = await proc.StandardError.ReadToEndAsync();

        await proc.WaitForExitAsync();

        LogOutput(errorMessage, result);
    }

    private void LogOutput(string errorMessage, string result)
    {
        var splittedErrors = errorMessage.Split(Environment.NewLine);
        var errors = splittedErrors.Where(x => x.StartsWith("Error")).Select(x => $"{x}{Environment.NewLine}").ToList();
        var warnings = splittedErrors.Where(x => x.StartsWith("Warning")).Select(x => $"{x}{Environment.NewLine}").ToList();

        _logger.LogInformation("Result: {result}", result);
        if (errors.Any())
        {
            _logger.LogError("{errors}", errors);
            throw new Exception($"{string.Join(Environment.NewLine, errors)}");
        }

        if (warnings.Any())
        {
            _logger.LogWarning("{warnings}", warnings);
        }
    }
}
