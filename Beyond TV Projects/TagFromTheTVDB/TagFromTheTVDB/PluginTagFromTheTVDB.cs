/*
 * TheTVDB.com Tagger for BTVShowInfo
 * 
 * Copyright (c) 2008 Thomas Marullo
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
using System.Globalization;
using System.Windows.Forms;
using System.Xml.XPath;
using System.Xml;
using System.Threading;
using System.Collections.Generic;
using Microsoft.VisualBasic;
using ThoughtLabs.BTVShowInfo.PluginInterface;

namespace ThoughtLabs {
	namespace BTVShowInfo {
		/// <summary>
		/// Summary description for TagFromTheTVDB.
		/// </summary>
		public class PluginTagFromTheTVDB : IShowInfoEditor {

            private Form progressForm;
            private Form multipleListingsForm;
            private Boolean isSkip;            

			/// <summary>
			/// Constructor
			/// </summary>
            public PluginTagFromTheTVDB()
            {
			}


			#region IShowInfoEditor Members

			/// <summary>
			/// Store the host and add menu items
			/// </summary>
			/// <param name="host">The host</param>
			public void Initialize( IPluginHost host ) {
				host.AddMenuItem( "Import show metadata from TheTVDB.com", this );
			}

			/// <summary>
			/// Do work on the data
			/// </summary>
			/// <param name="table">The datatable to read and edit</param>
			/// <param name="nav">XPathNavigator for column info</param>
			/// <returns>true if the edit was successful</returns>
			public Boolean EditShowInfo( ref DataTable table, XPathNavigator nav ) {
				Int32 updatedRowCount = 0;
                Boolean DEBUG = false;

                // List to save the Series names and ID
                List<string> seriesNameList = new List<string>();
                List<string> formalNameList = new List<string>();
                List<string> seriesIDList = new List<string>();

                XmlTextReader reader;
                XmlDocument doc;
                string URLString;
                Boolean isTagged;

                progressForm = new Form();
                progressForm.Size = new System.Drawing.Size(400, 100);
                progressForm.Text = "TheTVDb.com Tagger";
                progressForm.StartPosition = FormStartPosition.CenterScreen;
                Label progressLabel = new Label();
                progressLabel.Size = new System.Drawing.Size(300, 50);
                progressForm.Controls.Add(progressLabel);
                progressForm.Show();

                int count = 0;
				foreach ( DataRow row in table.Rows ) {
                    count++;
                    Thread.Sleep(500);

					// Parse filename to get Series name                    
                    Tagger tagger = new Tagger();                    

                    // Set variables
                    string seriesID;
                    string formalName;
                    bool isBTVshow = false;
                    
                    if (DEBUG)
                        MessageBox.Show("Tagging: " + row["Name"].ToString());

                    // See if it is a native BTV Recording                    
                    double num;
                    bool isNum = double.TryParse(row["Channel"].ToString().Trim(), out num);
                    if (isNum)
                    {
                        if (DEBUG)
                            MessageBox.Show("BTV Native Recording");
                        isBTVshow = true;                        
                        tagger.seriesName = row["Title"].ToString();
                        tagger.originalAirDate = row["OriginalAirDate"].ToString().Insert(6, "-").Insert(4, "-");
                        tagger.seasonNumber = "0";
                        tagger.episodeNumber = "0";
                        isTagged = true;
                    }
                    else
                    {
                        if (DEBUG)
                            MessageBox.Show("Not a BTV Native Recording");
                        // Parse the filename
                        isTagged = tagger.tagFilename(row["Name"].ToString());
                    }
                    
                    // Check if the tagging worked, otherwise, continue to the next item
                    if (!isTagged)
                    {
                        // Could not parse filename
                        // Ask user if they want to manually enter the show info             
                        DialogResult retval = MessageBox.Show("Could not parse " + row["Name"].ToString() + "\nManually enter show info?", row["Name"].ToString(), MessageBoxButtons.YesNo);
                        if (retval == DialogResult.Yes)
                        {
                            // Get the info manually
                            tagger.seriesName = Interaction.InputBox("Enter Series Name", row["Name"].ToString(), null, 0, 0);
                            tagger.seasonNumber = Interaction.InputBox("Enter Season Number", row["Name"].ToString(), null, 0, 0);
                            tagger.episodeNumber = Interaction.InputBox("Enter Episode Number", row["Name"].ToString(), null, 0, 0);

                            // Check if the cancel button was hit or if it was empty
                            if (tagger.seriesName.Length == 0 || tagger.seasonNumber.Length == 0 || tagger.episodeNumber.Length == 0)
                                continue;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    progressLabel.Text = "Tagging " + count + "/" + table.Rows.Count + ": " + row["Name"].ToString();
                
                    if (DEBUG)
                    {
                        MessageBox.Show("Title: " + tagger.seriesName);
                        MessageBox.Show("Season: " + tagger.seasonNumber);
                        MessageBox.Show("Episode: " + tagger.episodeNumber);
                    }                    

                    // Check array first
                    int indexOfShowInArray = seriesNameList.IndexOf(tagger.seriesName);
                    if (indexOfShowInArray != -1)
                    {
                        if (DEBUG)
                            MessageBox.Show(tagger.seriesName + " exists in memory");
                        formalName = formalNameList[indexOfShowInArray];
                        seriesID = seriesIDList[indexOfShowInArray];
                    }
                    else
                    {
                        // Search theTVDB.com API for the series ID   
                        URLString = "http://www.thetvdb.com/api/GetSeries.php?seriesname=" + tagger.seriesName + "&language=en";
                        reader = new XmlTextReader(URLString);
                        doc = new XmlDocument();
                        try
                        {
                            doc.Load(reader);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("Bad URL: " + URLString + "\nSkipping.", row["Name"].ToString());
                            continue;
                        }
                        reader.Close();

                        // Get the series name and the index in the XML if there are multiple
                        XmlNodeList seriesNamesList = doc.GetElementsByTagName("SeriesName");
                        int seriesCount = seriesNamesList.Count;
                        int seriesIndex = 0;
                        // Check if any items were found
                        if (seriesCount == 0)
                        {
                            formalName = "";
                            // Nothing found
                            // Ask user if they want to manually search for show name or skip                            
                            DialogResult retval = MessageBox.Show("Nothing found.  Search for show manually?", row["Name"].ToString(), MessageBoxButtons.YesNo);
                            if (retval == DialogResult.Yes)
                            {
                                Boolean searchError = false;
                                Boolean tryAgain = true;
                                // While the user wants to try, keep searching
                                while (tryAgain)
                                {
                                    // Get the new series name and save
                                    string newSeriesName = Interaction.InputBox("Enter Series Name", row["Name"].ToString(), null, 0, 0);                                    

                                    // Search theTVDB.com API for the series ID   
                                    URLString = "http://www.thetvdb.com/api/GetSeries.php?seriesname=" + newSeriesName + "&language=en";
                                    reader = new XmlTextReader(URLString);
                                    doc = new XmlDocument();
                                    try
                                    {
                                        doc.Load(reader);
                                    }
                                    catch (Exception e)
                                    {
                                        MessageBox.Show("Bad URL: " + URLString + "\nSkipping.", row["Name"].ToString());
                                        searchError = true;
                                        break;                                        
                                    }
                                    reader.Close();

                                    // Get the series name and the index in the XML if there are multiple
                                    seriesNamesList = doc.GetElementsByTagName("SeriesName");
                                    seriesCount = seriesNamesList.Count;
                                    seriesIndex = 0;                                    

                                    // Check if any items were found this time
                                    if (seriesCount == 0)
                                    {
                                        // Nothing found, try again?
                                        retval = MessageBox.Show("Nothing found.  Try again?", row["Name"].ToString(), MessageBoxButtons.YesNo);
                                        if (retval == DialogResult.No)
                                        {
                                            searchError = true;
                                            break;                                
                                        }
                                    }
                                    else if (seriesCount == 1)
                                    {
                                        // Only one show found, so assume this is it                 
                                        if (DEBUG)
                                            MessageBox.Show("Found: " + seriesNamesList.Item(0).InnerText);
                                        formalName = seriesNamesList.Item(0).InnerText;
                                        tryAgain = false;
                                    }
                                    else
                                    {
                                        // Found multiple shows, ask the user which one it is                        
                                        if (DEBUG)
                                            MessageBox.Show("Found multiple results");

                                        // Show a form with a combo box with the series available
                                        ComboBox comboBox = new ComboBox();
                                        comboBox.Location = new System.Drawing.Point(10, 40);
                                        comboBox.Size = new System.Drawing.Size(180, 150);
                                        for (int i = 0; i < seriesNamesList.Count; i++)
                                        {
                                            //Console.WriteLine((i + 1).ToString() + " :" + seriesNamesList.Item(i).InnerText);
                                            comboBox.Items.Add(seriesNamesList.Item(i).InnerText);
                                        }
                                        comboBox.SelectedIndex = 0;
                                        multipleListingsForm = new Form();
                                        multipleListingsForm.Text = "Select Series Name";
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
                                        // Check if a series was selected or skipped                        
                                        seriesIndex = comboBox.SelectedIndex;
                                        if (!isSkip)
                                        {
                                            // Set the series name to the selected item
                                            formalName = seriesNamesList.Item(seriesIndex).InnerText;
                                            tryAgain = false;
                                        }
                                        else
                                            // Skip search
                                            searchError = true;
                                            break;  
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
                        else if (seriesCount == 1)
                        {
                            // Only one show found, so assume this is it                 
                            if (DEBUG)
                                MessageBox.Show("Found: " + seriesNamesList.Item(0).InnerText);
                            formalName = seriesNamesList.Item(0).InnerText;
                        }
                        else
                        {
                            // Found multiple shows, ask the user which one it is                        
                            if (DEBUG)
                                MessageBox.Show("Found multiple results");

                            // Show a form with a combo box with the series available
                            ComboBox comboBox = new ComboBox();
                            comboBox.Location = new System.Drawing.Point(10, 40);
                            comboBox.Size = new System.Drawing.Size(180, 150);
                            for (int i = 0; i < seriesNamesList.Count; i++)
                            {
                                //Console.WriteLine((i + 1).ToString() + " :" + seriesNamesList.Item(i).InnerText);
                                comboBox.Items.Add(seriesNamesList.Item(i).InnerText);
                            }
                            comboBox.SelectedIndex = 0;
                            multipleListingsForm = new Form();
                            multipleListingsForm.Text = "Select Series Name";
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
                            // Check if a series was selected or skipped                        
                            seriesIndex = comboBox.SelectedIndex;
                            if (!isSkip)
                            {
                                // Set the series name to the selected item
                                formalName = seriesNamesList.Item(seriesIndex).InnerText;
                            }
                            else
                                // Skip search
                                continue;
                        }

                        // Get series ID from the XML                                
                        XmlNodeList seriesIDXMLList = doc.GetElementsByTagName("seriesid");
                        seriesID = seriesIDXMLList.Item(seriesIndex).InnerText;
                        if (DEBUG)
                            MessageBox.Show("Using " + tagger.seriesName + " with TVDB_ID: " + seriesID);

                        // Store tagged name and series ID
                        seriesNameList.Add(tagger.seriesName);
                        formalNameList.Add(formalName);
                        seriesIDList.Add(seriesID);
                    }

                    // Should have the Series name at this point //

                    // First check if BTV Recorded Show and get the details
                    if (isBTVshow)
                    {
                        URLString = "http://www.thetvdb.com/api/8DB53EF83E7E8308/series/" +
                                     seriesID + "/all/en.xml";
                        if (DEBUG)
                            MessageBox.Show("URL: " + URLString);
                        reader = new XmlTextReader(URLString);
                        doc = new XmlDocument();
                        try
                        {
                            doc.Load(reader);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("Bad URL: " + URLString + "\nSkipping.", row["Name"].ToString());
                            continue;
                        }
                        reader.Close();
                        
                        // Get the episode list
                        XmlNodeList episodeList = doc.GetElementsByTagName("Episode");
                        int episodeCount = episodeList.Count;
                        if (DEBUG)
                            MessageBox.Show("Found " + episodeCount.ToString() + " episode(s)");                        
                        // Go through each episode and find if the original air date exists
                        string[] seasonSearch = new string[10];
                        string[] episodeSearch = new string[10];
                        string[] episodeNameSearch = new string[10];
                        int dateMatches = 0;
                        for (int i = 0; i < episodeCount; i++)
                        {
                            string dateSearch = episodeList.Item(i).SelectSingleNode("FirstAired").InnerText.ToString();
                            if (dateSearch == tagger.originalAirDate)
                            {                                
                                seasonSearch[dateMatches] = episodeList.Item(i).SelectSingleNode("SeasonNumber").InnerText.ToString();
                                episodeSearch[dateMatches] = episodeList.Item(i).SelectSingleNode("EpisodeNumber").InnerText.ToString();
                                episodeNameSearch[dateMatches] = episodeList.Item(i).SelectSingleNode("EpisodeName").InnerText.ToString();
                                if (DEBUG)
                                {
                                    MessageBox.Show("Found match to original air date: " + episodeNameSearch[dateMatches] + " S" + seasonSearch[dateMatches] + "E" + episodeSearch[dateMatches]);                                    
                                }
                                dateMatches++;
                            }
                        }
                        if (DEBUG)
                            MessageBox.Show("Found " + dateMatches.ToString() + " matches");
                        // Found no matches
                        if (dateMatches <= 0)
                        {
                            DialogResult retval = MessageBox.Show("No match found for " + row["Name"].ToString() + " on " + tagger.originalAirDate + "\nManually enter show season and episode info?", row["Name"].ToString(), MessageBoxButtons.YesNo);
                            if (retval == DialogResult.Yes)
                            {
                                // Get the info manually                                
                                tagger.seasonNumber = Interaction.InputBox("Enter Season Number", row["Name"].ToString(), null, 0, 0);
                                tagger.episodeNumber = Interaction.InputBox("Enter Episode Number", row["Name"].ToString(), null, 0, 0);

                                // Check if the cancel button was hit or if it was empty
                                if (tagger.seasonNumber.Length == 0 || tagger.episodeNumber.Length == 0)
                                    continue;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        // Found one match
                        else if (dateMatches == 1)
                        {
                            tagger.seasonNumber = seasonSearch[0];
                            tagger.episodeNumber = episodeSearch[0];
                        }
                        // Found multiple matches, ask user
                        else
                        {
                            // Show a form with a combo box with the episodes available
                            ComboBox comboBox = new ComboBox();
                            comboBox.Location = new System.Drawing.Point(10, 40);
                            comboBox.Size = new System.Drawing.Size(180, 150);
                            for (int i = 0; i < dateMatches; i++)
                            {
                                comboBox.Items.Add("Season: " + seasonSearch[i] + ", Episode: " + episodeSearch[i] + ", " + episodeNameSearch[i]);
                            }
                            comboBox.SelectedIndex = 0;
                            multipleListingsForm = new Form();
                            multipleListingsForm.Text = "Select Episode";
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
                            // Check if a series was selected or skipped                        
                            int episodeIndex = comboBox.SelectedIndex;
                            if (!isSkip)
                            {
                                // Set the series name to the selected item
                                tagger.seasonNumber = seasonSearch[episodeIndex];
                                tagger.episodeNumber = episodeSearch[episodeIndex];
                            }
                            else
                                // Skip search
                                continue;
                        }
                    }

                    // Get episode info from TheTVDB.com API
                    URLString = "http://www.thetvdb.com/api/8DB53EF83E7E8308/series/" +
                                seriesID + "/default/" + tagger.seasonNumber + "/" + tagger.episodeNumber + "/en.xml";
                    if (DEBUG)
                        MessageBox.Show("URL: " + URLString);
                    reader = new XmlTextReader(URLString);
                    doc = new XmlDocument();
                    try
                    {
                        doc.Load(reader);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("Bad URL: " + URLString + "\nSkipping.", row["Name"].ToString());
                        continue;
                    }
                    reader.Close();

                    // Set series name
                    row["Title"] = formalName;

                    // Set Episode Name            
                    string episodeName = doc.GetElementsByTagName("EpisodeName").Item(0).InnerText;
                    row["EpisodeTitle"] = episodeName;                    
                    if (DEBUG)
                        MessageBox.Show("Episode Name: " + episodeName);

                    // Set Episode Date            
                    string firstAired = doc.GetElementsByTagName("FirstAired").Item(0).InnerText;
                    row["ActualStart"] = firstAired;                    
                    row["OriginalAirDate"] = firstAired.Replace("-", "");
                    if (DEBUG)
                        MessageBox.Show("Episode Aired: " + firstAired);

                    // Set Episode Overview            
                    string overview = doc.GetElementsByTagName("Overview").Item(0).InnerText;                    
                    if (DEBUG)
                        MessageBox.Show("Overview: " + overview);
                    // Append Series and Episode numbers
                    overview = overview + " - Season " + tagger.seasonNumber + ", Episode " + tagger.episodeNumber + ".";
                    row["EpisodeDescription"] = overview;

                    updatedRowCount++;                       
                    Thread.Sleep(500);
                    progressForm.Update();
				}
                progressForm.Close();

				return ( updatedRowCount > 0 );
                
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

			#endregion
		}
	}    
}