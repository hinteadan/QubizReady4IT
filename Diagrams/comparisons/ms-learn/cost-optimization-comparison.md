# Cost Optimization: Synchronous vs Asynchronous Microsoft Learn Integration

This document applies the
[WAF Cost Optimization pillar](https://learn.microsoft.com/azure/well-architected/cost-optimization/)
to the planned KnowledgeGram learning integration. See the
[overall comparison](./sync-async-comparison.md) and [container model](../../structurizr/workspace/workspace.dsl).

## 1. Cost boundary

**A (`Containers`)** retrieves from Microsoft Learn directly in the Web App.
**B (`ContainersAsync`)** adds Learning Integration Bus and Learning Integration Function.
The function retrieves content and sends results back to the Web App for database/file persistence.
It does not replace the Web App or its storage costs.

The full async diagram also adds a separate Notification Bus and Notification Processor Function.
Include those in a full-architecture budget, but do not attribute their entire cost to the
Microsoft Learn integration.

## 2. Incremental costs

| Cost dimension | A: synchronous | B: asynchronous |
| --- | --- | --- |
| Messaging | No learning broker | Broker capacity/operations, message retention, and network configuration |
| Compute | External retrieval runs within Web App operations | Worker execution plus Web App enqueue, callback, and persistence work |
| External service | Contract/license/API costs as applicable | Same contractual costs; retries or duplicates may increase calls |
| Data | Download, optional persistence, and normal telemetry | Same content plus queue payloads, operation status, deduplication records, and callback transfers |
| Engineering | Direct integration, timeouts, telemetry, safe persistence | Also message contracts, result callbacks, status UX, redelivery handling, and recovery |
| Operations | Web App/dependency monitoring and support | Also queue/worker alerts, replay procedures, and independent deployment |
| Bursts | May require more Web App capacity or reject work | May smooth load, but must still meet completion/freshness targets |

Microsoft Learn represents a purchased external solution in the model; actual pricing, allowed
downloads, and API availability are not established here. No concrete cost forecast is possible
without those contracts and measured usage.

## 3. Avoid unsupported cost assumptions

- The DSL selects a **generic message bus**, not an Azure Service Bus tier. If Azure Service Bus
  is chosen, price the actual tier, region, capacity, operations, and networking requirements.
  Do not assume a broker is free or entirely usage-priced while idle.
- Azure Functions idle cost and scale-to-zero depend on the hosting plan and configuration.
  Minimum/warm capacity may be needed for latency targets and adds cost.
- Buffering bursts can reduce peak provisioning, but a growing queue is not a cost saving if
  content becomes available too late. Size for required backlog drain time as well as sustained load.
- Async does not remove Web App capacity needs: result submission and saving still run there.
  Callback retries can amplify database/file writes unless deduplicated.
- Retries, telemetry retention, dead-letter investigation, cross-region traffic, and non-production
  environments belong in the cost model. Team time is material but is not proven to dominate here.

## 4. Cost controls and recommendation

For both options, measure requests, content sizes, duplicate imports, external call counts,
Web App utilization, and cost per successful saved import. Reuse eligible cached content and
bound retries. Track failed operations as well as successes.

For B, right-size the broker and Functions plan, bound payloads and retention, cap worker concurrency,
and budget for status/deduplication cleanup and controlled replay. Reuse deployment patterns without
assuming separate buses must share a resource or billing boundary.

**A is usually the lower-complexity starting point for low-volume, short retrieval. B can justify
extra spend through outage tolerance, burst handling, or background processing requirements.**
It is not automatically cheaper under bursts, nor automatically wasteful at low volume: business
requirements and measured total cost determine value.
