using Crm.Shared;
using Microsoft.Data.Sqlite;

namespace Crm.Api;

public sealed class CustomerStore : IDisposable
{
    public const string IN_MEMORY_PATH = ":memory:";

    private const string CUSTOMER_COLUMNS = "id, first_name, last_name, email, created_at";

    private readonly SqliteConnection connection;
    private readonly Lock connectionLock = new();

    public CustomerStore(string databasePath)
    {
        if (databasePath != IN_MEMORY_PATH)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(databasePath))!);
        }
        connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString());
        connection.Open();
        Execute("""
            CREATE TABLE IF NOT EXISTS customers (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                first_name TEXT NOT NULL,
                last_name TEXT NOT NULL,
                email TEXT NOT NULL UNIQUE COLLATE NOCASE,
                created_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now'))
            )
            """);
    }

    public IReadOnlyList<Customer> List() => Query($"SELECT {CUSTOMER_COLUMNS} FROM customers ORDER BY last_name, first_name, id");

    public Customer? Get(long customerId) => Query($"SELECT {CUSTOMER_COLUMNS} FROM customers WHERE id = $id", ("$id", customerId)).FirstOrDefault();

    public bool HasEmail(string email) => Query($"SELECT {CUSTOMER_COLUMNS} FROM customers WHERE email = $email", ("$email", email)).Count > 0;

    public Customer Create(NewCustomer customer)
    {
        lock (connectionLock)
        {
            Execute(
                "INSERT INTO customers (first_name, last_name, email) VALUES ($firstName, $lastName, $email)",
                ("$firstName", customer.FirstName),
                ("$lastName", customer.LastName),
                ("$email", customer.Email));
            using var command = CreateCommand("SELECT last_insert_rowid()");
            return Get((long)command.ExecuteScalar()!)!;
        }
    }

    public bool Remove(long customerId) => Execute("DELETE FROM customers WHERE id = $id", ("$id", customerId)) > 0;

    public void Dispose() => connection.Dispose();

    private SqliteCommand CreateCommand(string sql, params (string Name, object Value)[] parameters)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        parameters.ToList().ForEach(parameter => command.Parameters.AddWithValue(parameter.Name, parameter.Value));
        return command;
    }

    private int Execute(string sql, params (string Name, object Value)[] parameters)
    {
        lock (connectionLock)
        {
            using var command = CreateCommand(sql, parameters);
            return command.ExecuteNonQuery();
        }
    }

    private List<Customer> Query(string sql, params (string Name, object Value)[] parameters)
    {
        lock (connectionLock)
        {
            using var command = CreateCommand(sql, parameters);
            using var reader = command.ExecuteReader();
            var customers = new List<Customer>();
            while (reader.Read())
            {
                customers.Add(new Customer(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4)));
            }
            return customers;
        }
    }
}
