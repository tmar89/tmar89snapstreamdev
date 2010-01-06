/*
 * IMDb.com Movie Tagger for BTVShowInfo
 * 
 * Copyright (c) 2009 Thomas Marullo
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
*/

using System;
using System.Data;
using System.Net;
using System.IO;
using System.Globalization;
using System.Xml.XPath;
using System.Windows.Forms;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Microsoft.VisualBasic;
using ThoughtLabs.BTVShowInfo.PluginInterface;

namespace ThoughtLabs
{
    namespace BTVShowInfo
    {
        /// <summary>
        /// Summary description for MovieTagger.
        /// </summary>
        public class PluginMovieTagger : IShowInfoEditor
        {
            private Form progressForm;
            private Form multipleListingsForm;
            private Boolean isSkip;                         

            /// <summary>
            /// Constructor
            /// </summary>
            public PluginMovieTagger()
            {
            }


            #region IShowInfoEditor Members

            /// <summary>
            /// Store the host and add menu items
            /// </summary>
            /// <param name="host">The host</param>
            public void Initialize(IPluginHost host)
            {
                host.AddMenuItem("Tag from IMDb as Movie", this);
            }

            /// <summary>
            /// Do work on the data
            /// </summary>
            /// <param name="table">The datatable to read and edit</param>
            /// <param name="nav">XPathNavigator for column info</param>
            /// <returns>true if the edit was successful</returns>
            public Boolean EditShowInfo(ref DataTable table, XPathNavigator nav)
            {
                Int32 updatedRowCount = 0;

                Boolean DEBUG = false;

                // Progress box
                progressForm = new Form();
                progressForm.Size = new System.Drawing.Size(400, 100);
                progressForm.Text = "IMDb Movie Tagger";
                progressForm.StartPosition = FormStartPosition.CenterScreen;
                Label progressLabel = new Label();
                progressLabel.Size = new System.Drawing.Size(300, 50);
                progressForm.Controls.Add(progressLabel);
                progressForm.Show();

                // HTTP vars
                HttpWebRequest webreq;
                HttpWebResponse webres;
                Stream resStream;
                string response;
                string indexToFind;
                int startindex;
                int endindex;

                Regex yearRegex;
                Match yearMatch;                

                int count = 1;
                foreach (DataRow row in table.Rows)
                {
                    // Movie variable names                
                    string imdbID = "";
                    string movieTitle = "";
                    string moviePlot = "";
                    string movieGenre = "";
                    string movieYear = "";
                    string movieDate = "";
                    string movieActors = "";
                    int discNum = 1;
                    bool multidisc = false;
                    bool isIMDB = false;

                    Thread.Sleep(500);                                        

                    // Get the search URI for IMDb                    
                    string name = row["Name"].ToString();
                    // Remove the extension
                    name = name.Remove(name.LastIndexOf('.')).Trim();
                    // Remove some special characters for spacing
                    name = name.Replace("_", " ");
                    // See if it is a multi-part movie
                    if (name.IndexOf("-Disc") > 0)
                    {                        
                        discNum = int.Parse(name.Substring(name.IndexOf("-Disc") + 5));
                        name = name.Remove(name.IndexOf("-Disc")).Trim();                        
                        multidisc = true;
                    }
                    else
                    {
                        discNum = 1;
                        multidisc = false;
                    }

                    name = name.Replace(" ", "+");
                    string searchURI = "";
                    // Find out if the movie used the imdb ID or a title
                    if (name.StartsWith("tt"))
                    {
                        isIMDB = true;
                        imdbID = name;
                    }
                    else
                        searchURI = "http://www.imdb.com/find?s=all&q=" + name + "&x=0&y=0";

                    if (DEBUG)
                    {
                        MessageBox.Show("Tagging: " + name);                        
                    }

                    progressLabel.Text = "Tagging " + count + "/" + table.Rows.Count + ": " + row["Name"].ToString();                    

                    // Get the IMDB ID using the title
                    if (!isIMDB)
                    {
                        if (DEBUG)
                            MessageBox.Show("Finding IMDb ID by Title");

                        try
                        {
                            int popResults = 0;
                            int exactResults = 0;
                            int partialResults = 0;
                            int totalResults = 0;

                            webres = null;
                            try
                            {
                                webreq = (HttpWebRequest)WebRequest.Create(searchURI);
                                webreq.Timeout = 10000;
                                webreq.AllowAutoRedirect = true;
                                webres = (HttpWebResponse)webreq.GetResponse();                                
                            }
                            catch (Exception e)
                            {
                                MessageBox.Show(e.Message);
                                continue;
                            }                            
                            resStream = webres.GetResponseStream();
                            response = new StreamReader(resStream).ReadToEnd();
                            // Is it a direct hit?
                            string directHitLookup = "<link rel=\"canonical\" href=\"http://www.imdb.com/title/";
                            if (response.IndexOf(directHitLookup) != -1)
                            {
                                if (DEBUG)
                                    MessageBox.Show("Direct Hit");                                
                                startindex = response.IndexOf("imdb.com/title/") + 15;                                
                                if (startindex < 1)
                                {
                                    throw new Exception("Error Parsing for IMDb ID");                                    
                                }
                                imdbID = response.Substring(startindex, 9).Trim();                                
                                goto gotIMDBid;
                            }
                            
                            // Find Popular Titles
                            indexToFind = "<p><b>Popular Titles</b> (Displaying ";
                            startindex = response.IndexOf(indexToFind) + indexToFind.Length;
                            endindex = response.IndexOf("Result", startindex);
                            if ((endindex - startindex > 20) || endindex - startindex < 1)
                            {
                                popResults = 0;
                            }
                            else
                                popResults = int.Parse(response.Substring(startindex, endindex - startindex).Trim());
                            // Find Exact Title Matches
                            indexToFind = "<p><b>Titles (Exact Matches)</b> (Displaying ";
                            startindex = response.IndexOf(indexToFind) + indexToFind.Length;
                            endindex = response.IndexOf("Result", startindex);
                            if ((endindex - startindex > 20) || endindex - startindex < 1)
                            {
                                exactResults = 0;
                            }
                            else
                                exactResults = int.Parse(response.Substring(startindex, endindex - startindex).Trim());
                            // Find Partial Title Matches
                            indexToFind = "<p><b>Titles (Partial Matches)</b> (Displaying ";
                            startindex = response.IndexOf(indexToFind) + indexToFind.Length;
                            endindex = response.IndexOf("Result", startindex);
                            if ((endindex - startindex > 20) || endindex - startindex < 1)
                            {
                                partialResults = 0;
                            }
                            else
                                partialResults = int.Parse(response.Substring(startindex, endindex - startindex).Trim());

                            // Sum all the results
                            totalResults = popResults + exactResults + partialResults;

                            // Check if any items were found                            
                            if (totalResults == 0)
                            {
                                // Ask user if they want to manually search for show name or skip                            
                                DialogResult retval = MessageBox.Show("Nothing found.  Search for show manually?", row["Name"].ToString(), MessageBoxButtons.YesNo);

                                // If yes, get the new name
                                if (retval == DialogResult.Yes)
                                {
                                    Boolean searchError = false;
                                    Boolean tryAgain = true;
                                    // While the user wants to try, keep searching
                                    while (tryAgain)
                                    {
                                        // Get the new movie name and save
                                        name = Interaction.InputBox("Enter Movie Name or IMDb ID", row["Name"].ToString(), name, 0, 0);

                                        // Find out if the movie used the imdb ID or a title
                                        if (name.StartsWith("tt"))
                                        {
                                            isIMDB = true;
                                            imdbID = name;
                                            tryAgain = false;
                                            break;
                                        }
                                        else
                                            searchURI = "http://www.imdb.com/find?s=all&q=" + name + "&x=0&y=0";

                                        webres = null;
                                        try
                                        {
                                            webreq = (HttpWebRequest)WebRequest.Create(searchURI);
                                            webreq.Timeout = 10000;
                                            webreq.AllowAutoRedirect = true;
                                            webres = (HttpWebResponse)webreq.GetResponse();
                                        }
                                        catch (Exception e)
                                        {
                                            MessageBox.Show(e.Message);
                                            continue;
                                        }                                        
                                        resStream = webres.GetResponseStream();
                                        response = new StreamReader(resStream).ReadToEnd();
                                        // Is it a direct hit?
                                        directHitLookup = "<link rel=\"canonical\" href=\"http://www.imdb.com/title/";
                                        if (response.IndexOf(directHitLookup) != -1)
                                        {
                                            if (DEBUG)
                                                MessageBox.Show("Direct Hit");
                                            startindex = response.IndexOf("imdb.com/title/") + 15;
                                            if (startindex < 1)
                                            {
                                                throw new Exception("Error Parsing for IMDb ID");                                                
                                            }
                                            imdbID = response.Substring(startindex, 9).Trim();
                                            goto gotIMDBid;
                                        }

                                        // Find Popular Titles
                                        indexToFind = "<p><b>Popular Titles</b> (Displaying ";
                                        startindex = response.IndexOf(indexToFind) + indexToFind.Length;
                                        endindex = response.IndexOf("Result", startindex);
                                        if ((endindex - startindex > 20) || endindex - startindex < 1)
                                        {
                                            popResults = 0;
                                        }
                                        else
                                            popResults = int.Parse(response.Substring(startindex, endindex - startindex).Trim());
                                        // Find Exact Title Matches
                                        indexToFind = "<p><b>Titles (Exact Matches)</b> (Displaying ";
                                        startindex = response.IndexOf(indexToFind) + indexToFind.Length;
                                        endindex = response.IndexOf("Result", startindex);
                                        if ((endindex - startindex > 20) || endindex - startindex < 1)
                                        {
                                            exactResults = 0;
                                        }
                                        else
                                            exactResults = int.Parse(response.Substring(startindex, endindex - startindex).Trim());
                                        // Find Partial Title Matches
                                        indexToFind = "<p><b>Titles (Partial Matches)</b> (Displaying ";
                                        startindex = response.IndexOf(indexToFind) + indexToFind.Length;
                                        endindex = response.IndexOf("Result", startindex);
                                        if ((endindex - startindex > 20) || endindex - startindex < 1)
                                        {
                                            partialResults = 0;
                                        }
                                        else
                                            partialResults = int.Parse(response.Substring(startindex, endindex - startindex).Trim());

                                        // Sum all the results
                                        totalResults = popResults + exactResults + partialResults;

                                        if (totalResults == 0)
                                        {
                                            // Nothing found, try again?
                                            retval = MessageBox.Show("Nothing found.  Try again?", row["Name"].ToString(), MessageBoxButtons.YesNo);
                                            if (retval == DialogResult.No)
                                            {
                                                searchError = true;
                                                break;
                                            }
                                        }
                                        else if (totalResults >= 1)
                                        {
                                            if (DEBUG)
                                                MessageBox.Show("Found Results");

                                            string[] titles = new string[totalResults + 1];
                                            string[] years = new string[totalResults + 1];
                                            string[] imdbIDs = new string[totalResults + 1];
                                            // Get all popular results, getting ID and title
                                            if (popResults > 0)
                                            {
                                                // Start at the proper section
                                                string popResponse = response.Substring(response.IndexOf("<p><b>Popular Titles</b> (Displaying "));
                                                for (int i = 1; i <= popResults; i++)
                                                {
                                                    string nextTitle = i.ToString() + ".</td>";
                                                    int titleindex = popResponse.IndexOf(nextTitle);
                                                    string responseFromTitle = popResponse.Substring(titleindex);
                                                    int imdbIDstartindex = responseFromTitle.IndexOf("title/") + 6;
                                                    int imdbIDendindex = responseFromTitle.IndexOf("/\" onclick");
                                                    if (imdbIDendindex - imdbIDstartindex < 1)
                                                    {
                                                        throw new Exception("Error Parsing for IMDb ID");
                                                    }
                                                    imdbIDs[i] = responseFromTitle.Substring(imdbIDstartindex, imdbIDendindex - imdbIDstartindex).Trim();
                                                    string titleStart = imdbIDs[i] + "/';\">";
                                                    int titlestartindex = responseFromTitle.IndexOf(titleStart) + titleStart.Length;
                                                    int titleendindex = responseFromTitle.IndexOf("</a>");
                                                    if (titleendindex - titlestartindex < 1)
                                                    {
                                                        throw new Exception("Error Parsing Title");
                                                    }
                                                    titles[i] = responseFromTitle.Substring(titlestartindex, titleendindex - titlestartindex).Trim();
                                                    titles[i] = replaceSpecialChars(titles[i]);
                                                    // Find the year
                                                    yearRegex = new Regex("[0-9]{4}", RegexOptions.IgnoreCase);
                                                    yearMatch = yearRegex.Match(responseFromTitle.Substring(titlestartindex, 200));
                                                    // Check if this search worked
                                                    if (yearMatch.Success)
                                                        years[i] = yearMatch.ToString().Trim();
                                                    else
                                                        years[i] = "";
                                                    if (DEBUG)
                                                        MessageBox.Show("Search Result:  " + titles[i]);
                                                }
                                            }
                                            // Get all exact title results, getting ID and title
                                            if (exactResults > 0)
                                            {
                                                // Start at the proper section
                                                string exactResponse = response.Substring(response.IndexOf("<p><b>Titles (Exact Matches)</b> (Displaying "));
                                                for (int i = 1; i <= exactResults; i++)
                                                {
                                                    string nextTitle = i.ToString() + ".</td>";
                                                    int titleindex = exactResponse.IndexOf(nextTitle);
                                                    string responseFromTitle = exactResponse.Substring(titleindex);
                                                    int imdbIDstartindex = responseFromTitle.IndexOf("title/") + 6;
                                                    int imdbIDendindex = responseFromTitle.IndexOf("/\" onclick");
                                                    if (imdbIDendindex - imdbIDstartindex < 1)
                                                    {
                                                        throw new Exception("Error Parsing for IMDb ID");
                                                    }
                                                    imdbIDs[i + popResults] = responseFromTitle.Substring(imdbIDstartindex, imdbIDendindex - imdbIDstartindex).Trim();
                                                    string titleStart = imdbIDs[i + popResults] + "/';\">";
                                                    int titlestartindex = responseFromTitle.IndexOf(titleStart) + titleStart.Length;
                                                    int titleendindex = responseFromTitle.IndexOf("</a>");
                                                    if (titleendindex - titlestartindex < 1)
                                                    {
                                                        throw new Exception("Error Parsing Title");
                                                    }
                                                    titles[i + popResults] = responseFromTitle.Substring(titlestartindex, titleendindex - titlestartindex).Trim();
                                                    titles[i + popResults] = replaceSpecialChars(titles[i + popResults]);
                                                    // Find the year
                                                    yearRegex = new Regex("[0-9]{4}", RegexOptions.IgnoreCase);
                                                    yearMatch = yearRegex.Match(responseFromTitle.Substring(titlestartindex, 200));
                                                    // Check if this search worked
                                                    if (yearMatch.Success)
                                                        years[i + popResults] = yearMatch.ToString().Trim();
                                                    else
                                                        years[i + popResults] = "";
                                                    if (DEBUG)
                                                        MessageBox.Show("Search Result:  " + titles[i + popResults]);
                                                }
                                            }
                                            // Get all partial title results, getting ID and title
                                            if (partialResults > 0)
                                            {
                                                // Start at the proper section
                                                string partialResponse = response.Substring(response.IndexOf("<p><b>Titles (Partial Matches)</b> (Displaying "));
                                                for (int i = 1; i <= partialResults; i++)
                                                {
                                                    string nextTitle = i.ToString() + ".</td>";
                                                    int titleindex = partialResponse.IndexOf(nextTitle);
                                                    string responseFromTitle = partialResponse.Substring(titleindex);
                                                    int imdbIDstartindex = responseFromTitle.IndexOf("title/") + 6;
                                                    int imdbIDendindex = responseFromTitle.IndexOf("/\" onclick");
                                                    if (imdbIDendindex - imdbIDstartindex < 1)
                                                    {
                                                        throw new Exception("Error Parsing for IMDb ID");
                                                    }
                                                    imdbIDs[i + popResults + exactResults] = responseFromTitle.Substring(imdbIDstartindex, imdbIDendindex - imdbIDstartindex).Trim();
                                                    string titleStart = imdbIDs[i + popResults + exactResults] + "/';\">";
                                                    int titlestartindex = responseFromTitle.IndexOf(titleStart) + titleStart.Length;
                                                    int titleendindex = responseFromTitle.IndexOf("</a>");
                                                    if (titleendindex - titlestartindex < 1)
                                                    {
                                                        throw new Exception("Error Parsing Title");
                                                    }
                                                    titles[i + popResults + exactResults] = responseFromTitle.Substring(titlestartindex, titleendindex - titlestartindex).Trim();
                                                    titles[i + popResults + exactResults] = replaceSpecialChars(titles[i + popResults + exactResults]);
                                                    // Find the year
                                                    yearRegex = new Regex("[0-9]{4}", RegexOptions.IgnoreCase);
                                                    yearMatch = yearRegex.Match(responseFromTitle.Substring(titlestartindex, 200));
                                                    // Check if this search worked
                                                    if (yearMatch.Success)
                                                        years[i + popResults + exactResults] = yearMatch.ToString().Trim();
                                                    else
                                                        years[i + popResults + exactResults] = "";
                                                    if (DEBUG)
                                                        MessageBox.Show("Search Result:  " + titles[i + popResults + exactResults]);
                                                }
                                            }

                                            if (totalResults > 1)
                                            {
                                                // Found multiple shows, ask the user which one it is                        
                                                if (DEBUG)
                                                    MessageBox.Show("Found " + totalResults.ToString() + " result(s)");

                                                // Show a form with a combo box with the series available
                                                ComboBox comboBox = new ComboBox();
                                                comboBox.Location = new System.Drawing.Point(10, 40);
                                                comboBox.Size = new System.Drawing.Size(180, 150);
                                                for (int i = 1; i < titles.Length; i++)
                                                {
                                                    string comboLabel = titles[i].ToString() + " (" + years[i].ToString() + ")";
                                                    comboBox.Items.Add(comboLabel);
                                                }      
                                                comboBox.SelectedIndex = 0;
                                                multipleListingsForm = new Form();
                                                multipleListingsForm.Text = "Select Movie Name";
                                                multipleListingsForm.Size = new System.Drawing.Size(200, 140);
                                                multipleListingsForm.Controls.Add(comboBox);
                                                Button selectButton = new Button();
                                                Button skipButton = new Button();
                                                selectButton.Text = "Select";
                                                selectButton.Click += new System.EventHandler(this.selectButton_Click);
                                                selectButton.Location = new System.Drawing.Point(10, 70);
                                                selectButton.Size = new System.Drawing.Size(80, 30);
                                                skipButton.Text = "Skip";
                                                skipButton.Click += new System.EventHandler(this.skipButton_Click);
                                                skipButton.Location = new System.Drawing.Point(80, 70);
                                                skipButton.Size = new System.Drawing.Size(80, 30);
                                                Label label = new Label();
                                                label.Text = row["Name"].ToString();
                                                label.Location = new System.Drawing.Point(10, 10);
                                                label.Size = new System.Drawing.Size(180, 30);
                                                multipleListingsForm.Controls.Add(label);
                                                multipleListingsForm.Controls.Add(selectButton);
                                                multipleListingsForm.Controls.Add(skipButton);
                                                multipleListingsForm.StartPosition = FormStartPosition.CenterScreen;
                                                multipleListingsForm.ShowDialog();
                                                multipleListingsForm.Dispose();
                                                // Check if a movie was selected or skipped     
                                                if (DEBUG)
                                                    MessageBox.Show("Selected item: " + comboBox.SelectedIndex);
                                                int movieIndex = comboBox.SelectedIndex + 1;
                                                if (!isSkip)
                                                {
                                                    // Set the series name to the selected item
                                                    movieTitle = titles[movieIndex];
                                                    imdbID = imdbIDs[movieIndex];
                                                    tryAgain = false;
                                                    break;
                                                }
                                                else
                                                {
                                                    tryAgain = false;
                                                    searchError = true;
                                                    continue;
                                                }
                                            }
                                            else
                                            {
                                                if (DEBUG)
                                                    MessageBox.Show("Found Only One Result");

                                                imdbID = imdbIDs[1];
                                                movieTitle = titles[1];
                                                tryAgain = false;
                                                break;
                                            }                                            
                                        }
                                        else
                                        {
                                            searchError = false;
                                        }
                                    }

                                    // If there was an error in the manual search, skip
                                    if (searchError)
                                        continue;
                                }
                                else
                                {
                                    continue;
                                }
                            }

                            // Results found                            
                            else if (totalResults >= 1)
                            {
                                if (DEBUG)
                                    MessageBox.Show("Found Results");
                                
                                string[] titles = new string[totalResults + 1];
                                string[] years = new string[totalResults + 1];
                                string[] imdbIDs = new string[totalResults + 1];
                                // Get all popular results, getting ID and title
                                if (popResults > 0)
                                {
                                    // Start at the proper section
                                    string popResponse = response.Substring(response.IndexOf("<p><b>Popular Titles</b> (Displaying "));
                                    for (int i = 1; i <= popResults; i++)
                                    {
                                        string nextTitle = i.ToString() + ".</td>";
                                        int titleindex = popResponse.IndexOf(nextTitle);
                                        string responseFromTitle = popResponse.Substring(titleindex);
                                        int imdbIDstartindex = responseFromTitle.IndexOf("title/") + 6;
                                        int imdbIDendindex = responseFromTitle.IndexOf("/\" onclick");
                                        if (imdbIDendindex - imdbIDstartindex < 1)
                                        {
                                            throw new Exception("Error Parsing for IMDb ID");
                                        }
                                        imdbIDs[i] = responseFromTitle.Substring(imdbIDstartindex, imdbIDendindex - imdbIDstartindex).Trim();
                                        string titleStart = imdbIDs[i] + "/';\">";
                                        int titlestartindex = responseFromTitle.IndexOf(titleStart) + titleStart.Length;
                                        int titleendindex = responseFromTitle.IndexOf("</a>");
                                        if (titleendindex - titlestartindex < 1)
                                        {
                                            throw new Exception("Error Parsing Title");
                                        }
                                        titles[i] = responseFromTitle.Substring(titlestartindex, titleendindex - titlestartindex).Trim();
                                        titles[i] = replaceSpecialChars(titles[i]);
                                        // Find the year
                                        yearRegex = new Regex("[0-9]{4}", RegexOptions.IgnoreCase);
                                        yearMatch = yearRegex.Match(responseFromTitle.Substring(titlestartindex, 200));
                                        // Check if this search worked
                                        if (yearMatch.Success)
                                            years[i] = yearMatch.ToString().Trim();
                                        else
                                            years[i] = "";                                        
                                        if (DEBUG)
                                            MessageBox.Show("Search Result:  " + titles[i]);
                                    }
                                }
                                // Get all exact title results, getting ID and title
                                if (exactResults > 0)
                                {
                                    // Start at the proper section
                                    string exactResponse = response.Substring(response.IndexOf("<p><b>Titles (Exact Matches)</b> (Displaying "));
                                    for (int i = 1; i <= exactResults; i++)
                                    {
                                        string nextTitle = i.ToString() + ".</td>";
                                        int titleindex = exactResponse.IndexOf(nextTitle);
                                        string responseFromTitle = exactResponse.Substring(titleindex);
                                        int imdbIDstartindex = responseFromTitle.IndexOf("title/") + 6;
                                        int imdbIDendindex = responseFromTitle.IndexOf("/\" onclick");
                                        if (imdbIDendindex - imdbIDstartindex < 1)
                                        {
                                            throw new Exception("Error Parsing for IMDb ID");
                                        }
                                        imdbIDs[i + popResults] = responseFromTitle.Substring(imdbIDstartindex, imdbIDendindex - imdbIDstartindex).Trim();
                                        string titleStart = imdbIDs[i + popResults] + "/';\">";
                                        int titlestartindex = responseFromTitle.IndexOf(titleStart) + titleStart.Length;
                                        int titleendindex = responseFromTitle.IndexOf("</a>");
                                        if (titleendindex - titlestartindex < 1)
                                        {
                                            throw new Exception("Error Parsing Title");
                                        }
                                        titles[i + popResults] = responseFromTitle.Substring(titlestartindex, titleendindex - titlestartindex).Trim();
                                        titles[i + popResults] = replaceSpecialChars(titles[i + popResults]);
                                        // Find the year
                                        yearRegex = new Regex("[0-9]{4}", RegexOptions.IgnoreCase);
                                        yearMatch = yearRegex.Match(responseFromTitle.Substring(titlestartindex, 200));
                                        // Check if this search worked
                                        if (yearMatch.Success)
                                            years[i + popResults] = yearMatch.ToString().Trim();
                                        else
                                            years[i + popResults] = "";                                        
                                        if (DEBUG)
                                            MessageBox.Show("Search Result:  " + titles[i + popResults]);
                                    }
                                }
                                // Get all partial title results, getting ID and title
                                if (partialResults > 0)
                                {
                                    // Start at the proper section
                                    string partialResponse = response.Substring(response.IndexOf("<p><b>Titles (Partial Matches)</b> (Displaying "));
                                    for (int i = 1; i <= partialResults; i++)
                                    {                                        
                                        string nextTitle = i.ToString() + ".</td>";                                        
                                        int titleindex = partialResponse.IndexOf(nextTitle);                                        
                                        string responseFromTitle = partialResponse.Substring(titleindex);                                        
                                        int imdbIDstartindex = responseFromTitle.IndexOf("title/") + 6;                                        
                                        int imdbIDendindex = responseFromTitle.IndexOf("/\" onclick");                                        
                                        if (imdbIDendindex - imdbIDstartindex < 1)
                                        {
                                            throw new Exception("Error Parsing for IMDb ID");
                                        }
                                        imdbIDs[i + popResults + exactResults] = responseFromTitle.Substring(imdbIDstartindex, imdbIDendindex - imdbIDstartindex).Trim();
                                        string titleStart = imdbIDs[i + popResults + exactResults] + "/';\">";
                                        int titlestartindex = responseFromTitle.IndexOf(titleStart) + titleStart.Length;
                                        int titleendindex = responseFromTitle.IndexOf("</a>");
                                        if (titleendindex - titlestartindex < 1)
                                        {
                                            throw new Exception("Error Parsing Title");
                                        }
                                        titles[i + popResults + exactResults] = responseFromTitle.Substring(titlestartindex, titleendindex - titlestartindex).Trim();
                                        titles[i + popResults + exactResults] = replaceSpecialChars(titles[i + popResults + exactResults]);
                                        // Find the year
                                        yearRegex = new Regex("[0-9]{4}", RegexOptions.IgnoreCase);
                                        yearMatch = yearRegex.Match(responseFromTitle.Substring(titlestartindex, 200));
                                        // Check if this search worked
                                        if (yearMatch.Success)
                                            years[i + popResults + exactResults] = yearMatch.ToString().Trim();
                                        else
                                            years[i + popResults + exactResults] = "";
                                        if (DEBUG)
                                            MessageBox.Show("Search Result:  " + titles[i + popResults + exactResults]);
                                    }
                                }
                                
                                if (totalResults > 1)
                                {
                                    // Found multiple shows, ask the user which one it is                        
                                    if (DEBUG)
                                        MessageBox.Show("Found " + totalResults.ToString() + " result(s)");

                                    // Show a form with a combo box with the series available
                                    ComboBox comboBox = new ComboBox();
                                    comboBox.Location = new System.Drawing.Point(10, 40);
                                    comboBox.Size = new System.Drawing.Size(180, 150);
                                    for (int i = 1; i < titles.Length; i++)
                                    {                                        
                                        string comboLabel = titles[i].ToString() + " (" + years[i].ToString() + ")";                                        
                                        comboBox.Items.Add(comboLabel);                                        
                                    }                                    
                                    comboBox.SelectedIndex = 0;                                    
                                    multipleListingsForm = new Form();
                                    multipleListingsForm.Text = "Select Movie Name";
                                    multipleListingsForm.Size = new System.Drawing.Size(200, 140);
                                    multipleListingsForm.Controls.Add(comboBox);
                                    Button selectButton = new Button();
                                    Button skipButton = new Button();
                                    selectButton.Text = "Select";
                                    selectButton.Click += new System.EventHandler(this.selectButton_Click);
                                    selectButton.Location = new System.Drawing.Point(10, 70);
                                    selectButton.Size = new System.Drawing.Size(80, 30);
                                    skipButton.Text = "Skip";
                                    skipButton.Click += new System.EventHandler(this.skipButton_Click);
                                    skipButton.Location = new System.Drawing.Point(80, 70);
                                    skipButton.Size = new System.Drawing.Size(80, 30);
                                    Label label = new Label();
                                    label.Text = row["Name"].ToString();
                                    label.Location = new System.Drawing.Point(10, 10);
                                    label.Size = new System.Drawing.Size(180, 30);
                                    multipleListingsForm.Controls.Add(label);
                                    multipleListingsForm.Controls.Add(selectButton);
                                    multipleListingsForm.Controls.Add(skipButton);
                                    multipleListingsForm.StartPosition = FormStartPosition.CenterScreen;
                                    multipleListingsForm.ShowDialog();
                                    multipleListingsForm.Dispose();
                                    // Check if a movie was selected or skipped     
                                    if (DEBUG)
                                        MessageBox.Show("Selected item: " + comboBox.SelectedIndex);
                                    int movieIndex = comboBox.SelectedIndex + 1;
                                    if (!isSkip)
                                    {
                                        // Set the series name to the selected item
                                        movieTitle = titles[movieIndex];
                                        imdbID = imdbIDs[movieIndex];
                                    }
                                    else
                                        continue;                                    
                                }
                                else
                                {
                                    if (DEBUG)
                                        MessageBox.Show("Found Only One Result");

                                    imdbID = imdbIDs[1];
                                    movieTitle = titles[1];
                                }

                                if (DEBUG)
                                    MessageBox.Show("Found: " + movieTitle + " with ID: " + imdbID);      
                            }
                            else
                            {
                                throw new Exception("Unknown Error");
                            }
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("Error: " + e.Message);
                            continue;
                        }
                    gotIMDBid: if (imdbID == "") continue;
                    }                

                    // At this point, we have the IMDb ID
                    if (DEBUG)
                        MessageBox.Show("Using IMDb ID: " + imdbID);                    

                    // Now get movie info                    
                    searchURI = "http://www.imdb.com/title/" + imdbID + "/";
                    webres = null;
                    try
                    {
                        webreq = (HttpWebRequest)WebRequest.Create(searchURI);
                        webreq.Timeout = 10000;
                        webreq.AllowAutoRedirect = true;
                        webres = (HttpWebResponse)webreq.GetResponse();                        
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                        continue;
                    }
                    resStream = webres.GetResponseStream();
                    response = new StreamReader(resStream).ReadToEnd();
                    
                    // Find the title                    
                    indexToFind = "<title>";
                    startindex = response.IndexOf(indexToFind) + indexToFind.Length;
                    endindex = response.IndexOf("(", startindex);
                    if (endindex - startindex < 1)
                    {
                        throw new Exception("Error Parsing Title");
                    }
                    movieTitle = replaceSpecialChars(response.Substring(startindex, endindex - startindex));
                    // Find the year
                    yearRegex = new Regex("[0-9]{4}", RegexOptions.IgnoreCase);
                    yearMatch = yearRegex.Match(response.Substring(startindex, 200));                    
                    // Check if this search worked
                    if (yearMatch.Success)
                    {
                        movieYear = yearMatch.ToString().Trim();
                    }
                    else
                    {
                        //throw new Exception("Error Parsing Year");
                        // Get the year from the user
                        movieYear = Interaction.InputBox("No Year Found. Enter Movie Year", movieTitle, null, 0, 0);
                    }                    
                    // Find the plot
                    indexToFind = "Plot:</h5>";
                    startindex = response.IndexOf(indexToFind) + indexToFind.Length + 1 + 27;
                    endindex = response.IndexOf("<a", startindex);
                    if (endindex - startindex < 1 || endindex - startindex > 2000)
                    {
                        //throw new Exception("Error Parsing Plot");
                        // Get the plot from the user
                        moviePlot = Interaction.InputBox("No Plot Found. Enter Movie Plot", movieTitle, null, 0, 0);
                    }
                    else
                    {
                        moviePlot = replaceSpecialChars(response.Substring(startindex, endindex - startindex));
                    }
                    if (multidisc)
                        moviePlot = "Disc " + discNum + ": " + moviePlot;
                    // Find the genre
                    indexToFind = "Genre:</h5>";
                    startindex = response.IndexOf(indexToFind) + 27;                    
                    string subresp = response.Substring(startindex, 1000);                    
                    startindex = subresp.IndexOf("/\">") + 3;
                    endindex = subresp.IndexOf("</a>", startindex);
                    if (endindex - startindex < 1 || endindex - startindex > 50)
                    {
                        //throw new Exception("Error Parsing Genre");
                        // Get the genre from the user
                        movieGenre = Interaction.InputBox("No Genre Found. Enter Movie Genre", movieTitle, null, 0, 0);
                    }
                    else
                        movieGenre = subresp.Substring(startindex, endindex - startindex);
                    // Find the actors
                    indexToFind = "Cast</h3>";
                    startindex = response.IndexOf(indexToFind);
                    if (startindex != -1)
                    {                        
                        subresp = response.Substring(startindex, 10000);
                        movieActors = "";
                        int loops = 0;
                        while (loops <= 80)
                        {
                            // Get next name
                            startindex = subresp.IndexOf("link=/name");
                            if (DEBUG)
                                MessageBox.Show("sIndex: " + startindex);
                            // Found nothing, enter manually
                            if (startindex == -1)
                            {
                                if (loops == 0)
                                    movieActors = Interaction.InputBox("No Actors Found. Enter Movie Actors", movieTitle, null, 0, 0);
                                break;
                            }
                            else
                            {
                                subresp = subresp.Substring(startindex + 10);                                
                                endindex = subresp.IndexOf("</a>");                                
                                if (endindex == -1)
                                    break;

                                // Find an actor, not the link
                                if (endindex < 75)
                                {
                                    // Found Character Name
                                    if (DEBUG) 
                                        MessageBox.Show("Found a name");                                                                
                                    movieActors = movieActors + subresp.Substring(15, endindex - 15) + ", ";                                    
                                }
                            }
                            loops++;
                        }
                        // Trim off the last comma
                        if (movieActors.LastIndexOf(',') != -1)
                            movieActors = movieActors.Remove(movieActors.LastIndexOf(',')).Trim();
                        //MessageBox.Show(movieActors);
                    }
                    else
                        movieActors = "";
                    // Find the date
                    indexToFind = "Date:</h5>";                    
                    startindex = response.IndexOf(indexToFind) + indexToFind.Length + 1 + 27;                    
                    endindex = response.IndexOf("(", startindex);                    
                    if (endindex - startindex < 1 || endindex - startindex > 50)
                    {
                        //throw new Exception("Error Parsing Date");
                        movieDate = movieYear + "-01-01";
                    }
                    else
                    {                        
                        string date = response.Substring(startindex, endindex - startindex);                        
                        string[] dateParts = date.Split(' ');
                        string month = "January";
                        string day = "01";                        
                        if (dateParts.Length == 2)
                        {
                            // Only Year
                            movieDate = movieYear;                            
                        }
                        else if (dateParts.Length == 3)
                        {
                            // Month and Year
                            month = dateParts[0].Trim();
                        }
                        else if (dateParts.Length == 4)
                        {
                            // Day Month Year
                            // Adjust length of day
                            if (dateParts[0].Trim().Length == 1)
                                day = "0" + dateParts[0].Trim();
                            month = dateParts[1].Trim();
                        }
                        else
                        {
                            throw new Exception("Error Forming Date");
                        }
                        movieDate = movieYear;                        
                        // Lookup Month
                        if (month.Equals("January"))
                            movieDate = movieDate + "-01-" + day;
                        else if (month.Equals("February"))
                            movieDate = movieDate + "-02-" + day;
                        else if (month.Equals("March"))
                            movieDate = movieDate + "-03-" + day;
                        else if (month.Equals("April"))
                            movieDate = movieDate + "-04-" + day;
                        else if (month.Equals("May"))
                            movieDate = movieDate + "-05-" + day;
                        else if (month.Equals("June"))
                            movieDate = movieDate + "-06-" + day;
                        else if (month.Equals("July"))
                            movieDate = movieDate + "-07-" + day;
                        else if (month.Equals("August"))
                            movieDate = movieDate + "-08-" + day;
                        else if (month.Equals("September"))
                            movieDate = movieDate + "-09-" + day;
                        else if (month.Equals("October"))
                            movieDate = movieDate + "-10-" + day;
                        else if (month.Equals("November"))
                            movieDate = movieDate + "-11-" + day;
                        else if (month.Equals("December"))
                            movieDate = movieDate + "-12-" + day;
                        else
                            throw new Exception("Error Forming Date");
                    }

                    // Set title as Movies for now.  This needs to be fixed so DisplayTitle gets set
                    // to "Movies" and Title get set to the movie name.
                    row["Title"] = "Movies";
                    
                    // Set Episode Name as the movie      
                    if (movieTitle.IndexOf("The") == 0)
                        movieTitle = movieTitle.Substring(4).Trim() + ", The";
                    if (movieTitle.IndexOf("A ") == 0)
                        movieTitle = movieTitle.Substring(2).Trim() + ", A";
                    if (movieTitle.IndexOf("An ") == 0)
                        movieTitle = movieTitle.Substring(3).Trim() + ", An";
                    row["EpisodeTitle"] = movieTitle;

                    // Set the Description
                    row["EpisodeDescription"] = moviePlot;

                    // Set the Genre
                    row["Genre"] = movieGenre;

                    // Set the Actors
                    row["Actors"] = movieActors;

                    // Set the Date
                    //string firstAired = "2099-01-01";
                    row["ActualStart"] = movieDate;
                    row["OriginalAirDate"] = movieDate.Replace("-", "");
                    row["MovieYear"] = movieYear;

                    updatedRowCount++;
                    count++;
                    Thread.Sleep(500);
                    progressForm.Update();
                }

                progressForm.Close();

                return (updatedRowCount > 0);
            }

            private void selectButton_Click(object sender, System.EventArgs e)
            {
                multipleListingsForm.Close();
                isSkip = false;
            }

            private void skipButton_Click(object sender, System.EventArgs e)
            {
                multipleListingsForm.Close();
                isSkip = true;
            }

            private string replaceSpecialChars(string s) 
            {                
                s = s.Replace("&#x22;", "\"");
                s = s.Replace("&#x26;", "&");
                s = s.Replace("&#x27;", "'");
                s = s.Replace("&#xB7;", "·");                
                return s;
            }

            #endregion
        }
    }
}