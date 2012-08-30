using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Web.Services;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using BeyondTVLibrary;

namespace BTVTrailerDownloader
{
    public partial class GUI : Form
    {
        Thread scanner, downloader;
        BTVLibrary library;
        string auth, serverURL, port, username, password;
        bool verbose = false;
        string numTrailersToGet;
        System.Threading.Timer hourTimer;
        bool downloadSuccess = false;
        string trailerURL, movFile;

        public GUI()
        {
            InitializeComponent();
            loadSettings();
        }

        private void loadSettings()
        {
            formatBox.SelectedIndex = Properties.Settings.Default.formatIndex;
            numToGetCombo.SelectedIndex = Properties.Settings.Default.recentIndex;
            if (runOnStart.Checked)
            {
                //if (login())
                //{
                //    getMediaFolders();                                        
                //    scanButton.Checked = true;                    
                //}                
                scanButton.Checked = true;
                scanButton.Enabled = true;  
            }
        }

        private void saveSettings()
        {
            Properties.Settings.Default.formatIndex = formatBox.SelectedIndex;            
            Properties.Settings.Default.recentIndex = numToGetCombo.SelectedIndex;
            Properties.Settings.Default.Save();
        }

        private void getFoldersButton_Click(object sender, EventArgs e)
        {                        
            if (login())
            {
                getMediaFolders();
            }     
        }

        private void startScanning(Object obj)
        {
            if (scanner != null)
            {
                if (scanner.IsAlive)
                    scanner.Abort();                              
            }            
            scanner = new Thread(new ThreadStart(runwork));
            scanner.Start();         
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (scanButton.Checked.Equals(true)) {                
                if (!(locationBox.Text.Equals("{Select Location}"))) 
                {
                    enableControls(false);
                    numTrailersToGet = numToGetCombo.Text;
                    scanButton.Text = "Cancel Scan";                                                                                                    
                    // period (in milliseconds) - recurring interval to fire the timer (every 8 hours)
                    hourTimer = new System.Threading.Timer(startScanning, null, 0, 1000 * 60 * 60 * 4);                                        
                }
                else {
                    MessageBox.Show("Select a Download Location", "BTV Trailer Downloader Error");
                    scanButton.CheckState = CheckState.Unchecked;
                    enableControls(true);
                }
            }
            else {                
                scanButton.Text = "Scan";
                enableControls(true);
                hourTimer.Dispose();
            }
        }

        private bool login()
        {
            if (verbose)
                SetStatus(statusWindow, "Trying to log into Beyond TV Server");

            // Logon to the BeyondTV server            
            BTVLicenseManager manager = new BTVLicenseManager();
            serverURL = "localhost";
            port = portBox.Text;
            manager.Url = "http://" + serverURL + ":" + port + "/wsdl/BTVLicenseManager.asmx";
            username = usernameBox.Text;
            password = passwordBox.Text;
            PVSPropertyBag lbag = null;
            try
            {
                lbag = manager.Logon("", username, password);
            }
            catch (Exception e)
            {
                SetStatus(statusWindow, "Cannot log into Beyond TV Server");
                return false;
            }
            SetStatus(statusWindow, "Connected to Beyond TV Server!");

            // Get auth
            auth = "";
            foreach (PVSProperty pvp in lbag.Properties)
            {
                if (pvp.Name.Equals("AuthTicket"))
                {
                    //gets ticket so we can run all the other commands
                    auth = pvp.Value;
                }
            }            

            return true;
        }

        private void getMediaFolders()
        {
            locationBox.Items.Clear();
            // Get Media Folders
            library = new BTVLibrary();
            library.Url = "http://" + serverURL + ":" + port + "/wsdl/BTVLibrary.asmx";
            PVSPropertyBag[] bags = library.GetFolders(auth, String.Empty);
            foreach (PVSPropertyBag bag in bags)
            {
                foreach (PVSProperty prop in bag.Properties)
                {
                    if (prop.Name.Equals("FullName"))
                    {
                        if (verbose)                            
                            SetStatus(statusWindow, "Found Media Folder: " + prop.Value);                        
                        locationBox.Items.Add(prop.Value);
                    }
                }
            }
            locationBox.Enabled = true;
            scanButton.Enabled = true;
            SetStatus(statusWindow, "Folders Downloaded");
        }

        // Thread to scan
        private void runwork()
        {
            // Log in again 
            if (!login())
            {
                SetStatus(statusWindow, "Will try to log on again in 10 seconds");
                hourTimer.Change(10000, 1000 * 60 * 60 * 4);
                scanner.Abort();
            }

            try
            {
                // List to save movies to db file            
                List<string> downloadedTrailers = new List<string>();
                ObjectToSerialize trailerDBobject = new ObjectToSerialize();
                trailerDBobject.DownloadedTrailers = downloadedTrailers;

                // Scan for trailers
                SetStatus(statusWindow, "Scanning for new Trailers");
                string URL = "http://www.trailerfreaks.com/";
                HttpWebRequest webreq = (HttpWebRequest)WebRequest.Create(new System.Uri(URL));
                HttpWebResponse webres = (HttpWebResponse)webreq.GetResponse();
                Stream resStream = webres.GetResponseStream();
                string response = new StreamReader(resStream).ReadToEnd();
                // Find trailer
                string trailerStart = "a href =\"trai"; string toFind; string trailerName; string trailerDescription;
                string trailerDate; string trailerActors; string trailerNameClean; string trailerDetailsURL;
                int startindex, endindex;
                //int numToGet = 10000;
                //if (!numTrailersToGet.Equals("All"))
                //    numToGet = int.Parse(numTrailersToGet);
                //int count = 0;
                //while ((startindex = response.IndexOf(trailerStart)) > -1 && count < numToGet)
                while ((startindex = response.IndexOf(trailerStart)) > -1)
                {
                    bool skip = false;

                    if (startindex > -1)
                    {
                        response = response.Substring(startindex);
                    }
                    else
                    {
                        SetStatus(statusWindow, "Error Finding Trailer Page Links");
                        SetStatus(statusWindow, "Scanning aborted.");
                        enableControls(true);
                        scanner.Abort();
                    }
                    toFind = "title=\"";
                    startindex = response.IndexOf(toFind);
                    if (startindex > -1)
                    {
                        startindex += toFind.Length; // Move to starting position of new Trailer
                    }
                    else
                    {
                        SetStatus(statusWindow, "Error parsing for Trailer Name");
                        continue;
                    }
                    endindex = response.IndexOf("\"", startindex);
                    trailerName = response.Substring(startindex, endindex - startindex).Trim();
                    // Remove Year
                    trailerName = trailerName.Remove(trailerName.LastIndexOf(" "));
                    trailerNameClean = replaceSpecials(trailerName);
                    trailerNameClean = trailerNameClean.Remove(trailerNameClean.LastIndexOf(" "));
                    SetStatus(statusWindow, "Found Trailer: " + trailerName);
                    response = response.Substring(endindex);

                    // Check if it exists already in flat file
                    string trailerDB = Application.StartupPath + "\\trailerDB.txt";
                    if (!File.Exists(trailerDB))
                    {
                        if (verbose)
                            SetStatus(statusWindow, "New DB Created and will add Trailer after successfull download");
                    }
                    else
                    {
                        Serializer serializer = new Serializer();
                        trailerDBobject = serializer.DeSerializeObject(Application.StartupPath + "\\trailerDB.txt");
                        downloadedTrailers = trailerDBobject.DownloadedTrailers;
                        if (downloadedTrailers.Contains(trailerName + GetText(formatBox)))
                        {
                            SetStatus(statusWindow, "Already Downloaded, Skipping Trailer");
                            skip = true;
                        }
                        else
                        {
                            if (verbose)
                                SetStatus(statusWindow, "Not in DB will add Trailer after successfull download");
                        }
                    }

                    if (!skip)
                    {
                        // Get description
                        toFind = "<a href=\"";
                        startindex = response.IndexOf(toFind);
                        if (startindex > -1)
                        {
                            startindex += toFind.Length; // Move to starting position of new Trailer
                        }
                        else
                        {
                            SetStatus(statusWindow, "Error parsing for Trailer Details URL");
                            continue;
                        }
                        endindex = response.IndexOf("\"", startindex);
                        trailerDetailsURL = "http://www.trailerfreaks.com/" + response.Substring(startindex, endindex - startindex).Trim();
                        if (verbose)
                            SetStatus(statusWindow, "Found Detailed Page URL: " + trailerDetailsURL);
                        response = response.Substring(endindex);
                        // Open new URL to get description
                        HttpWebRequest webreqDesc = (HttpWebRequest)WebRequest.Create(new System.Uri(trailerDetailsURL));
                        HttpWebResponse webresDesc = (HttpWebResponse)webreqDesc.GetResponse();
                        Stream resStreamDesc = webresDesc.GetResponseStream();
                        string responseDesc = new StreamReader(resStreamDesc).ReadToEnd();
                        toFind = "class=\"plot\">";
                        int startindexDesc = responseDesc.IndexOf(toFind);
                        if (startindexDesc > -1)
                        {
                            startindexDesc += toFind.Length; // Move to starting position of new Trailer
                        }
                        else
                        {
                            SetStatus(statusWindow, "Error parsing for Trailer Description");
                            continue;
                        }
                        int endindexDesc = responseDesc.IndexOf("</td>", startindexDesc);
                        trailerDescription = responseDesc.Substring(startindexDesc, endindexDesc - startindexDesc).Trim();

                        // Find date
                        toFind = "trailer\" title=\"";
                        startindex = response.IndexOf(toFind);
                        if (startindex > -1)
                        {
                            startindex += toFind.Length; // Move to starting position of new Trailer
                        }
                        else
                        {
                            SetStatus(statusWindow, "Error parsing for Trailer Date");
                            continue;
                        }
                        endindex = startindex + 10;
                        trailerDate = response.Substring(startindex, endindex - startindex).Trim();
                        if (verbose)
                            SetStatus(statusWindow, "Found Date: " + trailerDate);
                        response = response.Substring(endindex);


                        // Find Actors
                        toFind = "trailer\" alt=\"";
                        startindex = response.IndexOf(toFind);
                        if (startindex > -1)
                        {
                            startindex += toFind.Length; // Move to starting position of new Trailer
                        }
                        else
                        {
                            SetStatus(statusWindow, "Error parsing for Trailer Actors");
                            continue;
                        }
                        endindex = response.IndexOf("\"", startindex);
                        trailerActors = response.Substring(startindex, endindex - startindex).Trim();
                        if (verbose)
                            SetStatus(statusWindow, "Found Actors: " + trailerActors);
                        response = response.Substring(endindex);

                        // Find MOV file
                        toFind = "class=\"trailerlink\">" + GetText(formatBox).ToUpper() + "</a>";
                        startindex = response.IndexOf(toFind);
                        if (startindex > -1)
                        {
                            startindex -= 250; // Move to starting position of new Trailer file
                        }
                        else
                        {
                            SetStatus(statusWindow, "Error parsing for Trailer File");
                            continue;
                        }
                        toFind = "<a href=\"";
                        startindex = response.IndexOf(toFind, startindex);
                        if (startindex > -1)
                        {
                            startindex += toFind.Length; // Move to starting position of new Trailer
                        }
                        else
                        {
                            SetStatus(statusWindow, "Error parsing for Trailer File");
                            continue;
                        }
                        endindex = response.IndexOf("\"", startindex);
                        trailerURL = response.Substring(startindex, endindex - startindex).Trim();
                        if (verbose)
                            SetStatus(statusWindow, "Found URL: " + trailerURL);
                        response = response.Substring(endindex + 250);

                        movFile = GetText(locationBox) + "\\" + trailerNameClean + "." + GetText(formatBox) + ".mov";
                        string mp4File = GetText(locationBox) + "\\" + trailerNameClean + "." + GetText(formatBox) + ".mp4";
                        if (verbose)
                            SetStatus(statusWindow, "Downloading Trailer File");
                        //WebClient Client = new WebClient();
                        //bool downloadSuccess = true;
                        //try
                        //{
                        //    Client.DownloadFile(trailerURL, movFile);
                        //}
                        //catch (Exception e)
                        //{
                        //    SetStatus(statusWindow, "Error Downloading.  Try again next time.");
                        //    downloadSuccess = false;
                        //}                    
                        downloader = new Thread(new ThreadStart(DownloadFile));
                        downloader.Start();
                        SetStatus(statusWindow, "\r\nDownloading");
                        while (downloader.IsAlive)
                        {
                            Thread.Sleep(1000);
                            SetStatusDownloading(statusWindow, ".");
                            if (GetText(scanButton) == "Scan")
                            {
                                SetStatus(statusWindow, "Download canceled");
                                downloader.Abort();
                                break;
                            }
                        }
                        downloader.Join();
                        downloader = null;
                        if (downloadSuccess)
                        {
                            SetStatus(statusWindow, "\r\nDownload Completed");
                            if (verbose)
                                SetStatus(statusWindow, "Converting to MP4");
                            // Convert using ffmpeg to mp4
                            string ffmpegPath = Application.StartupPath + "\\ffmpeg.exe";
                            string ffmpegParams = " -y -i \"" + movFile + "\" -vcodec copy -acodec copy \"" + mp4File + "\"";
                            if (verbose)
                            {
                                SetStatus(statusWindow, ffmpegPath);
                                SetStatus(statusWindow, ffmpegParams);
                            }
                            Process ffmpeg = new Process();
                            ffmpeg.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                            ffmpeg.StartInfo.FileName = ffmpegPath;
                            ffmpeg.StartInfo.Arguments = ffmpegParams;
                            //ffmpeg.StartInfo.FileName = "cmd.exe";
                            //ffmpeg.StartInfo.Arguments = "/k " + ffmpegPath + " " + ffmpegParams;                
                            ffmpeg.Start();
                            ffmpeg.WaitForExit();
                            System.IO.File.Delete(@movFile);
                            if (verbose)
                                SetStatus(statusWindow, "Conversion Completed");

                            // Add to database file
                            downloadedTrailers.Add(trailerName + GetText(formatBox));
                            Serializer serializer = new Serializer();
                            serializer.SerializeObject(Application.StartupPath + "\\trailerDB.txt", trailerDBobject);

                            // Wait for 5 seconds
                            Thread.Sleep(5000);

                            // Import metadata
                            // Find file
                            library = new BTVLibrary();
                            library.Url = "http://" + serverURL + ":" + port + "/wsdl/BTVLibrary.asmx";
                            PVSPropertyBag[] unknownFiles = library.GetItemsBySeries(auth, "Unknown");
                            foreach (PVSPropertyBag mediafile in unknownFiles)
                            {
                                foreach (PVSProperty pvp1 in mediafile.Properties)
                                {
                                    if (pvp1.Name.Equals("FullName"))
                                    {
                                        string filename = pvp1.Value;
                                        if (filename.Equals(mp4File))
                                        {
                                            if (verbose)
                                                SetStatus(statusWindow, "Found File in BTV Library");
                                            List<PVSProperty> propList = new List<PVSProperty>();

                                            PVSProperty pTitle = new PVSProperty();
                                            pTitle.Name = "Title";
                                            pTitle.Value = "Movie Trailers";
                                            propList.Add(pTitle);

                                            PVSProperty pEpisodeTitle = new PVSProperty();
                                            pEpisodeTitle.Name = "EpisodeTitle";
                                            pEpisodeTitle.Value = String.Empty;
                                            propList.Add(pEpisodeTitle);

                                            PVSProperty pDisplayTitle = new PVSProperty();
                                            pDisplayTitle.Name = "DisplayTitle";
                                            pDisplayTitle.Value = trailerName;
                                            propList.Add(pDisplayTitle);
                                            if (verbose)
                                                SetStatus(statusWindow, "Injected Title: " + pDisplayTitle.Value);

                                            PVSProperty pEpisodeDescription = new PVSProperty();
                                            pEpisodeDescription.Name = "EpisodeDescription";
                                            pEpisodeDescription.Value = "[" + GetText(formatBox) + "] " + trailerDescription;
                                            propList.Add(pEpisodeDescription);

                                            PVSProperty pActors = new PVSProperty();
                                            pActors.Name = "Actors";
                                            pActors.Value = trailerActors;
                                            propList.Add(pActors);

                                            PVSProperty pDate = new PVSProperty();
                                            pDate.Name = "OriginalAirDate";
                                            pDate.Value = trailerDate.Replace("-", "");
                                            propList.Add(pDate);
                                            if (verbose)
                                                SetStatus(statusWindow, "Injected Date: " + pDate.Value);

                                            PVSPropertyBag bag = new PVSPropertyBag();
                                            bag.Properties = (PVSProperty[])propList.ToArray();

                                            library.EditMedia(auth, @filename, bag);
                                            if (verbose)
                                                SetStatus(statusWindow, "Metadata Injected");
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Abort if unchecked
                    if (GetText(scanButton) == "Scan")
                    {
                        SetStatus(statusWindow, "Scanning aborted.");
                        enableControls(true);
                        scanner.Abort();
                    }

                    count++;
                }
            }
            catch (Exception e)
            {
                SetStatus(statusWindow, "Major Error: " + e.ToString());
            }

            // Done, for now
            SetStatus(statusWindow, "Scanning Completed.  Waiting 8 hours to scan again.  Force restart by clicking Scan button");
        }

        private void DownloadFile()
        {
            WebClient Client = new WebClient();
            downloadSuccess = false;
            try
            {                
                Client.DownloadFile(trailerURL, movFile);
                downloadSuccess = true;
            }
            catch (Exception e)
            {
                SetStatus(statusWindow, "Error Downloading.  Try again next time.");
                downloadSuccess = false;
            }
        }

        public delegate void SetStatusCallBack(Control control, string text);
        public static void SetStatus(Control control, string text)
        {
            if (control.InvokeRequired)
            {
                SetStatusCallBack d = new SetStatusCallBack(SetStatus);
                control.Invoke(d, new object[] { control, text });
            }
            else
            {
                control.Text = text + "\r\n" + control.Text;
            }
        }

        public delegate void SetStatusDownloadingCallBack(Control control, string text);
        public static void SetStatusDownloading(Control control, string text)
        {
            if (control.InvokeRequired)
            {
                SetStatusDownloadingCallBack d = new SetStatusDownloadingCallBack(SetStatusDownloading);
                control.Invoke(d, new object[] { control, text });
            }
            else
            {
                control.Text = text + control.Text;
            }
        }
        
        public delegate string GetTextCallBack(Control control);
        public static string GetText(Control control)
        {
            if (control.InvokeRequired)
            {
                GetTextCallBack d = new GetTextCallBack(GetText);
                return control.Invoke(d, new object[] { control }) as string;                            
            }
            else
            {
                return control.Text;
            }
        }     

        private string replaceSpecials(string s)
        {
            s = s.Replace(":", "_");
            s = s.Replace("<", "_");
            s = s.Replace(">", "_");
            s = s.Replace("\"", "_");
            s = s.Replace("/", "_");
            s = s.Replace("\\", "_");
            s = s.Replace("|", "_");
            s = s.Replace("?", "_");
            s = s.Replace("*", "_");
            return s;
        }

        private void enableControls(bool b) 
        {
            getFoldersButton.Enabled = b;
            portBox.Enabled = b;
            usernameBox.Enabled = b;
            passwordBox.Enabled = b;
            numToGetCombo.Enabled = b;
            eraseDB.Enabled = b;
            editDBButton.Enabled = b;
        }

        private void verboseStatus_CheckedChanged(object sender, EventArgs e)
        {
            verbose = verboseStatus.Checked;
        }

        private void eraseDB_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to clear your download history?", "Beyond TV Trailer Download", MessageBoxButtons.YesNo);
            if(result.Equals(DialogResult.Yes))
                System.IO.File.Delete(@"trailerDB.txt");
        }

        private void editDBButton_Click(object sender, EventArgs e)
        {
            string trailerDB = Application.StartupPath + "\\trailerDB.txt";
            if (File.Exists(trailerDB))
            {
                dBEditor dbeditor = new dBEditor();
                dbeditor.Show();
            }
            else
                MessageBox.Show("No History Yet", "Beyond TV Trailer Downloader Error");
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            saveSettings();
            Application.Exit();
        }           
    }
}