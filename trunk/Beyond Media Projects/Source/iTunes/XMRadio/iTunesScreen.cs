using System;
using System.Net;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;
using System.IO;

using SnapStream.ViewScape.Services;
using SnapStream.ViewScape.Widgets;
using SnapStream.Commands;

namespace SnapStream.Plugins.iTunes
{
	// iTunes Channel List
	internal class iTunesListItem : BaseListItem 
	{
		#region Private Members		
		private TextWindow	_artist;				
		private TextWindow	_song;
		private TextWindow	_album;
		#endregion Private Members

		#region Properties				
		// Window holds artist		
		public TextWindow Artist 
		{
			get 
			{
				return _artist;
			}
		}
		// Window holds song		
		public TextWindow Song 
		{
			get 
			{
				return _song;
			}
		}
		// Window holds album		
		public TextWindow Album
		{
			get 
			{
				return _album;
			}
		}
		#endregion Properties

		#region Constructors		
		public iTunesListItem( string channel ) 
		{			
			_artist = new TextWindow();			
			_artist.RelativeBounds = new Rectangle(70,10,190,20);
			_artist.HorizontalAlign = HAlignment.Left;
			Add( _artist );

			_song = new TextWindow();			
			_song.RelativeBounds = new Rectangle(270,10,190,20);
			_song.HorizontalAlign = HAlignment.Left;
			Add( _song );

			_album = new TextWindow();			
			_album.RelativeBounds = new Rectangle(470,10,190,20);
			_album.HorizontalAlign = HAlignment.Left;
			Add( _album );
			
			return;
		}
		#endregion Constructors		
	}

	// Screen that shows the iTunes Channels
	public class iTunesScreen : ScreenBase
	{		
		#region Private Members										
		private TextWindow					_header;
		private TextWindow					_volume;		
		private VariableItemList			_list;			
		private TextButton					_nextButton;		
		private TextButton					_shuffleButton;	
		private TextButton					_searchButton;
		private TextButton					_playallButton;
		private TextEntry					_searchEntry;
		private string						url;
		private string						password;		
		private WMPLib.WindowsMediaPlayer	wmp;
		private System.Uri					uri;
		private HttpWebRequest				webreq;
		private HttpWebResponse				webres;			
		private CookieContainer				cookies;
		private Stream						resStream;
		private string						response;
		private bool loginerror;					
		private string						shuffleMode;
		private Timer						updateTimer;
		TextWindow misc1, misc2, misc3;
		#endregion Private Members

		#region Properties		
		public TextWindow Header 
		{
			get 
			{
				return _header;
			}
		}

		public VariableItemList List 
		{
			get 
			{
				return _list;
			}
		}
		
		public TextWindow Volume 
		{
			get 
			{
				return _volume;
			}
		}
		public TextButton NextButton
		{
			get 
			{
				return _nextButton;
			}
		}	
		public TextButton ShuffleButton
		{
			get 
			{
				return _shuffleButton;
			}
		}	
		public TextButton PlayAllButton
		{
			get 
			{
				return _playallButton;
			}
		}	
		public TextButton SearchButton
		{
			get 
			{
				return _searchButton;
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

		#region Constructors
		/// <summary>
		/// Creates the ComicsScreen
		/// </summary>
		public iTunesScreen() 
		{
			SnapStream.Logging.WriteLog("iTunes Plugin Started");

			System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
			System.IO.FileInfo fi = new System.IO.FileInfo( a.Location );									

			// Text Objects			
			_header = new TextWindow();			
			Add( _header );

			_volume = new TextWindow();
			Add( _volume );

			// Create the list viewer
			_list = new VariableItemList();			
			//Add( _list );
			
			// Create control button			
			_nextButton = new TextButton();
			_nextButton.Click +=new EventHandler(_nextButton_Click);
			Add(_nextButton);
			
			// Create control button			
			_shuffleButton = new TextButton();		
			_shuffleButton.Click +=new EventHandler(_shuffleButton_Click);
			Add(_shuffleButton);

			// Create control button			
			_searchButton = new TextButton();		
			_searchButton.Click +=new EventHandler(_searchButton_Click);
			Add(_searchButton);

			// Search
			_searchEntry = new TextEntry();					
			Add( _searchEntry );					
			_searchEntry.Accept += new EventHandler(SearchEntry_Accept);
			_searchEntry.Cancel += new EventHandler(SearchEntry_Cancel);			

			// Create control button			
			_playallButton = new TextButton();		
			_playallButton.Click +=new EventHandler(_playallButton_Click);
			Add(_playallButton);

			_list.ItemActivated += new ItemActivatedEventHandler(List_ItemActivated);
			_list.Visible = true;
			_list.Focus();		
	
			misc1 = new TextWindow();
			misc1.RelativeBounds = new Rectangle(210,170,200,40);
			misc1.FontSize = 20;
			misc1.Text = "(N or >> button on Firefly)";
			Add(misc1);

			misc2 = new TextWindow();
			misc2.RelativeBounds = new Rectangle(210,210,300,40);
			misc2.FontSize = 20;
			misc2.Text = "(S or A button on Firefly)";
			Add(misc2);

			misc3 = new TextWindow();
			misc3.RelativeBounds = new Rectangle(0,240,400,40);
			misc3.FontSize = 20;
			misc3.Text = "+/- controls volume on keyboard and Firefly remote";
			Add(misc3);

			// Create the windows media player object
			wmp = new WMPLib.WindowsMediaPlayerClass();		

			// Create the timer
			updateTimer = new Timer();
			updateTimer.Interval = 1000;
			updateTimer.Tick += new EventHandler(updateTimer_Tick);									
						
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

			// Control WMP Volume
			if( e.KeyCode == System.Windows.Forms.Keys.Add ) 
			{
				wmp.settings.volume+=1;	
				_volume.Visible = true;
				_volume.Text = wmp.settings.volume.ToString();
				_volume.FadeOut();				
			}
			if( e.KeyCode == System.Windows.Forms.Keys.Subtract ) 
			{
				wmp.settings.volume-=1;
				_volume.Visible = true;
				_volume.Text = wmp.settings.volume.ToString();
				_volume.FadeOut();			
			}

			if( e.KeyCode == System.Windows.Forms.Keys.Escape ) 
			{
				SingletonSoundCache.Instance.PlaySound( DefaultSoundList.Cancel );
				RaiseExitEvent();
				e.Handled = true;
				return;
			}

			// Next
			if( e.KeyCode == System.Windows.Forms.Keys.N ) 
			{
				wmp.controls.next();					
			}

			// Shuffle Toggle
			if( e.KeyCode == System.Windows.Forms.Keys.S ) 
			{
				if (wmp.settings.getMode("shuffle")) 
				{
					wmp.settings.setMode("shuffle",false);
					shuffleMode = "";
				}
				else 
				{
					wmp.settings.setMode("shuffle",true);
					shuffleMode = "S";
				}
			}

			return;
		}

		private void updateTimer_Tick( object sender, EventArgs e ) 
		{			
			_header.Text = "iTunes: " + wmp.status.ToString() + ": " + wmp.currentMedia.durationString.ToString() + " " + shuffleMode;
		}

		// On entering the screen
		public override void Activate() 
		{				
			base.Activate();			
			
			// get user information
			SingletonConfig.Instance.GetPropertyAsString("iTunesURL",out url);			
			SingletonConfig.Instance.GetPropertyAsString("iTunesPassword",out password);									
				
			loginerror = false;
			Login();
			
			_volume.Text = wmp.settings.volume.ToString();
			_volume.FadeOut();
			
			updateTimer.Start();		

			return;
		}

		public override void Deactivate()
		{
			base.Deactivate ();			
			if (wmp.playState == WMPLib.WMPPlayState.wmppsPlaying)
				wmp.controls.stop();									
		}

		protected override void DisposeCore() 
		{
			base.DisposeCore();
			updateTimer.Dispose();
			updateTimer = null;
			return;
		}
		#endregion Window Overrides

		#region Private Methods							
		// Show slideshow when item is entered
		private void List_ItemActivated( object sender, ItemActivatedArgs args ) 
		{							
			string artist = ((iTunesListItem)_list.SelectedItem).Artist.Text;
			string song = ((iTunesListItem)_list.SelectedItem).Song.Text;
			//iTunesPlay(channel,artist,song);			
			return;
		}

		private void Login() 
		{
			_header.Text = "iTunes: Logging In...";
			
			// try to log in						
			uri = new System.Uri(url+"/mytunesrss/login?username=default&password="+password);
			SnapStream.Logging.WriteLog("iTunes: Trying "+url+"/mytunesrss/login?username=default&password="+password); 
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
				//SnapStream.Logging.WriteLog(response);
				if (response.IndexOf("logout") > 0)
					_header.Text = "iTunes: Logged In!";
			} 
			catch (System.Net.WebException e) 
			{
				SnapStream.Logging.WriteLog("iTunes: " + e.ToString());	
				_header.Text = "iTunes: Login Error";	
				loginerror = true;
			}		
		}

		/*
		private void iTunesPlay( string channel, string artist, string song ) 
		{
			if (channel == "000") 
			{
				wmp.URL = "http://player.xmradio.com/hotstream/metafile.jsp?op=xm_radio&ch=4&speed=high&s=1165420632333&e=1165420627233&h=f4fdeb93f3cd01f8575265c54b1c04c5&partner=XMROUS";
				wmp.controls.play();
				return;
			}
			// Play a channel			
			uri = new System.Uri("http://player.xmradio.com/player/2ft/playMedia.jsp?ch=" + channel + "&speed=" + speed);
			webreq = (HttpWebRequest)WebRequest.Create(uri);
			webreq.CookieContainer = xmCookies;
			// Get the response
			try 
			{
				webres = (HttpWebResponse)webreq.GetResponse();	
				resStream = webres.GetResponseStream();
				response = new StreamReader( resStream ).ReadToEnd();					
				int urlpos = response.IndexOf("SRC=\"");				
				if (urlpos > -1)
				{
					string[] tmpParse = response.Substring(urlpos).Split('"');					
					string channelURL = tmpParse[1];					
					wmp.URL = channelURL;										
					wmp.controls.play();
				}
			} 
			catch (System.Net.WebException e) 
			{
				SnapStream.Logging.WriteLog("XMRadio: " + e.ToString());
			}			
			_header.Text = "XM Radio: Playing: " + artist + " - " + song;			
		}		
		*/
		#endregion Private Methods

		private void _shuffleButton_Click(object sender, EventArgs e)
		{
			if (wmp.settings.getMode("shuffle")) 
			{
				wmp.settings.setMode("shuffle",false);
				shuffleMode = "";
			}
			else 
			{
				wmp.settings.setMode("shuffle",true);
				shuffleMode = "S";
			}
		}

		private void _nextButton_Click(object sender, EventArgs e)
		{
			wmp.controls.next();
		}

		private void _searchButton_Click(object sender, EventArgs e)
		{
			HideAll();
			_searchEntry.Visible = true;
			_searchEntry.Focus();
		}

		private void SearchEntry_Accept(object sender, EventArgs e) 
		{			
			ShowAll();
			_searchEntry.Visible = false;	
			//browseTrack?searchTerm=cynthia
			uri = new System.Uri(url+"/mytunesrss/browseTrack?searchTerm="+_searchEntry.Text);
			// Create a webclient			
			webreq = (HttpWebRequest)WebRequest.Create(uri);
			//cookies = new CookieContainer();
			webreq.CookieContainer = cookies;
			try 
			{
				webres = (HttpWebResponse)webreq.GetResponse();										
				resStream = webres.GetResponseStream();	
				response = new StreamReader( resStream ).ReadToEnd();				
				//SnapStream.Logging.WriteLog(response);
				string m3upath = response.Substring(response.IndexOf("createM3U"));
				m3upath = url + "/mytunesrss/" + m3upath.Split('\"')[0];
				SnapStream.Logging.WriteLog(m3upath);				
				wmp.URL = m3upath;
				wmp.controls.play();
				_header.Text = "iTunes: " + wmp.status.ToString() + ": " + wmp.currentMedia.durationString.ToString() + " " + shuffleMode;
			} 
			catch (System.Net.WebException e2) 
			{
				SnapStream.Logging.WriteLog("iTunes: " + e2.ToString());	
				_header.Text = "iTunes: Login Error";	
				loginerror = true;
			}		
			return;
		}

		private void SearchEntry_Cancel(object sender, EventArgs e) 
		{
			ShowAll();
			_searchEntry.Visible = false;
			return;
		}		

		private void _playallButton_Click(object sender, EventArgs e)
		{									
			uri = new System.Uri(url+"/mytunesrss/login?username=default&password="+password);
			// Create a webclient			
			webreq = (HttpWebRequest)WebRequest.Create(uri);
			//cookies = new CookieContainer();
			webreq.CookieContainer = cookies;
			try 
			{
				webres = (HttpWebResponse)webreq.GetResponse();										
				resStream = webres.GetResponseStream();	
				response = new StreamReader( resStream ).ReadToEnd();				
				//SnapStream.Logging.WriteLog(response);
				string m3upath = response.Substring(response.IndexOf("createM3U"));
				m3upath = url + "/mytunesrss/" + m3upath.Split('\"')[0];
				SnapStream.Logging.WriteLog(m3upath);
				//SnapStream.Logging.WriteLog(response);
				wmp.URL = m3upath;
				wmp.controls.play();
				_header.Text = "iTunes: " + wmp.status.ToString() + ": " + wmp.currentMedia.durationString.ToString() + " " + shuffleMode;
			} 
			catch (System.Net.WebException e2) 
			{
				SnapStream.Logging.WriteLog("iTunes: " + e2.ToString());	
				_header.Text = "iTunes: Login Error";	
				loginerror = true;
			}		
			return;
		}

		private void HideAll() 
		{
			_nextButton.Visible = false;
			_shuffleButton.Visible = false;
			_playallButton.Visible = false;
			_searchButton.Visible = false;			
			misc1.Visible = false;
			misc2.Visible = false;
			misc3.Visible = false;
		}

		private void ShowAll()
		{
			_nextButton.Visible = true;
			_shuffleButton.Visible = true;
			_playallButton.Visible = true;
			_searchButton.Visible = true;
			misc1.Visible = true;
			misc2.Visible = true;
			misc3.Visible = true;
		}

	}
}