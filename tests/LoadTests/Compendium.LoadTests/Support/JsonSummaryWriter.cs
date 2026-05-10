// -----------------------------------------------------------------------
// <copyright file="JsonSummaryWriter.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Compendium.LoadTests.Support;

/// <summary>
/// Writes a per-scenario JSON summary file derived from NBomber's
/// <c>NodeStats</c> result. We avoid serialising the raw object because
/// <c>NodeStats.NodeInfo.NodeType</c> is an F# discriminated union which
/// <see cref="JsonSerializer"/> cannot handle out of the box. Instead we
/// reflect over a curated set of fields and build a plain CLR object.
/// </summary>
public static class JsonSummaryWriter
{
    /// <summary>
    /// Writes <c><![CDATA[<artifacts>/<scenario>.json]]></c> next to NBomber's
    /// own reports. Failures are logged to stderr and a minimal envelope is
    /// written instead so callers still get a file at the expected path.
    /// </summary>
    public static void Write(ScenarioOptions options, object? nbomberResult)
    {
        ArgumentNullException.ThrowIfNull(options);

        var path = Path.Combine(options.ArtifactsFolder, $"{options.Scenario}.json");
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        try
        {
            var nodeStats = ExtractNodeStats(nbomberResult);

            // Try to serialise the raw NodeStats first — it carries every
            // metric NBomber computed. The only known landmine is the
            // F#-discriminated-union NodeType field, which we silence with
            // a custom converter.
            jsonOptions.Converters.Add(new ToStringConverter("NBomber.Contracts.Stats.NodeType"));
            jsonOptions.Converters.Add(new ToStringConverter("NBomber.Contracts.Cluster"));

            var envelope = new
            {
                scenario = options.Scenario,
                duration = options.Duration.ToString(),
                warmup = options.Warmup,
                generatedAtUtc = DateTimeOffset.UtcNow,
                summary = ExtractScenarioSummaries(nodeStats),
                raw = nodeStats,
            };

            File.WriteAllText(path, JsonSerializer.Serialize(envelope, jsonOptions));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: failed to write JSON summary to {path}: {ex.Message}");
            File.WriteAllText(path, JsonSerializer.Serialize(new
            {
                scenario = options.Scenario,
                duration = options.Duration.ToString(),
                error = ex.Message,
            }, jsonOptions));
        }
    }

    /// <summary>
    /// NBomber 6's <c>NBomberRunner.Run</c> returns either <c>NodeStats</c>
    /// directly or a <c>Result&lt;NodeStats, IDomainError&gt;</c> wrapper
    /// depending on the build. Walk both shapes via reflection.
    /// </summary>
    private static object? ExtractNodeStats(object? raw)
    {
        if (raw is null)
        {
            return null;
        }

        var type = raw.GetType();

        // F# Result.Ok shape — has IsOk + ResultValue.
        var isOk = type.GetProperty("IsOk", BindingFlags.Public | BindingFlags.Instance);
        if (isOk?.GetValue(raw) is true)
        {
            var value = type.GetProperty("ResultValue", BindingFlags.Public | BindingFlags.Instance);
            if (value is not null)
            {
                return value.GetValue(raw);
            }
        }

        // Already a NodeStats — has ScenarioStats.
        if (type.GetProperty("ScenarioStats") is not null)
        {
            return raw;
        }

        return raw;
    }

    private static List<object> ExtractScenarioSummaries(object? nodeStats)
    {
        var summaries = new List<object>();
        if (nodeStats is null)
        {
            return summaries;
        }

        var scenarios = nodeStats.GetType().GetProperty("ScenarioStats")?.GetValue(nodeStats) as System.Collections.IEnumerable;
        if (scenarios is null)
        {
            return summaries;
        }

        foreach (var s in scenarios)
        {
            if (s is null)
            {
                continue;
            }

            var stType = s.GetType();
            var name = stType.GetProperty("ScenarioName")?.GetValue(s)?.ToString();
            var duration = stType.GetProperty("Duration")?.GetValue(s)?.ToString();

            // NBomber renamed counters between minor versions; collect any
            // public count-like property so the JSON stays useful regardless.
            var counts = new Dictionary<string, long>();
            var rates = new Dictionary<string, double>();
            foreach (var prop in stType.GetProperties())
            {
                var v = prop.GetValue(s);
                if (v is null)
                {
                    continue;
                }

                if ((prop.PropertyType == typeof(int) || prop.PropertyType == typeof(long)) &&
                    (prop.Name.EndsWith("Count", StringComparison.Ordinal) ||
                     prop.Name.EndsWith("Bytes", StringComparison.Ordinal)))
                {
                    counts[prop.Name] = ToInt64(v);
                }
                else if (prop.PropertyType == typeof(double) || prop.PropertyType == typeof(float))
                {
                    rates[prop.Name] = ToDouble(v);
                }
            }

            // In NBomber 6 the scenario-level Ok / Fail measurements live
            // directly on ScenarioStats; StepStats is non-empty only when
            // the scenario uses Step.Run(...). Extract both shapes so the
            // JSON works whichever style the scenario uses.
            var ok = ExtractMetric(stType.GetProperty("Ok")?.GetValue(s));
            var fail = ExtractMetric(stType.GetProperty("Fail")?.GetValue(s));

            var stepStats = stType.GetProperty("StepStats")?.GetValue(s) as System.Collections.IEnumerable;
            var steps = new List<object>();
            if (stepStats is not null)
            {
                foreach (var step in stepStats)
                {
                    if (step is null)
                    {
                        continue;
                    }

                    var stepType = step.GetType();
                    var stepName = stepType.GetProperty("StepName")?.GetValue(step)?.ToString();
                    var stepOk = ExtractMetric(stepType.GetProperty("Ok")?.GetValue(step));
                    var stepFail = ExtractMetric(stepType.GetProperty("Fail")?.GetValue(step));

                    steps.Add(new
                    {
                        stepName,
                        ok = stepOk,
                        fail = stepFail,
                    });
                }
            }

            summaries.Add(new
            {
                scenario = name,
                duration,
                counters = counts,
                rates,
                ok,
                fail,
                steps,
            });
        }

        return summaries;
    }

    private static object? ExtractMetric(object? metric)
    {
        if (metric is null)
        {
            return null;
        }

        var t = metric.GetType();
        var request = t.GetProperty("Request")?.GetValue(metric);
        var latency = t.GetProperty("Latency")?.GetValue(metric);
        var data = t.GetProperty("DataTransfer")?.GetValue(metric);

        return new
        {
            request = ExtractRequest(request),
            latency = ExtractLatency(latency),
            dataTransfer = ExtractDataTransfer(data),
        };
    }

    private static object? ExtractRequest(object? r)
    {
        if (r is null)
        {
            return null;
        }

        var t = r.GetType();
        return new
        {
            count = ToInt64(t.GetProperty("Count")?.GetValue(r)),
            rps = ToDouble(t.GetProperty("RPS")?.GetValue(r)),
        };
    }

    private static object? ExtractLatency(object? l)
    {
        if (l is null)
        {
            return null;
        }

        var t = l.GetType();
        return new
        {
            minMs = ToDouble(t.GetProperty("MinMs")?.GetValue(l)),
            meanMs = ToDouble(t.GetProperty("MeanMs")?.GetValue(l)),
            maxMs = ToDouble(t.GetProperty("MaxMs")?.GetValue(l)),
            stdDev = ToDouble(t.GetProperty("StdDev")?.GetValue(l)),
            p50 = ToDouble(t.GetProperty("Percent50")?.GetValue(l)),
            p75 = ToDouble(t.GetProperty("Percent75")?.GetValue(l)),
            p95 = ToDouble(t.GetProperty("Percent95")?.GetValue(l)),
            p99 = ToDouble(t.GetProperty("Percent99")?.GetValue(l)),
        };
    }

    private static object? ExtractDataTransfer(object? d)
    {
        if (d is null)
        {
            return null;
        }

        var t = d.GetType();
        return new
        {
            minBytes = ToInt64(t.GetProperty("MinBytes")?.GetValue(d)),
            meanBytes = ToInt64(t.GetProperty("MeanBytes")?.GetValue(d)),
            maxBytes = ToInt64(t.GetProperty("MaxBytes")?.GetValue(d)),
            allBytes = ToInt64(t.GetProperty("AllBytes")?.GetValue(d)),
        };
    }

    private static long ToInt64(object? value)
    {
        return value switch
        {
            null => 0L,
            long l => l,
            int i => i,
            double d => (long)d,
            _ => Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture),
        };
    }

    /// <summary>
    /// Replaces any object whose type name starts with the given prefix with
    /// its <see cref="object.ToString"/>. Used to neutralise F# discriminated
    /// unions inside NBomber's <c>NodeStats</c> graph.
    /// </summary>
    private sealed class ToStringConverter : JsonConverterFactory
    {
        private readonly string _typeNamePrefix;

        public ToStringConverter(string typeNamePrefix)
        {
            _typeNamePrefix = typeNamePrefix;
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.FullName?.StartsWith(_typeNamePrefix, StringComparison.Ordinal) == true;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return (JsonConverter)Activator.CreateInstance(typeof(StringConverter<>).MakeGenericType(typeToConvert))!;
        }

        private sealed class StringConverter<T> : JsonConverter<T>
        {
            public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => default;

            public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value?.ToString() ?? string.Empty);
            }
        }
    }

    private static double ToDouble(object? value)
    {
        return value switch
        {
            null => 0d,
            double d => d,
            float f => f,
            long l => l,
            int i => i,
            _ => Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture),
        };
    }
}
