# Performance Efficiency: Synchronous vs Asynchronous Microsoft Learn Integration

This document applies the
[WAF Performance Efficiency pillar](https://learn.microsoft.com/azure/well-architected/performance-efficiency/)
to the planned learning integration in the [KnowledgeGram workspace](../../structurizr/workspace/workspace.dsl).
See the [overall comparison](./sync-async-comparison.md) for shared assumptions.

## 1. Paths and performance meaning

- **A (`Containers`):** Web App waits for a direct Microsoft Learn retrieval. If the operation
  includes local persistence, its completion time also includes saving through the Web App.
- **B (`ContainersAsync`):** Web App publishes to Learning Integration Bus. Learning Integration
  Function retrieves content and submits results to the Web App, which writes the Content
  Database or Local File Storage. No worker-to-storage shortcut exists.

An asynchronous HTTP implementation (`async`/`await`) is different from asynchronous messaging.
Proper asynchronous network I/O need not block a Web App thread while waiting in A, although the
operation still occupies resources and the user still waits for its outcome.

## 2. Performance tradeoffs

| Dimension | A: synchronous | B: asynchronous |
| --- | --- | --- |
| Initial response | Includes external retrieval outcome | Can acknowledge after broker acceptance, before retrieval |
| Time to usable content | External retrieval plus any persistence, without queue hops | Includes queue wait, worker startup, retrieval, callback, and persistence |
| Traffic bursts | Concurrency limits may throttle or reject requests | Queue can absorb a finite burst; workers drain at a controlled rate |
| Sustained throughput | Limited by external service and Web App/storage capacity | Still limited by external service and Web App/storage capacity |
| Scaling | Scale Web App within downstream limits | Scale worker independently, but callback still loads the Web App |
| Local content browsing | Existing local storage path | Same path; imported content appears only after saving |
| Tuning | Timeouts, connection reuse, rate/concurrency limits, caching | Same downstream limits plus worker concurrency, queue settings, and retry behavior |

**The queue does not make Microsoft Learn faster or remove its rate limits.** More workers can
increase throttling and retries rather than useful throughput. B may complete sooner under overload
when pacing prevents failures, but it does not inherently have lower completion latency.

## 3. Bottlenecks and controls

- Define separate percentile targets for acceptance latency and time to content availability.
  Also measure completion rate, errors, external throttling, queue age, and callback/save latency.
- Bound concurrency against Microsoft Learn limits **and** Web App persistence capacity.
  Queuing is load buffering, not automatic end-to-end backpressure; producers need limits too.
- At sustained load, processing capacity must exceed arrivals to drain backlog. Capacity based
  only on average traffic may violate freshness targets after a large burst.
- SQLite writes and local-disk throughput constrain both alternatives. Multiple Web App instances
  cannot be assumed to share the same local database/files.
- Reuse connections, avoid duplicate retrieval where content-use rights and freshness permit
  caching, and use stable identifiers to avoid repeated imports.
- Account for content size and callback transfer time. Large content should not be assumed to fit
  in a message; choose bounded payloads or authorized references appropriate to the implementation.
- Worker cold starts, retry delays, and ordering requirements can increase B's completion latency.
  No ordering/priority facility is selected by the generic bus in the DSL.

## 4. Recommendation

Prefer **A** for interactive retrieval when measured external latency fits the user experience
and volume is manageable. Prefer **B** for background imports and bursts where early acceptance
and controlled processing are more important than immediate content availability.

Neither alternative has measured performance evidence yet. Evaluate completion time and freshness,
not just how quickly the user receives an acknowledgement.
