using Microsoft.AspNetCore.Mvc;

namespace StoryCoffee.Api.Controllers;

public sealed class FilesController(IDocumentStorageService storage) : ControllerBase
{
    [HttpGet("api/files/download")]
    public async Task<IActionResult> Download([FromQuery] string fileKey, [FromQuery] string fileName, [FromQuery] long expires, [FromQuery] string signature, CancellationToken cancellationToken)
    {
        var document = await storage.Get(fileKey, signature, expires, cancellationToken);
        if (document is null)
        {
            return NotFound(new { code = "FILE_NOT_FOUND", message = "File not found or URL has expired." });
        }

        return File(document.Content, document.ContentType, string.IsNullOrWhiteSpace(fileName) ? document.FileName : fileName);
    }
}
