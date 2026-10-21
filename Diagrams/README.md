# KnowledgeGram C4 Diagrams

C4 model diagrams for KnowledgeGram, maintained in **Structurizr DSL** format using
[Structurizr](https://docs.structurizr.com/local).

The system context shows clients and administrators using KnowledgeGram and authenticating through
the same Microsoft Entra ID provider. It also shows planned integrations with a notification service
and Microsoft Learn. The container view reflects the current implementation: a Blazor Server web
application, SQLite content database, and local file storage. These external integrations are marked
as planned because they are not wired into the application yet.

The Level 2 container view also includes a planned **Content Notification Function** (Azure
Functions). It retrieves user preferences and checks for matching new content via the Web App,
then requests delivery through the Notification Service when matches are found. Preference
management and the Web App interface used by this function are planned, not existing functionality.

### Async container alternative

The same workspace provides two Level 2 views: **Containers** (synchronous, listed first for the
default context drill-down) and **ContainersAsync** (select separately from the diagram list).
The context and Level 3 component views remain shared; there is no second Structurizr instance.

The async alternative replaces the direct notification and Microsoft Learn connections with:

- **Notification Bus**: the Web App and Content Notification Function enqueue notification
  requests. A **Notification Processor Function** consumes them and invokes the Notification Service.
- **Learning Integration Bus**: the Web App enqueues learning content requests. A **Learning
  Integration Function** consumes them, retrieves content from Microsoft Learn, and submits the
  results to the Web App. The Web App persists results in the Content Database or Local File Storage;
  the function does not access either store directly.

Both buses, their processing functions, and their connections are planned. They appear only in
the async container view; direct calls to the Notification Service and Microsoft Learn appear
only in the synchronous container view. The existing Web App storage connections are shared.

The Level 3 **WebAppComponents** view expands the Web App into its current runtime components:
the Home page; Feed, Publishing, and Engagement services; and their repository, file storage,
user session, content kind, subject catalog, and clock dependencies. Interfaces and their concrete
implementations are shown together, rather than as separate components. The current user session
only stores a chosen username; Entra sign-in and administrator-specific functionality are not
implemented. Planned external integrations remain at the context and container levels.

## Architecture comparisons

Each folder contains a sync-vs-async overview and detailed comparisons for reliability, security,
cost optimization, operational excellence, and performance efficiency:

- [Microsoft Learn integration](./comparisons/ms-learn/sync-async-comparison.md)
- [Notification delivery](./comparisons/notifications/sync-async-comparison.md)

## Azure target deployment PNGs

These service-level diagrams are separate from the C4 workspace:

- [Synchronous Azure architecture](./azure/knowledgegram-azure-sync.png)
- [Asynchronous Azure architecture](./azure/knowledgegram-azure-async.png)
- [Service mapping, editable sources, and icon attribution](./azure/README.md)

## Prerequisites

- A Docker engine running locally. Either:
  - **Docker Desktop**, or
  - **Docker Engine (CE) inside WSL 2** — free, no license required. See
    [Running without Docker Desktop (WSL 2)](#running-without-docker-desktop-wsl-2).

## Running

### Quick start (Docker Desktop or WSL 2)

1. Open a terminal in this folder.
2. Start the Structurizr container:

   ```powershell
   docker compose up -d
   ```

3. Wait ~20–30 seconds for Java to boot, then open **http://localhost:8091** in your browser.
4. After editing `structurizr/workspace/workspace.dsl`, restart the container to regenerate the
   workspace, then refresh the page:

   ```powershell
   docker compose restart structurizr
   ```

To stop:

```powershell
docker compose down
```

### Running from PowerShell via WSL

If Docker runs inside WSL 2, wrap commands with `wsl`:

```powershell
# Start
wsl -d Ubuntu-24.04 -- bash -lc "cd '/mnt/c/Users/cosmin.vana/source/repos/ready4it/QubizReady4IT/Diagrams' && docker compose up -d structurizr"

# Stop
wsl -d Ubuntu-24.04 -- bash -lc "cd '/mnt/c/Users/cosmin.vana/source/repos/ready4it/QubizReady4IT/Diagrams' && docker compose down"
```

WSL 2 forwards container ports to Windows automatically, so http://localhost:8091 works from your
Windows browser.

> Note: port 8091 (rather than the default 8080) is used because other, unrelated Structurizr
> containers are already running on this machine for a different project.

## Running without Docker Desktop (WSL 2)

Docker Desktop requires a paid license for larger organizations, but the underlying
**Docker Engine (CE)** is free (Apache 2.0) and runs inside WSL 2 without any license.
The `docker-compose.yml` in this repo needs no changes.

### One-time setup

1. Ensure your distro is on WSL 2 (Docker needs a real Linux kernel):

   ```powershell
   wsl -l -v
   # If your distro shows VERSION 1, convert it:
   wsl --set-version Ubuntu-24.04 2
   ```

2. Install Docker Engine + the Compose plugin inside the distro:

   ```bash
   # Run inside WSL (Ubuntu)
   curl -fsSL https://get.docker.com | sudo sh
   sudo usermod -aG docker $USER   # run docker without sudo
   ```

   Then close and reopen the WSL terminal (or `wsl -t Ubuntu-24.04` from PowerShell) so the
   group change takes effect.

3. If the daemon isn't managed by systemd, start it with `sudo service docker start`.
   To make it auto-start, enable systemd by adding this to `/etc/wsl.conf` and running
   `wsl --shutdown`:

   ```ini
   [boot]
   systemd=true
   ```

### Day-to-day usage

Run compose from inside WSL. From PowerShell you can one-line it:

```powershell
# Start
wsl -d Ubuntu-24.04 -- bash -lc "cd '/mnt/c/Users/cosmin.vana/source/repos/ready4it/QubizReady4IT/Diagrams' && docker compose up -d structurizr"

# Stop
wsl -d Ubuntu-24.04 -- bash -lc "cd '/mnt/c/Users/cosmin.vana/source/repos/ready4it/QubizReady4IT/Diagrams' && docker compose down"
```

WSL 2 forwards the container ports to Windows automatically, so the services are still
reachable from your Windows browser at `localhost:8091`.

> The Docker install script prints a "We recommend Docker Desktop" notice — that's just
> advisory; the standalone Engine is the intended, license-free alternative.

## Folder structure

```
structurizr/
  workspace/workspace.dsl    # KnowledgeGram model + views (served on :8091)
  workspace/workspace.json   # Generated by Structurizr — do NOT hand-edit
  workspace/images/          # Generated thumbnails — do NOT hand-edit
docker-compose.yml           # Single Structurizr container
```

## Current implementation and planned integrations

Colors identify architectural roles consistently across views:

- **Grey** = all external systems (Microsoft Entra ID, Notification Service, and Microsoft Learn).
- **White** = Content Notification Function.
- **Blue** = the two message buses in the async container alternative.
- **Purple** = the functions processing messages from those buses.
- Users retain their dark blue styling.
- **Grey dashed relationships** = planned integrations. Planned status is also retained in
  element tags and descriptions, rather than represented by an additional element color.

## Adding more diagrams

Add new `component`/`dynamic`/`deployment` views inside the `views { ... }` block of
`structurizr/workspace/workspace.dsl` (components typically require adding `component` elements
under the relevant container in the `model` block first).

## Start / stop the container

Previously these were run with `docker compose up` / `docker compose down` under Docker Desktop.
With Docker Engine inside WSL 2 (see [Running without Docker Desktop (WSL 2)](#running-without-docker-desktop-wsl-2)),
the same commands are wrapped in `wsl`:

```powershell
# Start the Structurizr container in the background
wsl -d Ubuntu-24.04 -- bash -lc "cd '/mnt/c/Users/cosmin.vana/source/repos/ready4it/QubizReady4IT/Diagrams' && docker compose up -d structurizr"

# Stop and remove it
wsl -d Ubuntu-24.04 -- bash -lc "cd '/mnt/c/Users/cosmin.vana/source/repos/ready4it/QubizReady4IT/Diagrams' && docker compose down"

# Reload after editing the DSL
wsl -d Ubuntu-24.04 -- bash -lc "cd '/mnt/c/Users/cosmin.vana/source/repos/ready4it/QubizReady4IT/Diagrams' && docker compose restart structurizr"

# Check status
wsl -d Ubuntu-24.04 -- bash -lc "cd '/mnt/c/Users/cosmin.vana/source/repos/ready4it/QubizReady4IT/Diagrams' && docker compose ps"
```

After `up -d`, give Structurizr ~20-30 seconds to boot (it's a Java app) before opening
http://localhost:8091.

> Tip: or open an **Ubuntu** terminal, `cd "/mnt/c/Users/cosmin.vana/source/repos/ready4it/QubizReady4IT/Diagrams"` once,
> then just use the plain `docker compose up -d` / `docker compose down` as before.
