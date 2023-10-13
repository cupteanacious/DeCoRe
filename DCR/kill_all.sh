#!/bin/bash

stateManagerPid=$(lsof -t -i:5105)
orchestratorPid=$(lsof -t -i:9000)
buyerPid=$(lsof -t -i:9001)
seller1Pid=$(lsof -t -i:9002)
seller2Pid=$(lsof -t -i:9003)
shipperPid=$(lsof -t -i:9004)

kill -9 $stateManagerPid $orchestratorPid $buyerPid $seller1Pid $seller2Pid $shipperPid