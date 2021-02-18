module WebApi.App

open System
open System.IO
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open FSharp.Control.Tasks
open Giraffe
open Newtonsoft.Json
open ReadModels

// ---------------------------------
// Models
// ---------------------------------

type Message =
    {
        Text : string
    }

[<CLIMutable>]
[<JsonObject(MemberSerialization=MemberSerialization.OptOut)>]
type InventoryItemModel = 
    { 
        name : string;
        count : int;
        active : bool;
    }

[<CLIMutable>]
[<JsonObject(MemberSerialization=MemberSerialization.OptOut)>]
type CreateInventoryItemModel = 
    { 
        name : string;
    }

// ---------------------------------
// Views
// ---------------------------------

module Views =
    open Giraffe.ViewEngine

    let layout (content : XmlNode list) =
        html [] [
            head [] [
                title []  [ encodedText "WebApi" ]
                link [ _rel  "stylesheet"
                       _type "text/css"
                       _href "/main.css" ]
            ]
            body [] content
        ]

    let partial () =
        h1 [] [ encodedText "WebApi" ]

    let index (model : Message) =
        [
            partial()
            p [] [ encodedText model.Text ]
        ] |> layout

// ---------------------------------
// Web app
// ---------------------------------

let indexHandler (name : string) =
    let greetings = sprintf "Hello %s, from Giraffe!" name
    let model     = { Text = greetings }
    let view      = Views.index model
    htmlView view

let getInventory () =
    task {
        let! connection = Global.EventStore.Value
        let deserializer = fun data -> Serialization.deserializet<ReadModels.InventoryItemFlatReadModel>(data)
        let getFunc = EventStore.makeReadModelGetter connection deserializer
        return fun (id : Guid) -> getFunc ("InventoryItemFlatReadModel-" + id.ToString("N"))
    }

let GeInventorytHandler (guid : string) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            //88085239-6f0f-48c6-b73d-017333cb99ba
            if (String.IsNullOrEmpty guid) then
                return! RequestErrors.NOT_FOUND "Not Found" next ctx
            else
                let! func = getInventory ()
                let! item = func (System.Guid.Parse(guid))
                match item with
                    | Some item -> 
                        let resp = 
                            { InventoryItemModel.name = item.name
                              count = item.count
                              active = item.active }
                        return! Successful.OK resp next ctx
                    | None ->
                        return! RequestErrors.NOT_FOUND "Not Found" next ctx
        }

let handleCommand' () =
    task {
        let! conn = Global.EventStore.Value
        let repository = EventStore.makeRepository conn "InventoryItem" Serialization.serializer
        let commandHandler = (Aggregate.makeHandler 
                                {   zero = InventoryItem.State.Zero
                                    apply = InventoryItem.apply
                                    exec = InventoryItem.exec   }
                                repository)

        return commandHandler
    }

let CreateInventoryHandler : HttpHandler = 
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! handleCommand = handleCommand' ()

            // Binds a JSON payload to a CreateInventoryItemModel
            let! item = ctx.BindJsonAsync<CreateInventoryItemModel>()

            let id = Guid.NewGuid()
            let! __ = InventoryItem.Create(item.name) |> handleCommand (id, 0L)

            // Sends the object back to the client
            return! Successful.OK (id.ToString()) next ctx
        }

let webApp =
    choose [
        GET >=>
            choose [
                route "/" >=> indexHandler "world"
                routef "/hello/%s" indexHandler
                routef "/inventory/%s" GeInventorytHandler
            ]
        POST >=> route "/inventory" >=> CreateInventoryHandler
        setStatusCode 404 >=> text "Not Found" ]

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder : CorsPolicyBuilder) =
    builder
        .WithOrigins(
            "http://localhost:5000",
            "https://localhost:5001")
       .AllowAnyMethod()
       .AllowAnyHeader()
       |> ignore

let configureApp (app : IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IWebHostEnvironment>()
    (match env.IsDevelopment() with
    | true  ->
        app.UseDeveloperExceptionPage()
    | false ->
        app .UseGiraffeErrorHandler(errorHandler)
            .UseHttpsRedirection())
        .UseCors(configureCors)
        .UseStaticFiles()
        .UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    services.AddCors()    |> ignore
    services.AddGiraffe() |> ignore

let configureLogging (builder : ILoggingBuilder) =
    builder.AddConsole()
           .AddDebug() |> ignore

[<EntryPoint>]
let main args =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "WebRoot")
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(
            fun webHostBuilder ->
                webHostBuilder
                    .UseContentRoot(contentRoot)
                    .UseWebRoot(webRoot)
                    .Configure(Action<IApplicationBuilder> configureApp)
                    .ConfigureServices(configureServices)
                    .ConfigureLogging(configureLogging)
                    |> ignore)
        .Build()
        .Run()
    0