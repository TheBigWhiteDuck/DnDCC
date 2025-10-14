using Microsoft.AspNetCore.Identity;

namespace Projekt.Model.DataModels;

public class User : IdentityUser<int>
{
    public virtual IList<Character> Characters { get; set; } = new List<Character>();
}

