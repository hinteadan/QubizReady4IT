# KnowledgeGram: Synchronous vs Asynchronous Notifications

This comparison covers the **Containers** and **ContainersAsync** views in the
[Structurizr workspace](../../structurizr/workspace/workspace.dsl). Both notification alternatives
are planned, not implemented behavior. Microsoft Learn retrieval is covered
[separately](../ms-learn/sync-async-comparison.md).

## 1. Paths being compared

| | Option A: synchronous | Option B: asynchronous |
| --- | --- | --- |
| Producers | KnowledgeGram Web App and Content Notification Function | Same producers |
| Notification path | Each producer invokes Notification Service directly | Each producer publishes to Notification Bus; Notification Processor Function consumes and invokes Notification Service |
| Preference matching | Content Notification Function retrieves preferences and checks new content through the Web App | Unchanged; the bus decouples delivery, not preference lookup or matching |
| Initial success | Provider call succeeded according to its contract | Broker accepted the request; provider has not necessarily been called |
| Recipient delivery | Not guaranteed by a successful provider call | Not guaranteed by enqueueing or a successful provider call |

Notification Service is an external delivery provider. Its channels, API, delivery receipts,
idempotency support, and guarantees are unspecified. No provider callback or delivery-status
component is currently modeled. The generic bus does not select a broker product or tier.

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
`++` = advantage, `--` = tradeoff/disadvantage, and `~` = shared or workload-dependent.
They are qualitative, not points, pillar scores, or claims of WAF compliance.

| WAF pillar | Review dimension | Option A: synchronous | Option B: asynchronous |
| --- | --- | --- | --- |
| Reliability | Provider outage | `--` Failure or timeout affects the producer's notification operation immediately | `++` Durable accepted requests can wait for recovery within retention and capacity limits |
| Reliability | Broker and processor dependencies | `++` No notification broker or separate delivery processor | `--` Publishing can fail and processors can stop; recovery and explicit failure handling are required |
| Reliability | Duplicate sends and ambiguous outcomes | `~` Provider timeouts and producer retries can duplicate sends | `--` Redelivery adds replay cases; provider idempotency is still needed to resolve ambiguous sends safely |
| Reliability | Preference matching and recipient delivery | `~` Matching depends on the Web App; provider acceptance is not proof of recipient receipt | `~` Same matching dependency and delivery uncertainty; delivery queuing does not solve either |
| Security | Service and retained-data surface | `++` Fewer identities, endpoints, and message stores to secure | `--` Adds broker/consumer authorization, retained payloads, dead letters, and replay access |
| Security | Provider credential isolation | `--` Both producers require provider access | `++` Provider credentials can be limited to the notification processor |
| Security | Deferred authorization and opt-outs | `++` No queued delay between the decision to notify and the send attempt | `--` Must decide whether to recheck current preferences/opt-outs and reject stale requests |
| Cost optimization | Incremental infrastructure and engineering | `++` No notification broker/processor; fewer contracts and recovery paths | `--` Adds broker, function, deduplication/status, telemetry, and operational work |
| Cost optimization | Burst capacity and external calls | `~` Bursts can require producer capacity or cause rejection; retries can cost more | `~` Pacing can reduce peak pressure, but queue capacity, duplicate sends, and retention also cost; savings need measurement |
| Operational excellence | Diagnostics and recovery | `++` Shorter producer-to-provider trace; safe retries still need implementation | `--` Needs correlation across publish/consume/send, expiry handling, dead-letter review, and controlled replay |
| Operational excellence | Processing isolation | `--` Provider integration changes and failures are handled in each producer | `++` Processor can deploy separately and centralize delivery policy, with compatible message contracts |
| Performance efficiency | Initial response | `--` Producer waits for the provider outcome | `++` Producer can return after durable broker acceptance, before the provider is called |
| Performance efficiency | Time to provider acceptance | `++` No queue wait or processor startup at low load | `--` Adds queue/processor delay; early acceptance does not mean earlier recipient delivery |
| Performance efficiency | Burst handling | `--` Direct bursts pressure producers and the provider | `++` Can buffer finite bursts and cap consumer concurrency, subject to notification freshness limits |
| Performance efficiency | Sustained throughput | `~` Limited by provider capacity and producer resources | `~` Provider limits remain; extra consumers can increase throttling rather than useful throughput |

### Reliability: checklist impact and mitigation summary

| Official checklist items | What this decision touches | Tradeoff and required mitigation |
| --- | --- | --- |
| RE:01 simplicity; RE:03 failure analysis | Two direct producers versus a bus and centralized processor | B buffers outages but adds handoff and replay failure modes. Use stable logical notification IDs, outbox/reconciliation where required, and provider idempotency where supported. |
| RE:02 flows; RE:04 targets | Business action, matching, enqueueing, provider acceptance, and recipient receipt | B decouples delivery but can make alerts stale. Rank flows, set separate SLOs and maximum notification age, and define acceptable loss and duplicates. Do not claim receipt without provider evidence. |
| RE:05 redundancy; RE:09 disaster recovery | Producers, Web App preference dependency, broker, processor, and delivery records | No redundancy or DR is established. Choose failure-domain protection and test recovery of pending intent and deduplication history without blindly resending already accepted notifications. |
| RE:06 scaling; RE:07 self-preservation | Provider rate limits, finite backlog, retries, and expiry | B can pace sends but build a stale backlog. Cap concurrency and retries, expire obsolete alerts explicitly, and recheck opt-outs when required. |
| RE:08 testing; RE:10 monitoring | Matching through provider outcome, including ambiguous sends | Queue depth alone misses matching failures and duplicate sends. Inject faults, test replay, correlate logical notification IDs, and alert on age, failures, and duplicates. |

All ten items remain **open for implementation evidence**. The detailed review specifies mitigations,
failure tests, and cross-pillar costs; no checklist item is marked satisfied by adding a bus.

## 3. Required design decisions

- Define success separately for business actions, enqueueing, provider acceptance, and recipient
  delivery. Notification failure must not silently masquerade as success.
- If a committed business change must produce a notification, address the gap between that commit
  and sending/publishing, for example with a transactional outbox or reconciliation. A bus alone
  does not make those operations atomic.
- Give notifications stable IDs. Repeated preference scans and repeated message delivery must not
  generate new logical notifications for the same user/content event unintentionally.
- Expect ambiguous send outcomes: the provider may accept a request before a timeout or processor
  crash. Use provider idempotency where available; local deduplication alone cannot guarantee
  exactly-once external delivery.
- Define retry limits, expiry, dead-letter handling, and safe replay. Delayed alerts may become
  irrelevant; establish whether current preferences or opt-outs must be rechecked before sending.
- Choose how to expose terminal failure and, if required, delivery receipts. Neither is implemented
  by the diagram alone.

## 4. Recommendation

Prefer **A** for simple, low-volume notifications where inline provider failure can be handled
acceptably. Prefer **B** when producer responsiveness, burst handling, or deferred recovery matters,
and delayed delivery is acceptable. B isolates provider calls from producers but does not remove
the Content Notification Function's dependency on the Web App.

For the full async architecture, budget and operate Notification Bus separately from Learning
Integration Bus; neither flow's success proves the other's health.

## Detailed comparisons

- [Reliability](./reliability-comparison.md)
- [Security](./security-comparison.md)
- [Cost optimization](./cost-optimization-comparison.md)
- [Operational excellence](./operational-excellence-comparison.md)
- [Performance efficiency](./performance-efficiency-comparison.md)
