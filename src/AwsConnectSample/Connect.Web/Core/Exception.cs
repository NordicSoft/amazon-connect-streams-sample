using System.Text;

namespace Connect.Web.Core
{
	public class Exception : System.Exception
	{
		public Enums.Error Code { get; set; }
		public string Title { get; set; }
		public string Description { get; set; }

		public Exception(Enums.Error code)
		{
			Code = code;
		}
		
		public Exception(int code)
		{
			Code = (Enums.Error)code;
		}
		
		public Exception(string title, string description = "")
		{
		    Title = title;
		    Description = description;
		}

		public Exception(Enums.Error code, string title, string description = "")
        {
            Code = (Enums.Error)code;
		    Title = title;
		    Description = description;
		}

		public Exception(int code, string title, string description = "")
        {
            Code = (Enums.Error)code;
		    Title = title;
		    Description = description;
		}
		
		public JsonError ToJson()
		{
			return new JsonError { Code = (int)Code, Title = Title, Description = Description };
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
            sb.AppendFormat("App.Exception: Exception of type 'App.Exception' (ErrorCode: {0}, {1}) was thrown.", (int)Code, Code);
			if (!string.IsNullOrWhiteSpace(Description))
				sb.AppendFormat("\n\tDescription: {0}", Description);
			if (!string.IsNullOrWhiteSpace(StackTrace))
				sb.AppendFormat("\n\tStack Trace: {0}", StackTrace);
			return sb.ToString();
		}
	}
}
