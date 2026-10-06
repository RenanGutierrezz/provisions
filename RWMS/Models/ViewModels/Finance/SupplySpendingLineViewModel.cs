namespace RWMS.Models.ViewModels.Finance;

public class SupplySpendingLineViewModel
{
    public int SupplyItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int QuantityOrdered { get; set; }
    public decimal EstimatedUnitCost { get; set; }
    public decimal LineTotal { get; set; }
}
