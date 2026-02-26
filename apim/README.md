# APIM Petstore Web (.NET 10)

Aplicación web ASP.NET Core MVC en `.NET 10` que:

1. Se conecta al **Azure API Management** (plano de administración) para buscar la API con nombre **Swagger Petstore**.
2. Consume por gateway de APIM el endpoint de mascotas filtrando por estado: `available`, `pending`, `sold`.

## Configuración

Completa la sección `Apim` en `appsettings.json` (o mejor, User Secrets / variables de entorno):

```json
"Apim": {
  "SubscriptionId": "<tu-subscription-id>",
  "ResourceGroupName": "<tu-resource-group>",
  "ServiceName": "<tu-servicio-apim>",
  "ApiDisplayName": "Swagger Petstore",
  "GatewayBaseUrl": "",
  "SubscriptionKey": "<opcional-si-tu-api-lo-requiere>"
}
```

- `GatewayBaseUrl` es opcional; por defecto usa `https://<ServiceName>.azure-api.net`.
- La app usa `DefaultAzureCredential` para consultar Azure Management API.

## Requisitos de autenticación local

Inicia sesión con Azure CLI (o configura otra credencial compatible):

```bash
az login
```

## Ejecutar

```bash
dotnet run --project apim/apim.csproj
```

Abre la URL local de la aplicación y usa el selector de estado para listar mascotas.
