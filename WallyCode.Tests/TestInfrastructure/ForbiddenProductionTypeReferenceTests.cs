using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace WallyCode.Tests.TestInfrastructure;

/// <summary>
/// Static-analysis safeguard for the testing safety policy (see WallyCode.Tests/README.md).
///
/// Tests must never instantiate real LLM providers, never call <c>ProviderRegistry.Create</c>,
/// and never reach <c>GhProcess</c> (which shells out to <c>gh</c> / <c>copilot</c>). This test
/// inspects the test assembly's <c>TypeRef</c> table at the metadata level and fails if any test
/// code references a forbidden production type.
///
/// This lives entirely in the test project — production code has no awareness of testing.
///
/// If this test fails, do NOT add the offending type to the allowlist. Fix the test that reaches
/// for real infrastructure, and use <see cref="MockLlmProvider"/> + <see cref="CliHarness"/> instead.
/// </summary>
public class ForbiddenProductionTypeReferenceTests
{
    /// <summary>
    /// Fully-qualified production type names that test assemblies must never reference.
    /// Add to this list when a new "real provider" or "real external process" entry point appears.
    ///
    /// Note: <c>GhCopilotCliProvider</c> is intentionally NOT on this list. Constructing one is
    /// harmless (e.g. <c>ProviderRegistryTests</c> does so to verify registry shape). The actual
    /// chokepoint that shells out to <c>gh</c>/<c>copilot</c> is <c>GhProcess</c>, and every
    /// dangerous method on <c>GhCopilotCliProvider</c> routes through it. Banning <c>GhProcess</c>
    /// references is therefore both necessary and sufficient.
    /// </summary>
    private static readonly string[] ForbiddenTypeFullNames =
    [
        "WallyCode.ConsoleApp.Copilot.GhProcess",
        "WallyCode.ConsoleApp.Copilot.GhProcess+GhResult",
    ];

    [Fact]
    public void Test_assembly_does_not_reference_real_provider_or_gh_process_types()
    {
        var testAssemblyPath = typeof(ForbiddenProductionTypeReferenceTests).Assembly.Location;
        Assert.True(File.Exists(testAssemblyPath), $"Test assembly not found at {testAssemblyPath}.");

        var referencedTypes = ReadTypeReferences(testAssemblyPath);

        var violations = ForbiddenTypeFullNames
            .Where(forbidden => referencedTypes.Contains(forbidden))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        if (violations.Count > 0)
        {
            var message =
                "The test assembly references forbidden production types. Tests must use MockLlmProvider "
                + "and never reach real gh/copilot infrastructure. See WallyCode.Tests/README.md."
                + Environment.NewLine
                + "Forbidden references found:" + Environment.NewLine
                + string.Join(Environment.NewLine, violations.Select(v => "  - " + v));
            Assert.Fail(message);
        }
    }

    [Fact]
    public void Test_assembly_does_not_call_ProviderRegistry_Create()
    {
        // ProviderRegistry itself is allowed (tests construct it directly with a MockLlmProvider).
        // The forbidden surface is the static factory ProviderRegistry.Create(logger), which loads
        // real provider definitions from disk. Catch it by scanning each method body's IL for a
        // call/callvirt token whose member name is "Create" on the ProviderRegistry type.
        var assembly = typeof(ForbiddenProductionTypeReferenceTests).Assembly;

        var offenders = new List<string>();

        foreach (var type in SafelyEnumerateTypes(assembly))
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (MethodBodyCallsProviderRegistryCreate(method))
                {
                    offenders.Add($"{type.FullName}.{method.Name}");
                }
            }
        }

        if (offenders.Count > 0)
        {
            Assert.Fail(
                "Tests must never call ProviderRegistry.Create(...). Construct a ProviderRegistry "
                + "directly with a MockLlmProvider, or use CliHarness.Create(...). Offenders:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, offenders.Select(o => "  - " + o)));
        }
    }

    private static HashSet<string> ReadTypeReferences(string assemblyPath)
    {
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var metadata = peReader.GetMetadataReader();

        var results = new HashSet<string>(StringComparer.Ordinal);

        foreach (var handle in metadata.TypeReferences)
        {
            var typeRef = metadata.GetTypeReference(handle);
            var ns = metadata.GetString(typeRef.Namespace);
            var name = metadata.GetString(typeRef.Name);
            var fullName = string.IsNullOrEmpty(ns) ? name : ns + "." + name;

            // Nested type references show up with the parent as ResolutionScope; reconstruct
            // the "Outer+Inner" form so it matches Type.FullName conventions.
            if (typeRef.ResolutionScope.Kind == HandleKind.TypeReference)
            {
                var parent = metadata.GetTypeReference((TypeReferenceHandle)typeRef.ResolutionScope);
                var parentNs = metadata.GetString(parent.Namespace);
                var parentName = metadata.GetString(parent.Name);
                var parentFullName = string.IsNullOrEmpty(parentNs) ? parentName : parentNs + "." + parentName;
                fullName = parentFullName + "+" + name;
            }

            results.Add(fullName);
        }

        return results;
    }

    private static IEnumerable<Type> SafelyEnumerateTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }

    private static bool MethodBodyCallsProviderRegistryCreate(MethodBase method)
    {
        MethodBody? body;
        try
        {
            body = method.GetMethodBody();
        }
        catch
        {
            return false;
        }

        if (body is null)
        {
            return false;
        }

        var il = body.GetILAsByteArray();
        if (il is null || il.Length == 0)
        {
            return false;
        }

        var module = method.Module;

        // Scan for call (0x28) / callvirt (0x6F) opcodes followed by a 4-byte method token.
        // This is intentionally conservative; a false positive here is a feature (forces the
        // author to either remove the call or update the safeguard intentionally).
        for (var i = 0; i < il.Length; i++)
        {
            var opcode = il[i];
            if (opcode != 0x28 && opcode != 0x6F)
            {
                continue;
            }

            if (i + 4 >= il.Length)
            {
                break;
            }

            var token = BitConverter.ToInt32(il, i + 1);
            try
            {
                var resolved = module.ResolveMethod(token);
                if (resolved?.DeclaringType?.FullName == "WallyCode.ConsoleApp.Copilot.ProviderRegistry"
                    && resolved.Name == "Create")
                {
                    return true;
                }
            }
            catch
            {
                // Tokens that fail to resolve (generics, varargs, cross-module refs) are not our concern.
            }

            i += 4;
        }

        return false;
    }
}
