using AutoMapper;
using Microsoft.Extensions.Logging;
using Projekt.DAL;
using Projekt.Model.DataModels;

namespace Projekt.Services.ConcreteServices;

/// <summary>
/// Interfejs serwisu do zarządzania postaciami i ich ekwipunkiem.
/// </summary>
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
