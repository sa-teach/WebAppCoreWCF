using CoreWCF;
using System.Runtime.Serialization;

namespace WebAppCoreWCF.Soap;

[ServiceContract(Namespace = "urn:webappcorewcf:greeter")]
public interface IGreeterService
{
    [OperationContract]
    string SayHello(string name);

    [OperationContract]
    ServerInfo GetServerInfo();
}

[DataContract(Namespace = "urn:webappcorewcf:greeter:types")]
public class ServerInfo
{
    [DataMember(Order = 1)]
    public string MachineName { get; set; } = "";

    [DataMember(Order = 2)]
    public string OsVersion { get; set; } = "";

    [DataMember(Order = 3)]
    public DateTimeOffset UtcNow { get; set; }
}

