using System.Net.Http.Json;
using System.Text.Json;
using Crm.Shared;

namespace Crm.Web;

public sealed class ApiException(string message) : Exception(message);

public sealed class CustomersClient(HttpClient httpClient)
{
    private const string CUSTOMERS_URL = "api/customers";

    public async Task<List<Customer>> ListCustomers() =>
        (await (await Send(new HttpRequestMessage(HttpMethod.Get, CUSTOMERS_URL))).Content.ReadFromJsonAsync<List<Customer>>())!;

    public async Task<Customer> CreateCustomer(NewCustomer customer) =>
        (await (await Send(new HttpRequestMessage(HttpMethod.Post, CUSTOMERS_URL) { Content = JsonContent.Create(customer) })).Content.ReadFromJsonAsync<Customer>())!;

    public async Task DeleteCustomer(long customerId) => await Send(new HttpRequestMessage(HttpMethod.Delete, $"{CUSTOMERS_URL}/{customerId}"));

    private async Task<HttpResponseMessage> Send(HttpRequestMessage request)
    {
        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(await TryReadError(response) ?? $"Request failed with status {(int)response.StatusCode}");
        }
        return response;
    }

    private static async Task<string?> TryReadError(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String ? error.GetString() : null;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException or InvalidOperationException)
        {
            return null;
        }
    }
}
