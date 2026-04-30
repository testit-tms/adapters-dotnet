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
| 5.7     | 2.0.0-TMS-5.7   | 2.0.0-TMS-5.7   | 2.0.0-TMS-5.7   | 2.0.0-TMS-5.7   |
| Cloud   | 2.0.0 +         | 2.0.0 +         | 2.0.0 +         | 2.0.0 +         |

1. For current versions, see the releases tab. 
2. Starting with 5.2, we have added a TMS postscript, which means that the utility is compatible with a specific enterprise version. 
3. If you are in doubt about which version to use, check with the support staff. support@yoonion.ru


## What's new in 2.0.0?

- New logic with a fix for test results loading
- Added sync-storage subprocess usage for worker synchronization on port **49152** by defailt.
- importRealtime=false is a default mode (previously true)

### How to run 2.0+ locally?

You can change nothing, it's full compatible with previous versions of adapters for local run on all OS.


### How to run 2.0+ with CI/CD?

For CI/CD pipelines, we recommend starting the sync-storage instance before the adapter and waiting for its completion within the same job.

It can be OK for `adapterMode=2` and automatic creation of new test-run + call for `curl -v http://127.0.0.1:49152/wait-completion || true` in the end.  

There is a guide how to do everything with `adapterMode` `1` or `0`:

You can see how we implement this [here.](https://github.com/testit-tms/adapters-js/tree/main/.github/workflows/test.yml#143) 

- to get the latest version of sync-storage, please use our [script](https://github.com/testit-tms/adapters-js/tree/main/scripts/curl_last_version.sh)

- To download a specific version of sync-storage, use our [script](https://github.com/testit-tms/adapters-js/tree/main/scripts/get_sync_storage.sh) and pass the desired version number as the first parameter. Sync-storage will be downloaded as `.caches/syncstorage-linux-amd64`

1. Create an empty test run using `testit-cli` or use an existing one, and save the `testRunId`.
1.1 (alternative) You can use `curl + jq` to create empty test run, there is an example for github actions:

```bash
mkdir -p "$(dirname "${{ env.TEMP_FILE }}")"
BASE_URL="${{ env.TMS_URL }}"
BASE_URL="${BASE_URL%/}"
BODY=$(jq -nc \
    --arg projectId "${{ env.TMS_PROJECT_ID }}" \
    --arg name "${{ env.TMS_TEST_RUN_NAME }}" \
    '{projectId: $projectId, name: $name}')

curl -sS -f -X POST "${BASE_URL}/api/v2/testRuns" \
    -H "accept: application/json" \
    -H "Content-Type: application/json" \
    -H "Authorization: PrivateToken ${{ env.TMS_PRIVATE_TOKEN }}" \
    -d "$BODY" \
    | jq -er '.id' > "${{ env.TEMP_FILE }}"

echo "TMS_TEST_RUN_ID=$(<${{ env.TEMP_FILE }})" >> $GITHUB_ENV
echo "TMS_TEST_RUN_ID=$(<${{ env.TEMP_FILE }})" >> .env
export TMS_TEST_RUN_ID=$(<${{ env.TEMP_FILE }})
```

2. Start **sync-storage** with the correct parameters as a background process (alternatives to nohup can be used). Stream the log output to the `service.log` file:
```bash
nohup .caches/syncstorage-linux-amd64 --testRunId ${{ env.TMS_TEST_RUN_ID }} --port 49152 \
    --baseURL ${{ env.TMS_URL }} --privateToken ${{ env.TMS_PRIVATE_TOKEN }}  > service.log 2>&1 & 
```
3. Start the adapter using adapterMode=1 or adapterMode=0 for the selected testRunId.
4. Wait for sync-storage to complete background jobs by calling:
```bash
curl -v http://127.0.0.1:49152/wait-completion?testRunId=${{ env.TMS_TEST_RUN_ID }} || true
```
5. You can read the sync-storage logs from the service.log file.

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
