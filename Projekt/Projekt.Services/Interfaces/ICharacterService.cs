using AutoMapper;
using Projekt.DAL;
using Microsoft.Extensions.Logging;
using Projekt.Model.DataModels;

namespace Projekt.Services.ConcreteServices;

public interface ICharacterService
{
    public Character GetCharacter(int id);
    public IEnumerable<Character> GetCharacters(int? userId = null);
    public int SaveCharacter(Character character);
    public void DeleteCharacter(int id);
    public void UpdateCharacter(Character character);
    public void AddItem(Item item);
    public void RemoveItem(int itemId);
}
