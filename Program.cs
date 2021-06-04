using System;
using System.Net.Http;
using System.Web;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Luis_bot
{
    class Program
    {
        //for db
        static SqlConnection connection;
        static string connectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=\"C:\\Users\\EugeniuCojocaru\\Desktop\\Limbaje formale si translatoare\\tema2\\DB\\anime.mdf\";Integrated Security=True;Connect Timeout=30";
        static SqlDataReader reader;
        //for luis
        public static bool askInfo = false;// true if user's last intent was ask information
        public static bool askReco = false; // true if the user's last intent was ask recommendation
        public static bool askDarkSide = false; // true if the user's last intent was ask dark side
        public static bool exit = false; 
        public static bool start = false; 
        public static string currentResponse = ""; // current luis json 
        public static string animeSearchResponse = "";// last json about anime recommendation
        
        static int currentAnime = 0; // how many anime to skip from db
        public static async Task Main(string[] args)
        {
            var appId = "3049727d-314c-4159-b6f7-75f98d6c2c9e";
            var predictionKey = "81054ae0e62e428ba482198a2f7522eb";
            var predictionResourceName = "anime-sensei";
            var predictionEndpoint = String.Format("https://{0}.cognitiveservices.azure.com/", predictionResourceName);
            string responseIntent = "";
            string output = "";

            while (true)
            {
                // Read the text to recognize
                Console.Write("> ");
                string input = Console.ReadLine().Trim();

                if (input.ToLower() == "fuck you")
                {
                    Console.WriteLine("FUCK YOU 2!");                    
                    break;
                }
                else
                {
                    if (input.Length > 0)
                    {
                        Task<string> get = MakeRequest(predictionKey, predictionEndpoint, appId, input);
                        responseIntent = await get;
                        //Console.WriteLine($"Main: {responseIntent}");

                        Task<string> write = giveResponse(responseIntent.ToLower().Trim());
                        output = await write;                        
                        Console.WriteLine(output);
                        if (exit)
                            break;
                    }
                }
            }

           /* Console.WriteLine("Press ENTER to exit...");
            Console.ReadLine();*/
        }
        static async Task<string> MakeRequest(string predictionKey, string predictionEndpoint, string appId, string utterance)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // The request header contains your subscription key
            client.DefaultRequestHeaders.Add("2a842074-df2b-4d21-9f39-9b78004ed999", predictionKey);

            // The "q" parameter contains the utterance to send to LUIS
            queryString["query"] = utterance;

            // These optional request parameters are set to their default values
            queryString["verbose"] = "true";
            queryString["show-all-intents"] = "true";
            queryString["staging"] = "false";
            queryString["timezoneOffset"] = "0";

            var predictionEndpointUri = String.Format("{0}luis/prediction/v3.0/apps/{1}/slots/production/predict?subscription-key={2}&{3}", predictionEndpoint, appId, predictionKey, queryString);

            var response = await client.GetAsync(predictionEndpointUri);

            var strResponseContent = await response.Content.ReadAsStringAsync();

            // Display the JSON result from LUIS.
            //Console.WriteLine(predictionEndpointUri);
            //Console.WriteLine(strResponseContent.ToString());
            currentResponse = strResponseContent;
            int start = strResponseContent.IndexOf("\"topIntent\":") + 13;
            string s = "";
            while (strResponseContent[start] != '"')
            {
                s += strResponseContent[start++];
            }
            //Console.WriteLine("Thread: "+ s);
            return s;

        }
        static async Task<string> giveResponse(string intent)
        {
            //Console.WriteLine("---->" + intent);
            switch (intent)
            {
                case "greetings":
                    start = true;
                    return "# Ahh, User-san, I'm glad to have you here! How can I help you?";
                case "ask recommendation":
                    if(start==true)
                        return "# I can help you! Tell me what genres you like to watch!";
                    break;
                case "give information":
                    if (start == true)
                    {
                        askReco = true;
                        animeSearchResponse = currentResponse;
                        return getAnimeByGenre(0);
                    }
                    break;
                case "ask information":
                    if (start == true)
                    {
                        return getInfoAboutAnime();
                    }
                    break;
                case "get response":
                    if (start == true)
                    {
                        return getResponse();
                    }
                    break;
                case "ask about the dark side":
                    if (start == true)
                    {
                        return getDarkSide();
                    }
                    break;
                default: break;
            }
            return "# I'm not listening to impolite people!";
        }

        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~GIVE ANIME WITH SPECIFIC GENRES
        /// <summary>
        /// This function reads the json file returned by LUIS and gets all the data from entity Genre
        /// </summary>
        /// <returns>list of string( all genre titles in json file)</returns>
        static List<string> parseGenres()
        {
            dynamic obj = JObject.Parse(animeSearchResponse);
            List<string> s = new List<string>();
            if (obj["prediction"]["entities"]["Genre"] != null)
            {
                foreach (var v in obj["prediction"]["entities"]["Genre"])
                {
                    s.Add((string)v);
                }
            }
            return s;
        }
        /// <summary>
        /// Asks the database for animes that have specific genres
        /// </summary>
        /// <returns>a response from the bot</returns>
        static string getAnimeByGenre(int skipedAnime)
        {           
            string s = "";
            List<string> ls = parseGenres();

            using (connection = new SqlConnection(connectionString))
            {
                string query = "SELECT DISTINCT a.Title, a.Year, a.Score, a.Episodes FROM Anime as a INNER JOIN Anime_Genre as ag ON a.Id = ag.Id_anime INNER JOIN Genre as g on g.Id = ag.Id_genre WHERE";
                foreach (string ss in ls)
                {
                    query += " g.Genre = '" + ss + "' OR";
                }
                query = query.Substring(0, query.Length - 2);
                query += " ORDER BY a.Score DESC";

                //Console.WriteLine(query);
                SqlCommand co = new SqlCommand(query, connection);
                connection.Open();
                reader = co.ExecuteReader();
                while ((skipedAnime--)!=0)
                {
                    reader.Read();
                }
                if (reader.Read())
                {
                    s += "# I recommend: " + reader["Title"].ToString().Trim() + " from " + reader["Year"].ToString().Trim() + ", it has " + reader["Episodes"].ToString().Trim() + " episodes and a " + reader["Score"] + " score on myAnimeList.com.";
                    currentAnime++;
                    s += " Do you need another recommendation?";
                }
                else
                    s += "# I didn't find any anime that suits your taste. Do you need anything else?";
                connection.Close();
            }
            return s ;
        }

        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~GIVE INFO ABOUT ASKED ANIME 
        /// <summary>
        /// This function reads the json file returned by LUIS and gets all the anime names
        /// </summary>
        /// <returns>list of string( all anime titles in json file)</returns>
        static List<string> parseAnime()
        {
            dynamic obj = JObject.Parse(currentResponse);
            List<string> s = new List<string>();
            if (obj["prediction"]["entities"]["Anime"] != null)
            {
                foreach (var v in obj["prediction"]["entities"]["Anime"])
                {
                    s.Add((string)v);
                }
            }
            return s;
        }
        /// <summary>
        /// Asks the database for info about mentioned animes
        /// </summary>
        /// <returns>a response from the bot</returns>
        static string getInfoAboutAnime()
        {
            string s = "";
            askInfo = true;
            List<string> ls = parseAnime();
            if (ls.Count != 0)
            {
                using (connection = new SqlConnection(connectionString))
                {
                    string query = "SELECT a.Title, a.Year, a.Score, a.Episodes, g.Genre FROM Anime as a INNER JOIN Anime_Genre as ag ON a.Id = ag.Id_anime INNER JOIN Genre as g on g.Id = ag.Id_genre WHERE";
                    foreach (string ss in ls)
                    {
                        query += " a.Title = '" + ss + "' OR";
                    }
                    query = query.Substring(0, query.Length - 2);
                    query += "ORDER BY a.Score DESC";

                    //Console.WriteLine(query);
                    SqlCommand co = new SqlCommand(query, connection);
                    connection.Open();
                    reader = co.ExecuteReader();
                    int i = 0;
                    string title = "";
                    if (reader.Read())
                    {
                        i++;
                        title = reader["Title"].ToString().Trim();
                        s += "# Here's what I know: " + reader["Title"].ToString().Trim() + " from " + reader["Year"].ToString().Trim() + ", it has " + reader["Episodes"].ToString().Trim() + " episodes and a " + reader["Score"].ToString().Trim() + " score on myAnimeList.com. Genres: ";
                    }
                    while (reader.Read())
                    {
                        if (title.Equals(reader["Title"].ToString().Trim()))
                        {
                            s += reader["Genre"].ToString().Trim() + ", ";
                        }
                        else
                        {
                            title = reader["Title"].ToString().Trim();
                            s = s.Substring(0, s.Length - 2);
                            s += "\nHere's what I know: " + reader["Title"].ToString().Trim() + " from " + reader["Year"].ToString().Trim() + ", it has " + reader["Episodes"].ToString().Trim() + " episodes and a " + reader["Score"].ToString().Trim() + " score on myAnimeList.com. Genres: ";
                        }
                    }
                    if (i == 0)
                        s += "# I found no anime with the titles you chose. Do you need anything else?";
                    else
                    {
                        s = s.Substring(0, s.Length - 2);
                        s += ". Do you need anything else?";                        
                    }
                    connection.Close();
                }
            }
            else
            {
                s += "# I found no anime with the titles you chose. Do you need anything else?";
            }
            
            return s;
        }

        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~GET RESPONSE
        /// <summary>
        /// This function reads the json file returnes by LUIS and tells if the user approve/disapprove
        /// </summary>
        /// <returns>true/false</returns>
        static bool parseResponse()
        {
            dynamic obj = JObject.Parse(currentResponse);
            List<string> approve = new List<string>();
            List<string> disapprove = new List<string>();
            
            if (obj["prediction"]["entities"]["Approval"]!=null)
            {
                foreach (var v in obj["prediction"]["entities"]["Approval"])
                {
                    approve.Add((string)v);
                }
            }
            if (obj["prediction"]["entities"]["Disapproval"]!=null)
            {
                foreach (var v in obj["prediction"]["entities"]["Disapproval"])
                {
                    disapprove.Add((string)v);
                }
            }
            if (approve.Count > disapprove.Count)
                return true;
            else
                return false;
        }
        /// <summary>
        /// Do operations based on the user response
        /// </summary>
        /// <returns></returns>
        static string getResponse()
        {
            string s = "";
            if (askReco == true)
            {
                if (parseResponse() == true)
                {
                    //Console.WriteLine("Current anime: " + currentAnime);
                    return getAnimeByGenre(currentAnime);
                }
                else
                {
                    currentAnime = 0;
                    exit = true;
                    s += "# Ok then, I need to guide some other lonely souls so I'll take my leave! See ya!";
                }
                askReco = false;
            }
            else
            {
                if (askInfo)
                {
                    if (parseResponse() == true)
                    {
                        //Console.WriteLine("Current anime: " + currentAnime);
                        askInfo = false;
                        return "# Do you need help with anything else?";
                    }
                    else
                    {                        
                        s += "# Ok then, see you next time!";
                        exit = true;
                    }
                    askInfo = false;
                }
                else
                {
                    if (askDarkSide == true)
                    {
                        if (parseResponse() == true)
                        {
                            s += "# Ah I see you are a man of culture as well! But Kami-sama only thought me about anime. I don't know anything else. Shall I give you anime recommendations? ";
                            askReco = true;
                            return s;
                        }
                        else
                        {
                            exit = true;
                            return "# Oh my sweet summer child, you are too young to ruin your life with hentai. Anyone can grasp into heaven, but it takes a lot of willpower to let it go! ";
                        }
                    }
                   
                }
            }
            return s;

        }
        /// <summary>
        /// P L O T = people love oversized tits
        /// </summary>
        /// <returns>heaven</returns>
        static string getDarkSide()
        {
            askDarkSide = true;
            return "# Are you over 18 yo?";
        }


    }
}
