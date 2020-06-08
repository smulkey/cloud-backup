namespace CloudBackupClient
{
    public static class BackupClientConfigurationKeys
    {
        public static readonly string BackupSettings = "BackupSettings";

        public static readonly string BackupClientID = "BackupClientID";

        public static readonly string RootConfigurationSection = "BackupSettings";

        public static readonly string RunTimeLimitSeconds = "RunTimeLimitSeconds";

        public static readonly string BackupDirectories = "BackupDirectories";

        public static readonly string LocalCacheConfigSettingsSectionName = "LocalClientFileCacheConfig";

        public static readonly string MaxCachePerRunMB = "MaxCachePerRunMB";

        public static readonly string MaxTotalCacheSizeGB = "MaxTotalCacheSizeGB";        

        public static readonly string TempCopyDirectory = "TempCopyDirectory";
    }
}
