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
	/// ComicListItem represents a comic item in the list.
	/// It tries to minimize the amount of vertical space used and
	/// maximize the amount of horizontal space used
	/// </summary>
	public class ComicListItem : BaseListItem 
	{

		#region Private Members
		/// <summary>
		/// Window that holds the title of the comic
		/// </summary>
		private TextWindow	_caption;

		/// <summary>
		/// Window that holds the comic image
		/// </summary>
		private Window		_comic;

		/// <summary>
		/// Full path to the comic image
		/// </summary>
		private string		_fullName;
				
		#endregion Private Members

		#region Properties
		/// <summary>
		/// Window that holds the title of the comic
		/// </summary>
		public TextWindow Caption 
		{
			get 
			{
				return _caption;
			}
		}

		/// <summary>
		/// Window that holds the comic image
		/// </summary>
		public Window Comic 
		{
			get 
			{
				return _comic;
			}
		}

		/// <summary>
		/// Full path to the comic image
		/// </summary>
		public string FullName 
		{
			get 
			{
				return _fullName;
			}
		}
		#endregion Properties

		#region Constructors
		/// <summary>
		/// Creates a new ComicListItem
		/// </summary>
		/// <param name="text">Text to place in the caption</param>
		/// <param name="fileName">Full path to the comic image</param>
		public ComicListItem( string text, string fileName ) 
		{

			_caption = new TextWindow();
			_caption.Text = text;
			Add( _caption );

			_comic = new Window();
			_comic.Background = fileName;
			_comic.StretchBackground = false;
			Add( _comic );

			_fullName = fileName;
			return;
		}
		#endregion Constructors

		#region Protected Methods
		/// <summary>
		/// Overriden to set the bounds of the child windows
		/// </summary>
		/// <param name="x">x position in pixels</param>
		/// <param name="y">y position in pixels</param>
		/// <param name="width">width in pixels</param>
		/// <param name="height">height in pixels</param>
		/// <param name="specified"></param>
		/// <returns>
		/// True if no further bounds work needs to be handled
		/// </returns>
		protected override void SetBoundsCore( int x, int y, int width, int height ) 
		{

			base.SetBoundsCore( x, y, width, height );

			_caption.Bounds = new Rectangle( 5, 5, width - 10, 25 );
			_comic.Bounds = new Rectangle( 10, 35, width - 20, height - 45 );
			return;
		}
		#endregion Protected Methods
	}
}
