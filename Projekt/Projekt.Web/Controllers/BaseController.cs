using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Projekt.Web.Controllers
{
    /// <summary>
    /// Bazowy kontroler MVC, dostarcza podstawowe zależności: logger, mapper i lokalizator tekstów.
    /// </summary>
    public abstract class BaseController : Controller
    {
        protected readonly IStringLocalizer Localizer;
        protected readonly ILogger Logger;
        protected readonly IMapper Mapper;

        /// <summary>
        /// Tworzy instancję bazowego kontrolera i inicjalizuje zależności.
        /// </summary>
        public BaseController(ILogger logger, IMapper mapper, IStringLocalizer localizer)
        {
            Localizer = localizer;
            Logger = logger;
            Mapper = mapper;
        }
    }
}
