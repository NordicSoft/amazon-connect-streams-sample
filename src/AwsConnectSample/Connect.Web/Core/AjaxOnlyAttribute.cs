using System.Web.Mvc;

namespace Connect.Web.Core
{
	public class AjaxOnlyAttribute : ActionFilterAttribute
	{
		public AjaxOnlyAttribute()
		{
		}


		public override void OnActionExecuting(ActionExecutingContext filterContext)
		{
            var controller = (ControllerBase)filterContext.Controller;
			
			if (controller.Request.IsAjaxRequest())
			{
				base.OnActionExecuting(filterContext);
			}
			else
			{
                filterContext.Result = controller.Error404();
			}
		}
	}
}
