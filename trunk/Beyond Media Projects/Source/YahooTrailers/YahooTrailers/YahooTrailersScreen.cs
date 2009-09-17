using System;
using System.Net;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;
using System.IO;
using System.Xml;

using SnapStream.ViewScape.Services;
using SnapStream.ViewScape.Widgets;
using SnapStream.Action;
using SnapStream.Commands;

namespace SnapStream.Plugins.YahooTrailers
{
	// YahooTrailers Movie List
	internal class YahooTrailersListItem : BaseListItem 
	{
		#region Private Members		
		private TextWindow	_title;					
		private TextWindow	_starringWindow;	
		private PosterWindow	_poster;		
		public string		_details;
		public string		_starring;
		public string		_genre;
		public string		_releasedate;
		public string		_rating;
		public string		_jpegURL;
		public string[]     _trailer2URL;
		public string[]     _trailerURL;
		public string[]     _teaser2URL;
		public string[]     _teaserURL;		
		#endregion Private Members

		#region Properties				
		// Window holds title	
		public TextWindow Title 
		{
			get 
			{
				return _title;
			}
		}		
		public TextWindow Starring 
		{
			get 
			{
				return _starringWindow;
			}
		}		
		public PosterWindow Poster 
		{
			get 
			{
				return _poster;
			}			
		}		
		#endregion Properties

		#region Constructors		
		public YahooTrailersListItem( string caption ) 
		{			
			_title = new TextWindow();	
			_title.Text = caption;			
			_title.RelativeBounds = new Rectangle(70,10,640,30);
			_title.HorizontalAlign = HAlignment.Left;
			Add( _title );						
			
			return;
		}

		public void addStarring( string starring ) 
		{
			_starringWindow = new TextWindow();	
			_starringWindow.Text = starring;
			_starringWindow.FontSize = 18;
			_starringWindow.RelativeBounds = new Rectangle(70,45,640,30);
			_starringWindow.HorizontalAlign = HAlignment.Left;
			Add( _starringWindow );		

			return;
		}

		public void addPoster ( string title )
		{
			//SnapStream.Logging.WriteLog("Add Poster: " + title);
			System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
			System.IO.FileInfo fi = new System.IO.FileInfo( a.Location );									
			
			_poster = new PosterWindow();
			_poster.RelativeBounds = new Rectangle(23,10,35,50);
			string jpgpath = fi.DirectoryName + "\\Posters\\" + title.Replace(":","").Replace("?","") + ".jpg";
			((PosterWindow)_poster).LoadTexture( jpgpath );		
			Add( _poster );

			return;
		}
		#endregion Constructors		
	}

	// Screen that shows the YahooTrailers Channels
	public class YahooTrailersScreen : ScreenBase
	{		
		#region Private Members										
		private TextWindow					_header;
		private TextButton					_library;
		private TextButton					_view;
		private VariableItemList			_trailerlist;							
		private Window						_yahoologo;
		private Window						_downloading;
		private System.Uri					uri;
		private HttpWebRequest				webreq;
		private HttpWebResponse				webres;			
		private CookieContainer				cookies;
		private Stream						resStream;
		private string						response;		
		private YahooTrailersInfo			movieinfo;
		private string						homedir;
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

		public TextButton Library 
		{
			get 
			{
				return _library;
			}
		}

		public TextButton View 
		{
			get 
			{
				return _view;
			}
		}

		public VariableItemList TrailerList 
		{
			get 
			{
				return _trailerlist;
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
		/// Creates the Screen
		/// </summary>
		public YahooTrailersScreen() 
		{
			SnapStream.Logging.WriteLog("YahooTrailers Plugin Started");

			System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
			System.IO.FileInfo fi = new System.IO.FileInfo( a.Location );	
			
			homedir = fi.DirectoryName;		

			// Text Objects			
			_header = new TextWindow();			
			Add( _header );			

			// Text Button for the library
			_library = new TextButton();			
			_library.Click +=new EventHandler(_library_Click);
			Add( _library );

			// Text Button for the view
			_view = new TextButton();			
			_view.Click +=new EventHandler(_view_Click);
			Add( _view );

			// Create the list viewer
			_trailerlist = new VariableItemList();						
			Add( _trailerlist );						
			_trailerlist.ItemActivated += new ItemActivatedEventHandler(List_ItemActivated);
			_trailerlist.Visible = true;
			_trailerlist.Focus();	
			
			// Logo			
			_yahoologo = new Window();		
			Add( _yahoologo );							

			// Downloading logo		
			_downloading = new Window();		
			Add( _downloading );

			// Get the trailers
			GetTrailers();	

			// Create the timer
			updateTimer = new System.Windows.Forms.Timer();
			updateTimer.Interval = 250;
			updateTimer.Tick += new EventHandler(updateTimer_Tick);
			updateTimer.Start();
						
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
				RaiseExitEvent();
				e.Handled = true;
				return;
			}

			// Go to and from Library Button
			if( e.KeyCode == System.Windows.Forms.Keys.Right ) 
			{
				if (_library.Focus())
					_view.Focus();					
				else
					_library.Focus();			
			}

			if( e.KeyCode == System.Windows.Forms.Keys.Left )
			{
				if (_view.Focus())
					_library.Focus();								
				else
					_trailerlist.Focus();									
			}

			if( e.KeyCode == System.Windows.Forms.Keys.Down )
			{
				_trailerlist.Focus();			
			}
			
			return;
		}

		// On entering the screen
		public override void Activate() 
		{			
			string[] movList;			
			if (base.NavigatingForward) 
			{
				base.Activate();	

				// Get the days to keep setting
				int sDaysToKeep;
				SingletonConfig.Instance.GetPropertyAsInt( "YahooTrailers.DaysToKeep", out sDaysToKeep );

				// Delete all trailers 7 days or more older				
				movList = Directory.GetFiles(homedir + "\\Trailers", "*.mov");
				foreach (string mov in movList)
				{
					FileInfo movInfo = new FileInfo(mov);				
					if (movInfo.CreationTime < DateTime.Now.AddDays(-sDaysToKeep))
						movInfo.Delete();
				}								

				// Delete all posters depending on settings				
				string[] posterList = Directory.GetFiles(homedir + "\\Posters", "*.jpg");
				foreach (string jpg in posterList)
				{
					FileInfo jpgInfo = new FileInfo(jpg);				
					if (jpgInfo.CreationTime < DateTime.Now.AddDays(-sDaysToKeep))
						jpgInfo.Delete();
				}							
			}			

			// Update the count for the header
			movList = Directory.GetFiles(homedir + "\\Trailers", "*.mov");
			_library.Text = "Trailer Library: " + movList.Length.ToString();
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
		// Go to the Trailer Library Screen
		private void _library_Click(object sender, EventArgs e)
		{				
			ShowScreen s = new ShowScreen( "MovFileBrowserScreen" );
			s.Execute();
		}

		// Go to the other View
		private void _view_Click(object sender, EventArgs e)
		{				
			ShowScreen s = new ShowScreen( "YahooTrailersPosterScreen" );
			s.Execute();
		}

					
		// Show when item is entered
		private void List_ItemActivated( object sender, ItemActivatedArgs args ) 
		{				
			movieinfo = new YahooTrailersInfo();			
			movieinfo.Title = ((YahooTrailersListItem)_trailerlist.SelectedItem).Title.Text;			
			movieinfo.Details = replaceSpecials(((YahooTrailersListItem)_trailerlist.SelectedItem)._details);
			movieinfo.Starring = replaceSpecials(((YahooTrailersListItem)_trailerlist.SelectedItem)._starring);
			movieinfo.Genre = replaceSpecials(((YahooTrailersListItem)_trailerlist.SelectedItem)._genre);			
			movieinfo.ReleaseDate = replaceSpecials(((YahooTrailersListItem)_trailerlist.SelectedItem)._releasedate);			
			movieinfo.Rating = replaceSpecials(((YahooTrailersListItem)_trailerlist.SelectedItem)._rating);
			movieinfo.JPEGURL = replaceSpecials(((YahooTrailersListItem)_trailerlist.SelectedItem)._jpegURL);
			if ( ((YahooTrailersListItem)_trailerlist.SelectedItem)._trailer2URL != null )
				movieinfo._trailer2URL = ((YahooTrailersListItem)_trailerlist.SelectedItem)._trailer2URL;
			if ( ((YahooTrailersListItem)_trailerlist.SelectedItem)._trailerURL != null )
				movieinfo._trailerURL = ((YahooTrailersListItem)_trailerlist.SelectedItem)._trailerURL;
			if ( ((YahooTrailersListItem)_trailerlist.SelectedItem)._teaser2URL != null )
				movieinfo._teaser2URL = ((YahooTrailersListItem)_trailerlist.SelectedItem)._teaser2URL;				
			if ( ((YahooTrailersListItem)_trailerlist.SelectedItem)._teaserURL != null )
				movieinfo._teaserURL = ((YahooTrailersListItem)_trailerlist.SelectedItem)._teaserURL;				
			ShowScreen s = new ShowScreen( "YahooTrailersDetailsScreen", movieinfo );
			s.Execute();	
			return;
		}

		private void GetTrailers() 
		{						
			// try to log in						
			uri = new System.Uri("http://movies.yahoo.com/feature/hdtrailers.html");
			SnapStream.Logging.WriteLog("YahooTrailers: Scraping http://movies.yahoo.com/feature/hdtrailers.html"); 
			// Create a webclient			
			webreq = (HttpWebRequest)WebRequest.Create(uri);
			cookies = new CookieContainer();
			webreq.CookieContainer = cookies;
			// Get the response
			try 
			{
				webres = (HttpWebResponse)webreq.GetResponse();										
				resStream = webres.GetResponseStream();	
				response = new StreamReader( resStream ).ReadToEnd();	
				response = response.Substring(response.IndexOf("<!-- Menu Links -->"));				
				// Create a new List item
				YahooTrailersListItem trailerlistitem;
				while (response.IndexOf("\"a-m-t\">") != -1) 
				{
					// Get the Title
					int startindex = response.IndexOf("\"a-m-t\">")+19;
					int endindex = response.IndexOf("</dt>",startindex);
					string title = response.Substring(startindex,endindex-startindex).Trim();								
					trailerlistitem = new YahooTrailersListItem(title);
					//SnapStream.Logging.WriteLog("YahooTrailers: " + title);									
					response = response.Substring(startindex+title.Length);
					// Get the Poster URL
					startindex = response.IndexOf("img src=\"")+9;
					endindex = response.IndexOf("\" border",startindex);
					trailerlistitem._jpegURL = response.Substring(startindex,endindex-startindex).Trim();
					////SnapStream.Logging.WriteLog(trailerlistitem._jpegURL);		
					response = response.Substring(startindex+trailerlistitem._jpegURL.Length);				
					// Get the Details
					startindex = response.IndexOf("<div>")+6;
					endindex = response.IndexOf("<br />",startindex)-1;
					trailerlistitem._details = response.Substring(startindex,endindex-startindex).Trim();
					//SnapStream.Logging.WriteLog(trailerlistitem._details);		
					response = response.Substring(startindex+trailerlistitem._details.Length);						
					// Get the Starring actors
					startindex = response.IndexOf("Starring: ")+10;						
					endindex = response.IndexOf("<br />",startindex);				
					trailerlistitem._starring = response.Substring(startindex,endindex-startindex).Trim();
					trailerlistitem.addStarring( trailerlistitem._starring );
					//SnapStream.Logging.WriteLog(trailerlistitem._starring);						
					response = response.Substring(startindex+trailerlistitem._starring.Length);												
					// Get the Genre
					startindex = response.IndexOf("<br />")+7;						
					endindex = response.IndexOf("<br />",startindex);				
					trailerlistitem._genre = response.Substring(startindex,endindex-startindex).Trim();
					//SnapStream.Logging.WriteLog(trailerlistitem._genre);						
					response = response.Substring(startindex+trailerlistitem._genre.Length);																
					// Get the release date
					startindex = response.IndexOf("<br />")+7;
					endindex = response.IndexOf("<br />",startindex);
					trailerlistitem._releasedate = response.Substring(startindex,endindex-startindex).Trim();
					//SnapStream.Logging.WriteLog(trailerlistitem._releasedate);		
					response = response.Substring(startindex+trailerlistitem._releasedate.Length);					
					// Get the Rating
					startindex = response.IndexOf("<br />")+7;
					endindex = response.IndexOf("<br />",startindex);
					trailerlistitem._rating = response.Substring(startindex,endindex-startindex).Trim();
					//SnapStream.Logging.WriteLog(trailerlistitem._rating);		
					response = response.Substring(startindex+trailerlistitem._rating.Length);				
					// Find Trailers															
					int foundTrailer2 = response.IndexOf("Trailer 2:",0,response.IndexOf("</dd>"));
					int foundTrailer = response.IndexOf("Trailer:",0,response.IndexOf("</dd>"));
					int foundTeaser2 = response.IndexOf("Teaser 2:",0,response.IndexOf("</dd>"));
					int foundTeaser = response.IndexOf("Teaser:",0,response.IndexOf("</dd>"));					
					string subresponse;				
					// Get Trailer 2
					if (foundTrailer2 != -1) 
					{
						//SnapStream.Logging.WriteLog("YahooTrailers: Found Trailer 2");

						// Declare the trailer array
						trailerlistitem._trailer2URL = new string[3];

						// Get to the HTML
						subresponse = response.Substring(foundTrailer2);
						//SnapStream.Logging.WriteLog(subresponse);

						// Get all three resolutions																				
						startindex = subresponse.IndexOf("href=\"")+6;
						endindex = subresponse.IndexOf("\"",startindex);
						trailerlistitem._trailer2URL[0] = subresponse.Substring(startindex,endindex-startindex).Trim();
						//SnapStream.Logging.WriteLog(trailerlistitem._trailer2URL[0]);		
						//response = response.Substring(startindex+trailerlistitem._trailer2URL[i].Length);		
						trailerlistitem._trailer2URL[1] = trailerlistitem._trailer2URL[0].Replace("480","720");
						//SnapStream.Logging.WriteLog(trailerlistitem._trailer2URL[1]);		
						trailerlistitem._trailer2URL[2] = trailerlistitem._trailer2URL[0].Replace("480","1080");
						//SnapStream.Logging.WriteLog(trailerlistitem._trailer2URL[2]);
					}
					// Get Trailer
					if (foundTrailer != -1) 
					{
						////SnapStream.Logging.WriteLog("YahooTrailers: Found Trailer");

						// Declare the trailer array
						trailerlistitem._trailerURL = new string[3];

						// Get to the HTML
						subresponse = response.Substring(foundTrailer);
						////SnapStream.Logging.WriteLog(subresponse);

						// Get all three resolutions																				
						startindex = subresponse.IndexOf("href=\"")+6;
						endindex = subresponse.IndexOf("\"",startindex);
						trailerlistitem._trailerURL[0] = subresponse.Substring(startindex,endindex-startindex).Trim();
						//SnapStream.Logging.WriteLog(trailerlistitem._trailerURL[0]);		
						//response = response.Substring(startindex+trailerlistitem._trailerURL[i].Length);		
						trailerlistitem._trailerURL[1] = trailerlistitem._trailerURL[0].Replace("480","720");
						//SnapStream.Logging.WriteLog(trailerlistitem._trailerURL[1]);		
						trailerlistitem._trailerURL[2] = trailerlistitem._trailerURL[0].Replace("480","1080");
						//SnapStream.Logging.WriteLog(trailerlistitem._trailerURL[2]);	
					}
					// Get Teaser 2
					if (foundTeaser2 != -1) 
					{
						////SnapStream.Logging.WriteLog("YahooTrailers: Found Teaser 2");

						// Declare the trailer array
						trailerlistitem._teaser2URL = new string[3];

						// Get to the HTML
						subresponse = response.Substring(foundTeaser2);
						////SnapStream.Logging.WriteLog(subresponse);

						// Get all three resolutions																				
						startindex = subresponse.IndexOf("href=\"")+6;
						endindex = subresponse.IndexOf("\"",startindex);
						trailerlistitem._teaser2URL[0] = subresponse.Substring(startindex,endindex-startindex).Trim();
						//SnapStream.Logging.WriteLog(trailerlistitem._teaser2URL[0]);		
						//response = response.Substring(startindex+trailerlistitem._teaser2URL[i].Length);		
						trailerlistitem._teaser2URL[1] = trailerlistitem._teaser2URL[0].Replace("480","720");
						//SnapStream.Logging.WriteLog(trailerlistitem._teaser2URL[1]);		
						trailerlistitem._teaser2URL[2] = trailerlistitem._teaser2URL[0].Replace("480","1080");
						//SnapStream.Logging.WriteLog(trailerlistitem._teaser2URL[2]);	
					}
					// Get Teaser
					if (foundTeaser != -1) 
					{
						//SnapStream.Logging.WriteLog("YahooTrailers: Found Teaser");

						// Declare the trailer array
						trailerlistitem._teaserURL = new string[3];

						// Get to the HTML
						subresponse = response.Substring(foundTeaser);
						////SnapStream.Logging.WriteLog(subresponse);

						// Get all three resolutions																				
						startindex = subresponse.IndexOf("href=\"")+6;
						endindex = subresponse.IndexOf("\"",startindex);
						trailerlistitem._teaserURL[0] = subresponse.Substring(startindex,endindex-startindex).Trim();
						//SnapStream.Logging.WriteLog(trailerlistitem._teaserURL[0]);		
						//response = response.Substring(startindex+trailerlistitem._teaserURL[i].Length);		
						trailerlistitem._teaserURL[1] = trailerlistitem._teaserURL[0].Replace("480","720");
						//SnapStream.Logging.WriteLog(trailerlistitem._teaserURL[1]);		
						trailerlistitem._teaserURL[2] = trailerlistitem._teaserURL[0].Replace("480","1080");
						//SnapStream.Logging.WriteLog(trailerlistitem._teaserURL[2]);		
					}

					// Get Posters												
					string jpgpath = homedir + "\\Posters\\" + title.Replace(":","").Replace("?","") + ".jpg";			
					try 
					{
						System.Net.WebClient webClient = new System.Net.WebClient();				
						webClient.DownloadFile( trailerlistitem._jpegURL, jpgpath);						
						trailerlistitem.addPoster(title);
					}		
					catch (System.Net.WebException e) 
					{	
						SnapStream.Logging.WriteLog(e.Message);
					}	

					// Add to trailer list
					trailerlistitem.Height = 75;
					_trailerlist.AddItem(trailerlistitem);
				}
				SnapStream.Logging.WriteLog("YahooTrailers: Done fetching trailer information");
			} 
			catch (System.Net.WebException e) 
			{
				SnapStream.Logging.WriteLog("YahooTrailers: " + e.ToString());	
				_header.Text = "Yahoo Trailers: Connection Error";					
			}		
		}	

		private string replaceSpecials(string s) 
		{
			s = s.Replace("&amp;","&");
			s = s.Replace("&#x2019;","\'");
			s = s.Replace("&#x201C;","\"");
			s = s.Replace("&#x201D;","\"");
			s = s.Replace("&#x2026;","...");
			s = s.Replace("&#x2014;","-");
			s = s.Replace("&#223;","é");
			return s;
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