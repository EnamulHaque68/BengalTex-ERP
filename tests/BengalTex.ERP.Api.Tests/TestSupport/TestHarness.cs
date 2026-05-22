using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;

namespace BengalTex.ERP.Api.Tests.TestSupport;

/// <summary>
/// Spins up a real <see cref="ApplicationDbContext"/> backed by EF Core InMemory, so command
/// handlers / services can be exercised against an actual EF query+save surface (the riskiest
/// business logic — stock posting, lot FIFO, costing, approvals — rather than mocked queryables).
/// Each call gets an isolated database. Cross-cutting concerns the logic doesn't need
/// (audit interceptor, MediatR publishing) are stubbed.
/// </summary>
public static class TestHarness
{
    public static ApplicationDbContext NewContext(ICurrentUserService? currentUser = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("test-" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .EnableSensitiveDataLogging()
            .Options;

        var publisher = new Mock<IPublisher>().Object;
        return new ApplicationDbContext(options, publisher, currentUser ?? new StubCurrentUser());
    }

    /// <summary>A numbering stub that hands out deterministic, unique codes per series.</summary>
    public static Mock<INumberingService> Numbering()
    {
        var counter = 0;
        var mock = new Mock<INumberingService>();
        mock.Setup(x => x.NextAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string series, int? _, CancellationToken __) => $"{series}-{++counter:00000}");
        return mock;
    }
}

public sealed class StubCurrentUser : ICurrentUserService
{
    public string? UserId { get; set; } = "test-user-id";
    public string? UserName { get; set; } = "Tester";
    public int? FactoryId => null;
    public string? IpAddress => null;
    public string? UserAgent => null;
    public bool IsAuthenticated => true;
    public IReadOnlyList<string> Roles { get; set; } = new[] { "Admin" };
    public bool IsInRole(string role) => Roles.Contains(role);
    public bool HasPermission(string permission) => true;
    public IReadOnlyList<string> Permissions => Array.Empty<string>();
}

public sealed class StubClock : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 5, 22, 8, 0, 0, TimeSpan.Zero);
}
