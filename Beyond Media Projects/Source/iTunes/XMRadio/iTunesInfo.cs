using System;
using System.Collections;
using System.Text;
using System.Xml;
using System.Threading;
using System.Globalization;
using System.IO;

using SnapStream;
using SnapStream.Util;
using SnapStream.ViewScape.Services;
using SnapStream.Configuration;

namespace SnapStream.Plugins.iTunes
{
	// iTunesInfo Class
	public class iTunesInfo 
	{			
		private string		_url;
		private string		_password;		

		public string URL 
		{
			get 
			{
				return _url;
			}
			set 
			{
				_url = value;
			}
		}
		public string Password 
		{
			get 
			{
				return _password;
			}
			set 
			{
				_password = value;
			}
		}		

		public iTunesInfo() 
		{			
			_url = "nourl";
			_password = "nopassword";			

			return;
		}		
	}	
	

	// Singleton Class
	public sealed class SingletoniTunesInfo : iTunesInfo  
	{
		public static readonly iTunesInfo Instance = new iTunesInfo();

		public SingletoniTunesInfo() 
		{
			return;
		}
	}
}
