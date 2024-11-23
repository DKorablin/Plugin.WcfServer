# Windows Communication Foundation server plugin
The main purpose of this module is to remotely automate the activities of other plugins. Based on REST or SOAP protocol you can call other public member plugin loaded remotely. Stop, Start, invoke some action or collect telemetry data in working process.

Warning! This plugin is based on WCF technology, considering that WCF technology only available from .NET 3.5 to .NET 4.8 and partially restored only in .NET 7, there can be problems while transferring applications to .NET Core versions.

Due to the dynamics and strong typing of the SOAP protocol, the response and the array of arguments are passed in the REST format.