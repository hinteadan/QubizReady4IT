# KnowledgeGram Azure target deployment

Two PNG diagrams map the C4 container alternatives to Azure services. The existing Structurizr
workspace remains unchanged and continues to describe its current/abstract container model.

- [Synchronous Azure architecture](./knowledgegram-azure-sync.png)
- [Asynchronous Azure architecture](./knowledgegram-azure-async.png)

## Confirmed service mapping

| Logical responsibility | Azure target |
| --- | --- |
| Public entry point | Azure Front Door, ahead of App Service; tier and WAF unspecified |
| KnowledgeGram Web App | Azure App Service |
| Content Database | Azure SQL Database (Microsoft SQL engine) |
| Uploaded/imported files | Azure Blob Storage in an Azure Storage account |
| Content Notification Function | Its own Azure Function App in both alternatives |
| Learning Integration Function | Its own Azure Function App, async only |
| Notification Processor Function | Its own Azure Function App, async only |
| Learning Integration Bus | Separate Azure Service Bus namespace with one queue, async only |
| Notification Bus | Separate Azure Service Bus namespace with one queue, async only |
| Shared user identity provider | One Microsoft Entra ID for clients and administrators |
| Notification provider | SendGrid |
| External knowledge provider | Microsoft Learn, subject to confirmation of API/content-use contract |
| Application/provider secrets | Azure Key Vault, shared application support |
| Application telemetry | Application Insights, shared application support |

The shared support band denotes dependencies of App Service and every Function App. Individual
secret-access and telemetry arrows are omitted for readability. This is not a network or security
deployment blueprint: managed identities, deployment regions, private endpoints, and VNet topology
are not depicted. Runtime authentication/authorization and Key Vault access still need to be
configured; the absence of managed identity icons is not an instruction to leave services unsecured.
App Service/Functions plans, Function host storage, and other hosting prerequisites are not
enumerated; select these when preparing infrastructure deployment.

## Integration paths

The synchronous diagram shows direct Web App access to Microsoft Learn, and direct SendGrid calls
from the Web App and Content Notification Function.

The asynchronous diagram has two independent messaging paths:

1. Web App -> Learning Service Bus queue -> Learning Integration Function App -> Microsoft Learn.
   Results return to the Web App for persistence in Azure SQL Database or Blob Storage.
   There are no direct learning-function storage writes.
2. Web App and Content Notification Function App -> Notification Service Bus queue ->
   Notification Processor Function App -> SendGrid.

The Content Notification Function App continues to obtain preferences and matching content via
the Web App in both alternatives. Its trigger/schedule is not specified.
Queue acceptance, content persistence, provider acceptance, and recipient delivery are distinct
outcomes. Retries, idempotency, status tracking, and dead-letter operations remain implementation
requirements, not guarantees of these pictures.

## Editable sources and icons

The self-contained SVG files are editable companions to the PNGs. Generate SVG/HTML sources with
`node generate.mjs` from this folder, then render each HTML at a 3000 x 1900 viewport to reproduce
the PNG. The generator uses Node.js built-ins only.

Icons are unmodified official assets from:

- [Azure Architecture Center icon collection](https://learn.microsoft.com/azure/architecture/icons/)
  (Azure Public Service Icons V24, including SendGrid and Learn).
- [Microsoft Entra architecture icons](https://learn.microsoft.com/entra/architecture/architecture-icons)
  (October 2023 color Entra ID icon).

Microsoft permits these icons in architecture diagrams, training materials, and documentation;
Microsoft reserves other rights. The included assets are used for that permitted purpose.
Icon colors and proportions are preserved. Background cards distinguish external services,
message buses, and message-processing Function Apps; they do not alter the product icons.
