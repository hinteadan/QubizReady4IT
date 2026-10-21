# Operational Excellence: Synchronous vs Asynchronous Notifications

This applies the
[WAF Operational Excellence pillar](https://learn.microsoft.com/azure/well-architected/operational-excellence/)
to the planned paths in the [notification overview](./sync-async-comparison.md) and
[workspace](../../structurizr/workspace/workspace.dsl).

## Operational tradeoffs

| Dimension | A: direct provider calls | B: queued delivery |
| --- | --- | --- |
| Deployment | Provider integration in both producers | Also processor and broker configuration; message contracts must stay compatible |
| Tracing | Producer to provider | Producer to queue to processor to provider |
| Failure triage | Inline errors plus configured telemetry | Also publish failures, backlog, retries, expiry, and dead letters |
| Recovery | Retry safely from known notification intent | Pause/drain/replay with stable IDs and duplicate-send safeguards |
| Ownership | Producer/provider integration | Also queue lifecycle, worker health, and delivery-state ownership |

## Required operating practices

- Use one logical notification ID across detection, publishing, processing, and provider response.
  Instrument both producers so their requests can be distinguished.
- Track business action, notification creation, broker acceptance, provider acceptance, and
  recipient delivery as separate outcomes. Provider receipts require a separately designed
  integration if needed; the current model does not include one.
- For B, alert on oldest-message age, backlog, processing failures, dead letters, and publish
  failures. A low queue depth is not proof of delivery: messages may have expired or failed.
- Define runbooks for provider downtime/throttling, poisoned requests, obsolete notifications,
  changed preferences, and ambiguous send results.
- Make replay reviewed and rate-limited. Retain logical IDs and account for provider idempotency
  limits; do not automatically resend every dead-letter request.
- Version message schemas and templates with older queued messages in mind. Rolling back a
  deployment cannot unsend notifications already accepted by the provider.
- Keep notification operations separate from learning-import operations. The Content Notification
  Function's preference-check health must be monitored independently of delivery.
- When implemented, codify broker, processor, permissions, retry/expiry settings, and alerts.
  Queue metrics alone do not provide an application trace or user-visible notification history.

## Recommendation

A minimizes operational components. B provides delivery-worker isolation and queue visibility,
but requires explicit notification lifecycle management and safe recovery procedures. Prefer B
when those capabilities are valuable and owned, not on the assumption that a bus supplies
automatic end-to-end observability or replay safety.
