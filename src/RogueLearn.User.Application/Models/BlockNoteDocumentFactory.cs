namespace RogueLearn.User.Application.Models;

public static class BlockNoteDocumentFactory
{
    public static List<object> FromPlainText(string text, int headingLevel = 2)
    {
        var blocks = new List<object>();
        if (string.IsNullOrWhiteSpace(text)) return blocks;

        var lines = text.Replace("\r\n", "\n").Split('\n');
        var nonEmptyLines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

        if (nonEmptyLines.Count > 0)
        {
            var headingId = Guid.NewGuid().ToString();
            blocks.Add(new
            {
                id = headingId,
                type = "heading",
                props = new Dictionary<string, object>
                {
                    { "level", headingLevel },
                    { "textColor", "default" },
                    { "isToggleable", false },
                    { "textAlignment", "left" },
                    { "backgroundColor", "default" }
                },
                content = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        { "type", "text" },
                        { "text", nonEmptyLines[0].Trim() },
                        { "styles", new Dictionary<string, object>() }
                    }
                },
                children = new List<object>()
            });
        }

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            var paragraphId = Guid.NewGuid().ToString();
            var contentList = string.IsNullOrWhiteSpace(line)
                ? new List<object>()
                : new List<object>
                {
                    new Dictionary<string, object>
                    {
                        { "type", "text" },
                        { "text", line.Trim() },
                        { "styles", new Dictionary<string, object>() }
                    }
                };

            blocks.Add(new
            {
                id = paragraphId,
                type = "paragraph",
                props = new Dictionary<string, object>
                {
                    { "textColor", "default" },
                    { "textAlignment", "left" },
                    { "backgroundColor", "default" }
                },
                content = contentList,
                children = new List<object>()
            });
        }

        return blocks;
    }
}