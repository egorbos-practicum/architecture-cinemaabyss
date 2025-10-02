using Proxy.Enums;

namespace Proxy.Abstractions;

public interface IRequestDistributor
{
    TargetService DetermineTargetService(bool gradualMigrationEnabled, int migrationPercent);
    Dictionary<string, object> GetDistributionStats();
    void ResetStats();
}