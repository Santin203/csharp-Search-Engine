using System;
using HtmlAgilityPack;
using UglyToad.PdfPig;
using Newtonsoft.Json;
using System.Dynamic;
using System.IO;
using System.Xml;
using Porter2Stemmer;
using System.Text.Json;

namespace Indexer
{
    public abstract class Files
    {
        public string fileData;
        public int nTerms;
        public List<(string term, int frequency)> terms; // Change from array to List<T>

        //Constructor for Files class, default values included
        public Files(string data = "", int termNumber = -1, List<(string term, int frequency)>? termsList = null)
        {
            fileData = data;
            nTerms = termNumber;
            terms = termsList ?? new List<(string, int)> { ("", -1) };  // Initialize if termsList is null
        }

        //Build instance of the Current Class
        public bool ExtractContent(string filePath)
        {
            Console.WriteLine("File build has started.");

            //Read data from file
            this.fileData = this.GetFileData(filePath);

            //File found, raw data stored
            if(fileData != null)
            {
                Console.WriteLine("File data read successfully!");
                terms = ParseData(fileData);

                //Parsing of fileData successful
                if(terms[0].term != "")
                {
                    Console.WriteLine("File data parsed successfully!");
                    nTerms = terms.Count; // Change from Length to Count
                    return true;
                }
                //Error in parsing fileData
                else
                {
                    Console.WriteLine("File data could not be parsed.");
                    return false;
                }
            }
            //File not found
            else
            {
                Console.WriteLine("File data could not be read.");
                return false;
            }
        }

        //Get file name from user, return data
        protected string GetFileData(string filePath)
        {

            string fileData = string.Empty;


            //Try to read file
            try
            {
                fileData = GetRawText(filePath);
            }
            //File was not found
            catch(FileNotFoundException)
            {
                Console.WriteLine($"File {filePath} not found.");
            }
            //IO error
            catch (IOException ex)
            {
                Console.WriteLine($"An I/O error occurred: {ex.Message}");
            }
            
            return fileData ?? string.Empty;
        }

        protected abstract string GetRawText(string filePath);
        
        protected string RemoveBadChars(string rawText)
        {
            string[] specialChars = { "@", "#", "!", "$", ",", ".", ";", "(", ")", "[", "]", "{", "}", "\"", "'", "\r", "\n" };

            // Replace each special character with an empty string
            foreach (string specialChar in specialChars)
            {
                rawText = rawText.Replace(specialChar, " ");
            }
            return rawText;
        }

        protected List<(string term, int frequency)> ParseData(string rawData)
        {
            // Initialize list to dummy value
            List<(string term, int frequency)> terms = new List<(string, int)>
            {
                ("", -1)
            };

            // Create tuple for copying and editing values
            (string term, int frequency) prevTuple;

            // Helper variables
            int n;
            bool found;

            // Write code for stemming algorithm here.
            string[] rawWords = rawData.Split(" ");
            rawWords = StemmWords(rawWords);

            // Traverse array of stemmed words and count the number of appearances
            foreach(string word in rawWords)
            {
                // If list still hasn't been initialized with real values
                if(terms.Count == 1 && terms[0].term == "")
                {
                    terms[0] = (word, 1);
                }
                // Else traverse list and search for current word
                else
                {
                    found = false;
                    for(int j = 0; j < terms.Count; j++)
                    {
                        // If word is inside current tuple, increase frequency
                        if(terms[j].term == word)
                        {
                            prevTuple = terms[j];

                            n = prevTuple.frequency;

                            terms[j] = (prevTuple.term, n + 1);
                            found = true;
                            break; // Break after updating to avoid unnecessary iterations
                        }
                    }
                    // Else add to list
                    if(!found)
                    {
                        terms.Add((word, 1));
                    }
                }
            }

            return terms;
        }

        protected string[] StemmWords(string[] rawStrings)
        {
            // Make new instance of stemmer
            var stemmer = new EnglishPorter2Stemmer();

            // Traverse all strings in array
            for(int i = 0; i < rawStrings.Length; i++)
            {
                // Stem word in current i index
                var stemmed = stemmer.Stem(rawStrings[i]);

                // Assign stemmed word to i index
                rawStrings[i] = stemmed.Value;
            }
            return rawStrings;
        }
    }

    public class TxtFiles : Files
    {
        public TxtFiles()
        {
        }

        // Constructor for TxtFiles that calls the base constructor
        public TxtFiles(string data, int termNumber, List<(string term, int frequency)> termsList) 
            : base(data, termNumber, termsList)
        {
        }

        protected override string GetRawText(string filePath)
        {
            string fileData;

            // Read all text from file
            fileData = File.ReadAllText(filePath);

            // Remove special chars from string and return
            this.RemoveBadChars(fileData);
            return fileData;
        }
    }

    public class PdfFiles : Files
    {
        public PdfFiles()
        {
        }

        public PdfFiles(string data, int termNumber, List<(string term, int frequency)> termsList)
            : base(data, termNumber, termsList)
        {
        }

        protected override string GetRawText(string filePath)
        {
            string pageData;

            string fileData = "";
            using (var pdf = PdfDocument.Open(filePath))
            {
                // Iterate through pages
                foreach (var page in pdf.GetPages())
                {
                    // Raw text of the page's content stream.
                    pageData = page.Text;

                    // Concatenate with previous data collected
                    fileData = string.Join(" ", fileData, pageData);
                }
            }

            // Remove special chars from string and return
            this.RemoveBadChars(fileData);
            return fileData;
        }
    }

    public class HTMLFiles : Files
    {
        public HTMLFiles()
        {
        }

        public HTMLFiles(string data, int termNumber, List<(string term, int frequency)> termsList)
            : base(data, termNumber, termsList)
        {
        }

        protected override string GetRawText(string filePath)
        {
            // Load the HTML file
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.Load(filePath);

            // Extract the plain text content from the HTML
            string fileData = htmlDoc.DocumentNode.InnerText;

            // Remove unwanted special characters from the text
            string cleanedText = this.RemoveBadChars(fileData);

            // Return the cleaned plain text content
            return cleanedText;
        }
    }


    

    public class JsonFiles : Files
    {
        public JsonFiles()
        {
        }

        public JsonFiles(string data, int termNumber, List<(string term, int frequency)> termsList)
            : base(data, termNumber, termsList)
        {
        }

        protected override string GetRawText(string filePath)
        {
            string fileData = File.ReadAllText(filePath);
            
            // Parse the JSON and extract the content (values only)
            Dictionary<string, object> jsonData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(fileData);
            List<string> values = ExtractValues(jsonData);
            
            // Join all the values into a single string
            string contentOnly = string.Join(" ", values);

            // Remove any unwanted characters and return
            return this.RemoveBadChars(contentOnly);
        }

        private List<string> ExtractValues(Dictionary<string, object> jsonData)
        {
            var values = new List<string>();

            foreach (var value in jsonData.Values)
            {
                if (value is JsonElement jsonElement)
                {
                    switch (jsonElement.ValueKind)
                    {
                        case JsonValueKind.Object:
                            // Recursively extract values from nested objects
                            var nestedData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonElement.GetRawText());
                            values.AddRange(ExtractValues(nestedData));
                            break;
                        case JsonValueKind.Array:
                            // Extract values from arrays
                            foreach (var element in jsonElement.EnumerateArray())
                            {
                                if (element.ValueKind == JsonValueKind.String)
                                {
                                    values.Add(element.GetString());
                                }
                            }
                            break;
                        case JsonValueKind.String:
                            values.Add(jsonElement.GetString());
                            break;
                        case JsonValueKind.Number:
                        case JsonValueKind.True:
                        case JsonValueKind.False:
                        case JsonValueKind.Null:
                            values.Add(jsonElement.ToString());
                            break;
                    }
                }
                else
                {
                    // For non-JsonElement values (if deserialization resulted in other object types)
                    values.Add(value.ToString());
                }
            }

            return values;
        }
    }


    public class XmlFiles : Files
    {
        public XmlFiles()
        {
        }

        public XmlFiles(string data, int termNumber, List<(string term, int frequency)> termsList)
            : base(data, termNumber, termsList)
        {
        }

        protected override string GetRawText(string filePath)
        {
            string fileData = "";
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(filePath);

            // Access the root element
            XmlElement root = xmlDoc.DocumentElement;

            // Iterate over all child nodes of the root element
            foreach (XmlNode node in root.ChildNodes)
            {
                // Concatenate only the inner text of each node to the fileData string.
                fileData = string.Join(" ", fileData, node.InnerText);
            }

            return fileData;
        }
    }



    public class CSVFiles : Files
    {
        public CSVFiles()
        {
        }

        public CSVFiles(string data, int termNumber, List<(string term, int frequency)> termsList)
            : base(data, termNumber, termsList)
        {
        }

        protected override string GetRawText(string filePath)
        {
            string fileData;

            fileData = File.ReadAllText(filePath);
            fileData = this.RemoveBadChars(fileData);

            return fileData;
        }
    }
}
