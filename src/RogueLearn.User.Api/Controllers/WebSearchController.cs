using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Api.Controllers
{
    [ApiController]
    [Route("api/web-search")]
    public class WebSearchController : ControllerBase
    {
        private readonly Kernel _kernel;
        private readonly IWebSearchService? _webSearchService;

        public WebSearchController(Kernel kernel, IServiceProvider serviceProvider)
        {
            _kernel = kernel;
            _webSearchService = serviceProvider.GetService<IWebSearchService>();
        }

        /// <summary>
        /// Test endpoint that performs a web search around a topic and returns an AI-crafted answer
        /// based solely on the search results.
        /// </summary>
        /// <param name="topic">The topic to research on the web.</param>
        /// <returns>Answer generated from web search results.</returns>
        [HttpGet("test")]
        public async Task<IActionResult> Test([FromQuery] string topic, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                return BadRequest("Query parameter 'topic' is required.");
            }

            if (_webSearchService is null)
            {
                return BadRequest("Web search connector is not configured. Please set GoogleSearch:ApiKey and GoogleSearch:SearchEngineId in configuration.");
            }

            // Perform web search and format results
            var results = await _webSearchService.SearchAsync(topic, count: 10, offset: 0, cancellationToken);
            var formattedResults = string.Join("\n\n", results);

            var prompt = @"
                You are a research assistant.

                User topic: {{$input}}

                Search results:
                {{$searchResults}}

                Task:
                - Based ONLY on the search results, select up to 4 distinct and relevant articles (prefer authoritative sources).
                - Produce a multiple-choice JSON object with this exact structure:
                  question: string
                  options: array of 4 objects [{ id: 'A'|'B'|'C'|'D', title: string, url: string, summary: string }]
                  correctOptionId: 'A'|'B'|'C'|'D'
                  rationale: string

                Rules:
                - Return ONLY valid JSON. No markdown, no code fences, no commentary.
                - The question should restate the user's topic as a question.
                - Each option MUST correspond to a different article from the search results and include the direct URL from the results.
                - summary must be one concise sentence per option.
                - Choose the most authoritative or comprehensive article as the correctOptionId and justify it in rationale.
                - If fewer than 4 suitable articles are available, include as many as you can, maintaining option IDs from 'A' upward.
            ";

            var researcher = _kernel.CreateFunctionFromPrompt(prompt);
            var variables = new KernelArguments
            {
                ["input"] = topic,
                ["searchResults"] = formattedResults
            };
            var result = await _kernel.InvokeAsync(researcher, variables, cancellationToken);

            var answer = result.GetValue<string>() ?? string.Empty;

            // Try to parse the answer as JSON and return it directly
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(answer);
                return new JsonResult(doc.RootElement);
            }
            catch
            {
                // Fallback: return raw string if parsing fails
                return Ok(new { topic, answer });
            }
        }
    }
}