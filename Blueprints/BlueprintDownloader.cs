using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading;

namespace StarcoreDiscordBot
{
    class BlueprintDownloader
    {

        const string InfoURL = "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/";
        const string RequestURL = "https://backend-03-prd.steamworkshopdownloader.io/api/download/request";

        const string StatusURL = "https://backend-03-prd.steamworkshopdownloader.io/api/download/status";
        const string DownloadURL = "https://backend-03-prd.steamworkshopdownloader.io/api/download/transmit?uuid=";

        public bool IsValid = true;
        public string Error = null;

        private ulong BpId;
        private int GID;

        public BlueprintDownloader(ulong id)
        {
            this.BpId = id;
            GID = (int)(Utils.Rng.NextDouble() * int.MaxValue);
        }

        public BlueprintDownloader()
        {
            GID = (int)(Utils.Rng.NextDouble() * int.MaxValue);
        }

        public WCBlueprint RequestDownload(ulong Requester, ulong RequesterSteamID, string URL)
        {
            WCBlueprint blueprint = new WCBlueprint()
            {
                Creator = RequesterSteamID,
                GID = GID,
                Requester = Requester,
                Title = Path.GetFileNameWithoutExtension(URL),
                Creation = DateTime.Now,
            };

            if (!Directory.Exists(blueprint.GetSaveDirectory()))
                Directory.CreateDirectory(blueprint.GetSaveDirectory());

            using WebClient client = new WebClient();
            using (Stream s = client.OpenRead(URL))
            {
                string tempDir = blueprint.GetTempDir() + ".tmp";
                using FileStream f = File.Create(tempDir);
                s.CopyTo(f);
                f.Close();

                using ZipArchive zip = ZipFile.OpenRead(tempDir);
                if (!IsValidBlueprintZip(zip))
                {
                    
                    Error = "Phase 1 error: Invalid blueprint file! (missing sbc file)";
                    File.Delete(tempDir);
                    Directory.Delete(blueprint.GetSaveDirectory());
                    return null;
                }

                foreach (var e in zip.Entries)
                    if (!string.IsNullOrEmpty(e.Name))
                        e.ExtractToFile(Path.Combine(blueprint.GetSaveDirectory(), e.Name), true);
                zip.Dispose();

                File.Delete(tempDir);
            }

            IsValid = true;
            blueprint.Save();
            return blueprint;
        }

        public WCBlueprint RequestDownload(ulong Requester)
        {
            var BP = SendRequest();
            if (BP != null && SendDownloadRequest(BP))
            {
                BP.Requester = Requester;
                int attempts = 0;
                do
                {
                    attempts++;
                    Thread.Sleep(1000);
                }
                while (!IsReady(BP) || attempts == 15);
                if (attempts != 15)
                {
                    if (DownloadBlueprint(BP))
                    {
                        BP.Save();
                        return BP;
                    }
                } 
                else
                {
                    Error = "Phase 3 error: Session timed out!";
                }
            }
            IsValid = false;
            return null;
        }

        private bool DownloadBlueprint(WCBlueprint blueprint)
        {
            if (!Directory.Exists(blueprint.GetSaveDirectory()))
                Directory.CreateDirectory(blueprint.GetSaveDirectory());

            HttpWebRequest requestDownload = (HttpWebRequest)WebRequest.Create(DownloadURL + blueprint.DownloadUUID);
            using (Stream s = ((HttpWebResponse)requestDownload.GetResponse()).GetResponseStream())
            {
                string tempDir = blueprint.GetTempDir() + ".tmp";
                using FileStream f = File.Create(tempDir);
                s.CopyTo(f);
                f.Close();

                using ZipArchive zip = ZipFile.OpenRead(tempDir);
                if (!IsValidBlueprintZip(zip))
                {
                    Error = "Phase 4 error: Invalid blueprint file! (missing sbc file)";
                    File.Delete(tempDir);
                    Directory.Delete(blueprint.GetSaveDirectory());
                    return false;
                }
                zip.ExtractToDirectory(blueprint.GetSaveDirectory());
                zip.Dispose();
                File.Delete(tempDir);
            }

            return true;
        }

        private bool IsReady(WCBlueprint blueprint)
        {
            byte[] output = Encoding.UTF8.GetBytes("{ \"uuids\":[\"" + blueprint.DownloadUUID + "\"]}");

            HttpWebRequest requestStatus = (HttpWebRequest)WebRequest.Create(StatusURL);
            requestStatus.Method = "POST";
            using (Stream s = requestStatus.GetRequestStream())
            {
                s.Write(output, 0, output.Length);
            }

            using (Stream s = ((HttpWebResponse)requestStatus.GetResponse()).GetResponseStream())
            {
                byte[] byteData = new byte[100];
                s.Read(byteData, 0, byteData.Length);
                return Encoding.UTF8.GetString(byteData).Contains("prepared");
            }
        }

        private bool SendDownloadRequest(WCBlueprint blueprint)
        {
            Utils.Log($"Preparing download request for blueprint ID: {BpId}");
            try
            {
                JObject a = new JObject();
                a.Add("publishedFileId", BpId);
                a.Add("collectionId", null);
                a.Add("extract", true);
                a.Add("hidden", true);
                a.Add("direct", false);
                byte[] output = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(a).ToString());

                HttpWebRequest requestUUID = (HttpWebRequest)WebRequest.Create(RequestURL);
                requestUUID.Method = "POST";
                using (Stream s = requestUUID.GetRequestStream())
                {
                    s.Write(output, 0, output.Length);
                }

                string uuid;
                using (Stream s = ((HttpWebResponse)requestUUID.GetResponse()).GetResponseStream())
                {
                    byte[] byteData = new byte[100];
                    s.Read(byteData, 0, byteData.Length);
                    uuid = JObject.Parse(Encoding.UTF8.GetString(byteData)).GetValue("uuid").ToString();
                }

                Utils.Log($"Got download UUID of: {uuid}");
                blueprint.DownloadUUID = uuid;
                return true;
            }
            catch(Exception e)
            {
                Error = "Phase 2 error: Failed to request download UUID!";
                Utils.Log($"Error when requesting UUID of blueprint {BpId}\n{e.Message}:\n{e.StackTrace}");
            }
            return false;
        }

        private WCBlueprint SendRequest()
        {
            Utils.Log($"Preparing network request for blueprint ID: {BpId}");
            NameValueCollection infoData = new NameValueCollection()
            {
                ["itemcount"] = "1",
                ["publishedfileids[0]"] = BpId.ToString()
            };
            string infoResponce;
            using (WebClient requestInfo = new WebClient())
            {
                infoResponce = Encoding.UTF8.GetString(requestInfo.UploadValues(InfoURL, "POST", infoData));
            }

            WCBlueprint blueprint = new WCBlueprint()
            {
                Creation = DateTime.Now,
                GID = GID,
                SteamID = BpId,
            };

            try
            {
                Utils.Log($"Got network response for blueprint ID: {BpId}");
                JObject json = (JObject)((JArray)((JObject)JObject.Parse(infoResponce).GetValue("response")).GetValue("publishedfiledetails"))[0];

                blueprint.Creator = ulong.Parse((string)json.GetValue("creator"));
                blueprint.Title = (string)json.GetValue("title");

                string appCreator = (string)json.GetValue("creator_app_id");
                if (!appCreator.Equals("244850"))
                {
                    Error = "Phase 1 error: Not a Space Engineers workshop item!";
                    return null;
                }

                string fileSize = (string)json.GetValue("file_size");
                if (int.Parse(fileSize) > 100000000)
                {
                    Error = "Phase 1 error: Blueprint too large!";
                    return null;
                }

                bool isBP = false;
                foreach (JObject tag in (JArray)json.GetValue("tags"))
                {
                    if (tag.GetValue("tag").ToString().Equals("blueprint"))
                    {
                        isBP = true;
                    }
                }
                if (!isBP)
                {
                    Error = "Phase 1 error: Not a Space Engineers blueprint!";
                    return null;
                }
            }
            catch(Exception e)
            {
                Error = "Phase 1 error: Invalid file! (Item is set to Unlisted?)";
                Utils.Log($"Error when processing blueprint {BpId}\n{e.Message}:\n{e.StackTrace}");
                return null;
            }

            return blueprint;
        }

        public bool IsValidBlueprintZip(ZipArchive zip)
        {
            bool hasSbc = false;
            foreach (var e in zip.Entries)
            {
                if (!string.IsNullOrEmpty(e.Name) && Path.GetExtension(e.Name) == ".sbc")
                {
                    hasSbc = true;
                }
            }

            return hasSbc;
        }

    }
}
