using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net;
using MySql.Data.MySqlClient;

namespace ApiToMySQL
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string db = "flaria_doctumstest";
            string dbhost = "laria-net.com";
            string dbuser = "flaria_doctumstestusr";
            string dbpassword = "Oy.udN8]t*iC"; //Parametrize for production
            string connectionString = "server=" + dbhost + ";user=" + dbuser + ";database=" + db + ";port=3306;password=" + dbpassword + ";";
            // Debugging
            //Console.WriteLine(connectionString);

            MySqlConnection conn = new MySqlConnection(connectionString);

            string fact;
            int length;

            try
            {
                // Open connection to MySQL server
                Console.WriteLine("Connecting to MySQL...");
                conn.Open();


                string apiUrl = "https://catfact.ninja/fact";
                int successfulApiCalls = 0;
                int maxAttempts = 5;
                using (HttpClient client = new HttpClient())

                {
                    // Loop to make 5 API calls
                    for (int i = 0; i < maxAttempts; i++)
                    {
                        try
                        {
                            // Make API request
                            Console.WriteLine($"Attempting API call {i + 1}...");
                            HttpResponseMessage response = await client.GetAsync(apiUrl);
                            // Throw exception if not successful
                            response.EnsureSuccessStatusCode();

                            // Deserialize Json response
                            string jsonResponse = await response.Content.ReadAsStringAsync();
                            JsonDocument data = JsonDocument.Parse(jsonResponse);
                            var root = data.RootElement;

                            // Print the API response - debugging
                            // Console.WriteLine(jsonResponse);

                            // Set variables to create MySQL command
                            fact = root.GetProperty("fact").GetString();
                            length = root.GetProperty("length").GetInt32();
                            DateTime callTime = DateTime.Now;
                            // Debugging
                            //Console.WriteLine(fact);
                            //Console.WriteLine(length);
                            //Console.WriteLine(callTime);

                            // Create MySQL command
                            string sql = "INSERT INTO CatFacts (Fact, FactLength, Timestamp) VALUES (@fact, @factlength, @timestamp)";
                            MySqlCommand cmd = new MySqlCommand(sql, conn);

                            cmd.Parameters.AddWithValue("@fact", fact);
                            cmd.Parameters.AddWithValue("@factlength", length);
                            cmd.Parameters.AddWithValue("@timestamp", callTime);

                            // If the response is not null, insert it into the database
                            if(data != null)
                            {
                                try
                                {
                                    //Insert into database

                                    int rowsAffected = cmd.ExecuteNonQuery();
                                    successfulApiCalls++;
                                    Console.WriteLine($"Data inserted successfully. Rows affected: {rowsAffected}");
                                    Console.WriteLine($"Total successfull calls: {successfulApiCalls}/{maxAttempts}");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.ToString());
                                }
                            }
                            else
                            {
                                Console.WriteLine("No valid response returned from API.");
                            }


                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error during API call or database insertion: {ex.Message}");
                        }
                    }
                }

            }

            finally
            {
                // Close connection
                conn.Close();
                Console.WriteLine("Connection closed.");
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
    }
}