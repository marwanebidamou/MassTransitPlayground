using AutoMapper;
using MassTransit;
using MassTransit.Configuration;
using Microsoft.EntityFrameworkCore;
using ServiceUno.Config;
using ServiceUno.DbContexts;
using ServiceUno.Models;
using ServiceUno.Repository;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


//Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});
//Configure AutoMapper
IMapper mapper = MappingConfig.RegisterMaps().CreateMapper();
builder.Services.AddSingleton(mapper);
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());


//Repo services
builder.Services.AddScoped<IProductRepository, ProductRepository>();


builder.Services.AddMassTransit(configure =>
   {
       //consumers registration
       //consumers are the classes that are charges of consuming messages from the RabbitMQ messages
       //we gonna register consumers by define the assembly that should have all the consumers already defined, and that's going to be the entry assemby for wichever microservice is invoking this class
       configure.AddConsumers(Assembly.GetEntryAssembly());

       configure.UsingRabbitMq((context, configurator) =>
       {
           var configuration = context.GetService<IConfiguration>();
           //service settings
           var serviceSettings = configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();

           var rabbitMQSettings = configuration.GetSection(nameof(RabbitMQSettings)).Get<RabbitMQSettings>();
           configurator.Host(rabbitMQSettings.Host);
           configurator.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter(serviceSettings.ServiceName, false));
       });

       configure.AddEntityFrameworkOutbox<ApplicationDbContext>(o =>
       {
           // configure which database lock provider to use (Postgres, SqlServer, or MySql)
           o.UseSqlServer();

           // enable the bus outbox
           o.UseBusOutbox();
       });
   });
   var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
