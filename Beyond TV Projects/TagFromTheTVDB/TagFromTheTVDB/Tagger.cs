using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ThoughtLabs
{
    namespace BTVShowInfo
    {
        class Tagger
        {
            public string seriesName;
            public string seasonNumber;
            public string episodeNumber;
            public string originalAirDate;

            public Tagger()
            {
            }

            public Boolean tagFilename(string filename)
            {
                string fullFileName = filename.Replace(".", " ").Replace("_", " ");

                // Get Season Number
                Regex seasonNumberRegex = new Regex("S[0-9]+", RegexOptions.IgnoreCase);
                Match seasonMatch = seasonNumberRegex.Match(fullFileName);
                Match episodeMatch = null;
                // Check if this tagging worked
                if (seasonMatch.Success)
                {
                    seasonNumber = seasonMatch.ToString().Substring(1);
                    seasonNumber = seasonNumber.TrimStart('0');                    

                    // Get Episode Number
                    Regex episodeNumberRegex = new Regex("E[0-9]+", RegexOptions.IgnoreCase);
                    episodeMatch = episodeNumberRegex.Match(fullFileName);
                    // Check if worked
                    if (episodeMatch.Success)
                    {
                        episodeNumber = episodeMatch.ToString().Substring(1);
                        episodeNumber = episodeNumber.TrimStart('0');                        
                    }
                    else
                    {                        
                        return false;
                    }
                }
                else
                {
                    // Try next matching pattern
                    // Get Season Number
                    seasonNumberRegex = new Regex("[0-9]{3,4}");
                    seasonMatch = seasonNumberRegex.Match(fullFileName);
                    // Check if this tagging worked
                    if (seasonMatch.Success)
                    {                        
                        if (seasonMatch.ToString().Length == 3)
                        {
                            seasonNumber = seasonMatch.ToString().Remove(1);
                            episodeNumber = seasonMatch.ToString().Substring(1);
                            seasonNumber = seasonNumber.TrimStart('0');
                            episodeNumber = episodeNumber.TrimStart('0');                            
                        }
                        else if (seasonMatch.ToString().Length == 4)
                        {
                            seasonNumber = seasonMatch.ToString().Remove(2);
                            episodeNumber = seasonMatch.ToString().Substring(2);
                            seasonNumber = seasonNumber.TrimStart('0');
                            episodeNumber = episodeNumber.TrimStart('0');                            
                        }
                        else
                        {                            
                            return false;
                        }
                    }
                    else
                    {
                        // Try next matching pattern
                        // Get Season Number
                        seasonNumberRegex = new Regex("[0-9]{1,2}x[0-9]{1,2}");
                        seasonMatch = seasonNumberRegex.Match(fullFileName);
                        // Check if this tagging worked
                        if (seasonMatch.Success)
                        {                            
                            seasonNumber = seasonMatch.ToString().Split('x')[0];
                            episodeNumber = seasonMatch.ToString().Split('x')[1];
                            seasonNumber = seasonNumber.TrimStart('0');
                            episodeNumber = episodeNumber.TrimStart('0');                            
                        }
                        else
                        {                            
                            return false;
                        }
                    }
                }

                // We should have the series name now            
                seriesName = fullFileName.Remove(seasonMatch.Index);

                return true;
            }
        }
    }
}
