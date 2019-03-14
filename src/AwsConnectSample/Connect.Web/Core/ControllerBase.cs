using System.IO;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Connect.Web.Core
{
    public class ControllerBase : System.Web.Mvc.Controller
    {
        protected readonly NLog.Logger Logger = Web.Logger.Instance;

        public ControllerBase()
        {

        }

        

        protected ActionResult ErrorAjax(System.Exception ex)
        {

            if (ex is Exception)
            {
                var eaException = ex as Exception;
                switch (eaException.Code)
                {
                    case Enums.Error.Http404:
                        return new JsonNetResult(new JsonResponse(false), 404);
                    case Enums.Error.Http403:
                        return new JsonNetResult(new JsonResponse(false), 403);
                    default:
                        return new JsonNetResult(new JsonResponse(false), 500);
                }
            }

            return new JsonNetResult(new JsonResponse(false), 500);
        }

        public ActionResult Error(System.Exception ex)
        {
            Logger.Error(ex);
            HttpContext.Items["ErrorHandled"] = true;

            Response.SuppressFormsAuthenticationRedirect = true;

            if (Request.IsAjaxRequest())
            {
                return ErrorAjax(ex);
            }

            if (ex is Exception)
            {
                var eaException = ex as Exception;
                switch (eaException.Code)
                {
                    case Enums.Error.Http404:
                        Response.StatusCode = (int)Enums.Error.Http404;
                        return View("Error");
                    case Enums.Error.Http403:
                        Response.StatusCode = (int)Enums.Error.Http403;
                        return View("Error");
                    default:
                        return View("Error");
                }
            }
            /*
            if (ex is DbEntityValidationException)
            {
                foreach (var validationErrors in (ex as DbEntityValidationException).EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        Trace.TraceInformation("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage);
                    }
                }
            }*/
            return View("Error");
        }

        public ActionResult Error403()
        {
            var ex = new Exception(Enums.Error.Http403);
            var requestedUrl = Request.Url.OriginalString;
            var referrerUrl = Request.UrlReferrer != null &&
                Request.UrlReferrer.OriginalString != requestedUrl ?
                Request.UrlReferrer.OriginalString : "null";
            ex.Description = string.Format("403 Forbidden.\n\t\tRequested URL: {0}\n\t\tReferrer URL: {1}", requestedUrl, referrerUrl);
            return Error(ex);
        }

        public ActionResult Error404()
        {
            var ex = new Exception(Enums.Error.Http404);
            var requestedUrl = Request.Url.OriginalString;
            var referrerUrl = Request.UrlReferrer != null &&
                Request.UrlReferrer.OriginalString != requestedUrl ?
                Request.UrlReferrer.OriginalString : "null";
            ex.Description = string.Format("404 Not Found.\n\t\tRequested URL: {0}\n\t\tReferrer URL: {1}", requestedUrl, referrerUrl);
            return Error(ex);
        }

        protected override void HandleUnknownAction(string actionName)
        {
            try
            {
                View(actionName).ExecuteResult(this.ControllerContext);
            }
            catch
            {
                Error(new Exception(Enums.Error.Http404)).ExecuteResult(this.ControllerContext);
            }
        }
        /*
        protected override void OnException(ExceptionContext filterContext)
        {
            Error(filterContext.Exception).ExecuteResult(this.ControllerContext);
        }
        */

        protected bool ViewExists(string name)
        {
            var result = ViewEngines.Engines.FindView(ControllerContext, name, null);
            return (result.View != null);
        }

        protected string RenderPartial(string viewName, object model = null)
        {
            if (string.IsNullOrEmpty(viewName))
                viewName = ControllerContext.RouteData.GetRequiredString("action");

            ViewData.Model = model;

            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(ControllerContext, viewName);
                var viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);

                return sw.GetStringBuilder().ToString();
            }
        }
        

    }
}
