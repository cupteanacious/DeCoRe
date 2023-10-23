# Setup
This codebase requires .NET 7.0 to run. .NET 7.0 can be downloaded from https://dotnet.microsoft.com/en-us/download/dotnet.
For Jolie code generation, the Java Development Kit 17 (JDK 17), as well as Maven, are required as well.
JDK 17: https://www.oracle.com/java/technologies/downloads/#java17
Maven: https://maven.apache.org/download.cgi


It was tested to work using the following versions:
```
.NET SDK:
	Version:   7.0.202
	Commit:    6c74320bc3

.NET runtimes:
	Microsoft.AspNetCore.App 7.0.4
	Microsoft.NETCore.App 7.0.4

MSBuild version 17.5.0+6f08c67f3 for .NET

JDK 17

Apache Maven 3.9.5
```

# Running the CLI
To run the CLI from a terminal, go to the folder `DeCoRe/DCR/DCR.CLI` and run the command: `dotnet run`. This will output a help message of how to use the CLI.
Typically, you will run it like so: 
`dotnet run --flatten --codegen --output <output_path> <input_file>`
E.g.:
`dotnet run --flatten --codegen --output ./out examples/buyer-seller-shipper.json`

To get dot notation outputted as well, include `--dot`. It will be saved in `<output_folder>/graphviz/<input_filename>.dot`. Grab it and use e.g. https://dreampuf.github.io/GraphvizOnline/ to visualize it.

If an ArguParseException occurs because of malformed arguments, it often makes more sense to look at the usage example from the help message, than what the reported error is, as it can be misleading. 
`USAGE: dcr_compiler.exe [--help] [--flatten] [--codegen] [--dot] [--force <bool>] [--verbose] [--overwrite] [--output <path>] <DCR File>`

# Tests

To run the tests from a terminal, go to the folder `DCRCompiler/` and run the command: `dotnet test`. 
To list the name of each test that is run, together with its outcome, run the command: `dotnet test -l:"console;verbosity=normal`. 

To debug the tests in VSCode, you will need the `DCRCompiler/.vscode/launch.json` and `DCRCompiler/.vscode/tasks.json` files as they appear in this repository. Inside VSCode, press the menu item `Terminal -> Run Task... -> debugs tests`. You should see output in the VSCode terminal, similiar to the following output, with a process ID listed for you to attach to:

```
Starting test execution, please wait...
A total of 1 test files matched the specified pattern.
Host debugging is enabled. Please attach debugger to testhost process to continue.
Process Id: 34945, Name: dotnet
Waiting for debugger attach...
```

In this instance, it was `34945`. Press the menu item `Run -> Start Debugging -> <Enter process id>`
This will attach you to the running tests. Set some breakpoints and continue with the debugging.

Notice that by default, `Module is optimized and the debugger option 'Just My Code' is enabled.` is set, which means that the symbols for Microsoft libraries themselves are not loaded. If you want to debug these as well, google how to disable module optimization and "Just My Code". 

# License
This code is released under the MIT license. 