# Test IT Dotnet Integrations

The repository contains adapters for .NET test frameworks that integrate automated tests with the Test IT test management system.

## Compatibility

| Test IT | MSTest          | NUnit           | XUnit           | SpecFlow        |
|---------|-----------------|-----------------|-----------------|-----------------|
| 4.0     | 1.0             | 1.0             | 1.0             | 1.0             |
| 4.5     | 1.1             | 1.1             | 1.1             | 1.1             |
| 4.6     | 1.4             | 1.4             | 1.4             | 1.4             |
| 5.0     | 1.6             | 1.6             | 1.6             | 1.6             |
| 5.2     | 1.7             | 1.7             | 1.7             | 1.7             |
| 5.3     | 1.9.2-TMS-5.3   | 1.9.2-TMS-5.3   | 1.9.2-TMS-5.3   | 1.9.2-TMS-5.3   |
| 5.4     | 1.9.6-TMS-5.4   | 1.9.6-TMS-5.4   | 1.9.6-TMS-5.4   | 1.9.6-TMS-5.4   |
| 5.5     | 1.10.1-TMS-5.5  | 1.10.1-TMS-5.5  | 1.10.1-TMS-5.5  | 1.10.1-TMS-5.5  |
| 5.6     | 1.12.0-TMS-5.6  | 1.12.0-TMS-5.6  | 1.12.0-TMS-5.6  | 1.12.0-TMS-5.6  |
| Cloud   | 1.12.0 +        | 1.12.0 +        | 1.12.0 +        | 1.12.0 +        |

1. For current versions, see the releases tab. 
2. Starting with 5.2, we have added a TMS postscript, which means that the utility is compatible with a specific enterprise version. 
3. If you are in doubt about which version to use, check with the support staff. support@yoonion.ru

## Project Structure

This repository contains several components:

1. **Core Components** (`Tms.Adapter.Core`) - Shared functionality used by all adapters including:
   - TMS API Client for communication with Test IT
   - Adapter Manager for controlling test lifecycle
   - Data models for test information
   - Metadata attributes for linking tests to Test IT entities

2. **Framework-Specific Adapters**:
   - [MsTest/NUnit](https://github.com/testit-tms/adapters-dotnet/tree/main/Tms.Adapter) - Adapter for MSTest and NUnit frameworks
   - [XUnit](https://github.com/testit-tms/adapters-dotnet/tree/main/Tms.Adapter.XUnit) - Adapter for XUnit framework
   - [SpecFlow](https://github.com/testit-tms/adapters-dotnet/tree/main/Tms.Adapter.SpecFlowPlugin) - Plugin for SpecFlow framework

3. **TmsRunner** - A standalone test runner for MSTest/NUnit tests that can send results directly to Test IT


<a href='https://coveralls.io/github/testit-tms/adapters-dotnet?branch=main'>
	<img src='https://coveralls.io/repos/github/testit-tms/adapters-dotnet/badge.svg?branch=main' alt='Coverage Status' />
</a>
