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
            ? DbContext.Characters.AsQueryable().ToList()
            : DbContext.Characters.AsQueryable().Where(c => c.UserId == userId);
    }
}
