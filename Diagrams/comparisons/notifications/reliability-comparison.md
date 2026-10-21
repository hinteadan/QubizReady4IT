# Reliability: Synchronous vs Asynchronous Notifications

This applies the [WAF Reliability pillar](https://learn.microsoft.com/azure/well-architected/reliability/)
to the paths defined in the [notification overview](./sync-async-comparison.md).
The [workspace](../../structurizr/workspace/workspace.dsl) models both alternatives as planned.

## 1. Paths and reliability boundaries

- **A (`Containers`):** Web App and Content Notification Function invoke Notification Service
  directly. The Content Notification Function first obtains preferences and checks matching content
  through the Web App.
- **B (`ContainersAsync`):** the same producers publish to Notification Bus; Notification Processor
  Function consumes requests and invokes Notification Service. Preference lookup and matching
  remain dependent on the Web App, before delivery is queued.

Business-action success, notification intent, broker acceptance, provider acceptance, and recipient
receipt are separate outcomes. The model specifies neither a provider idempotency contract nor
delivery receipts/status storage. The learning integration bus is a separate flow, not failover
capacity for notifications.

## 2. Reliability checklist review

This review explicitly follows Microsoft's
[Reliability design review checklist](https://learn.microsoft.com/en-us/azure/well-architected/reliability/checklist).
The recommendation names below are condensed from that checklist. **Every item is open**:
the DSL describes topology, not implemented controls or proof that reliability targets are met.
`++` marks a relative advantage, `--` a cost or disadvantage, and `~` an unchanged/shared concern.

| Checklist item | What we touch | A/B tradeoff | Mitigation and evidence needed to close the item |
| --- | --- | --- | --- |
| [RE:01 Simplicity](https://learn.microsoft.com/en-us/azure/well-architected/reliability/simplify) | Two producers calling a provider versus a bus and centralized processor | A `++` has fewer runtime components. B `++` centralizes delivery policy but `--` adds distributed state and handoffs. | Keep matching separate from delivery, define one delivery policy, and document ownership of pending/failed notifications. Add deferred processing only when required by reliability and timeliness targets. |
| [RE:02 Identify and rate flows](https://learn.microsoft.com/en-us/azure/well-architected/reliability/identify-flows) | Business action, preference matching, notification handoff, provider acceptance, and recipient receipt | B `++` can decouple provider outages from producers; `--` alerts can arrive late. Both `~` retain the Web App dependency for preference/content matching. | Rank notification types by criticality and useful lifetime. Decide whether a business action may succeed when notification fails and whether current opt-outs must be rechecked. Do not assign all notification types one undifferentiated delivery guarantee. |
| [RE:03 Failure mode analysis](https://learn.microsoft.com/en-us/azure/well-architected/reliability/failure-mode-analysis) | Matching failures, commit/send or commit/publish gaps, broker/processor failures, ambiguous provider outcomes | B `++` retains durably accepted intent through temporary outages; `--` redelivery adds duplicate-send cases. A also has ambiguous timeouts and retry duplicates. | Use the failure table below as the FMA starting point. Use stable logical notification IDs, durable intent with outbox/reconciliation where required, and provider idempotency/status lookup where supported. Document the residual duplicate/loss risk when the provider cannot resolve ambiguous sends. |
| [RE:04 Reliability and recovery targets](https://learn.microsoft.com/en-us/azure/well-architected/reliability/metrics) | Matching timeliness, acceptance success, provider acceptance, expiry, RTO, and RPO | B `++` can improve acceptance during provider outages; `--` acceptance may remain healthy while delivery misses its useful window. Neither proves recipient receipt. | Set separate SLOs for intent-to-provider acceptance and, only where evidence exists, recipient delivery. Agree maximum notification age, acceptable duplicates/loss, recovery time objective (RTO), and recovery point objective (RPO) for pending intent and delivery records. No numerical targets or achieved results exist here. |
| [RE:05 Redundancy](https://learn.microsoft.com/en-us/azure/well-architected/reliability/redundancy) | Producers, Web App preference dependency, broker, processor, provider, and durable delivery state | B `++` allows separately recoverable consumers; `--` extra services need failure-domain protection. Both `~` depend on the provider and upstream matching; a queue does not make these redundant. | Select hosting/broker redundancy from targets and verify provider availability assumptions. Establish resilient delivery-state storage and upstream recovery. Do not add a second provider without deciding cross-provider deduplication, routing, and delivery semantics. |
| [RE:06 Scaling](https://learn.microsoft.com/en-us/azure/well-architected/reliability/scaling) | Matching rate, producer bursts, provider quotas, consumer throughput, and backlog drain | B `++` buffers finite bursts and paces sends; `--` a queue can accumulate notifications that will expire before processing. More consumers can cause provider throttling. | Bound producer admission and consumer concurrency, respect provider quotas, and size drain capacity for the useful notification lifetime. Load-test both matching and sending; queue scaling does not fix a slow preference/content lookup. |
| [RE:07 Self-preservation and self-healing](https://learn.microsoft.com/en-us/azure/well-architected/reliability/self-preservation) | Retry storms, poison requests, duplicate sends, expiry, and replay | B `++` supports deferred retry; `--` indiscriminate retries/replay can spam recipients or send obsolete alerts. A has fewer recovery states but less outage buffering. | Use timeouts, bounded backoff with jitter, circuit breaking, delivery limits, explicit expiry, and owned dead-letter review. Preserve logical IDs across retries; use provider idempotency where available. Recheck opt-outs when required and record suppressed, expired, and terminally failed outcomes. |
| [RE:08 Reliability testing](https://learn.microsoft.com/en-us/azure/well-architected/reliability/reliability-test) | Producer, matching, bus, processor, and external-send failure boundaries | A `++` has fewer infrastructure scenarios. B `--` adds publish/acknowledgement loss and redelivery tests; neither architecture establishes safe external sends on its own. | Execute the failure-injection cases below in a controlled environment. Verify agreed duplicate/loss behavior, explicit failure/expiry, and recovery within RE:04 targets. Retain evidence before marking this item complete. |
| [RE:09 Disaster recovery](https://learn.microsoft.com/en-us/azure/well-architected/reliability/disaster-recovery) | Pending notification intent, queued messages, delivery/deduplication history, and upstream state | B `++` may retain pending intent if configured for the failure scope; `--` restoring stale delivery history can resend notifications already accepted by the provider. Queue retention is not backup. | Define backup/restore and reconciliation for intent, messages, and delivery records. Verify whether broker DR protects message data or only configuration. Test RTO/RPO and expired-request handling; do not blindly replay all restored messages. |
| [RE:10 Health monitoring](https://learn.microsoft.com/en-us/azure/well-architected/reliability/monitoring) | Matching through handoff/provider outcome, plus recipient evidence when available | B `++` exposes backlog and processing signals; `--` an empty queue can hide failed matching or discarded notifications. Provider acceptance alone is not recipient delivery. | Correlate stable notification IDs across matching, publication, consumption, and provider calls. Track missed/failed scans, unsent intent, oldest age, retries, dead letters, expiry, duplicate signals, and provider failures. Test alerts and distinguish provider acceptance from verified receipt in reports. |

## 3. Failure behavior

| Concern | A: direct provider calls | B: Notification Bus and processor |
| --- | --- | --- |
| Provider outage | Producer sees failure or timeout | Accepted requests can wait, subject to durability, retention, and capacity |
| Business change commits but notification handoff fails | Notification intent can be lost unless durably retained/reconciled | Same commit/publish gap; a bus alone does not make it atomic |
| Broker outage | No broker dependency | Publishing fails; producer must report or durably retain the unsent request |
| Processor outage | No separate processor | Work accumulates until recovery, expiry, or capacity exhaustion |
| Ambiguous provider timeout | Retrying may send twice | Redelivery after a timeout or crash may send twice |
| Preference-check failure | Content Notification Function cannot identify matching notifications | Same; queuing delivery does not fix preference lookup or matching |
| Extended delay | Inline retries are limited by operation time budget | Backlog can contain stale or no-longer-authorized notifications |
| Provider accepts, then response or local acknowledgement is lost | Retry may send again; outcome needs reconciliation | Redelivery may send again; local deduplication cannot atomically commit an external send |
| Restored delivery history is older than provider sends | Retried intent can resend a previously accepted notification | Replaying restored messages can resend previously accepted notifications |

## 4. Required controls

- Define provider timeouts and bounded retries for transient failures, respecting throttling.
  Distinguish permanent rejection from temporary failure.
- Use stable logical notification IDs across repeated matching scans and delivery retries.
- Confirm the provider's idempotency contract. A crash after provider acceptance but before local
  recording/message acknowledgement can cause duplicates. Exactly-once recipient delivery is not
  established by a queue or a local deduplication table.
- Separate business-action success from notification outcome. If reliable event-to-notification
  handoff is required, implement an outbox or reconciliation covering the commit/publish gap.
- For B, confirm durable publication before reporting acceptance. Acknowledge consumption only
  after the intended provider outcome is recorded; handle acknowledgement loss safely.
- Define request lifetime, stale-message handling, retry limits, and dead-letter review/replay.
  Recheck current opt-outs/preferences when the business rule requires it.
- Record terminal failures and alert on oldest-message age, dead letters, and provider errors.
  Broker disaster recovery and redundancy depend on the selected implementation.

## 5. Verification scenarios (RE:08)

These are required future tests, not tests executed in this diagram repository. Measure recovery
against the agreed RE:04 targets; no unspecified target should be treated as passed.

| Injected failure | Expected result / verification |
| --- | --- |
| Provider timeout, throttling, or outage | A reports or durably retains the failed notification according to policy. B preserves durably accepted intent within limits, uses bounded retries, and drains within the useful lifetime after recovery. |
| Business commit succeeds but send/publish fails | Required intent remains discoverable through the chosen outbox/reconciliation design; no false notification-success result. Business outcome follows the agreed flow criticality. |
| Broker accepts but publish confirmation is lost | Retry keeps the same logical notification ID; duplicate publication does not create a new logical notification. Verify provider-side deduplication behavior, not just local message handling. |
| Provider accepts, then processor crashes before recording/acknowledging | Use provider idempotency or outcome reconciliation where supported. If neither exists, demonstrate and document the chosen duplicate-versus-loss policy rather than asserting exactly-once delivery. |
| Repeated matching scans, or Web App preference lookup unavailable | Stable IDs prevent new logical notifications for the same event/user; failed matching is observable and recovered separately from delivery. An empty queue must not be reported as a healthy matching flow. |
| Opt-out while queued, stale request, or poison payload | Apply the agreed current-preference rule, record suppression/expiry, and quarantine permanent failures. Replay does not bypass those rules. |
| Burst exceeds drain capacity or retention | Admission/expiry is explicit; alerts fire before agreed age limits are breached. Recovery respects provider quotas and does not produce a retry storm. |
| Restore intent, messages, and delivery history after a simulated disaster | Reconcile pending versus already-accepted sends, suppress expired intent, and validate RTO/RPO and the agreed duplicate/loss policy. |

## 6. Reliability tradeoffs with other pillars

These apply the official
[Reliability tradeoff guidance](https://learn.microsoft.com/en-us/azure/well-architected/reliability/tradeoffs)
to this flow.

| Pillar affected | What we gain / give up | Mitigation |
| --- | --- | --- |
| Cost Optimization | B `++` can retain intent through outages; `--` adds durable messaging, delivery/deduplication state, observability, redundancy, and recovery/testing costs. Retries or duplicates may incur provider charges. | Price against notification criticality and RE:04 targets. Bound retries/retention and include on-call, replay, and drills rather than counting only function executions. |
| Operational Excellence | B `++` centralizes delivery policy and consumer recovery; `--` adds schema compatibility, dead-letter ownership, expiry, and uncertain-send reconciliation. | Assign owners, preserve logical IDs, codify retry/expiry rules, and rehearse recovery with old queued message versions. |
| Performance Efficiency | B `++` reduces producer wait and paces bursts; `--` queueing/backoff delays time-sensitive alerts and recovery capacity may sit idle normally. | Measure intent-to-provider acceptance separately from enqueue latency; size for maximum useful age and cap concurrency to provider limits. |
| Security | B `++` isolates provider credentials; `--` durable recipient data and replay tools add permissions and exposure. Availability pressure must not override opt-outs or authorization. | Minimize retained data, restrict/audit replay, preserve authorization during incidents, and apply the agreed current-preference checks before delayed sends. |

## 7. Recommendation

**B can preserve accepted notification intent through temporary provider or worker outages**, with
finite buffering and additional failure modes. A is simpler when immediate explicit failure meets
requirements. Neither proves recipient delivery: define separate targets for enqueue acceptance,
provider acceptance, and receipt where the provider supports evidence of it.
