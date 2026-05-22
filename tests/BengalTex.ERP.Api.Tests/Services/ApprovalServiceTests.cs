using BengalTex.ERP.Api.Tests.TestSupport;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Infrastructure.Persistence;
using BengalTex.ERP.Infrastructure.Persistence.CrossCutting;
using BengalTex.ERP.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BengalTex.ERP.Api.Tests.Services;

public class ApprovalServiceTests
{
    private const decimal Threshold = 50_000m;
    private const string ApproverRole = "Admin";

    private static (ApprovalService svc, ApplicationDbContext ctx, Mock<INotificationService> notify) Build()
    {
        var ctx = TestHarness.NewContext();
        var settings = Options.Create(new ApprovalSettings
        {
            PurchaseOrderThreshold = Threshold,
            PurchaseOrderApproverRole = ApproverRole
        });
        var notify = new Mock<INotificationService>();
        var svc = new ApprovalService(ctx, settings, new StubClock(), new StubCurrentUser(), notify.Object);
        return (svc, ctx, notify);
    }

    [Fact]
    public async Task Amount_within_threshold_auto_approves_without_notifying()
    {
        var (svc, ctx, notify) = Build();

        var result = await svc.SubmitAsync("PurchaseOrder", 1, "PO-1", Threshold);   // exactly at threshold
        await ctx.SaveChangesAsync();

        result.AutoApproved.Should().BeTrue();
        var request = await ctx.ApprovalRequests.Include(r => r.Steps).SingleAsync();
        request.Status.Should().Be(ApprovalStatus.Approved);
        request.Steps.Single().Status.Should().Be(ApprovalStepStatus.Skipped);
        notify.Verify(n => n.NotifyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Amount_above_threshold_creates_pending_request_and_notifies_approver()
    {
        var (svc, ctx, notify) = Build();

        var result = await svc.SubmitAsync("PurchaseOrder", 1, "PO-1", Threshold + 1m);
        await ctx.SaveChangesAsync();

        result.AutoApproved.Should().BeFalse();
        var request = await ctx.ApprovalRequests.Include(r => r.Steps).SingleAsync();
        request.Status.Should().Be(ApprovalStatus.Pending);
        request.Steps.Single().Status.Should().Be(ApprovalStepStatus.Pending);
        request.Steps.Single().ApproverRole.Should().Be(ApproverRole);
        notify.Verify(n => n.NotifyAsync(NotificationChannels.InApp, ApproverRole, It.IsAny<string>(),
            It.IsAny<string>(), "PurchaseOrder", 1L, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Approving_a_single_level_request_completes_it()
    {
        var (svc, ctx, _) = Build();
        await svc.SubmitAsync("PurchaseOrder", 1, "PO-1", Threshold + 1m);
        await ctx.SaveChangesAsync();
        var id = (await ctx.ApprovalRequests.SingleAsync()).Id;

        var result = await svc.DecideAsync(id, approve: true, userId: "u1",
            roles: new[] { ApproverRole }, isSuperAdmin: false, comment: "ok");
        await ctx.SaveChangesAsync();

        result.Success.Should().BeTrue();
        result.Outcome.Should().Be(ApprovalOutcome.Approved);
        var request = await ctx.ApprovalRequests.Include(r => r.Steps).SingleAsync();
        request.Status.Should().Be(ApprovalStatus.Approved);
        request.Steps.Single().Status.Should().Be(ApprovalStepStatus.Approved);
    }

    [Fact]
    public async Task Rejecting_marks_the_request_rejected()
    {
        var (svc, ctx, _) = Build();
        await svc.SubmitAsync("PurchaseOrder", 1, "PO-1", Threshold + 1m);
        await ctx.SaveChangesAsync();
        var id = (await ctx.ApprovalRequests.SingleAsync()).Id;

        var result = await svc.DecideAsync(id, approve: false, userId: "u1",
            roles: new[] { ApproverRole }, isSuperAdmin: false, comment: "no");
        await ctx.SaveChangesAsync();

        result.Outcome.Should().Be(ApprovalOutcome.Rejected);
        (await ctx.ApprovalRequests.SingleAsync()).Status.Should().Be(ApprovalStatus.Rejected);
    }

    [Fact]
    public async Task A_user_without_the_approver_role_cannot_decide()
    {
        var (svc, ctx, _) = Build();
        await svc.SubmitAsync("PurchaseOrder", 1, "PO-1", Threshold + 1m);
        await ctx.SaveChangesAsync();
        var id = (await ctx.ApprovalRequests.SingleAsync()).Id;

        var result = await svc.DecideAsync(id, approve: true, userId: "u1",
            roles: new[] { "SalesManager" }, isSuperAdmin: false, comment: null);

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }
}
