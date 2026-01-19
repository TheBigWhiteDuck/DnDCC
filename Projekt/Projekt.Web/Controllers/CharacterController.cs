using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Projekt.Model.ApiResponses;
using Projekt.Model.DataModels;
using Projekt.Services.ConcreteServices;

namespace Projekt.Web.Controllers
{
    /// <summary>
    /// Kontroler do zarządzania postaciami graczy, wyposażeniem, notatkami oraz generowaniem próbek postaci.
    /// </summary>
    [Authorize]
    public class CharacterController : BaseController
    {
        private readonly HttpClient _httpClient;
        protected readonly ICharacterService characterService;
        private readonly IWebHostEnvironment _env;

        /// <summary>
        /// Tworzy instancję CharacterController i inicjalizuje zależności: HttpClient, CharacterService, logger, mapper, lokalizator i środowisko web.
        /// </summary>
        public CharacterController(
            IHttpClientFactory httpClientFactory,
            ICharacterService _characterService,
            ILogger logger,
            IMapper mapper,
            IStringLocalizer localizer,
            IWebHostEnvironment env
        )
            : base(logger, mapper, localizer)
        {
            _httpClient = httpClientFactory.CreateClient();
            characterService = _characterService;
            _env = env;
        }

        /// <summary>
        /// Wyświetla listę postaci zalogowanego użytkownika.
        /// </summary>
        public IActionResult Index()
        {
            var userIdClaim = User
                ?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                ?.Value;
            if (!int.TryParse(userIdClaim, out var currentUserId))
            {
                return Unauthorized();
            }

            var characters = characterService.GetCharacters(currentUserId).ToList();
            return View(characters);
        }

        /// <summary>
        /// Usuwa spacje z przekazanego ciągu znaków.
        /// </summary>
        public string ClearSpaces(string sentence)
        {
            return sentence.Contains(" ") ? sentence.Replace(" ", "") : sentence;
        }

        /// <summary>
        /// Wyświetla szczegóły postaci, w tym cechy i umiejętności.
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var character = characterService.GetCharacter(id);

            var proficiencies = await GetNamesByIndex(character.Proficiencies, "proficiencies");
            var traits = await GetNamesByIndex(character.Traits, "traits");
            ViewData["Proficiencies"] = proficiencies;
            ViewData["Traits"] = traits;

            return View(character);
        }

        /// <summary>
        /// Pobiera nazwy obiektów z API DnD5e na podstawie listy indeksów.
        /// </summary>
        public async Task<IList<string>> GetNamesByIndex(IList<string> indexes, string url)
        {
            IList<string> data = new List<string>();
            foreach (string index in indexes)
            {
                var name = await GetNameByIndex(url + "/" + index);
                data.Add(name);
            }
            return data;
        }

        /// <summary>
        /// Zapisuje notatki dla konkretnej postaci.
        /// </summary>
        [HttpPost]
        public async Task<bool> SaveNotes(int charId, string? notes)
        {
            try
            {
                var character = characterService.GetCharacter(charId);
                Console.WriteLine("Character: " + charId);
                Console.WriteLine("Notes: " + notes);
                character.Notes = notes;

                characterService.UpdateCharacter(character);
                return true;
            }
            catch (Exception e) { }
            return false;
        }

        /// <summary>
        /// Wyświetla ekran zarządzania ekwipunkiem postaci.
        /// </summary>
        public async Task<IActionResult> Equipment(int characterId)
        {
            var character = characterService.GetCharacter(characterId);

            var response = await _httpClient.GetStringAsync(
                $"https://www.dnd5eapi.co/api/2014/equipment"
            );
            using var itemDoc = JsonDocument.Parse(response);
            var allItems = itemDoc
                .RootElement.GetProperty("results")
                .EnumerateArray()
                .Select(r => new SelectListItem
                {
                    Text = r.GetProperty("name").GetString(),
                    Value = r.GetProperty("name").GetString(),
                })
                .ToList();

            ViewData["AllItems"] = allItems;
            ViewBag.CharId = characterId;
            return View("Equipment", character.Items);
        }

        /// <summary>
        /// Dodaje przedmiot do ekwipunku postaci.
        /// </summary>
        [HttpPost]
        public IActionResult AddEquipment(Item item)
        {
            characterService.AddItem(item);
            var character = characterService.GetCharacter(item.CharacterId);
            return RedirectToAction("Details", character);
        }

        /// <summary>
        /// Usuwa przedmiot z ekwipunku postaci.
        /// </summary>
        public IActionResult RemoveEquipment(int itemId)
        {
            characterService.RemoveItem(itemId);
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Pobiera nazwę elementu z API DnD5e na podstawie indeksu.
        /// </summary>
        public async Task<string> GetNameByIndex(string url)
        {
            var response = await _httpClient.GetStringAsync(
                $"https://www.dnd5eapi.co/api/2014/{url}"
            );
            using var doc = JsonDocument.Parse(response);
            return doc.RootElement.GetProperty("name").GetString();
        }

        /// <summary>
        /// Wyświetla formularz dodawania nowej postaci, wczytując rasy, klasy i alignments z API.
        /// </summary>
        public async Task<IActionResult> Add()
        {
            var raceResponse = await _httpClient.GetStringAsync(
                $"https://www.dnd5eapi.co/api/2014/races"
            );
            using var raceDoc = JsonDocument.Parse(raceResponse);

            var classResponse = await _httpClient.GetStringAsync(
                $"https://www.dnd5eapi.co/api/2014/classes"
            );
            using var classDoc = JsonDocument.Parse(classResponse);

            var aligmentResponse = await _httpClient.GetStringAsync(
                $"https://www.dnd5eapi.co/api/2014/alignments"
            );
            using var alignmentDoc = JsonDocument.Parse(aligmentResponse);

            var races = raceDoc
                .RootElement.GetProperty("results")
                .EnumerateArray()
                .Select(r => new SelectListItem
                {
                    Text = r.GetProperty("name").GetString(),
                    Value = r.GetProperty("index").GetString(),
                })
                .ToList();

            var classes = classDoc
                .RootElement.GetProperty("results")
                .EnumerateArray()
                .Select(c => new SelectListItem
                {
                    Text = c.GetProperty("name").GetString(),
                    Value = c.GetProperty("index").GetString(),
                })
                .ToList();

            var alignments = alignmentDoc
                .RootElement.GetProperty("results")
                .EnumerateArray()
                .Select(c => new SelectListItem
                {
                    Text = c.GetProperty("name").GetString(),
                    Value = c.GetProperty("index").GetString(),
                })
                .ToList();

            ViewData["Races"] = races;
            ViewData["Classes"] = classes;
            ViewData["Alignments"] = alignments;

            return View("Add");
        }

        /// <summary>
        /// Dodaje nową postać dla użytkownika, uwzględniając ograniczenia konta standardowego/premium oraz początkowe wyposażenie.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CharacterRequest request)
        {
            var userIdClaim = User
                ?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                ?.Value;
            if (!int.TryParse(userIdClaim, out var currentUserId))
            {
                return Unauthorized(new { error = "Brak identyfikatora użytkownika." });
            }

            var premiumClaim = User?.FindFirst("premium")?.Value;
            var isPremium = string.Equals(premiumClaim, "true", StringComparison.OrdinalIgnoreCase);

            if (!isPremium)
            {
                var existingCount = characterService.GetCharacters(currentUserId).Count();
                if (existingCount >= 6)
                {
                    return BadRequest(
                        new
                        {
                            error = "Konto standardowe może mieć maksymalnie 6 postaci. Usuń jedną z nich lub przejdź na konto Premium, aby tworzyć kolejne.",
                        }
                    );
                }
            }

            var className = request.Character.Class;
            var raceName = request.Character.Race;
            //request.Character.SubClass = request.Character.SubClass ?? string.Empty;
            //request.Character.Spells = request.Character.Spells ?? string.Empty;

            request.Character.Proficiencies = request.Proficiencies;

            var raceUrl = $"https://www.dnd5eapi.co/api/2014/races/{raceName}/";
            var raceResponse = await _httpClient.GetStringAsync(raceUrl);
            using var raceDoc = JsonDocument.Parse(raceResponse);

            request.Character.Traits = new List<string>();
            if (raceDoc.RootElement.TryGetProperty("traits", out var traitsArray))
            {
                foreach (var trait in traitsArray.EnumerateArray())
                {
                    var index = trait.GetProperty("index").GetString();
                    request.Character.Traits.Add(index);
                }
            }

            int speed = raceDoc.RootElement.GetProperty("speed").GetInt32();
            request.Character.Speed = speed;

            var classResponse = await _httpClient.GetStringAsync(
                $"https://www.dnd5eapi.co/api/2014/classes/{className}/"
            );
            using var doc2 = JsonDocument.Parse(classResponse);
            int maxHP =
                doc2.RootElement.GetProperty("hit_die").GetInt32()
                + (int)((request.Character.Constitution - 10) / 2);
            request.Character.MaxHP = maxHP;
            request.Character.CurrentHP = maxHP;
            request.Character.TemporaryHP = 0;

            if (doc2.RootElement.TryGetProperty("proficiencies", out var proficienciesArray))
            {
                foreach (var proficiency in proficienciesArray.EnumerateArray())
                {
                    var index = proficiency.GetProperty("index").GetString();
                    request.Character.Proficiencies.Add(index);
                }
            }

            request.Character.ArmorClass = 10 + (int)((request.Character.Dexterity - 10) / 2);

            request.Character.Items = new List<Item>();

            if (doc2.RootElement.TryGetProperty("starting_equipment", out var eqArray))
            {
                foreach (var eq in eqArray.EnumerateArray())
                {
                    var itemName = eq.GetProperty("equipment").GetProperty("name").GetString();
                    var itemQuantity = eq.GetProperty("quantity").GetInt32();
                    request.Character.Items.Add(
                        new Item { Name = itemName, Quantity = itemQuantity }
                    );
                }
            }

            if (request.Items != null && request.Items.Any())
            {
                Console.WriteLine("Character Items");
                foreach (var item in request.Items)
                {
                    if (item.Name.Contains(";"))
                    {
                        string[] itemParts = item.Name.Split(';');
                        foreach (var subItem in itemParts)
                        {
                            string[] parts = subItem.Split('×');
                            var subItemName = parts[0];
                            var subItemQuantity = Int32.Parse(ClearSpaces(parts[1]));

                            request.Character.Items.Add(
                                new Item { Name = subItemName, Quantity = subItemQuantity }
                            );
                            Console.WriteLine(
                                "item: " + subItemName + " Quantity: " + subItemQuantity
                            );
                        }
                    }
                    else
                    {
                        if (item.Name.Contains("×"))
                        {
                            string[] parts = item.Name.Split('×');
                            item.Name = ClearSpaces(parts[0]);
                            item.Quantity = Int32.Parse(ClearSpaces(parts[1]));
                        }

                        request.Character.Items.Add(
                            new Item { Name = item.Name, Quantity = item.Quantity }
                        );

                        Console.WriteLine("item: " + item.Name + " Quantity: " + item.Quantity);
                    }
                }
            }

            request.Character.UserId = currentUserId;

            characterService.SaveCharacter(request.Character);
            return Ok(new { success = true, redirectUrl = Url.Action("Index") });
        }

        /// <summary>
        /// Usuwa postać i powiązane pliki awatarów.
        /// </summary>
        [HttpPost]
        public IActionResult Delete(int id)
        {
            DeleteAvatarFiles(id);
            characterService.DeleteCharacter(id);
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Usuwa pliki awatara postaci z katalogu wwwroot/images/avatars.
        /// </summary>
        private void DeleteAvatarFiles(int characterId)
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var avatarDir = Path.Combine(webRoot, "images", "avatars");
            if (!Directory.Exists(avatarDir))
                return;

            var stable = Path.Combine(avatarDir, $"avatar_{characterId}.png");
            if (System.IO.File.Exists(stable))
            {
                try
                {
                    System.IO.File.Delete(stable);
                }
                catch { }
            }
            foreach (var file in Directory.EnumerateFiles(avatarDir, $"avatar_{characterId}_*.png"))
            {
                try
                {
                    System.IO.File.Delete(file);
                }
                catch { }
            }
        }

        /// <summary>
        /// Pobiera listę umiejętności dla danej klasy w formacie ChoiceModel z API DnD5e.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProficiencies2(string className)
        {
            if (string.IsNullOrEmpty(className))
                return BadRequest("Missing class name");

            var choices = new List<ChoiceModel>();

            var classResponse = await _httpClient.GetStringAsync(
                $"https://www.dnd5eapi.co/api/2014/classes/{className}/"
            );
            using var doc = JsonDocument.Parse(classResponse);
            var proficiencyChoices = doc.RootElement.GetProperty("proficiency_choices");

            foreach (var choice in proficiencyChoices.EnumerateArray())
            {
                var desc = choice.GetProperty("desc").GetString();
                var choose = choice.GetProperty("choose").GetInt32();
                var from = choice.GetProperty("from");
                var options = from.GetProperty("options");

                var optionsList = new List<OptionModel>();

                foreach (var option in options.EnumerateArray())
                {
                    if (option.GetProperty("option_type").GetString() == "choice")
                    {
                        var options2 = option
                            .GetProperty("choice")
                            .GetProperty("from")
                            .GetProperty("options");
                        foreach (var option2 in options2.EnumerateArray())
                        {
                            var item2 = option2.GetProperty("item");
                            optionsList.Add(
                                new OptionModel
                                {
                                    Text = item2.GetProperty("name").GetString(),
                                    Value = item2.GetProperty("index").GetString(),
                                }
                            );
                        }
                    }
                    else
                    {
                        var item = option.GetProperty("item");
                        optionsList.Add(
                            new OptionModel
                            {
                                Text = item.GetProperty("name").GetString(),
                                Value = item.GetProperty("index").GetString(),
                            }
                        );
                    }
                }
                choices.Add(
                    new ChoiceModel
                    {
                        Description = desc,
                        ChooseCount = choose,
                        Options = optionsList,
                    }
                );
            }
            return Json(choices);
        }

        /// <summary>
        /// Pobiera listę umiejętności dla klasy w formie SelectListItem.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProficiencies(string className)
        {
            if (string.IsNullOrEmpty(className))
                return BadRequest("Missing class name");

            var endpoint = $"https://www.dnd5eapi.co/api/2014/classes/{className}";
            var result = await _httpClient.GetFromJsonAsync<DndClassProficiencyResponse>(endpoint);

            if (result?.Proficiency_Choices == null)
                return NotFound();

            var profOptions = result
                .Proficiency_Choices.SelectMany(pc => pc.From.Options)
                .Select(o => new SelectListItem { Text = o.Item.Name, Value = o.Item.Index })
                .ToList();

            return Json(profOptions);
        }

        /// <summary>
        /// Pobiera listę czarów dla danej klasy.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSpells(string className)
        {
            if (string.IsNullOrEmpty(className))
                return BadRequest("Missing class name");

            var endpoint = $"https://www.dnd5eapi.co/api/2014/classes/{className}/spells";
            var result = await _httpClient.GetFromJsonAsync<DndClassSpellResponse>(endpoint);

            if (result?.Spells == null)
                return NotFound();

            var spells = result
                .Spells.SelectMany(pc => pc.Results)
                .Select(o => new SelectListItem { Text = o.Name, Value = o.Index })
                .ToList();

            return Json(spells);
        }

        /// <summary>
        /// Pobiera przedmioty z określonej kategorii z API DnD5e.
        /// </summary>
        public async Task<List<ItemModel>> GetItemsByCategory(string category, int choose = 1)
        {
            var categoryResponse = await _httpClient.GetStringAsync(
                $"https://www.dnd5eapi.co/api/2014/equipment-categories/{category}/"
            );

            using var doc = JsonDocument.Parse(categoryResponse);
            var equipmentOptions = doc.RootElement.GetProperty("equipment");

            var items = new List<ItemModel>();
            foreach (var item in equipmentOptions.EnumerateArray())
            {
                items.Add(
                    new ItemModel { Name = item.GetProperty("name").GetString(), Quantity = 1 }
                );
            }
            return items;
        }

        /// <summary>
        /// Pobiera opcje początkowego wyposażenia dla klasy i generuje struktury wyboru przedmiotów.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetItems(string className)
        {
            if (string.IsNullOrEmpty(className))
                return BadRequest("Missing class name");

            var classResponse = await _httpClient.GetStringAsync(
                $"https://www.dnd5eapi.co/api/2014/classes/{className}/"
            );
            using var doc = JsonDocument.Parse(classResponse);
            var equipmentOptions = doc.RootElement.GetProperty("starting_equipment_options");

            // CHOOSE SET
            var choices = new List<ItemChoiceModel>();

            foreach (var choice in equipmentOptions.EnumerateArray()) // Select: (a) a light crossbow and 20 bolts or (b) any simple weapon
            {
                var desc = choice.GetProperty("desc").GetString();
                var choose = choice.GetProperty("choose").GetInt32();
                var from = choice.GetProperty("from");
                var options_set_type = from.GetProperty("option_set_type");

                Console.WriteLine("Desc: " + desc);
                var sets = new List<ItemSet>();
                // SETS
                if (options_set_type.GetString() == "equipment_category")
                {
                    // Select any from melee weapons
                    var category = from.GetProperty("equipment_category").GetProperty("index"); // Any simple weapon
                    List<ItemModel> items = await GetItemsByCategory(category.GetString());
                    ItemSet set = new ItemSet { CategoryCount = 1, CategoryItems = items };
                    sets.Add(set);
                }
                else if (options_set_type.GetString() == "options_array")
                {
                    // Select shortbow and 20 bolts
                    // Select rapier
                    var options = from.GetProperty("options");
                    //List<ItemModel> items = new List<ItemModel>();
                    foreach (var option in options.EnumerateArray())
                    {
                        var option_type = option.GetProperty("option_type");

                        if (option_type.GetString() == "multiple")
                        {
                            var itemSet = new ItemSet();
                            var multiple_items = option.GetProperty("items");
                            foreach (var _item in multiple_items.EnumerateArray())
                            {
                                if (_item.GetProperty("option_type").ToString() == "choice")
                                {
                                    var category = _item
                                        .GetProperty("choice")
                                        .GetProperty("from")
                                        .GetProperty("equipment_category")
                                        .GetProperty("index");
                                    var chooseCount = _item
                                        .GetProperty("choice")
                                        .GetProperty("choose")
                                        .GetInt32();
                                    List<ItemModel> items = await GetItemsByCategory(
                                        category.GetString()
                                    );
                                    //ItemSet set = new ItemSet { ChooseCount = 0, Items = items };
                                    itemSet.CategoryCount = chooseCount;
                                    itemSet.CategoryItems.AddRange(items);
                                }
                                else if (
                                    _item.GetProperty("option_type").ToString()
                                    == "counted_reference"
                                )
                                {
                                    var item = new ItemModel
                                    {
                                        Quantity = Int32.Parse(
                                            _item.GetProperty("count").ToString()
                                        ),
                                        Name = _item
                                            .GetProperty("of")
                                            .GetProperty("name")
                                            .ToString(),
                                    };
                                    itemSet.RegularItems.Add(item);
                                    itemSet.RegularCount = 1;
                                }
                            }
                            sets.Add(itemSet);
                        }
                        else if (option_type.GetString() == "counted_reference")
                        {
                            var itemSet = new ItemSet();
                            itemSet.RegularItems.Add(
                                new ItemModel
                                {
                                    Name = option.GetProperty("of").GetProperty("name").GetString(), //Crossbow, light  ||  Bolts
                                    Quantity = option.GetProperty("count").GetInt32(), // 1 || 20
                                }
                            );
                            itemSet.RegularCount = 1;
                            sets.Add(itemSet);
                        }
                        else if (option_type.GetString() == "choice")
                        {
                            var item = option.GetProperty("choice").GetProperty("from");
                            var chooseCount = option
                                .GetProperty("choice")
                                .GetProperty("choose")
                                .GetInt32();
                            var option_set_type = item.GetProperty("option_set_type");
                            Console.WriteLine("Option set type: " + option_set_type.GetString());
                            if (option_set_type.GetString() == "equipment_category")
                            {
                                var category = item.GetProperty("equipment_category")
                                    .GetProperty("index");
                                Console.WriteLine("category: " + category);
                                List<ItemModel> items = await GetItemsByCategory(
                                    category.GetString()
                                );
                                ItemSet set = new ItemSet
                                {
                                    CategoryCount = chooseCount,
                                    CategoryItems = items,
                                };
                                Console.WriteLine("items count: " + items.Count);
                                sets.Add(set);
                            }
                        }
                    }
                }

                choices.Add(
                    new ItemChoiceModel
                    {
                        Description = desc,
                        ChooseCount = choose,
                        ItemSets = sets.ToList(),
                    }
                );
            }
            foreach (var choice in choices)
            {
                choice.Print();
            }
            return Json(choices);
        }

        /// <summary>
        /// Tworzy przykładowego człowieka wojownika z początkowym wyposażeniem.
        /// </summary>
        public async Task<IActionResult> SampleHumanFighter([FromBody] string name)
        {
            var userIdClaim = User
                ?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                ?.Value;
            if (!int.TryParse(userIdClaim, out var currentUserId))
            {
                return Unauthorized();
            }

            var premiumClaim = User?.FindFirst("premium")?.Value;
            var isPremium = string.Equals(premiumClaim, "true", StringComparison.OrdinalIgnoreCase);

            if (!isPremium)
            {
                var existingCount = characterService.GetCharacters(currentUserId).Count();
                if (existingCount >= 2)
                {
                    return BadRequest(
                        new
                        {
                            error = "Konto standardowe może mieć maksymalnie 6 postaci. Usuń jedną z nich lub przejdź na konto Premium, aby tworzyć kolejne.",
                        }
                    );
                }
            }

            List<string> proficiencies = new List<string>
            {
                "skill-athletics",
                "skill-perception",
                "light-armor",
                "medium-armor",
                "heavy-armor",
                "shields",
                "simple-weapons",
                "martial-weapons",
            };
            Character character = new Character
            {
                Name = name,
                Alignment = "lawful-neutral",
                Strength = 16,
                Dexterity = 13,
                Constitution = 14,
                Intelligence = 10,
                Wisdom = 12,
                Charisma = 10,
                Race = "human",
                Class = "fighter",
                MaxHP = 12,
                CurrentHP = 12,
                TemporaryHP = 0,
                ArmorClass = 18,
                Speed = 30,
                UserId = currentUserId,
                Proficiencies = proficiencies,
            };
            int charId = characterService.SaveCharacter(character);
            List<Item> items = new List<Item>
            {
                new Item("Chain Mail", 1, charId),
                new Item("Shield", 1, charId),
                new Item("Longsword", 1, charId),
                new Item("Shortsword", 1, charId),
                new Item("Light Crossbow", 1, charId),
                new Item("Bolts", 20, charId),
                new Item("Explorer\'s pack", 1, charId),
            };
            foreach (Item item in items)
            {
                characterService.AddItem(item);
            }
            return Ok(new { success = true, redirectUrl = Url.Action("Index") });
        }

        /// <summary>
        /// Tworzy przykładowego półelfa złodzieja z początkowym wyposażeniem i cechami.
        /// </summary>
        public async Task<IActionResult> SampleHalfElfRogue([FromBody] string name)
        {
            var userIdClaim = User
                ?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                ?.Value;
            if (!int.TryParse(userIdClaim, out var currentUserId))
            {
                return Unauthorized();
            }

            var premiumClaim = User?.FindFirst("premium")?.Value;
            var isPremium = string.Equals(premiumClaim, "true", StringComparison.OrdinalIgnoreCase);

            if (!isPremium)
            {
                var existingCount = characterService.GetCharacters(currentUserId).Count();
                if (existingCount >= 2)
                {
                    return BadRequest(
                        new
                        {
                            error = "Konto standardowe może mieć maksymalnie 6 postaci. Usuń jedną z nich lub przejdź na konto Premium, aby tworzyć kolejne.",
                        }
                    );
                }
            }

            List<string> proficiencies = new List<string>
            {
                "skill-stealth",
                "skill-acrobatics",
                "skill-sleight-of-hand",
                "skill-perception",
                "light-armor",
                "simple-weapons",
                "hand-crossbows",
                "longswords",
                "rapiers",
                "shortswords",
                "thieves-tools",
            };
            List<string> traits = new List<string>
            {
                "darkvision",
                "fey-ancestry",
                "skill-versatility",
            };
            Character character = new Character
            {
                Name = name,
                Alignment = "chaotic-evil",
                Strength = 8,
                Dexterity = 17,
                Constitution = 14,
                Intelligence = 10,
                Wisdom = 12,
                Charisma = 14,
                Race = "half-elf",
                Class = "rogue",
                MaxHP = 10,
                CurrentHP = 10,
                TemporaryHP = 0,
                ArmorClass = 14,
                Speed = 30,
                UserId = currentUserId,
                Proficiencies = proficiencies,
                Traits = traits,
            };
            int charId = characterService.SaveCharacter(character);
            List<Item> items = new List<Item>
            {
                new Item("Rapier", 1, charId),
                new Item("Shortbow", 1, charId),
                new Item("Arrow", 20, charId),
                new Item("Leather Armor", 1, charId),
                new Item("Dagger", 2, charId),
                new Item("Thieves' Tools", 1, charId),
                new Item("Burglar\'s pack", 1, charId),
            };
            foreach (Item item in items)
            {
                characterService.AddItem(item);
            }
            return Ok(new { success = true, redirectUrl = Url.Action("Index") });
        }

        /// <summary>
        /// Tworzy przykładowego krasnoluda kapłana z początkowym wyposażeniem i cechami.
        /// </summary>
        public async Task<IActionResult> SampleDwarfCleric([FromBody] string name)
        {
            var userIdClaim = User
                ?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                ?.Value;
            if (!int.TryParse(userIdClaim, out var currentUserId))
            {
                return Unauthorized();
            }

            var premiumClaim = User?.FindFirst("premium")?.Value;
            var isPremium = string.Equals(premiumClaim, "true", StringComparison.OrdinalIgnoreCase);

            if (!isPremium)
            {
                var existingCount = characterService.GetCharacters(currentUserId).Count();
                if (existingCount >= 2)
                {
                    return BadRequest(
                        new
                        {
                            error = "Konto standardowe może mieć maksymalnie 6 postaci. Usuń jedną z nich lub przejdź na konto Premium, aby tworzyć kolejne.",
                        }
                    );
                }
            }

            List<string> proficiencies = new List<string>
            {
                "skill-medicine",
                "skill-insight",
                "light-armor",
                "medium-armor",
                "heavy-armor",
                "shields",
                "simple-weapons",
            };
            List<string> traits = new List<string>
            {
                "darkvision",
                "dwarven-resilience",
                "stonecunning",
                "dwarven-combat-training",
                "tool-proficiency",
            };
            Character character = new Character
            {
                Name = name,
                Alignment = "lawful-good",
                Strength = 14,
                Dexterity = 10,
                Constitution = 16,
                Intelligence = 10,
                Wisdom = 16,
                Charisma = 8,
                Race = "dwarf",
                Class = "rogue",
                MaxHP = 11,
                CurrentHP = 11,
                TemporaryHP = 0,
                ArmorClass = 18,
                Speed = 25,
                UserId = currentUserId,
                Proficiencies = proficiencies,
                Traits = traits,
            };
            int charId = characterService.SaveCharacter(character);
            List<Item> items = new List<Item>
            {
                new Item("Chain Mail", 1, charId),
                new Item("Shield", 1, charId),
                new Item("Warhammer", 1, charId),
                new Item("Holy Symbol", 1, charId),
                new Item("Priest\'s pack", 1, charId),
            };
            foreach (Item item in items)
            {
                characterService.AddItem(item);
            }
            return Ok(new { success = true, redirectUrl = Url.Action("Index") });
        }

        /// <summary>
        /// Tworzy przykładowego elfa czarodzieja z początkowym wyposażeniem i cechami.
        /// </summary>
        public async Task<IActionResult> SampleElfWizard([FromBody] string name)
        {
            var userIdClaim = User
                ?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                ?.Value;
            if (!int.TryParse(userIdClaim, out var currentUserId))
            {
                return Unauthorized();
            }

            var premiumClaim = User?.FindFirst("premium")?.Value;
            var isPremium = string.Equals(premiumClaim, "true", StringComparison.OrdinalIgnoreCase);

            if (!isPremium)
            {
                var existingCount = characterService.GetCharacters(currentUserId).Count();
                if (existingCount >= 2)
                {
                    return BadRequest(
                        new
                        {
                            error = "Konto standardowe może mieć maksymalnie 6 postaci. Usuń jedną z nich lub przejdź na konto Premium, aby tworzyć kolejne.",
                        }
                    );
                }
            }

            List<string> proficiencies = new List<string>
            {
                "skill-arcana",
                "skill-investigation",
                "daggers",
                "quarterstaffs",
                "light-crossbows",
            };
            List<string> traits = new List<string>
            {
                "darkvision",
                "fey-ancestry",
                "trance",
                "keen-senses",
            };
            Character character = new Character
            {
                Name = name,
                Alignment = "neutral-good",
                Strength = 8,
                Dexterity = 14,
                Constitution = 14,
                Intelligence = 16,
                Wisdom = 12,
                Charisma = 10,
                Race = "elf",
                Class = "wizard",
                MaxHP = 8,
                CurrentHP = 8,
                TemporaryHP = 0,
                ArmorClass = 12,
                Speed = 30,
                UserId = currentUserId,
                Proficiencies = proficiencies,
                Traits = traits,
            };
            int charId = characterService.SaveCharacter(character);
            List<Item> items = new List<Item>
            {
                new Item("Quarterstaff", 1, charId),
                new Item("Dagger", 1, charId),
                new Item("Spellbook", 1, charId),
                new Item("Component Pouch", 1, charId),
                new Item("Scholar\'s pack", 1, charId),
            };
            foreach (Item item in items)
            {
                characterService.AddItem(item);
            }
            return Ok(new { success = true, redirectUrl = Url.Action("Index") });
        }

        /// <summary>
        /// Tworzy przykładowego półorka barbarzyńcę z początkowym wyposażeniem i cechami.
        /// </summary>
        public async Task<IActionResult> SampleHalfOrcBarbarian([FromBody] string name)
        {
            var userIdClaim = User
                ?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                ?.Value;
            if (!int.TryParse(userIdClaim, out var currentUserId))
            {
                return Unauthorized();
            }

            var premiumClaim = User?.FindFirst("premium")?.Value;
            var isPremium = string.Equals(premiumClaim, "true", StringComparison.OrdinalIgnoreCase);

            if (!isPremium)
            {
                var existingCount = characterService.GetCharacters(currentUserId).Count();
                if (existingCount >= 2)
                {
                    return BadRequest(
                        new
                        {
                            error = "Konto standardowe może mieć maksymalnie 6 postaci. Usuń jedną z nich lub przejdź na konto Premium, aby tworzyć kolejne.",
                        }
                    );
                }
            }

            List<string> proficiencies = new List<string>
            {
                "skill-athletics",
                "skill-intimidation",
                "light-armor",
                "medium-armor",
                "shields",
                "simple-weapons",
                "martial-weapons",
            };
            List<string> traits = new List<string>
            {
                "darkvision",
                "savage-attacks",
                "relentless-endurance",
                "menacing",
            };
            Character character = new Character
            {
                Name = name,
                Alignment = "chaotic-neutral",
                Strength = 17,
                Dexterity = 14,
                Constitution = 16,
                Intelligence = 8,
                Wisdom = 10,
                Charisma = 8,
                Race = "half-orc",
                Class = "barbarian",
                MaxHP = 15,
                CurrentHP = 15,
                TemporaryHP = 0,
                ArmorClass = 15,
                Speed = 30,
                UserId = currentUserId,
                Proficiencies = proficiencies,
                Traits = traits,
            };
            int charId = characterService.SaveCharacter(character);
            List<Item> items = new List<Item>
            {
                new Item("Greataxe", 1, charId),
                new Item("Handaxe", 2, charId),
                new Item("Javelin", 4, charId),
                new Item("Explorer\'s pack", 1, charId),
            };
            foreach (Item item in items)
            {
                characterService.AddItem(item);
            }
            return Ok(new { success = true, redirectUrl = Url.Action("Index") });
        }
    }
}
