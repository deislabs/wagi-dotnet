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
      if (((username?.ToLowerInvariant() == "admin") || (username?.ToLowerInvariant() == "superadmin") || (username?.ToLowerInvariant() == "specialadmin")) && (password?.ToLowerInvariant() == "admin"))
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

        if (username?.ToLowerInvariant() == "superadmin")
        {
          claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "superadmin"));
        }

        if (username?.ToLowerInvariant() == "specialadmin")
        {
          claimsIdentity.AddClaim(new Claim(ClaimTypes.UserData, "IsSpecialAdmin"));
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
