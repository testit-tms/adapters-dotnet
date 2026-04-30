# Sync Storage + XUnit + SpecFlow hardening (summary)

This document summarizes the recent reliability fixes and behavior changes around Sync Storage integration, the XUnit adapter lifecycle, and SpecFlow plugin tests.

## Sync Storage: `TestResultCutApiModel` correctness

### Centralized cut-model creation
Cut models sent to Sync Storage are now built via a single mapping in `Tms.Adapter.Core/Client/Converter.cs`:

- `statusCode`: adapter outcome as string (`Passed` / `Failed` / `Skipped` / ...). For `Undefined` it is normalized to `Passed` to match bindings behavior.
- `statusType`: derived from `statusCode` using the existing status mapping (`Succeeded` / `Failed` / `Incomplete` / `InProgress`).
- `startedOn`: mapped from the test start timestamp when available.

### `projectId` is mandatory
Sync Storage requires `projectId`, so the model constructor and mapping were tightened:

- `Tms.Adapter.Core/Client/Converter.cs`: `ToTestResultCutApiModel(...)` requires `projectId` and throws `ArgumentException` if it is empty.
- Call sites:
  - `Tms.Adapter.Core/Service/AdapterManager.cs`: if `ProjectId` is missing in config, Sync Storage “in_progress” is skipped with a warning.
  - `TmsRunner/Services/ProcessorService.cs`: in_progress is only sent when `ProjectId` is configured.

## XUnit: lifecycle ordering + duplicate `in_progress` prevention

### Ensure `OnRunningStarted()` is called early enough
In XUnit the worker status could reach `completed` too early if `OnRunningStarted()` was only triggered “late” (e.g., on first test case).

The XUnit message bus now hooks assembly-level messages:

- `ITestAssemblyStarting` → `OnRunningStarted()` (sets worker `in_progress`)
- `ITestAssemblyFinished` → `OnBlockCompleted()` (sets worker `completed`)

`ProcessExit` remains as a fallback to ensure cleanup on abrupt termination.

### Fix race condition: multiple `in_progress` sends
XUnit can run tests in parallel. Previously, `_isAlreadyInProgress` was set *after* a successful HTTP request, allowing multiple concurrent tests to pass the “not yet in progress” check and send multiple `in_progress` results.

`Tms.Adapter.Core/SyncStorage/SyncStorageRunner.cs` now uses an atomic reservation (`Interlocked.CompareExchange`) so only one test can claim the single in_progress slot at a time. If the HTTP call fails, the reservation is released to allow a later retry.

## SpecFlow plugin tests: hermetic (no config file, no network)

CI failures were caused by:

- missing `Tms.config.json` in test output (expected; config is provided via env in tests)
- accidental real network calls (e.g., `TMS_URL=https://example.com`) during unit tests
- cached `AdapterManager.Instance` references inside plugin code, preventing test isolation

Fixes:

### Disable networking for tests
`Tms.Adapter.Core/Service/AdapterManager.cs` supports a test-only switch:

- `TMS_DISABLE_NETWORK=true` forces a `NoopTmsClient` (no HTTP) and skips Sync Storage initialization.

`Tms.Adapter.SpecFlowPluginTests/Helper/TestsBase.cs` sets:

- `AdapterManager.ClearInstance()` per test
- `TMS_DISABLE_NETWORK=true`

### Avoid cached singleton instances in plugin
Static cached fields like `private static readonly AdapterManager Adapter = AdapterManager.Instance;` were replaced with a property accessor:

- `private static AdapterManager Adapter => AdapterManager.Instance;`

This ensures `ClearInstance()` in tests actually takes effect.

### Storage robustness for SpecFlow “root container”
Some SpecFlow flows reference a “root” container id that may not have been explicitly started. `AdapterManager.UpdateTestContainer(...)` now ensures such a container exists before applying updates.

### Disable test parallelization (SpecFlowPluginTests)
`Tms.Adapter.SpecFlowPluginTests/AssemblyInfo.cs` includes `[assembly: DoNotParallelize]` to prevent cross-test interference from shared static state.

## Logging: CA1873 fixes
Several `LogDebug(...)` calls in `Tms.Adapter.Core/Client/TmsClient.cs` were guarded by:

```csharp
if (_logger.IsEnabled(LogLevel.Debug))
{
    _logger.LogDebug(...);
}
```

This prevents potentially expensive argument evaluation (e.g., structured objects) when debug logging is disabled.

