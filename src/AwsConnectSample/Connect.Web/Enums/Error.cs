namespace Connect.Web.Enums
{
	public enum Error
	{
		DatabaseConnectionTimeout,
        PaymentError,
		Http401 = 401,
		Http403 = 403,
		Http404 = 404,
		Http423 = 423 // banned
	}
}
