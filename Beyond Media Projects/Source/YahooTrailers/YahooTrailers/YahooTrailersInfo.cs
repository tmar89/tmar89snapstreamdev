using System;


namespace SnapStream.Plugins.YahooTrailers
{
	/// <summary>
	/// Summary description for YahooTrailersInfo.
	/// </summary>
	public class YahooTrailersInfo
	{
		private string		_title;
		private string		_details;	
		private string		_starring;
		private string		_genre;	
		private string		_releasedate;
		private string		_rating;	
		private string		_jpegURL;
		public string[]     _trailer2URL;
		public string[]     _trailerURL;
		public string[]     _teaser2URL;
		public string[]     _teaserURL;

		public string Title 
		{
			get 
			{
				return _title;
			}
			set 
			{
				_title = value;
			}
		}		
		public string Details 
		{
			get 
			{
				return _details;
			}
			set 
			{
				_details = value;
			}
		}	
		public string Starring 
		{
			get 
			{
				return _starring;
			}
			set 
			{
				_starring = value;
			}
		}	
		public string Genre 
		{
			get 
			{
				return _genre;
			}
			set 
			{
				_genre = value;
			}
		}			
		public string ReleaseDate 
		{
			get 
			{
				return _releasedate;
			}
			set 
			{
				_releasedate = value;
			}
		}
		public string Rating 
		{
			get 
			{
				return _rating;
			}
			set 
			{
				_rating = value;
			}
		}
		public string JPEGURL 
		{
			get 
			{
				return _jpegURL;
			}
			set 
			{
				_jpegURL = value;
			}
		}	

		public YahooTrailersInfo()
		{
			_title = "";
			_details = "";
			_starring = "";
			_genre = "";			
			_releasedate = "";
			_rating = "";
			_jpegURL = "";
		}			
	}
}
