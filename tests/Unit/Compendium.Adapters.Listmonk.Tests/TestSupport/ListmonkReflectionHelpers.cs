// -----------------------------------------------------------------------
// <copyright file="ListmonkReflectionHelpers.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using Compendium.Adapters.Listmonk.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Compendium.Adapters.Listmonk.Tests.TestSupport;

/// <summary>
/// Reflection helpers that materialize the internal <c>ListmonkHttpClient</c> against a
/// caller-supplied <see cref="HttpMessageHandler"/>. Mirrors the established pattern used
/// by the LemonSqueezy adapter test project — the production adapter exposes only public
/// service interfaces, but the HTTP client itself has many endpoints (campaigns, lists,
/// templates) that are not reachable through those services.
/// </summary>
internal static class ListmonkReflectionHelpers
{
    private static readonly Assembly AdapterAssembly = typeof(ListmonkOptions).Assembly;

    public static readonly Type HttpClientType = AdapterAssembly.GetType(
        "Compendium.Adapters.Listmonk.Http.ListmonkHttpClient",
        throwOnError: true)!;

    /// <summary>
    /// Creates the internal <c>ListmonkHttpClient</c> with a custom <see cref="HttpMessageHandler"/>.
    /// </summary>
    public static object CreateHttpClient(
        HttpMessageHandler handler,
        ListmonkOptions? options = null)
    {
        options ??= new ListmonkOptions
        {
            BaseUrl = "https://listmonk.test/",
            Username = "admin",
            Password = "secret",
            DefaultListId = 1,
            TimeoutSeconds = 5
        };

        // The HttpClient's BaseAddress is overwritten by the constructor itself, but we still
        // hand in a fresh client so disposal is straightforward.
        var http = new HttpClient(handler);

        var optionsWrapper = Options.Create(options);
        var logger = CreateNullLogger(HttpClientType);

        return Activator.CreateInstance(
            HttpClientType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object[] { http, optionsWrapper, logger },
            culture: null)!;
    }

    /// <summary>
    /// Invokes a method on a target by name and unwraps the returned <see cref="Task"/> /
    /// <c>Task&lt;T&gt;</c> into the awaited result. Disambiguates overloads by matching
    /// the parameter list against the supplied arguments (only non-generic methods are
    /// considered, so private generic helpers like <c>GetListAsync&lt;T&gt;</c> never collide
    /// with their public overloads).
    /// </summary>
    public static async Task<object?> InvokeAsync(
        object instance,
        string methodName,
        params object?[] args)
    {
        var method = ResolveMethod(instance.GetType(), methodName, args);
        var task = (Task)method.Invoke(instance, args)!;
        await task.ConfigureAwait(false);
        var resultProp = task.GetType().GetProperty("Result");
        return resultProp?.GetValue(task);
    }

    private static MethodInfo ResolveMethod(Type type, string name, object?[] args)
    {
        var candidates = type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(m => m.Name == name && !m.IsGenericMethod && m.GetParameters().Length == args.Length)
            .ToList();

        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        // Multiple overloads: pick the one whose parameter types are assignable from each arg.
        foreach (var candidate in candidates)
        {
            var parameters = candidate.GetParameters();
            var ok = true;
            for (var i = 0; i < parameters.Length; i++)
            {
                var arg = args[i];
                if (arg is null)
                {
                    if (parameters[i].ParameterType.IsValueType
                        && Nullable.GetUnderlyingType(parameters[i].ParameterType) is null)
                    {
                        ok = false;
                        break;
                    }
                }
                else if (!parameters[i].ParameterType.IsAssignableFrom(arg.GetType()))
                {
                    ok = false;
                    break;
                }
            }

            if (ok)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            $"No non-generic '{name}' overload on {type.FullName} matches the supplied {args.Length} argument(s).");
    }

    /// <summary>
    /// Builds the request type from the adapter assembly using its full type name.
    /// </summary>
    public static object CreateRequest(string typeName, Action<object>? configure = null)
    {
        var type = AdapterAssembly.GetType(typeName, throwOnError: true)!;
        var instance = Activator.CreateInstance(type)!;
        configure?.Invoke(instance);
        return instance;
    }

    /// <summary>
    /// Reads a property value from the given instance using reflection.
    /// </summary>
    public static T? GetProperty<T>(object instance, string propertyName)
    {
        var prop = instance.GetType().GetProperty(propertyName)
            ?? throw new InvalidOperationException($"Property '{propertyName}' not found on {instance.GetType().FullName}");
        var value = prop.GetValue(instance);
        return value is null ? default : (T)value;
    }

    private static object CreateNullLogger(Type forType)
    {
        var loggerType = typeof(NullLogger<>).MakeGenericType(forType);
        return loggerType.GetField("Instance", BindingFlags.Public | BindingFlags.Static)!
            .GetValue(null)!;
    }
}
