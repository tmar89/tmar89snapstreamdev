using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;

using SnapStream.ViewScape.Services;
using SnapStream.ViewScape.Widgets;
using SnapStream.Commands;

namespace SnapStream.Plugins.iTunes
{
	public class iTunesSettingsScreen : ScreenBase
	{
		#region Private Members
		private TextList	_options;
		private TextWindow	_url;
		private TextEntry	_urlEntry;
		private TextWindow	_password;
		private TextEntry	_passwordEntry;				
		#endregion Private Members

		#region Properties
		public TextList OptionList 
		{
			get 
			{
				return _options;
			}
		}
		public TextWindow URL 
		{
			get 
			{
				return _url;
			}
		}
		public TextEntry URLEntry 
		{
			get 
			{
				return _urlEntry;
			}			
		}
		public TextWindow Password 
		{
			get 
			{
				return _password;
			}
		}
		public TextEntry PasswordEntry
		{
			get 
			{
				return _passwordEntry;
			}
		}		
		#endregion Properties

		public iTunesSettingsScreen()
		{
			_options = new TextList();
			Add( _options );

			_url = new TextWindow();			
			Add( _url );

			_urlEntry = new TextEntry();			
			Add( _urlEntry );

			_password = new TextWindow();			
			Add( _password );

			_passwordEntry = new TextEntry();			
			Add( _passwordEntry );			

			_urlEntry.Accept += new EventHandler(URLEntry_Accept);
			_urlEntry.Cancel += new EventHandler(URLEntry_Cancel);

			_passwordEntry.Accept += new EventHandler(PasswordEntry_Accept);
			_passwordEntry.Cancel += new EventHandler(PasswordEntry_Cancel);			

			_options.Focus();
			_options.ItemActivated += new ItemActivatedEventHandler( OnItemActivated );

			return;
		}

		private void URLEntry_Accept(object sender, EventArgs e) 
		{
			SingletonConfig.Instance.SetProperty("iTunesURL",_urlEntry.Text);		
			FillOptions();
			ShowBaseScreen();
			return;
		}

		private void URLEntry_Cancel(object sender, EventArgs e) 
		{
			ShowBaseScreen();
			return;
		}

		private void PasswordEntry_Accept(object sender, EventArgs e) 
		{
			SingletonConfig.Instance.SetProperty("iTunesPassword",_passwordEntry.Text);		
			FillOptions();
			ShowBaseScreen();
			return;
		}

		private void PasswordEntry_Cancel(object sender, EventArgs e) 
		{
			ShowBaseScreen();
			return;
		}		

		public override void Activate() 
		{
			base.Activate();
			
			ShowBaseScreen();
			if( NavigatingForward == false ) 
			{
				return;
			}
			FillOptions();

			return;
		}

		private void ShowBaseScreen() 
		{			
			HideAll();
			_options.Visible = true;
			_options.Focus();				
			return;
		}

		private void HideAll() 
		{
			_url.Visible = false;
			_urlEntry.Visible = false;
			_password.Visible = false;
			_passwordEntry.Visible = false;			
			_options.Visible = false;				
			return;
		}

		private void FillOptions() 
		{
			_options.Clear();
			string s;
			SingletonConfig.Instance.GetPropertyAsString("iTunesURL",out s);
			_options.AddTextItem( "URL: " + s);
			SingletonConfig.Instance.GetPropertyAsString("iTunesPassword",out s);
			_options.AddTextItem( "Password: " + s);							
			return;
		}

		#region Private Methods
		private void OnItemActivated( object sender, ItemActivatedArgs args ) 
		{
			if( _options.SelectedItem.Text.StartsWith("URL") ) 
			{
				HideAll();
				_url.Visible = true;
				_urlEntry.Visible = true;
				_urlEntry.Focus();
			}		
			else if( _options.SelectedItem.Text.StartsWith("Password") ) 
			{
				HideAll();
				_password.Visible = true;
				_passwordEntry.Visible = true;
				_passwordEntry.Focus();
			}				
			return;
		}				
		#region Window Overrides
		public override void OnKeyDown( object sender, System.Windows.Forms.KeyEventArgs e ) 
		{
			base.OnKeyDown( sender, e );			

			if( e.Handled ) 
			{
				return;
			}					

			if( e.KeyCode == System.Windows.Forms.Keys.Escape ) 
			{
				RaiseExitEvent();
				SingletonSoundCache.Instance.PlaySound( DefaultSoundList.Cancel );
				e.Handled = true;
				return;
			}

			return;
		}
		#endregion Window Overrides

		protected override void DisposeCore() 
		{
			base.DisposeCore();
			return;
		}
		#endregion Window Overrides
	}
}
