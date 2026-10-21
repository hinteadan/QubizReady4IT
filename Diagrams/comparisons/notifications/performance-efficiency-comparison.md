# Performance Efficiency: Synchronous vs Asynchronous Notifications

This applies the
[WAF Performance Efficiency pillar](https://learn.microsoft.com/azure/well-architected/performance-efficiency/)
to the planned alternatives in the [notification overview](./sync-async-comparison.md).
See the [workspace](../../structurizr/workspace/workspace.dsl) for their paths.

## Performance tradeoffs

| Dimension | A: direct provider calls | B: queued delivery |
| --- | --- | --- |
| Producer completion | Waits for provider response | Can finish after confirmed broker acceptance |
| Time to provider acceptance | Direct call, without queue/worker startup | Queue wait, worker startup, processing, and provider call |
| Time to recipient delivery | Depends on provider/channel | Still depends on provider/channel, plus preceding queue delay |
| Bursts | Reach provider unless producers limit concurrency | Finite queue buffers bursts; processor can pace provider calls |
| Sustained throughput | Limited by provider and producer resources | Still limited by provider; adding consumers cannot bypass quotas |
| Preference matching | Function checks through Web App | Same check; bus does not accelerate content matching |

Synchronous architecture does not imply blocking threads: properly awaited provider I/O can release
threads while the producer operation remains pending. Conversely, fast enqueue acknowledgement
does not mean a recipient receives the notification sooner.

## Targets and controls

- Measure producer latency, queue age, provider acceptance latency, and receipt latency separately
  where receipts exist. Define a maximum useful notification age.
- Set bounded concurrency and retries across both producers in A, and across processor instances
  in B. Respect provider quotas and retry guidance.
- Ensure sustained processing capacity and post-outage drain capacity meet timeliness targets.
  A queue cannot absorb unlimited work; producers need overload handling too.
- Autoscaling must consider provider throttling, not just queue length. Extra workers can worsen
  retries without increasing successful sends.
- Account for cold starts and unnecessary ordering constraints. Ordering and priority features
  depend on the broker selected and are not guaranteed by the generic bus.
- Deduplicate repeated scans and requests to avoid spending capacity on the same logical alert.
  Expire obsolete notifications rather than delivering an unbounded stale backlog.

## Recommendation

B is useful when Web App responsiveness or burst smoothing matters more than immediate provider
confirmation. A can have lower provider-acceptance latency at light load and fewer tuning settings.
Neither changes the provider's delivery capacity; compare useful notifications delivered within
target time, not just request acceptance speed.
