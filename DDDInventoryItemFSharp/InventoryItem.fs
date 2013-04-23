/// An inventory item.
[<RequireQualifiedAccess>]
module InventoryItem

/// Represents the state of an inventory item.
type State = {
    isActive : bool;
}
with static member Zero = { isActive = false }

/// An inventory item command.
type Command = 
    | Create of string
    | Deactivate 
    | Rename of string
    | CheckInItems of int
    | RemoveItems of int

/// An inventory item event.
type Event = 
    | Created of string
    | Deactivated
    | Renamed of string
    | ItemsCheckedIn of int
    | ItemsRemoved of int

/// Applies a inventory item event to a state.
let apply item = function
    | Created _ -> { item with State.isActive = true; }
    | Deactivated _ -> { item with State.isActive = false; }
    | Renamed _ -> item
    | ItemsCheckedIn _ -> item
    | ItemsRemoved _ -> item

/// Assertions used to maintain invariants upon command execution.
module private Assert =
    let validName name = if System.String.IsNullOrEmpty(name) then invalidArg "name" "The name must not be null." else name
    let validCount count = if count <= 0 then invalidArg "count" "Inventory count must be positive." else count
    let inactive item = if item.isActive = true then failwith "The item is already deactivated." else item

/// Executes an inventory item command.
let exec item = 

    let apply event = 
        let newItem = apply item event
        event

    function
    | Create(name)        -> name |> Assert.validName |> Created |> apply                    
    | Deactivate          -> item |> Assert.inactive |> ignore; Deactivated |> apply        
    | Rename(name)        -> name |> Assert.validName |> Renamed |> apply        
    | CheckInItems(count) -> count |> Assert.validCount |> ItemsCheckedIn |> apply        
    | RemoveItems(count)  -> count |> Assert.validCount |> ItemsRemoved |> apply
