using System;
using System.Net;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Threading;
using System.Text;

using SnapStream.ViewScape.Services;
using SnapStream.ViewScape.Widgets;
using SnapStream.Commands;

namespace SnapStream.Plugins.YahooTrailers
{	
	// YahooTrailers Movie List
	internal class Trailer : BaseListItem 
	{
		#region Private Members		
		private string	_title;							
		private TextWindow	_display;	
		public string		_movieURL;
		private Window		_logo;
		private Window		_controlbutton;
		#endregion Private Members

		#region Properties				
		// Window holds title	
		public string Title 
		{
			get 
			{
				return _title;
			}
		}	
		public TextWindow Display 
		{
			get 
			{
				return _display;
			}
		}	

		public Window Logo 
		{
			get 
			{
				return _logo;
			}
		}
	
		public Window ControlButton 
		{
			get 
			{
				return _controlbutton;
			}
		}
		public void ChangeControl(string control)
		{			
			System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
			System.IO.FileInfo fi = new System.IO.FileInfo( a.Location );

			if (control.Equals("PLAY")) 
			{
				_title = "PLAY " + _title;
				_controlbutton.Background = fi.DirectoryName + "\\play.png";
			}
			else 
			{
				_title = _title.Replace("PLAY ","");
				_controlbutton.Background = fi.DirectoryName + "\\download.png";			
			}
		}
		#endregion Properties

		#region Constructors		
		public Trailer( string title, string display, string format ) 
		{			
			System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
			System.IO.FileInfo fi = new System.IO.FileInfo( a.Location );

			_title = title;

			_display = new TextWindow();	
			_display.Text = display;			
			_display.RelativeBounds = new Rectangle(80,5,200,25);
			_display.HorizontalAlign = HAlignment.Left;
			Add( _display );
			
			_logo = new Window();	
			if (format.Equals("480p"))
				_logo.Background = fi.DirectoryName + "\\qtlogo480p.png";
			else if (format.Equals("720p"))
				_logo.Background = fi.DirectoryName + "\\qtlogo720p.png";
			else if (format.Equals("1080p"))
				_logo.Background = fi.DirectoryName + "\\qtlogo1080p.png";
			else
				_logo.Background = fi.DirectoryName + "\\qtlogo.png";
			_logo.RelativeBounds = new Rectangle(20,7,20,20);			
			Add( _logo );

			_controlbutton = new Window();	
			if (title.StartsWith("PLAY"))
				_controlbutton.Background = fi.DirectoryName + "\\play.png";
			else
				_controlbutton.Background = fi.DirectoryName + "\\download.png";
			_controlbutton.RelativeBounds = new Rectangle(50,7,20,20);			
			Add( _controlbutton );
			
			return;
		}		
		#endregion Constructors		
	}

	

	// Screen that shows the YahooTrailers Channels
	public class YahooTrailersDetailsScreen : ScreenBase
	{		
		#region Private Members										
		private TextWindow					_header;			
		private TextWindow					_details;					
		private TextWindow					_starring;
		private TextWindow					_genre;
		private TextWindow					_releasedate;
		private TextWindow					_rating;
		private VariableItemList			_trailers;		
		private PosterWindow				_poster;	
		private Window						_yahoologo;
		private Window						_downloading;
		private string						homedir;			
		private string						movURL;	
		private string						movLocalFile;
		private string						movLocalPath;
		private string						movTitle;		
		//private Thread						dlThread;
		private System.Windows.Forms.Timer	updateTimer;	
		#endregion Private Members

		#region Properties		
		public TextWindow Header 
		{
			get 
			{
				return _header;
			}
		}
		public PosterWindow Poster 
		{
			get 
			{
				return _poster;
			}
		}
		public TextWindow Details 
		{
			get 
			{
				return _details;
			}
		}
		public TextWindow Starring 
		{
			get 
			{
				return _starring;
			}
		}
		public TextWindow Genre 
		{
			get 
			{
				return _genre;
			}
		}		
		public TextWindow ReleaseDate 
		{
			get 
			{
				return _releasedate;
			}
		}
		public TextWindow Rating 
		{
			get 
			{
				return _rating;
			}
		}
		public VariableItemList Trailers
		{
			get 
			{
				return _trailers;
			}	
		}
		public Window YahooLogo 
		{
			get 
			{
				return _yahoologo;
			}
		}
		public Window Downloading 
		{
			get 
			{
				return _downloading;
			}
		}
		#endregion Properties

		#region Constructors
		/// <summary>
		/// Creates the ComicsScreen
		/// </summary>
		public YahooTrailersDetailsScreen() 
		{			
			System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
			System.IO.FileInfo fi = new System.IO.FileInfo( a.Location );		
			
			homedir = fi.DirectoryName;

			// Text Objects			
			_header = new TextWindow();			
			Add( _header );

			_poster = new PosterWindow();
			Add( _poster );

			_details = new TextWindow();	
			Add( _details );

			_starring = new TextWindow();				
			Add( _starring );		

			_genre = new TextWindow();				
			Add( _genre );			

			_releasedate = new TextWindow();				
			Add( _releasedate );	
		
			_rating = new TextWindow();				
			Add( _rating );

			_trailers = new VariableItemList();						
			Add( _trailers );											
			_trailers.ItemActivated += new ItemActivatedEventHandler(List_ItemActivated);
			_trailers.Visible = true;
			_trailers.Focus();	

			// Logo			
			_yahoologo = new Window();		
			Add( _yahoologo );	

			// Downloading logo		
			_downloading = new Window();		
			Add( _downloading );	

			// Create the timer
			updateTimer = new System.Windows.Forms.Timer();
			updateTimer.Interval = 250;
			updateTimer.Tick += new EventHandler(updateTimer_Tick);
			updateTimer.Start();
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
				RaiseExitEvent();
				e.Handled = true;
				return;
			}
			
			return;
		}
		
		// On entering the screen
		public override void Activate() 
		{				
			if (base.NavigatingForward) 
			{
				base.Activate();		
				
				// Get all trailers downloaded
				string[] movList = Directory.GetFiles(homedir + "\\Trailers", "*.mov");
				for (int i = 0; i < movList.Length; i++) {
					int lastIndex = movList[i].LastIndexOf("\\");
					movList[i] = movList[i].Substring(lastIndex+1);
				}
			
				// Get the movie info to display				
				YahooTrailersInfo movieinfo = (YahooTrailersInfo)ScreenArgs;		
				//SnapStream.Logging.WriteLog("YahooTrailers: Loading " + movieinfo.Title.ToString());
				_header.Text = movieinfo.Title.ToString();				
				_details.Text = movieinfo.Details.ToString();				
				_starring.Text = movieinfo.Starring.ToString();
				_genre.Text = movieinfo.Genre.ToString();
				_releasedate.Text = movieinfo.ReleaseDate.ToString();								
				_rating.Text = movieinfo.Rating.ToString();				

				// Get Poster				
				string jpgpath = homedir + "\\Posters\\" + movieinfo.Title.ToString().Replace(":","").Replace("?","") + ".jpg";			
				((PosterWindow)_poster).LoadTexture( jpgpath );

				// Create variable item list of trailers
				((BaseList)_trailers).Clear();

				// List all the available trailers
				string movieName = _header.Text;
				Trailer trailer;

				// Trailer 2
				if (movieinfo._trailer2URL != null) 
				{
					//SnapStream.Logging.WriteLog("YahooTrailers: Getting Trailer #2 Links");
					// 480p Link
					if (isInLibrary(movieName + "_Trailer2@480p.mov", movList))
						trailer = new Trailer("PLAY Trailer2@480p", "Trailer 2", "480p");					
					else
						trailer = new Trailer("Trailer2@480p", "Trailer 2", "480p");	
					trailer._movieURL = movieinfo._trailer2URL[0];
					trailer.Height = 35;
					_trailers.AddItem(trailer);
					// 720p Link
					if (isInLibrary(movieName + "_Trailer2@720p.mov", movList))
						trailer = new Trailer("PLAY Trailer2@720p", "Trailer 2", "720p");					
					else
						trailer = new Trailer("Trailer2@720p", "Trailer 2", "720p");													
					trailer._movieURL = movieinfo._trailer2URL[1];
					trailer.Height = 35;
					_trailers.AddItem(trailer);
					// 1080p Link
					if (isInLibrary(movieName + "_Trailer2@1080p.mov", movList))
						trailer = new Trailer("PLAY Trailer2@1080p", "Trailer 2", "1080p");					
					else
						trailer = new Trailer("Trailer2@1080p", "Trailer 2", "1080p");										
					trailer._movieURL = movieinfo._trailer2URL[2];
					trailer.Height = 35;
					_trailers.AddItem(trailer);
				}
				// Trailer
				if (movieinfo._trailerURL != null) 
				{
					//SnapStream.Logging.WriteLog("YahooTrailers: Getting Trailer Links");
					// 480p Link										
					if (isInLibrary(movieName + "_Trailer@480p.mov", movList))
						trailer = new Trailer("PLAY Trailer@480p", "Trailer", "480p");					
					else
						trailer = new Trailer("Trailer@480p", "Trailer", "480p");					
					trailer._movieURL = movieinfo._trailerURL[0];
					trailer.Height = 35;
					_trailers.AddItem(trailer);
					// 720p Link
					if (isInLibrary(movieName + "_Trailer@720p.mov", movList))
						trailer = new Trailer("PLAY Trailer@720p", "Trailer", "720p");					
					else
						trailer = new Trailer("Trailer@720p", "Trailer", "720p");									
					trailer._movieURL = movieinfo._trailerURL[1];
					trailer.Height = 35;
					_trailers.AddItem(trailer);
					// 1080p Link
					if (isInLibrary(movieName + "_Trailer@1080p.mov", movList))
						trailer = new Trailer("PLAY Trailer@1080p", "Trailer", "1080p");					
					else
						trailer = new Trailer("Trailer@1080p", "Trailer", "1080p");										
					trailer._movieURL = movieinfo._trailerURL[2];
					trailer.Height = 35;
					_trailers.AddItem(trailer);
				}
				// Teaser
				if (movieinfo._teaser2URL != null) 
				{
					//SnapStream.Logging.WriteLog("YahooTrailers: Getting Teaser #2 Links");
					// 480p Link
					if (isInLibrary(movieName + "_Teaser2@480p.mov", movList))
						trailer = new Trailer("PLAY Teaser2@480p", "Teaser 2", "480p");					
					else
						trailer = new Trailer("Teaser2@480p", "Teaser 2", "480p");			
					trailer._movieURL = movieinfo._teaser2URL[0];
					trailer.Height = 35;
					_trailers.AddItem(trailer);
					// 720p Link
					if (isInLibrary(movieName + "_Teaser2@720p.mov", movList))
						trailer = new Trailer("PLAY Teaser2@720p", "Teaser 2", "720p");					
					else
						trailer = new Trailer("Teaser2@720p", "Teaser 2", "720p");						
					trailer._movieURL = movieinfo._teaser2URL[1];
					trailer.Height = 35;
					_trailers.AddItem(trailer);
					// 1080p Link
					if (isInLibrary(movieName + "_Teaser2@1080p.mov", movList))
						trailer = new Trailer("PLAY Teaser2@1080p", "Teaser 2", "1080p");					
					else
						trailer = new Trailer("Teaser2@1080p", "Teaser 2", "1080p");						
					trailer._movieURL = movieinfo._teaser2URL[2];
					trailer.Height = 35;
					_trailers.AddItem(trailer);
				}
				// Teaser
				if (movieinfo._teaserURL != null) 
				{
					//SnapStream.Logging.WriteLog("YahooTrailers: Getting Teaser Links");
					// 480p Link
					if (isInLibrary(movieName + "_Teaser@480p.mov", movList))
						trailer = new Trailer("PLAY Teaser@480p", "Teaser", "480p");					
					else
						trailer = new Trailer("Teaser@480p", "Teaser", "480p");					
					trailer._movieURL = movieinfo._teaserURL[0];					
					trailer.Height = 35;
					_trailers.AddItem(trailer);
					// 720p Link
					if (isInLibrary(movieName + "_Teaser@720p.mov", movList))
						trailer = new Trailer("PLAY Teaser@720p", "Teaser", "720p");					
					else
						trailer = new Trailer("Teaser@720p", "Teaser", "720p");								
					trailer._movieURL = movieinfo._teaserURL[1];
					trailer.Height = 35;
					_trailers.AddItem(trailer);
					// 1080p Link
					if (isInLibrary(movieName + "_Teaser@1080p.mov", movList))
						trailer = new Trailer("PLAY Teaser@1080p", "Teaser", "1080p");					
					else
						trailer = new Trailer("Teaser@1080p", "Teaser", "1080p");					
					trailer._movieURL = movieinfo._teaserURL[2];
					trailer.Height = 35;
					_trailers.AddItem(trailer);
				}
				
							
				return;
			}			
		}

		public override void Deactivate()
		{
			base.Deactivate ();					
		}

		protected override void DisposeCore() 
		{
			base.DisposeCore();
			SingletonDownloader.Instance.Dispose();
			updateTimer.Dispose();
			updateTimer = null;			
			return;
		}
		#endregion Window Overrides

		#region Private Methods							
		// Play selected trailer
		private void List_ItemActivated( object sender, ItemActivatedArgs args ) 
		{	
			string movieURL = ((Trailer)_trailers.SelectedItem)._movieURL.Replace("&amp;","&");			
			SnapStream.Logging.WriteLog(movieURL);
			movURL = null;
						
			// Create a webclient			
			try 
			{				
				// Get the trailer qtl file			
				HttpWebRequest webreq = (HttpWebRequest)WebRequest.Create(new System.Uri(movieURL));				
				webreq.Referer = "http://movies.yahoo.com/";
				webreq.AllowAutoRedirect = false;
				HttpWebResponse webres = (HttpWebResponse)webreq.GetResponse();								
				Stream resStream = webres.GetResponseStream();	
				string response = new StreamReader( resStream ).ReadToEnd();	
				//SnapStream.Logging.WriteLog("Movie URL Response: " + response);				
				int startindex = response.IndexOf("HREF=\"")+6;
				int endindex = response.IndexOf("\">",startindex);				
				string qtlURL = response.Substring(startindex,endindex-startindex).Trim();
				//SnapStream.Logging.WriteLog("QTL URL: " + qtlURL);

				// Get the sid in the qtl file
				webreq = (HttpWebRequest)WebRequest.Create(new System.Uri(qtlURL.Replace("&amp;","&")));				
				webreq.AllowAutoRedirect = true; // was false
				webreq.Referer = "http://movies.yahoo.com/";
				webres = (HttpWebResponse)webreq.GetResponse();							
				resStream = webres.GetResponseStream();	
				response = new StreamReader( resStream ).ReadToEnd();	
				//SnapStream.Logging.WriteLog("QTL URL Response: " + response);		
				startindex = response.IndexOf("sid=")+4;
				endindex = response.IndexOf("&t",startindex);						
				movURL = "http://playlist.yahoo.com/makeplaylist.dll?sdm=web&pt=rd&sid=" + response.Substring(startindex,endindex-startindex).Trim();
				//SnapStream.Logging.WriteLog("MOV URL: " + movURL);													

				webreq = (HttpWebRequest)WebRequest.Create(new System.Uri(movURL.Replace("&amp;","&")));				
				webreq.AllowAutoRedirect = false; // was false				
				webres = (HttpWebResponse)webreq.GetResponse();							
				resStream = webres.GetResponseStream();	
				response = new StreamReader( resStream ).ReadToEnd();	
				//SnapStream.Logging.WriteLog("QTL URL Response: " + response);									
				
				// Create local movie file path
				movTitle = _header.Text + "_" + ((Trailer)_trailers.SelectedItem).Title;
				movLocalFile = movTitle + ".mov";
				movLocalFile = movLocalFile.Replace(":","").Replace("?","");
				movLocalPath = homedir + "\\Trailers\\" + movLocalFile;	
				//SnapStream.Logging.WriteLog(movLocalFile);
				
				// Download or Play trailer
				if (((Trailer)_trailers.SelectedItem).Title.StartsWith("PLAY")) 
				{
					movLocalPath = movLocalPath.Replace("PLAY ","");
					//SnapStream.Logging.WriteLog("Playing: " + movLocalPath);
					ShowScreen s = new ShowScreen("VideoPlayerScreen", movLocalPath);
					s.Execute();	
				}
				else 
				{
					//SnapStream.Logging.WriteLog("Downloading");
					//dlThread = new Thread(new ThreadStart(download));
					//dlThread.Start();					
					SingletonDownloader.Instance.movLocalPath = movLocalPath;
					SingletonDownloader.Instance.movURL = movURL;										
					SingletonDownloader.Instance.Download();
					((Trailer)_trailers.SelectedItem).ChangeControl("PLAY");
					//SnapStream.Logging.WriteLog(SingletonDownloader.Instance.isDownloading.ToString());
				}				
			}
			catch(Exception exp)
			{
				string str = exp.Message;
			}						
		}	
	
		// Handle movie downloads
		//private void download() 
		//{
		//	SnapStream.Logging.WriteLog("Yahoo Trailers: Downloading " + movURL);												
		//	WebClient webClient = new WebClient();
		//	webClient.DownloadFile( movURL, movLocalPath );							
		//}		

		// Search for existing trailer file
		private bool isInLibrary(string filename, string[] movList) 
		{			
			filename = filename.Replace(":","").Replace("?","");			
			bool isFound = false;			
			for (int i = 0; i < movList.Length; i++) 
			{
				//SnapStream.Logging.WriteLog("Comparing: " + movList[i]);
				if (filename.Equals(movList[i])) 
				{							
					isFound = true;
					break;
				}
			}		
			return isFound;
		}

		// Update the downloading icon
		private void updateTimer_Tick( object sender, EventArgs e ) 
		{			
			// Check the thread status
			if (SingletonDownloader.Instance.isDownloading) 
			{
				_downloading.Visible = true;
			}
			else 
			{
				_downloading.Visible = false;				
			}
						
		}
		#endregion Private Methods		
	}
}