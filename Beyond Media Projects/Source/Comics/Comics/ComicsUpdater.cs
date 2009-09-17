using System;
using System.Collections;
using System.Text;
using System.Xml;
using System.Threading;
using System.Globalization;
using System.IO;
using System.Net;

using SnapStream;
using SnapStream.Util;
using SnapStream.ViewScape.Services;
using SnapStream.Configuration;

namespace SnapStream.Plugins.Comics
{
	public class ComicInfo 
	{
		private string		_website;
		private string		_folderName;
		private string		_displayName;		
		private string		_imageSuffix;
		private string		_imageFilename;
		private string		_imagePath;
		private bool		_subscribed;	
	

		public ComicInfo() 
		{
			_website = "";
			_folderName = "";
			_displayName = "";
			_imageSuffix = "";
			_imageFilename = "";
			_imagePath = "";
			_subscribed = false;			
			return;
		}

		public string Website 
		{
			get 
			{
				return _website;
			}
			set 
			{
				_website = value;
			}
		}

		public string FolderName 
		{
			get 
			{
				return _folderName;
			}
			set 
			{
				_folderName = value;
			}
		}

		public string DisplayName 
		{
			get 
			{
				return _displayName;
			}
			set 
			{
				_displayName = value;
			}
		}
	
		public string ImageSuffix 
		{
			get 
			{
				return _imageSuffix;
			}
			set 
			{
				_imageSuffix = value;
			}
		}

		public string ImageFilename 
		{
			get 
			{
				return _imageFilename;
			}
			set 
			{
				_imageFilename = value;
			}
		}

		public string ImagePath 
		{
			get 
			{
				return _imagePath;
			}
			set 
			{
				_imagePath = value;
			}
		}
	
		public bool Subscribed 
		{
			get 
			{
				return _subscribed;
			}
			set 
			{
				_subscribed = value;
			}
		}		
	}

	/// <summary>
	/// Sorts from A-Z
	/// </summary>
	public class ComicInfoNameComparer : IComparer 
	{

		public ComicInfoNameComparer() 
		{
			return;
		}

		int IComparer.Compare( Object x, Object y ) 
		{
			ComicInfo	xTemp, yTemp;

			xTemp = (ComicInfo)x;
			yTemp = (ComicInfo)y;

			return String.Compare( xTemp.DisplayName, yTemp.DisplayName );
		}
	}	

	/// <summary>
	/// Summary description for Class.
	/// </summary>
	public class ComicsUpdater : IDisposable 
	{

		private ArrayList	_availableComics;
		private string		_homeDirectory;
		private DateTime	_lastUpdate;
		private Thread		WorkerThread;
		private int			daysToKeep;
		private bool		_updating;		
		private bool		_showPreview;
		private string		_comicToPreview;

		public string HomeDirectory 
		{
			get 
			{
				return _homeDirectory;
			}
		}

		public DateTime LastUpdate 
		{
			get 
			{
				return _lastUpdate;
			}
		}
		public bool Updating 
		{
			get 
			{
				return _updating;
			}			
		}
		public bool ShowPreview 
		{
			get
			{	
				return _showPreview;
			}
			set 
			{
				_showPreview = value;
			}
		}
		public string ComicToPreview 
		{
			get
			{	
				return _comicToPreview;
			}
			set 
			{
				_comicToPreview = value;
			}
		}

		/// <summary>
		/// The list of comics available for subscription
		/// </summary>
		public ComicInfo[] AvailableComics 
		{
			get 
			{
				return (ComicInfo[])_availableComics.ToArray( typeof(ComicInfo) );
			}
		}

		/// <summary>
		/// The list of comics to which the user is subscribed
		/// </summary>
		public ComicInfo[] SubscribedComics 
		{
			get 
			{
				ArrayList subscribedComics = new ArrayList();
				foreach( ComicInfo ci in _availableComics ) 
				{
					if( ci.Subscribed == true ) 
					{
						subscribedComics.Add( ci );
					}
				}

				return (ComicInfo[])subscribedComics.ToArray( typeof(ComicInfo) );
			}
		}

		public void Initialize( string directoryName ) 
		{
			_homeDirectory = directoryName;

			_availableComics = LoadComics( _homeDirectory + "\\AvailableComics.xml" );
			_availableComics.Sort( new ComicInfoNameComparer() );						

			WorkerThread = new Thread( new ThreadStart(WorkerProc) );
			WorkerThread.Start();
			
			return;
		}

		// Constructor
		public ComicsUpdater() 
		{			
			daysToKeep = 3;
			_updating = false;	
			_showPreview = false;
		}

		private void WorkerProc() 
		{
			try 
			{				
				while( true ) 
				{

					// Hardcoded call interval to 8 hours
					TimeSpan callInterval = new TimeSpan( 8, 0, 0 );
					if( DateTime.UtcNow - _lastUpdate < callInterval ) 
					{
						System.Threading.Thread.Sleep( 1000 );						
						continue;
					}

					SnapStream.Logging.WriteLog( "ComicsUpdater starting." );					
					
					// Set the updating flag
					_updating = true;

					// Get the number of days to keep			
					try 
					{
						string	sDaysToKeep;
						SingletonConfig.Instance.GetPropertyAsString( "Comics.DaysToKeep", out sDaysToKeep );
						daysToKeep = int.Parse( sDaysToKeep );										
					}
					catch { daysToKeep = 3; }

					// Add subscribed comics to the list
					ArrayList subscribedComics = new ArrayList();
					lock( this ) 
					{
						foreach( ComicInfo ci in _availableComics ) 
						{							
							if( ci.Subscribed == true ) 
							{
								subscribedComics.Add( ci );
							}
						}
					}
					
					// Update the comics and expire the old
					UpdateSubscribedComics( subscribedComics );
					lock( this ) 
					{						
						ExpireComics( _availableComics );
					}					
					
					// Set last update time to now
					_lastUpdate = DateTime.UtcNow;

					SnapStream.Logging.WriteLog( "ComicsUpdater finished." );
					
					// Done updating
					_updating = false;
				}
			} 
			catch( ThreadAbortException ) 
			{
				Thread.ResetAbort();
			}

			return;
		}

		// Force Update by setting the last update time to the minumum
		public void UpdateNow() 
		{			
			_lastUpdate = DateTime.MinValue;
		}

		// Subscribe to or unsubscribe from a comic
		public void Subscribe( string name, bool subscribed ) 
		{
			bool	newSubscription;
			newSubscription = false;
			foreach( ComicInfo ci in _availableComics ) 
			{
				if( name == ci.DisplayName ) 
				{
					// Add a new subscription and flag that there is a new one
					if( ci.Subscribed == false && subscribed == true ) 
					{
						newSubscription = true;						
					}

					// Remove the folder for the removed subscription
					if( ci.Subscribed == true && subscribed == false ) 
					{						
						string alphaNumericName = ToAlphaNumericString( ci.FolderName );
						string comicDirectory = _homeDirectory + "\\" + alphaNumericName;
						if( System.IO.Directory.Exists(comicDirectory) == true ) 
						{
							foreach (string file in Directory.GetFiles(comicDirectory, "*.*"))
								File.Delete(file);
							Directory.Delete( comicDirectory );
						}	
					}


					ci.Subscribed = subscribed;
				}
			}

			// if a new subscription, update now
			if( newSubscription == true ) 
			{
				_availableComics.Sort( new ComicInfoNameComparer() );								
			}

			//SaveComics( _availableComics, _homeDirectory + "\\AvailableComics.xml" );			
			//UpdateNow();
			return;
		}

		// Save the XML when subscribed
		public void Save() 
		{
			SaveComics( _availableComics, _homeDirectory + "\\AvailableComics.xml" );			
			return;
		}

		// Update the subscribed comics
		public void UpdateSubscribedComics( ArrayList subscribedComics ) 
		{			
			ArrayList newComics = new ArrayList();

			// Download each comic subscribed to
			foreach( ComicInfo ci in subscribedComics ) 
			{												
				try 
				{					
					ComicInfo ciAdded = UpdateSingleComic( ci );
				}
				catch 
				{
				}
			}			
		}

		// Get a single comic
		private ComicInfo UpdateSingleComic( ComicInfo ci ) 
		{						
			for (int i = 0; i < daysToKeep; i++) 
			{ 
				// Create a webclient
				System.Net.WebClient webClient = new System.Net.WebClient();
				
				// Create the image filename from the format in the XML file
				char[] delims = {'$'};
				// Parse the String format and form the path with the year
				String[] ImagePathTokens = ci.ImagePath.Split(delims,100);
				String ImagePath = "";	
				foreach( String s in ImagePathTokens ) 
				{
					int dt = DateTime.Now.Year;				
					// Year
					if ( s.Equals("yyyy") )					
						ImagePath += dt.ToString();
					else
						ImagePath += s;
				}				

				// Parse the String format and form the filename with the date
				// Dilbert Hack: Create a filename to store that is consistent with the rest
				String[] FilenameTokens = ci.ImageFilename.Split(delims,100);
				String URIImageFilename = "";					
				String SavedImageFilename = "";
				foreach( String s in FilenameTokens ) 
				{
					DateTime dt = DateTime.Now.AddDays(-i);					
						// Year
					if ( s.Equals("YY") ) 
					{					
						SavedImageFilename += dt.ToString("yy",DateTimeFormatInfo.InvariantInfo);					
					}
						// Month
					else if ( s.Equals("MM") ) 
					{
						SavedImageFilename += dt.ToString("MM",DateTimeFormatInfo.InvariantInfo);
					}
						// Day
					else if ( s.Equals("DD") ) 
					{
						SavedImageFilename += dt.ToString("dd",DateTimeFormatInfo.InvariantInfo);						
					}					
					else 
					{
						SavedImageFilename += s;												
					}
				}					
				
				// Comics.com support
				if ( ci.Website == "Comics.com" ) 
				{							
					try 
					{
						DateTime dt = DateTime.Now.AddDays(-i);		
						HttpWebRequest webreq;
						HttpWebResponse	webres;
						string trimmedPath =  ci.ImagePath.Remove(ci.ImagePath.IndexOf("images/"),7);							
						System.Uri uri = new System.Uri(trimmedPath+ci.FolderName+"-"+DateTime.Now.Year.ToString() + dt.ToString("MM",DateTimeFormatInfo.InvariantInfo) + dt.ToString("dd",DateTimeFormatInfo.InvariantInfo) + ".html");														
						webreq = (HttpWebRequest)WebRequest.Create(uri);
						webres = (HttpWebResponse)webreq.GetResponse();										
						Stream resStream = webres.GetResponseStream();						
						string response = new StreamReader( resStream ).ReadToEnd();						
						response = response.Substring(response.IndexOf("archive/images/"+ci.FolderName));						
						string[] responseArray = response.Split('/');																													
						char[] dms = new  Char[] {'"','&'};									
						URIImageFilename = responseArray[2].Split(dms)[0];												
						ci.ImageSuffix = URIImageFilename.Split('.')[1];						
					}
					catch (Exception e) 
					{
						SnapStream.Logging.WriteLog( e.StackTrace );
					}				
				}				
				// End Comics.com support										

				// Add the extension name to the save file name
				SavedImageFilename += "." + ci.ImageSuffix;	

				// ucomics filename needs an extension
				if (ci.Website != "Comics.com")
					URIImageFilename = SavedImageFilename;		

				// Add the website path before the image filename
				String URIImagePath = ImagePath + URIImageFilename;

				// Download the image			
				try 
				{
					// Prepare the local storage folder, if it needs to be created
					string alphaNumericName = ToAlphaNumericString( ci.FolderName );
					string comicDirectory = _homeDirectory + "\\" + alphaNumericName;
					if( System.IO.Directory.Exists(comicDirectory) == false ) 
					{
						System.IO.Directory.CreateDirectory( comicDirectory );
					}		
					
					// Now create the local image path to save the file
					string imagePath = _homeDirectory + "\\" + alphaNumericName + "\\" + SavedImageFilename;			
					
					// If the file exists, don't download it again
					if( System.IO.File.Exists(imagePath) == false ) 
					{					
						string tempFilename = System.IO.Path.GetTempFileName();					   
						webClient.DownloadFile( URIImagePath, tempFilename );
						System.IO.File.Copy( tempFilename, imagePath, true );
						System.IO.File.Delete( tempFilename );
					}
				}
				catch (Exception e)
				{
					SnapStream.Logging.WriteLog( e.StackTrace );
				}				
			}
			// Now add
			ComicInfo ciAdd = new ComicInfo();
			return ciAdd = ci;
		}

		private string ToAlphaNumericString( string input ) 
		{
			string output = "";

			char[] chars = input.ToCharArray();
			foreach( char c in chars ) 
			{
				if( char.IsLetterOrDigit(c) == false ) 
				{
					continue;
				}

				output += c;
			}

			return output;
		}		

		public static ArrayList LoadComics( string fileName ) 
		{
			ArrayList			comics;
			// PropertyBagArray
			PropertyBag			bags;

			comics = new ArrayList();
			bags = new PropertyBag();
			bags.Load( fileName );
			for( int x=0; x < bags.BagCount; x++ ) 
			{
				PropertyBag bag;
				ComicInfo	ci;

				bag = (PropertyBag)bags.GetBagAt(x);
				ci = new ComicInfo();

				for( int y=0; y < bag.PropertyCount; y++ ) 
				{
					string	name, val;
					bag.GetPropertyAt( y, out name, out val );

					if( name == "Website" ) 
					{
						ci.Website = val;
					}	
					if( name == "FolderName" ) 
					{
						ci.FolderName = val;
					}					
					if( name == "DisplayName" ) 
					{
						ci.DisplayName = val;
					}					
					else if( name == "ImageSuffix" ) 
					{
						ci.ImageSuffix = val;
					}
					else if( name == "ImageFilename" ) 
					{
						ci.ImageFilename = val;
					}
					else if( name == "ImagePath" ) 
					{
						ci.ImagePath = val;
					}					
					else if( name == "Subscribed" ) 
					{
						try 
						{
							ci.Subscribed = bool.Parse( val );
						}
						catch( Exception e ) 
						{
							Helpers.AppException.PrintException( e );
						}
					}
				}

				comics.Add( ci );
			}

			return comics;
		}

		private void SaveComics( ArrayList aComics, string fileName ) 
		{
			
			PropertyBag bags;

			bags = new PropertyBag();

			foreach( ComicInfo ci in aComics ) 
			{
				PropertyBag comicBag;

				comicBag = new PropertyBag();
				comicBag.Name = ci.DisplayName;
				comicBag.SetProperty( "Website", ci.Website );
				comicBag.SetProperty( "FolderName", ci.FolderName );
				comicBag.SetProperty( "DisplayName", ci.DisplayName );
				comicBag.SetProperty( "ImageSuffix", ci.ImageSuffix );
				comicBag.SetProperty( "ImageFilename", ci.ImageFilename );
				comicBag.SetProperty( "ImagePath", ci.ImagePath );
				comicBag.SetProperty( "Subscribed", ci.Subscribed.ToString() );

				bags.AddBag( comicBag );
			}

			bags.Save( fileName );
			return;
		}

		// Remove expired comics
		public void ExpireComics( ArrayList comics ) 
		{			
			// Get directory listing for each comic
			foreach( ComicInfo ci in comics ) 
			{								
				string alphaNumericName = ToAlphaNumericString( ci.FolderName );
				string comicDirectory = _homeDirectory + "\\" + alphaNumericName;				
				DirectoryInfo di = new DirectoryInfo( comicDirectory );
				// If the directory exists, get all the files
				if( System.IO.Directory.Exists(comicDirectory) == true ) 
				{
					char[] delims = {':'};
					String[] FilenameTokens = ci.ImageFilename.Split(delims,100);
					FileInfo[] images = di.GetFiles(FilenameTokens[0] + "*." + ci.ImageSuffix);
					int dayCount = images.Length;
					// From oldest to newest
					foreach(FileInfo fi in images)
					{
						// Remove the file if the day count is greater than the days to keep
						if (dayCount-- > daysToKeep)							
							System.IO.File.Delete( fi.FullName );
					}
				}	
			}
			return;
		}

		public void Dispose() 
		{
			WorkerThread.Abort();
			return;
		}
	}


	// Singleton Updater to let other classes access this module
	public sealed class SingletonComicsUpdater : ComicsUpdater  
	{
		public static readonly ComicsUpdater Instance = new ComicsUpdater();

		public SingletonComicsUpdater() 
		{
			return;
		}
	}

}
