module IntegrationTests

open EventStore.ClientAPI
open EventStore.ClientAPI.SystemData

let endPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 2113)

let conn = EventStore.conn endPoint

let handleCommand' =
    Aggregate.makeHandler 
        { zero = InventoryItem.State.Zero; apply = InventoryItem.apply; exec = InventoryItem.exec }
        (EventStore.makeRepository conn "InventoryItem" Serialization.serializer)

let handleCommand (id,v) c = handleCommand' (id,v) c |> Async.RunSynchronously

let id = System.Guid.Parse("88085239-6f0f-48c6-b73d-017333cb99ba")

[<Xunit.Fact>]
let initProjections() = 
    let pm = new ProjectionsManager(new EventStore.ClientAPI.Common.Log.ConsoleLogger(), endPoint)
    let file p = System.IO.File.ReadAllText(@"..\..\" + p)
    pm.CreateContinuous("FlatReadModelProjection", file "FlatReadModelProjection.js", UserCredentials("admin", "changeit"))
    pm.CreateContinuous("OverviewReadModelProjection", file "OverviewReadModelProjection.js", UserCredentials("admin", "changeit")) 
    ()

[<Xunit.Fact>]
let createInventoryItem() =     
    let version = 0
    InventoryItem.Create("Pool Pump") |> handleCommand (id,version)

[<Xunit.Fact>]
let renameInventoryItem() =
    let version = 1 
    InventoryItem.Rename("Cooler Pool Pump") |> handleCommand (id,version)

[<Xunit.Fact>]
let checkInItemsItem() =
    let version = 2 
    InventoryItem.CheckInItems(100) |> handleCommand (id,version)

[<Xunit.Fact>]
let removeItems() =
    let version = 3
    InventoryItem.RemoveItems(37) |> handleCommand (id,version)

[<Xunit.Fact>]
let getFlatReadModel() =
    let get = EventStore.makeReadModelGetter conn (fun data -> Serialization.deserializet<ReadModels.InventoryItemFlatReadModel>(data))
    let readModel = get ("InventoryItemFlatReadModel-" + id.ToString("N")) |> Async.RunSynchronously
    printfn "%A" readModel

[<Xunit.Fact>]
let getOverviewReadModel() =
    let get = EventStore.makeReadModelGetter conn (fun data -> Serialization.deserializet<ReadModels.InventoryItemOverviewReadModel>(data))
    let readModel = get "InventoryItemOverviewReadModel"  |> Async.RunSynchronously
    printfn "%A" readModel
    

