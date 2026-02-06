## ASP.NET Core + CoreWCF demo

Этот репозиторий содержит минимальный пример **ASP.NET Core (.NET 10)** приложения, внутри которого поднят **SOAP‑сервис на CoreWCF**.

- Web API контроллер: `WeatherForecastController`
- SOAP‑сервис: `IGreeterService` / `GreeterService`

Подробный пошаговый **гайд с объяснениями, как всё устроено и как это повторить**, лежит в отдельном файле:

- см. файл **`CoreWCF-Guide.md`**

В нём описано:
- что такое CoreWCF и чем он отличается от старого WCF;
- какие NuGet‑пакеты нужны;
- какие классы/файлы создать (контракт, реализация);
- как именно настроен `Program.cs` и pipeline ASP.NET Core;
- как тестировать SOAP‑методы через PowerShell и другие клиенты.

Кратко, как быстро проверить сервис:

1. Запустите приложение:
   ```bash
   dotnet run
   ```
2. Откройте WSDL в браузере:
   ```text
   http://localhost:5104/soap/GreeterService.svc?wsdl
   ```
3. Вызовите метод `SayHello` через PowerShell (пример и разбор ответа — в `CoreWCF-Guide.md`).

