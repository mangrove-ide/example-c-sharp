namespace Crm.Api;

public static class CrmConfig
{
    public static string DatabasePath =>
        Environment.GetEnvironmentVariable("DATABASE_PATH") ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "data", "crm.sqlite");

    public static int Port => int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var port) ? port : 4000;
}
