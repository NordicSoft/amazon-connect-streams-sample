using NLog;

namespace Connect.Web
{
	public static class Logger
	{
		private static readonly object lockObject = new object();

		static Logger()
		{
			MinLevel = Settings.MinLogLevel;
		}

		private static NLog.Logger _logger;
		public static NLog.Logger Instance
		{
			get
			{
				lock (lockObject)
				{
					if (_logger == null)
						_logger = LogManager.GetCurrentClassLogger();
				}
				return _logger;
			}
		}

        private static Enums.LogLevel _minLevel;
        public static Enums.LogLevel MinLevel
		{
			get
			{
				if (IsEnabled)
					return _minLevel;
				return Enums.LogLevel.Off;
			}
			set
			{
				_minLevel = value;
				if (IsEnabled)
					LogManager.GlobalThreshold = LogLevel.FromString(value.ToString());
			}
		}

		public static bool IsEnabled
		{
			get { return LogManager.GlobalThreshold != LogLevel.Off; }
			set { LogManager.GlobalThreshold = !value ? LogLevel.Off : LogLevel.FromString(MinLevel.ToString()); }
		}

		public static void Disable()
		{
			IsEnabled = false;
		}

		public static void Enable()
		{
			IsEnabled = true;
		}
	}
}
