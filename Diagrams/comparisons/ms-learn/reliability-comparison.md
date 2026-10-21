# Reliability: Synchronous vs Asynchronous Microsoft Learn Integration

This document applies the [WAF Reliability pillar](https://learn.microsoft.com/azure/well-architected/reliability/)
to the planned KnowledgeGram learning integration. See the
[overall comparison](./sync-async-comparison.md) for scope and assumptions and the
[workspace](../../structurizr/workspace/workspace.dsl) for both container views.

## 1. Paths being compared

- **A (`Containers`):** Web App calls Microsoft Learn directly and handles the retrieval outcome.
  Any local persistence uses the Web App's database/file storage access.
- **B (`ContainersAsync`):** Web App publishes to Learning Integration Bus; Learning Integration
  Function consumes, retrieves from Microsoft Learn, and returns results to the Web App for
  persistence. The function does not write directly to either store.

The separate notification flow is outside this comparison. Both learning paths are planned.

## 2. Reliability checklist review

This review explicitly follows Microsoft's
[Reliability design review checklist](https://learn.microsoft.com/en-us/azure/well-architected/reliability/checklist).
The recommendation names below are condensed from that checklist. **Every item is open**:
the DSL describes topology, not implemented controls or proof that reliability targets are met.
`++` marks a relative advantage, `--` a cost or disadvantage, and `~` an unchanged/shared concern.

| Checklist item | What we touch | A/B tradeoff | Mitigation and evidence needed to close the item |
| --- | --- | --- | --- |
| [RE:01 Simplicity](https://learn.microsoft.com/en-us/azure/well-architected/reliability/simplify) | Direct Web App retrieval versus bus, worker, callback, and deferred state | A `++` has fewer dependencies. B `--` adds distributed failure states in exchange for buffering. | Keep writes behind the Web App as modeled. Add only the status/deduplication and recovery mechanisms required by agreed targets; document ownership of every state transition and recovery path. |
| [RE:02 Identify and rate flows](https://learn.microsoft.com/en-us/azure/well-architected/reliability/identify-flows) | Request acceptance, external retrieval, persistence, and browsing saved content | A `++` provides an inline outcome. B `++` separates acceptance from slow retrieval, but `--` delays usable content. Both `~` retain the existing local browsing/storage path. | Rank interactive retrieval, background import, and local browsing by business criticality. Specify whether retrieval failure may affect browsing and whether a failed import may be safely resubmitted. Obtain agreement on delayed availability. |
| [RE:03 Failure mode analysis](https://learn.microsoft.com/en-us/azure/well-architected/reliability/failure-mode-analysis) | Microsoft Learn, publish confirmation, worker, callback, database, and files | B `++` can retain accepted work through temporary outages; `--` introduces ambiguous publish/acknowledgement and callback outcomes. Neither has an atomic database/file transaction. | Use the failure table below as the FMA starting point. Define detection, owner, retry/terminal outcome, and reconciliation for each boundary. Use stable operation IDs and idempotent persistence; address the operation-record/publish gap with outbox or reconciliation when required. |
| [RE:04 Reliability and recovery targets](https://learn.microsoft.com/en-us/azure/well-architected/reliability/metrics) | Acceptance success, saved-import success, content freshness, RTO, and RPO | B `++` can improve acceptance during external outages; `--` may miss completion/freshness targets while the queue looks healthy. | Agree separate SLOs and latency percentiles for acceptance and saved content, maximum pending age, recovery time objective (RTO), and recovery point objective (RPO) for content and accepted intent. No numerical targets or achieved results exist here. |
| [RE:05 Redundancy](https://learn.microsoft.com/en-us/azure/well-architected/reliability/redundancy) | Web App, local stores, broker, worker, and failure domains | Both `~` depend on Web App persistence and SQLite/local disk. B `--` adds infrastructure; a durable queue is not redundant application storage. | Select hosting/broker redundancy to match targets. Establish how Web App and storage survive instance/zone failure before claiming HA; do not simply add Web App replicas with independent local files/databases. Verify the chosen product's data durability and failover behavior. |
| [RE:06 Scaling](https://learn.microsoft.com/en-us/azure/well-architected/reliability/scaling) | External rate limits, worker throughput, callback and storage capacity | B `++` buffers finite bursts and allows separate worker scaling; `--` backlog can exceed freshness/retention or overload the Web App on recovery. | Bound producer admission and consumer concurrency against both dependencies. Size retention and drain capacity for the agreed outage/burst; load-test backlog recovery. Include SQLite write contention and local-file throughput, not just worker instance counts. |
| [RE:07 Self-preservation and self-healing](https://learn.microsoft.com/en-us/azure/well-architected/reliability/self-preservation) | Timeouts, retries, circuit breakers, duplicate effects, and poison requests | A `++` has a shorter recovery path but little request-time outage tolerance. B `++` enables deferred retries; `--` can cause retry storms or repeated partial writes. | Use bounded exponential backoff with jitter, dependency timeouts, circuit breaking, idempotent callbacks, delivery limits, expiry, and owned dead-letter/replay procedures. Record terminal failure rather than retrying permanent faults indefinitely. |
| [RE:08 Reliability testing](https://learn.microsoft.com/en-us/azure/well-architected/reliability/reliability-test) | Entire path under failure, overload, and recovery | A `++` has fewer boundaries to test. B `--` adds delivery, acknowledgement, callback, and backlog scenarios; topology alone proves none. | Execute the failure-injection cases below in a controlled environment. Verify no false completion, safe duplicate handling, and recovery within RE:04 targets; retain results before marking this item complete. |
| [RE:09 Disaster recovery](https://learn.microsoft.com/en-us/azure/well-architected/reliability/disaster-recovery) | Content Database, Local File Storage, operation/deduplication state, and queued intent | Both `~` need consistent database/file recovery. B `--` must also reconcile restored state with retained or lost messages; queue retention is not a backup. | Define backup, restore order, and reconciliation for all state. Confirm whether the selected broker's DR protects messages or only configuration. Restore and replay within RTO/RPO, preserving logical IDs and detecting already-saved content. |
| [RE:10 Health monitoring](https://learn.microsoft.com/en-us/azure/well-architected/reliability/monitoring) | User request through saved/available content, plus backlog health | A `++` has a shorter trace. B `++` exposes waiting work but `--` requires distributed correlation; a live worker does not prove successful callbacks or saves. | Correlate request, message, external retrieval, callback, and persistence with the operation ID. Track acceptance failures, completion failures/latency, oldest pending age, throttling, dead letters, and storage failures. Test alerts and retain evidence of incident detection and recovery. |

## 3. Failure behavior

| Failure or concern | A: synchronous | B: asynchronous |
| --- | --- | --- |
| Microsoft Learn unavailable | Current retrieval fails or times out; bounded retries may help | Accepted requests can wait for recovery if the bus is durable and retention is sufficient |
| Broker unavailable | No broker dependency | New requests cannot be accepted through the bus; do not return a success-shaped acknowledgement |
| Worker unavailable | No separate worker | Work waits within queue limits; detection and recovery are required |
| Web App unavailable | Requests cannot start or finish normally | New requests cannot start; workers also cannot persist results through the callback |
| Duplicate execution | Client or network retries can repeat retrieval/persistence | Redelivery and callback retries require explicit deduplication |
| Crash after saving | Retried operations must recognize already saved content | Crash before message acknowledgement can replay the whole operation; callback must be idempotent |
| Database/file write failure | Operation must report failure or an explicit partial outcome | Keep operation incomplete; retry safely or record a terminal failure |
| Extended outage | Requests fail within their time budgets | Backlog grows, expires, or reaches capacity; buffering is finite |

Synchronous request/response does not imply strong consistency across Microsoft Learn, SQLite, and
files. A failed retrieval is not automatically a lost database write. Likewise, message acceptance
does not guarantee content delivery or successful persistence.

## 4. Required reliability controls

### Common to both

- Set connection/operation timeouts, bounded transient retries, circuit-breaking behavior, and
  explicit errors. Respect external throttling and any supplied retry guidance.
- Define completion as the required content being saved and available, not merely fetched.
- Make imports idempotent using stable operation/content/version identifiers.
- Define recovery for partial database/file writes; these are not one atomic transaction.
- Back up and restore the Content Database and Local File Storage consistently. Their current
  SQLite/local-disk design is not made redundant by the async alternative.

### Additional controls for B

- Select and configure durable messaging; acknowledge acceptance only after publish confirmation.
- Track operation state durably and expose pending, completed, and failed outcomes to users.
- Acknowledge processing only after the Web App confirms persistence; handle duplicate callbacks
  and acknowledgement loss.
- Bound retry duration, delivery attempts, message lifetime, and backlog. Use dead-letter handling
  where supported, plus an owned review/replay procedure rather than unlimited automatic replay.
- If a local operation record and message publish must succeed together, design an outbox or
  reconciliation mechanism. They are not automatically atomic.
- Test callback downtime, poison requests, duplicate delivery, expiry, and crash-after-save.

## 5. Verification scenarios (RE:08)

These are required future tests, not tests executed in this diagram repository. Measure recovery
against the agreed RE:04 targets; no unspecified target should be treated as passed.

| Injected failure | Expected result / verification |
| --- | --- |
| Microsoft Learn timeout, throttling, or outage | A fails explicitly within its request budget. B retains durably accepted work within limits; bounded retries respect throttling and content becomes available after recovery. |
| Publish accepted but producer loses confirmation | Retry with the same logical operation ID. Duplicate publication must not create duplicate saved imports or falsely report completion. |
| Web App callback unavailable while worker is healthy | Operation stays incomplete; retry/recovery is bounded and observable. Recovery drains the backlog without exceeding Web App/storage capacity. |
| Crash after save but before callback response or message acknowledgement | Redelivery recognizes the saved operation and returns a consistent outcome without duplicate content/files. |
| Database save succeeds but file save fails (and the reverse) | No false completed status or silently broken content; reconciliation cleans up or finishes the partial import safely. |
| Poison request, expired message, or retention/capacity exhaustion | Permanent failure/expiry is explicit and observable; dead-letter handling and authorized replay do not loop indefinitely. Producer rejection is not reported as acceptance. |
| Restore stores, operation records, and broker after a simulated disaster | Reconcile accepted-but-missing and already-saved operations; validate database/file consistency and RTO/RPO, not merely service startup. |

## 6. Reliability tradeoffs with other pillars

These apply the official
[Reliability tradeoff guidance](https://learn.microsoft.com/en-us/azure/well-architected/reliability/tradeoffs)
to this flow.

| Pillar affected | What we gain / give up | Mitigation |
| --- | --- | --- |
| Cost Optimization | B `++` preserves accepted work during temporary outages; `--` adds durable messaging, status/deduplication, monitoring, and recovery/testing costs. Redundancy and spare drain capacity add further cost. | Buy capacity/redundancy against RE:04 targets, not an assumption that more components equal more reliability. Include team operations and drills in total cost. |
| Operational Excellence | B `++` allows isolated worker recovery; `--` adds message/callback versioning, replay, distributed tracing, and incident procedures. | Assign owners, codify configuration, keep contracts backward-compatible for queued work, and rehearse callback failure and partial-save recovery. |
| Performance Efficiency | B `++` paces bursts and shortens acceptance; `--` queueing, backoff, durable state, and callbacks increase time to usable content. | Measure completion separately, cap concurrency, and verify backlog drain against freshness targets. Avoid scaling consumers past the callback/storage bottleneck. |
| Security | B `++` can isolate external credentials; `--` durable messages and recovery tools add sensitive state and permissions. Disabling callback authorization to recover availability would create a security loss. | Preserve authentication/authorization during incidents, minimize payloads, scope replay permissions, and audit recovery without logging secrets or full content. |

## 7. Targets and recommendation

Measure **request acceptance**, **successful import completion**, and **time until content is
available** separately. Define tolerated content staleness and recovery time for backlog drain.
Broker redundancy, disaster recovery, and message retention must be selected and tested; none
is guaranteed by the diagram.

**B can improve resilience to transient Microsoft Learn outages for accepted work**, but retains
the Web App persistence dependency and adds broker/worker failure modes. Choose it when delayed
completion and recovery requirements justify those controls. **A remains valid** for short,
low-volume operations where explicit failure and retry meet the business target.
