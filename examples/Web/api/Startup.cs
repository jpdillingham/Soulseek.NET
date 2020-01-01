namespace WebAPI
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Threading;
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
    using Soulseek.Diagnostics;
    using Soulseek.Exceptions;
    using Swashbuckle.AspNetCore.Swagger;
    using WebAPI.Trackers;

    public class Startup
    {
        private static string Username { get; set; }
        private static string Password { get; set; }
        private static string WebRoot { get; set; }
        private static int ListenPort { get; set; }
        public static string OutputDirectory { get; private set; }
        private static string SharedDirectory { get; set; }

        private SoulseekClient Client { get; set; }
        private object ConsoleSyncRoot { get; } = new object();

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            Username = Configuration.GetValue<string>("USERNAME");
            Password = Configuration.GetValue<string>("PASSWORD");
            WebRoot = Configuration.GetValue<string>("WEBROOT");
            ListenPort = Configuration.GetValue<int>("LISTEN_PORT");
            OutputDirectory = Configuration.GetValue<string>("OUTPUT_DIR");
            SharedDirectory = Configuration.GetValue<string>("SHARED_DIR");
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

            services.AddSingleton<ISoulseekClient, SoulseekClient>(serviceProvider => Client);
            services.AddSingleton<ITransferTracker, TransferTracker>();
            services.AddSingleton<ISearchTracker, SearchTracker>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApiVersionDescriptionProvider provider, ITransferTracker tracker)
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

            // ---------------------------------------------------------------------------------------------------------------------------------------------
            // begin SoulseekClient implementation
            // ---------------------------------------------------------------------------------------------------------------------------------------------

            // create options for the client.
            // see the implementation of Func<> and Action<> options for detailed info.
            var clientOptions = new SoulseekClientOptions(
                listenPort: ListenPort,
                concurrentDistributedChildrenLimit: 10,
                minimumDiagnosticLevel: DiagnosticLevel.Debug,
                concurrentPeerMessageConnectionLimit: 1000000,
                serverConnectionOptions: new ConnectionOptions(inactivityTimeout: 15),
                peerConnectionOptions: new ConnectionOptions(inactivityTimeout: 5),
                transferConnectionOptions: new ConnectionOptions(inactivityTimeout: 30),
                userInfoResponseResolver: UserInfoResponseResolver,
                browseResponseResolver: BrowseResponseResolver, 
                enqueueDownloadAction: (username, ipAddress, port, filename) => EnqueueDownloadAction(username, ipAddress, port, filename, tracker), 
                searchResponseResolver: SearchResponseResolver);

            Client = new SoulseekClient(options: clientOptions);

            // bind the DiagnosticGenerated event so we can trap and display diagnostic messages.  this is optional, and if the event 
            // isn't bound the minimumDiagnosticLevel should be set to None.
            Client.DiagnosticGenerated += (e, args) =>
            {
                lock (ConsoleSyncRoot)
                {
                    if (args.Level == DiagnosticLevel.Debug) Console.ForegroundColor = ConsoleColor.DarkGray;
                    if (args.Level == DiagnosticLevel.Warning) Console.ForegroundColor = ConsoleColor.Yellow;

                    Console.WriteLine($"[DIAGNOSTIC:{e.GetType().Name}] [{args.Level}] {args.Message}");
                    Console.ResetColor();
                }
            };

            // bind transfer events.  see TransferStateChangedEventArgs and TransferProgressEventArgs.
            Client.TransferStateChanged += (e, args) => 
                Console.WriteLine($"[{args.Transfer.Direction.ToString().ToUpper()}] [{args.Transfer.Username}/{Path.GetFileName(args.Transfer.Filename)}] {args.PreviousState} => {args.Transfer.State}");
            Client.TransferProgressUpdated += (e, args) =>
            {
                // this is really verbose.
                // Console.WriteLine($"[{args.Transfer.Direction.ToString().ToUpper()}] [{args.Transfer.Username}/{Path.GetFileName(args.Transfer.Filename)}] {args.Transfer.PercentComplete} {args.Transfer.AverageSpeed}kb/s");
            };
            
            // bind BrowseProgressUpdated to track progress of browse response payload transfers.  
            // these can take a while depending on number of files shared.
            Client.BrowseProgressUpdated += (e, args) => Console.WriteLine($"[BROWSE] {args.Username}: {args.BytesTransferred} of {args.Size} ({args.PercentComplete}%)");
            
            // bind UserStatusChanged to monitor the status of users added via AddUserAsync().
            Client.UserStatusChanged += (e, args) => Console.WriteLine($"[USER] {args.Username}: {args.Status}");

            async Task ConnectAndLogIn()
            {
                await Client.ConnectAsync();
                await Client.LoginAsync(Username, Password);
            }

            Client.Disconnected += async (e, args) =>
            {
                Console.WriteLine($"Disconnected from Soulseek server: {args.Message}");

                // don't reconnect if the disconnecting Exception is either of these types.
                // if KickedFromServerException, another client was most likely signed in, and retrying will cause a connect loop.
                // if ObjectDisposedException, the client is shutting down.
                if (!(args.Exception is KickedFromServerException || args.Exception is ObjectDisposedException))
                {
                    Console.WriteLine($"Attepting to reconnect...");
                    await ConnectAndLogIn();
                }
            };
            
            Task.Run(async () => {
                await ConnectAndLogIn();
            }).GetAwaiter().GetResult();

            Console.WriteLine($"Connected and logged in.");
        }

        /// <summary>
        ///     Creates and returns a <see cref="UserInfo"/> object in response to a remote request.
        /// </summary>
        /// <param name="username">The username of the requesting user.</param>
        /// <param name="ipAddress">The IP address of the requesting user.</param>
        /// <param name="port">The port on which the request was received.</param>
        /// <returns>A Task resolving the UserInfo instance.</returns>
        private Task<UserInfo> UserInfoResponseResolver(string username, IPAddress ipAddress, int port) 
        {
            var info = new UserInfo(
                description: $"Soulseek.NET Web Example! also, your username is {username}, IP address is {ipAddress}, and the port on which you connected to me is {port}",
                picture: System.IO.File.ReadAllBytes(@"etc/slsk_bird.jpg"),
                uploadSlots: 1,
                queueLength: 0,
                hasFreeUploadSlot: false);

            return Task.FromResult(info);
        }

        /// <summary>
        ///     Creates and returns an <see cref="IEnumerable{T}"/> of <see cref="Soulseek.Directory"/> in response to a remote request.
        /// </summary>
        /// <param name="username">The username of the requesting user.</param>
        /// <param name="ipAddress">The IP address of the requesting user.</param>
        /// <param name="port">The port on which the request was received.</param>
        /// <returns>A Task resolving an IEnumerable of Soulseek.Directory.</returns>
        private Task<IEnumerable<Soulseek.Directory>> BrowseResponseResolver(string username, IPAddress ipAddress, int port)
        {
            var result = System.IO.Directory
                .GetDirectories(SharedDirectory, "*", SearchOption.AllDirectories)
                .Select(dir => new Soulseek.Directory(dir, System.IO.Directory.GetFiles(dir)
                    .Select(f => new Soulseek.File(1, Path.GetFileName(f), new FileInfo(f).Length, Path.GetExtension(f), 0))));

            return Task.FromResult(result);
        }

        /// <summary>
        ///     Invoked upon a remote request to download a file.  
        /// </summary>
        /// <param name="username">The username of the requesting user.</param>
        /// <param name="ipAddress">The IP address of the requesting user.</param>
        /// <param name="port">The port on which the request was received.</param>
        /// <param name="filename">The filename of the requested file.</param>
        /// <param name="tracker">(for example purposes) the ITransferTracker used to track progress.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="EnqueueDownloadException">Thrown when the download is rejected.  The Exception message will be passed to the remote user.</exception>
        /// <exception cref="Exception">Thrown on any other Exception other than a rejection.  A generic message will be passed to the remote user for security reasons.</exception>
        private Task EnqueueDownloadAction(string username, IPAddress ipAddress, int port, string filename, ITransferTracker tracker)
        {
            // create a new cancellation token source so that we can cancel the upload from the UI.
            var cts = new CancellationTokenSource();
            var topts = new TransferOptions(stateChanged: (e) => tracker.AddOrUpdate(e, cts), progressUpdated: (e) => tracker.AddOrUpdate(e, cts));

            // accept all download requests, and begin the upload immediately.
            // normally there would be an internal queue, and uploads would be handled separately.
            Task.Run(async () =>
            {
                using (var stream = new FileStream(filename, FileMode.Open))
                {
                    await Client.UploadAsync(username, filename, new FileInfo(filename).Length, stream, options: topts, cancellationToken: cts.Token);
                }
            }).ContinueWith(t => { throw t.Exception; }, TaskContinuationOptions.OnlyOnFaulted); // fire and forget

            // return a completed task so that the invoking code can respond to the remote client.
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Creates and returns a <see cref="SearchResponse"/> in response to the given <paramref name="query"/>.
        /// </summary>
        /// <param name="username">The username of the requesting user.</param>
        /// <param name="token">The search token.</param>
        /// <param name="query">The search query.</param>
        /// <returns>A Task resolving a SearchResponse, or null.</returns>
        private Task<SearchResponse> SearchResponseResolver(string username, int token, string query)
        {
            var defaultResponse = Task.FromResult<SearchResponse>(null);

            // sanitize the query string.  there's probably more to it than this.
            query = query.Replace("/", string.Empty).Replace("\\", string.Empty);

            // some bots continually query for very common strings.  blacklist known names here.
            var blacklist = new[] { "Lola45", "Lolo51" };
            if (blacklist.Contains(username))
            {
                return defaultResponse;
            }

            // some bots and perhaps users search for very short terms.  only respond to queries >= 3 characters.  sorry, U2 fans.
            if (query.Length < 3)
            {
                return defaultResponse;
            }

            var results = new List<Soulseek.File>();

            // add all files from any directory matching the search query
            // to be done properly this needs to be recursively applied, but that's outside of the scope of this example.
            results.AddRange(System.IO.Directory
                .GetDirectories(SharedDirectory, $"*{query}*", SearchOption.AllDirectories)
                .SelectMany(dir => System.IO.Directory.GetFiles(dir, "*")
                    .Select(f => new Soulseek.File(1, f, new FileInfo(f).Length, Path.GetExtension(f), 0))));

            // add all files matching the query, regardless of directory
            results.AddRange(System.IO.Directory
                .GetFiles(SharedDirectory, $"{query}", SearchOption.AllDirectories)
                .Select(f => new Soulseek.File(1, f, new FileInfo(f).Length, Path.GetExtension(f), 0)));

            // we may have added some files twice, so dedupe entries
            results = results
                .GroupBy(f => f.Filename)
                .Select(group => group.First())
                .ToList();

            if (results.Count() > 0)
            {
                Console.WriteLine($"[SENDING SEARCH RESULTS]: {results.Count()} records to {username} for query {query}");

                return Task.FromResult(new SearchResponse(
                    Username,
                    token,
                    freeUploadSlots: 1,
                    uploadSpeed: 0,
                    queueLength: 0,
                    fileList: results));
            }

            // if no results, either return null or an instance of SearchResponse with a fileList of length 0
            // in either case, no response will be sent to the requestor.
            return Task.FromResult<SearchResponse>(null);
        }
    }
}
