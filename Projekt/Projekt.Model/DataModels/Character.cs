namespace Projekt.Model.DataModels;

/// <summary>
/// Reprezentuje postać gracza w systemie DnD.
/// Zawiera podstawowe informacje fabularne, statystyki oraz powiązane zasoby.
/// </summary>
public class Character
{
    /// <summary>
    /// Unikalny identyfikator postaci.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Imię postaci.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Charakter (alignment) postaci, np. Lawful Good, Chaotic Neutral.
    /// </summary>
    public string Alignment { get; set; }

    /// <summary>Siła (STR).</summary>
    public int Strength { get; set; }

    /// <summary>Zręczność (DEX).</summary>
    public int Dexterity { get; set; }

    /// <summary>Kondycja (CON).</summary>
    public int Constitution { get; set; }

    /// <summary>Inteligencja (INT).</summary>
    public int Intelligence { get; set; }

    /// <summary>Mądrość (WIS).</summary>
    public int Wisdom { get; set; }

    /// <summary>Charyzma (CHA).</summary>
    public int Charisma { get; set; }

    /// <summary>
    /// Rasa postaci (np. Human, Elf, Dragonborn).
    /// </summary>
    public string Race { get; set; }

    /// <summary>
    /// Klasa postaci (np. Fighter, Wizard).
    /// </summary>
    public string Class { get; set; }

    /// <summary>
    /// Lista biegłości wybranych przez użytkownika.
    /// </summary>
    public IList<string> Proficiencies { get; set; } = new List<string>();

    /// <summary>
    /// Cechy rasowe i klasowe przypisywane automatycznie.
    /// </summary>
    public IList<string> Traits { get; set; } = new List<string>();

    /// <summary>
    /// Maksymalna liczba punktów życia postaci.
    /// Wartość obliczana automatycznie.
    /// </summary>
    public int MaxHP { get; set; }

    /// <summary>
    /// Aktualna liczba punktów życia postaci.
    /// </summary>
    public int CurrentHP { get; set; }

    /// <summary>
    /// Tymczasowe punkty życia.
    /// </summary>
    public int TemporaryHP { get; set; }

    /// <summary>
    /// Klasa pancerza (Armor Class).
    /// Wartość obliczana automatycznie.
    /// </summary>
    public int ArmorClass { get; set; }

    /// <summary>
    /// Prędkość poruszania się postaci.
    /// </summary>
    public int Speed { get; set; }

    /// <summary>
    /// Dodatkowe notatki fabularne lub mechaniczne.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Lista przedmiotów posiadanych przez postać.
    /// </summary>
    public virtual IList<Item> Items { get; set; } = new List<Item>();

    /// <summary>
    /// Id użytkownika będącego właścicielem postaci.
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Użytkownik, do którego należy postać.
    /// </summary>
    public virtual User? User { get; set; }
}
