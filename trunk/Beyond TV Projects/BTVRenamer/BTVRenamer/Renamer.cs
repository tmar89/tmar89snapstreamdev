using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.XPath;
using System.Xml;
using BeyondTVLibrary;

namespace ConsoleApplication1
{
    class Renamer
    {
        static void Main(string[] args)
        {
            bool simMode = true;
            bool unattended = false;

            // Bag of all the media files in the BTV Library
            PVSPropertyBag[] mediafiles;
            int mediaCounter = 0;

            // List to save the Series names and ID
            List<string> seriesNameList = new List<string>();
            List<string> seriesIDList = new List<string>();

            // String for holding console line responses
            String res;

            // Logon to the BeyondTV server            
            BTVLicenseManager manager = new BTVLicenseManager();
            // Get server details
            Console.Write("Enter BeyondTV Server port [8129]: ");
            string port = Console.ReadLine();
            if (port.Equals(""))
                port = "8129";
            manager.Url = "http://127.0.0.1:"+port+"/wsdl/BTVLicenseManager.asmx";
            Console.Write("Enter BeyondTV Server username [username]: ");
            string username = Console.ReadLine();
            if (username.Equals(""))
                username = "username";
            Console.Write("Enter BeyondTV Server password [password]: ");
            string password = Console.ReadLine();
            if (password.Equals(""))
                password = "password";
            Console.WriteLine("Connecting to Beyond TV Server...");
            PVSPropertyBag lbag = null;
            try
            {
                lbag = manager.Logon("", username, password);
            }
            catch (Exception e) { Console.WriteLine("Cannot log into Beyond TV Server.  Exitting..."); return; }
            string auth = "";
            foreach (PVSProperty pvp in lbag.Properties)
            {
                if (pvp.Name.Equals("AuthTicket"))
                {
                    //gets ticket so we can run all the other commands
                    auth = pvp.Value;
                }
            }
            Console.WriteLine("Connected!");

            Console.Write("Run in Simulation Mode? [y]/n: ");
            res = Console.ReadLine();
            if (!res.Equals("y") && !res.Equals("Y") && !res.Equals(""))
                simMode = false;

            if (!simMode)
            {
                Console.Write("Run Unattended (Unless user input required)? y/[n]: ");
                res = Console.ReadLine();
                if (res.Equals("y") || res.Equals("Y"))
                    unattended = true;
            }

            // Load Library
            BTVLibrary library = new BTVLibrary();
            library.Url = "http://127.0.0.1:"+port+"/wsdl/BTVLibrary.asmx";
            Console.Write("Retrieving Library... ");
            
            // Get all filenames in library that were recorded by BTV
            mediafiles = library.FlatViewByTitle(auth);
            Console.WriteLine(" Found {0} files.", mediafiles.Length);
            foreach (PVSPropertyBag mediafile in mediafiles)
            {                
                // Define variables for the media looping                
                string seriesName = null;
                string originalAirDate = null;
                string channel;
                string seasonNumber = null;
                string episodeNumber = null;
                string episodeTitle = null;
                string filename = null;
                string newfilename;
                string URLString;
                XmlTextReader reader = null;
                XmlDocument doc = null;
                int seriesIndex = 0;
                int episodeIndex = 0;
                string seriesID = null;                              

                // Reset the flag to see if the channel data is set
                bool hasChannel = false;

                // Reset the flag to see if the channel data is set
                bool foundMovie = false;

                // Reset the flag to see if the series exists in memory
                bool existsInMemory = false;

                // Reset the flag to see if the user wants to scrape and rename
                bool rename = false;

                // Reset the flag to see if a series was found
                bool foundSeries = false;

                // Reset the flag to see if an episode was found
                bool foundEpisode = false;                               

                // See if the media has channel info
                foreach (PVSProperty pvp in mediafile.Properties)
                {
                    if (pvp.Name.Equals("Channel"))
                        hasChannel = true;
                    if (pvp.Name.Equals("FullName"))
                    {
                        Console.WriteLine("{1}/{2} File: {0}", pvp.Value, ++mediaCounter, mediafiles.Length);
                        filename = pvp.Value;
                    }
                }

                // Display and retrieve the data 
                if (hasChannel)
                {
                    foreach (PVSProperty pvp in mediafile.Properties)
                    {
                        //Console.WriteLine("{0} : {1}", pvp.Name, pvp.Value);
                        if (pvp.Name.Equals("Title") && pvp.Value.Equals("Movies"))
                        {
                            foundMovie = true;
                            Console.WriteLine("Skipping natively recorded movie.");                            
                            break;
                        }
                        if (pvp.Name.Equals("FullName"))
                        {
                            Console.WriteLine("File: {0}", pvp.Value);
                            filename = pvp.Value;
                        }
                        if (pvp.Name.Equals("SortableName"))
                        {
                            Console.WriteLine("Series Name: {0}", pvp.Value);
                            seriesName = pvp.Value;
                        }
                        if (pvp.Name.Equals("OriginalAirDate"))
                        {
                            originalAirDate = pvp.Value;
                            originalAirDate = originalAirDate.Insert(6, "-").Insert(4, "-");
                            Console.WriteLine("Original Air Date: {0}", originalAirDate);
                        }
                        if (pvp.Name.Equals("EpisodeTitle"))
                        {
                            Console.WriteLine("Episode Name: {0}", pvp.Value);
                            episodeTitle = pvp.Value;
                        }
                        if (pvp.Name.Equals("Channel"))
                        {
                            Console.WriteLine("Channel: {0}", pvp.Value);
                            channel = pvp.Value;
                        }                        
                    }
                }
                else
                    Console.WriteLine("Media not a Beyond TV native recording.  Ignoring.");

                // Ignore a recorded movie
                if (foundMovie)
                {
                    //Console.WriteLine("-----------PRESS ANY KEY TO CONTINUE-----------");
                    //Console.ReadLine();
                    Console.WriteLine("---------------------------------------\n");
                    continue;
                }

                // Does the filename already have SxxExx in it?
                if (hasChannel)
                {
                    if (Regex.IsMatch(filename, "[Ss]+([0-9]+)+[Ee]+([0-9]+)"))
                    {
                        Console.WriteLine("Show already in proper format!");
                        //Console.WriteLine("-----------PRESS ANY KEY TO CONTINUE-----------");
                        //Console.ReadLine();
                        Console.WriteLine("---------------------------------------\n");
                        continue;
                    }
                }

                // Ask user if this file should be scraped and renamed since this was deemed a BTV recording
                if (hasChannel)
                {
                    if (!unattended)
                    {
                        Console.Write("Try to rename this recording? [y]/n: ");
                        res = Console.ReadLine();
                        if (res.Equals("y") || res.Equals("Y") || res.Equals(""))
                            rename = true;
                    }
                    else
                        rename = true;
                }

                // Check array first
                if (hasChannel && rename)
                {
                    int indexOfShowInArray = seriesNameList.IndexOf(seriesName);
                    if (indexOfShowInArray != -1)
                    {
                        Console.WriteLine("'{0}' exists in memory already", seriesName);                        
                        seriesID = seriesIDList[indexOfShowInArray];
                        existsInMemory = true;
                        foundSeries = true;
                    }
                }

                // Search TheTVDB.com since I have the permission to rename        
                if (hasChannel && rename && !existsInMemory)
                {
                    // Search theTVDB.com API for the series ID   
                    Console.WriteLine("Searching TheTVDB.com...");
                    URLString = "http://www.thetvdb.com/api/GetSeries.php?seriesname=" + seriesName + "&language=en";
                    reader = new XmlTextReader(URLString);
                    doc = new XmlDocument();
                    try
                    {
                        doc.Load(reader);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Bad URL: {0} for Series '{1}'", URLString, seriesName);
                        Console.WriteLine("-----------PRESS ANY KEY TO CONTINUE-----------");
                        Console.ReadLine();
                        continue;
                    }                    
                    reader.Close();

                    // Get the series name and the index in the XML if there are multiple
                    XmlNodeList seriesNamesList = doc.GetElementsByTagName("SeriesName");
                    int seriesCount = seriesNamesList.Count;
                    seriesIndex = 0;
                    // Check if any items were found
                    if (seriesCount == 0) // Found no shows
                    {                        
                        Console.WriteLine("No Series Found for '{0}'", seriesName);
                        foundSeries = false;
                    }
                    else if (seriesCount == 1) // Found one show
                    {
                        Console.WriteLine("Found: {0}", seriesNamesList.Item(0).InnerText);
                        foundSeries = true;
                    }
                    else // Found multiple shows
                    {                        
                        Console.WriteLine("Found Multiple Shows: {0}", seriesCount);
                        // Show all the series found
                        for (int i = 0; i < seriesNamesList.Count; i++)
                        {
                            Console.WriteLine(" " + (i + 1).ToString() + " :" + seriesNamesList.Item(i).InnerText);                            
                        }                        
                        // Ask user to select a show or 0 to skip                        
                        Console.Write("Select a Series to match '{0}' or enter 0 to skip [1]: ", seriesName);
                        res = Console.ReadLine();
                        if (res.Equals(""))
                            seriesIndex = 0;
                        else
                            seriesIndex = int.Parse(res) - 1;
                        // Check if > 0 to see if the user picked a series
                        if (seriesIndex > -1)
                        {
                            Console.WriteLine("Using Series '{0}'", seriesNamesList.Item(seriesIndex).InnerText);
                            foundSeries = true;
                        }
                        else
                        {
                            Console.WriteLine("Ignoring Series '{0}' ", seriesName);
                            foundSeries = false;
                        }                    
                    }
                }

                // Get series ID from the XML since at this point I have the series index from the search
                if (hasChannel && rename && foundSeries && !existsInMemory)
                {
                    XmlNodeList seriesIDXMLList = doc.GetElementsByTagName("seriesid");
                    seriesID = seriesIDXMLList.Item(seriesIndex).InnerText;
                    Console.WriteLine("Using {0} with TVDB_ID: {1}", seriesName, seriesID);

                    // Store tagged name and series ID in memory
                    seriesNameList.Add(seriesName);                    
                    seriesIDList.Add(seriesID);
                }

                // Get the Series and Episode Numbers now
                if (hasChannel && rename && foundSeries && seriesID != null)
                {
                    URLString = "http://www.thetvdb.com/api/8DB53EF83E7E8308/series/" + seriesID + "/all/en.xml";
                    reader = new XmlTextReader(URLString);
                    doc = new XmlDocument();
                    try
                    {
                        doc.Load(reader);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Bad URL: {0} for Series '{1}'", URLString, seriesName);
                        Console.WriteLine("-----------PRESS ANY KEY TO CONTINUE-----------");
                        Console.ReadLine();
                        continue;
                    }
                    reader.Close();

                    // Get the episode list
                    XmlNodeList episodeList = doc.GetElementsByTagName("Episode");
                    int episodeCount = episodeList.Count;
                    Console.WriteLine("Found {0} episode(s)", episodeCount.ToString());
                    // Go through each episode and find if the original air date exists
                    string[] seasonSearch = new string[10];
                    string[] episodeSearch = new string[10];
                    string[] episodeNameSearch = new string[10];
                    int dateMatches = 0;
                    for (int i = 0; i < episodeCount; i++)
                    {
                        string dateSearch = episodeList.Item(i).SelectSingleNode("FirstAired").InnerText.ToString();
                        if (dateSearch == originalAirDate)
                        {
                            seasonSearch[dateMatches] = episodeList.Item(i).SelectSingleNode("SeasonNumber").InnerText.ToString();
                            episodeSearch[dateMatches] = episodeList.Item(i).SelectSingleNode("EpisodeNumber").InnerText.ToString();
                            episodeNameSearch[dateMatches] = episodeList.Item(i).SelectSingleNode("EpisodeName").InnerText.ToString();
                            Console.WriteLine("Found episode match to original air date: '{0}' S{1}E{2}", episodeNameSearch[dateMatches], (int.Parse(seasonSearch[dateMatches])).ToString("D2"), (int.Parse(episodeSearch[dateMatches])).ToString("D2"));
                            dateMatches++;
                        }
                    }                                       
                    // Check to see how many matches were found
                    if (dateMatches <= 0) // No Matches Found
                    {
                        Console.WriteLine("No Episodes found for Original Air Date {0}", originalAirDate);
                        foundEpisode = false;
                    }
                    else if (dateMatches == 1) // Found one match
                    {
                        seasonNumber = (int.Parse(seasonSearch[0])).ToString("D2");
                        episodeNumber = (int.Parse(episodeSearch[0])).ToString("D2"); 
                        foundEpisode = true;
                    }
                    else // Found multiple matches
                    {
                        Console.WriteLine("Found Multiple Episodes: {0}", dateMatches);
                        // Show all the episodes found
                        for (int i = 0; i < dateMatches; i++)
                        {                            
                            Console.WriteLine(" " + (i + 1).ToString() + " : '{0}' S{1}E{2}", episodeNameSearch[i], (int.Parse(seasonSearch[i])).ToString("D2"), (int.Parse(episodeSearch[i])).ToString("D2")); 
                        }                                                
                        // Ask user to select a show or 0 to skip                        
                        Console.Write("Select an Episode to use or enter 0 to skip [1]: ");
                        res = Console.ReadLine();
                        if (res.Equals(""))
                            episodeIndex = 0;
                        else 
                            episodeIndex = int.Parse(res) - 1;
                        // Check if > 0 to see if the user picked a series
                        if (episodeIndex > -1)
                        {
                            Console.WriteLine("Using '{0}' S{1}E{2}'", episodeNameSearch[episodeIndex], (int.Parse(seasonSearch[episodeIndex])).ToString("D2"), (int.Parse(episodeSearch[episodeIndex])).ToString("D2"));
                            seasonNumber = (int.Parse(seasonSearch[episodeIndex])).ToString("D2");
                            episodeNumber = (int.Parse(episodeSearch[episodeIndex])).ToString("D2");
                            foundEpisode = true;
                        }
                        else
                        {
                            Console.WriteLine("Ignoring Episode Search");
                            foundEpisode = false;
                        }                    
                    }
                }

                // Move file to new filename format and delete original file if user wants since I have everything now
                if (hasChannel && rename && foundSeries && seriesID != null && foundEpisode)
                {
                    string extension = filename.Substring(filename.LastIndexOf("."));
                    string filenameNoExt = filename.Remove(filename.LastIndexOf("."));
                    string path = filename.Remove(filename.LastIndexOf("\\")+1);
                    seriesName = Renamer.replaceSpecialChars(seriesName);
                    episodeTitle = Renamer.replaceSpecialChars(episodeTitle);
                    string newfilenameNoExt = path + seriesName + ".S" + seasonNumber + "E" + episodeNumber + "." + episodeTitle;
                    newfilename = newfilenameNoExt + extension;
                    if (!File.Exists(newfilename))
                    {
                        if (simMode)
                            Console.WriteLine("SIMULATING Creating {0}.", newfilename);
                        else
                        {
                            // Add SxxExx to Episode Description if it doesn't exist                            
                            foreach (PVSProperty pvp in mediafile.Properties)
                            {
                                if (pvp.Name.Equals("EpisodeDescription"))
                                {
                                    string episodeDescription = pvp.Value;
                                    if (!(Regex.IsMatch(episodeDescription, "[Ss]+([0-9]+)+[Ee]+([0-9]+)")))
                                    {
                                        episodeDescription = "S" + seasonNumber + "E" + episodeNumber + " - " + episodeDescription;
                                        pvp.Value = episodeDescription;                                                                                                                             
                                    }
                                    library.EditMedia(auth, @filename, mediafile);
                                }
                            }                           

                            // Copy all files with this filename
                            // Copy main video file
                            //System.IO.File.Copy(@filename, @newfilename);                                                        
                            System.IO.File.Move(@filename, @newfilename);
                            // Copy chapters xml file                            
                            string tempsource = filename + ".chapters.xml";
                            string tempdest = newfilename + ".chapters.xml";
                            if (File.Exists(tempsource))
                                //System.IO.File.Copy(@tempsource, tempdest);
                                System.IO.File.Move(@tempsource, @tempdest);
                            // Copy edl file
                            tempsource = filenameNoExt + ".edl";
                            tempdest = newfilenameNoExt + ".edl";
                            if (File.Exists(tempsource))
                                //System.IO.File.Copy(@tempsource, tempdest);
                                System.IO.File.Move(@tempsource, @tempdest);
                            // Copy txt file
                            tempsource = filenameNoExt + ".txt";
                            tempdest = newfilenameNoExt + ".txt";
                            if (File.Exists(tempsource))
                                //System.IO.File.Copy(@tempsource, @tempdest);
                                System.IO.File.Move(@tempsource, @tempdest);
                            // Copy log file
                            tempsource = filenameNoExt + ".log";
                            tempdest = newfilenameNoExt + ".log";
                            if (File.Exists(tempsource))
                                //System.IO.File.Copy(@tempsource, tempdest);
                                System.IO.File.Move(@tempsource, @tempdest);
                        }

                        /*
                        if (simMode)
                            Console.WriteLine("SIMULATING Deleting {0} if the user requested.", filename);
                        else
                        {
                            // Ask user to delete the original                        
                            Console.Write("Delete the original file? [y]/n: ");
                            res = Console.ReadLine();
                            if (res.Equals("y") || res.Equals("Y") || res.Equals(""))
                            {
                                // Delete Main Video File
                                FileInfo file = new FileInfo(filename);
                                if (file.IsReadOnly)
                                {
                                    Console.Write("File is Read Only, override and delete? [y]/n: ");
                                    res = Console.ReadLine();
                                    if (res.Equals("y") || res.Equals("Y") || res.Equals(""))
                                    {
                                        file.IsReadOnly = false;
                                        System.IO.File.Delete(@filename);
                                    }
                                }
                                else
                                    System.IO.File.Delete(@filename);

                                // Delete Chapters XML file
                                string tempsource = filename + ".chapters.xml";
                                if (File.Exists(tempsource))
                                {
                                    file = new FileInfo(tempsource);
                                    if (file.IsReadOnly)
                                    {
                                        Console.Write("File is Read Only, override and delete? [y]/n: ");
                                        res = Console.ReadLine();
                                        if (res.Equals("y") || res.Equals("Y") || res.Equals(""))
                                        {
                                            file.IsReadOnly = false;
                                            System.IO.File.Delete(@tempsource);
                                        }
                                    }
                                    else
                                        System.IO.File.Delete(@tempsource);
                                }

                                // Delete EDL file
                                tempsource = filenameNoExt + ".edl";
                                if (File.Exists(tempsource))
                                {
                                    file = new FileInfo(tempsource);
                                    if (file.IsReadOnly)
                                    {
                                        Console.Write("File is Read Only, override and delete? [y]/n: ");
                                        res = Console.ReadLine();
                                        if (res.Equals("y") || res.Equals("Y") || res.Equals(""))
                                        {
                                            file.IsReadOnly = false;
                                            System.IO.File.Delete(@tempsource);
                                        }
                                    }
                                    else
                                        System.IO.File.Delete(@tempsource);
                                }

                                // Delete TXT file
                                tempsource = filenameNoExt + ".txt";
                                if (File.Exists(tempsource))
                                {
                                    file = new FileInfo(tempsource);
                                    if (file.IsReadOnly)
                                    {
                                        Console.Write("File is Read Only, override and delete? [y]/n: ");
                                        res = Console.ReadLine();
                                        if (res.Equals("y") || res.Equals("Y") || res.Equals(""))
                                        {
                                            file.IsReadOnly = false;
                                            System.IO.File.Delete(@tempsource);
                                        }
                                    }
                                    else
                                        System.IO.File.Delete(@tempsource);
                                }

                                // Delete LOG file
                                tempsource = filenameNoExt + ".log";
                                if (File.Exists(tempsource))
                                {
                                    file = new FileInfo(tempsource);
                                    if (file.IsReadOnly)
                                    {
                                        Console.Write("File is Read Only, override and delete? [y]/n: ");
                                        res = Console.ReadLine();
                                        if (res.Equals("y") || res.Equals("Y") || res.Equals(""))
                                        {
                                            file.IsReadOnly = false;
                                            System.IO.File.Delete(@tempsource);
                                        }
                                    }
                                    else
                                        System.IO.File.Delete(@tempsource);
                                }
                            }
                        }
                        */
                    }
                    else {
                        Console.WriteLine("File {0} Exists. Ignoring changes.", newfilename);                        
                    }
                }                                
                
                //Console.WriteLine("-----------PRESS ANY KEY TO CONTINUE-----------");
                //Console.ReadLine();
                Console.WriteLine("---------------------------------------\n");
            }            
              
            //Logoff the BeyondTV server
            Console.WriteLine("Finished...logging off");
            manager.Logoff(auth);
            Console.ReadLine();
        }

        static string replaceSpecialChars(string mystring)
        {
            string newstring = mystring;
            newstring = newstring.Replace(" ", ".");
            newstring = newstring.Replace("&", "and");
            newstring = newstring.Replace("\\", "");
            newstring = newstring.Replace("/", "");
            newstring = newstring.Replace(":", "");
            newstring = newstring.Replace("*", "");
            newstring = newstring.Replace("?", "");
            newstring = newstring.Replace("\"", "");
            newstring = newstring.Replace("<", "");
            newstring = newstring.Replace(">", "");
            newstring = newstring.Replace("|", "");
            return newstring;
        }
    }
}
