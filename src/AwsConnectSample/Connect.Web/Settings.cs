using System;
using System.Collections.Generic;
using System.Web.Configuration;
using Connect.Web.Enums;
using Newtonsoft.Json;

namespace Connect.Web
{
    /// <summary>
    /// Summary description for Settings
    /// </summary>
    public class Settings
    {
        public string this[string key]
        {
            get { return Get(key); }
            set { Set(key, value); }
        }

        public static string Get(string key)
        {
            return WebConfigurationManager.AppSettings[key];
        }

        public static void Set(string key, string value)
        {
            var config = WebConfigurationManager.OpenWebConfiguration("~");
            config.AppSettings.Settings[key].Value = value;
            config.Save();
        }

        public static Enums.LogLevel MinLogLevel
        {
            get
            {
                LogLevel minLogLevel;
                if (Enum.TryParse(Get("MinLogLevel"), true, out minLogLevel))
                    return minLogLevel;
                return Enums.LogLevel.Trace;
            }
            set
            {
                Set("MinLogLevel", value.ToString());
                Logger.MinLevel = value;
            }
        }

        public static string SiteDomain
        {
            get
            {
                return Get("SiteDomain");
            }
            set
            {
                Set("SiteDomain", value);
            }
        }


        public static int SystemUserId
        {
            get
            {
                return int.Parse(Get("SystemUserId"));
            }
            set
            {
                Set("SystemUserId", value.ToString());
            }
        }

        public static int SiteId
        {
            get
            {
                return int.Parse(Get("SiteId"));
            }
            set
            {
                Set("SiteId", value.ToString());
            }
        }

        public static int ProjectId
        {
            get
            {
                return int.Parse(Get("ProjectId"));
            }
            set
            {
                Set("ProjectId", value.ToString());
            }
        }


        public static bool IsAdminSite
        {
            get
            {
                var result = false;
                bool.TryParse(Get("IsAdminSite"), out result);
                return result;
            }
            set
            {
                Set("IsAdminSite", value.ToString());
            }
        }

        public static string AdminApiUrl
        {
            get
            {
                return Get("AdminApiUrl");
            }
            set
            {
                Set("AdminApiUrl", value.ToString());
            }
        }

        public static string SiteApiUrl
        {
            get
            {
                return Get("SiteApiUrl");
            }
            set
            {
                Set("SiteApiUrl", value.ToString());
            }
        }


        public static string AWSUserName
        {
            get
            {
                return Get("AWSUserName");
            }
            set
            {
                Set("AWSUserName", value);
            }
        }

        public static string AWSAccessKey
        {
            get
            {
                return Get("AWSAccessKey");
            }
            set
            {
                Set("AWSAccessKey", value);
            }
        }

        public static string AWSSecretKey
        {
            get
            {
                return Get("AWSSecretKey");
            }
            set
            {
                Set("AWSSecretKey", value);
            }
        }

        public static string AWSRegion
        {
            get
            {
                return Get("AWSRegion");
            }
            set
            {
                Set("AWSRegion", value);
            }
        }

        public static bool IsDebug
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }
    }
}