using StoryCoffee.Contracts;

namespace StoryCoffee.Application.Documents;

public static class StoryCoffeeDocumentProfile
{
    public static CompanyDocumentProfile Default { get; } = new(
        "Story Coffee Roasters",
        "PO BOX 9065, New Market",
        "Auckland 1149",
        "New Zealand",
        "www.storycoffee.co.nz",
        "105-912-471",
        "ASB",
        "reborn Edge Limited",
        "12-3077-0789998-00");
}
