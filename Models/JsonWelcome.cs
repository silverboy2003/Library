﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LittleWeebLibrary.Models
{
    class JsonWelcome
    {
        public string type { get; set; } = "welcome";
        public string welcome { get; set; } = "Succesfully made connection with LittleWeeb Back-end!";
        public bool local { get; set; } = true;
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public override string ToString()
        {
            JObject jobject = ToJObject();
            string properties = string.Empty;
            foreach (var x in jobject)
            {
                properties += x.Key + ": " + x.Value.ToString();
            }
            return properties;
        }

        public JObject ToJObject()
        {

            JObject jobject = JObject.FromObject(this);
            return jobject;
        }
    }
}
