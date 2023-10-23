namespace DCRGraph

module GraphvizTypes =
    let mutable INDENTLEVEL = 0

    let indent (str: string) : string =
        INDENTLEVEL <- INDENTLEVEL + 1
        let indentation = String.replicate INDENTLEVEL "\t"
        let output = indentation + str.Replace("\n", "\n" + indentation)
        INDENTLEVEL <- INDENTLEVEL - 1
        output

    type Shape =
        | Record
        | Plaintext

        member this.Codegen =
            match this with
            | Record -> sprintf "record"
            | Plaintext -> sprintf "plaintext"

    type Attribute =
        //| Label of string
        | Shape of Shape
        | Rankdir of string
        | Compound of bool
        | Label of string
        | Style of string
        | Labelloc of string

        member this.Codegen =
            match this with
            | Rankdir(value: string) -> sprintf "rankdir=\"%s\"" value
            | Compound(value: bool) -> sprintf "compound=%b" value
            | Shape(shape) -> sprintf "shape=%s" shape.Codegen
            | Label(label) -> sprintf "label=\"%s\"" label
            | Style(style) -> sprintf "style=\"%s\"" style
            | Labelloc(loc) -> sprintf "labelloc=\"%s\"" loc

    type Attributes = List<Attribute>

    type Event =
        { id: string
          attributes: Attributes }

        member this.Codegen =
            sprintf
                "%s [%s]"
                this.id
                (String.concat ", " (this.attributes |> List.map (fun (attr: Attribute) -> attr.Codegen)))

    and Events = List<Event>

    type Subgraph =
        { id: string
          attributes: Attributes
          events: list<string>
          subgraphs: Subgraphs }

        member this.Codegen =
            sprintf
                "subgraph cluster_%s {\n%s\n\n%s\n\n%s\n\n%s\n}"
                this.id
                (indent (
                    String.concat "\n" (this.attributes |> List.map (fun (attribute: Attribute) -> attribute.Codegen))
                ))
                (indent (String.concat "\n" (this.events |> List.map (fun (event: string) -> event))))
                (indent (String.concat "\n" (this.subgraphs |> List.map (fun (subgraph: Subgraph) -> subgraph.Codegen))))
                (indent (sprintf "invisible_%s [shape = plaintext, label=\"\"]" this.id)) // Invisible dummy object for relationships to/from nestings.

    and Subgraphs = List<Subgraph>

    type EdgeAttribute =
        | Arrowhead of string
        | Ltail of string
        | Lhead of string

        member this.Codegen =
            match this with
            | Arrowhead(shape) -> sprintf "arrowhead=%s" shape
            | Ltail(label) -> sprintf "ltail=%s" label
            | Lhead(label) -> sprintf "lhead=%s" label

    and EdgeAttributes = list<EdgeAttribute>

    type Relationship =
        { source: string
          sink: string
          attributes: EdgeAttributes }

        member this.Codegen: string =
            sprintf
                "%s -> %s [%s]"
                this.source
                this.sink
                (String.concat ", " (this.attributes |> List.map (fun (attr: EdgeAttribute) -> attr.Codegen)))

    and Relationships = List<Relationship>

    type GraphvizFigure =
        { graphAttributes: Attributes
          events: Events
          subgraphs: Subgraphs
          relationships: Relationships }

        member this.Codegen: string =
            sprintf
                "digraph {\n%s\n\n%s\n\n%s\n\n%s\n}"
                (indent (
                    String.concat
                        "\n"
                        (this.graphAttributes
                         |> List.map (fun (graphAttribute: Attribute) -> graphAttribute.Codegen))
                ))
                (indent (String.concat "\n" (this.events |> List.map (fun (event: Event) -> event.Codegen))))
                (indent (String.concat "\n" (this.subgraphs |> List.map (fun (subgraph: Subgraph) -> subgraph.Codegen))))
                (indent (
                    String.concat
                        "\n"
                        (this.relationships
                         |> List.map (fun (relationship: Relationship) -> relationship.Codegen))
                ))

module GraphvizUtils =
    open GraphvizTypes
    open DCRGraphDTO

    let mutable SYMBOLTABLE: Map<string, DcrNamed> = Map.empty

    let extractDcrLabel (dcrEvent: DcrEventDTO) : string =
        sprintf "%s|%s|%s" dcrEvent.Initiator dcrEvent.Action (String.concat ", " dcrEvent.Receivers)

    let DcrEventsToGraphvizEvents (dcrEvents: list<DcrEventDTO>) : list<Event> =
        let events: list<Event> =
            dcrEvents
            |> List.map (fun (dcrEvent: DcrEventDTO) ->
                { id = dcrEvent.Id
                  attributes = [ Label(extractDcrLabel dcrEvent); Shape(Record) ] })

        events

    let rec DcrNestingToGraphvizSubgraph (dcrNesting: DcrNestingDTO) : Subgraph =
        let subgraph: Subgraph =
            { id = dcrNesting.Id
              attributes = [ Rankdir("LR"); Label(dcrNesting.Id); Style("dashed"); Labelloc("b") ]
              events = dcrNesting.Events
              subgraphs =
                dcrNesting.ChildNestings
                |> List.map (fun (nesting: DcrNestingDTO) -> DcrNestingToGraphvizSubgraph nesting) }

        subgraph

    let GetNamedString (id: string) (prefix: string) : string =
        match SYMBOLTABLE.TryFind(id) with
        | Some(dcrNamed) ->
            match dcrNamed with
            | DcrEventDTO(event) -> id
            | DcrNestingDTO(nesting) -> sprintf "%s_%s" prefix id
        | None -> failwith (sprintf "Could not find %s in Symbol Table. Should not happen. Symbol table: %A" id SYMBOLTABLE)

    let DcrRelationshipToGraphvizRelationship (dcrRelationship: DcrRelationshipDTO) : list<Relationship> =
        let mutable relationships: list<Relationship> = []

        for rel in dcrRelationship.Relations do
            let arrowhead =
                match rel.RelationType with
                | "Milestone" -> Arrowhead("odiamond")
                | "Response" -> Arrowhead("dotnormal")
                | "Condition" -> Arrowhead("normaldot")
                | "Inclusion" -> Arrowhead("dot")
                | "Exclusion" -> Arrowhead("odot")
                | "Cancel" -> Arrowhead("curveicurve")
                | _ -> failwith "RelationType not supported"


            let relationship =
                { source = GetNamedString dcrRelationship.From "invisible"
                  sink = GetNamedString dcrRelationship.To "invisible"
                  attributes =
                    [ arrowhead
                      Ltail(GetNamedString dcrRelationship.From "cluster")
                      Lhead(GetNamedString dcrRelationship.To "cluster") ] }

            relationships <- relationship :: relationships

        relationships

    let DCRGraphToGraphviz (graph: DCRGraphDTO) : GraphvizFigure =
        SYMBOLTABLE <- getSymbolTable graph

        let graphvizFigure: GraphvizFigure =
            { events = DcrEventsToGraphvizEvents graph.Events
              graphAttributes = [ Compound(true); Rankdir("LR") ]
              subgraphs =
                graph.Nestings
                |> List.map (fun (n: DcrNestingDTO) -> DcrNestingToGraphvizSubgraph n)
              relationships =
                graph.Relationships
                |> List.collect (fun (r: DcrRelationshipDTO) -> DcrRelationshipToGraphvizRelationship r) }

        graphvizFigure
