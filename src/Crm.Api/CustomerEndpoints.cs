using System.Text.Json;
using System.Text.RegularExpressions;
using Crm.Shared;

namespace Crm.Api;

public static partial class CustomerEndpoints
{
    private static readonly string[] REQUIRED_FIELDS = ["firstName", "lastName", "email"];

    [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$")]
    private static partial Regex EmailPattern();

    public static void MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var customers = app.MapGroup("/api/customers");

        customers.MapGet("/", (CustomerStore customerStore) => Results.Ok(customerStore.List()));

        customers.MapPost("/", async (HttpRequest request, CustomerStore customerStore) =>
        {
            var (customer, error) = ParseCustomer(await TryReadJson(request));
            if (customer == null)
            {
                return Results.BadRequest(new { error });
            }
            if (customerStore.HasEmail(customer.Email))
            {
                return Results.Conflict(new { error = "A customer with this email already exists" });
            }
            var created = customerStore.Create(customer);
            return Results.Created($"/api/customers/{created.Id}", created);
        });

        customers.MapDelete("/{customerId}", (string customerId, CustomerStore customerStore) =>
            long.TryParse(customerId, out var parsedCustomerId) && customerStore.Remove(parsedCustomerId)
                ? Results.NoContent()
                : Results.NotFound(new { error = "Customer not found" }));
    }

    private static async Task<JsonElement?> TryReadJson(HttpRequest request)
    {
        try
        {
            return await JsonSerializer.DeserializeAsync<JsonElement>(request.Body);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static (NewCustomer? Customer, string? Error) ParseCustomer(JsonElement? body)
    {
        var values = REQUIRED_FIELDS.ToDictionary(field => field, field => ReadTrimmedString(body, field));
        var missingFields = REQUIRED_FIELDS.Where(field => values[field] == "").ToList();
        if (missingFields.Count > 0)
        {
            return (null, $"Missing required fields: {string.Join(", ", missingFields)}");
        }
        if (!EmailPattern().IsMatch(values["email"]))
        {
            return (null, "Email is not valid");
        }
        return (new NewCustomer(values["firstName"], values["lastName"], values["email"]), null);
    }

    private static string ReadTrimmedString(JsonElement? body, string field) =>
        body is { ValueKind: JsonValueKind.Object } && body.Value.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()!.Trim()
            : "";
}
