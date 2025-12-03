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
  - `dotnet add {ProjectName}.Tests package AutoFixture.Xunit2`
  - `dotnet add {ProjectName}.Tests package FluentAssertions`
  - `dotnet add {ProjectName}.Tests package NSubstitute`
  - `dotnet add {ProjectName}.Tests package coverlet.collector`
- [ ] Optional: use MSBuild coverlet instead of collector
  - `dotnet add {ProjectName}.Tests package coverlet.msbuild`

## Test Suites
- [ ] Use `[Theory]` for parameterized tests.
- [ ] Create three suites per subject:
  - Normal Data: typical valid inputs, expected outputs and interactions.
  - Abnormal Data: invalid inputs, exceptions, nulls, misconfigurations.
  - Boundary Data: min/max, empty, one-item, thresholds, precision edges.
- [ ] Prefer `[InlineData]` for small sets; `[MemberData]` for complex datasets.
- [ ] Use `[AutoData]` / `[InlineAutoData]` to generate objects and reduce boilerplate.
- [ ] Use `NSubstitute` to mock dependencies and verify interactions.
- [ ] Use `FluentAssertions` for expressive assertions.

## Template
```csharp
using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Foo.Tests.Services;

public class BarServiceTests
{
    [Theory]
    [InlineData(2, 3, 5)]
    [InlineData(10, 20, 30)]
    [InlineData(-1, 4, 3)]
    public void NormalData_Add_ReturnsExpected(int a, int b, int expected)
    {
        var dep = Substitute.For<IDependency>();
        dep.Compute(a, b).Returns(expected);
        var sut = new BarService(dep);
        var result = sut.Add(a, b);
        result.Should().Be(expected);
        dep.Received(1).Compute(a, b);
    }

    [Theory]
    [InlineData(int.MaxValue, 1)]
    [InlineData(int.MinValue, -1)]
    public void BoundaryData_Add_HandlesEdges(int a, int b)
    {
        var sut = new BarService(Substitute.For<IDependency>());
        Action act = () => sut.Add(a, b);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineAutoData(0, 0)]
    [InlineAutoData(-100, 0)]
    public void AbnormalData_Add_InvalidInputs_Throws(int a, int b, BarService sut)
    {
        Action act = () => sut.Add(a, b);
        act.Should().Throw<ArgumentException>();
    }
}
```

## MemberData Example
```csharp
public static class BarData
{
    public static IEnumerable<object[]> Normal =>
        new[]
        {
            new object[] { 1, 2, 3 },
            new object[] { 100, 200, 300 }
        };

    public static IEnumerable<object[]> Boundary =>
        new[]
        {
            new object[] { int.MaxValue, 0 },
            new object[] { int.MinValue, 0 }
        };

    public static IEnumerable<object[]> Abnormal =>
        new[]
        {
            new object[] { 0, 0 },
            new object[] { -1, -1 }
        };
}

public class BarServiceTheorySets
{
    [Theory]
    [MemberData(nameof(BarData.Normal), MemberType = typeof(BarData))]
    public void Normal(int a, int b, int expected)
    {
        var dep = Substitute.For<IDependency>();
        dep.Compute(a, b).Returns(expected);
        var sut = new BarService(dep);
        sut.Add(a, b).Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(BarData.Boundary), MemberType = typeof(BarData))]
    public void Boundary(int a, int b)
    {
        var sut = new BarService(Substitute.For<IDependency>());
        Action act = () => sut.Add(a, b);
        act.Should().NotThrow();
    }

    [Theory]
    [MemberData(nameof(BarData.Abnormal), MemberType = typeof(BarData))]
    public void Abnormal(int a, int b)
    {
        var sut = new BarService(Substitute.For<IDependency>());
        Action act = () => sut.Add(a, b);
        act.Should().Throw<ArgumentException>();
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