# Setup
This codebase requires .NET 7.0 to run. .NET 7.0 can be downloaded from https://dotnet.microsoft.com/en-us/download/dotnet.

It was tested to work using the following versions:
```
.NET SDK:
	Version:   7.0.202
	Commit:    6c74320bc3

.NET runtimes:
	Microsoft.AspNetCore.App 7.0.4
	Microsoft.NETCore.App 7.0.4

MSBuild version 17.5.0+6f08c67f3 for .NET
```

# Running the CLI

To run the CLI from a terminal, go to the folder `DCRCompiler/DCRCompiler.CLI/` and run the command: `dotnet run`. This will output a help message of how to use the CLI.
For a quick test, run `dotnet run --run-example`. 


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