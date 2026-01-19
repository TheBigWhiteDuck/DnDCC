using AutoMapper;
using Microsoft.Extensions.Logging;
using Projekt.DAL;
using Projekt.Model.DataModels;

namespace Projekt.Services.ConcreteServices;

/// <summary>
/// Serwis do zarządzania postaciami graczy, obsługuje CRUD postaci i przedmiotów.
/// </summary>
public class CharacterService : BaseService, ICharacterService
{
    /// <summary>
    /// Tworzy instancję serwisu postaci.
    /// </summary>
    public CharacterService(ApplicationDbContext dbContext, IMapper mapper, ILogger logger)
        : base(dbContext, mapper, logger) { }

    /// <summary>
    /// Pobiera pojedynczą postać po identyfikatorze.
    /// </summary>
    public Character GetCharacter(int id)
    {
        var character = DbContext.Characters.FirstOrDefault(c => c.Id == id);
        return character;
    }

    /// <summary>
    /// Pobiera wszystkie postaci użytkownika lub pustą listę, jeśli userId nie podany.
    /// </summary>
    public IEnumerable<Character> GetCharacters(int? userId = null)
    {
        return userId == null
            ? Enumerable.Empty<Character>()
            : DbContext.Characters.AsQueryable().Where(c => c.UserId == userId.Value).ToList();
    }

    /// <summary>
    /// Aktualizuje dane istniejącej postaci w bazie.
    /// </summary>
    public void UpdateCharacter(Character character)
    {
        try
        {
            DbContext.Characters.Update(character);
        }
        catch (Exception e)
        {
            //obsluga bledu
        }
        DbContext.SaveChanges();
    }

    /// <summary>
    /// Zapisuje nową postać w bazie i zwraca jej Id.
    /// </summary>
    public int SaveCharacter(Character character)
    {
        try
        {
            DbContext.Characters.Add(character);
        }
        catch (Exception e)
        {
            //obsluga bledu
        }
        DbContext.SaveChanges();
        return character.Id;
    }

    /// <summary>
    /// Usuwa postać po identyfikatorze.
    /// </summary>
    public void DeleteCharacter(int id)
    {
        var character = DbContext.Characters.FirstOrDefault(c => c.Id == id);
        if (character == null)
            return;
        DbContext.Characters.Remove(character);
        DbContext.SaveChanges();
    }

    /// <summary>
    /// Dodaje przedmiot do postaci lub zwiększa jego ilość, jeśli już istnieje.
    /// </summary>
    public void AddItem(Item item)
    {
        var existingEquipment = DbContext.Item.FirstOrDefault(e =>
            e.Name == item.Name && e.CharacterId == item.CharacterId
        );
        if (existingEquipment != null)
        {
            existingEquipment.Quantity += item.Quantity;
        }
        else
        {
            DbContext.Item.Add(
                new Item
                {
                    Name = item.Name,
                    Quantity = item.Quantity,
                    CharacterId = item.CharacterId,
                }
            );
        }

        DbContext.SaveChanges();
    }

    /// <summary>
    /// Usuwa przedmiot z postaci po Id przedmiotu.
    /// </summary>
    public void RemoveItem(int itemId)
    {
        var it = DbContext.Item.FirstOrDefault(i => i.Id == itemId);
        if (it != null)
        {
            DbContext.Item.Remove(it);
            DbContext.SaveChanges();
        }
    }
}
