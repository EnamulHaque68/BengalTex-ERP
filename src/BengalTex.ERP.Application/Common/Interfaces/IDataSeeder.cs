namespace BengalTex.ERP.Application.Common.Interfaces;

public interface IDataSeeder
{
    Task SeedAsync(CancellationToken ct = default);
}
