using CleanupFiles.Models;
using Quartz;
using System.Diagnostics;

namespace CleanupFiles
{
    public class CleanUpJob : IJob
    {
        private readonly ILogger<CleanUpJob> _logger;
        private readonly CleanupSettings _cleanupSettings;

        public CleanUpJob(ILogger<CleanUpJob> logger, CleanupSettings cleanupSettings)
        {
            _logger = logger;
            _cleanupSettings = cleanupSettings;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Job running at: {time}", DateTimeOffset.Now);
            await Task.Delay(100, context.CancellationToken);

            if (_cleanupSettings.IsHardDelete)
            {
                _cleanupSettings.PathRoots?
                    .AsParallel()
                    .WithDegreeOfParallelism(_cleanupSettings.MaxDegreeOfParallelism)
                    .ForAll(DeleteFolder(context.CancellationToken));
            }

            Thread.Sleep(3000);

            DeleteNPMCacheFolder(context.CancellationToken);

            Thread.Sleep(3000);
        }

        private Action<string> DeleteFolder(CancellationToken stoppingToken)
        {
            return rootPath =>
            {
                if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
                {
                    return;
                }

                Directory.GetDirectories(rootPath)
                    .Where(x => _cleanupSettings.MatchCases?.Any(c => x.Contains(c, StringComparison.InvariantCultureIgnoreCase)) ?? false)
                    .AsParallel()
                    .WithDegreeOfParallelism(_cleanupSettings.MaxDegreeOfParallelism)
                    .ForAll(Delete(stoppingToken));
            };
        }

        private Action<string> Delete(CancellationToken stoppingToken)
        {
            return pathFolder =>
            {
                if (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        _logger.LogInformation($"Delete folder {pathFolder}");

                        var watch = Stopwatch.StartNew();
                        Directory.Delete(pathFolder, true);
                        watch.Stop();

                        _logger.LogInformation($"Delete folder {pathFolder} take {watch.Elapsed.Duration()}");
                        Thread.Sleep(3000);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(message: $"Failed to delete folder {pathFolder}");
                        _logger.LogError(ex, message: ex.Message);
                    }
                }
            };
        }

        private void DeleteNPMCacheFolder(CancellationToken stoppingToken)
        {
            if (!_cleanupSettings.IsDeleteNpmCache)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_cleanupSettings.NpmCacheFolder) || !Directory.Exists(_cleanupSettings.NpmCacheFolder))
            {
                return;
            }

            Directory.GetDirectories(_cleanupSettings.NpmCacheFolder)
                   .AsParallel()
                   .WithDegreeOfParallelism(_cleanupSettings.MaxDegreeOfParallelism)
                   .ForAll(Delete(stoppingToken));
        }
    }
}
