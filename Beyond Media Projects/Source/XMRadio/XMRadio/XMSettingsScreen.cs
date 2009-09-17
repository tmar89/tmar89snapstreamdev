using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;

using SnapStream.ViewScape.Services;
using SnapStream.ViewScape.Widgets;
using SnapStream.Commands;

namespace SnapStream.Plugins.XMRadio
{
	public class XMSettingsScreen : ScreenBase
	{
		#region Private Members
		private TextList	_options;
		private TextWindow	_email;
		private TextEntry	_emailEntry;
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
		public TextWindow Email 
		{
			get 
			{
				return _email;
			}
		}
		public TextEntry EmailEntry 
		{
			get 
			{
				return _emailEntry;
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

		public XMSettingsScreen()
		{
			_options = new TextList();
			Add( _options );

			_email = new TextWindow();			
			Add( _email );

			_emailEntry = new TextEntry();			
			Add( _emailEntry );

			_password = new TextWindow();			
			Add( _password );

			_passwordEntry = new TextEntry();			
			Add( _passwordEntry );			

			_emailEntry.Accept += new EventHandler(EmailEntry_Accept);
			_emailEntry.Cancel += new EventHandler(EmailEntry_Cancel);

			_passwordEntry.Accept += new EventHandler(PasswordEntry_Accept);
			_passwordEntry.Cancel += new EventHandler(PasswordEntry_Cancel);			

			_options.Focus();
			_options.ItemActivated += new ItemActivatedEventHandler( OnItemActivated );

			return;
		}

		private void EmailEntry_Accept(object sender, EventArgs e) 
		{
			SingletonConfig.Instance.SetProperty("XMEmail",_emailEntry.Text);		
			FillOptions();
			ShowBaseScreen();
			return;
		}

		private void EmailEntry_Cancel(object sender, EventArgs e) 
		{
			ShowBaseScreen();
			return;
		}

		private void PasswordEntry_Accept(object sender, EventArgs e) 
		{
			SingletonConfig.Instance.SetProperty("XMPassword",_passwordEntry.Text);		
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
			_email.Visible = false;
			_emailEntry.Visible = false;
			_password.Visible = false;
			_passwordEntry.Visible = false;			
			_options.Visible = false;				
			return;
		}

		private void FillOptions() 
		{
			_options.Clear();
			string s;
			SingletonConfig.Instance.GetPropertyAsString("XMEmail",out s);
			_options.AddTextItem( "Email: " + s);
			SingletonConfig.Instance.GetPropertyAsString("XMPassword",out s);
			_options.AddTextItem( "Password: " + s);							
			return;
		}

		#region Private Methods
		private void OnItemActivated( object sender, ItemActivatedArgs args ) 
		{
			if( _options.SelectedItem.Text.StartsWith("Email") ) 
			{
				HideAll();
				_email.Visible = true;
				_emailEntry.Visible = true;
				_emailEntry.Focus();
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
