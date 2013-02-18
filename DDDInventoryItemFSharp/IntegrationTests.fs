module IntegrationTests

let conn = EventStore.conn()

let handleCommand = 
    Aggregate.makeHandler 
        { zero = InventoryItem.State.Zero; apply = InventoryItem.apply; exec = InventoryItem.exec }
        (EventStore.make conn Serialization.serializer)

[<Xunit.Fact>]
let createInventoryItem() = 
    //let id = System.Guid.NewGuid()
    let id = System.Guid.Parse("88085239-6f0f-48c6-b73d-017333cb99ba")
    let version = 0
    InventoryItem.Create(id,"Pool Pump") |> handleCommand (id,version)

[<Xunit.Fact>]
let renameInventoryItem() =            
    let id = System.Guid.Parse("88085239-6f0f-48c6-b73d-017333cb99ba")
    let version = 1 
    InventoryItem.Rename("Cooler Pool Pump") |> handleCommand (id,version )


