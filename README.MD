Базовый адрес приложения (по логам):

SOAP endpoint:
```
http://localhost:ХХХХ/soap/GreeterService.svc
```
Когда вы отправите SOAP‑запрос к SayHello, в ответном XML вы увидите Hello, <имя>!.

Быстрый вызов SayHello через PowerShell

Откройте новый PowerShell и выполните (при запущенном dotnet run):
```
$soap = @'
	<s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">  
		<s:Body>    
			<SayHello xmlns="urn:webappcorewcf:greeter">      
				<name>World</name>    
			</SayHello>  
		</s:Body>
	</s:Envelope>
'@

Invoke-WebRequest `
	-Uri "http://localhost:5104/soap/GreeterService.svc" `  
	-Method Post `  -ContentType "text/xml; charset=utf-8" `  
	-Headers @{ SOAPAction = "urn:webappcorewcf:greeter/IGreeterService/SayHello" } `  
	-Body $soap | Select-Object -ExpandProperty Content
```

В результате вы получите XML, внутри которого будет что‑то вроде:
```
<SayHelloResponse ...>  <SayHelloResult>Hello, World!</SayHelloResult></SayHelloResponse>
```
