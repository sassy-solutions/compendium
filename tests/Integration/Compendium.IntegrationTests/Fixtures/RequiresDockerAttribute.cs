// -----------------------------------------------------------------------
// <copyright file="RequiresDockerAttribute.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Xunit;

namespace Compendium.IntegrationTests.Fixtures;

/// <summary>
/// Custom Fact attribute that skips the test when Docker is not available.
/// Uses the same detection as TestContainers.
/// </summary>
public sealed class RequiresDockerFactAttribute : FactAttribute
{
    public RequiresDockerFactAttribute()
    {
        if (!DockerDetection.IsDockerAvailable)
        {
            Skip = "Docker is not running or misconfigured. Start Docker to run integration tests.";
        }
    }
}

/// <summary>
/// Docker availability detection cache.
/// </summary>
internal static class DockerDetection
{
    private static readonly Lazy<bool> _isAvailable = new(DetectDocker);

    public static bool IsDockerAvailable => _isAvailable.Value;

    private static bool DetectDocker()
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "info",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
