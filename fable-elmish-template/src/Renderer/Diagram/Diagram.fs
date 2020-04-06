﻿module Diagram

open Fulma
open Elmish
open Elmish.HMR
open Elmish.React
open Elmish.Debug
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Import.Browser
open Fable.Core
open Fable.Core.JsInterop

open DiagramTypes
open Draw2dWrapper
open JSHelpers
open DiagramStyle

type Model = {
    Diagram : Draw2dWrapper
    Zoom : float
    State : Component list * Connection list
    SelectedComponent : (Component * JSComponent) option // Keep a ref to the original jsObject to edit it.
}

// -- Init Model

let init() = {
    Diagram = new Draw2dWrapper()
    Zoom = 1.0
    State = [], []
    SelectedComponent = None
}

// -- Create View

let prettyPrintState (components, connections) =
    [ str "Components:"; br [] ] @
    List.collect (fun c -> [ str <| sprintf "%A" c; br [] ]) components @
    [ str "Connections:"; br [] ] @
    List.collect (fun c -> [ str <| sprintf "%A" c; br [] ]) connections

let extractPorts jsPorts =
    let extractPort jsPort = jsPort?id
    let portsLen = jsPorts?length
    List.map (fun i -> extractPort jsPorts?(i)) [0..portsLen - 1]

let extractComponent (jsComponent : JSComponent) : Component = {
    Id = jsComponent?id
    InputPorts = extractPorts jsComponent?inputPorts?data
    OutputPorts = extractPorts jsComponent?outputPorts?data
    // Very hacky. Assume every component has a label children which is the
    // first children of the array. TODO: do properly.
    Label = jsComponent?children?data?(0)?figure?text
}

let extractComponents (jsComponents : JSComponents) : Component list =
    let componentsLen : int = jsComponents?length
    List.map (fun i -> extractComponent jsComponents?(i)) [0..componentsLen - 1]

let extractConnections (jsConnections : JSConnections) : Connection list =
    let extractPort jsPort : ConnectionPort = {
        ComponentId = jsPort?node
        PortId = jsPort?port
    }
    let extract (jsConnection : JSConnection) : Connection = {
        Id = jsConnection?id
        Source = extractPort jsConnection?source
        Target = extractPort jsConnection?target
    }
    let connectionsLen : int = jsConnections?length
    List.map (fun i -> extract jsConnections?(i)) [0..connectionsLen - 1]

let extractState (state : JSCanvasState) =
    log state
    let components : JSComponents = state?components
    let connections : JSConnections = state?connections
    extractComponents components, extractConnections connections

let getStateAction model dispatch =
    match model.Diagram.GetCanvasState () with
    | None -> ()
    | Some state -> extractState state |> UpdateState |> dispatch

let viewSelectedComponent model =
    match model.SelectedComponent with
    | None -> div [] [ str "No component selected" ]
    | Some (comp, jsComp) ->
        let formId = "component-properties-form-" + comp.Id
        let readOnlyFormField name value =
            Field.div [] [
                Label.label [] [ str name ]
                Control.div [] [ Input.text [ Input.Props [ ReadOnly true; Name name ]; Input.IsStatic true; Input.Value value ] ] ]
        let formField name defaultValue =
            // Use comp.Id as key to refresh. DefaultValue is only updated when
            // the form is created and not anymore. The Key force the re-rendering
            // of the element every time the Key value changes.
            Field.div [] [
                Label.label [] [ str name ]
                Control.div [] [ Input.text [ Input.Props [ Name name; Key comp.Id ]; Input.DefaultValue defaultValue ] ] ]
        form [ Id formId ] [
            readOnlyFormField "Id" comp.Id
            formField "Label" comp.Label
            readOnlyFormField "Input ports" comp.InputPorts.[0]
            readOnlyFormField "Output ports" comp.OutputPorts.[0]
            // Submit.
            Field.div [ Field.IsGrouped ] [
                Control.div [] [
                    Button.button [
                        Button.Color IsPrimary
                        Button.OnClick (fun e ->
                            e.preventDefault()
                            // TODO: dont think this is the right way to do it.
                            let form = document.getElementById <| formId
                            let label : string = form?elements?Label?value
                            // TODO: again, this very hacky.
                            jsComp?children?data?(0)?figure?setText(label)
                        )
                    ] [ str "Update" ]
                ]
            ]
        ]

let hideView model dispatch =
    div [] [
        model.Diagram.CanvasReactElement (JSDiagramMsg >> dispatch) Hidden
    ]

let displayView model dispatch =
    div [] [
        model.Diagram.CanvasReactElement (JSDiagramMsg >> dispatch) Visible
        div [ rightSectionStyle ] [
            div [ Style [Width "90%"; MarginLeft "5%"; ] ] [
                Heading.h4 [] [ str "Component properties" ]
                viewSelectedComponent model
            ]
        ]
        div [ bottomSectionStyle ] [
            Button.button [ Button.Props [ OnClick (fun _ -> model.Diagram.CreateBox()) ] ] [ str "Add box" ]
            Button.button [ Button.Props [ OnClick (fun _ -> getStateAction model dispatch) ] ] [ str "Get state" ]
            div [] (prettyPrintState model.State)
        ]
        //Button.button [ Button.Props [ OnClick (fun _ -> dispatch ZoomIn)] ] [ str "Zoom in" ]
        //Button.button [ Button.Props [ OnClick (fun _ -> dispatch ZoomOut)] ] [ str "Zoom out" ]
    ]

// -- Update Model

let handleJSDiagramMsg msg model =
    match msg with
    | InitCanvas canvas -> // Should be triggered only once.
        model.Diagram.InitCanvas canvas
        model
    | SelectComponent jsComponent ->
        { model with SelectedComponent = Some (extractComponent jsComponent, jsComponent) }
    | UnselectComponent jsComponent ->
         { model with SelectedComponent = None }

let update msg model =
    match msg with
    | JSDiagramMsg msg' -> handleJSDiagramMsg msg' model
    | UpdateState (com, con) -> {model with State = (com, con)}
    //| ZoomIn ->
    //    model.Canvas.SetZoom <| min (model.Zoom + 0.5) 10.0
    //    { model with Zoom = model.Zoom + 0.5 }
    //| ZoomOut ->
    //    model.Canvas.SetZoom <| max (model.Zoom - 0.5) 0.5
    //    { model with Zoom = model.Zoom - 0.5 }
