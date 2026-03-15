using SonyDevCleaner.App.Models;

namespace SonyDevCleaner.App.Services;

public abstract class CleanerTarget
{
    protected CleanerTarget(string id, string displayName, string description, bool requiresElevation = false)
    {
        Id = id;
        DisplayName = displayName;
        Description = description;
        RequiresElevation = requiresElevation;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public bool RequiresElevation { get; }

    public abstract Task<CleanerScanSummary> ScanAsync(CancellationToken cancellationToken);

    public abstract Task<CleanerExecutionResult> CleanAsync(CancellationToken cancellationToken);
}
