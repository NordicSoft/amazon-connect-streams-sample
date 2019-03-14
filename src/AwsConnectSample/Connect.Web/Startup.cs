using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Connect.Web.Startup))]
namespace Connect.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
        }
    }
}
