using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Projekt.Model.ApiResponses;
using Projekt.Model.DataModels;
using Projekt.Services.ConcreteServices;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.Authorization;

namespace Projekt.Web.Controllers
{
    [Authorize]
    public class CharacterController : BaseController
    {
        private readonly HttpClient _httpClient;
        protected readonly ICharacterService characterService;
        private readonly IWebHostEnvironment _env;

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

        public IActionResult Index()
        {
            var userIdClaim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var currentUserId))
            {
                return Unauthorized();
            }

            var characters = characterService.GetCharacters(currentUserId).ToList();
            return View(characters);
        }

        public string ClearSpaces(string sentence){
            return sentence.Contains(" ") ? sentence.Replace(" ", "") : sentence;
        }

        public async Task<IActionResult> Details(int id)
        {
            var character = characterService.GetCharacter(id);

            var proficiencies = await GetNamesByIndex(character.Proficiencies, "proficiencies");
            var traits = await GetNamesByIndex(character.Traits, "traits");
            ViewData["Proficiencies"] = proficiencies;
            ViewData["Traits"] = traits;

            return View(character);
        }

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

        [HttpPost]
        public async Task<bool> SaveNotes(int charId, string? notes) {
            try {
                var character = characterService.GetCharacter(charId);
                Console.WriteLine("Character: " + charId);
                Console.WriteLine("Notes: " + notes);
                character.Notes = notes;

                characterService.UpdateCharacter(character);
                return true;
            } catch (Exception e) {
                
            }
            return false;    
        }

        public async Task<IActionResult> Equipment(int characterId){
            var character = characterService.GetCharacter(characterId);
            
            var response = await _httpClient.GetStringAsync($"https://www.dnd5eapi.co/api/2014/equipment");
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

        [HttpPost]
        public IActionResult AddEquipment(Item item){
            characterService.AddItem(item);
            var character = characterService.GetCharacter(item.CharacterId);
            return RedirectToAction("Details", character);
        }

        public IActionResult RemoveEquipment(int itemId){

            characterService.RemoveItem(itemId);
            return RedirectToAction("Index");
        }

        public async Task<string> GetNameByIndex(string url)
        {
            var response = await _httpClient.GetStringAsync(
                $"https://www.dnd5eapi.co/api/2014/{url}"
            );
            using var doc = JsonDocument.Parse(response);
            return doc.RootElement.GetProperty("name").GetString();
        }

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

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CharacterRequest request)
        {
            var className = request.Character.Class;
            var raceName = request.Character.Race;
            request.Character.SubClass = request.Character.SubClass ?? string.Empty;
            request.Character.Spells = request.Character.Spells ?? string.Empty;

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
                    request.Character.Items.Add(new Item { Name = itemName, Quantity = itemQuantity});
                }
            }
            
            if (request.Items != null && request.Items.Any())
            {
                
                Console.WriteLine("Character Items");
                foreach (var item in request.Items)
                {
                    if (item.Name.Contains(";")) {
                        string[] itemParts = item.Name.Split(';');
                        foreach(var subItem in itemParts) {
                            string[] parts = subItem.Split('×');
                            var subItemName = parts[0];
                            var subItemQuantity = Int32.Parse(ClearSpaces(parts[1]));
                            
                            request.Character.Items.Add(new Item
                            {
                                Name = subItemName,
                                Quantity = subItemQuantity
                            });
                            Console.WriteLine("item: " + subItemName + " Quantity: " + subItemQuantity);
                        }
                    } else {
                        if (item.Name.Contains("×")) {
                            string[] parts = item.Name.Split('×');
                            item.Name = ClearSpaces(parts[0]);
                            item.Quantity = Int32.Parse(ClearSpaces(parts[1]));
                        }

                        request.Character.Items.Add(new Item
                        {
                            Name = item.Name,
                            Quantity = item.Quantity
                        });
                        
                        Console.WriteLine("item: " + item.Name + " Quantity: " + item.Quantity);
                    }
                }
            }

            var userIdClaim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var currentUserId))
            {
                request.Character.UserId = currentUserId;
            }
            else
            {
                return Unauthorized(new { error = "Brak identyfikatora użytkownika." });
            }

            characterService.SaveCharacter(request.Character);
            return Ok(new { success = true, redirectUrl = Url.Action("Index") });
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            DeleteAvatarFiles(id);
            characterService.DeleteCharacter(id);
            return RedirectToAction("Index");
        }

        private void DeleteAvatarFiles(int characterId)
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var avatarDir = Path.Combine(webRoot, "images", "avatars");
            if (!Directory.Exists(avatarDir)) return;

            var stable = Path.Combine(avatarDir, $"avatar_{characterId}.png");
            if (System.IO.File.Exists(stable))
            {
                try { System.IO.File.Delete(stable); } catch { }
            }
            foreach (var file in Directory.EnumerateFiles(avatarDir, $"avatar_{characterId}_*.png"))
            {
                try { System.IO.File.Delete(file); } catch { }
            }
        }

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
                    if (option.GetProperty("option_type").GetString() == "choice") {
                        var options2 = option.GetProperty("choice").GetProperty("from").GetProperty("options");
                        foreach (var option2 in options2.EnumerateArray()) {
                            var item2 = option2.GetProperty("item");
                            optionsList.Add(
                                new OptionModel
                                {
                                    Text = item2.GetProperty("name").GetString(),
                                    Value = item2.GetProperty("index").GetString(),
                                }
                            );
                        }
                    } else {
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
                    new ItemModel
                    {
                        Name = item.GetProperty("name").GetString(),
                        Quantity = 1,
                    }
                );
            }
            return items;
        }
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
                if (options_set_type.GetString() == "equipment_category") {
                    // Select any from melee weapons
                    var category = from.GetProperty("equipment_category").GetProperty("index"); // Any simple weapon
                    List<ItemModel> items = await GetItemsByCategory(category.GetString());
                    ItemSet set = new ItemSet { CategoryCount = 1, CategoryItems = items };
                    sets.Add(set);

                } else if (options_set_type.GetString() == "options_array") {
                    // Select shortbow and 20 bolts
                    // Select rapier
                    var options = from.GetProperty("options");
                    //List<ItemModel> items = new List<ItemModel>();
                    foreach (var option in options.EnumerateArray())
                    {
                        var option_type = option.GetProperty("option_type");
                        
                        if (option_type.GetString() == "multiple") {
                            var itemSet = new ItemSet();
                            var multiple_items = option.GetProperty("items");
                            foreach (var _item in multiple_items.EnumerateArray()) {
                                if (_item.GetProperty("option_type").ToString() == "choice") {
                                    var category = _item.GetProperty("choice").GetProperty("from").GetProperty("equipment_category").GetProperty("index");
                                    var chooseCount =  _item.GetProperty("choice").GetProperty("choose").GetInt32();
                                    List<ItemModel> items = await GetItemsByCategory(category.GetString());
                                    //ItemSet set = new ItemSet { ChooseCount = 0, Items = items };
                                    itemSet.CategoryCount = chooseCount;
                                    itemSet.CategoryItems.AddRange(items);
                                } else if (_item.GetProperty("option_type").ToString() == "counted_reference") {
                                    var item = new ItemModel {
                                        Quantity = Int32.Parse(_item.GetProperty("count").ToString()),
                                        Name = _item.GetProperty("of").GetProperty("name").ToString(),
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
                            var chooseCount =  option.GetProperty("choice").GetProperty("choose").GetInt32();
                            var option_set_type = item.GetProperty("option_set_type");
                            Console.WriteLine("Option set type: " + option_set_type.GetString());
                            if (option_set_type.GetString() == "equipment_category")
                            {
                                var category = item.GetProperty("equipment_category").GetProperty("index");
                                Console.WriteLine("category: " + category);
                                List<ItemModel> items = await GetItemsByCategory(category.GetString());
                                ItemSet set = new ItemSet { CategoryCount = chooseCount, CategoryItems = items };
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
            foreach (var choice in choices) {
                choice.Print();
            }
            return Json(choices);
        } 
        /*
        [HttpGet]
        public async Task<IActionResult> GetItems(string className)
        {
            if (string.IsNullOrEmpty(className))
                return BadRequest("Missing class name");

            var classResponse = await _httpClient.GetStringAsync(
                $"https://www.dnd5eapi.co/api/2014/classes/{className}/"
            );

            using var doc = JsonDocument.Parse(classResponse);
            if (
                !doc.RootElement.TryGetProperty(
                    "starting_equipment_options",
                    out var equipmentOptions
                )
            )
                return NotFound("No starting equipment options found for this class.");

            var choices = new List<ItemChoiceModel>();

            foreach (var choice in equipmentOptions.EnumerateArray())
            {
                var desc = choice.GetProperty("desc").GetString();
                var choose = choice.GetProperty("choose").GetInt32();
                var from = choice.GetProperty("from");

                var itemSets = new List<ItemSet>();
                var optionSetType = from.GetProperty("option_set_type").GetString();

                // --- Obsługa "options_array" ---
                if (optionSetType == "options_array")
                {
                    foreach (var option in from.GetProperty("options").EnumerateArray())
                    {
                        var items = new List<ItemModel>();

                        var optionType = option.GetProperty("option_type").GetString();

                        switch (optionType)
                        {
                            case "counted_reference":
                                var itemRef = option.GetProperty("of");
                                items.Add(
                                    new ItemModel
                                    {
                                        Name = itemRef.GetProperty("name").GetString(),
                                        Quantity = option.GetProperty("count").GetInt32(),
                                    }
                                );
                                break;

                            case "multiple":
                                foreach (
                                    var subItem in option.GetProperty("items").EnumerateArray()
                                )
                                {
                                    var subType = subItem.GetProperty("option_type").GetString();
                                    if (subType == "counted_reference")
                                    {
                                        var subRef = subItem.GetProperty("of");
                                        items.Add(
                                            new ItemModel
                                            {
                                                Name = subRef.GetProperty("name").GetString(),
                                                Quantity = subItem.GetProperty("count").GetInt32(),
                                            }
                                        );
                                    }
                                }
                                break;

                            case "choice":
                                var choiceDesc = option
                                    .GetProperty("choice")
                                    .GetProperty("desc")
                                    .GetString();
                                items.Add(new ItemModel { Name = choiceDesc, Quantity = 1 });
                                break;

                            default:
                                // fallback w razie nowych struktur
                                items.Add(new ItemModel { Name = optionType, Quantity = 1 });
                                break;
                        }

                        itemSets.Add(new ItemSet { ChooseCount = 1, Items = items });
                    }
                }
                // --- Obsługa innych typów (np. equipment_category: holy symbols) ---
                else if (optionSetType == "equipment_category")
                {
                    var catName = from.GetProperty("equipment_category")
                        .GetProperty("name")
                        .GetString();
                    itemSets.Add(
                        new ItemSet
                        {
                            ChooseCount = 1,
                            Items = new List<ItemModel>
                            {
                                new ItemModel
                                {
                                    Name = $"Any from category: {catName}",
                                    Quantity = 1,
                                },
                            },
                        }
                    );
                }

                choices.Add(
                    new ItemChoiceModel
                    {
                        Description = desc,
                        ChooseCount = choose,
                        ItemSets = itemSets,
                    }
                );
            }

            return Ok(choices);
        }

        [HttpGet]
        public async Task<IActionResult> GetCategoryItems(string category)
        {
            if (string.IsNullOrEmpty(category))
                return BadRequest("Missing category name");

            // zamiana spacji na myślniki, bo tak działa API
            var formattedCategory = category.ToLower().Replace(" ", "-");

            var url = $"https://www.dnd5eapi.co/api/2014/equipment-categories/{formattedCategory}";

            try
            {
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return NotFound($"Category '{category}' not found.");

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("equipment", out var equipmentArray))
                    return NotFound("No equipment found in this category.");

                var items = new List<ItemModel>();

                foreach (var item in equipmentArray.EnumerateArray())
                {
                    items.Add(
                        new ItemModel { Name = item.GetProperty("name").GetString()!, Quantity = 1 }
                    );
                }

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching category '{category}': {ex.Message}");
            }
        }*/
    }
}
