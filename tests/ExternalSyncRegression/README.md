# External sync runtime regression

This console project calls the application's actual `ExternalSyncWorker` and `OperationsController` methods, runs their SQL against PostgreSQL, and replaces only the outbound HTTP handler with an in-memory response sequence. It creates and drops a unique test schema containing the worker tables and minimal controller authorization fixtures. It does not read business records or call supplier endpoints.

Use a disposable database, supply its connection string only through `EXTERNAL_SYNC_TEST_CONNECTION`, and explicitly enable test schema creation:

```sh
EXTERNAL_SYNC_TEST_ALLOW_SCHEMA_CREATE=YES \
dotnet run --project tests/ExternalSyncRegression/ExternalSyncRegression.csproj
```

`EXTERNAL_SYNC_TEST_CONNECTION` must already be set securely in the environment. The runner does not print it. The test process needs permission to create and drop its own schema.

The 31 scenarios cover:

- No claim or HTTP send for unconfigured, failed, disabled and manual-import systems.
- HTTP 503/429/408, network interruption and timeout: requeue, backoff, new worker instance, successful retry, stable idempotency key and renewed request ID.
- Permanent HTTP 400 and exhausted attempts: terminal task, completion timestamp, no automatic resend, explicit connection revalidation required.
- Successful and failed in-flight requests cannot undo later operator configuration changes or connection-test failures.
- Old lease/request completions cannot overwrite a newer task attempt or close its connection gate.
- Same-address revalidation, with or without saving first, prevents an old task failure from overriding the new verification.
- Saving a replacement address while a connection test is in flight rejects its old receipt with HTTP 409 and leaves the replacement unverified.
- Overlapping connection tests preserve the latest-started test result, regardless of response order.
- Concurrent successful and terminally failed deliveries still close their original verification gate in either completion order.
- Timestamp fencing round-trips PostgreSQL microseconds and advances monotonically despite a backward clock; dry-run checks remain read-only, including manual-import mode.

Connection status records the previously verified outbound configuration. A transient task failure is recorded on the task and keeps the verification gate so its retry can proceed, including after a restart. A terminal failure changes the connection to `failed` only if its verification generation is still current. Saving a configuration and starting a real connection test reset it to `unconfigured` and advance its existing `更新时间` by at least one database microsecond. Completion requires that exact generation. Successful deliveries update their task without changing the administrator's verification generation, so they cannot mask another concurrent task's terminal failure. Retry logic never promotes an unverified configuration.

These tests prove controller, worker and database behavior with a synthetic administrator identity and HTTP responses. They are not evidence of a real supplier connection or hospital acceptance.
