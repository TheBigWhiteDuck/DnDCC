namespace Projekt.Model.DataModels;

/// <summary>
/// Reprezentuje pojedynczy przedmiot należący do postaci.
/// </summary>
public class Item
{
    /// <summary>
    /// Unikalny identyfikator przedmiotu.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Nazwa przedmiotu.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Ilość danego przedmiotu.
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Id postaci, do której przypisany jest przedmiot.
    /// </summary>
    public int CharacterId { get; set; }

    /// <summary>
    /// Postać, do której należy przedmiot.
    /// </summary>
    public virtual Character Character { get; set; }

    /// <summary>
    /// Konstruktor bezparametrowy wymagany przez ORM.
    /// </summary>
    public Item() { }

    /// <summary>
    /// Tworzy nowy przedmiot przypisany do konkretnej postaci.
    /// </summary>
    /// <param name="name">Nazwa przedmiotu</param>
    /// <param name="quantity">Ilość</param>
    /// <param name="charId">Id postaci</param>
    public Item(string name, int quantity, int charId)
    {
        Name = name;
        Quantity = quantity;
        CharacterId = charId;
    }
}
