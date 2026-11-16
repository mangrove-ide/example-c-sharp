using System.Net;
using System.Net.Http.Json;
using Bunit;
using Crm.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Crm.Web.Tests;

public sealed class AppTests : BunitContext
{
    private static readonly Customer ADA = new(1, "Ada", "Lovelace", "ada@example.com", "2026-01-01T00:00:00.000Z");
    private static readonly Customer GRACE = new(2, "Grace", "Hopper", "grace@example.com", "2026-01-01T00:00:00.000Z");

    private readonly QueuedHttpHandler httpHandler = new();

    public AppTests()
    {
        Services.AddSingleton(new CustomersClient(new HttpClient(httpHandler) { BaseAddress = new Uri("http://localhost/") }));
        JSInterop.Setup<bool>("confirm", _ => true).SetResult(true);
    }

    private static HttpResponseMessage JsonResponse(object? body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = body == null ? null : JsonContent.Create(body) };

    private static void FillForm(IRenderedComponent<App> cut, NewCustomer customer)
    {
        cut.Find("input[aria-label='First name']").Change(customer.FirstName);
        cut.Find("input[aria-label='Last name']").Change(customer.LastName);
        cut.Find("input[aria-label='Email']").Change(customer.Email);
        cut.Find("form").Submit();
    }

    [Fact]
    public void ListsAddsAndRemovesCustomers()
    {
        httpHandler.Enqueue(JsonResponse(new[] { ADA }));
        var cut = Render<App>();
        cut.WaitForAssertion(() => Assert.Contains("Lovelace", cut.Markup));

        httpHandler.Enqueue(JsonResponse(GRACE, HttpStatusCode.Created));
        FillForm(cut, new NewCustomer("Grace", "Hopper", "grace@example.com"));
        cut.WaitForAssertion(() => Assert.Contains("Hopper", cut.Markup));

        httpHandler.Enqueue(JsonResponse(null, HttpStatusCode.NoContent));
        cut.Find("button[aria-label='Remove Ada Lovelace']").Click();
        cut.WaitForAssertion(() => Assert.DoesNotContain("Lovelace", cut.Markup));
        var lastRequest = httpHandler.Requests.Last();
        Assert.Equal((HttpMethod.Delete, "/api/customers/1"), (lastRequest.Method, lastRequest.RequestUri!.AbsolutePath));
    }

    [Fact]
    public void ShowsTheApiErrorWhenAddingFails()
    {
        httpHandler.Enqueue(JsonResponse(Array.Empty<Customer>()));
        var cut = Render<App>();
        cut.WaitForAssertion(() => Assert.Contains("No customers yet", cut.Markup));

        httpHandler.Enqueue(JsonResponse(new { error = "A customer with this email already exists" }, HttpStatusCode.Conflict));
        FillForm(cut, new NewCustomer("Ada", "Lovelace", "ada@example.com"));
        cut.WaitForAssertion(() => Assert.Contains("already exists", cut.Find("[role='alert']").TextContent));
    }

    private sealed class QueuedHttpHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> responses = new();

        public List<HttpRequestMessage> Requests { get; } = [];

        public void Enqueue(HttpResponseMessage response) => responses.Enqueue(response);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(responses.Dequeue());
        }
    }
}
