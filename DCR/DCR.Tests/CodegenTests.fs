namespace Tests

open System.IO
open NUnit.Framework

open DCR2Jolie.Utils

open DCR2Jolie.JolieConstructs
open DCR2Jolie.JavaConstructs

module TestUtils =
    let getFileStream path =
        try
            Some (new FileStream(path, FileMode.Open))
        with
        | :? IOException as ex -> 
            printfn "An I/O error occurred: %s" ex.Message
            None
    
    let writeToFile (path: string) (text: string) =
        let fileStream = new FileStream(path, FileMode.Create)
        let streamWriter = new StreamWriter(fileStream)
        streamWriter.Write(text)
        streamWriter.Close()
        fileStream.Close()

    let rec compareFiles (fs1: FileStream) (fs2: FileStream) =
        match fs1.ReadByte(),fs2.ReadByte() with
        | -1,-1 -> true //all bytes have been enumerated and were all equal
        | _,-1 -> false //the files are of different length
        | -1,_ -> false //the files are of different length
        | x,y when x <> y -> false
                //only continue to the next bytes when the present two are equal 
        | _ -> compareFiles fs1 fs2

// [<TestFixture>]
// type CodegenTests() =
//     [<Test>]
//     member this.GenerateEmptyJolieService() =
//         Assert.Ignore "Input test files not up to date anymore."

//         let testFiles = System.IO.Path.Join(__SOURCE_DIRECTORY__, "JolieTestFiles")
//         let outputDir = System.IO.Path.Join(__SOURCE_DIRECTORY__, "JolieTestFiles", "out")

//         let expectedFilePath = System.IO.Path.Join(testFiles, "emptyJolieService.ol")
//         let actualFilePath = System.IO.Path.Join(outputDir, "emptyJolieService.ol")

//         let emptyJolieService: Service = { 
//             ServiceName = "EmptyService"; 
//             MainMethod = { 
//                 OperationList = List.empty
//             }
//             PortList = List.empty 
//             Embeds = List.empty
//             Execution = ""
//         }

//         let code = emptyJolieService.Codegen { IndentLevel = 0 }
//         TestUtils.writeToFile actualFilePath code

//         let expectedFileStream = TestUtils.getFileStream expectedFilePath
//         let actualFileStream = TestUtils.getFileStream actualFilePath

//         let areEqual = TestUtils.compareFiles expectedFileStream.Value actualFileStream.Value

//         Assert.AreEqual(true, areEqual)
    
//     [<Test>]
//     member this.GenerateJolieServiceWithInputPort() =
//         Assert.Ignore "Input test files not up to date anymore."

//         let testFiles = System.IO.Path.Join(__SOURCE_DIRECTORY__, "JolieTestFiles")
//         let outputDir = System.IO.Path.Join(__SOURCE_DIRECTORY__, "JolieTestFiles", "out")

//         let expectedFilePath = System.IO.Path.Join(testFiles, "serviceWithInputPort.ol")
//         let actualFilePath = System.IO.Path.Join(outputDir, "serviceWithInputPort.ol")

//         let port1: Port = {
//             PortType = "inputPort";
//             PortName = "InputPortName"; 
//             Location = {Location = "socket://localhost:8000"}; 
//             Protocol = {Protocol = "sodep"}; 
//             Interfaces = ["MyInterface"]
//         }

//         let portList = [port1]

//         let jolieServiceWithPorts: Service = { 
//             ServiceName = "ServiceWithInputPort"; 
//             MainMethod = { 
//                 OperationList = List.empty
//             }; 
//             PortList = portList
//             Embeds = List.empty
//             Execution = ""
//         }

//         let code = jolieServiceWithPorts.Codegen { IndentLevel = 0 }
//         TestUtils.writeToFile actualFilePath code

//         let expectedFileStream = TestUtils.getFileStream expectedFilePath
//         let actualFileStream = TestUtils.getFileStream actualFilePath

//         let areEqual = TestUtils.compareFiles expectedFileStream.Value actualFileStream.Value

//         Assert.AreEqual(true, areEqual)

//     [<Test>]
//     member this.GenerateJolieServiceWithMultiplePorts() =
//         Assert.Ignore "Input test files not up to date anymore."

//         let testFiles = System.IO.Path.Join(__SOURCE_DIRECTORY__, "JolieTestFiles")
//         let outputDir = System.IO.Path.Join(__SOURCE_DIRECTORY__, "JolieTestFiles", "out")

//         let expectedFilePath = System.IO.Path.Join(testFiles, "serviceWithMultiplePorts.ol")
//         let actualFilePath = System.IO.Path.Join(outputDir, "serviceWithMultiplePorts.ol")

//         let port1: Port = {
//             PortType = "inputPort";
//             PortName = "InputPortName"; 
//             Location = {Location = "socket://localhost:8000"}; 
//             Protocol = {Protocol = "sodep"}; 
//             Interfaces = ["MyInterface"]
//         }

//         let port2: Port = {
//             PortType = "outputPort";
//             PortName = "OutputPortName"; 
//             Location = {Location = "socket://localhost:8001"}; 
//             Protocol = {Protocol = "sodep"}; 
//             Interfaces = ["MyInterface2"]
//         }

//         let portList = [port1; port2]

//         let jolieServiceWithPorts: Service = { 
//             ServiceName = "ServiceWithMultiplePort"; 
//             MainMethod = { 
//                 OperationList = List.empty
//             }; 
//             PortList = portList
//             Embeds = List.empty 
//             Execution = ""
//         }

//         let code = jolieServiceWithPorts.Codegen { IndentLevel = 0 }
//         TestUtils.writeToFile actualFilePath code

//         let expectedFileStream = TestUtils.getFileStream expectedFilePath
//         let actualFileStream = TestUtils.getFileStream actualFilePath

//         let areEqual = TestUtils.compareFiles expectedFileStream.Value actualFileStream.Value

//         Assert.AreEqual(true, areEqual)

//     [<Test>]
//     member this.GenerateCodeForImportStatements() =
//         let testFiles = System.IO.Path.Join(__SOURCE_DIRECTORY__, "JolieTestFiles")
//         let outputDir = System.IO.Path.Join(__SOURCE_DIRECTORY__, "JolieTestFiles", "out")

//         let expectedFilePath = System.IO.Path.Join(testFiles, "imports.ol")
//         let actualFilePath = System.IO.Path.Join(outputDir, "imports.ol")

//         let import1: Import = {
//             ModuleSpecifier = "a";
//             ImportTarget = "A"
//         }

//         let import2: Import = {
//             ModuleSpecifier = ".b";
//             ImportTarget = "B"
//         }

//         let import3: Import = {
//             ModuleSpecifier = "..c";
//             ImportTarget = "C"
//         }

//         let importList = [import1; import2; import3]

//         let code = GenerateCodegenList importList { IndentLevel = 0 } "\n"

//         TestUtils.writeToFile actualFilePath code

//         let expectedFileStream = TestUtils.getFileStream expectedFilePath
//         let actualFileStream = TestUtils.getFileStream actualFilePath
//         let areEqual = TestUtils.compareFiles expectedFileStream.Value actualFileStream.Value
//         Assert.AreEqual(true, areEqual)

//     [<Test>]
//     member this.GenerateSimpleJolieInterface() =
//         let testFiles = System.IO.Path.Join(__SOURCE_DIRECTORY__, "JolieTestFiles")
//         let outputDir = System.IO.Path.Join(__SOURCE_DIRECTORY__, "JolieTestFiles", "out")

//         let expectedFilePath = System.IO.Path.Join(testFiles, "simpleJolieInterface.ol")
//         let actualFilePath = System.IO.Path.Join(outputDir, "simpleJolieInterface.ol")

//         let simpleJolieInterface: JolieInterface = { 
//             Name = "SimpleInterface"; 
//             OneWayOperations = [
//                 {
//                     Name = "owOp1";
//                     RequestType = "type1";
//                 }
//             ];
//             RequestResponseOperations = [
//                 {
//                     Name = "rrOp1";
//                     RequestType = "type2";
//                     ResponseType = "type3";
//                 }
//             ]
//         }

//         let code = simpleJolieInterface.Codegen { IndentLevel = 0 }
//         TestUtils.writeToFile actualFilePath code

//         let expectedFileStream = TestUtils.getFileStream expectedFilePath
//         let actualFileStream = TestUtils.getFileStream actualFilePath

//         let areEqual = TestUtils.compareFiles expectedFileStream.Value actualFileStream.Value

//         Assert.AreEqual(true, areEqual)

//     [<Test>]
//     member this.GenerateSimpleJolieInterfaceOneWayOnly() =
//         let testFiles = System.IO.Path.Join(__SOURCE_DIRECTORY__, "JolieTestFiles")
//         let outputDir = System.IO.Path.Join(__SOURCE_DIRECTORY__, "JolieTestFiles", "out")

//         let expectedFilePath = System.IO.Path.Join(testFiles, "simpleJolieInterfaceOneWayOnly.ol")
//         let actualFilePath = System.IO.Path.Join(outputDir, "simpleJolieInterfaceOneWayOnly.ol")

//         let simpleJolieInterface: JolieInterface = { 
//             Name = "SimpleInterface"; 
//             OneWayOperations = [
//                 {
//                     Name = "owOp1";
//                     RequestType = "type1";
//                 };
//                 {
//                     Name = "owOp2";
//                     RequestType = "type2";
//                 }
//             ];
//             RequestResponseOperations = List.Empty
//         }

//         let code = simpleJolieInterface.Codegen { IndentLevel = 0 }
//         TestUtils.writeToFile actualFilePath code

//         let expectedFileStream = TestUtils.getFileStream expectedFilePath
//         let actualFileStream = TestUtils.getFileStream actualFilePath

//         let areEqual = TestUtils.compareFiles expectedFileStream.Value actualFileStream.Value

//         Assert.AreEqual(true, areEqual)

//     [<Test>]
//     member this.GenerateSimpleJolieInterfaceReqResOnly() =
//         let testFiles = System.IO.Path.Join(__SOURCE_DIRECTORY__, "JolieTestFiles")
//         let outputDir = System.IO.Path.Join(__SOURCE_DIRECTORY__, "JolieTestFiles", "out")

//         let expectedFilePath = System.IO.Path.Join(testFiles, "simpleJolieInterfaceReqResOnly.ol")
//         let actualFilePath = System.IO.Path.Join(outputDir, "simpleJolieInterfaceReqResOnly.ol")

//         let simpleJolieInterface: JolieInterface = { 
//             Name = "SimpleInterface"; 
//             OneWayOperations = List.Empty;
//             RequestResponseOperations = [
//                 {
//                     Name = "rrOp1";
//                     RequestType = "type1";
//                     ResponseType = "type2";
//                 };
//                 {
//                     Name = "rrOp2";
//                     RequestType = "type3";
//                     ResponseType = "type4";
//                 }
//             ]
//         }

//         let code = simpleJolieInterface.Codegen { IndentLevel = 0 }
//         TestUtils.writeToFile actualFilePath code

//         let expectedFileStream = TestUtils.getFileStream expectedFilePath
//         let actualFileStream = TestUtils.getFileStream actualFilePath

//         let areEqual = TestUtils.compareFiles expectedFileStream.Value actualFileStream.Value

//         Assert.AreEqual(true, areEqual)

//     [<Test>]
//     member this.GenerateSimpleJolieInterfaceEmpty() =
//         let testFiles = System.IO.Path.Join(__SOURCE_DIRECTORY__, "JolieTestFiles")
//         let outputDir = System.IO.Path.Join(__SOURCE_DIRECTORY__, "JolieTestFiles", "out")

//         let expectedFilePath = System.IO.Path.Join(testFiles, "simpleJolieInterfaceEmpty.ol")
//         let actualFilePath = System.IO.Path.Join(outputDir, "simpleJolieInterfaceEmpty.ol")

//         let simpleJolieInterface: JolieInterface = { 
//             Name = "SimpleInterface"; 
//             OneWayOperations = List.Empty;
//             RequestResponseOperations = List.Empty
//         }

//         let code = simpleJolieInterface.Codegen { IndentLevel = 0 }
//         TestUtils.writeToFile actualFilePath code

//         let expectedFileStream = TestUtils.getFileStream expectedFilePath
//         let actualFileStream = TestUtils.getFileStream actualFilePath

//         let areEqual = TestUtils.compareFiles expectedFileStream.Value actualFileStream.Value

//         Assert.AreEqual(true, areEqual)

//     [<Test>]
//     member this.GenerateJolieType() =
//         let testFiles = System.IO.Path.Join(__SOURCE_DIRECTORY__, "JolieTestFiles")
//         let outputDir = System.IO.Path.Join(__SOURCE_DIRECTORY__, "JolieTestFiles", "out")

//         let expectedFilePath = System.IO.Path.Join(testFiles, "simpleJolieType.ol")
//         let actualFilePath = System.IO.Path.Join(outputDir, "simpleJolieType.ol")

//         let jolieType: JolieType = { 
//             Name = "SimpleType"; 
//             Fields = [
//                 { Name = "f1"; FieldType = "string" }
//                 { Name = "f2"; FieldType = "string" }
//                 { Name = "f3"; FieldType = "int" }
//             ]
//         }

//         let code = jolieType.Codegen { IndentLevel = 0 }
//         TestUtils.writeToFile actualFilePath code

//         let expectedFileStream = TestUtils.getFileStream expectedFilePath
//         let actualFileStream = TestUtils.getFileStream actualFilePath

//         let areEqual = TestUtils.compareFiles expectedFileStream.Value actualFileStream.Value

//         Assert.AreEqual(true, areEqual)


//     [<Test>]
//     member this.GenerateJolieOperations() =
//         Assert.Ignore "Input test files not up to date anymore."

//         let testFiles = System.IO.Path.Join(__SOURCE_DIRECTORY__, "JolieTestFiles")
//         let outputDir = System.IO.Path.Join(__SOURCE_DIRECTORY__, "JolieTestFiles", "out")

//         let expectedFilePath = System.IO.Path.Join(testFiles, "jolieServiceOperations.ol")
//         let actualFilePath = System.IO.Path.Join(outputDir, "jolieServiceOperations.ol")

//         let invokeOrderOnSeller : OperationExecutionOW =
//             {
//                 OperationName = "order";
//                 Request = "request";
//                 ServiceName = "Seller"
//             }

//         let BuyerOrdersSellerOperationHeader : OneWayOperation =
//             {
//                 Name = "BuyerOrdersSeller";
//                 RequestType = "request"
//             }

//         let BuyerOrdersSellerOperationDefinition : OperationDefinition =
//             {
//                 Body = [OperationExecutionOW invokeOrderOnSeller];
//                 OperationType = OneWayOperation BuyerOrdersSellerOperationHeader
//             }

//         ///
        
//         let invokeConfirmBuyer : OperationExecutionOW =
//             {
//                 OperationName = "confirm";
//                 Request = "request";
//                 ServiceName = "Buyer"
//             }

//         let SellerConfirmsBuyerOperationHeader : OneWayOperation =
//             {
//                 Name = "SellerConfirmsBuyer";
//                 RequestType = "request"
//             }

//         let SellerConfirmsBuyerOperationDefinition : OperationDefinition =
//             {
//                 Body = [OperationExecutionOW invokeConfirmBuyer];
//                 OperationType = OneWayOperation SellerConfirmsBuyerOperationHeader
//             }

//         let OperationDefinitionList =
//             {
//                 OperationList = [
//                     BuyerOrdersSellerOperationDefinition;
//                     SellerConfirmsBuyerOperationDefinition
//                 ]
//             }

//         let code = OperationDefinitionList.Codegen { IndentLevel = 0 }
//         TestUtils.writeToFile actualFilePath code

//         let expectedFileStream = TestUtils.getFileStream expectedFilePath
//         let actualFileStream = TestUtils.getFileStream actualFilePath

//         let areEqual = TestUtils.compareFiles expectedFileStream.Value actualFileStream.Value

//         Assert.AreEqual(true, areEqual)

//     [<Test>]
//     member this.GenerateJavaInterface() =
//         let testFiles = System.IO.Path.Join(__SOURCE_DIRECTORY__, "JolieTestFiles")
//         let outputDir = System.IO.Path.Join(__SOURCE_DIRECTORY__, "JolieTestFiles", "out")

//         let expectedFilePath = System.IO.Path.Join(testFiles, "simpleJavaInterface.java")
//         let actualFilePath = System.IO.Path.Join(outputDir, "simpleJavaInterface.java")

//         let method1: Method = {
//             Name = "simpleJavaMethod1";
//             AccessModifier = AccessModifier.Public;
//             ReturnType = ReturnType.Void;
//             Parameters = {
//                 Parameters = [
//                     { Name = "param1"; Type = Type.Type "string" }
//                 ]
//             };
//             Body = None
//         }

//         let method2: Method = {
//             Name = "simpleJavaMethod2";
//             AccessModifier = AccessModifier.Public;
//             ReturnType = ReturnType.Void;
//             Parameters = {
//                 Parameters = [
//                     { Name = "param2"; Type = Type.Type "string" }
//                 ]
//             };
//             Body = None
//         }

//         let methods: MethodList = {
//             Methods = [method1; method2]
//         }

//         let javaInterface: JavaInterface = { 
//             Name = "SimpleJavaInterface";
//             AccessModifier = AccessModifier.Public;
//             Methods = methods
//         }

//         let code = javaInterface.Codegen { IndentLevel = 0 }
//         TestUtils.writeToFile actualFilePath code

//         let expectedFileStream = TestUtils.getFileStream expectedFilePath
//         let actualFileStream = TestUtils.getFileStream actualFilePath

//         let areEqual = TestUtils.compareFiles expectedFileStream.Value actualFileStream.Value

//         Assert.AreEqual(true, areEqual)