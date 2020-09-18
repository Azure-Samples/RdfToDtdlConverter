# RdfToDtdlConverter

**RdfToDtdlConverter** is a .NET Core command-line application that translates an OWL-based ontology to JSON-LD-based [Digital Twins Definition Language (DTDL) version 2](https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/dtdlv2.md) for use by the [Azure Digital Twins](https://docs.microsoft.com/azure/digital-twins/overview) service. 

This sample accompanies the TBD article.

## Features

This project framework provides the following features:

* Feature 1
* Feature 2
* ...

# Usage

```RdfToDtdlConverter.exe --rdf-file Pizza.ttl --dtdl-file Pizza.json --dtmi-prefix com:example --model-version 2```

# Options

```
--rdf-file          Path to rdf input file. Example, c:\Pizza.ttl
--dtdl-file         DTDL output file. Example, Pizza.json
--dtmi-prefix       Digital Twin Model Identifier prefix. Example, com:example
--model-version     Digital Twin Model Identifier model version. Example, 1 as in dtmi:com:example:Thermostat;1
```

# OWL/RDFS to DTDL Mapping

The RdfToDtdlConverter maps OWL/RDFS constructs to DTDL v2 constructs according to the following table:

| RDFS/OWL Concept       | RDFS/OWL Construct     | DTDL Concept     | DTDL Construct(s)     |
| :------------- | :----------: | -----------: | -----------: |
|  Classes | owl:Class   | Interface    | @type:Interface |
| Classes   | rdfs:label |Interface | @id, displayName

ToDo - Finish above mapping table

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

# Limitations and Known Issues
- owl:AnnotationProperty -> DTDL not implemented.
- owl:Restriction -> DTDL not implemented.
- owl:? -> DTDL Telemetry not implemented.
- owl:? -> DTDL Command not implemented.
- owl:? -> DTDL Component not implemented.
- owl:Imports are not imported. The DTDL parser will flag these missing Interfaces during the validation phase. You can add these Interfaces manually to the JSON output file. 
- owl:DisjointWith we assume all owl:Classes are disjoint, even if owl:disjointWith is omitted.
- owl:ObjectProperty + owl:Domain + owl:Range are required to create a DTDL Relationship. 
  - If no owl:Range, we omit ```target```, which means the target can be any interface. 
  - If no owl:Domain, the DTDL relationship is not created. 
- owl:DatatypeProperty + owl:Domain + owl:Range are required to create a DTDL Property.
- Duplicate DTMI Ids are possible when converting an ontology that has duplicate class names in the class heirarchy. You may need to implement logic to detect and resolve duplicate Ids, such as including the super class name in the DTMI.

# Resources

(Any additional resources or related projects)

- Link to supporting information
- Link to similar sample
- ...
