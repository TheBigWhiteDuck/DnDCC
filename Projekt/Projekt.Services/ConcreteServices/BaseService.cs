using AutoMapper;
using Microsoft.Extensions.Logging;
using Projekt.DAL;

namespace Projekt.Services.ConcreteServices;

/// <summary>
/// Bazowy serwis dla warstwy biznesowej, zapewniający dostęp do DbContext, Mappera i Loggera.
/// </summary>
public abstract class BaseService
{
    protected readonly ApplicationDbContext DbContext = null!;
    protected readonly ILogger Logger = null!;
    protected readonly IMapper Mapper = null!;

    /// <summary>
    /// Tworzy instancję bazowego serwisu.
    /// </summary>
    public BaseService(ApplicationDbContext dbContext, IMapper mapper, ILogger logger)
    {
        DbContext = dbContext;
        Logger = logger;
        Mapper = mapper;
    }
}
