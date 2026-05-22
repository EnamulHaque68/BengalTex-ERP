using BengalTex.ERP.Application.Notifications.Commands;
using BengalTex.ERP.Application.Production.Commands;
using FluentAssertions;
using Xunit;

namespace BengalTex.ERP.Api.Tests.Validators;

public class SendTestNotificationCommandValidatorTests
{
    private readonly SendTestNotificationCommandValidator _validator = new();

    [Theory]
    [InlineData("InApp")]
    [InlineData("Email")]
    [InlineData("Sms")]
    public void Valid_channels_pass(string channel) =>
        _validator.Validate(new SendTestNotificationCommand(channel, "to@x.com", "Subj", "Body")).IsValid.Should().BeTrue();

    [Fact]
    public void Unknown_channel_fails() =>
        _validator.Validate(new SendTestNotificationCommand("Telegram", "to", "s", "b")).IsValid.Should().BeFalse();

    [Fact]
    public void Empty_recipient_fails() =>
        _validator.Validate(new SendTestNotificationCommand("InApp", "", "s", "b")).IsValid.Should().BeFalse();

    [Fact]
    public void Empty_body_fails() =>
        _validator.Validate(new SendTestNotificationCommand("InApp", "to", "s", "")).IsValid.Should().BeFalse();
}

public class CompleteProductionStageCommandValidatorTests
{
    private readonly CompleteProductionStageCommandValidator _validator = new();

    [Fact]
    public void Valid_command_passes() =>
        _validator.Validate(new CompleteProductionStageCommand(1, 100m, 2m, null)).IsValid.Should().BeTrue();

    [Fact]
    public void Non_positive_stage_id_fails() =>
        _validator.Validate(new CompleteProductionStageCommand(0, 100m, 0m, null)).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(-1)]
    [InlineData(-0.5)]
    public void Negative_completed_quantity_fails(decimal qty) =>
        _validator.Validate(new CompleteProductionStageCommand(1, qty, 0m, null)).IsValid.Should().BeFalse();

    [Fact]
    public void Negative_rejected_quantity_fails() =>
        _validator.Validate(new CompleteProductionStageCommand(1, 100m, -1m, null)).IsValid.Should().BeFalse();
}

public class CreateProductionOrderStagesValidatorTests
{
    private readonly CreateProductionOrderCommandValidator _validator = new();

    private static CreateProductionOrderCommand WithStages(params ProductionStageInput[] stages) =>
        new(ProductId: 1, BomId: 1, Quantity: 10m, IssueWarehouseId: 1, ReceiveWarehouseId: 1,
            PlannedStartDate: null, PlannedEndDate: null, Notes: null, Stages: stages);

    [Fact]
    public void Stage_without_name_fails() =>
        _validator.Validate(WithStages(new ProductionStageInput(1, "", null, null, null, null)))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Stage_with_non_positive_planned_quantity_fails() =>
        _validator.Validate(WithStages(new ProductionStageInput(1, "Cutting", 0m, null, null, null)))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Valid_stages_pass() =>
        _validator.Validate(WithStages(
            new ProductionStageInput(1, "Cutting", null, "Line A", null, null),
            new ProductionStageInput(2, "Sewing", 10m, null, null, null))).IsValid.Should().BeTrue();

    [Fact]
    public void No_stages_is_valid() =>
        _validator.Validate(WithStages()).IsValid.Should().BeTrue();
}
