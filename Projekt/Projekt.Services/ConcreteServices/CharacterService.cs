using AutoMapper;
using Microsoft.Extensions.Logging;
using Projekt.DAL;
using Projekt.Model.DataModels;

namespace Projekt.Services.ConcreteServices;

public class CharacterService : BaseService, ICharacterService
{
    public CharacterService(ApplicationDbContext dbContext, IMapper mapper, ILogger logger)
        : base(dbContext, mapper, logger) { }

    public Character GetCharacter(int id)
    {
        var character = DbContext.Characters.FirstOrDefault(c => c.Id == id);
        return character;
    }

    public IEnumerable<Character> GetCharacters(int? userId = null)
    {
        return userId == null
            ? Enumerable.Empty<Character>()
            : DbContext.Characters.AsQueryable().Where(c => c.UserId == userId.Value).ToList();
    }

    public void UpdateCharacter(Character character)
    {
        try {
            DbContext.Characters.Update(character);
        } catch (Exception e) {
            //obsluga bledu
        }
        DbContext.SaveChanges();
    }

    public int SaveCharacter(Character character) {
        try {
            DbContext.Characters.Add(character);
        } catch (Exception e) {
            //obsluga bledu
        }
        DbContext.SaveChanges();
        return character.Id;
    }

    public void DeleteCharacter(int id)
    {
        var character = DbContext.Characters.FirstOrDefault(c => c.Id == id);
        if (character == null) return;
        DbContext.Characters.Remove(character);
        DbContext.SaveChanges();
    }

    public void AddItem(Item item){
        var existingEquipment = DbContext.Item.FirstOrDefault(e => e.Name == item.Name && e.CharacterId == item.CharacterId);
        if (existingEquipment != null)
        {
            existingEquipment.Quantity += item.Quantity;
        }
        else
        {
            DbContext.Item.Add(new Item
            {
                Name = item.Name,
                Quantity = item.Quantity,
                CharacterId = item.CharacterId
            });
        }

        DbContext.SaveChanges();
    }

    public void RemoveItem(int itemId){
        var it = DbContext.Item.FirstOrDefault(i => i.Id == itemId);
        if (it != null)
        {
            DbContext.Item.Remove(it);
            DbContext.SaveChanges();
        }
    }
}
