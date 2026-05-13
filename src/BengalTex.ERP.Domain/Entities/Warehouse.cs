using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Storage location within a factory. Multiple warehouses per factory:
/// typically one Raw Material, one Finished Goods, one WIP, plus optional Reject/General.
/// (FactoryId, Code) is unique — same code can repeat across factories.
/// </summary>
public class Warehouse : BaseEntity, IFactoryScoped
{
    public string Code { get; set; } = string.Empty;       // MAIN, RM, FG, WIP, REJECT
    public string Name { get; set; } = string.Empty;
    public WarehouseType WarehouseType { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;

    public int FactoryId { get; set; }
    public Factory Factory { get; set; } = null!;
}

public enum WarehouseType
{
    General = 1,
    RawMaterial = 2,
    FinishedGoods = 3,
    WorkInProgress = 4,
    Reject = 5
}
