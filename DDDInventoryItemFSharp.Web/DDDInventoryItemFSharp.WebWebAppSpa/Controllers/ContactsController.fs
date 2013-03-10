namespace DDDInventoryItemFSharp.Web.Controllers

open System.Collections.Generic
open System.Web
open System.Web.Mvc
open System.Net.Http
open System.Web.Http
open Newtonsoft.Json

[<CLIMutable>]
[<JsonObject(MemberSerialization=MemberSerialization.OptOut)>]
type Contact = { 
    firstName : string;
    lastName : string;
    phone : string;    
}

type ContactsController() =
    inherit ApiController()

    let contacts = seq { yield { firstName = "John"; lastName = "Doe"; phone = "123-123-1233" }
                         yield { firstName = "Jane"; lastName = "Doe"; phone = "123-111-9876" } }
    member x.Get() = 
        contacts |> Array.ofSeq

    member x.Post ([<FromBody>] contact:Contact) = 
        contacts |> Seq.append [ contact ] 