/// Integration with EventStore.
[<RequireQualifiedAccess>]
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
let make (conn:EventStoreConnection) category (serialize:InventoryItem.Event -> string * byte array, deserialize: string * byte array -> InventoryItem.Event) =

    let streamId (id:Guid) = category + "-" + id.ToString("N").ToLower()

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

let makeReadModelGetter (conn:EventStoreConnection) category (deserialize:byte array -> _) =    
    fun (aggregateId:Guid option) ->    
        let streamId = 
            match aggregateId with
            | Some id -> category + "-" + id.ToString("N")
            | _ -> category
        let eventsSlice = conn.ReadStreamEventsBackward(streamId, -1, 1, false)
        if eventsSlice.Status <> SliceReadStatus.Success then None
        elif eventsSlice.Events.Length = 0 then None
        else 
            let lastEvent = eventsSlice.Events.[0]
            if lastEvent.Event.EventNumber = 0 then None
            else Some(deserialize(lastEvent.Event.Data))
