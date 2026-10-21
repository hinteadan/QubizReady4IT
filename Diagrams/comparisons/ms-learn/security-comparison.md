# Security: Synchronous vs Asynchronous Microsoft Learn Integration

This is an architectural tradeoff comparison using the
[WAF Security pillar](https://learn.microsoft.com/azure/well-architected/security/), not a vulnerability
assessment or a statement that controls are implemented. See the
[overall comparison](./sync-async-comparison.md) and [KnowledgeGram model](../../structurizr/workspace/workspace.dsl).

## 1. Trust boundaries

**A (`Containers`):** Web App authenticates to the external knowledge solution and handles retrieved
content. **B (`ContainersAsync`):** Web App sends to Learning Integration Bus; Learning Integration
Function consumes, calls Microsoft Learn, and submits results to an authenticated Web App interface.
Only the Web App writes the Content Database and Local File Storage.

Both clients and administrators are modeled against the same Entra provider. Entra integration
and administrator-specific authorization are planned, not current guarantees. The current
in-memory username session is not evidence of production authentication.

## 2. Security tradeoffs

| Dimension | A: synchronous | B: asynchronous |
| --- | --- | --- |
| Surface to secure | Web App, external access, and existing stores | Also broker, worker, message contracts, and result-submission endpoint |
| External credentials | Available to the Web App as required by the provider | Can be restricted to the learning function |
| Service permissions | Web App external access and storage | Web App send, function receive, function external access, and function callback permission |
| Durable data | Imported content/files and any logs/status | Also queued requests, failed/dead-letter messages, and deduplication/status records |
| Authorization context | Validate user permissions at request and persistence boundaries | Preserve ownership and authorize the deferred action/result; permissions may change while queued |
| Duplicate requests | Client retries can duplicate effects | Redelivery and callback retries add replay scenarios |
| Storage access | Web App owns writes | Same boundary; do not grant the learning function direct DB/file access |

Fewer components reduce the configuration surface, but **A is not automatically more secure**.
B can isolate external credentials and processing permissions, provided its additional boundaries
are secured. Idempotency prevents duplicate effects; it does not replace authentication or authorization.

## 3. Controls required in both alternatives

- Verify Microsoft Learn's actual integration/authentication contract and content licensing before
  implementing retrieval. Do not assume Entra or managed identity is accepted by the external API.
- Authenticate users and enforce roles/ownership server-side. Treat service-to-service identity
  separately from client/admin sign-in.
- Use protected transport for remote calls and configure protection for stored data. SQLite and
  local files are not automatically encrypted or access-controlled by their diagram labels.
- Validate retrieved content, size, type, and identifiers before serving or persisting it. Treat
  external content as untrusted and avoid accepting arbitrary fetch locations without validation.
- Restrict storage permissions, protect secrets, and govern access to logs, status, backups, and files.
  Knowledge content, identifiers, or preferences may be sensitive even though this is not a pension system.

## 4. Additional controls for the async alternative

- Separate send/receive permissions using least privilege. Use managed identities where supported
  by the chosen Azure services; scope callback access to the learning-import operation.
- Authenticate and authorize the function's result submission. Bind operation ID, intended owner,
  and allowed result to the original accepted request; do not trust ownership claims in a payload alone.
- Minimize message data, using bounded identifiers/references where practical. References still
  need authorization and may reveal sensitive metadata; avoid long-lived secrets in messages.
- Configure broker network access and transport/storage protection according to the chosen product
  and tier. Private connectivity is a deployment decision, not a property established by the DSL.
- Deduplicate imports and validate request freshness. Recheck relevant permissions at processing
  time where required, rather than assuming enqueue-time authorization remains valid forever.
- Restrict dead-letter inspection/replay and explicitly dispose of retained messages under policy.
  Do not assume ordinary message TTL automatically purges a dead-letter queue.
- Audit enqueue, processing, callback authorization, and persistence with correlation IDs, without
  unnecessarily copying full content or credentials into telemetry.

## 5. Recommendation

Choose between A's smaller integration surface and B's additional credential/processing isolation
based on data classification, authorization needs, and operational capability. Neither architecture
establishes secure authentication, storage encryption, or audit coverage on its own. B is appropriate
only when the broker, deferred authorization, and Web App callback are protected as well as the
external retrieval itself.
