# KnowledgeGram: Synchronous vs Asynchronous Microsoft Learn Integration

This comparison uses the **Containers** and **ContainersAsync** views in the single
[Structurizr workspace](../../structurizr/workspace/workspace.dsl). It focuses on the integration
between KnowledgeGram Web App and Microsoft Learn, using the five pillars of the
[Azure Well-Architected Framework](https://learn.microsoft.com/azure/well-architected/).
Both integration alternatives are planned, not implemented application behavior.

## 1. Scope and assumptions

Microsoft Learn represents the external purchased knowledge solution in this model. Its actual
API, authentication, content-use rights, availability, and rate limits must be established before
implementation; the diagrams do not assert a specific Microsoft Learn API contract or purchase model.

| | Option A: synchronous | Option B: asynchronous |
| --- | --- | --- |
| Diagram key | `Containers` | `ContainersAsync` |
| Request path | Web App calls Microsoft Learn directly and waits for the retrieval outcome | Web App enqueues a request on the Learning Integration Bus |
| External access | Web App retrieves learning content | Learning Integration Function consumes the request and retrieves learning content |
| Result path | Web App receives the result and can persist it using its existing storage access | Function submits results to the Web App, which persists them in Content Database or Local File Storage |
| Processor storage access | No separate processor | Function has **no direct database or file storage access** |
| User-visible completion | Retrieval succeeds or fails during the operation; success must distinguish retrieval from any required persistence | Enqueue acknowledgement means accepted, **not retrieved or saved**; completion is reported later |
| Local content browsing | Uses existing Web App storage paths | Same local paths; imported content becomes available only after persistence |

This is **not** a comparison of synchronous reads versus asynchronous writes to Microsoft Learn.
The asynchronous operation is retrieval/import of external learning content, followed by local
persistence. It does not make all Web App operations asynchronous.

The async diagram also has a **separate Notification Bus and Notification Processor Function**.
The Web App and Content Notification Function publish notification requests there. That flow
is distinct from learning integration and is excluded from the primary comparison below.
Costs or operational estimates for the full async architecture must include both flows.

## 2. Tradeoffs by pillar

Review references: [Reliability checklist](https://learn.microsoft.com/en-us/azure/well-architected/reliability/checklist),
[Security checklist](https://learn.microsoft.com/en-us/azure/well-architected/security/checklist),
[Cost Optimization checklist](https://learn.microsoft.com/en-us/azure/well-architected/cost-optimization/checklist),
[Operational Excellence checklist](https://learn.microsoft.com/en-us/azure/well-architected/operational-excellence/checklist),
and [Performance Efficiency checklist](https://learn.microsoft.com/en-us/azure/well-architected/performance-efficiency/checklist).
The table below summarizes architectural tradeoffs, not a completed assessment of every checklist.
Reliability is assessed item by item against **RE:01 through RE:10** in the
[detailed reliability review](./reliability-comparison.md#2-reliability-checklist-review).

The markers show a **relative advantage or disadvantage for the dimension in that row**:
`++` = advantage, `--` = tradeoff/disadvantage, and `~` = shared or workload-dependent. They are
not points, pillar scores, or claims of WAF compliance. A marker does not mean the option is
universally better; read the condition in the cell.

| WAF pillar | Review dimension | Option A: synchronous | Option B: asynchronous |
| --- | --- | --- | --- |
| Reliability | Microsoft Learn outage | `--` A timeout or failure affects the current retrieval; the user may need to retry | `++` Durable accepted work can wait for recovery, within broker retention and capacity limits |
| Reliability | Broker, worker, and callback dependencies | `++` No broker, separate worker, or result callback in the learning path | `--` Adds failure modes; retrieval is not complete until the Web App callback persists the result |
| Reliability | Duplicate delivery and partial persistence | `~` Client retries can repeat work; database and file writes still need safe recovery | `--` Redelivery and callback/acknowledgement ambiguity add replay cases; persistence still needs idempotency |
| Reliability | Backlog and recovery limits | `++` No queued backlog to drain or expire; still needs bounded retries and dependency recovery | `--` Buffering is finite; retention, capacity, dead-letter handling, and backlog drain need explicit policies |
| Security | Service and trust-boundary surface | `++` Fewer service identities, endpoints, and durable message stores to secure | `--` Adds broker, worker, message, and callback boundaries to protect |
| Security | External credential isolation | `--` The Web App needs the external-access credentials | `++` External credentials can be limited to the learning function |
| Security | Deferred authorization and retained data | `++` No learning queue or dead-letter message retention | `--` Must protect queued data and status, preserve ownership context, and account for permission changes before processing |
| Security | Baseline controls | `~` Both need verified external API/authentication terms, user authorization, secret protection, and safe content validation | `~` Same baseline; the additional service boundaries do not secure themselves |
| Cost optimization | Incremental infrastructure and delivery complexity | `++` No learning broker or worker; generally simpler for short, low-volume retrieval | `--` Adds broker, function, status/deduplication, telemetry, and operating costs |
| Cost optimization | Burst capacity and cost | `--` Bursts may require more Web App capacity or cause throttling/rejection | `++` Can smooth finite bursts and pace workers; it does not remove downstream capacity or freshness needs |
| Cost optimization | External calls and persistence costs | `~` External service and local persistence costs remain; retries can increase calls | `~` Same underlying costs; duplicate processing, callbacks, retention, and telemetry can add more |
| Operational excellence | Deployment and contracts | `++` Fewer deployables and a shorter integration path | `--` Adds broker configuration, worker, message/callback contracts, and compatibility requirements |
| Operational excellence | Diagnostics and recovery | `++` Failures can surface in the request path; still requires telemetry and safe retry procedures | `--` Requires end-to-end correlation, status, queue/worker alerts, dead-letter review, and safe replay |
| Operational excellence | Processing isolation and queue visibility | `--` Retrieval work is coupled to the Web App operation and deployment | `++` Worker can deploy and scale separately; queue signals help reveal waiting work but do not prove successful persistence |
| Performance efficiency | Initial response and user wait | `--` The operation waits for the external retrieval outcome (and any required persistence) | `++` Can acknowledge after durable broker acceptance, before retrieval finishes |
| Performance efficiency | Time until usable content | `++` May finish sooner at low load because there are no queue or callback hops | `--` Adds queue wait, worker startup, callback, and persistence time; acceptance is not completion |
| Performance efficiency | Burst handling and concurrency | `--` Direct request pressure can overload the Web App or downstream services | `++` Can buffer finite bursts and pace consumers; producer limits and backlog-drain capacity are still required |
| Performance efficiency | Sustained throughput and bottlenecks | `~` Bounded by Microsoft Learn and Web App/storage capacity | `~` Same downstream limits; independent workers do not remove the Web App callback or SQLite/local-disk constraints |

The markers describe architectural tendencies, not measured outcomes. Both alternatives still need
timeouts, bounded retries, monitoring, secure storage, and defined recovery. A diagram cannot
establish availability, encryption, disaster recovery, or achieved performance targets.

### Reliability: checklist impact and mitigation summary

| Official checklist items | What this decision touches | Tradeoff and required mitigation |
| --- | --- | --- |
| RE:01 simplicity; RE:03 failure analysis | Direct retrieval versus broker, worker, and result callback | B gains outage buffering but adds failure states. Keep one owner for persistence, use stable operation IDs, and reconcile partial saves and commit/publish gaps. |
| RE:02 flows; RE:04 targets | Accepting an import is distinct from making content usable | B gains early acceptance but loses immediate completion. Set separate acceptance/completion SLOs, freshness limits, RTO, and RPO; expose pending and terminal outcomes. |
| RE:05 redundancy; RE:09 disaster recovery | Web App, SQLite, files, broker, and operation records | Neither view establishes high availability or DR. Select redundancy from the targets and test coordinated recovery of content, files, status, and retained messages. A queue is not a backup. |
| RE:06 scaling; RE:07 self-preservation | Worker concurrency, external throttling, callback capacity, finite backlog | B can pace work but can accumulate expired requests or overwhelm persistence. Bound concurrency, retries, queue age, and retention; use dead-letter handling and controlled replay. |
| RE:08 testing; RE:10 monitoring | Entire path from request to saved content | Queue health alone misses callback/storage failures. Inject failure at each boundary and verify correlated status, idempotency, alerts, and recovery within agreed targets. |

All ten items remain **open for implementation evidence**. The detailed review specifies mitigations,
failure tests, and cross-pillar costs; no checklist item is marked satisfied by adding a bus.

## 3. Requirements before implementing the async alternative

1. **Acceptance and status:** acknowledge only after the bus confirms durable acceptance. Give
   each operation a stable ID and persist status such as accepted, processing, completed, or failed.
   The status endpoint or UI update mechanism is a design requirement, not an existing component.
2. **Result persistence:** the function calls an authenticated Web App interface. Mark the operation
   complete only after the Web App confirms all required database/file writes. A retrieval success
   alone is not completion.
3. **Idempotency and acknowledgement:** expect redelivery unless the selected broker guarantees
   otherwise. Deduplicate result submission and use stable content/version identifiers. Acknowledge
   the message only after persistence succeeds. Handle a crash after saving but before acknowledgement.
4. **Recovery and limits:** define bounded retries with backoff for transient failures, handling for
   permanent failures, expiry, dead-letter review, and controlled replay. A queue has finite capacity
   and retention; outages cannot be buffered indefinitely.
5. **Safe capacity:** cap concurrency against both Microsoft Learn limits and Web App persistence
   capacity. The callback retains a runtime dependency on the Web App; the worker is not fully isolated.
6. **Data ownership:** keep writes behind the Web App as modeled. SQLite and local disk remain
   deployment/scaling constraints; adding a bus does not turn them into shared, highly available storage.

The bus technology, durability configuration, hosting plans, and these controls are not specified
by the DSL. Azure Service Bus is a possible implementation, not a selected resource in the model.

## 4. Recommendation

Use **Option A** for short, low-volume retrieval where users need the result during the current
interaction and failure/retry can be handled acceptably within the request budget.

Use **Option B** for background imports, bursts, or long-running retrieval when users can accept
delayed availability and the team can implement durable status, idempotent callbacks, and recovery.
It buffers work rather than eliminating external failures. Measure acceptance latency separately
from time until the content is saved and available.

Choose using expected volume, external latency/limits, acceptable content freshness, outage tolerance,
Web App/storage capacity, and operating cost. There is no evidence here for a universal winner.

## Detailed comparisons

- [Reliability](./reliability-comparison.md)
- [Security](./security-comparison.md)
- [Cost optimization](./cost-optimization-comparison.md)
- [Operational excellence](./operational-excellence-comparison.md)
- [Performance efficiency](./performance-efficiency-comparison.md)
