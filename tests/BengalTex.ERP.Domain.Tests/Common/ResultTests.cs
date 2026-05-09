using BengalTex.ERP.Domain.Common;
using FluentAssertions;
using Xunit;

namespace BengalTex.ERP.Domain.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Success_HasNoError()
    {
        var r = Result.Success();
        r.IsSuccess.Should().BeTrue();
        r.IsFailure.Should().BeFalse();
        r.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_CapturesMessage()
    {
        var r = Result.Failure("Boom", "BOOM_CODE");
        r.IsFailure.Should().BeTrue();
        r.Error.Should().Be("Boom");
        r.ErrorCode.Should().Be("BOOM_CODE");
    }

    [Fact]
    public void GenericSuccess_CapturesValue()
    {
        var r = Result.Success(42);
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be(42);
    }

    [Fact]
    public void ValidationFailure_PopulatesErrors()
    {
        var errors = new List<ValidationError> { new("Name", "Required") };
        var r = Result.ValidationFailure(errors);
        r.IsFailure.Should().BeTrue();
        r.ValidationErrors.Should().HaveCount(1);
        r.ErrorCode.Should().Be("VALIDATION_ERROR");
    }
}