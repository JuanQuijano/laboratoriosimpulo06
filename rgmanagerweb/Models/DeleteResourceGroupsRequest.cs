using System.ComponentModel.DataAnnotations;

namespace rgmanagerweb.Models;

public class DeleteResourceGroupsRequest
{
    [Required]
    [StringLength(128)]
    public string SubscriptionId { get; set; } = string.Empty;

    public List<string> SelectedResourceGroups { get; set; } = [];
}