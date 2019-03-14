using System.Collections.Generic;

namespace Connect.Web.Core
{
	public class JsonResponse
	{
		public JsonResponse()
		{
			Errors = new List<JsonError>();
		}

		public JsonResponse(bool success)
		{
			Success = success;
			Errors = new List<JsonError>();
		}

		public JsonResponse(bool success, object data)
		{
			Success = success;
			Data = data;
			Errors = new List<JsonError>();
		}

        public JsonResponse(bool success, object data, JsonError error)
		{
			Success = success;
			Data = data;
			Errors = new List<JsonError> { error };
		}

        public JsonResponse(bool success, object data, string error)
		{
			Success = success;
			Data = data;
			Errors = new List<JsonError> { new JsonError { Title = error } };
		}

        public JsonResponse(bool success, object data, int errorCode)
		{
			Success = success;
			Data = data;
            Errors = new List<JsonError> { new JsonError { Code = errorCode } };
		}

		public bool Success { get; set; }
		public List<JsonError> Errors { get; set; }
        public object Data { get; set; }
	}
}
