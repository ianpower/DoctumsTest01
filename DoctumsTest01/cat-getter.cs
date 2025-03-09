using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net;

namespace ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string apiUrl = "https://catfact.ninja/fact";
            using (HttpClient client = new HttpClient())
            {

                // Initialize a list to store results
                List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();

                for (int i = 0; i < 20; i++)
                {
                    try
                    {
                        // Make API request
                        HttpResponseMessage response = await client.GetAsync(apiUrl);
                        // Throw exception if not successful
                        response.EnsureSuccessStatusCode();

                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        JsonDocument data = JsonDocument.Parse(jsonResponse);

                        // Print the API response - debugging
                        //Console.WriteLine(jsonResponse);

                        // Add JSON result to list                        
                        var root = data.RootElement;

                        string fact = root.GetProperty("fact").GetString();
                        int length = root.GetProperty("length").GetInt32();

                        // Store the extracted data in a dictionary
                        Dictionary<string, string> result = new Dictionary<string, string>
                        {
                            { "Fact", EscapeCsvValue(fact) },
                            { "Length", length.ToString() }
                        };

                        results.Add(result);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error calling API: {ex.Message}");
                    }
                }
                // Generate filename based on the current date and time
                string csvFilePath = DateTime.Now.ToString("yyyy-MM-dd-HHmmss") + "-catfacts.csv";
                WriteToCsv(results, csvFilePath);

                // Upload the CSV file to the FTP server
                 await UploadToFtp(csvFilePath);

            }
        }

        static string EscapeCsvValue(string value)
        {
            // Enclose the value in quotes and escape any quotes within the value
            if (value.Contains("\""))
            {
                value = value.Replace("\"", "\"\"");
            }

            // Enclose the entire value in quotes
            return $"\"{value}\"";
        }

        static void WriteToCsv(List<Dictionary<string, string>> results, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                // Write the header row (based on keys of the first dictionary entry)
                var headers = results[0].Keys;
                writer.WriteLine(string.Join(",", headers));

                // Write the data rows
                foreach (var result in results)
                {
                    writer.WriteLine(string.Join(",", result.Values));
                }
            }

            Console.WriteLine($"Data has been written to {filePath}");
        }

        static async Task UploadToFtp(string filePath)
        {
            string ftpHost = "ftp://laria-net.com";
            string ftpUsername = "doctums.dev.tests@laria-net.com";
            string ftpPassword = "pN7[Fv2oIoTa";

            string ftpRemotePath = "/"+filePath;

            try
            {
                // Create an FtpWebRequest to the FTP server
                Uri ftpUri = new Uri(ftpHost + ftpRemotePath);
                FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(ftpUri);

                // Set the FTP request properties
                ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;
                ftpRequest.Credentials = new NetworkCredential(ftpUsername, ftpPassword);
                ftpRequest.UseBinary = true;  // Use binary mode for the file transfer
                ftpRequest.KeepAlive = false; // Close the connection after upload

                // Read the file and upload it
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (Stream ftpStream = ftpRequest.GetRequestStream())
                {
                    await fs.CopyToAsync(ftpStream);  // Asynchronously copy the file stream to the FTP server
                }

                Console.WriteLine("CSV file has been uploaded to the FTP server.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading file to FTP: {ex.Message}");
            }
        }

    }
}
