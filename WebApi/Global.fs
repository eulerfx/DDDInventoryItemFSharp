[<RequireQualifiedAccess>]
module Global

open System

let EventStore = lazy (
    let connectionString = "ConnectTo=tcp://localhost:1113; HeartBeatTimeout=500"
    EventStore.conn connectionString)
