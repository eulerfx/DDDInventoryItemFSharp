module EventStore

open System
open System.Net
open EventStore.ClientAPI

/// Creates and opens an EventStore connection.
let conn () = 
    let conn = EventStoreConnection.Create() 
    conn.Connect(IPEndPoint(IPAddress.Parse("127.0.0.1"), 1113))
    conn

/// Creates an event store functions with an InventoryItem-specific serializer.
let make (conn:EventStoreConnection) (serialize:InventoryItem.Event -> string * byte array, deserialize: string * byte array -> InventoryItem.Event) =

    let streamId id = "InventoryItem-" + id.ToString().ToLower()

    let load id =
        let streamId = streamId id
        let eventsSlice = conn.ReadStreamEventsForward(streamId, 1, Int32.MaxValue, false)
        eventsSlice.Events 
        |> Seq.map (fun e -> deserialize(e.Event.EventType, e.Event.Data))

    let commit (id,expectedVersion) (e:InventoryItem.Event) =
        let streamId = streamId id
        let eventType,data = serialize(e)
        let metaData = [||] : byte array
        let eventData = new EventData(Guid.NewGuid(), eventType, true, data, metaData)
        if expectedVersion = 0 
            then conn.CreateStream(streamId, Guid.NewGuid(), true, metaData)
        conn.AppendToStream(streamId, expectedVersion, eventData)

    load,commit