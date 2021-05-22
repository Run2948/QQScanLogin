using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using QQScanLogin.Models;

namespace QQScanLogin.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return Redirect("~/");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(ScanViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 创建用户登录标识，Cookie名称与IServiceCollection中配置的一样即可
                var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                // 添加之后，可使用User.Identity.Name获取该值
                identity.AddClaim(new Claim(ClaimTypes.Name, model.Nick));
                // identity中还可以添加自定义数据
                identity.AddClaim(new Claim("qq", model.Number));
                // var customValue = User.Claims.SingleOrDefault(s => s.Type == "qq").Value;
                await HttpContext.SignInAsync(new ClaimsPrincipal(identity));

                return Redirect("~/");
            }

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult QrCode()
        {
            var (stream, qrsig) = ScanLoginHelper.GetLoginQrCode();
            HttpContext.Session.SetString("qrsig", qrsig);
            return File(stream, "image/png");
        }

        [HttpGet]
        public IActionResult QrResult()
        {
            var qrsig = HttpContext.Session.GetString("qrsig");
            if (string.IsNullOrEmpty(qrsig))
            {
                return Json(new { success = false, message = "二维码已过期，请刷新后重试！" });
            }
            var (success, message, data) = ScanLoginHelper.GetQqScanResult(qrsig);
            return Json(new { success, message, data });
        }
    }
}
