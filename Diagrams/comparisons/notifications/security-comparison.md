# Security: Synchronous vs Asynchronous Notifications

This is a planned-architecture tradeoff analysis using the
[WAF Security pillar](https://learn.microsoft.com/azure/well-architected/security/), not a
vulnerability assessment. See the [notification overview](./sync-async-comparison.md) and
[workspace](../../structurizr/workspace/workspace.dsl).

## Trust boundaries

| Dimension | A: direct provider calls | B: Notification Bus and processor |
| --- | --- | --- |
| Provider credentials | Needed by Web App and Content Notification Function | Can be confined to Notification Processor Function |
| Permissions | Both producers authorized to send through provider | Producers send to bus; processor receives and invokes provider |
| Durable data | Application records, provider records, and logs as designed | Also queued/dead-letter requests and delivery/deduplication records |
| Abuse surface | Unauthorized or repeated direct sends | Also unauthorized publishing, replay, and consumer misuse |

Recipients, contact details, preferences, message content, and even identifiers can be sensitive.
Neither alternative is automatically secure, and shared user Entra sign-in does not establish
service-to-service authorization.

## Required controls

- Authorize who may trigger each notification and which recipients/content are allowed. The
  processor must validate messages rather than treating any queue payload as trusted instructions.
- Keep provider credentials out of messages and logs. Use service identities where supported,
  otherwise protect and rotate provider secrets.
- For B, separate send and receive permissions and isolate the processor's provider access.
  Select broker network and encryption controls appropriate to its product and tier.
- Minimize payload data. References may reduce stored personal data, but need authorized lookup
  and introduce a dependency on the source; no lookup component is selected by the current model.
- Define how current opt-outs and preferences affect delayed sends. Prevent replay from bypassing
  those rules or generating duplicate notifications.
- Protect templates, recipient selection, and any links or user-provided content against misuse.
  Apply rate limits and validate payload sizes in both designs.
- Restrict dead-letter inspection and replay. Explicitly govern retention and disposal; do not
  assume ordinary queue TTL automatically purges dead-letter messages.
- Audit producer identity, notification ID, processing outcome, and replay actions without exposing
  unnecessary recipient details or full message content.

## Recommendation

A has fewer boundaries to configure; B can reduce provider-credential exposure but adds a durable
message store and consumer authorization responsibilities. Choose based on data sensitivity and
the ability to protect the complete delivery path, not component count alone.
