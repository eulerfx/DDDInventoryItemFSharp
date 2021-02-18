[<RequireQualifiedAccess>]
module InventoryItem

type State = {
    isActive : bool;
}
with static member Zero = { isActive = false }

type Command = 
    | Create of string
    | Deactivate 
    | Rename of string
    | CheckInItems of int
    | RemoveItems of int

type Event = 
    | Created of string
    | Deactivated
    | Renamed of string
    | ItemsCheckedIn of int
    | ItemsRemoved of int

let apply item = function
    | Created _ -> { item with State.isActive = true; }
    | Deactivated _ -> { item with State.isActive = false; }
    | Renamed _ -> item
    | ItemsCheckedIn _ -> item
    | ItemsRemoved _ -> item

open Validator

module private Assert =
    let validName name = notNull ["The name must not be null."] name <* notEmptyString ["The name must not be empty"] name
    let validCount count = validator (fun c -> c > 0) ["The item count must be positive."] count
    let inactive state = validator (fun i -> not i.isActive) ["The item is already deactivated."] state

let exec state =
    function
        | Create name        -> Assert.validName name   <?> Created(name)
        | Deactivate         -> Assert.inactive state   <?> Deactivated 
        | Rename name        -> Assert.validName name   <?> Renamed(name)
        | CheckInItems count -> Assert.validCount count <?> ItemsCheckedIn(count)
        | RemoveItems count  -> Assert.validCount count <?> ItemsRemoved(count)