module WebApp.Client.Main

open Elmish
open Bolero
open Bolero.Html
open System.Text.Json
open System.Net.Http
open System.Text

type Page =
    | Overview
    | Add

type Model =
    {
        Page: Page
        NewItem: NewItem option
        Overview: InventoryOverview option
        Error: string option
    }
and NewItem =
    {
        Id: string option
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
        AddItem: NewItem -> Async<string>
    }

let initModel =
    {
        Page = Overview
        NewItem = None
        Overview = None
        Error = None
    }

type Message =
    | SetPage of Page
    | GetOverview
    | ShowOverview of InventoryOverview
    | Error of exn
    | SetNameForCreate of string
    | CreateItemCmd
    | ItemCreated of string

let httpClient = new HttpClient()

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
                let url = "https://localhost:5001/inventory/"
                let json = JsonSerializer.Serialize(newItem)
                let reqContent = new StringContent(json, Encoding.UTF8, "application/json")
                let! response = httpClient.PostAsync(url, reqContent) |> Async.AwaitTask
                response.EnsureSuccessStatusCode () |> ignore
                let! respContent = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                return respContent;
                //return! JsonSerializer.DeserializeAsync<InventoryOverview>(content).AsTask() |> Async.AwaitTask
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
    | SetNameForCreate itemName ->
        { model with NewItem = Some { Id = None; Name = itemName } }, Cmd.none
    | CreateItemCmd ->
        match model.NewItem with
        | Some value ->
            model,
            Cmd.OfAsync.either inventoryService.AddItem (value) ItemCreated Error
        | None -> model, Cmd.none
    | ItemCreated id ->
        let newItem = model.NewItem |> Option.map (fun x -> { x with Id = Some id })
        { model with NewItem = newItem; Page = Overview }, Cmd.none
    | Error exn ->
        { model with Error = Some exn.Message }, Cmd.none

let router = Router.infer SetPage (fun m -> m.Page)

type Overview = Template<"""
<div id="hello">
    <p>
        <a href="/Add" class="btn">Create Inventory Item</a>
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

type CreateItem = Template<"""
<div class="row-fluid">
    <h2>Add New Inventory Item</h2>
    <form id="inventoryItemForm" class="form-horizontal" onsubmit="${Submit}">       
        <div class="control-group">
            <label class="control-label" for="name">Name</label>
            <div class="controls">
                <input id="name" name="name" type="text" placeholder="Name" bind-onchange="${Name}"/>
            </div>
        </div>
        <div class="control-group">
            <div class="controls">
                <button class="btn btn-primary" type="submit">Save</button>
            </div>
        </div>
    </form>
</div>
""">

let createItemPage (model : Model) dispatch =
    match model.NewItem with
    | Some item ->
        CreateItem()
            .Name(item.Name, fun n -> n |> SetNameForCreate |> dispatch)
            .Submit(fun _ -> dispatch CreateItemCmd)
            .Elt()
    | None ->
        CreateItem()
            .Name("", fun n -> n |> SetNameForCreate |> dispatch)
            .Submit(fun _ -> dispatch CreateItemCmd)
            .Elt()
    

let view model dispatch =
    cond model.Page <| function
    | Overview -> overviewPage model dispatch
    | Add -> createItemPage model dispatch
    | _ -> text "Hello, world!"

type MyApp() =
    inherit ProgramComponent<Model, Message>()

    override this.Program =
        Program.mkProgram (fun _ -> initModel, Cmd.none) update view
        |> Program.withRouter router
