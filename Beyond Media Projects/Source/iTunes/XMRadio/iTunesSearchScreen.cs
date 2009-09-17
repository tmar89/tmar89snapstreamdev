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
	public class iTunesSearchScreen : ScreenBase
	{
		#region Private Members
		private TextList	_options;
		private TextWindow	_search;
		private TextEntry	_searchEntry;
		#endregion Private Members

		#region Properties
		public TextList OptionList 
		{
			get 
			{
				return _options;
			}
		}
		public TextWindow Search 
		{
			get 
			{
				return _search;
			}
		}
		public TextEntry SearchEntry 
		{
			get 
			{
				return _searchEntry;
			}			
		}
		#endregion Properties

		public iTunesSearchScreen()
		{
			_options = new TextList();
			Add( _options );

			_search = new TextWindow();			
			Add( _search );

			_searchEntry = new TextEntry();			
			Add( _searchEntry );					

			_searchEntry.Accept += new EventHandler(SearchEntry_Accept);
			_searchEntry.Cancel += new EventHandler(SearchEntry_Cancel);
			
			_options.Focus();
			_options.ItemActivated += new ItemActivatedEventHandler( OnItemActivated );

			return;
		}

		private void SearchEntry_Accept(object sender, EventArgs e) 
		{
			SingletonConfig.Instance.SetProperty("iTunesSearch",_searchEntry.Text);		
			FillOptions();
			ShowBaseScreen();
			return;
		}

		private void SearchEntry_Cancel(object sender, EventArgs e) 
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
			_search.Visible = false;
			_searchEntry.Visible = false;			
			_options.Visible = false;				
			return;
		}

		private void FillOptions() 
		{
			_options.Clear();
			string s;
			SingletonConfig.Instance.GetPropertyAsString("iTunesSearch",out s);
			_options.AddTextItem( "Search: " + s);			
			return;
		}

		#region Private Methods
		private void OnItemActivated( object sender, ItemActivatedArgs args ) 
		{
			if( _options.SelectedItem.Text.StartsWith("Search") ) 
			{
				HideAll();
				_search.Visible = true;
				_searchEntry.Visible = true;
				_searchEntry.Focus();
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
