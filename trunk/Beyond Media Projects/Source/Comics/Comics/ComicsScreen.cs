using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;

using SnapStream.ViewScape.Services;
using SnapStream.ViewScape.Widgets;
using SnapStream.ViewScape.Input;
using SnapStream.Commands;

namespace SnapStream.Plugins.Comics
{
	/// <summary>
	/// Screen that displays the list of comics that have been downloaded.
	/// </summary>
	public class ComicsScreen : ScreenBase
	{
		#region Public Members
		// Register the ExecuteCommand action
		public static readonly Command daysChanged = SingletonCommandManager.Instance.Register( "Comics.ChangeDaysToKeep" );
		#endregion Public Members

		#region Private Members		
		private string						_homeDirectory;								
		private TextWindow					_instructions;		
		private VariableItemList			_comicsViewer;
		private OptionList					_sortBy;
		int previousItem = 1;
				
		#endregion Private Members

		#region Properties		
		public TextWindow Instructions 
		{
			get 
			{
				return _instructions;
			}
		}

		public VariableItemList ComicsViewer 
		{
			get 
			{
				return _comicsViewer;
			}
		}
		public OptionList SortBy
		{
			get
			{
				return _sortBy;
			}
		}
		#endregion Properties

		#region Constructors
		/// <summary>
		/// Creates the ComicsScreen
		/// </summary>
		public ComicsScreen() 
		{
			SnapStream.Logging.WriteLog("Comics Plugin Started");

			System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
			System.IO.FileInfo fi = new System.IO.FileInfo( a.Location );			

			_homeDirectory = fi.DirectoryName;

			// Create a new updater
			//_updater = new ComicsUpdater();
			SingletonComicsUpdater.Instance.Initialize( fi.DirectoryName );

			// Register the handler for changing days
			daysChanged.Execute += new CommandExecuteHandler( daysChanged_Execute );

			// Text Objects
			// Show the instructions
			_instructions = new TextWindow();
			Add( _instructions );

			// Create the viewer window for the comics
			_comicsViewer = new VariableItemList();
			Add( _comicsViewer );

			// Sort by option
			_sortBy = new OptionList();
			_sortBy.DefaultItemTextHeightPercent = 0.5;
			OptionListItem byWhat = new OptionListItem("Sort By");			
			byWhat.AddSelectorItem("Date","Date");
			byWhat.AddSelectorItem("Comic","Comic");
			_sortBy.AddItem(byWhat);
			Add ( _sortBy );
			byWhat.SelectorValueChanged += new SelectorValueChangedEventHandler( sortBy_SelectorValueChanged );
			_sortBy.Visible = false;

			_comicsViewer.ItemActivated += new ItemActivatedEventHandler(ComicsViewer_ItemActivated);			
			_comicsViewer.Visible = true;
			_comicsViewer.Focus();
			_comicsViewer.Height = 480;
			this.Render();

			_comicsViewer.HighlightItemImage = String.Empty;
			_comicsViewer.DefaultItemImage = String.Empty;

			return;
		}
		#endregion Constructors

		#region Window Overrides		
		public override void OnKeyDown( object sender, System.Windows.Forms.KeyEventArgs e ) 
		{
			base.OnKeyDown( sender, e );							

			if (e.KeyCode == System.Windows.Forms.Keys.Up && _comicsViewer.SelectedIndex == 0) 
			{								
				if (previousItem == 1) 
				{										
					previousItem = 0;
					return;
				}
				if (previousItem == 0) {
					_sortBy.Visible = true;
					_sortBy.FadeIn();
					_sortBy.Focus();
					previousItem = 1;
					return;
				}
			}		
			
			if (e.KeyCode == System.Windows.Forms.Keys.Down && _sortBy.IsFocused()) 
			{								
				_comicsViewer.Focus();				
				_sortBy.FadeOut();				
			}

			if( e.Handled ) 
			{
				return;
			}			

			if( e.KeyCode == System.Windows.Forms.Keys.Escape ) 
			{
				SingletonSoundCache.Instance.PlaySound( DefaultSoundList.Cancel );
				RaiseExitEvent();
				e.Handled = true;
				return;
			}

			return;
		}

		// On entering the screen
		public override void Activate() 
		{				
			Fillcomics();
		}

		protected override void DisposeCore() 
		{
			base.DisposeCore();	
			SingletonComicsUpdater.Instance.Dispose();		
			return;
		}
		#endregion Window Overrides

		#region Private Methods
		// Function called when the DaysToKeep setting is changed
		private void daysChanged_Execute(object sender, CommandExecuteArgs args) 
		{
			SingletonComicsUpdater.Instance.UpdateNow();
		}	
		private void sortBy_SelectorValueChanged( object sender, SelectorValueChangedEventArgs args ) 
		{
			SingletonConfig.Instance.SetProperty( "Comics.SortBy", args.SelectedValue );			
			Fillcomics();
			return;
		}

		// Show the comics in the window
		private void Fillcomics() 
		{
			System.IO.FileInfo	fi;
			ArrayList			comics;

			fi = new System.IO.FileInfo( _homeDirectory + "\\AvailableComics.xml" );							

			_comicsViewer.Clear();
			// Show the "Downloading Comics..." item when the updater is running and there are comics to download
			if (SingletonComicsUpdater.Instance.Updating == true && SingletonComicsUpdater.Instance.SubscribedComics.Length != 0 ) 			
			{
				ComicListItem	item;
				item = new ComicListItem( "Downloading Comics... Click to refresh", "");
				item.RelativeBounds = new Rectangle( 0, 0, 200, 40 );
				_comicsViewer.AddItem( item );
			}
			
			// Load the comics or show the instructions screen
			try 
			{
				comics = ComicsUpdater.LoadComics( fi.FullName );					
			}
			catch 
			{
				_instructions.Text = "Please subscribe to comics";
				_instructions.Visible = true;
				return;
			}

			// If there are no subscribed comics, show instructions			
			foreach( ComicInfo ci in comics ) 
			{
				if( ci.Subscribed == true ) 
				{			
					_instructions.Visible = false;
					break;
				}	
				else 		
					_instructions.Visible = true;
			}		
						
			// Get the number of days to keep
			int daysToKeep;
			try 
			{
				string	sDaysToKeep;

				SingletonConfig.Instance.GetPropertyAsString( "Comics.DaysToKeep", out sDaysToKeep );
				daysToKeep = int.Parse( sDaysToKeep );								
			}
			catch { daysToKeep = 7; }

			string sSortBy;
			SingletonConfig.Instance.GetPropertyAsString( "Comics.SortBy", out sSortBy);
			if (sSortBy == "Date") 
			{
				// Retrieve the local image for each comic
				for (int i = 0; i < daysToKeep; i++) 
				{								
					foreach( ComicInfo ci in comics ) 
					{				
						if (ci.Subscribed == true) 
						{
							string	title, fullName;

							DateTime dt = DateTime.Now.AddDays(-i);
							title = ci.DisplayName + " - " + dt.ToString("D",DateTimeFormatInfo.InvariantInfo);
							// Create the image filename from the format in the XML file
							char[] delims = {'$'};
							String[] FilenameTokens = ci.ImageFilename.Split(delims,100);
							String ImageFilename = "";
							// Parse the String format and form the filename
							foreach( String s in FilenameTokens ) 
							{
								// Year
								if ( s.Equals("YY") )					
									ImageFilename += dt.ToString("yy",DateTimeFormatInfo.InvariantInfo);
									// Month
								else if ( s.Equals("MM") )
									ImageFilename += dt.ToString("MM",DateTimeFormatInfo.InvariantInfo);
									// Day
								else if ( s.Equals("DD") )
									ImageFilename += dt.ToString("dd",DateTimeFormatInfo.InvariantInfo);								
								else
									ImageFilename += s;
							}
							fullName = _homeDirectory + "\\" + ci.FolderName + "\\" + ImageFilename + "." + ci.ImageSuffix;						
							bool noComic = true;
							if( !System.IO.File.Exists(fullName) ) 
							{						
								noComic = true;
								// try the Dilbert hack
								fullName = _homeDirectory + "\\" + ci.FolderName + "\\" + ImageFilename + ".jpg";							
								if( System.IO.File.Exists(fullName) )
								{
									noComic = false;
								}
							}
							else 
								noComic = false;

							if (noComic) continue;

							// width is always 600
							// height is a minimum of 200, maximum of 600
							Bitmap	bmp;
							float	aspectRatio;
							int		calculatedHeight, calculatedWidth;

							bmp = null;
							try 
							{
								bmp = new Bitmap( fullName );
							}
							catch( Exception e ) 
							{
								SnapStream.Logging.WriteLog( "Fillcomics - Could not load the comic: " + ci.DisplayName);
								SnapStream.Logging.WriteLog( e.ToString() );
								continue;
							}

							if( bmp.Size.Height == 0 ) 
							{
								SnapStream.Logging.WriteLog( "Fillcomics - bitmap had no height: " + ci.DisplayName);
								continue;
							}

							aspectRatio = (float)bmp.Size.Width / (float)bmp.Size.Height;
							if( aspectRatio > 600/200 ) 
							{
								calculatedWidth = 600;
								calculatedHeight = 200;
							}						
							else 
							{
								calculatedWidth = 600;
								calculatedHeight = (int)( (float)bmp.Size.Width / aspectRatio);
							}

							ComicListItem	item;
							item = new ComicListItem( title, fullName );
							item.RelativeBounds = new Rectangle( 0, 0, calculatedWidth, calculatedHeight );
							_comicsViewer.AddItem( item );
						}
					}
				}
			}
			if (sSortBy == "Comic") 
			{
				// Retrieve the local image for each comic
				foreach( ComicInfo ci in comics ) 
				{
					for (int i = 0; i < daysToKeep; i++) 
					{																	
						if (ci.Subscribed == true) 
						{
							string	title, fullName;

							DateTime dt = DateTime.Now.AddDays(-i);
							title = ci.DisplayName + " - " + dt.ToString("D",DateTimeFormatInfo.InvariantInfo);
							// Create the image filename from the format in the XML file
							char[] delims = {'$'};
							String[] FilenameTokens = ci.ImageFilename.Split(delims,100);
							String ImageFilename = "";
							// Parse the String format and form the filename
							foreach( String s in FilenameTokens ) 
							{
								// Year
								if ( s.Equals("YY") )					
									ImageFilename += dt.ToString("yy",DateTimeFormatInfo.InvariantInfo);
									// Month
								else if ( s.Equals("MM") )
									ImageFilename += dt.ToString("MM",DateTimeFormatInfo.InvariantInfo);
									// Day
								else if ( s.Equals("DD") )
									ImageFilename += dt.ToString("dd",DateTimeFormatInfo.InvariantInfo);
								else
									ImageFilename += s;
							}
							fullName = _homeDirectory + "\\" + ci.FolderName + "\\" + ImageFilename + "." + ci.ImageSuffix;																											
							bool noComic = true;
							if( !System.IO.File.Exists(fullName) ) 
							{						
								noComic = true;
								// try the Dilbert hack
								fullName = _homeDirectory + "\\" + ci.FolderName + "\\" + ImageFilename + ".jpg";							
								if( System.IO.File.Exists(fullName) )
								{
									noComic = false;
								}
							}
							else 
								noComic = false;

							if (noComic) continue;

							// width is always 600
							// height is a minimum of 200, maximum of 600
							Bitmap	bmp;
							float	aspectRatio;
							int		calculatedHeight, calculatedWidth;

							bmp = null;
							try 
							{
								bmp = new Bitmap( fullName );
							}
							catch( Exception e ) 
							{
								SnapStream.Logging.WriteLog( "Fillcomics - Could not load the comic: " + ci.DisplayName);
								SnapStream.Logging.WriteLog( e.ToString() );
								continue;
							}

							if( bmp.Size.Height == 0 ) 
							{
								SnapStream.Logging.WriteLog( "Fillcomics - bitmap had no height: " + ci.DisplayName);
								continue;
							}

							aspectRatio = (float)bmp.Size.Width / (float)bmp.Size.Height;
							if( aspectRatio > 600/200 ) 
							{
								calculatedWidth = 600;
								calculatedHeight = 200;
							}						
							else 
							{
								calculatedWidth = 600;
								calculatedHeight = (int)( (float)bmp.Size.Width / aspectRatio);
							}

							ComicListItem	item;
							item = new ComicListItem( title, fullName );
							item.RelativeBounds = new Rectangle( 0, 0, calculatedWidth, calculatedHeight );
							_comicsViewer.AddItem( item );
						}
					}
				}
			}
			
			// Start at the first
			_comicsViewer.SelectedIndex = 0;

			return;
		}

		// Show slideshow when item is entered
		private void ComicsViewer_ItemActivated( object sender, ItemActivatedArgs args ) 
		{
			if (((ComicListItem)(_comicsViewer.SelectedItem)).Caption.Text == "Downloading Comics... Click to refresh")
			{
				Fillcomics();
				return;
			}
			
			ArrayList	aComics;

			aComics = new ArrayList();
			foreach( ComicListItem item in _comicsViewer.Items ) 
			{
				aComics.Add( item.FullName );
			}

			string[]	comics = (string[])aComics.ToArray( typeof(string) );
			double		zoom;

			try 
			{
				string	sZoom;

				SingletonConfig.Instance.GetPropertyAsString( "Comics.DefaultZoom", out sZoom );
				zoom = double.Parse( sZoom );
				zoom /= 100;
			}
			catch 
			{
				zoom = 1.5;
			}

			
			ShowScreen	s = new ShowScreen( "ComicsSlideShowScreen", 
				new object[] { comics, _comicsViewer.SelectedIndex, false, zoom, true } );
			s.Execute();
			
			return;
		}
		#endregion Private Methods
	}
}