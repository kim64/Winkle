﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Net;

namespace Winkle
{
    public class VersionCheck
    {
        private string winkleVersion = "0.2";
        private XmlDocument WinkleResponse = new XmlDocument();
        public string errorCode = "";
        public string versionToUse = "CurrentStableVersion";
        public bool showWindowIfUpdateAvailable = true;
        private string applicatenName = "";
        private string updateInfoUrl = "";
        private List<DescriptionOfChanges> changeLog = new List<DescriptionOfChanges>();

        public VersionCheck(string appName, string updateUrl)
        {
            applicatenName = appName;
            updateInfoUrl = updateUrl;

        }

        public UpdateInfo checkForUpdate(System.Reflection.Assembly assembly, bool includeBetaVersions) {
                Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                
                return _doUpdateCheck(v.Major, v.Minor, v.Build, v.Revision,false);
        }

        public UpdateInfo checkForUpdate(int currentMajorVersion, int currentMinorVersion, int currentBuild, int currentRevision, bool includeBetaVersions) {
            return _doUpdateCheck(currentMajorVersion, currentMinorVersion, currentBuild, currentRevision, includeBetaVersions);
        }

        private UpdateInfo _doUpdateCheck(int currentMajorVersion, int currentMinorVersion, int currentBuild, int currentRevision, bool includeBetaVersions) {
            UpdateInfo returnCode = new UpdateInfo();
            WinkleResponse = getUpdateFile(updateInfoUrl);
            if (WinkleResponse == null)
            {
                returnCode.errorCode = 1;
                returnCode.updateInfoRetrievalSuccessfull = false;
                returnCode.errorTitle = "Retrieval of update information failed";
                returnCode.errorDescription = "Cannot download the update file description from " + updateInfoUrl;
                return returnCode;
            }
            int major = getLatestMajorVersion(includeBetaVersions);
            int minor = getLatestMinorVersion(includeBetaVersions);
            int build = getLatestBuild(includeBetaVersions);
            int revision = getLatestRevision(includeBetaVersions);



            getChangeLog();
           // return returnCode;
            bool updateAvailable = false;

            if (currentMajorVersion < major)
            {
                updateAvailable = true;
            }
            else if (currentMajorVersion == major && currentMinorVersion < minor)
            {
                updateAvailable = true;
            }
            else if (currentMajorVersion == major && currentMinorVersion == minor && currentBuild < build)
            {
                updateAvailable = true;
            }
            else if (currentMajorVersion == major && currentMinorVersion == minor && currentBuild == build && currentRevision < revision)
            {
                updateAvailable = true;
            }

            if (updateAvailable)
            {
                returnCode.updateAvailable = true;
                returnCode.updateInfoRetrievalSuccessfull = true;
                returnCode.updateMajor = major;
                returnCode.updateMinor = minor;
                returnCode.updateBuild = build;
                
                returnCode.manualDownloadUrl = new System.Uri(getDownloadLinkUrl(includeBetaVersions));

                if(showWindowIfUpdateAvailable) {
                    Winkle.UpdateNotification myUpdateNotification = new UpdateNotification();
                    myUpdateNotification.setVersion(applicatenName, major.ToString() + "." + minor.ToString() + "." + build.ToString() + "." + revision.ToString());
                    myUpdateNotification.setDownloadLink(returnCode.manualDownloadUrl.ToString());
                    string myDescription = "";
                    foreach (DescriptionOfChanges myChanges in changeLog) {
                        myDescription += myChanges.updateDescription;
                        myDescription += "\n--------\n";
                    }
                    myUpdateNotification.setDescription(myDescription);
                    myUpdateNotification.Show();
                }

                return returnCode;
            }

            returnCode.updateInfoRetrievalSuccessfull = true;
            return returnCode;
        }

        private int getLatestMajorVersion(bool includeBetaVersion)
        {
            return getSingleVersionNumber("Major", includeBetaVersion);
        }

        private int getLatestMinorVersion(bool includeBetaVersion)
        {
            return getSingleVersionNumber("Minor", includeBetaVersion);
        }

        private int getLatestBuild(bool includeBetaVersion)
        {
            return getSingleVersionNumber("Build", includeBetaVersion);
        }

        private int getLatestRevision(bool includeBetaVersion)
        {
            return getSingleVersionNumber("Revision", includeBetaVersion);
        }

        private int getSingleVersionNumber(string partOfNumber, bool includeBetaVersion)
        {
            string stringForNumber = WinkleResponse.SelectNodes("Winkle/StableVersions/StableVersion/" + partOfNumber).Item(0).InnerText;
            int returnCode = 0;
            try
            {
                returnCode = Convert.ToInt32(stringForNumber);
            }
            catch (Exception e)
            {
                errorCode = "Error parsing update file - cannot parse " + partOfNumber + " version: " + e.Message;
            }
            return returnCode;
        }

        private string getDownloadLinkUrl(bool includeBetaVersion)
        {
            return WinkleResponse.SelectNodes("Winkle/StableVersions/StableVersion/UpdateFileUrl").Item(0).InnerText;
        }


        private string getUpdateDescription(bool includeBetaVersion)
        {
            return WinkleResponse.SelectNodes("Winkle/StableVersions/StableVersion/NewInThisVersion").Item(0).InnerText;
        }

        private void getChangeLog() {
            XmlNodeList allChanges = WinkleResponse.SelectNodes("Winkle/StableVersions/StableVersion");
            foreach (XmlNode thisChange in allChanges)
            {
                DescriptionOfChanges thisVersion = new DescriptionOfChanges();
                thisVersion.updateDescription = thisChange["NewInThisVersion"].InnerText;
                changeLog.Add(thisVersion);
            }
        }

        private XmlDocument getUpdateFile(string updateUrl)
        {
            XmlDocument Winkle_XMLdoc = null;
            if(updateUrl.StartsWith("file",true,System.Globalization.CultureInfo.CurrentCulture)) {
                FileWebRequest Winkle_Request;
                FileWebResponse Winkle_Response = null;
                
                try
                {
                    Winkle_Request = (FileWebRequest)WebRequest.Create(string.Format(updateUrl));
                    //Winkle_Request.UserAgent = @"Winkle automatic update system " + winkleVersion;
                    Winkle_Response = (FileWebResponse)Winkle_Request.GetResponse();
                    Winkle_XMLdoc = new XmlDocument();
                    Winkle_XMLdoc.Load(Winkle_Response.GetResponseStream());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                if (Winkle_Response != null)
                {
                    Winkle_Response.Close();
                }
            }
            else 
            {
                   HttpWebRequest Winkle_Request;
                   HttpWebResponse Winkle_Response = null;
                    
                    try
                    {
                        Winkle_Request = (HttpWebRequest)WebRequest.Create(string.Format(updateUrl));
                        Winkle_Request.UserAgent = @"Winkle automatic update system " + winkleVersion;
                        Winkle_Response = (HttpWebResponse)Winkle_Request.GetResponse();
                        Winkle_XMLdoc = new XmlDocument();
                        Winkle_XMLdoc.Load(Winkle_Response.GetResponseStream());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    if (Winkle_Response != null)
                    {
                        Winkle_Response.Close();
                    }
                }
        
            return Winkle_XMLdoc;
        }
    }
}
