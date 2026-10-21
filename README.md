# Electrical Design Toolkit

[![CI](https://github.com/cooperyungblut/electrical-toolkit/actions/workflows/ci.yml/badge.svg)](https://github.com/cooperyungblut/electrical-toolkit/actions/workflows/ci.yml)

A set of calculation tools for electrical design work. The first one is a panel load balancer: give it a panel size and a list of loads, and it assigns breaker slots to keep the phases as even as possible.

## Current state

- 208Y/120V three-phase panels, single-pole loads
- Greedy balancer: largest load first, onto the lightest phase that still has open slots
- Imbalance % = largest deviation from the phase average, divided by the average

Greedy isn't always optimal. Loads of 6, 3, 3, 2, 2, 2 kVA come out 7/6/5 when 6/6/6 is possible. A local-search pass to fix cases like that is next.

## Planned

Web API, Docker, Azure deployment, a web UI, 2- and 3-pole loads, saved panels, 120/240V and 480Y/277V panels. Later: conduit fill and voltage drop.

## Build

```
dotnet test
```

Requires .NET 10.

## AI assistance

*I wrote the core logic and its tests, with Claude reviewing my code along the way. Project scaffolding, CI config and this README were drafted with Claude's help.*

## Disclaimer

*This tool is a calculation aid. It does not replace the judgment of a qualified engineer, and results must be verified against the governing code, the equipment manufacturer's data, and project requirements before use.*
