service ServiceWithInputPort {
    inputPort InputPortName {
        location: "socket://localhost:8000"
        protocol: sodep
        interfaces: MyInterface
    }

    main {
        nullProcess
    }
}