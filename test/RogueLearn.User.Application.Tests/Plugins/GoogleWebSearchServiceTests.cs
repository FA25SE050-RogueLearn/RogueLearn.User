using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Plugins;
using Xunit;

namespace RogueLearn.User.Application.Tests.Plugins;

public class CapturingHandler : DelegatingHandler
{
    public Uri? LastRequestUri { get; private set; }
    private readonly string _json;

    public CapturingHandler(string json)
    {
        _json = json;
        InnerHandler = new HttpClientHandler();
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequestUri = request.RequestUri;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(_json)
        };
        return Task.FromResult(response);
    }
}

public class FailingHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        return Task.FromResult(response);
    }
}

public class GoogleWebSearchServiceTests
{
    private static string BuildSearchJson()
    {
         var payload = new
         {
             items = new[]
             {
                 new { title = "RecyclerView tutorial", link = "https://geeksforgeeks.org/android-recyclerview", snippet = "gfg" },
                 new { title = "RecyclerView docs", link = "https://developer.android.com/guide/topics/ui/layout/recyclerview", snippet = "docs" }
             }
         };
         return JsonSerializer.Serialize(payload);
    }

    [Fact]
    public async Task SearchAsync_Prioritizes_TutorialSites_Over_OfficialDocs()
    {
        var json = BuildSearchJson();
        var handler = new CapturingHandler(json);
        var http = new HttpClient(handler);
        var logger = Substitute.For<ILogger<GoogleWebSearchService>>();
        var svc = new GoogleWebSearchService("key", "cx", http, logger);

        var results = (await svc.SearchAsync("Android RecyclerView", 5)).ToList();
        results.Should().HaveCount(2);
        results.First().Should().Contain("geeksforgeeks");
        results.Last().Should().Contain("developer.android.com");
        results.First().Should().Contain("Source: Trusted Tutorial Site");
        results.Last().Should().Contain("Source: Official Documentation");
    }

    [Fact]
    public async Task SearchAsync_VietnameseQuery_BoostsVietnameseSitesInQuery()
    {
        var json = BuildSearchJson();
        var handler = new CapturingHandler(json);
        var http = new HttpClient(handler);
        var svc = new GoogleWebSearchService("key", "cx", http);

        await svc.SearchAsync("Lập trình Android cơ bản", 5);
        handler.LastRequestUri.Should().NotBeNull();
        handler.LastRequestUri!.ToString().Should().Contain("viblo.asia");
        handler.LastRequestUri!.ToString().Should().Contain("topdev.vn");
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_OnHttpFailure()
    {
        var http = new HttpClient(new FailingHandler());
        var svc = new GoogleWebSearchService("key", "cx", http);

        var results = await svc.SearchAsync("test", 5);
        results.Should().BeEmpty();
    }
}