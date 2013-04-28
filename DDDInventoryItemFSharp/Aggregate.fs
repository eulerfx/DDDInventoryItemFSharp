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
    exec : 'TState -> 'TCommand -> Choice<'TEvent, string list>;
}

type Id = System.Guid

/// Creates a persistent command handler for an aggregate.
let makeHandler (aggregate:Aggregate<'TState, 'TCommand, 'TEvent>) (load:System.Type * Id -> Async<obj seq>, commit:Id * int -> obj -> Async<unit>) =
    fun (id,version) command -> async {
        let! events = load (typeof<'TEvent>,id)
        let events = events |> Seq.cast :> 'TEvent seq
        let state = Seq.fold aggregate.apply aggregate.zero events
        let event = aggregate.exec state command
        match event with
        | Choice1Of2 event ->
            let state = event |> aggregate.apply state
            let! _ = event |> commit (id,version)
            return Choice1Of2 ()
        | Choice2Of2 errors -> 
            return errors |> Choice2Of2
    }