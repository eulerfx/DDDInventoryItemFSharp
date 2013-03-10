namespace DDDInventoryItemFSharp.Web.Controllers

open System.Web
open System.Web.Mvc

[<HandleError>]
type HomeController() =
    inherit Controller()
    member this.Index () = this.View()
