# Windows Communication Foundation server plugin
[![Auto build](https://github.com/DKorablin/Plugin.WcfServer/actions/workflows/release.yml/badge.svg)](https://github.com/DKorablin/Plugin.WcfServer/releases/latest)

Remote automation layer for other SAL plugins via Windows Communication Foundation (WCF). Exposes public members of loaded plugins so they can be started, stopped, invoked, or queried for telemetry over SOAP or lightweight REST-style endpoints.

## Features
- Invoke public members of remotely loaded plugins (methods / properties)
- Start / stop plugin lifecycle
- Collect telemetry / state information
- Dual protocol surface: SOAP (strongly typed) + simplified REST wrapper for dynamic invocation
- .NET Framework 3.5 compatibility and experimental .NET 7/8 (CoreWCF / limited) support
- Packaged as a NuGet plugin module

## Why both SOAP and REST?
SOAP provides strong typing and contract metadata, but dynamic invocation scenarios (variable argument arrays, loosely typed results) are exposed via a REST style endpoint. Complex argument arrays and return values are serialized as JSON for flexibility.

## Compatibility Notes
WCF is officially supported only on .NET Framework (3.5–4.8). Modern .NET (Core / 5+ / 8) does not ship full WCF server implementation. Migration uses community packages (e.g. CoreWCF) with limitations:
- Not all bindings / behaviors available
- Security modes may differ
- Serialization nuances can appear when moving between runtimes
Test service contracts early when upgrading.

### Invoking via REST (dynamic)
```
POST /api/plugins/invoke
{
  "plugin": "SamplePlugin",
  "member": "DoWork",
  "args": [ 42, "payload" ]
}
```
Response:
```
{
  "result": "OK",
  "durationMs": 15
}
```

### Invoking via SOAP (strongly typed)
Generate client from the service metadata (WSDL). Call `DoWork` directly with typed arguments.

## Telemetry
A telemetry endpoint returns JSON describing:
- Loaded plugins
- Available members
- Basic health / uptime

## Migration Guidance
| Scenario | Recommendation |
|----------|----------------|
| New development on .NET Framework | Use full WCF feature set |
| Migrating to .NET 7/8 | Validate required bindings; prefer BasicHttpBinding equivalents |
| Advanced security (WS-* specs) | Remain on .NET Framework or redesign |
| High throughput JSON only | Consider simplifying to REST-only service |

## Configuration Tips
- Keep bindings minimal (BasicHttpBinding / WSHttpBinding) for portability
- Prefer DataContract types with version-tolerant members
- Log all dynamic invocations for audit

## Limitations
- Some WCF features missing on .NET 7/8 (transaction flow, full security stack)
- Dynamic REST invocation bypasses compile-time checks
- Requires host process to manage plugin lifecycle



---
Warning: WCF transport differences on modern .NET may cause subtle behavior changes. Thoroughly test serialization, security, and timeouts after migration.