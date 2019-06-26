namespace WebAPI
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.ApiExplorer;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.FileProviders;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Soulseek;
    using Soulseek.Messaging.Messages;
    using Soulseek.Tcp;
    using Swashbuckle.AspNetCore.Swagger;

    public class Startup
    {
        private static string Username { get; set; }
        private static string Password { get; set; }
        private static string WebRoot { get; set; }
        private static int ListenPort { get; set; }
        public static string OutputDirectory { get; private set; }

        private SoulseekClient Client { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            Username = Configuration.GetValue<string>("USERNAME");
            Password = Configuration.GetValue<string>("PASSWORD");
            WebRoot = Configuration.GetValue<string>("WEBROOT");
            ListenPort = Configuration.GetValue<int>("LISTEN_PORT");
            OutputDirectory = Configuration.GetValue<string>("OUTPUT_DIR");

            var resolvers = new SoulseekClientResolvers(
                browseResponse: (u, i, p) => 
                {
                    var dir = new Soulseek.Directory(@"\test\test", 9, new List<Soulseek.File>()
                    {
                        new Soulseek.File(1, @"anything1.txt", 57, ".txt", 0),
                        new Soulseek.File(1, @"anything2.txt", 57, ".txt", 0),
                        new Soulseek.File(1, @"anything3.txt", 57, ".txt", 0),
                        new Soulseek.File(1, @"anything4.txt", 57, ".txt", 0),
                        new Soulseek.File(1, @"anything5.txt", 57, ".txt", 0),
                        new Soulseek.File(1, @"anything6.txt", 57, ".txt", 0),
                        new Soulseek.File(1, @"anything7.txt", 57, ".txt", 0),
                        new Soulseek.File(1, @"anything8.txt", 57, ".txt", 0),
                        new Soulseek.File(1, @"anything9.txt", 57, ".txt", 0),
                    });

                    return new BrowseResponse(1, new List<Soulseek.Directory>() { dir });
                });

            var options = new SoulseekClientOptions(
                listenPort: ListenPort,
                minimumDiagnosticLevel: DiagnosticLevel.Debug,
                concurrentPeerMessageConnectionLimit: 1000000,
                serverConnectionOptions: new ConnectionOptions(inactivityTimeout: 15),
                peerConnectionOptions: new ConnectionOptions(inactivityTimeout: 5),
                transferConnectionOptions: new ConnectionOptions(inactivityTimeout: 5));

            Client = new SoulseekClient(resolvers: resolvers, options: options);
            Client.DiagnosticGenerated += (e, args) =>
            {
                if (args.Level == DiagnosticLevel.Debug) Console.ForegroundColor = ConsoleColor.DarkGray;
                if (args.Level == DiagnosticLevel.Warning) Console.ForegroundColor = ConsoleColor.Yellow;

                Console.WriteLine($"[DIAGNOSTIC:{e.GetType().Name}] [{args.Level}] {args.Message}");
                Console.ResetColor();
            };

            Client.DownloadStateChanged += (e, args) => Console.WriteLine($"[Download] [{args.Username}/{Path.GetFileName(args.Filename)}] {args.PreviousState} => {args.State}");
            Client.UserStatusChanged += (e, args) => Console.WriteLine($"[USER] {args.Username}: {args.Status}");
            //Client.DownloadProgressUpdated += (e, args) => Console.WriteLine($"[Download] [{args.Username}/{Path.GetFileName(args.Filename)}] {args.PercentComplete} {args.AverageSpeed}kb/s");
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options => options.AddPolicy("AllowAll", builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Latest)
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                    options.SerializerSettings.Converters.Add(new IPAddressConverter());
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                });

            services.AddApiVersioning(options => options.ReportApiVersions = true);
            services.AddVersionedApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            services.AddSwaggerGen(options =>
            {
                services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>()
                    .ApiVersionDescriptions.ToList()
                        .ForEach(description => options.SwaggerDoc(description.GroupName, new Info { Title = "Soulseek.NET Example API", Version = description.GroupName }));

                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, typeof(Startup).GetTypeInfo().Assembly.GetName().Name + ".xml"));
            });

            Task.Run(async () => {
                await Client.ConnectAsync();
                await Client.LoginAsync(Username, Password);
            }).GetAwaiter().GetResult();

            services.AddSingleton<ISoulseekClient, SoulseekClient>(serviceProvider => Client);
            services.AddSingleton<IDownloadTracker, DownloadTracker>();
            services.AddSingleton<ISearchTracker, SearchTracker>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApiVersionDescriptionProvider provider)
        {
            if (!env.IsDevelopment())
            {
                app.UseHsts();
            }

            app.UseCors("AllowAll");

            WebRoot = WebRoot ?? Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().GetName().CodeBase).AbsolutePath), "wwwroot");
            Console.WriteLine($"Serving static content from {WebRoot}");

            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(WebRoot ?? Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().GetName().CodeBase).AbsolutePath), "wwwroot")),
                RequestPath = "",
                EnableDirectoryBrowsing = false,
                EnableDefaultFiles = true
            });

            app.UseMvc();

            app.UseSwagger(options => 
            {
                // use camelCasing for routes and properties
                options.PreSerializeFilters.Add((document, request) =>
                {
                    string camelCase(string key) =>
                        string.Join('/', key.Split('/').Select(x => x.Contains("{") || x.Length < 2 ? x : char.ToLowerInvariant(x[0]) + x.Substring(1)));

                    document.Paths = document.Paths.ToDictionary(p => camelCase(p.Key), p => p.Value);
                    document.Paths.ToList()
                        .ForEach(path => typeof(PathItem).GetProperties().Where(p => p.PropertyType == typeof(Operation)).ToList()
                            .ForEach(operation => ((Operation)operation.GetValue(path.Value, null))?.Parameters.ToList()
                                .ForEach(prop => prop.Name = camelCase(prop.Name))));
                });
            });

            app.UseSwaggerUI(options => provider.ApiVersionDescriptions.ToList()
                .ForEach(description => options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName)));
        }
    }

    class IPAddressConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(IPAddress));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return IPAddress.Parse((string)reader.Value);
        }
    }
}
