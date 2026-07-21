namespace TEMO.AI;

internal sealed class VercelDeployService(string cwd)
{
    public async Task<List<VercelScopeOption>> LoadTeamsAsync()
    {
        if (VercelAuthStore.TryGetToken() is not { } token)
            return [];

        var apiScopes = await VercelApiClient.TryGetScopesAsync(token);
        return apiScopes ?? [];
    }

    public async Task<List<VercelProjectOption>> LoadProjectsAsync(VercelScopeOption scope)
    {
        if (VercelAuthStore.TryGetToken() is not { } token)
            return [];

        return await VercelApiClient.TryGetProjectsAsync(token, scope) ?? [];
    }

    public async Task<int> DeployAsync(string scope, string projectName, bool createNew,
        Action<string> onLog, CancellationToken ct = default)
    {
        if (createNew)
        {
            onLog($"$ vercel project add {projectName} --scope {scope}");
            var create = await VercelCli.CreateProjectAsync(cwd, scope, projectName, onLog);
            onLog($"Exit code: {create.ExitCode}\n");
            if (!create.Success) return create.ExitCode;
        }

        onLog($"$ vercel link --project {projectName} --scope {scope}");
        var link = await VercelCli.LinkAsync(cwd, scope, projectName, onLog);
        onLog($"Exit code: {link.ExitCode}\n");
        if (!link.Success) return link.ExitCode;

        onLog($"$ vercel deploy --prod --scope {scope}");
        var deploy = await VercelCli.DeployAsync(cwd, scope, onLog, ct);
        onLog($"Exit code: {deploy.ExitCode}");
        return deploy.ExitCode;
    }
}
