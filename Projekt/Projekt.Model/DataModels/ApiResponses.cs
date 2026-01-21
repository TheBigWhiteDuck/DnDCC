using System.ComponentModel.DataAnnotations;
using Projekt.Model.DataModels;

namespace Projekt.Model.ApiResponses;

/// <summary>
/// Odpowiedź API zawierająca listę zaklęć dostępnych dla danej klasy postaci.
/// </summary>
public class DndClassSpellResponse
{
    /// <summary>
    /// Lista zestawów zaklęć zwróconych przez API.
    /// </summary>
    public List<SpellResults> Spells { get; set; }
}

/// <summary>
/// Zawiera informacje o wynikach zapytania dotyczącego zaklęć.
/// </summary>
public class SpellResults
{
    /// <summary>
    /// Liczba zaklęć spełniających kryteria zapytania.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Lista zaklęć.
    /// </summary>
    public List<SpellItem> Results { get; set; }
}

/// <summary>
/// Reprezentuje pojedyncze zaklęcie zwrócone przez zewnętrzne API.
/// </summary>
public class SpellItem
{
    /// <summary>
    /// Techniczny identyfikator zaklęcia w API.
    /// </summary>
    public string Index { get; set; }

    /// <summary>
    /// Nazwa zaklęcia.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Poziom zaklęcia.
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Adres URL do szczegółów zaklęcia w API.
    /// </summary>
    public string Url { get; set; }
}

/// <summary>
/// Odpowiedź API zawierająca możliwe wybory biegłości dla klasy.
/// </summary>
public class DndClassProficiencyResponse
{
    /// <summary>
    /// Lista możliwych zestawów wyboru biegłości.
    /// </summary>
    public List<ProficiencyChoice> Proficiency_Choices { get; set; }
}

/// <summary>
/// Definiuje pojedynczy wybór biegłości dostępny dla klasy.
/// </summary>
public class ProficiencyChoice
{
    /// <summary>
    /// Opis wyboru biegłości.
    /// </summary>
    public string Desc { get; set; }

    /// <summary>
    /// Liczba elementów, które użytkownik musi wybrać.
    /// </summary>
    public int Choose { get; set; }

    /// <summary>
    /// Typ wyboru (np. proficiencies).
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Zbiór opcji dostępnych do wyboru.
    /// </summary>
    public From From { get; set; }
}

/// <summary>
/// Definiuje zestaw opcji, z których użytkownik może dokonać wyboru.
/// </summary>
public class From
{
    /// <summary>
    /// Typ zestawu opcji.
    /// </summary>
    public string Option_Set_Type { get; set; }

    /// <summary>
    /// Lista dostępnych opcji.
    /// </summary>
    public List<Option> Options { get; set; }
}

/// <summary>
/// Reprezentuje pojedynczą opcję wyboru.
/// </summary>
public class Option
{
    /// <summary>
    /// Typ opcji.
    /// </summary>
    public string Option_Type { get; set; }

    /// <summary>
    /// Element API powiązany z opcją.
    /// </summary>
    public ApiItem Item { get; set; }
}

/// <summary>
/// Ogólny model wyników zwróconych przez API.
/// </summary>
public class Results
{
    /// <summary>
    /// Liczba elementów w wyniku.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Lista elementów API.
    /// </summary>
    public List<ApiItem> ApiItems { get; set; }
}

/// <summary>
/// Reprezentuje pojedynczy element zwrócony przez API D&D.
/// </summary>
public class ApiItem
{
    /// <summary>
    /// Techniczny identyfikator elementu.
    /// </summary>
    public string Index { get; set; }

    /// <summary>
    /// Nazwa elementu.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Adres URL do szczegółów elementu w API.
    /// </summary>
    public string Url { get; set; }
}

/// <summary>
/// Model pomocniczy opisujący wybór, którego może dokonać użytkownik.
/// </summary>
public class ChoiceModel
{
    /// <summary>
    /// Opis wyboru prezentowany użytkownikowi.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Liczba opcji, które należy wybrać.
    /// </summary>
    public int ChooseCount { get; set; }

    /// <summary>
    /// Lista dostępnych opcji.
    /// </summary>
    public List<OptionModel> Options { get; set; }
}

/// <summary>
/// Reprezentuje pojedynczą opcję wyboru.
/// </summary>
public class OptionModel
{
    /// <summary>
    /// Tekst wyświetlany użytkownikowi.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Wartość techniczna opcji.
    /// </summary>
    public string Value { get; set; }
}

/// <summary>
/// Model żądania utworzenia lub aktualizacji postaci.
/// </summary>
public class CharacterRequest
{
    /// <summary>
    /// Dane postaci.
    /// </summary>
    [Required]
    public Character Character { get; set; } = new();

    /// <summary>
    /// Lista wybranych biegłości.
    /// </summary>
    public List<string> Proficiencies { get; set; } = new();

    /// <summary>
    /// Lista przedmiotów przypisanych do postaci.
    /// </summary>
    public List<Item> Items { get; set; } = new();
}

/// <summary>
/// Model wyboru zestawu przedmiotów.
/// </summary>
public class ItemChoiceModel
{
    /// <summary>
    /// Opis zestawu wyboru przedmiotów.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Liczba zestawów, które użytkownik może wybrać.
    /// </summary>
    public int ChooseCount { get; set; }

    /// <summary>
    /// Dostępne zestawy przedmiotów.
    /// </summary>
    public List<ItemSet> ItemSets { get; set; }

    /// <summary>
    /// Metoda pomocnicza do debugowania zawartości modelu.
    /// </summary>
    public void Print()
    {
        Console.WriteLine("Description: " + Description);
        int i = 0;
        foreach (var set in ItemSets)
        {
            Console.WriteLine("Set no.: " + i);
            foreach (var item in set.RegularItems)
            {
                Console.WriteLine("Item Regular: " + item.Name + " x " + item.Quantity);
            }
            foreach (var item in set.CategoryItems)
            {
                Console.WriteLine("Item Category: " + item.Name + " x " + item.Quantity);
            }
            i++;
        }
    }
}

/// <summary>
/// Reprezentuje pojedynczy zestaw przedmiotów do wyboru.
/// </summary>
public class ItemSet
{
    /// <summary>
    /// Liczba regularnych przedmiotów do wyboru.
    /// </summary>
    public int RegularCount { get; set; } = 0;

    /// <summary>
    /// Liczba przedmiotów kategorii do wyboru.
    /// </summary>
    public int CategoryCount { get; set; } = 0;

    /// <summary>
    /// Lista regularnych przedmiotów.
    /// </summary>
    public List<ItemModel> RegularItems { get; set; } = new();

    /// <summary>
    /// Lista przedmiotów kategorii.
    /// </summary>
    public List<ItemModel> CategoryItems { get; set; } = new();
}

/// <summary>
/// Model uproszczonego przedmiotu używany w zestawach wyboru.
/// </summary>
public class ItemModel
{
    /// <summary>
    /// Nazwa przedmiotu.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Ilość przedmiotu.
    /// </summary>
    public int Quantity { get; set; }
}
