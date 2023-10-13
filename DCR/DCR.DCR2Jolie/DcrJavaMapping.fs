namespace DCR2Jolie

module DcrJavaMapping =
    open DCRGraph.DCRGraph
    open JavaConstructs

    let makeJavaClassMethod (e: Event): Method = 
        let parameters: ParameterList = 
            {
                Parameters = 
                [
                    {
                        Type = Type.Type "Value"; Name = "request"
                    }
                ]
            }

        {
            Name = e.Id
            AccessModifier = AccessModifier.Public
            ReturnType = ReturnType.Void
            Parameters = parameters
            Body = Some ({ Statements = List.Empty})
        }
    
    let makeJavaInterfaceMethod (e: Event): Method = 
        let parameters: ParameterList = 
            {
                Parameters = 
                [
                    {
                        Type = Type.Type "Value"; Name = "request"
                    }
                ]
            }

        {
            Name = e.Id
            AccessModifier = AccessModifier.Public
            ReturnType = ReturnType.Void
            Parameters = parameters
            Body = None
        }
    let makeJavaInterfaceMethods (receiving: Set<Event>): MethodList = 
        let methodList = [ for e in receiving -> makeJavaInterfaceMethod e ]
        {
            Methods = methodList
        }
    let makeJavaClassMethods (receiving: Set<Event>): MethodList = 
        let methodList = [ for e in receiving -> makeJavaClassMethod e ]
        {
            Methods = methodList
        }

    let makeJavaClass (role: string) (receiving: Set<Event>): JavaClass = 
        let methodList: MethodList = makeJavaClassMethods receiving

        {
            ClassName = sprintf "%sService" role
            AccessModifier = AccessModifier.Public
            Extends = "JavaService"
            Implements = sprintf"I%sService" role
            Methods = methodList
        }

    let makeJavaServiceInterfaceImports : List<JavaImport> =
        let jValue = {
            Name = "jolie.runtime.Value"
        }
        [jValue]

    let makeJavaServiceImports : List<JavaImport> =
        let jCommMessage = {
            Name = "jolie.net.CommMessage"
        }
        let jJavaService = {
            Name = "jolie.runtime.JavaService"
        }
        let jValue = {
            Name = "jolie.runtime.Value"
        }
        [jCommMessage; jJavaService; jValue]

    let makeJavaPackage (role: string) : JavaPackage = 
        {
            PackageName = role.ToLower()
        }

    let makeJavaInterface (role: string) (receiving: Set<Event>): JavaInterface =
        let methodList: MethodList = makeJavaInterfaceMethods receiving
        {
            Name = sprintf "I%sService" role
            AccessModifier = AccessModifier.Public
            Methods = methodList
        }