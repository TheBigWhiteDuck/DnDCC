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
    public string SubClass { get; set; } = string.Empty;
    public string Spells { get; set; } = string.Empty;

    public IList<string> Proficiencies { get; set; } = new List<string>(); // OPCJE
    public IList<string> Traits { get; set; } = new List<string>(); // UZUPEŁNIONE AUTOMATYCZNIE

    public int MaxHP { get; set; } // UZUPEŁNIONE AUTOMATYCZNIE
    public int CurrentHP { get; set; } // UZUPEŁNIONE AUTOMATYCZNIE
    public int TemporaryHP { get; set; } // UZUPEŁNIONE AUTOMATYCZNIE
    public int ArmorClass { get; set; } // UZUPEŁNIONE AUTOMATYCZNIE
    public int Speed { get; set; } // UZUPEŁNIONE AUTOMATYCZNIE

    public string? Notes { get; set; }

    public virtual IList<Item> Items { get; set; } = new List<Item>(); // OPCJE

    public int? UserId {get;set;}
    public virtual User? User {get;set;}
}
