# Unit Test Checklist (xUnit, AutoFixture, FluentAssertions, NSubstitute, coverlet)

## Structure
- [ ] Create `tests` at solution root mirroring `/src` folders and namespaces.
- [ ] For each `/src/{ProjectName}`, create `tests/{ProjectName}.Tests`.
- [ ] Mirror folders: `Services`, `Controllers`, `Repositories`, `Domain`, `Utilities`, etc.
- [ ] Map files: `/src/Foo/Services/BarService.cs` → `/tests/Foo.Tests/Services/BarServiceTests.cs`.

## Dependencies
- [ ] Add packages to each test project:
  - `dotnet add {ProjectName}.Tests package xunit`
  - `dotnet add {ProjectName}.Tests package xunit.runner.visualstudio`
  - `dotnet add {ProjectName}.Tests package AutoFixture`
  - `dotnet add {ProjectName}.Tests package FluentAssertions`
  - `dotnet add {ProjectName}.Tests package NSubstitute`
  - `dotnet add {ProjectName}.Tests package coverlet.collector`
- [ ] Optional: use MSBuild coverlet instead of collector
  - `dotnet add {ProjectName}.Tests package coverlet.msbuild`

## Test Suites
- [ ] Use `[Fact]` tests; iterate in-method datasets for multiple cases.
- [ ] Create three suites per subject:
  - Normal Data: typical valid inputs, expected outputs and interactions.
  - Abnormal Data: invalid inputs, exceptions, nulls, misconfigurations.
  - Boundary Data: min/max, empty, one-item, thresholds, precision edges.
- [ ] For small sets, define tuples/arrays in the test and iterate.
- [ ] For complex datasets, extract helpers/static datasets and iterate.
- [ ] Use `AutoFixture` inside tests to generate objects and reduce boilerplate.
- [ ] Use `NSubstitute` to mock dependencies and verify interactions.
- [ ] Use `FluentAssertions` for expressive assertions.

## Template
```csharp
using AutoFixture;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Foo.Tests.Services;

public class BarServiceTests
{
    [Fact]
    public void NormalData_Add_ReturnsExpected()
    {
        var cases = new[]
        {
            (2, 3, 5),
            (10, 20, 30),
            (-1, 4, 3)
        };

        var dep = Substitute.For<IDependency>();
        foreach (var (a, b, expected) in cases)
        {
            dep.Compute(a, b).Returns(expected);
            var sut = new BarService(dep);
            var result = sut.Add(a, b);
            result.Should().Be(expected);
            dep.Received(1).Compute(a, b);
            dep.ClearReceivedCalls();
        }
    }

    [Fact]
    public void BoundaryData_Add_HandlesEdges()
    {
        var cases = new[]
        {
            (int.MaxValue, 1),
            (int.MinValue, -1)
        };

        var sut = new BarService(Substitute.For<IDependency>());
        foreach (var (a, b) in cases)
        {
            Action act = () => sut.Add(a, b);
            act.Should().NotThrow();
        }
    }

    [Fact]
    public void AbnormalData_Add_InvalidInputs_Throws()
    {
        var sut = new BarService(Substitute.For<IDependency>());
        var cases = new[]
        {
            (0, 0),
            (-100, 0)
        };

        foreach (var (a, b) in cases)
        {
            Action act = () => sut.Add(a, b);
            act.Should().Throw<ArgumentException>();
        }
    }
}
```

## Dataset Example
```csharp
public static class BarData
{
    public static (int a, int b, int expected)[] Normal =>
        new[]
        {
            (1, 2, 3),
            (100, 200, 300)
        };

    public static (int a, int b)[] Boundary =>
        new[]
        {
            (int.MaxValue, 0),
            (int.MinValue, 0)
        };

    public static (int a, int b)[] Abnormal =>
        new[]
        {
            (0, 0),
            (-1, -1)
        };
}

public class BarServiceFactSets
{
    [Fact]
    public void Normal()
    {
        var dep = Substitute.For<IDependency>();
        foreach (var (a, b, expected) in BarData.Normal)
        {
            dep.Compute(a, b).Returns(expected);
            var sut = new BarService(dep);
            sut.Add(a, b).Should().Be(expected);
            dep.ClearReceivedCalls();
        }
    }

    [Fact]
    public void Boundary()
    {
        var sut = new BarService(Substitute.For<IDependency>());
        foreach (var (a, b) in BarData.Boundary)
        {
            Action act = () => sut.Add(a, b);
            act.Should().NotThrow();
        }
    }

    [Fact]
    public void Abnormal()
    {
        var sut = new BarService(Substitute.For<IDependency>());
        foreach (var (a, b) in BarData.Abnormal)
        {
            Action act = () => sut.Add(a, b);
            act.Should().Throw<ArgumentException>();
        }
    }
}
```

## Coverage
- [ ] Enable coverage collection via collector:
  - `dotnet test --collect:"XPlat Code Coverage"`
- [ ] Or via MSBuild:
  - `dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura`
- [ ] Optional thresholds:
  - `dotnet test /p:CollectCoverage=true /p:Threshold=80 /p:ThresholdType=line /p:ThresholdStat=total`

## 100% Coverage Requirements
- [ ] Enforce minimum total coverage at 100% (lines):
  - `dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Threshold=100 /p:ThresholdType=line /p:ThresholdStat=total`
- [ ] Optionally enforce branch coverage for critical modules:
  - `dotnet test /p:CollectCoverage=true /p:Threshold=100 /p:ThresholdType=branch /p:ThresholdStat=total`
- [ ] Add MSBuild defaults in test projects (optional):
  - In `{ProjectName}.Tests.csproj` add:
    ```xml
    <PropertyGroup>
      <CollectCoverage>true</CollectCoverage>
      <CoverletOutputFormat>cobertura</CoverletOutputFormat>
      <Threshold>100</Threshold>
      <ThresholdType>line</ThresholdType>
      <ThresholdStat>total</ThresholdStat>
    </PropertyGroup>
    ```
- [ ] Design tests to execute all branches:
  - Normal, Abnormal, Boundary suites must collectively cover all paths including exceptions and guard clauses.
- [ ] Isolate non-determinism and external calls:
  - Use `NSubstitute` to mock I/O, time, randomness, and external services so branches are testable.
- [ ] Document any exclusions explicitly and keep minimal:
  - Allowed exclusions: auto-generated files, EF migrations, designer files.
  - Prefer targeting 100% on application code; avoid broad `Exclude` patterns.
- [ ] Fail CI when coverage < 100%:
  - Use the threshold commands above in CI pipeline step that runs `dotnet test`.

## Execution
- [ ] Run all tests: `dotnet test`
- [ ] With coverage: `dotnet test --collect:"XPlat Code Coverage"`
- [ ] Per project: `dotnet test tests/{ProjectName}.Tests`