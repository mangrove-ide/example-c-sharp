namespace Crm.Shared;

public sealed record Customer(long Id, string FirstName, string LastName, string Email, string CreatedAt);

public sealed record NewCustomer(string FirstName, string LastName, string Email);
