using System;
using System.Collections;
using System.Net;
using System.Text.RegularExpressions;

using SnapStream.Configuration;
using SnapStream.ViewScape.Services;


namespace SnapStream.MovieShowtimes
{
	/// <summary>
	/// Summary description for AppleTrailerParser.
	/// </summary>
	public class AppleTrailerParser : GenericParser {
		private string		_justAddedUrl = "http://www.apple.com/trailers/home/feeds/just_added.json";
		private string		_studiosUrl = "http://www.apple.com/trailers/home/feeds/studios.json";
		private string		_trailersUrl = "http://www.apple.com/trailers/";
		private string		_baseUrl = "http://www.apple.com";
		private ArrayList	_trailerPagesSeen = new ArrayList();

		public AppleTrailerParser() {
			//
			// TODO: Add constructor logic here
			//
		}

		public void GetTrailerUrls(ArrayList moviePairs, out ArrayList trailerPairs) {
			_trailerPagesSeen.Clear();
			string	pageString = WebRequest(_studiosUrl);
			string[] lines = pageString.Split("\n".ToCharArray());
			int qualityIndex = 0;
			trailerPairs = new ArrayList();
			string trailerQuality;

			SingletonConfig.Instance.GetPropertyAsString( "MovieShowtimes.TrailerQuality", out trailerQuality);
			switch (trailerQuality) {
				case "High" : qualityIndex = 3;
					break;
				case "Medium" : qualityIndex = 2;
					break;
				default : qualityIndex = 1;
					break;
			}

			foreach (string line in lines) {
				string title = string.Empty;
				string url = string.Empty;

				MatchCollection matches = Regex.Matches(line.Trim(), "\".*?\":\".*?\"");
				foreach (Match match in matches) {
					string temp = match.Value.Trim().Replace( "\"", "" );
					int index = temp.IndexOf(':');
					string[] parts = new string[2];
					parts[0] = temp.Substring(0, index);
					parts[1] = temp.Substring(index + 1);

					parts[1] = parts[1].Replace( "&amp;", "&" );
					parts[1] = parts[1].Replace( "&#146;", "'" );
					if( parts[0] == "title" && title == string.Empty) {
						title = parts[1];
					}
					else if( parts[0] == "location" && url == string.Empty) {
						url = parts[1];
					}
				}
				
				if( title.Length > 0 ) {
					GetUrlIfNeeded( title.ToLower(), url, qualityIndex, moviePairs, trailerPairs );
				}

			}

			pageString = WebRequest(_justAddedUrl);
			lines = pageString.Split("\n".ToCharArray());
			
			foreach (string line in lines) {
				string title = string.Empty;
				string url = string.Empty;

				MatchCollection matches = Regex.Matches(line.Trim(), "\".*?\":\".*?\"");
				foreach (Match match in matches) {
					string temp = match.Value.Trim().Replace( "\"", "" );
					int index = temp.IndexOf(':');
					string[] parts = new string[2];
					parts[0] = temp.Substring(0, index);
					parts[1] = temp.Substring(index + 1);

					parts[1] = parts[1].Replace( "&amp;", "&" );
					parts[1] = parts[1].Replace( "&#146;", "'" );
					if( parts[0] == "title" && title == string.Empty) {
						title = parts[1];
					}
					else if( parts[0] == "location" && url == string.Empty) {
						url = parts[1];
					}
				}
				
				if( title.Length > 0 ) {
					GetUrlIfNeeded( title.ToLower(), url, qualityIndex, moviePairs, trailerPairs );
				}

			}

			return;
		}

		private void GetUrlIfNeeded( string title, string pageUrl, int qualityIndex, ArrayList moviePairs, ArrayList trailerPairs ) {
			
			foreach (MovieNamePair pair in moviePairs) {
				if (NameMatching(pair.Name,title)) {
					string trailerUrl = "";
					string[] trailerPageUrls;
								
					if (pageUrl.EndsWith(".html")) {
						trailerPageUrls = new string[1];
						if( pageUrl.StartsWith("/") ) {
								
							trailerPageUrls[0] = _baseUrl + pageUrl;
						}
						else {
							trailerPageUrls[0] = _trailersUrl + pageUrl;
						}
					} else {
						if( pageUrl.StartsWith("/") ) {
							trailerPageUrls = GetTrailerLinkFromPage( _baseUrl + pageUrl, qualityIndex );
						}
						else {
							trailerPageUrls = GetTrailerLinkFromPage(_trailersUrl + pageUrl, qualityIndex);
						}
					}
					
					if (trailerPageUrls.Length > 0) {
						foreach( string trailerPageUrl in trailerPageUrls ) {
							trailerUrl = GetTrailerFromPage(trailerPageUrl);
							if (trailerUrl != "") {
								trailerPairs.Add(new TrailerPair(pair.Movie, trailerUrl));
							}
						}
					}
				}
			}

			return;
		}

		private string[] GetTrailerLinkFromPage( string url, int qualityIndex ) {

			if( _trailerPagesSeen.Contains(url) ) {
				return new string[0];
			}

			_trailerPagesSeen.Add( url );
			int startIndex = 0;
			string pageString = "";
			string result = "";
			string[] searchForArray = new string[] { "href=\"", "HREF=\"" };
			ArrayList linkList = new ArrayList();
			try {
				pageString = WebRequest( url );
			} catch (Exception) {
				pageString = "";
			}

			int searchIndex = pageString.IndexOf("<body");
			if (searchIndex == -1) {
				searchIndex = pageString.IndexOf("<BODY");
				if (searchIndex == -1) {
					return new string[0];
				}
			}

			string baseUrl = url.Replace( _baseUrl, "" );
			int bufferPageIndex = pageString.IndexOf( "<ul class=\"movie-links\">", searchIndex );
			if( bufferPageIndex > -1 ) {
				linkList.Add( url );

				ArrayList trailerPageUrls = new ArrayList();
				
				foreach( string searchFor in searchForArray ) {
					startIndex = searchIndex;
					result = "";
					while( result != null ) {
						result = FindSubstring(
							pageString,
							"",
							searchFor,
							"\"",
							ref startIndex );

						if( result != null ) {
							if( !result.StartsWith(baseUrl) ) {
								continue;
							}
							trailerPageUrls.Add(_baseUrl + result);
						}
					}
				}

				foreach( string pageUrl in trailerPageUrls ) {
					string[] urls = GetTrailerLinkFromPage(pageUrl, qualityIndex);
					foreach( string potentialUrl in urls ) {
						if( !linkList.Contains(potentialUrl) ) {
							linkList.Add(potentialUrl);
						}
					}
				}

				return (string[])linkList.ToArray(typeof(string));				
			}

			foreach( string searchFor in searchForArray ) {
				startIndex = searchIndex;
				result = "";
				while( result != null ) {
					result = FindSubstring(
						pageString,
						"",
						searchFor,
						"\"",
						ref startIndex );

					if( result != null ) {
						if( result.Length == 0 ) {
							continue;
						}

						if( result.ToLower().StartsWith("http:") ) {
							continue;
						}

						if( result.StartsWith("/") ) {
							continue;
						}
						
						if( result == "#" ) {
							continue;
						}

						if( result == "index.html" ) {
							continue;
						}

						if( result.ToLower().StartsWith("javascript") ) {
							continue;
						}

						linkList.Add(url + result);
					}
				}
			}

			return (string[])linkList.ToArray(typeof(string));
		}

		private string GetTrailerFromPage( string url) 
		{
			string pageString;
			try {
				pageString = WebRequest( url );
			} catch (Exception) {
				pageString = "";
			}
			pageString = pageString.ToLower();
			
			return GetTrailer(ref pageString, 0);

		}

		private string GetTrailer(ref string pageString, int initialIndex) 
		{
			string trailer = "";
			string temp = "";
			int startIndex = 0;//pageString.IndexOf("<td id=\"trailer\">", initialIndex);
				

			if( pageString.IndexOf( "qt_writeobject_xhtml" ) == -1 ) {
				while( true ) {
					trailer = FindSubstring(
						pageString,
						"",
						"http:",
						".mov",
						ref startIndex);

					if( trailer == null ) {
						break;
					}
					if( trailer.LastIndexOf("http:") > -1 ) {
						trailer = trailer.Substring(trailer.LastIndexOf("http:") + 5);
					}
					trailer = "http:" + trailer + ".mov";

					if( trailer.EndsWith("_ctp.mov") ) {
						continue;
					}
					if( trailer.EndsWith("480p.mov") || trailer.EndsWith("720p.mov") || trailer.EndsWith("1080p.mov") ) {
						continue;
					}
					
					break;
				}

				if( trailer == null ) {
					trailer = "";
				}
				return trailer;

			}
	
			while( true ) {
				int tempIndex = 0;
				temp = FindSubstring(
					pageString,
					"qt_writeobject_xhtml",
					"(",
					")",
					ref startIndex );

				if( temp == null ) {
					return trailer;
				}

				if( temp.IndexOf("'href'") > -1 ) {
					trailer = FindSubstring(
						temp, 
						"'href'",
						",'",
						"',",
						ref tempIndex );
				}
				else {
					trailer = FindSubstring(
						temp,
						"",
						"'",
						"',",
						ref tempIndex );
				}

				if( trailer == null ) {
					break;
				}
				if( trailer.EndsWith("_ctp.mov") ) {
					continue;
				}
				if( trailer.EndsWith("480p.mov") || trailer.EndsWith("720p.mov") || trailer.EndsWith("1080p.mov") ) {
					continue;
				}
				if( trailer.EndsWith(".mov") ) {
					break;
				}
			}

			if( trailer == null ) {
				trailer = "";
			}
			return trailer;
		}

		private bool NameMatching(string first, string second) 
		{
			if ((first == null) || (second == null)) 
			{
				return false;
			}
			string style;
			SingletonConfig.Instance.GetPropertyAsString("MovieShowtimes.TrailerNameMatching", out style);
			
			if( first.Length < 4 || second.Length < 4 ) {
				style = "Full";
			}

			if (style == "Full") 
			{
				return (first == second);
			} 
			else {
				int index = -1;
				int partialIndex = -1;
				if (first.Length > second.Length) {
					index = first.IndexOf(second);
				}
				else {
					index = second.IndexOf(first);
				}

				first = first.Substring( 0, Math.Min(25, first.Length) );
				if (first.Length > second.Length) {
					partialIndex = first.IndexOf(second);
				}
				else {
					partialIndex = second.IndexOf(first);
				}

				return ( (index != -1) || (partialIndex != -1) );
			}
		}
	}
}
