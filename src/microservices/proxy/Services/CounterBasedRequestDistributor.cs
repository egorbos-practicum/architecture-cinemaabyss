using Proxy.Abstractions;
using Proxy.Enums;

namespace Proxy.Services;

public class CounterBasedRequestDistributor : IRequestDistributor
{
    private long _totalRequests;
    private long _moviesRequests;
    private long _monolithRequests;
    private readonly object _lockObject = new();

    public TargetService DetermineTargetService(bool gradualMigrationEnabled, int migrationPercent)
    {
        TargetService targetService;

        lock (_lockObject)
        {
            _totalRequests++;
            
            if (!gradualMigrationEnabled)
            {
                _monolithRequests++;
                return TargetService.Monolith;
            }

            var currentMoviesPercent = (double)_moviesRequests / _totalRequests * 100;

            if (currentMoviesPercent < migrationPercent)
            {
                _moviesRequests++;
                targetService = TargetService.MoviesService;
            }
            else
            {
                _monolithRequests++;
                targetService = TargetService.Monolith;
            }
        }

        return targetService;
    }

    public Dictionary<string, object> GetDistributionStats()
    {
        lock (_lockObject)
        {
            var moviesPercent = _totalRequests > 0 ? (double)_moviesRequests / _totalRequests * 100 : 0;
            var monolithPercent = _totalRequests > 0 ? (double)_monolithRequests / _totalRequests * 100 : 0;
            return new Dictionary<string, object>
            {
                ["TotalRequests"] = _totalRequests,
                ["MoviesRequests"] = _moviesRequests,
                ["MonolithRequests"] = _monolithRequests,
                ["MoviesPercentage"] = Math.Round(moviesPercent, 2),
                ["MonolithPercentage"] = Math.Round(monolithPercent, 2)
            };
        }
    }

    public void ResetStats()
    {
        lock (_lockObject)
        {
            _totalRequests = 0;
            _moviesRequests = 0;
            _monolithRequests = 0;
        }
    }
}