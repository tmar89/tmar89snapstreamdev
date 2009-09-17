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

namespace SnapStream.Plugins.XMRadio
{
	// XMInfo Class
	public class XMInfo 
	{			
		private string		_email;
		private string		_password;
		private string		_speed;

		public string Email 
		{
			get 
			{
				return _email;
			}
			set 
			{
				_email = value;
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
		public string Speed 
		{
			get 
			{
				return _speed;
			}
			set 
			{
				_speed = value;
			}
		}


		public XMInfo() 
		{			
			_email = "noemail";
			_password = "nopassword";
			_speed = "high";

			return;
		}		
	}	
	

	// Singleton Class
	public sealed class SingletonXMInfo : XMInfo  
	{
		public static readonly XMInfo Instance = new XMInfo();

		public SingletonXMInfo() 
		{
			return;
		}
	}
}
