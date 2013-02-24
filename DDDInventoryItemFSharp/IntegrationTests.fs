module IntegrationTests

let conn = EventStore.conn()

let handleCommand = 
    Aggregate.makeHandler 
        { zero = InventoryItem.State.Zero; apply = InventoryItem.apply; exec = InventoryItem.exec }
        (EventStore.makeRepository conn "InventoryItem" Serialization.serializer)

let id = System.Guid.Parse("88085239-6f0f-48c6-b73d-017333cb99ba")
//let id = System.Guid.NewGuid()

//[<Xunit.Fact>]
//let deleteStream() =
//    let id = "$ce-InventoryItem-88085239-6f0f-48c6-b73d"
//    conn.DeleteStream(id, EventStore.ClientAPI.ExpectedVersion.Any)

[<Xunit.Fact>]
let createInventoryItem() =     
    let version = 0
    InventoryItem.Create(id,"Pool Pump") |> handleCommand (id,version)

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
    let readModel = get ("InventoryItemFlatReadModel-" + id.ToString("N"))
    printfn "%A" readModel

[<Xunit.Fact>]
let getOverviewReadModel() =
    let get = EventStore.makeReadModelGetter conn (fun data -> Serialization.deserializet<ReadModels.InventoryItemOverviewReadModel>(data))
    let readModel = get "InventoryItemOverviewReadModel"
    printfn "%A" readModel

[<Xunit.Fact>]
let subscribe() =
    ()
    