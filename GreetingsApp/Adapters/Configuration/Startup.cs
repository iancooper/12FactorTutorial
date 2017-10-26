using System;
using GreetingsCore.Adapters.DI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Polly;
using SimpleInjector;
using SimpleInjector.Integration.AspNetCore.Mvc;
using SimpleInjector.Lifestyles;
using SimpleInjector.Integration.WebApi;

namespace GreetingsApp.Adapters.Configuration
{
    public class Startup
    {
        private Container _container;

        public IConfiguration Configuration { get; private set; }
        
        public Startup(IHostingEnvironment env)
        {
            //use a sensible constructor resolution approach
            _container = new Container();
            _container.Options.ConstructorResolutionBehavior = new MostResolvableParametersConstructorResolutionBehavior(_container);

            BuildConfiguration(env);
        }

        private void BuildConfiguration(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath);

            if (env.IsEnvironment("Development"))
            {
                // This will push telemetry data through Application Insights pipeline faster, allowing you to view results immediately.
                builder.AddApplicationInsightsSettings(developerMode: true);
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }
        

        // This method gets called by the runtime. Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddApplicationInsightsTelemetry(Configuration);

            services.AddMvc();

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader().AllowCredentials()
                );
            });

            IntegrateSimpleInjector(services);
        }

        private void IntegrateSimpleInjector(IServiceCollection services)
        {
            _container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddSingleton<IControllerActivator>(
                new SimpleInjectorControllerActivator(_container));
            services.AddSingleton<IViewComponentActivator>(
                new SimpleInjectorViewComponentActivator(_container));

            services.EnableSimpleInjectorCrossWiring(_container);
            services.UseSimpleInjectorAspNetRequestScoping(_container);
   
        }
       
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime appLifetime)
        {
            InitializeContainer(app);
            _container.Verify();

            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseCors("AllowAll");

            app.UseMvc();

            //CheckDbIsUp(Configuration["Database:ToDoDb"]);

            //CreateMessageTable(Configuration["Database:MessageStore"], Configuration["Database:MessageTableName"]);
        }
        
        private void InitializeContainer(IApplicationBuilder app)
        {
            _container.Register<DbContextOptions<ToDoContext>>( () => new DbContextOptionsBuilder<ToDoContext>().UseMySql(Configuration["Database:ToDo"]).Options, Lifestyle.Singleton);
            // Add application presentation components:
            _container.RegisterMvcControllers(app);
            _container.RegisterMvcViewComponents(app);

            RegisterCommandProcessor();

            // Cross-wire ASP.NET services (if any). For instance:
            _container.CrossWire<ILoggerFactory>(app);
            // NOTE: Prevent cross-wired instances as much as possible.
            // See: https://simpleinjector.org/blog/2016/07/
        }



        private void RegisterCommandProcessor()
        {
            //create handler 
            var subscriberRegistry = new SubscriberRegistry();
            _container.Register<IHandleRequestsAsync<AddToDoCommand>, AddToDoCommandHandlerAsync>(Lifestyle.Scoped);
            _container.Register<IHandleRequestsAsync<DeleteAllToDosCommand>, DeleteAllToDosCommandHandlerAsync>(Lifestyle.Scoped);
            _container.Register<IHandleRequestsAsync<DeleteToDoByIdCommand>, DeleteToDoByIdCommandHandlerAsync>(Lifestyle.Scoped);
            _container.Register<IHandleRequestsAsync<UpdateToDoCommand>, UpdateToDoCommandHandlerAsync>(Lifestyle.Scoped);

            subscriberRegistry.RegisterAsync<AddToDoCommand, AddToDoCommandHandlerAsync>();
            subscriberRegistry.RegisterAsync<DeleteAllToDosCommand, DeleteAllToDosCommandHandlerAsync>();
            subscriberRegistry.RegisterAsync<DeleteToDoByIdCommand, DeleteToDoByIdCommandHandlerAsync>();
            subscriberRegistry.RegisterAsync<UpdateToDoCommand, UpdateToDoCommandHandlerAsync>();

            //create policies
            var retryPolicy = Policy.Handle<Exception>().WaitAndRetry(new[] { TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(150) });
            var circuitBreakerPolicy = Policy.Handle<Exception>().CircuitBreaker(1, TimeSpan.FromMilliseconds(500));
             var retryPolicyAsync = Policy.Handle<Exception>().WaitAndRetryAsync(new[] { TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(150) });
            var circuitBreakerPolicyAsync = Policy.Handle<Exception>().CircuitBreakerAsync(1, TimeSpan.FromMilliseconds(500));
            var policyRegistry = new PolicyRegistry()
            {
                { CommandProcessor.RETRYPOLICY, retryPolicy },
                { CommandProcessor.CIRCUITBREAKER, circuitBreakerPolicy },
                { CommandProcessor.RETRYPOLICYASYNC, retryPolicyAsync },
                { CommandProcessor.CIRCUITBREAKERASYNC, circuitBreakerPolicyAsync }
            };

            var servicesHandlerFactory = new ServicesHandlerFactoryAsync(_container);

            var messagingGatewayConfiguration = RmqGatewayBuilder.With.Uri(new Uri(Configuration["RabbitMQ:Uri"])).Exchange(Configuration["RabbitMQ:Exchange"]).DefaultQueues();

            var gateway = new RmqMessageProducer(messagingGatewayConfiguration);
            var sqlMessageStore = new MySqlMessageStore(new MySqlMessageStoreConfiguration(Configuration["Database:MessageStore"], Configuration["Database:MessageTableName"]));

             var messageMapperFactory = new MessageMapperFactory(_container);
            _container.Register<IAmAMessageMapper<BulkAddToDoCommand>, BulkAddToDoMessageMapper>();
            _container.Register<IAmAMessageMapper<TaskCompletedEvent>, TaskCompleteEventMessageMapper>();
            _container.Register<IAmAMessageMapper<TaskCreatedEvent>, TaskCreatedEventMessageMapper>();

            var messageMapperRegistry = new MessageMapperRegistry(messageMapperFactory)
            {
                {typeof(BulkAddToDoCommand), typeof(BulkAddToDoMessageMapper)},
                {typeof(TaskCompletedEvent), typeof(TaskCompleteEventMessageMapper)},
                {typeof(TaskCreatedEvent), typeof(TaskCreatedEventMessageMapper)}
            };

            var messagingConfiguration = new MessagingConfiguration(
                messageStore: sqlMessageStore,
                messageProducer: gateway,
                messageMapperRegistry: messageMapperRegistry);

            var commandProcessor = CommandProcessorBuilder.With()
                .Handlers(new Paramore.Brighter.HandlerConfiguration(subscriberRegistry, servicesHandlerFactory))
                .Policies(policyRegistry)
                .TaskQueues(messagingConfiguration)
                .RequestContextFactory(new Paramore.Brighter.InMemoryRequestContextFactory())
                .Build();

            _container.RegisterSingleton<IAmACommandProcessor>(commandProcessor);
        }

        
        /*
        
        private static void CheckDbIsUp(string connectionString)
        {
            var policy = Policy.Handle<MySqlException>().WaitAndRetryForever(
                retryAttempt => TimeSpan.FromSeconds(2),
                (exception, timespan) =>
                {
                    Console.WriteLine($"Healthcheck: Waiting for the database {connectionString} to come online - {exception.Message}");
                });

            policy.Execute(() =>
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                }
            });
        }


        private static void CreateMessageTable(string dataSourceTestDb, string tableNameMessages)
        {
            try
            {
                using (var sqlConnection = new MySqlConnection(dataSourceTestDb))
                {
                    sqlConnection.Open();
                    using (var command = sqlConnection.CreateCommand())
                    {
                        command.CommandText = MySqlMessageStoreBuilder.GetDDL(tableNameMessages);
                        command.ExecuteScalar();
                    }
                }
                
            }
            catch (System.Exception e)
            {
                Console.WriteLine($"Issue with creating MessageStore table, {e.Message}");
            }
        }
 
         */
    }
    
}
