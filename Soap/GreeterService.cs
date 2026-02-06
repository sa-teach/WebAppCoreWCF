namespace WebAppCoreWCF.Soap;

public sealed class GreeterService : IGreeterService
{
    public string SayHello(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "world";
        }

        return $"Hello, {name}!";
    }

    public ServerInfo GetServerInfo()
    {
        return new ServerInfo
        {
            MachineName = Environment.MachineName,
            OsVersion = Environment.OSVersion.VersionString,
            UtcNow = DateTimeOffset.UtcNow
        };
    }
}

