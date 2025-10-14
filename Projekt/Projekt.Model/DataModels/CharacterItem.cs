namespace Projekt.Model.DataModels;

public class CharacterItem
{
    public int CharacterId { get; set; }
    public virtual Character Character { get; set; }

    public int ItemId { get; set; }
    public virtual Item Item { get; set; }

    public int Quantity { get; set; } = 1;
}