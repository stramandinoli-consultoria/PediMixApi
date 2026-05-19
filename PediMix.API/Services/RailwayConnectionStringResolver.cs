using System.Text.RegularExpressions;

namespace PediMix.API.Services;

public static class RailwayConnectionStringResolver
{
    public static string Resolve(IConfiguration configuration)
    {
        var url = FirstNonEmpty(
            Environment.GetEnvironmentVariable("MYSQL_URL"),
            Environment.GetEnvironmentVariable("URL_MYSQL"),
            Environment.GetEnvironmentVariable("URL_PUBLICA_DO_MYSQL"),
            Environment.GetEnvironmentVariable("URL_PÚBLICA_DO_MYSQL"));

        if (!string.IsNullOrWhiteSpace(url))
        {
            return ToEfConnectionString(url);
        }

        var host = FirstNonEmpty(
            Environment.GetEnvironmentVariable("MYSQLHOST"),
            Environment.GetEnvironmentVariable("MYSQL_HOST"));

        var port = FirstNonEmpty(
            Environment.GetEnvironmentVariable("MYSQLPORT"),
            Environment.GetEnvironmentVariable("MYSQL_PORT"),
            "3306");

        var database = FirstNonEmpty(
            Environment.GetEnvironmentVariable("MYSQLDATABASE"),
            Environment.GetEnvironmentVariable("MYSQL_DATABASE"),
            Environment.GetEnvironmentVariable("BANCO_DE_DADOS_MYSQL"),
            Environment.GetEnvironmentVariable("BANCO_DE_DADOS__MYSQL"),
            "pedmix_db");

        var user = FirstNonEmpty(
            Environment.GetEnvironmentVariable("MYSQLUSER"),
            Environment.GetEnvironmentVariable("MYSQL_USER"),
            Environment.GetEnvironmentVariable("USUARIO_MYSQL"),
            Environment.GetEnvironmentVariable("USUARIO_DO_MYSQL"),
            Environment.GetEnvironmentVariable("USUÁRIO_MYSQL"),
            "root");

        var password = FirstNonEmpty(
            Environment.GetEnvironmentVariable("MYSQLPASSWORD"),
            Environment.GetEnvironmentVariable("MYSQL_PASSWORD"),
            Environment.GetEnvironmentVariable("SENHA_DO_MYSQL"),
            Environment.GetEnvironmentVariable("SENHA_DO__MYSQL"),
            Environment.GetEnvironmentVariable("SENHA_ROOT_DO_MYSQL"));

        if (!string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(password))
        {
            return $"Server={host};Port={port};Database={database};User={user};Password={password};SslMode=Required;";
        }

        return configuration.GetConnectionString("DefaultConnection")
               ?? throw new InvalidOperationException("Connection string not configured.");
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
    }

    private static string ToEfConnectionString(string rawUrl)
    {
        var sanitized = rawUrl.Trim();

        if (!sanitized.StartsWith("mysql://", StringComparison.OrdinalIgnoreCase))
        {
            return sanitized;
        }

        var match = Regex.Match(
            sanitized,
            "^mysql://(?<user>[^:]+):(?<pwd>[^@]+)@(?<host>[^:/]+):(?<port>\\d+)/(?<db>[^?]+)",
            RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            throw new InvalidOperationException("Invalid MYSQL url format.");
        }

        var user = Uri.UnescapeDataString(match.Groups["user"].Value);
        var pwd = Uri.UnescapeDataString(match.Groups["pwd"].Value);
        var host = match.Groups["host"].Value;
        var port = match.Groups["port"].Value;
        var db = match.Groups["db"].Value;

        return $"Server={host};Port={port};Database={db};User={user};Password={pwd};SslMode=Required;";
    }
}
