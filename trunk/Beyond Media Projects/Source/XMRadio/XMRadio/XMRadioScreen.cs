/* Notes
 * BTVSkin, BMTivo
 * Get Channel Info: (station type)
http://player.xmradio.com/player/2ft/channel_data.jsp?all_channels=true&remote=true
*/

using System;
using System.Net;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

using SnapStream.ViewScape.Services;
using SnapStream.ViewScape.Widgets;
using SnapStream.Commands;

namespace SnapStream.Plugins.XMRadio
{	
	// Favorite Logos
	internal class XMFavItem : BaseListItem 
	{		
		private Window		_logo;
		// Window holds logo
		public Window Logo
		{
			get 
			{
				return _logo;
			}
		}		
		public XMFavItem(int number) 
		{
			System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
			System.IO.FileInfo fi = new System.IO.FileInfo( a.Location );								

			_logo = new Window();			
			_logo.RelativeBounds = new Rectangle(5,5,90,40);			
			Add( _logo );
		}

	}

	// XM Channel List
	internal class XMListItem : BaseListItem 
	{			
		#region Private Members
		private TextWindow	_channel;				
		private TextWindow	_artist;				
		private TextWindow	_song;
		private TextWindow	_album;		
		private Window		_logo;
		#endregion Private Members

		#region Properties		
		// Window holds channel number		
		public TextWindow Channel
		{
			get 
			{
				return _channel;
			}
		}
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
		// Window holds logo
		public Window Logo
		{
			get 
			{
				return _logo;
			}
		}
		#endregion Properties

		#region Constructors		
		public XMListItem( string channel ) 
		{
			System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
			System.IO.FileInfo fi = new System.IO.FileInfo( a.Location );

			_channel = new TextWindow();
			_channel.Text = channel;	
			_channel.RelativeBounds = new Rectangle(25,10,50,20);
			_channel.HorizontalAlign = HAlignment.Left;
			Add( _channel );			

			_logo = new Window();
			_logo.Background = fi.DirectoryName + "\\Images\\" + channel + ".png";
			_logo.RelativeBounds = new Rectangle(25,30,80,40);
			Add( _logo );

			_artist = new TextWindow();			
			_artist.RelativeBounds = new Rectangle(110,15,370,20);
			_artist.HorizontalAlign = HAlignment.Left;
			Add( _artist );

			_song = new TextWindow();			
			_song.RelativeBounds = new Rectangle(110,35,370,20);
			_song.HorizontalAlign = HAlignment.Left;
			Add( _song );

			_album = new TextWindow();			
			_album.RelativeBounds = new Rectangle(110,55,370,20);
			_album.HorizontalAlign = HAlignment.Left;
			Add( _album );			
			
			return;
		}
		#endregion Constructors		
	}

	// Screen that shows the XM Channels
	public class XMRadioScreen : ScreenBase
	{		
		#region Private Members										
		private TextWindow					_header;
		private TextWindow					_header2;
		private TextWindow					_volume;
		private Window						_logo;
		private Window						_logo2;
		private VariableItemList			_xmChannels;
		private VariableItemList			_xmGenres;
		private VariableItemList			_xmFavs;
		private string[]					favorites;
		private string						email;
		private string						password;
		private string						speed;
		private WMPLib.WindowsMediaPlayer	wmp;
		private System.Uri					uri;
		private HttpWebRequest				webreq;
		private HttpWebResponse				webres;			
		private CookieContainer				xmCookies;
		private Stream						resStream;
		private string						response;
		private bool						loggedin;
		private ArrayList					padDataList;
		private XMListItem[]				xmPadDataList;
		private Timer	updateTimer;		
		private int							currentChannel;				
		private System.IO.FileInfo			fi;			
		private bool						wasUIsoundsOn;

		//private delegate void	UpdateTask();
		#endregion Private Members

		#region Properties		
		public TextWindow Header 
		{
			get 
			{
				return _header;
			}
		}

		public TextWindow Header2 
		{
			get 
			{
				return _header2;
			}
		}

		public VariableItemList XMChannels 
		{
			get 
			{
				return _xmChannels;
			}
		}

		public VariableItemList XMGenres 
		{
			get 
			{
				return _xmGenres;
			}
		}

		public VariableItemList XMFavs 
		{
			get 
			{
				return _xmFavs;
			}
		}
		
		public TextWindow Volume 
		{
			get 
			{
				return _volume;
			}
		}
		
		public Window Logo
		{
			get 
			{
				return _logo;
			}
		}	
	
		public Window Logo2
		{
			get 
			{
				return _logo2;
			}
		}	
		#endregion Properties

		#region Constructors
		/// <summary>
		/// Creates the ComicsScreen
		/// </summary>
		public XMRadioScreen() 
		{
			SnapStream.Logging.WriteLog("XMRadio Plugin Started");

			System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
			fi = new System.IO.FileInfo( a.Location );									

			// XM Logo
			_logo = new Window();
			_logo.Background = fi.DirectoryName + "\\Images\\" + "xmlogo.png";
			Add(_logo);	
			
			// Channel Logo
			Window _logobg = new Window();			
			_logobg.RelativeBounds = new Rectangle(44,30,60,30);
			_logobg.Background = fi.DirectoryName + "\\Images\\" + "blackpixel.png";
			Add( _logobg );

			_logo2 = new TextWindow();			
			_logo2.Background = fi.DirectoryName + "\\Images\\" + "blackpixel.png";
			Add(_logo2);		

			// Now Playing header
			_header = new TextWindow();				
			_header.Color = System.Drawing.Color.FromArgb(255,224,16);
			_header.Background = fi.DirectoryName + "\\Images\\" + "blackpixel.png";
			Add( _header );
			
			// Now Playing header2
			_header2 = new TextWindow();				
			_header2.Color = System.Drawing.Color.FromArgb(255,224,16);
			_header2.Background = fi.DirectoryName + "\\Images\\" + "blackpixel.png";
			Add( _header2 );

			// Volume control
			_volume = new TextWindow();
			Add( _volume );				

			// Create the genre listing
			_xmGenres = new VariableItemList();
			_xmGenres.ItemActivated +=new ItemActivatedEventHandler(_xmGenres_ItemActivated);
			_xmGenres.ScrollBar.Visible = false;	
			_xmGenres.GridPadding = 0;			
			Add( _xmGenres );

			// Create the channel listing
			_xmChannels = new VariableItemList();						
			_xmChannels.ItemActivated += new ItemActivatedEventHandler(XMChannels_ItemActivated);						
			Add( _xmChannels );

			// Create the favorites listing
			_xmFavs = new VariableItemList();
			_xmFavs.ItemActivated +=new ItemActivatedEventHandler(_xmFavs_ItemActivated);
			_xmFavs.ScrollBar.Visible = false;	
			Add( _xmFavs );

			// Create the windows media player object
			wmp = new WMPLib.WindowsMediaPlayerClass();		
			
			// Create the timer
			updateTimer = new Timer();
			updateTimer.Interval = 5000;
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

			if( e.KeyCode == System.Windows.Forms.Keys.Left )
			{
				if (_xmChannels.IsFocused())
					_xmGenres.Focus();	
				else if (_xmFavs.IsFocused())
					_xmChannels.Focus();
			}

			if( e.KeyCode == System.Windows.Forms.Keys.Right )
			{
				if (_xmChannels.IsFocused())
					_xmFavs.Focus();	
				else if (_xmGenres.IsFocused())
					_xmChannels.Focus();				
			}

			if( e.KeyCode == System.Windows.Forms.Keys.D1 )
			{
				// Store in the BM variable
				SingletonConfig.Instance.SetProperty("XMFav0",_xmChannels.SelectedIndex.ToString());
				favorites[0] = _xmChannels.SelectedIndex.ToString();
				createFavsList();
			}		
	
			if( e.KeyCode == System.Windows.Forms.Keys.D2 )
			{
				// Store in the BM variable
				SingletonConfig.Instance.SetProperty("XMFav1",_xmChannels.SelectedIndex.ToString());
				favorites[1] = _xmChannels.SelectedIndex.ToString();
				createFavsList();
			}		

			if( e.KeyCode == System.Windows.Forms.Keys.D3 )
			{
				// Store in the BM variable
				SingletonConfig.Instance.SetProperty("XMFav2",_xmChannels.SelectedIndex.ToString());
				favorites[2] = _xmChannels.SelectedIndex.ToString();
				createFavsList();
			}		

			if( e.KeyCode == System.Windows.Forms.Keys.D4 )
			{
				// Store in the BM variable
				SingletonConfig.Instance.SetProperty("XMFav3",_xmChannels.SelectedIndex.ToString());
				favorites[3] = _xmChannels.SelectedIndex.ToString();
				createFavsList();
			}		

			if( e.KeyCode == System.Windows.Forms.Keys.D5 )
			{
				// Store in the BM variable
				SingletonConfig.Instance.SetProperty("XMFav4",_xmChannels.SelectedIndex.ToString());
				favorites[4] = _xmChannels.SelectedIndex.ToString();
				createFavsList();
			}		

			if( e.KeyCode == System.Windows.Forms.Keys.D6 )
			{
				// Store in the BM variable
				SingletonConfig.Instance.SetProperty("XMFav5",_xmChannels.SelectedIndex.ToString());
				favorites[5] = _xmChannels.SelectedIndex.ToString();
				createFavsList();
			}		

			if( e.KeyCode == System.Windows.Forms.Keys.D7 )
			{
				// Store in the BM variable
				SingletonConfig.Instance.SetProperty("XMFav6",_xmChannels.SelectedIndex.ToString());
				favorites[6] = _xmChannels.SelectedIndex.ToString();
				createFavsList();
			}		

			if( e.KeyCode == System.Windows.Forms.Keys.S )
			{
				// Stop XM Radio playback
				wmp.controls.stop();
			}					

			return;
		}

		// On entering the screen
		public override void Activate() 
		{				
			base.Activate();						

			// Mute UI sounds			
			if (SingletonSoundCache.Instance.MuteSoundEffects == true) 
			{
				wasUIsoundsOn = false;
			}
			else 
			{
				wasUIsoundsOn = true;
				SingletonSoundCache.Instance.MuteSoundEffects = true;
			}
			
			// get user information
			SingletonConfig.Instance.GetPropertyAsString("XMEmail",out email);			
			SingletonConfig.Instance.GetPropertyAsString("XMPassword",out password);						
			SingletonConfig.Instance.GetPropertyAsString("XMRadio.Speed",out speed);							
				
			// Log into XMRO
			loggedin = false;
			loggedin = Login();			
			if (loggedin) 
			{				
				// Prepare the Pad Data List			
				PreparePadDataArray();				
				updatePadDataDisplay();

				// Create genres
				createGenreList();

				// get favorites and create the list
				favorites = new string[7];
				for (int i = 0; i < favorites.Length; i++)
					SingletonConfig.Instance.GetPropertyAsString("XMFav"+i.ToString(),out favorites[i]);
				createFavsList();

				// Start the pad data updater
				updateTimer.Start();
				currentChannel = 0;			

				// Volume control
				_volume.Text = wmp.settings.volume.ToString();
				_volume.FadeOut();
			}
			else 
			{
				// Clear just in case a previous login worked
				_xmChannels.Clear();
				_xmFavs.Clear();
				_xmGenres.Clear();

				// Just show the preview
				_header.Text = "Not Logged In";
				XMListItem xmchannelitem;
				xmchannelitem = new XMListItem("000");
				xmchannelitem.RelativeBounds = new Rectangle(0,0,380,88);
				xmchannelitem.Artist.Text = "No Subscription";
				xmchannelitem.Song.Text = "No Subscription";
				_xmChannels.AddItem(xmchannelitem);			
			}				
		
			// Give focus to the Channel List
			_xmChannels.Focus();			
		}

		public override void Deactivate()
		{
			base.Deactivate ();
	
			// Reenable sound effects			
			if (wasUIsoundsOn) 			
				SingletonSoundCache.Instance.MuteSoundEffects = false;			

			// Stop WMP
			//if (wmp.playState == WMPLib.WMPPlayState.wmppsPlaying)
			//	wmp.controls.stop();

			// Logout		
			if (loggedin) 
			{
				uri = new System.Uri("http://xmro.xmradio.com/xstream/end_session.jsp");
				webreq = (HttpWebRequest)WebRequest.Create(uri);
				webreq.CookieContainer = xmCookies;
				// Get the response
				try 
				{
					webres = (HttpWebResponse)webreq.GetResponse();										
					resStream = webres.GetResponseStream();
					response = new StreamReader( resStream ).ReadToEnd();										
				} 
				catch (System.Net.WebException e) 
				{
					SnapStream.Logging.WriteLog("XMRadio: " + e.ToString());				
				}				

				// Stop the pad data timer
				updateTimer.Stop();
			}
		
		}

		protected override void DisposeCore() 
		{
			base.DisposeCore();	

			// Reenable sound effects if they close the window here
			if (wasUIsoundsOn) 			
				SingletonSoundCache.Instance.MuteSoundEffects = false;	

			updateTimer.Dispose();
			updateTimer = null;
			return;
		}
		#endregion Window Overrides

		#region Private Methods							

		// Create Genre list
		private void createGenreList() 
		{
			TextListItem genre;			
			genre = new TextListItem("Decades", "Decades");
			genre.RelativeBounds = new Rectangle(10,0,140,30);						
			genre.TextWindow.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.FontSize = 20;
			_xmGenres.AddItem(genre);			
			genre = new TextListItem("Country", "Country");
			genre.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.FontSize = 20;
			_xmGenres.AddItem(genre);
			genre = new TextListItem("Pop & Hits", "PopHits");
			genre.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.FontSize = 20;
			_xmGenres.AddItem(genre);
			genre = new TextListItem("Christian", "Christian");
			genre.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.FontSize = 20;
			_xmGenres.AddItem(genre);			
			genre = new TextListItem("Rock", "Rock");
			genre.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.FontSize = 20;
			_xmGenres.AddItem(genre);
			genre = new TextListItem("Hip Hop & Urban", "HipHop");
			genre.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.FontSize = 20;
			_xmGenres.AddItem(genre);
			genre = new TextListItem("Jazz & Blues", "Jazz");
			genre.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.FontSize = 20;
			_xmGenres.AddItem(genre);
			genre = new TextListItem("Classical", "Classical");
			genre.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.FontSize = 20;
			_xmGenres.AddItem(genre);
			genre = new TextListItem("Dance", "Dance");
			genre.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.FontSize = 20;
			_xmGenres.AddItem(genre);
			genre = new TextListItem("Latin and World", "LatinWorld");
			genre.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.FontSize = 20;
			_xmGenres.AddItem(genre);
			genre = new TextListItem("Sirius Best Of", "SiriusBestOf");
			genre.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.FontSize = 20;
			_xmGenres.AddItem(genre);
			genre = new TextListItem("Kids", "Kids");
			genre.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.FontSize = 20;
			_xmGenres.AddItem(genre);
			genre = new TextListItem("News", "News");
			genre.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.FontSize = 20;
			_xmGenres.AddItem(genre);
			genre = new TextListItem("Sports", "Sports");
			genre.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.FontSize = 20;
			_xmGenres.AddItem(genre);
			genre = new TextListItem("Comedy", "Comedy");
			genre.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.FontSize = 20;
			_xmGenres.AddItem(genre);	
			genre = new TextListItem("Talk", "Talk");
			genre.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.RelativeBounds = new Rectangle(10,0,140,30);
			genre.TextWindow.FontSize = 20;
			_xmGenres.AddItem(genre);				
		}

		// Create the favorite list
		private void createFavsList() 
		{
			_xmFavs.Clear();
			XMFavItem fav;
			Regex reNum = new Regex(@"^\d+$"); // For number matching
			for (int i = 0; i < favorites.Length; i++) 
			{
				fav = new XMFavItem(i+1);						
				fav.RelativeBounds = new Rectangle(0,0,100,50);
				if (reNum.Match(favorites[i]).Success) 
				{					
					string channel = ((XMListItem)_xmChannels.Items.ToArray()[int.Parse(favorites[i])]).Channel.Text;
					fav.Logo.Background = fi.DirectoryName + "\\Images\\" + channel + ".png";				
				}
				_xmFavs.AddItem(fav);			
			}
		}
		
		// Change channel
		private void XMChannels_ItemActivated( object sender, ItemActivatedArgs args ) 
		{				
			string channel = ((XMListItem)_xmChannels.SelectedItem).Channel.Text;			
			string artist = ((XMListItem)_xmChannels.SelectedItem).Artist.Text;			
			string song = ((XMListItem)_xmChannels.SelectedItem).Song.Text;
			string album = ((XMListItem)_xmChannels.SelectedItem).Album.Text;
			XMplay(channel,artist,song,album);
			currentChannel = int.Parse(channel);
			return;
		}

		// Select new Genre and go to the first index in the list
		private void _xmGenres_ItemActivated(object sender, ItemActivatedArgs args)
		{			
			switch (args.ActivatedIndex) 
			{
				case 0: // Decades
					_xmChannels.SelectedIndex = 0;
					break;
				case 1: // Country
					_xmChannels.SelectedIndex = 6;
					break;
				case 2: // Pop
					_xmChannels.SelectedIndex = 15;
					break;
				case 3: // Christian
					_xmChannels.SelectedIndex = 24;
					break;
				case 4: // Rock
					_xmChannels.SelectedIndex = 29;
					break;
				case 5: // Hip Hop
					_xmChannels.SelectedIndex = 49;
					break;
				case 6: // Jazz
					_xmChannels.SelectedIndex = 56;
					break;
				case 7: // Classical
					_xmChannels.SelectedIndex = 63;
					break;
				case 8: // Dance
					_xmChannels.SelectedIndex = 66;
					break;
				case 9: // Latin and World
					_xmChannels.SelectedIndex = 71;
					break;
				case 10: // Best of Sirius
					_xmChannels.SelectedIndex = 79;
					break;
				case 11: // Kids
					_xmChannels.SelectedIndex = 82;
					break;
				case 12: // News
					_xmChannels.SelectedIndex = 87;
					break;
				case 13: // Sports
					_xmChannels.SelectedIndex = 97;
					break;
				case 14: // Comedy
					_xmChannels.SelectedIndex = 100;
					break;
				case 15: // Talk
					_xmChannels.SelectedIndex = 104;
					break;
				default:
					break;
			}
		}

		// Tune to favorite
		private void _xmFavs_ItemActivated(object sender, ItemActivatedArgs args)
		{			
			_xmChannels.SelectedIndex = int.Parse(favorites[_xmFavs.SelectedIndex]);
			string channel = ((XMListItem)_xmChannels.SelectedItem).Channel.Text;			
			string artist = ((XMListItem)_xmChannels.SelectedItem).Artist.Text;			
			string song = ((XMListItem)_xmChannels.SelectedItem).Song.Text;
			string album = ((XMListItem)_xmChannels.SelectedItem).Album.Text;
			XMplay(channel,artist,song,album);
			currentChannel = int.Parse(channel);
			return;			
		}

		// Log into XMRO
		private bool Login() 
		{
			_header.Text = "Logging In...";
			
			// try to log in			
			uri = new System.Uri("http://xmro.xmradio.com/xstream/login_servlet.jsp?confcode=&user_id=" + email + "&pword=" + password + "&submit.x=4&submit.y=11&userForward=default");
			// Create a webclient			
			webreq = (HttpWebRequest)WebRequest.Create(uri);
			xmCookies = new CookieContainer();
			webreq.CookieContainer = xmCookies;
			// Get the response
			try 
			{
				webres = (HttpWebResponse)webreq.GetResponse();										
				resStream = webres.GetResponseStream();		
				response = new StreamReader( resStream ).ReadToEnd();
				// Check if the response shows logged in
				if (response.IndexOf("MANAGE YOUR ACCOUNT") == -1) return false;				
				_header.Text = "Logged In as " + email;
				return true;
			} 
			catch (System.Net.WebException e) 
			{
				SnapStream.Logging.WriteLog("XMRadio: " + e.ToString());	
				_header.Text = "Login Error";	
				return false;
			}		
		}

		// Play a channel
		private void XMplay( string channel ) 
		{
			XMplay(channel, "", "", "");
		}

		// Play a channel
		private void XMplay( string channel, string artist, string song, string album ) 
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
				_header.Text = "Playing: " + song;			
				_header2.Text = "   " + artist + " (" + album + ")";
				_logo2.Background = fi.DirectoryName + "\\Images\\" + channel + ".png";
			} 
			catch (System.Net.WebException e) 
			{
				SnapStream.Logging.WriteLog("XMRadio: " + e.ToString());
			}						
		}

		// Get the size of the array for the Pad Data
		private void PreparePadDataArray() 
		{			
			GetPadData();
			xmPadDataList = new XMListItem[padDataList.Count];
			_xmChannels.Clear();
			// Create reference to XMListItems
			int i = 0;
			foreach (PadData pd in padDataList) 
			{
				xmPadDataList[i] = new XMListItem(pd.Channel.ToString());				
				xmPadDataList[i].RelativeBounds = new Rectangle(0,0,380,88);
				_xmChannels.AddItem(xmPadDataList[i]);
				i++;
			}

		}

		// Download and display the Pad Data
		private void GetPadData()
		{
			// Get pad Data
			uri = new System.Uri("http://player.xmradio.com/padData/pad_data_servlet.jsp?all_channels=true&remote=true");
			webreq = (HttpWebRequest)WebRequest.Create(uri);
			webreq.CookieContainer = xmCookies;
			// Get the response
			try 
			{
				webres = (HttpWebResponse)webreq.GetResponse();										
				resStream = webres.GetResponseStream();
				response = new StreamReader( resStream ).ReadToEnd();										
			} 
			catch (System.Net.WebException e) 
			{
				SnapStream.Logging.WriteLog(e.ToString());
				_header.Text = "Login Error";
				loggedin = false;
			}										
			
			try 
			{
				// Get channels and pad data
				string whatson = response.Substring(response.IndexOf("{"));
				whatson = whatson.Replace("{","");
				whatson = whatson.Replace("}","");
				whatson = whatson.Replace(")","");
				whatson = whatson.Replace("]","");
				whatson = whatson.Replace(";","");
				string[] pdata = whatson.Split('"');	
				padDataList = new ArrayList();				
				for (int i = 0; i < pdata.Length-1; i = i + 6) 
				{	
					PadData cPadData = new PadData();	
					string temp = pdata[i].Substring(pdata[i].IndexOf(':')+1);
					string[] tempA = temp.Split(',');
					cPadData.Channel = int.Parse(tempA[0].Trim()); // channel					
					cPadData.Artist = replaceAscii(pdata[i+1].Trim()); // artist				
					cPadData.Song = replaceAscii(pdata[i+3].Trim()); // song
					cPadData.Album = replaceAscii(pdata[i+5].Trim()); // album
					padDataList.Add(cPadData);

					// Update header if playing
					if (wmp.playState == WMPLib.WMPPlayState.wmppsPlaying) 
					{
						if (currentChannel == cPadData.Channel) 
						{							
							_header.Text = "Playing: " + cPadData.Song;			
							_header2.Text = "   " + cPadData.Artist + " (" + cPadData.Album + ")";
							_logo2.Background = fi.DirectoryName + "\\Images\\" + cPadData.Channel.ToString() + ".png";
						}
					}
					else if (wmp.playState == WMPLib.WMPPlayState.wmppsBuffering) 
					{
						_header.Text = "Buffering";
						_header2.Text = "";
						_logo2.Background = fi.DirectoryName + "\\Images\\" + "blackpixel.png";
					}
					else if (wmp.playState == WMPLib.WMPPlayState.wmppsTransitioning) 
					{
						_header.Text = "Changing Channels";
						_header2.Text = "";
						_logo2.Background = fi.DirectoryName + "\\Images\\" + "blackpixel.png";
					}
					else 
					{
						_header.Text = "";
						_header2.Text = "";
						_logo2.Background = fi.DirectoryName + "\\Images\\" + "blackpixel.png";
					}
				}							
			}
			catch (Exception e) 
			{
				SnapStream.Logging.WriteLog("XMRadio: " + e.ToString());
			}
			// Sort the channels
			padDataList.Sort( new PadDataComparer() );										
		}

		// Update the Pad Data Display
		private void updatePadDataDisplay() {
			int i = 0; 			
			foreach (PadData pd in padDataList) 
			{					
				// Update the pad list
				xmPadDataList[i].Channel.Text = pd.Channel.ToString();								
				xmPadDataList[i].Artist.Text = "Artist: " + pd.Artist;									
				xmPadDataList[i].Song.Text = "Title: " + pd.Song;				
				xmPadDataList[i].Album.Text = "Album: " + pd.Album;				
				i++;
			}															
		}

		// Replace special ASCII characters
		private string replaceAscii(string text) 
		{
			text = text.Replace("&#39","'");
			text = text.Replace("&#47","/");
			return text;
		}

		// Update the pad data
		private void updateTimer_Tick( object sender, EventArgs e ) 
		{			
			// Store current index
			int index = _xmChannels.SelectedIndex;

			// Get the pad data if logged in, otherwise try to log in again
			if (loggedin) 
			{
				PreparePadDataArray();
				updatePadDataDisplay();
			}
			else 
				loggedin = Login();

			// Restore current index
			_xmChannels.SelectedIndex = index;					
						
		}

		#endregion Private Methods



		public class PadDataComparer : IComparer
		{
			#region IComparer Members

			public int Compare(object x, object y)
			{
				PadData xTemp, yTemp;

				xTemp = (PadData)x;
				yTemp = (PadData)y;

				return xTemp.Channel.CompareTo(yTemp.Channel); 
			}

			#endregion

		}


		internal sealed class PadData 
			{
				private int channel;
				private string artist;
				private string song;
				private string album;

				public PadData() 
				{
				}

				public int Channel 
				{
					get { return channel; }
					set { channel = value; }
				}
				public string Artist 
				{
					get { return artist; }
					set { artist = value; }
				}
				public string Song 
				{
					get { return song; }
					set { song = value; }
				}
				public string Album 
				{
					get { return album; }
					set { album = value; }
				}
		}

		
	}
}
