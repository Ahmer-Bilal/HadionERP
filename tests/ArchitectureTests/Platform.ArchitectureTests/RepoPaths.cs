namespace Platform.ArchitectureTests;

internal static class RepoPaths
{
    /// <summary>Walks up from the test assembly's output directory to find the repository root,
    /// identified by erp-platform.sln — makes these tests work regardless of who runs them or where
    /// the repo is cloned.</summary>
    public static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "erp-platform.sln")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            throw new InvalidOperationException(
                "Could not locate the repository root (erp-platform.sln) starting from " + AppContext.BaseDirectory);
        }

        return dir.FullName;
    }
}
