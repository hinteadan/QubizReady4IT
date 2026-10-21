# Operational Excellence: Synchronous vs Asynchronous Microsoft Learn Integration

This document applies the
[WAF Operational Excellence pillar](https://learn.microsoft.com/azure/well-architected/operational-excellence/)
to the planned KnowledgeGram learning integration. See the
[overall comparison](./sync-async-comparison.md) and [workspace](../../structurizr/workspace/workspace.dsl).

## 1. Operational paths

- **A (`Containers`):** Web App calls Microsoft Learn directly, handles errors, and performs any
  required local persistence.
- **B (`ContainersAsync`):** Web App publishes to Learning Integration Bus; Learning Integration
  Function retrieves content and submits it back to the Web App for database/file persistence.
  The callback is part of the critical path, not an optional implementation detail.

The separate notification bus/processor needs its own operational signals and ownership.
Success on one bus does not establish the health of the other.

## 2. Tradeoffs

| Dimension | A: synchronous | B: asynchronous |
| --- | --- | --- |
| Deployables and contracts | Web App integration and external contract | Also broker configuration, worker, message schema, and Web App callback |
| Diagnostics | Trace retrieval and save within the operation | Correlate enqueue, delivery, external retrieval, callback, and save |
| Failure visibility | Errors can surface inline; durable logs/status still require implementation | Queue signals help, but do not prove results were saved |
| Deployment independence | Integration changes ship with the Web App | Worker can deploy separately if message and callback contracts stay compatible |
| Recovery | Retry the operation safely; reconcile partial saves | Also pause consumers, inspect dead letters, and replay without duplicate effects |
| Team burden | Fewer moving parts | More distributed-state reasoning and runbooks |

Good logs and tracing are possible in both options. The bus does not automatically create an
end-to-end trace, durable user-visible status, safe replay, or application retry policy.
Managed delivery features depend on the broker and configuration selected.

## 3. Operating requirements

### Shared

- Correlate external calls with import identifiers and persistence outcomes. Avoid logging raw
  user data, credentials, or full content unnecessarily.
- Alert on retrieval failures, throttling, Web App availability, and storage errors/capacity.
- Document response to Microsoft Learn downtime and partial database/file persistence.
- Use staged rollout and contract tests; establish the real external API contract first.

### Additional for B

- Persist operation status and expose accepted, processing, completed, and failed outcomes.
  No status UI or endpoint is currently implemented merely by adding these containers.
- Track oldest-message age, backlog, retry/delivery counts, dead-letter count, callback failures,
  and end-to-end completion latency. A healthy consumer with a failing callback is not a healthy flow.
- Define owners and procedures for expired requests, poison messages, authorization failures,
  Web App downtime, backlog drain, and duplicate callbacks.
- Make replay deliberate: inspect failure causes, authorize replay, retain operation IDs, and
  rate-limit recovery. Blind automatic replay can repeat a permanent fault or duplicate writes.
- Version message and callback contracts compatibly with queued older messages. A rollback
  cannot undo content already saved; define reconciliation or compensating actions.
- Codify broker, function, identities, limits, and alerts in deployment configuration when implemented.
  Choose pause/drain behavior compatible with retention and user completion targets.

## 4. Recommendation

**A minimizes operational surface. B provides useful processing isolation and queue visibility,
but demands more explicit lifecycle management.** Choose B when asynchronous imports deliver
business value and the team can operate the complete path, including the Web App callback and
local storage. Do not select it merely on an assumption of automatic observability or recovery.
