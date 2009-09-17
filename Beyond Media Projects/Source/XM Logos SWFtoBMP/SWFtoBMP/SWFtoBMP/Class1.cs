using System;
using System.IO;
using System.Net;
using SWFToImage;

namespace SWFtoBMP
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class Class1
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>		
		static void Main(string[] args)
		{
			string IMAGEMAGICK_PATH = "ImageMagick-6.4.5-Q16";

			SWFToImage.SWFToImageObject SWFToImage = new SWFToImage.SWFToImageObjectClass();

			SWFToImage.InitLibrary("demo","demo");

			// Download all files first
			for (int i = 0; i < 350; i++)
			{
				try
				{
					WebClient client = new WebClient();
					client.DownloadFile("http://player.xmradio.com/player/_logos/"+i.ToString()+".swf", i.ToString()+".swf");
				}
				catch (Exception)
				{					
				}
			}
		


			string[] swfs = System.IO.Directory.GetFiles(System.IO.Directory.GetCurrentDirectory(),"*.swf");

			foreach (string swf in swfs)
			{
				System.Console.WriteLine("Converting: " + swf);
				SWFToImage.InputSWFFileName = swf;
				SWFToImage.FrameIndex = 0;
				try {SWFToImage.Execute();} 
				catch (Exception e) {System.Console.WriteLine(e.StackTrace + "-->" + e.Message);}
				SWFToImage.SaveToFile(swf.Split('.')[0] + ".bmp");					
			
				
				// Convert to PNG				
				string argument = "\"" + swf.Split('.')[0] + ".bmp\" \"" + swf.Split('.')[0] + ".png\"";				
				System.Diagnostics.Process p = System.Diagnostics.Process.Start("C:\\Program Files\\"+IMAGEMAGICK_PATH+"\\convert.exe", argument);
				System.Console.WriteLine("C:\\Program Files\\"+IMAGEMAGICK_PATH+"\\convert.exe" + argument);
				while (!p.HasExited) System.Threading.Thread.Sleep(100);								
				// Crop								
				argument = " -crop 320x240+60+70 \"" + swf.Split('.')[0] + ".png\" \"" + swf.Split('.')[0] + ".png\"";				
				p = System.Diagnostics.Process.Start("C:\\Program Files\\"+IMAGEMAGICK_PATH+"\\convert.exe", argument);
				System.Console.WriteLine("C:\\Program Files\\"+IMAGEMAGICK_PATH+"\\convert.exe" + argument);
				while (!p.HasExited) System.Threading.Thread.Sleep(100);								
				argument = " -crop 320x240-60-70 \"" + swf.Split('.')[0] + ".png\" \"" + swf.Split('.')[0] + ".png\"";				
				p = System.Diagnostics.Process.Start("C:\\Program Files\\"+IMAGEMAGICK_PATH+"\\convert.exe", argument);
				System.Console.WriteLine("C:\\Program Files\\"+IMAGEMAGICK_PATH+"\\convert.exe" + argument);				
				// Make transparent
				while (!p.HasExited) System.Threading.Thread.Sleep(100);				
				argument = " -transparent white \"" + swf.Split('.')[0] + ".png\" \"" + swf.Split('.')[0] + ".png\"";				
				p = System.Diagnostics.Process.Start("C:\\Program Files\\"+IMAGEMAGICK_PATH+"\\convert.exe", argument);
				System.Console.WriteLine("C:\\Program Files\\"+IMAGEMAGICK_PATH+"\\convert.exe" + argument);				
			}

			System.Console.In.Read();
		}
	}
}
