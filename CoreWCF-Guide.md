## Введение: что за проект

Это минимальный пример, показывающий, **как встроить CoreWCF (SOAP‑сервис) внутрь ASP.NET Core приложения на .NET 10**.

- Веб‑часть: обычный ASP.NET Core Web API (контроллер `WeatherForecastController`).
- SOAP‑часть: сервис `IGreeterService` / `GreeterService`, хостится через **CoreWCF**.

Цель гайда — не только показать, *что* нужно сделать, но и объяснить **почему** каждый шаг важен.

---

## Шаг 1. Что такое CoreWCF и чем он отличается от WCF

- **WCF (Windows Communication Foundation)** — классическая технология .NET Framework для SOAP‑сервисов, тесно связана с Windows/IIS.
- **CoreWCF** — порт *серверной части* WCF на современный .NET (Core / .NET 8/9/10 и т.д.).
  - Позволяет переносить/писать SOAP‑сервисы на кроссплатформенном .NET.
  - Встраивается в **ASP.NET Core pipeline** (как middleware).

**Ключевая идея:** ASP.NET Core отвечает за HTTP‑хостинг, а CoreWCF внутри обрабатывает SOAP‑сообщения так же, как старый WCF.

### How to explain this in the guide
Сформулируйте, что CoreWCF — это “WCF в мире .NET Core/ASP.NET Core”, а не отдельный сервер. Он живёт внутри того же приложения, что и ваши Web API контроллеры.

---

## Шаг 2. Требуемые NuGet‑пакеты

Минимальный набор для HTTP + WSDL:

- **`CoreWCF.Http` (1.8.0)** — HTTP‑интеграция CoreWCF с ASP.NET Core.
- **`CoreWCF.Primitives` (1.8.0)** — базовые типы и атрибуты `[ServiceContract]`, `[OperationContract]` и т.д.

Фрагмент `WebAppCoreWCF.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.2" />
  <PackageReference Include="CoreWCF.Http" Version="1.8.0" />
  <PackageReference Include="CoreWCF.Primitives" Version="1.8.0" />
</ItemGroup>
```

### Команды установки

```bash
dotnet add package CoreWCF.Http --version 1.8.0
dotnet add package CoreWCF.Primitives --version 1.8.0
```

### Почему это важно
- Без `CoreWCF.Http` у приложения не будет middleware, которое умеет понимать SOAP.
- Без `CoreWCF.Primitives` вы не сможете использовать привычные атрибуты WCF (`[ServiceContract]`, `[OperationContract]`, `[DataContract]` и т.п.).

### How to explain this in the guide
Опишите пакеты как “две половинки”: одна даёт **интеграцию с ASP.NET Core**, другая — **контрактный уровень WCF**, привычный тем, кто писал WCF‑сервисы на .NET Framework.

---

## Шаг 3. Описание контракта и реализация сервиса

### 3.1 Контракт (`Soap/IGreeterService.cs`)

```csharp
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
```

**Зачем нужны атрибуты:**
- `[ServiceContract]` — помечает интерфейс как SOAP‑контракт, его операции появятся в WSDL.
- `[OperationContract]` — конкретный метод, который будет доступен клиентам.
- `[DataContract]` / `[DataMember]` — описывают структуру сложных типов, попадающих в SOAP‑сообщения.
- `Namespace = "..."` — задаёт стабильное пространство имён в XML, чтобы клиенты могли однозначно ориентироваться.

### 3.2 Реализация (`Soap/GreeterService.cs`)

```csharp
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
```

**Почему реализация простая:**
- Вся “магия” CoreWCF находится на уровне контракта и хостинга.
- Сам класс — обычный C#‑сервис, который вы можете тестировать отдельно от SOAP.

### How to explain this in the guide
Подчеркните разделение: **контракт** описывает протокол (то, что видит клиент), **реализация** — бизнес‑логику. Это тот же подход, что в старом WCF, только теперь он живёт внутри ASP.NET Core.

---

## Шаг 4. Настройка Program.cs и ASP.NET Core pipeline

Полный `Program.cs` из проекта:

```csharp
using CoreWCF;
using CoreWCF.Channels;
using CoreWCF.Configuration;
using CoreWCF.Description;
using WebAppCoreWCF.Soap;

namespace WebAppCoreWCF;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // CoreWCF uses some legacy APIs which require synchronous IO.
        // Without this you will typically get runtime errors when calling the SOAP endpoint.
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.AllowSynchronousIO = true;
        });

        // Add services to the container.
        builder.Services.AddControllers();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        // CoreWCF: registers the service model runtime + metadata (WSDL) support.
        builder.Services.AddServiceModelServices()
            .AddServiceModelMetadata();

        // Makes generated WSDL/help-page use host/port from incoming request headers.
        // Important when behind reverse proxies / containers.
        builder.Services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        // For simplest local testing, keep HTTP enabled (no forced redirect to HTTPS).
        // If you want HTTPS-only SOAP, configure a Transport-secured binding and enable redirection.
        app.UseAuthorization();

        // Enable WSDL publishing.
        var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
        serviceMetadataBehavior.HttpGetEnabled = true;
        serviceMetadataBehavior.HttpsGetEnabled = true;

        // Host SOAP endpoints in ASP.NET Core pipeline.
        app.UseServiceModel(serviceBuilder =>
        {
            var binding = new BasicHttpBinding();

            serviceBuilder
                .AddService<GreeterService>(serviceOptions =>
                {
                    serviceOptions.DebugBehavior.IncludeExceptionDetailInFaults = app.Environment.IsDevelopment();
                })
                .AddServiceEndpoint<GreeterService, IGreeterService>(
                    binding,
                    "/soap/GreeterService.svc");
        });

        app.MapControllers();
        app.Run();
    }
}
```

### Ключевые моменты по шагам

1. **Synchronous IO (`AllowSynchronousIO`)**
   - Некоторые внутренние части CoreWCF используют синхронный IO.
   - В ASP.NET Core синхронный IO по умолчанию запрещён → без этой настройки вы получите runtime‑ошибки при реальном вызове SOAP.

2. **`AddServiceModelServices().AddServiceModelMetadata()`**
   - Регистрирует в DI всё, что нужно CoreWCF для обработки SOAP‑сообщений.
   - `AddServiceModelMetadata()` включает поддержку **WSDL/metadata** и HTML‑страницы помощи (как в старом WCF).

3. **`UseRequestHeadersForMetadataAddressBehavior`**
   - Делает так, чтобы в WSDL использовался **хост/порт из реального запроса**, а не `localhost`.
   - Особенно важно, если приложение будет за reverse proxy (`nginx`, `IIS`, Kubernetes Ingress и т.п.).

4. **`ServiceMetadataBehavior.HttpGetEnabled = true`**
   - Включает публикацию WSDL по `?wsdl`.
   - Без этого WSDL не будет доступен даже при работающем сервисе.

5. **`UseServiceModel` и объявление endpoint’а**
   - `AddService<GreeterService>` — регистрирует класс как CoreWCF‑сервис.
   - `AddServiceEndpoint<GreeterService, IGreeterService>(binding, "/soap/GreeterService.svc")` —
     - биндинг: `BasicHttpBinding` (классический SOAP 1.1 без шифрования);
     - адрес: `http://{host}:{port}/soap/GreeterService.svc`.

### How to explain this in the guide
Разбейте кусок `Program.cs` на логические блоки: “настройка Kestrel”, “регистрация CoreWCF”, “включение metadata”, “описание endpoint’а”. Каждому блоку дайте по 1–2 предложения “зачем это нужно”.

---

## Шаг 5. Как запустить и проверить сервис

### 5.1 Запуск

```bash
dotnet run
```

По умолчанию в `launchSettings.json`:

- HTTP: `http://localhost:5104`
- HTTPS: `https://localhost:7280`

Мы используем **HTTP** для простоты тестов.

### 5.2 Проверка WSDL

Откройте в браузере:

```text
http://localhost:5104/soap/GreeterService.svc?wsdl
```

Если WSDL отображается — значит:

- CoreWCF настроен,
- metadata включены,
- endpoint живой.

### 5.3 Вызов метода `SayHello` через PowerShell

1. Подготовьте SOAP‑Envelope:

```powershell
$soap = @'
<s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
  <s:Body>
    <SayHello xmlns="urn:webappcorewcf:greeter">
      <name>World</name>
    </SayHello>
  </s:Body>
</s:Envelope>
'@
```

2. Отправьте запрос:

```powershell
Invoke-WebRequest `
  -Uri "http://localhost:5104/soap/GreeterService.svc" `
  -Method Post `
  -ContentType "text/xml; charset=utf-8" `
  -Headers @{ SOAPAction = "urn:webappcorewcf:greeter/IGreeterService/SayHello" } `
  -Body $soap `
| Select-Object -ExpandProperty Content
```

3. В ответе увидите XML с `Hello, World!`:

```xml
<s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
  <s:Body>
    <SayHelloResponse xmlns="urn:webappcorewcf:greeter">
      <SayHelloResult>Hello, World!</SayHelloResult>
    </SayHelloResponse>
  </s:Body>
</s:Envelope>
```

4. Чтобы вывести только текст результата:

```powershell
$xml = [xml](Invoke-WebRequest -Uri "http://localhost:5104/soap/GreeterService.svc" `
  -Method Post `
  -ContentType "text/xml; charset=utf-8" `
  -Headers @{ SOAPAction = "urn:webappcorewcf:greeter/IGreeterService/SayHello" } `
  -Body $soap).Content

$xml.Envelope.Body.SayHelloResponse.SayHelloResult
```

### How to explain this in the guide
Объясните два уровня проверки: (1) **WSDL отдаётся** — инфраструктура работает; (2) **операция реально вызывается** — контракт/биндинг/endpoint настроены правильно.

---

## Шаг 6. Минимальный .NET‑клиент, который передаёт имя `Andrei`

Иногда удобнее не собирать SOAP‑запрос руками, а написать **обычный .NET‑клиент**, который вызывает метод как обычный C#‑метод.

Ниже пример минимального консольного клиента на .NET 10, который отправит на сервис имя `Andrei` и выведет результат.

### 6.1 Создаём консольный проект клиента

В отдельной папке (на уровень выше вашего WebAppCoreWCF) выполните:

```bash
dotnet new console -n GreeterClient10
cd GreeterClient10
```

Этот клиент будет знать только WSDL, поэтому нам не нужно шарить C#‑интерфейс `IGreeterService` как библиотеку.

### 6.2 Добавляем ссылку на WCF‑клиентский стек

Клиент для SOAP удобно писать через **WCF‑клиентские пакеты** (они умеют работать с CoreWCF по HTTP).

```bash
dotnet add package System.ServiceModel.Http --version 8.0.0
```

> Версия можно обновить до актуальной, когда вы будете писать реальный клиент; здесь фиксируем конкретную, чтобы пример был воспроизводим.

### 6.3 Полный код клиента (`Program.cs`)

Замените содержимое `GreeterClient10/Program.cs`:

```csharp
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

// ВНИМАНИЕ: адрес и namespace должны совпадать с вашим CoreWCF‑сервисом

// Контракт клиента (сигнатуры совпадают с IGreeterService на сервере)
[ServiceContract(Namespace = "urn:webappcorewcf:greeter")]
public interface IGreeterServiceClient
{
    [OperationContract]
    string SayHello(string name);
}

public class Program
{
    public static void Main(string[] args)
    {
        // Адрес CoreWCF‑сервиса (HTTP)
        var address = new EndpointAddress("http://localhost:5104/soap/GreeterService.svc");

        // Такой же базовый биндинг, как на сервере (BasicHttpBinding без безопасности)
        Binding binding = new BasicHttpBinding();

        // Создаём канал-клиент
        var factory = new ChannelFactory<IGreeterServiceClient>(binding, address);
        IGreeterServiceClient client = factory.CreateChannel();

        const string name = "Andrei";

        Console.WriteLine($"Calling SayHello(\"{name}\") ...");
        var result = client.SayHello(name);

        Console.WriteLine("Response from service:");
        Console.WriteLine(result);

        ((IClientChannel)client).Close();
        factory.Close();
    }
}
```

### 6.4 Запуск клиента

1. Убедитесь, что **ваш WebAppCoreWCF уже запущен**:

   ```bash
   dotnet run --project ../WebAppCoreWCF/WebAppCoreWCF.csproj
   ```

2. В другой консоли запустите клиента:

   ```bash
   cd GreeterClient10
   dotnet run
   ```

3. В выводе клиента вы увидите:

   ```text
   Calling SayHello("Andrei") ...
   Response from service:
   Hello, Andrei!
   ```

### Как это работает внутри

- Атрибуты `[ServiceContract]` / `[OperationContract]` на клиенте **должны совпадать** по `Namespace` и именам операций с серверным контрактом.
- `BasicHttpBinding` на клиенте совпадает с биндингом, который вы настроили в CoreWCF.
- Вызов `client.SayHello("Andrei")` автоматически:
  - сериализует параметр `name` в SOAP‑XML (`<name>Andrei</name>`),
  - отправит его на CoreWCF‑endpoint,
  - десериализует ответ и вернёт обычную строку в C#.

### How to explain this in the guide
Покажите, что клиент выглядит как “обычный интерфейс + вызов метода”, а вся SOAP‑магия спрятана в WCF‑клиентских библиотеках. Отдельно подчеркните, что имя `Andrei` — это обычный аргумент метода, который попадает в `<name>Andrei</name>` на wire‑уровне.

---

## Шаг 7. Типичные ошибки и как их чинить

### 7.1 404 на `/soap/GreeterService.svc` или `?wsdl`

- Проверьте путь в `AddServiceEndpoint(..., "/soap/GreeterService.svc")`.
- Убедитесь, что `UseServiceModel` реально вызывается *до* `app.Run()`.

### 7.2 WSDL не открывается, но endpoint отвечает

- Скорее всего не включён:

```csharp
var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
serviceMetadataBehavior.HttpGetEnabled = true;
```

### 7.3 Ошибки про synchronous IO

- Проверьте, что есть:

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.AllowSynchronousIO = true;
});
```

### 7.4 Ошибки NuGet/SSL при восстановлении пакетов

- Это не проблема CoreWCF, а сетевой/корпоративной инфраструктуры.
- Решение: настроить прокси/сертификаты/NuGet‑источники согласно политике вашей компании.

### How to explain this in the guide
В конце полезно дать читателю раздел “если что‑то пошло не так”: типичные симптомы + 1–2 строчки, куда смотреть. Это сильно экономит время тем, кто повторяет пример.

---

## Что можно сделать дальше

- Добавить **HTTPS‑endpoint** (TLS), чтобы SOAP ходил по защищённому каналу.
- Подключить **клиент WCF** (на старом .NET Framework или .NET 8/9) по сгенерированному WSDL.
- Расширить контракт до “реального” бизнес‑сервиса и показать версионирование.

Этот демонстрационный проект и этот файл можно использовать как “скелет”.

