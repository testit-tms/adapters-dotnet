# Test IT Dotnet Integrations

The repository contains new versions of adaptors for dotnet test frameworks.

## Compatibility

| Test IT | MSTest | NUnit | XUnit |
|---------|--------|-------|-------|
| 4.0     | 1.0    | 1.0   | 1.0   |

Supported test frameworks :

1. [MsTest/NUnit](https://github.com/testit-tms/adapters-dotnet/tree/main/Tms.Adapter)
2. [XUnit](https://github.com/testit-tms/adapters-dotnet/tree/main/Tms.Adapter.XUnit)
3. [SpecFlow](https://github.com/testit-tms/adapters-dotnet/tree/main/Tms.Adapter.SpecFlowPlugin)

## Environment

Provide `ADAPTER_AUTOTESTS_RERUN_COUNT` environment variable as integer to retry failed tests N times.
