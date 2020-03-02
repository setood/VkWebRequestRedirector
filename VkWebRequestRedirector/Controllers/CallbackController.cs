using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using VkNet;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.Keyboard;
using VkNet.Model.RequestParams;
using VkNet.Utils;
using VkWebRequestRedirector.Model;

namespace VkWebRequestRedirector.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CallbackController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public CallbackController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET api/values
        [HttpPost]
        public async Task<IActionResult> Callback([FromBody] object updatesObj)
        {
            Updates updates = JsonConvert.DeserializeObject<Updates>(updatesObj.ToString());
            if (updates.Type == "confirmation")
            {
                return Ok(_configuration["Config:Confirmation"]);
            }
            ///////////////////////
            var api = new VkApi();
            api.Authorize(new ApiAuthParams {AccessToken = _configuration["Config:AccessToken"]});
            var msg = Message.FromJson(new VkResponse(updates.Object));
            var keyboard = new KeyboardBuilder()
                .AddButton("Подтвердить", "btnValue", KeyboardButtonColor.Primary)
                .SetInline(false)
                .AddLine()
                .AddButton("Отменить", "btnValue", KeyboardButtonColor.Primary)
                .Build();
            api.Messages.Send(new MessagesSendParams{ 
                RandomId = new DateTime().Millisecond,
                PeerId = msg.PeerId.Value,
                Message = "redir",
                Keyboard = keyboard
            });
            ////////////////
            string url = Environment.GetEnvironmentVariable("RederectURI");
            var httpWebRequest = (HttpWebRequest) WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = updatesObj.ToString();

                streamWriter.Write(json);
            }

            var httpResponse = (HttpWebResponse) await httpWebRequest.GetResponseAsync();
            return Ok("ok");
        }
    }
}