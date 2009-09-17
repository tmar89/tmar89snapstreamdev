using System;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;
using System.Text;
using System.Net;
using System.IO;

using SnapStream.ViewScape.Services;
using SnapStream.ViewScape.Widgets;

namespace SnapStream.Plugins.Comics
{
	public class ComicsPreview : SnapStream.ViewScape.Widgets.Window 
	{	
		#region Members
		private Window	_comic;
		private bool isVisible = false;
		private string tempFilename;			
		private System.Timers.Timer listener = new System.Timers.Timer(100);				

		public Window ComicWindow 
		{
			get 
			{
				return _comic;
			}
		}
		#endregion Members

		#region Constructors
		public ComicsPreview() 
		{
			// Create the comic window
			_comic = new Window();
			_comic.RelativeBounds = new Rectangle( 0, 0, 600, 200 );						
			_comic.StretchBackground = false;
			Add( _comic );
			
			//Add Event Handlers
			listener.Elapsed += new System.Timers.ElapsedEventHandler(listener_Elapsed);			
			listener.Start();

			this.Visible = false;
			return;
		}
		#endregion Constructors

		#region EventHandlers
		private void listener_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{			
			listener.Stop();			
						
			if (SingletonComicsUpdater.Instance.ShowPreview)
			{
				if (!isVisible) 
				{
					ShowPopup();						
				}
				isVisible = true;										
			}
			else
			{
				HidePopup();
				isVisible = false;				
			}

			//This will start the process over again :)
			listener.Start();
		}

		#endregion EventHandlers

		#region OverrideFunctions
		#endregion OverrideFunctions

		public void HidePopup()
		{
			this.Visible = false;

			try 
			{
				System.IO.File.Delete( tempFilename );			
			}
			catch (Exception e) {}							
		}

		public void ShowPopup()
		{			
			this.Visible = true;			
						
			ComicInfo ci;
			foreach( ComicInfo _ci in SingletonComicsUpdater.Instance.AvailableComics ) 
			{
				if (SingletonComicsUpdater.Instance.ComicToPreview == _ci.DisplayName) 
				{
					DateTime dt = DateTime.Now;
					String ImageFilename = "";	
					String URIImagePath = "";
					ci = _ci;
					// Create a webclient
					System.Net.WebClient webClient = new System.Net.WebClient();

					// Create the image filename from the format in the XML file
					char[] delims = {'$'};
					// Parse the String format and form the path with the year
					String[] ImagePathTokens = ci.ImagePath.Split(delims,100);
					String ImagePath = "";	
					foreach( String s in ImagePathTokens ) 
					{
						int dtny = DateTime.Now.Year;				
						// Year
						if ( s.Equals("yyyy") )					
							ImagePath += dtny.ToString();
						else
							ImagePath += s;
					}				

					// Parse the String format and form the filename with the date
					String[] FilenameTokens = ci.ImageFilename.Split(delims,100);									
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

					// Comics.com support
					if (_ci.Website == "Comics.com") 
					{						
						try 
						{							
							HttpWebRequest webreq;
							HttpWebResponse	webres;							
							string trimmedPath =  ci.ImagePath.Remove(ci.ImagePath.IndexOf("images/"),7);							
							System.Uri uri = new System.Uri(trimmedPath+ci.FolderName+"-"+DateTime.Now.Year.ToString() + dt.ToString("MM",DateTimeFormatInfo.InvariantInfo) + dt.ToString("dd",DateTimeFormatInfo.InvariantInfo) + ".html");																					
							webreq = (HttpWebRequest)WebRequest.Create(uri);
							webres = (HttpWebResponse)webreq.GetResponse();										
							Stream resStream = webres.GetResponseStream();
							string response = new StreamReader( resStream ).ReadToEnd();																														
							response = response.Substring(response.IndexOf(ci.FolderName+"/archive/images/"));													
							string[] responseArray = response.Split('/');																													
							char[] dms = new  Char[] {'"','&'};
							string fullImageFilename = responseArray[3].Split(dms)[0];																			
							ImageFilename = fullImageFilename.Split('.')[0];
							ci.ImageSuffix = fullImageFilename.Split('.')[1];
						}
						catch (Exception e) {SnapStream.Logging.WriteLog(e.StackTrace + "\n" + e.Message);}				
					}
					// End Comics.com support

					ImageFilename += "." + ci.ImageSuffix;	

					// Add the website path before the image filename
					URIImagePath = ImagePath + ImageFilename;					

					// Download the image								
					try 
					{ 
						byte[] dataBuffer = webClient.DownloadData( URIImagePath );			
						tempFilename = System.IO.Path.GetTempFileName();					
						webClient.DownloadFile( URIImagePath, tempFilename );			
					}
					catch (Exception)
					{	
						System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
						System.IO.FileInfo fi = new System.IO.FileInfo( a.Location );	
						tempFilename = fi.DirectoryName + "//Images//nopreview.png";
					}																									
					
					_comic.Background = tempFilename;					
					break;					
				}
				else
				{
					System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
					System.IO.FileInfo fi = new System.IO.FileInfo( a.Location );			
					_comic.Background = fi.DirectoryName + "//Images//downloading.png";
				}
			}						
		}

		protected override void DisposeCore() 
		{
			base.DisposeCore();	
			this.Visible = false;
			SingletonComicsUpdater.Instance.ShowPreview = false;	
			SingletonComicsUpdater.Instance.Dispose();
			this.Dispose();
			return;
		}

	}
}