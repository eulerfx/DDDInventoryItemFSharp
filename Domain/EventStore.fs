/// Integration with EventStore.
[<RequireQualifiedAccess>]
module EventStore

open System
open FSharp.Control.Tasks
open EventStore.ClientAPI

/// Creates and opens an EventStore connection.
let conn (uri : string) = 
    let conn = EventStoreConnection.Create(uri)
    task {
        do! conn.ConnectAsync()
        return conn
    }
    

/// Creates event store based repository.
let makeRepository
    (conn:IEventStoreConnection)
    category 
    (serialize:obj -> string * byte array, deserialize: Type * string * byte array -> obj)
    =
    let streamId (id:Guid) = category + "-" + id.ToString("N").ToLower()

    let load (t, id) = task {
        let streamId = streamId id
        let! eventsSlice = conn.ReadStreamEventsForwardAsync(streamId, 1L, 4096, false)
        return eventsSlice.Events |> Seq.map (fun e -> deserialize(t, e.Event.EventType, e.Event.Data))
    }

    let commit (id, expectedVersion) e = task {
        let streamId = streamId id
        let eventType,data = serialize e
        let metaData = [||] : byte array
        let eventData = EventData(Guid.NewGuid(), eventType, true, data, metaData)
        
        if expectedVersion = 0L then
            let! res = conn.AppendToStreamAsync(streamId, (int64 ExpectedVersion.Any), eventData) 
            res |> ignore
        else
            let! res = conn.AppendToStreamAsync(streamId, expectedVersion, eventData)
            res |> ignore
    }

    load, commit

/// Creates a function that returns a read model from the last event of a stream.
let makeReadModelGetter 
    (conn : IEventStoreConnection)
    (deserialize : byte array -> _)
    =
    fun streamId -> task {
        let! eventsSlice = conn.ReadStreamEventsBackwardAsync(streamId, -1L, 1, false)
        if eventsSlice.Status <> SliceReadStatus.Success then return None
        elif eventsSlice.Events.Length = 0 then return None
        else 
            let lastEvent = eventsSlice.Events.[0]
            if lastEvent.Event.EventNumber = 0L then return None
            else return Some(deserialize(lastEvent.Event.Data))    
    }
