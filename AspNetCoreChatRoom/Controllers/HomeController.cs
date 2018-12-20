using Microsoft.AspNetCore.Mvc;
using System;

namespace AspNetCoreChatRoom.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View("InsertUserName");
        }

        [HttpPost]
        public IActionResult Index(string username, string password)
        {
            ChatWebSocketMiddleware.userName = username;
            return View("Index", username);
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
