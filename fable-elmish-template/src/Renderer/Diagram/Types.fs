module DiagramTypes

//=============================//
// Types for library interface //
//=============================//

type JSCanvas      = | JSCanvas of obj
type JSCanvasState = | JSCanvasState of obj // Only the relevant fields from a JSCanvas.
type JSComponent   = | JSComponent of obj
type JSComponents  = | JSComponents of obj // JS list of JSComponent.
type JSConnection  = | JSConnection of obj
type JSConnections = | JSConnections of obj // JS list of JSConnection.

// TODO unify the type for ports.

// Component mapped to f# object.
type Component = {
    Id : string
    InputPorts : string list // list of ids.
    OutputPorts : string list // list of ids.
}

// Connection mapped to f# object.
type ConnectionPort = {
    ComponentId : string
    PortId : string
}
type Connection = {
    Id : string
    Source : ConnectionPort // Will always be an output port.
    Target : ConnectionPort // Will always be an input port.
}

//==========//
// Messages //
//==========//

// Messages that will be sent from JS code.
type JSEditorMsg =
    | InitCanvas of JSCanvas
    | SelectFigure of JSComponent
    | UnselectFigure of JSComponent

type Msg =
    | JSEditorMsg of JSEditorMsg
    | UpdateState of Component list * Connection list
    //| ZoomIn
    //| ZoomOut
