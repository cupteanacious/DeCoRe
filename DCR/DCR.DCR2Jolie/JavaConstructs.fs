namespace DCR2Jolie

module JavaConstructs =
    open Utils

    type Type =
        | Type of string
        member this.Codegen =
            match this with
            | Type t -> sprintf "%s" t

    type ReturnType =
        | Void
        | Type of Type
        member this.Codegen =
            match this with
            | Void -> sprintf "void"
            | Type t -> sprintf "%s" t.Codegen

    type AccessModifier =
        | Public
        | Private
        member this.Codegen =
            match this with 
            | Public -> sprintf "public"
            | Private -> sprintf "private"

    type Parameter = 
        {
            Type: Type
            Name: string
        }
        member this.Codegen (ctx: Context) =
            sprintf "%s %s" this.Type.Codegen this.Name

    type ParameterList = 
        {
            Parameters: List<Parameter>
        }
        member this.Codegen =
            GenerateCodegenList this.Parameters { IndentLevel=0 } ", "

    type Statement =
        | CodeBlock of CodeBlock
        member this.Codegen (ctx: Context) =
            match this with 
            | CodeBlock cb -> cb.Codegen ctx 
    type StatementList =
        {
            Statements: List<Statement>
        }
        member this.Codegen (ctx: Context) =
            GenerateCodegenList this.Statements ctx "\n"

    type Method =
        {
            Name: string
            AccessModifier: AccessModifier
            ReturnType: ReturnType
            Parameters: ParameterList
            Body: StatementList option
        }
        member this.Codegen (ctx: Context) =
            let bodyCode = 
                match this.Body with 
                | Some body -> sprintf " {\n%s\n}" (body.Codegen ctx.IncrementIndentLevel)
                | None -> ";"  // No body, this is an interface method
            
            ctx.Indent (
                sprintf "%s %s %s( %s )%s" 
                    this.AccessModifier.Codegen
                    this.ReturnType.Codegen
                    this.Name
                    (this.Parameters.Codegen)
                    bodyCode
                    )

    type MethodList =
        {
            Methods: List<Method>
        }
        member this.Codegen (ctx: Context) =
            GenerateCodegenList this.Methods ctx "\n\n"

    type JavaInterface =
        {
            Name: string
            AccessModifier: AccessModifier
            Methods: MethodList
        }
        member this.Codegen (ctx: Context) = 
            sprintf "%s interface %s {\n%s\n}"
                this.AccessModifier.Codegen
                this.Name
                (this.Methods.Codegen ctx.IncrementIndentLevel)

     type JavaClass = 
        {
            ClassName: string
            AccessModifier: AccessModifier 
            Extends: string
            Implements: string
            Methods: MethodList
        }
        member this.Codegen (ctx: Context) =
            let mutable more = ""
            if this.Extends.Length > 0 then
                more <- sprintf " extends %s" this.Extends
            if this.Implements.Length > 0 then
                more <- more + sprintf " implements %s" this.Implements

            sprintf "%s class %s%s\n{\n%s\n}\n"
                this.AccessModifier.Codegen
                this.ClassName
                more
                (this.Methods.Codegen ctx.IncrementIndentLevel)

    type JavaImport =
        {
            Name: string
        }
        member this.Codegen =
            sprintf "import %s;" this.Name

    type JavaPackage =
        {
            PackageName: string
        }
        member this.Codegen =
            sprintf "package %s;" this.PackageName
