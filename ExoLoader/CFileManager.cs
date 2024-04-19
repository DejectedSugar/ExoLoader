﻿using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using static System.Net.Mime.MediaTypeNames;
using ExoLoader.Debugging;

namespace ExoLoader
{
    public class CFileManager
    {
        public static string commonFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CustomContent", "common");

        private static readonly string[] expectedCharacterEntries = new string[]
        {
            "Data","Likes","Dislikes","Ages","OnMap","HelioOnly","SpriteSize","Skeleton","Jobs"
        };
        private static readonly string[] expectedCharacterDataEntries = new string[]
        {
            "ID",
            "NAME",
            "NICKNAME",
            "GENDER",
            "LOVE",
            "AGE10",
            "BIRTHDAY",
            "DIALOGUECOLOR",
            "DEFAULTBG",
            "BASICS",
            "MORE",
            "ENHANCEMENT",
            "FILLBAR1LEFT",
            "FILLBAR1RIGHT",
            "FILLBAR1CHILD",
            "FILLBAR1TEEN",
            "FILLBAR1ADULT",
            "FILLBAR2LEFT",
            "FILLBAR2RIGHT",
            "FILLBAR2CHILD",
            "FILLBAR2TEEN",
            "FILLBAR2ADULT",
            "FILLBAR3LEFT",
            "FILLBAR3RIGHT",
            "FILLBAR3CHILD",
            "FILLBAR3TEEN",
            "FILLBAR3ADULT"
        };

        public static CharaData ParseCustomData(string folderName)
        {
            string fullJson = File.ReadAllText(Path.Combine(folderName, "data.json"));
            ModInstance.log("Read text");

            if (fullJson == null ||  fullJson.Length == 0)
            {
                DataDebugHelper.PrintDataError("Error reading " + Path.GetFileName(folderName) + " json file", "Couldn't read text for " + folderName + " data.json file", "It should be named data.json");
                return null;
            }

            Dictionary<string, object> parsedJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(fullJson);
            ModInstance.log("Converted json");
            if (parsedJson == null || parsedJson.Count == 0)
            {
                DataDebugHelper.PrintDataError("Error parsing " + Path.GetFileName(folderName) + " json file", "Couldn't parse " + folderName + "/data.json","","Is the json file malformed? missing bracket, comma or quotation mark?");
                return null;
            }
            List<string> missingKeys = new List<string>() { "Not all required entries were found in the character's data.json file. Missing:"};
            foreach (string key in expectedCharacterEntries)
            {
                if (!parsedJson.ContainsKey(key))
                {
                    missingKeys.Add(key);
                }
            }
            if (missingKeys.Count > 1)
            {
                DataDebugHelper.PrintDataError("Missing entry in " + Path.GetFileName(folderName) + " json file", missingKeys.ToArray());
                return null;
            }
            CharaData data = new CharaData();

            if (parsedJson.TryGetValue("Data", out object dataValue))
            {
                Dictionary<string, object> dataMap;
                try
                {
                    dataMap = ((JObject)dataValue).ToObject<Dictionary<string, object>>();
                } catch
                {
                    DataDebugHelper.PrintDataError("Data entry can't be parsed in " + Path.GetFileName(folderName) + " json file", "A Data entry was found but couldn't be parsed as a dictionary.");
                    return null;
                }
                List<string> missingDataKeys = new List<string>() { "Not all required entries were found in the character's data entry. Missing:" };
                foreach (string key in expectedCharacterDataEntries)
                {
                    if (!dataMap.ContainsKey(key))
                    {
                        missingDataKeys.Add(key);
                    }
                }
                if (missingKeys.Count > 1)
                {
                    DataDebugHelper.PrintDataError("Missing entries in " + Path.GetFileName(folderName) + " data entry", missingDataKeys.ToArray());
                    return null;
                }

                ModInstance.log("Reading Data entry");


                data.id = (string)dataMap["ID"];
                data.name = (string)dataMap["NAME"];
                data.nickname = (string)dataMap["NICKNAME"];

                //ModInstance.log("ID, NAME, and NICKNAME read");

                string g = (string) dataMap["GENDER"];
                if (g.Equals("X"))
                {
                    data.gender = GenderID.nonbinary;
                }
                else if (g.Equals("F"))
                {
                    data.gender = GenderID.female;
                }
                else if (g.Equals("M"))
                {
                    data.gender = GenderID.male;
                } 
                else
                {
                    DataDebugHelper.PrintDataError("Gender error in " + Path.GetFileName(folderName) + " json file","Couldn't parse GENDER entry in the Data entry.", "Does it exist? Is it X (for nb), M (for male) or F (for female)");
                }

                string love = (string)dataMap["LOVE"];
                if (love.Equals("TRUE"))
                {
                    data.canLove = true;
                }
                else if (love.Equals("FALSE"))
                {
                    data.canLove = false;
                }
                else
                {
                    DataDebugHelper.PrintDataError("Love error in " + Path.GetFileName(folderName) + " json file", "Couldn't parse LOVE entry in the Data entry.", "Does it exist? Is it set to exactly FALSE or TRUE?");
                }
                ModInstance.log("Gender and Love read");

                dataMap.TryGetValue("AGE10", out object age10);

                data.ageOffset = int.Parse((string) age10) - 10;
                //ModInstance.log("Age read");

                data.birthday = (string) dataMap.GetValueSafe("BIRTHDAY");
                data.dialogueColor = (string) dataMap.GetValueSafe("DIALOGUECOLOR");
                data.defaultBg = (string) dataMap.GetValueSafe("DEFAULTBG");
                data.basicInfo = (string) dataMap.GetValueSafe("BASICS");
                data.moreInfo = (string) dataMap.GetValueSafe("MORE");
                data.augment = (string) dataMap.GetValueSafe("ENHANCEMENT");

                //ModInstance.log("Up to Sliders read");

                data.slider1left = (string) dataMap.GetValueSafe("FILLBAR1LEFT");
                data.slider1right = (string) dataMap.GetValueSafe("FILLBAR1RIGHT");
                data.slider1values = new int[]
                {
                int.Parse((string) dataMap.GetValueSafe("FILLBAR1CHILD")),
                int.Parse((string) dataMap.GetValueSafe("FILLBAR1TEEN")),
                int.Parse((string) dataMap.GetValueSafe("FILLBAR1ADULT"))
                };
                data.slider2left = (string) dataMap.GetValueSafe("FILLBAR2LEFT");
                data.slider2right = (string) dataMap.GetValueSafe("FILLBAR2RIGHT");
                data.slider2values = new int[]
                {
                int.Parse((string)  dataMap.GetValueSafe("FILLBAR2CHILD")),
                int.Parse((string) dataMap.GetValueSafe("FILLBAR2TEEN")),
                int.Parse((string) dataMap.GetValueSafe("FILLBAR2ADULT"))
                };
                data.slider3left = (string) dataMap.GetValueSafe("FILLBAR3LEFT");
                data.slider3right = (string) dataMap.GetValueSafe("FILLBAR3RIGHT");
                data.slider3values = new int[]
                {
                int.Parse((string) dataMap.GetValueSafe("FILLBAR3CHILD")),
                int.Parse((string) dataMap.GetValueSafe("FILLBAR3TEEN")),
                int.Parse((string) dataMap.GetValueSafe("FILLBAR3ADULT")) };
                ModInstance.log("Data entry read");
            }
            else
            {
                DataDebugHelper.PrintDataError("No data entry found in " + Path.GetFileName(folderName), "Your json file needs a Data entry. If you do have one, check for typos (it is case-sensitive)");
                return null;
            }

            string onMap = (string)parsedJson["OnMap"];
            data.onMap = onMap == "TRUE";
            ModInstance.log("OnMap read");

            if (data.onMap)
            {
                string helioOnly = (string)parsedJson["HelioOnly"];
                data.helioOnly = helioOnly == "TRUE";
                //ModInstance.log("HelioOnly read");

                if (!data.helioOnly)
                {
                    string[] stringMapSpot = ((JArray)(parsedJson.GetValueSafe("PreHelioMapSpot"))).ToObject<string[]>();
                    if (stringMapSpot == null || stringMapSpot.Length != 3)
                    {
                        DataDebugHelper.PrintDataError("Pre-helio coordinates missing or broken for " + Path.GetFileName(folderName));
                        return null;
                    }
                    float[] mapSpot = { float.Parse(stringMapSpot[0]), float.Parse(stringMapSpot[1]), float.Parse(stringMapSpot[2]) };
                    data.stratoMapSpot = mapSpot;

                    string[] stringMapSpotD = ((JArray)(parsedJson.GetValueSafe("DestroyedMapSpot"))).ToObject<string[]>();
                    if (stringMapSpot == null || stringMapSpot.Length == 0)
                    {
                        DataDebugHelper.PrintDataError("Destroyed colony coordinates missing or broken for " + Path.GetFileName(folderName));
                        return null;
                    }
                    float[] mapSpotD = { float.Parse(stringMapSpotD[0]), float.Parse(stringMapSpotD[1]), float.Parse(stringMapSpotD[2]) };
                    data.destroyedMapSpot = mapSpotD;
                }
                //ModInstance.log("Non-HelioOnly map spots read");

                string[] stringMapSpotHelio = ((JArray)(parsedJson.GetValueSafe("PostHelioMapSpot"))).ToObject<string[]>();
                if (stringMapSpotHelio == null || stringMapSpotHelio.Length == 0)
                {
                    DataDebugHelper.PrintDataError("Post-helio coordinates missing or broken for " + Path.GetFileName(folderName));
                    return null;
                }
                float[] mapSpotHelio = { float.Parse(stringMapSpotHelio[0]), float.Parse(stringMapSpotHelio[1]), float.Parse(stringMapSpotHelio[2]) };
                data.helioMapSpot = mapSpotHelio;

                ModInstance.log("Helio map spot read");
            }

            string[] likes = ((JArray)(parsedJson.GetValueSafe("Likes"))).ToObject<string[]>();
            if (likes == null)
            {
                DataDebugHelper.PrintDataError("Likes entry for " + Path.GetFileName(folderName) + "broken");
                return null;
            }
            data.likes = likes;

            string[] dislikes = ((JArray)(parsedJson.GetValueSafe("Dislikes"))).ToObject<string[]>();
            if (dislikes == null)
            {
                DataDebugHelper.PrintDataError("Dislikes entry for " + Path.GetFileName(folderName) + "broken");
                return null;
            }
            data.dislikes = dislikes;


            string skeleton = (string)parsedJson["Skeleton"];
            data.skeleton = skeleton;

            int spriteSize = int.Parse((string)parsedJson.GetValueSafe("SpriteSize"));
            data.spriteSize = spriteSize;

            data.ages = (string)parsedJson["Ages"] == "TRUE";
            

            string[] jobs = ((JArray)(parsedJson.GetValueSafe("Jobs"))).ToObject<string[]>();
            if (jobs != null)
            {
                data.jobs = jobs;
            }
            else
            {
                DataDebugHelper.PrintDataError("Jobs entry for " + Path.GetFileName(folderName) + "broken");
            }
            ModInstance.log("Jobs read");

            ModInstance.instance.Log("Finished Parsing");
            data.folderName = folderName;
            return data;
        }

        public static string[] GetAllCustomCharaFolders()
        {
            List<string> list = new List<string>();
            foreach (string bundleFolder in GetAllCustomContentFolders())
            {
                foreach (string characterFolder in Directory.GetDirectories(bundleFolder))
                {
                    if (Path.GetFileName(characterFolder) == "Characters")
                    {
                        list.AddRange(Directory.GetDirectories(characterFolder));
                    }
                }
            }
            return list.ToArray();
        }

        public static List<string> GetAllCustomContentFolders(string type) {
            List<string> patchFolders = new List<string>();
            foreach (string contentFolder in GetAllCustomContentFolders())
            {
                foreach (string candidateFolder in Directory.GetDirectories(contentFolder))
                {
                    if (Path.GetFileName(candidateFolder) == type)
                    {
                        patchFolders.Add(candidateFolder);
                    }
                }
            }
            return patchFolders;
        }

        public static string[] GetAllCustomContentFolders()
        {
            return Directory.GetDirectories(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CustomContent"));
        }


        public static Dictionary<string,Sprite> customSprites = new Dictionary<string,Sprite>();

        public static Sprite GetCustomImage(string folderName, string imageName)
        {
            return GetCustomImage(folderName, imageName, 16);
        }

        public static Sprite GetCustomImage(string folderName, string imageName, int targetHeight)
        {
            ModInstance.log("Looking for image " + imageName + " in " + TrimFolderName(folderName));
            if (customSprites.ContainsKey(imageName))
            {
                ModInstance.log("Requested image is already loaded!");
                return customSprites[imageName];
            } else
            {
                string imagePath = Path.Combine(folderName, "Sprites", imageName + ".png");
                if (!File.Exists(imagePath))
                {
                    ModInstance.log("Couldn't find image " + TrimFolderName(imagePath));
                    return null;
                }
                Sprite image = null;
                Texture2D texture = null;
                Byte[] bytes = null;
                try
                {
                    texture = new Texture2D(2,2);
                    bytes = File.ReadAllBytes(imagePath);
                    ImageConversion.LoadImage(texture, bytes);
                    texture.Apply();
                    int height = texture.height;
                    int targetHeightInUnits = targetHeight;
                    float density = height / targetHeightInUnits;
                    image = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0), density);
                } catch (Exception e)
                {
                    ModInstance.log("Couldn't make sprite from file " + TrimFolderName(imagePath));
                    ModInstance.log(texture == null ? "The texture is null" : texture.isReadable.ToString());
                    ModInstance.log(bytes.Length.ToString() + "bytes in the image");
                    ModInstance.log(e.ToString());
                }
                customSprites.Add(imageName, image);
                ModInstance.log("Sprite created, " + image.texture.height + " in height");
                return image;
            }
        }

        public static Sprite GetCustomPortrait(string folderName,string imageName)
        {
            ModInstance.log("Looking for portrait image " + imageName + " in folder " +  TrimFolderName(folderName));
            string portraitName = "portrait_" + imageName;
            if (customSprites.ContainsKey(portraitName))
            {
                ModInstance.log("Requested image is already loaded!");
                return customSprites[portraitName];
            }

            string path = Path.Combine(folderName, "Sprites", portraitName + ".png");
            if (!File.Exists(path))
            {
                ModInstance.log("Couldn't find image " + TrimFolderName(path));
                return null;
            }

            
            Texture2D texture = GetTexture(path);
            Sprite image = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0));
            customSprites.Add(portraitName, image);
            ModInstance.log("Sprite created, " + image.texture.height + " in height");
            return image;
        }

        public static Texture2D GetTexture(string path)
        {
            Texture2D texture = null;
            Byte[] bytes = null;
            try
            {
                texture = new Texture2D(2, 2);
                bytes = File.ReadAllBytes(path);
                ImageConversion.LoadImage(texture, bytes);
                texture.Apply();
            }
            catch (Exception e)
            {
                ModInstance.log("Couldn't make sprite from file " + TrimFolderName(path));
                ModInstance.log(texture == null ? "The texture is null" : texture.isReadable.ToString());
                ModInstance.log(bytes.Length.ToString() + "bytes in the image");
                ModInstance.log(e.ToString());
            }
            return texture;
        }


        public static Sprite GetCustomCardSprite(string cardID, string originFile)
        {
            if (originFile == null)
            {
                ModInstance.log("Tried getting sprite for non-custom card");
                return null;
            }

            ModInstance.log("Looking for card image " + cardID);
            string spriteName = "card_" + cardID;
            if (customSprites.ContainsKey(spriteName))
            {
                ModInstance.log("Requested image is already loaded!");
                return customSprites[spriteName];
            }

            string path = originFile.Replace(".json", ".png");
            if (!File.Exists(path))
            {
                ModInstance.log("Couldn't find image " + Path.GetFileName(path));
                return null;
            }

            Sprite image = null;
            Texture2D texture = null;
            Byte[] bytes = null;
            try
            {
                texture = new Texture2D(2, 2);
                bytes = File.ReadAllBytes(path);
                ImageConversion.LoadImage(texture, bytes);
                texture.Apply();
                image = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0));
            }
            catch (Exception e)
            {
                ModInstance.log("Couldn't make sprite from file " + TrimFolderName(path));
                ModInstance.log(texture == null ? "The texture is null" : texture.isReadable.ToString());
                ModInstance.log(bytes.Length.ToString() + "bytes in the image");
                ModInstance.log(e.ToString());
            }
            customSprites.Add(spriteName, image);
            ModInstance.log("Sprite created, " + image.texture.height + " in height");
            return image;
        }

    public static string TrimFolderName(string folderName)
    {
        return folderName.RemoveStart(AppDomain.CurrentDomain.BaseDirectory);
    }
    }
}
