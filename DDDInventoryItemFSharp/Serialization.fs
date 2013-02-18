module Serialization

open System
open System.Reflection
open System.IO
open System.Text
open System.ComponentModel
open Newtonsoft.Json
open Newtonsoft.Json.Serialization   

/// A serialization helper type.
type private InventoryItemEvent = {
    [<JsonProperty(DefaultValueHandling=DefaultValueHandling.Ignore)>] 
    Name : string;        
    [<JsonProperty(DefaultValueHandling=DefaultValueHandling.Ignore)>] 
    Count : int;
}

let serialize (e:InventoryItem.Event) =
    let eventType,e = 
        match e with
        | InventoryItem.Created(name)         -> "Created",{Name=name;Count=0}
        | InventoryItem.Deactivated           -> "Deactivated",{Name=null;Count=0}
        | InventoryItem.Renamed(name)         -> "Renamed",{Name=name;Count=0}
        | InventoryItem.ItemsCheckedIn(count) -> "ItemsCheckedIn",{Name=null;Count=count}
        | InventoryItem.ItemsRemoved(count)   -> "ItemsRemoved",{Name=null;Count=count}

    let json = JsonConvert.SerializeObject(e)
    let data = Encoding.UTF8.GetBytes(json)
    eventType,data

let deserialize (eventType:string, data:byte array) =  
    let json = Encoding.UTF8.GetString(data)
    let e = JsonConvert.DeserializeObject<InventoryItemEvent>(json)
    match eventType with
    | "Created"        -> InventoryItem.Created(e.Name)
    | "Deactivated"    -> InventoryItem.Deactivated
    | "Renamed"        -> InventoryItem.Renamed(e.Name) 
    | "ItemsCheckedIn" -> InventoryItem.ItemsCheckedIn(e.Count)
    | "ItemsRemoved"   -> InventoryItem.ItemsRemoved(e.Count)
    | _                -> failwith "Invalid event type!"

let serializer = serialize,deserialize