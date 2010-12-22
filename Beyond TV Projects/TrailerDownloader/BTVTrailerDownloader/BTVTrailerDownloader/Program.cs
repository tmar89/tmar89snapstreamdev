/* Form: Port, Username, Password, Format, Scan Time, Enabled, Status Window
 * http://www.trailerfreaks.com/
 * 
 */

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace BTVTrailerDownloader
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GUI());
        }
    }
}