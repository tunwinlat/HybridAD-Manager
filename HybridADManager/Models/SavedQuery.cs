namespace HybridADManager.Models;

public class SavedQuery
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? FilterName { get; set; }
    public string? FilterEmail { get; set; }
    public string? FilterDescription { get; set; }
    public string? FilterPhone { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
