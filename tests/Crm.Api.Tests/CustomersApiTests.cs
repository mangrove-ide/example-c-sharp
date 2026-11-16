using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Crm.Api;
using Crm.Shared;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Crm.Api.Tests;

public sealed class CustomersApiTests : IDisposable
{
    private const string CUSTOMERS_URL = "/api/customers";

    private readonly WebApplicationFactory<Program> factory;
    private readonly HttpClient client;

    public CustomersApiTests()
    {
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.Replace(ServiceDescriptor.Singleton(_ => new CustomerStore(CustomerStore.IN_MEMORY_PATH)))));
        client = factory.CreateClient();
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
    }

    private Task<HttpResponseMessage> PostCustomer(object body) => client.PostAsJsonAsync(CUSTOMERS_URL, body);

    private static async Task<string> ReadError(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString()!;

    [Fact]
    public async Task CreatesListsAndDeletesACustomer()
    {
        var createResponse = await PostCustomer(new { firstName = " Ada ", lastName = "Lovelace", email = "ada@example.com" });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = (await createResponse.Content.ReadFromJsonAsync<Customer>())!;
        Assert.Equal(("Ada", "Lovelace", "ada@example.com"), (created.FirstName, created.LastName, created.Email));
        Assert.True(created.Id > 0);
        Assert.False(string.IsNullOrEmpty(created.CreatedAt));

        Assert.Single((await client.GetFromJsonAsync<List<Customer>>(CUSTOMERS_URL))!);

        var deleteResponse = await client.DeleteAsync($"{CUSTOMERS_URL}/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<List<Customer>>(CUSTOMERS_URL))!);
    }

    [Fact]
    public async Task RejectsMissingFieldsAndInvalidEmails()
    {
        var missingResponse = await PostCustomer(new { firstName = "Ada" });
        Assert.Equal(HttpStatusCode.BadRequest, missingResponse.StatusCode);
        Assert.Contains("lastName, email", await ReadError(missingResponse));

        var invalidResponse = await PostCustomer(new { firstName = "Ada", lastName = "Lovelace", email = "not-an-email" });
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
    }

    [Fact]
    public async Task RejectsDuplicateEmailsRegardlessOfCase()
    {
        await PostCustomer(new { firstName = "Ada", lastName = "Lovelace", email = "ada@example.com" });
        var duplicateResponse = await PostCustomer(new { firstName = "Ada", lastName = "L", email = "ADA@example.com" });
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task ListsCustomersByLastThenFirstName()
    {
        await PostCustomer(new { firstName = "Grace", lastName = "Hopper", email = "grace@example.com" });
        await PostCustomer(new { firstName = "Ada", lastName = "Lovelace", email = "ada@example.com" });
        await PostCustomer(new { firstName = "Alan", lastName = "Hopper", email = "alan@example.com" });

        var customers = (await client.GetFromJsonAsync<List<Customer>>(CUSTOMERS_URL))!;
        Assert.Equal(["alan@example.com", "grace@example.com", "ada@example.com"], customers.Select(customer => customer.Email));
    }

    [Fact]
    public async Task Returns404WhenDeletingAnUnknownCustomer()
    {
        var response = await client.DeleteAsync($"{CUSTOMERS_URL}/999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
