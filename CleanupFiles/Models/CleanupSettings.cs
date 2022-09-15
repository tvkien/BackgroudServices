namespace CleanupFiles.Models
{
    public class CleanupSettings
    {
        public bool IsHardDelete { get; set; }

        public int MaxDegreeOfParallelism { get; set; }

        public string[]? PathRoots { get; set; }

        public string[]? MatchCases { get; set; }

        public bool IsDeleteNpmCache { get; set; }

        public string NpmCacheFolder { get; set; }
    }
}