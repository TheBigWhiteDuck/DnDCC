namespace Projekt.Model.DataModels;

public class Item
{
    public int Id {get; set;}
    public string Name { get; set; }
    public string Type { get; set; }
    public int Quantity { get; set; } = 1;
    public double Weight { get; set; }
    public string Reference { get; set; }

    public virtual IList<CharacterItem> CharacterItems { get; set; } = new List<CharacterItem>();
}