service ServiceWithMultiplePort {
    inputPort InputPortName {
        location: "socket://localhost:8000"
        protocol: sodep
        interfaces: MyInterface
    }

    outputPort OutputPortName {
        location: "socket://localhost:8001"
        protocol: sodep
        interfaces: MyInterface2
    }

    main {
        nullProcess
    }
}