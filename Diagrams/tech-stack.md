# Tech Stack

This document lists the technologies, libraries and infrastructure used to build and run the
Pension Platform. Each entry has a short description of what it is and what we use it for.
For the architecture itself, see the container diagrams in [`plantuml/`](./plantuml) and
[`structurizr/`](./structurizr).

---

## 1. Technologies

| Technology | Description | Where we use it |
| --- | --- | --- |
| **.NET** | Microsoft's cross-platform runtime and SDK for building services, background jobs and tools. | Runtime for every backend component we build. |
| **C#** | The main language of the .NET ecosystem: statically typed, object-oriented, with strong async support. | Language for all backend code. |
| **ASP.NET Core** | The .NET framework for building HTTP APIs and web applications. Provides routing, dependency injection, configuration, authentication and middleware. | Pension API, Admin API and the Procentia Facade API. |
| **PostgreSQL** | Open-source relational database, available as a managed service on Azure (Azure Database for PostgreSQL). | Candidate engine for the Operational Database. |
| **Microsoft SQL Server** | Microsoft's relational database, available as a managed service on Azure (Azure SQL). | The existing Procentia database (read by the ETL Sync Engine), and a candidate engine for the Operational Database. |
| **React** | JavaScript library for building browser user interfaces. | Pension UI and Admin UI. |

> The engine for the **Operational Database** (PostgreSQL or SQL Server) is still to be decided.

---

## 2. Libraries

| Library | Description | Where we use it |
| --- | --- | --- |
| **Wolverine** | .NET library for in-process messaging (mediator), asynchronous messaging over brokers such as Azure Service Bus, and a durable transactional inbox/outbox backed by PostgreSQL or SQL Server. Handles retries, error queues and scheduled messages. | Command handling inside the APIs. In Option B it publishes commands to the Command Bus and runs them in the Command Processor. In Option A its outbox can provide durable writes and retries. |

---

## 3. Infrastructure

| Tool / concern | Description | Where we use it |
| --- | --- | --- |
| **Containers** | Packaging an application with its runtime and dependencies into a portable, immutable image. | Every deployable component (APIs, Command Processor, background services) ships as a container image. |
| **Docker** | Tooling to build, run and compose containers. Docker Compose starts several containers together from one file. | Building images, and running services and their dependencies locally (see [`docker-compose.yml`](./docker-compose.yml)). |
| **WSL 2** (Windows Subsystem for Linux) | Runs a real Linux kernel on Windows. Lets developers run Docker Engine inside WSL 2 without Docker Desktop. | Local development on Windows machines (see the [README](./README.md#running-without-docker-desktop-wsl-2)). |
| **Terraform** | Infrastructure as Code tool. Azure resources are described in declarative files, reviewed like code and applied repeatably to each environment. | Provisioning all Azure resources (databases, Service Bus, container hosting, identities, networking). |
| **Azure Service Bus** | Azure's managed message broker, with queues, topics, dead-letter queues and retries. | The Command Bus in Option B (async writes). |
| **Microsoft Entra ID** | Microsoft's cloud identity provider (formerly Azure Active Directory). | Authentication for admin users today; pension users are planned to migrate to it from Procentia. Managed identities for service-to-service access. |

---

## 4. Source Control and Deployment

| Platform | Description | Where we use it |
| --- | --- | --- |
| **GitHub** | Hosted Git platform for repositories, branches and pull requests. | All source code, Terraform and documentation repositories, and code review through pull requests. |
| **Microsoft Azure** | Microsoft's cloud platform. | Target environment for every deployed component, plus managed services: databases, Service Bus, Entra ID and container hosting. |
