using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Projekt.Model.ApiResponses;
using Projekt.Services.ConcreteServices;

namespace Projekt.Web.Controllers
{
    public class CharacterController : BaseController
    {
        private readonly HttpClient _httpClient;
        protected readonly ICharacterService characterService;
        public CharacterController(IHttpClientFactory httpClientFactory, ICharacterService _characterService, ILogger logger, IMapper mapper, IStringLocalizer localizer)
            : base(logger,mapper,localizer)
        {
            _httpClient = httpClientFactory.CreateClient();
            characterService = _characterService;
        }

        public async Task<Dictionary<string, object>> CreateDictionary(IList<string> data, string type){
            var result = new Dictionary<string, object>();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var obj in data)
            {
                string json = await _httpClient.GetStringAsync($"/api/{type}/{obj}");
                var jsonData = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);

                if (jsonData != null && jsonData.ContainsKey("name"))
                {
                    string name = jsonData["name"].ToString();
                    result[name] = jsonData;
                }
            }
            return result;
        }

        public IActionResult Index(){
            // UWAGA. Jak będzie dodane logowanie, tutaj wrzucić userId
            var characters = characterService.GetCharacters(null);
            return View(characters);
        }

        public IActionResult Details(int id){
            var character = characterService.GetCharacter(id);

            var proficiencies = CreateDictionary(character.Proficiencies, "proficiencies");
            var traits = CreateDictionary(character.Traits, "traits");
            var spells = CreateDictionary(character.Spells, "spells");
            ViewData["Proficiencies"] = proficiencies;
            ViewData["Traits"] = traits;
            ViewData["Spells"] = spells;

            return View(character);
        }

        public IActionResult Add(){
            return View("Add");
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

            var profOptions = result.Proficiency_Choices
                .SelectMany(pc => pc.From.Options)
                .Select(o => new SelectListItem
                {
                    Text = o.Item.Name,
                    Value = o.Item.Index
                })
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

            var spells = result.Spells
                .SelectMany(pc => pc.Results)
                .Select(o => new SelectListItem
                {
                    Text = o.Name,
                    Value = o.Index
                })
                .ToList();

            return Json(spells);
        }
    }
}
