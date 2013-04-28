[<RequireQualifiedAccess>]
module InventoryItem

open Validation

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

module private Assert =
    let validName n = notNull ["The name must not be null."] n
    let validCount c = validator (fun c -> c > 0) ["The item count must be positive."] c
    let inactiveItem i = validator (fun i -> i.isActive = false) ["The item is already deactivated."] i

let exec item =
    function
    | Create name        -> Assert.validName name *> puree (Created name)
    | Deactivate         -> Assert.inactiveItem item *> puree Deactivated  
    | Rename name        -> Assert.validName name *> puree (Renamed name)        
    | CheckInItems count -> Assert.validCount count *> puree (ItemsCheckedIn count)
    | RemoveItems count  -> Assert.validCount count *> puree (ItemsRemoved count)
