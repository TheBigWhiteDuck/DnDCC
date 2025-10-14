namespace Projekt.Model.ApiResponses;

public class DndClassSpellResponse
{
    public List<SpellResults> Spells { get; set; }
}

public class SpellResults {
    public int Count {get;set;}
    public List<SpellItem> Results {get;set;}
}

public class SpellItem {
    public string Index { get; set; }
    public string Name { get; set; }
    public int Level {get;set;}
    public string Url { get; set; }
}

public class DndClassProficiencyResponse
{
    public List<ProficiencyChoice> Proficiency_Choices { get; set; }
}

public class ProficiencyChoice
{
    public string Desc { get; set; }
    public int Choose { get; set; }
    public string Type { get; set; }
    public From From { get; set; }
}

public class From
{
    public string Option_Set_Type { get; set; }
    public List<Option> Options { get; set; }
}

public class Option
{
    public string Option_Type { get; set; }
    public ProficiencyItem Item { get; set; }
}

public class ProficiencyItem
{
    public string Index { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
}