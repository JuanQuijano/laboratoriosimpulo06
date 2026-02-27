using System.ComponentModel.DataAnnotations;

namespace rgmanagerweb.Models;

public class CreateResourceGroupRequest
{
    [Required]
    [StringLength(128)]
    public string SubscriptionId { get; set; } = string.Empty;

    [Required]
    [StringLength(90)]
    public string ResourceGroupName { get; set; } = string.Empty;

    [Required]
    [StringLength(60)]
    public string Location { get; set; } = string.Empty;
}