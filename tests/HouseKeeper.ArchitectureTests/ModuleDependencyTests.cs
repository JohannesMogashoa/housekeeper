using System.Reflection;

using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;

using HouseKeeper.Contracts.Households;
using HouseKeeper.Modules.Households;
using HouseKeeper.Modules.Households.Domain;

using Xunit;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace HouseKeeper.ArchitectureTests;

public sealed class ModuleDependencyTests
{
    private static Architecture Architecture { get; } = new ArchLoader()
        .LoadAssemblies(
            typeof(HouseholdName).Assembly,
            typeof(HouseholdSummary).Assembly,
            typeof(HouseKeeper.Web.App).Assembly)
        .Build();

    [Fact]
    public void HouseholdDomainDoesNotDependOnEntityFrameworkCore()
    {
        IArchRule rule = Types()
            .That()
            .ResideInNamespace("HouseKeeper.Modules.Households.Domain")
            .Should()
            .NotDependOnAny(
                Types()
                    .That()
                    .ResideInNamespace("Microsoft.EntityFrameworkCore"));

        rule.Check(Architecture);
    }

    [Fact]
    public void ContractsDoNotReferenceFeatureModules()
    {
        AssemblyName[] references = typeof(HouseholdSummary).Assembly
            .GetReferencedAssemblies();

        Assert.DoesNotContain(
            references,
            reference => reference.Name?.StartsWith(
                "HouseKeeper.Modules.",
                StringComparison.Ordinal) == true);
    }

    [Fact]
    public void WebDoesNotReferenceHouseholdsImplementation()
    {
        AssemblyName[] references = typeof(HouseKeeper.Web.App).Assembly
            .GetReferencedAssemblies();

        Assert.DoesNotContain(
            references,
            reference => reference.Name == typeof(HouseholdsModule).Assembly.GetName().Name);
    }

    [Fact]
    public void ContractsAndModulesDoNotReferenceAwsProviderAssemblies()
    {
        System.Reflection.Assembly[] applicationAssemblies =
        [
            typeof(HouseholdSummary).Assembly,
            typeof(HouseholdsModule).Assembly
        ];

        foreach (System.Reflection.Assembly assembly in applicationAssemblies)
        {
            Assert.DoesNotContain(
                assembly.GetReferencedAssemblies(),
                reference => reference.Name is not null &&
                    (reference.Name.StartsWith("Amazon.", StringComparison.Ordinal) ||
                     reference.Name.StartsWith("AWSSDK.", StringComparison.Ordinal)));
        }
    }
}
