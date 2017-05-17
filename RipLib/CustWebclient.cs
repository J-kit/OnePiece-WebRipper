using System.Net;

namespace RipLib
{
	public class CustWebclient : WebClient
	{
		public object StateObject { get; set; }
	}
}