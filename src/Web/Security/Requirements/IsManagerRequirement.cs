using Microsoft.AspNetCore.Authorization;

namespace Forum.Security;

public class IsManagerRequirement : IAuthorizationRequirement
{
}