using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BabyNames.Controllers
{
    public class CMUDict
    {
        public JObject Cache { get; set; } = new JObject();
    }
}