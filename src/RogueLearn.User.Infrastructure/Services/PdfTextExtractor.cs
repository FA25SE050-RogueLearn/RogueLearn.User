using RogueLearn.User.Application.Interfaces;
using UglyToad.PdfPig;
using System.Text;

namespace RogueLearn.User.Infrastructure.Services;

public class PdfTextExtractor : IPdfTextExtractor
{
    public Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();

        // PdfPig requires a seekable stream; ensure position is at start
        if (pdfStream.CanSeek)
        {
            pdfStream.Position = 0;
        }

        using (var document = PdfDocument.Open(pdfStream))
        {
            foreach (var page in document.GetPages())
            {
                foreach (var word in page.GetWords())
                {
                    sb.Append(word.Text);
                    sb.Append(' ');
                }
                sb.AppendLine();
            }
        }

        return Task.FromResult(sb.ToString());
    }
}