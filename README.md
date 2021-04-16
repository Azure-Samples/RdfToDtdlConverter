---
page_type: sample
languages:
- csharp
products:
- azure-digital-twins
name: RdfToDtdlConverter sample application to convert RDF to DTDL
description: .NET Core command-line application that translates an RDF-based ontology to JSON-LD-based Digital Twins Definition Language (DTDL)
urlFragment: digital-twins-model-conversion-samples
---

# RdfToDtdlConverter

![License](https://img.shields.io/badge/license-MIT-green.svg) ![.NET Core](https://github.com/Azure-Samples/RdfToDtdlConverter/workflows/.NET%20Core/badge.svg)

**RdfToDtdlConverter** is a .NET Core command-line **sample application** that converts an RDF-based ontology to JSON-LD-based [Digital Twins Definition Language (DTDL) version 2](https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/dtdlv2.md) for use by the [Azure Digital Twins](https://docs.microsoft.com/azure/digital-twins/overview) service. 

This sample application accompanies the [***Converting industry-standard models to DTDL***](https://docs.microsoft.com/azure/digital-twins/concepts-convert-models) article and demonstrates how to convert an RDF-based model. 

It includes a sample Pizza (Turtle) model ([WebVOWL](http://www.visualdataweb.de/webvowl/#iri=https://raw.githubusercontent.com/Azure-Samples/RdfToDtdlConverter/main/Pizza.ttl)) which is used to demonstrate the conversion process. You can use this sample to see the conversion patterns in context, and to have as a building block for your own applications performing model conversions according to your own specific needs.

## Features

**RdfToDtdlConverter** provides the following features:

* Conversion of RDF-based ontology files (Turtle, RDF/XML, etc.) to JSON-LD-based DTDL v2.
* Customizable DTMI prefix
* DTDL validation using ```Microsoft.Azure.DigitalTwins.Parser```
* A sample model (Pizza.ttl) and its converted DTDL model (Pizza.json)
* Simple, extensible

## Usage

```RdfToDtdlConverter.exe --rdf-file Pizza.ttl --dtdl-file Pizza.json --dtmi-prefix com:example --model-version 2```

## Options

```
--rdf-file          Path to rdf input file. Example, c:\Pizza.ttl
--dtdl-file         DTDL output file. Example, Pizza.json
--dtmi-prefix       Digital Twin Model Identifier prefix. Example, com:example
--model-version     Digital Twin Model Identifier model version. Example, 1 as in dtmi:com:example:Thermostat;1
```

## OWL/RDFS to DTDL Mapping

The RdfToDtdlConverter maps OWL/RDFS constructs to DTDL v2 constructs according to the following table:

| RDFS/OWL Construct  |                      | DTDL Construct       |                                    |
|---------------------|----------------------|----------------------|------------------------------------|
| Classes             | owl:Class            | Interface            | @type:Interface                    |
|                     | IRI suffix           |                      | @id                                |
|                     | rdfs:label           |                      | displayName                        |
|                     | rdfs:comment         |                      | comment                            |
| Subclasses          | owl:Class            | Interface            | @type:Interface                    |
|                     | IRI suffix           |                      | @id                                |
|                     | rdfs:label           |                      | displayName                        |
|                     | rdfs:comment         |                      | comment                            |
|                     | rdfs:subClassOf      |                      | extends                            |
| Datatype Properties | owl:DatatypeProperty | Interface Properties | @type:Property                     |
|                     | rdfs:label or INode  |                      | name                               |
|                     | rdfs:label           |                      | displayName                        |
|                     | rdfs:range           |                      | schema                             |
|                     | rdfs:comment         |                      | comment                            |
| Annotation Properties | owl:AnnotationProperty | Interface Properties | @type:Property                     |
|                     | rdfs:label or INode  |                      | name                               |
|                     | rdfs:label           |                      | displayName                        |
|                     | rdfs:range           |                      | schema                             |
|                     | rdfs:comment         |                      | comment                            |
| Object Properties   | owl:ObjectProperty   | Relationship         | @type:Relationship                 |
|                     | rdfs:label or INode  |                      | name                               |
|                     | rdfs:range           |                      | target or omitted if no rdfs:range |
|                     | rdfs:comment         |                      | comment                            |
|                     | rdfs:label           |                      | displayName                        |

Depending on the model, you may need to modify the code and mappings. For example, some industry models use ```skos:definition``` rather than ```rdfs:comment```.

For DTDL ```Property```, the following primitive [schema](https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/dtdlv2.md#schemas) mappings have been implemented:
```
_map.Add("http://www.w3.org/2001/XMLSchema#boolean", "boolean");
_map.Add("http://www.w3.org/2001/XMLSchema#date", "date");
_map.Add("http://www.w3.org/2001/XMLSchema#dateTime", "dateTime");
_map.Add("http://www.w3.org/2001/XMLSchema#double", "double");
_map.Add("http://www.w3.org/2001/XMLSchema#duration", "duration");
_map.Add("http://www.w3.org/2001/XMLSchema#float", "float");
_map.Add("http://www.w3.org/2001/XMLSchema#int", "integer");
_map.Add("http://www.w3.org/2001/XMLSchema#integer", "integer");
_map.Add("http://www.w3.org/2001/XMLSchema#long", "long");
_map.Add("http://www.w3.org/2001/XMLSchema#string", "string");
_map.Add("http://www.w3.org/2001/XMLSchema#time", "time");
```

### Other Implementation Details
- By default, all DTDL properties have their ```writable``` property set to ```true```
- owl:Restriction -> DTDL not implemented.
- owl:? -> DTDL Telemetry not implemented.
- owl:? -> DTDL Command not implemented.
- owl:? -> DTDL Component not implemented.
- owl:Imports are not imported. The DTDL parser will flag these missing Interfaces during the validation phase with the following message:
  - ```No DtmiResolver provided to resolve requisite reference(s): dtmi:...```
  - You can add these Interfaces manually to the JSON output file. 
- owl:DisjointWith we assume all owl:Classes are disjoint, even if owl:disjointWith is omitted. 
  - In DTDL v2, a twin instance can only be created from a single DTDL interface.
  - As a work around, you can use TDL’s ```extends``` property to create another Interface that inherits from two other DTDL Interfaces, and create your twin instance from it.
- owl:ObjectProperty + owl:Domain + owl:Range are required to create a DTDL Relationship. 
  - If no owl:Range, we omit ```target```, which means the target can be any interface. 
  - If no owl:Domain, the DTDL relationship is not created. 
