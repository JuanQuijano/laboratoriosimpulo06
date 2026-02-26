namespace apim.Models;

public class PetItem
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
}

public class PetSearchResult
{
    public bool ApiFound { get; set; }
    public string ApiName { get; set; } = "Swagger Petstore";
    public string ApiPath { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public List<PetItem> Pets { get; set; } = [];
}

public class PetListViewModel
{
    public string SelectedStatus { get; set; } = "available";
    public List<string> Statuses { get; } = ["available", "pending", "sold"];
    public bool ApiFound { get; set; }
    public string ApiName { get; set; } = string.Empty;
    public string ApiPath { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public List<PetItem> Pets { get; set; } = [];
}