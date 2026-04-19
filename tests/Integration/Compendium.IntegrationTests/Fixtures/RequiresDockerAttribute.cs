// -----------------------------------------------------------------------
// <copyright file="RequiresDockerAttribute.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
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
