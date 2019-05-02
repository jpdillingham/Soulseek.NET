namespace WebAPI
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.ApiExplorer;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Swashbuckle.AspNetCore.Swagger;

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options => options.AddPolicy("AllowAll", builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader().AllowCredentials()));

            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Latest)
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
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
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApiVersionDescriptionProvider provider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseCors("AllowAll");

            app.UseHttpsRedirection();
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
}
