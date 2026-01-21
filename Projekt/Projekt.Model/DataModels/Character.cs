using System.ComponentModel.DataAnnotations;

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
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Charakter (alignment) postaci, np. Lawful Good, Chaotic Neutral.
    /// </summary>
    [Required]
    public string Alignment { get; set; } = string.Empty;

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
    [Required]
    public string Race { get; set; } = string.Empty;

    /// <summary>
    /// Klasa postaci (np. Fighter, Wizard).
    /// </summary>
    [Required]
    public string Class { get; set; } = string.Empty;

    /// <summary>
    /// Podklasa postaci (np. Battle Master, Evocation).
    /// </summary>
    [Required]
    public string SubClass { get; set; } = string.Empty;

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
    /// Lista zaklec w formie tekstu lub JSON.
    /// </summary>
    [Required]
    public string Spells { get; set; } = string.Empty;

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
    public int UserId { get; set; }

    /// <summary>
    /// Użytkownik, do którego należy postać.
    /// </summary>
    public virtual User? User { get; set; }
}
