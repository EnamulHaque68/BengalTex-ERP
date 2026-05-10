using BengalTex.ERP.Application.Common.Interfaces;

namespace BengalTex.ERP.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}