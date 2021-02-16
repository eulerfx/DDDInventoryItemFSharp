/// Aggregate framework.
[<RequireQualifiedAccess>]
module Aggregate

open System.Threading.Tasks
open FSharp.Control.Tasks

/// Represents an aggregate.
type Aggregate<'TState, 'TCommand, 'TEvent> = {
    
    /// An initial state value.
    zero : 'TState;

    /// Applies an event to a state returning a new state.
    apply : 'TState -> 'TEvent -> 'TState;

    /// Executes a command on a state yielding an event.
    exec : 'TState -> 'TCommand -> Choice<'TEvent, string list>;
}

type Id = System.Guid

/// Creates a persistent, async command handler for an aggregate given load and commit functions.
let makeHandler 
    (aggregate : Aggregate<'TState, 'TCommand, 'TEvent>)
    (load : System.Type * Id -> Task<obj seq>, commit : Id * int64 -> obj -> Task<unit>)
    =
    fun (id,version) command -> task {
        let! events = load (typeof<'TEvent>,id)
        let events = events |> Seq.cast :> 'TEvent seq
        let state = Seq.fold aggregate.apply aggregate.zero events
        let event = aggregate.exec state command
        match event with
        | Choice1Of2 event ->
            let! _ = event |> commit (id,version)
            return Choice1Of2 ()
        | Choice2Of2 errors -> 
            return errors |> Choice2Of2
    }

/// Creates a persistent command handler for an aggregate given load and commit functions.
let makeHandlerSync 
    (aggregate : Aggregate<'TState, 'TCommand, 'TEvent>)
    (load : System.Type * Id -> obj seq, commit : Id * int -> obj -> unit)
    =
    fun (id,version) command ->
        let events = load (typeof<'TEvent>,id) |> Seq.cast :> 'TEvent seq
        let state = Seq.fold aggregate.apply aggregate.zero events
        let result = aggregate.exec state command
        match result with
        | Choice1Of2 event  -> event |> commit (id,version) |> Choice1Of2 
        | Choice2Of2 errors -> errors |> Choice2Of2
