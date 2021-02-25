module WebApp.Client.Main

open Elmish
open Bolero
open Bolero.Html
open System.Text.Json

type Page =
    | Overview
    | Add

type Model =
    {
        Page: Page
        AddItem: NewItem option
        Overview: InventoryOverview option
        Error: string option
    }
and NewItem =
    {
        Name: string
    }
and InventoryItem =
    {
        Name: string
        Count: int
    }
and InventoryOverview =
    {
        total: int
    }

/// Remote service definition.
type InventoryService =
    {
        /// Get the list of all item in the collection.
        GetOverview: unit -> Async<InventoryOverview>

        /// Add a item in the collection.
        AddItem: NewItem -> Async<unit>
    }

let initModel =
    {
        Page = Overview
        AddItem = None
        Overview = None
        Error = None
    }

type Message =
    | SetPage of Page
    | GetOverview
    | ShowOverview of InventoryOverview
    | AddItem of NewItem
    | Error of exn

let httpClient = new System.Net.Http.HttpClient()

let inventoryService : InventoryService =
    {
        GetOverview = fun () ->
            async {
                let url = "https://localhost:5001/inventory"
                let! response = httpClient.GetAsync(url) |> Async.AwaitTask
                response.EnsureSuccessStatusCode () |> ignore
                let! content = response.Content.ReadAsStreamAsync() |> Async.AwaitTask
                return! JsonSerializer.DeserializeAsync<InventoryOverview>(content).AsTask() |> Async.AwaitTask
            }
        AddItem = fun (newItem : NewItem) ->
            async {
                return ()
            }
    }

let update message model =
    match message with
    | SetPage page -> 
        match page with
        | Overview ->
            { model with Page = page }, Cmd.ofMsg GetOverview
        | _ -> { model with Page = page }, Cmd.none
    | GetOverview ->
        model,
        Cmd.OfAsync.either inventoryService.GetOverview () ShowOverview Error
    | ShowOverview overview ->
        { model with Overview = Some overview }, Cmd.none
    | AddItem newItem ->
        (inventoryService.AddItem newItem) |> Async.RunSynchronously
        { model with AddItem = Some newItem }, Cmd.none
    | Error exn ->
        { model with Error = Some exn.Message }, Cmd.none

let router = Router.infer SetPage (fun m -> m.Page)

type Overview = Template<"""
<div id="hello">
    <p>
        <a href="#/create" class="btn">Create Inventory Item</a>
    </p>

    <h3>Items</h3>

    <div class="is-fullwidth">
        ${Rows}
        <template id="EmptyData">
          <span>Downloading overview...</span>
        </template>
    </div>

</div>""">

let overviewPage model dispatch =
    Overview()
        .Rows(cond model.Overview <| function
            | None ->
                Overview.EmptyData().Elt()
            | Some overview ->
                p [] [
                    span [] [ text "Count:" ]
                    span [] [ text (string overview.total) ]
                ])
        .Elt()

let view model dispatch =
    cond model.Page <| function
    | Overview -> overviewPage model dispatch
    | _ -> text "Hello, world!"

type MyApp() =
    inherit ProgramComponent<Model, Message>()

    override this.Program =
        Program.mkProgram (fun _ -> initModel, Cmd.none) update view
        |> Program.withRouter router
