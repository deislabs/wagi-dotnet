using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace watmwithauth.Controllers
{
  public class HomeController : Controller
  {
    public IActionResult Login()
    {
      return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password, string returnUrl)
    {
      if (((username?.ToLowerInvariant() == "admin") || (username?.ToLowerInvariant() == "writer") || (username?.ToLowerInvariant() == "reader")) && (password?.ToLowerInvariant() == "admin"))
      {
        var claimsIdentity = new ClaimsIdentity(
          new[] {
                    new Claim(ClaimTypes.Name,username),
           },
           CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
          AllowRefresh = true,
          ExpiresUtc = DateTimeOffset.Now.AddDays(1),
          IsPersistent = true,
        };

        if (username?.ToLowerInvariant() == "reader")
        {
          claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "blobreader"));
        }

        if (username?.ToLowerInvariant() == "writer")
        {
          claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "blobwriter"));
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
        if (string.IsNullOrEmpty(returnUrl))
        {
          return RedirectToAction("Login", "Home");
        }
        return Redirect(returnUrl);
      }
      else
      {
        return View();
      }
    }

    public async Task<IActionResult> Logout()
    {
      await HttpContext.SignOutAsync();
      return RedirectToAction("Login", "Home");
    }

    public IActionResult AccessDenied(string returnUrl)
    {
      ViewData["ReturnUrl"] = returnUrl;
      return View();
    }
  }
}
