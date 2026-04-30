export SYNC_STORAGE_VERSION=$(grep -o 'SyncStorageVersion = "[^"]*"' Tms.Adapter.Core/SyncStorage/SyncStorageRunner.cs | cut -d'"' -f2)
