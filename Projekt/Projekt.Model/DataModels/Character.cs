namespace Projekt.Model.DataModels;

public class Character
{
    public int Id {get;set;}
    public string Name {get;set;}
    public string Alignment {get;set;}

    public int Strength { get; set; }
    public int Dexterity { get; set; }
    public int Constitution { get; set; }
    public int Intelligence { get; set; }
    public int Wisdom { get; set; }
    public int Charisma { get; set; }

    public string Race {get;set;}
    public string Class {get;set;}
    public string SubClass {get;set;}

    public IList<string> Proficiencies { get; set; } = new List<string>();
    public IList<string> Traits { get; set; } = new List<string>();

    public int MaxHP { get; set; }
    public int CurrentHP { get; set; }
    public int TemporaryHP { get; set; }
    public int ArmorClass { get; set; }
    public int Speed { get; set; }

    public IList<string> Spells { get; set; } = new List<string>();
    public virtual IList<CharacterItem> CharacterItems { get; set; } = new List<CharacterItem>();

    public int UserId {get;set;}
    public virtual User User {get;set;}
}