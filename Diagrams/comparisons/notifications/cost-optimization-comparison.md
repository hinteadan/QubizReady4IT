# Cost Optimization: Synchronous vs Asynchronous Notifications

This applies the
[WAF Cost Optimization pillar](https://learn.microsoft.com/azure/well-architected/cost-optimization/)
to the alternatives in the [notification overview](./sync-async-comparison.md).
See the [workspace](../../structurizr/workspace/workspace.dsl) for the modeled containers.

## Cost boundary

Both alternatives retain the Web App, Content Notification Function, and Notification Service.
Preference matching through the Web App exists in both. B additionally uses Notification Bus and
Notification Processor Function; the separate Learning Integration Bus/function is excluded here.

| Cost | A: direct provider calls | B: queued delivery |
| --- | --- | --- |
| Infrastructure | Existing producers and provider | Also broker and processor compute |
| Provider charges | Depends on contract and notification volume | Same; duplicates/retries may increase billable attempts |
| Producer execution | Includes provider wait/retries | Includes publish work; provider wait/retries move to processor |
| Engineering | Provider integration, safe retries, telemetry | Also message contracts, deduplication, expiry, and recovery |
| Operations | Provider and producer monitoring | Also queue alerts, dead-letter triage, and replay |
| Data | Application/provider records as required | Also message retention and processing/status records |

## Cost controls

- Measure cost per logical notification and per accepted/delivered notification where measurable.
  Include failures, repeated scans, retries, duplicate sends, and provider fees.
- Bound retries and expire obsolete alerts. Avoid retry storms or uncontrolled replay after outages.
- Keep message payloads and telemetry retention proportionate to operational and compliance needs.
- Choose the actual broker and Functions hosting plan before estimating baseline or idle cost.
  The generic bus does not imply a particular Service Bus tier; Functions do not always scale
  to zero or incur no idle costs.
- Pace provider calls to avoid paid failures and excess retries, while meeting notification timeliness.
  Queue buffering does not make capacity based only on average load sufficient.
- Include development, non-production environments, secrets/networking, and operational ownership.
  Do not assume team cost or infrastructure cost dominates without evidence.

## Recommendation

A usually has lower incremental cost for a simple, low-volume flow. B adds cost but may be justified
by responsiveness, recovery, or burst requirements. Price the entire notification path and compare
business value; neither lower cost under bursts nor guaranteed savings from serverless is established.
