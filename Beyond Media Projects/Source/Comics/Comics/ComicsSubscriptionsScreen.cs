using System;
using System.Drawing;
using System.Windows.Forms;

using SnapStream.ViewScape.Services;
using SnapStream.ViewScape.Widgets;

namespace SnapStream.Plugins.Comics
{
	/// <summary>
	/// Screen that displays the available comic subscriptions
	/// </summary>
	public class ComicsSubscriptionsScreen : ScreenBase
	{
		#region Private Members		
		private TextWindow			_instructions;
		private TextList			_availableComics;
		#endregion Private Members

		#region Properties
		public TextWindow Instructions 
		{
			get 
			{
				return _instructions;
			}
		}

		public TextList AvailableComics 
		{
			get 
			{
				return _availableComics;
			}
		}
		#endregion Properties

		#region Constructors
		public ComicsSubscriptionsScreen() 
		{
			System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
			System.IO.FileInfo fi = new System.IO.FileInfo( a.Location );						

			// Instructions if no comics are in the XML
			_instructions = new TextWindow();
			Add( _instructions );

			// Show comics from XML 
			_availableComics = new TextList();
			Add( _availableComics );

			_availableComics.Focus();
			_availableComics.ItemActivated += new ItemActivatedEventHandler( OnItemActivated );								

			// Initialize the sort by variable if it isn't set
			if (!SingletonConfig.Instance.IsSet( "Comics.SortBy" ))
				SingletonConfig.Instance.SetProperty( "Comics.SortBy", "Date" );

			return;
		}
		#endregion Constructors

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
				SingletonSoundCache.Instance.PlaySound( DefaultSoundList.Cancel );
				// Remove the popup just in case it is up
				SingletonComicsUpdater.Instance.ShowPreview = false;
				RaiseExitEvent();
				e.Handled = true;
				return;
			}

			if ( e.KeyCode == System.Windows.Forms.Keys.I ||
				 e.KeyCode == System.Windows.Forms.Keys.F7 ) 
			{				
				if (SingletonComicsUpdater.Instance.ShowPreview) 
				{					
					SingletonComicsUpdater.Instance.ShowPreview = false;					
					this.Focus();
				}
				else  
				{										
					SingletonComicsUpdater.Instance.ComicToPreview = _availableComics.SelectedItem.Text;
					SingletonComicsUpdater.Instance.ShowPreview = true;
				}
			}
			
			return;
		}
		

		// When exiting from the screen, update the subscriptions
		protected override void OnVisibleChanged( EventArgs e ) 
		{			
			FillAvailableComics();
			return;
		}

		protected override void DisposeCore() 
		{
			base.DisposeCore();	
			SingletonComicsUpdater.Instance.Dispose();
			return;
		}

		// When exiting the screen, save and update
		public override void Deactivate()
		{
			base.Deactivate ();
			SingletonComicsUpdater.Instance.Save();
			SingletonComicsUpdater.Instance.UpdateNow();
		}

		#endregion Window Overrides

		#region Private Methods
		// Click the checkbox to subscribe or unsubscribe
		private void OnItemActivated( object sender, ItemActivatedArgs args ) 
		{							
			CheckBoxListItem item = _availableComics.Items[ args.ActivatedIndex ] as CheckBoxListItem;
			if( item == null ) 
			{
				return;
			}

			item.CheckBox.Checked = !item.CheckBox.Checked;
			ComicInfo ci = (ComicInfo)item.Value;
			SingletonComicsUpdater.Instance.Subscribe( ci.DisplayName, item.CheckBox.Checked );	
			return;
		}

		// Handle mouse clicks
		private void OnCheckBoxClickEvent( object sender, EventArgs args ) 
		{				
			CheckBoxListItem item = _availableComics.Items[ _availableComics.SelectedIndex ] as CheckBoxListItem;
			if( item == null ) 
			{
				return;
			}
			
			ComicInfo ci = (ComicInfo)item.Value;
			SingletonComicsUpdater.Instance.Subscribe( ci.DisplayName, item.CheckBox.Checked );	
			return;
		}		

		// Show comics in the subscribed list
		private void FillAvailableComics() 
		{
			_availableComics.Clear();

			foreach( ComicInfo ci in SingletonComicsUpdater.Instance.AvailableComics ) 
			{				
				CheckBoxListItem item = new CheckBoxListItem( ci.DisplayName, ci );
				item.CheckBoxChanged += new EventHandler( OnCheckBoxClickEvent );
				item.CheckBox.Checked = ci.Subscribed;						
				_availableComics.AddItem( item );					
			}

			return;
		}
		#endregion Private Methods
	}
}
