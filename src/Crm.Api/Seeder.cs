using Crm.Shared;

namespace Crm.Api;

public static class Seeder
{
    private static readonly NewCustomer[] DEMO_CUSTOMERS =
    [
        new("Ada", "Lovelace", "ada@example.com"),
        new("Grace", "Hopper", "grace@example.com"),
        new("Alan", "Turing", "alan@example.com"),
        new("Katherine", "Johnson", "katherine@example.com"),
    ];

    public static void Run(string databasePath)
    {
        using var customerStore = new CustomerStore(databasePath);
        var newCustomers = DEMO_CUSTOMERS.Where(customer => !customerStore.HasEmail(customer.Email)).ToList();
        newCustomers.ForEach(customer => customerStore.Create(customer));
        Console.WriteLine($"Seeded {newCustomers.Count} customers into {Path.GetFullPath(databasePath)}");
    }
}
