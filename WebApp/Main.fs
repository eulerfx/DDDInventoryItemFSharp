module WebApp.Client.Main

open Elmish
open Bolero
open Bolero.Html

type Page =
    | Overview
    | Add

type Model =
    {
        Page: Page
        AddItem: NewItem option
        Overview: InventoryItem[] option
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
    | AddItem

let update message model =
    match message with
    | SetPage page -> 
        page match
        | Overview -> { model with Page = page }, Cmd.ofMsg GetOverview
        | _ -> { model with Page = page }
    | GetOverview -> { }
    | AddItem -> { }

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
    Overview
        .Rows(cond model.Overview <| function
            | None ->
                Main.EmptyData().Elt()
            | Some overview ->
                forEach items <| fun item ->
                    p [] [
                        span [] [ text "Name:" ]
                        span [] [ text item.Name ]
                    ]
                    p [] [
                        span [] [ text "Count:" ]
                        span [] [ text item.Count ]
                    ])
        .Elt()

let view model dispatch =
    text "Hello, world!"

type MyApp() =
    inherit ProgramComponent<Model, Message>()

    override this.Program =
        Program.mkSimple (fun _ -> initModel) update view
        |> Program.withRouter router


/// Remote service definition.
type InventoryService =
    {
        /// Get the list of all item in the collection.
        GetOverview: unit -> Async<InventoryItem[]>

        /// Add a item in the collection.
        AddItem: NewItem -> Async<unit>
    }

