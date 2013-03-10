namespace DDDInventoryItemFSharp.Web.Controllers

open System
open System.Collections.Generic
open System.Web
open System.Web.Mvc
open System.Net.Http
open System.Web.Http
open Newtonsoft.Json
open DDDInventoryItemFSharp.Web

[<CLIMutable>]
[<JsonObject(MemberSerialization=MemberSerialization.OptOut)>]
type InventoryItemModel = { 
    name : string;
    count : int;
    active : bool;
}

[<CLIMutable>]
[<JsonObject(MemberSerialization=MemberSerialization.OptOut)>]
type CreateInventoryItemModel = { 
    name : string;
}

type InventoryItemsController() =
    inherit ApiController()

    let get =         
        let get = EventStore.makeReadModelGetter Global.EventStore (fun data -> Serialization.deserializet<ReadModels.InventoryItemFlatReadModel>(data))
        fun (id:Guid) -> get ("InventoryItemFlatReadModel-" + id.ToString("N")) |> Async.RunSynchronously
            
    let handleCommand' =
        Aggregate.makeHandler 
            { zero = InventoryItem.State.Zero; apply = InventoryItem.apply; exec = InventoryItem.exec }
            (EventStore.makeRepository Global.EventStore "InventoryItem" Serialization.serializer)

    let handleCommand (id,v) c = handleCommand' (id,v) c |> Async.RunSynchronously

    member x.Get() =
        let item = get (System.Guid.Parse("88085239-6f0f-48c6-b73d-017333cb99ba"))
        match item with
        | Some item -> { InventoryItemModel.name = item.name; count = item.count; active = item.active }
        | None -> Unchecked.defaultof<InventoryItemModel>

    member x.Post ([<FromBody>] item:CreateInventoryItemModel) = 
        let id = Guid.NewGuid()
        InventoryItem.Create(id,item.name) |> handleCommand (id,0)
        id.ToString("N")