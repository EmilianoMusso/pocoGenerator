using Microsoft.Extensions.Configuration;
using System;
using System.Data.SqlClient;
using System.IO;

namespace pocoGenerator
{
    class Program
    {
        private static IConfigurationRoot configuration;

        static void Main(string[] args)
        {
            try
            {
                configuration = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                                .AddJsonFile("appsettings.json", false)
                                .Build();

                var _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "pocoGenerator");
                Directory.CreateDirectory(_path);

                var _nameSpace = configuration.GetSection("namespace")?.Value;
                using (var connection = new SqlConnection(configuration.GetSection("connectionString").Value))
                {
                    connection.Open();

                    Utils.OutpMessage("Creating classes...");

                    Utils.SqlObjectsProcess(connection, _nameSpace, _path, args, true);
                    Utils.SqlObjectsProcess(connection, _nameSpace, _path, args, false);

                    Utils.OutpMessage("Operation completed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }



       

    }
}
