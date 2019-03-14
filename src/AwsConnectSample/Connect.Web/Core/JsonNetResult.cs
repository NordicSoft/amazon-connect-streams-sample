using System;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Connect.Web.Core
{
    public class JsonNetResult : ActionResult
    {
        public Encoding ContentEncoding { get; set; }
        public string ContentType { get; set; }
        public int StatusCode { get; set; }
        public object Data { get; private set; }
        public JsonSerializerSettings SerializerSettings { get; set; }
        public Formatting Formatting { get; set; }

        public JsonNetResult()
        {
            SerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                PreserveReferencesHandling = PreserveReferencesHandling.None,
                //CheckAdditionalContent = true,
                //ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                //DefaultValueHandling = DefaultValueHandling.Ignore,
                //ObjectCreationHandling = ObjectCreationHandling.Replace,
                //TypeNameHandling = TypeNameHandling.Auto 
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat
            };
            this.Formatting = Settings.IsDebug ? Formatting.Indented : Formatting.None;
            StatusCode = (int) HttpStatusCode.OK;
        }

        public JsonNetResult(object data, int statusCode = 200)
        {
            SerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                PreserveReferencesHandling = PreserveReferencesHandling.None,
                //CheckAdditionalContent = true,
                //ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                //DefaultValueHandling = DefaultValueHandling.Ignore,
                //ObjectCreationHandling = ObjectCreationHandling.Replace,
                //TypeNameHandling = TypeNameHandling.Auto ,
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat
            };
            Formatting = Settings.IsDebug ? Formatting.Indented : Formatting.None;
            Data = data;
            StatusCode = statusCode;
        }
        
        public override void ExecuteResult(ControllerContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            HttpResponseBase response = context.HttpContext.Response;
            
            response.ContentType = !string.IsNullOrEmpty(ContentType)
              ? ContentType
              : "application/json";

            response.StatusCode = StatusCode;
            if (StatusCode >= 400)
            {
                HttpContext.Current.Items["ErrorHandled"] = true;
            }

            if (ContentEncoding != null)
                response.ContentEncoding = ContentEncoding;

            if (Data != null)
            {
                var writer = new JsonTextWriter(response.Output) { Formatting = Formatting };
                
                JsonSerializer serializer = JsonSerializer.Create(SerializerSettings);
                serializer.Serialize(writer, Data);
                writer.Flush();
            }
        }
    }
}