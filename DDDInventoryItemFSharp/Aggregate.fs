/// Aggregate framework.
[<RequireQualifiedAccess>]
module Aggregate

/// Represents an aggregate.
type Aggregate<'TState, 'TCommand, 'TEvent> = {
    
    /// An initial state value.
    zero : 'TState;

    /// Applies an event to a state returning a new state.
    apply : 'TState -> 'TEvent -> 'TState;

    /// Executes a command on a state yielding an event.
    exec : 'TState -> 'TCommand -> 'TEvent;
}

type Id = System.Guid

/// Creates a persistent command handler for an aggregate.
let makeHandler (aggregate:Aggregate<'TState, 'TCommand, 'TEvent>) (load:System.Type * Id -> obj seq, commit:Id * int -> obj -> unit) =
    fun (id,version) command ->
        let state = load (typeof<'TEvent>,id) |> Seq.cast :> 'TEvent seq |> Seq.fold aggregate.apply aggregate.zero
        let event = aggregate.exec state command
        event |> commit (id,version)