using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Collections.Generic;
using RdfToDtdlConverter.Models;
using VDS.RDF.Ontology;
using VDS.RDF.Parsing;
using System.Linq;
using Microsoft.Azure.DigitalTwins.Parser;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using AngleSharp.Dom;

namespace RdfToDtdlConverter
{
    class Program
    {

        private static readonly OntologyGraph _ontologyGraph = new OntologyGraph();         // Ontology graph we load the file into and that we work with
        private static readonly string _context = "dtmi:dtdl:context;2";                    // DTDL v2 version
        private static List<DtdlInterface> _interfaceList = new List<DtdlInterface>();      // List of DTDL Interfaces
        private static string _dtmiPrefix;                                                  // DTMI prefix passed in from the command line
        private static ushort _modelVersion;                                                // Model version passed in from the command line
        private static Dictionary<string, string> _map = new Dictionary<string, string>();  // Primitive schema map

        /// <summary>
        /// Main method
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static int Main(string[] args)
        {

            // Create a root command with some options
            var rootCommand = new RootCommand
            {
                new Option<FileInfo>(
                    "--rdf-file",
                    "Path to rdf input file. Example, c:\\Pizza.ttl"),
                new Option<string>(
                    "--dtdl-file",
                    "DTDL output file. Example, Pizza.json"),
                new Option<string>(
                    "--dtmi-prefix",
                    "Digital Twin Model Identifier prefix. Example, com:example"),
                new Option<ushort>(
                    "--model-version",
                    "Digital Twin Model Identifier model version. Example, 1 as in dtmi:com:example:Thermostat;1"),
                new Option<ushort>(
                    "--dtdl-version",
                    "(Future) DTDL version. Currently set to dtmi:dtdl:context;2")
            };

            rootCommand.Description = "Converts Rdf model files, such as a .ttl file, to a DTDL model.";

            // Note that the parameters of the handler method are matched according to the names of the options
            rootCommand.Handler = CommandHandler.Create<FileInfo, FileInfo, string, ushort, ushort>((rdfFile, dtdlFile, dtmiPrefix, modelVersion, dtdlVersion) =>
            {
                try
                {
                    // Check for missing args
                    if (args.Length == 0)
                    {

                        throw new System.ArgumentException("One or more missing arguments.");

                    }

                    // Check if file exists
                    if (!rdfFile.Exists)
                    {

                        throw new System.ArgumentException("Rdf file does not exist.");

                    }

                    // Check if file exists
                    if (String.IsNullOrEmpty(dtdlFile.Name.ToString()))
                    {

                        throw new System.ArgumentException("Invalid or missing --dtdl-file.");

                    }

                    // Check for prefix
                    if (String.IsNullOrEmpty(dtmiPrefix))
                    {

                        throw new System.ArgumentException("Invalid or missing --dtmi-prefix");

                    }
                    else
                    {

                        _dtmiPrefix = dtmiPrefix;

                    }

                    // Check for model version
                    if (modelVersion > 0)
                    {

                        _modelVersion = modelVersion;

                    }
                    else
                    {

                        throw new System.ArgumentException("Invalid or missing --model-version");

                    }

                    try
                    {

                        CreateSchemaMap();

                        GenerateDTDL(rdfFile, dtdlFile);

                    }
                    catch (Exception e)
                    {

                        Console.WriteLine($"Conversion failed!");
                        Console.WriteLine($"{e.Message}");

                    }

                }
                catch (Exception e)
                {

                    Console.WriteLine($"Error: {e.Message}");
                    Console.WriteLine($"Use /? to view valid arguments.");

                }


            });

            // Parse the incoming args and invoke the handler
            return rootCommand.InvokeAsync(args).Result;

        }

        /// <summary>
        /// Generates a DTDL JSON file from an OWL-based RDF file.
        /// </summary>
        /// <param name="rdfFile">RDF input file</param>
        /// <param name="dtdlFile">DTDL output file</param>
        private static void GenerateDTDL(FileInfo rdfFile, FileInfo dtdlFile)
        {

            try
            {

                FileLoader.Load(_ontologyGraph, rdfFile.FullName);

                Console.WriteLine("Parsing file...");

                // Start looping through for each owl:Class
                foreach (OntologyClass owlClass in _ontologyGraph.OwlClasses)
                {

                    // Generate a DTMI for the owl:Class
                    string Id = GenerateDTMI(owlClass);

                    if (!String.IsNullOrEmpty(Id))
                    {

                        Console.WriteLine($"{owlClass.Resource.ToString()} -> {Id}");

                        // Create Interface
                        DtdlInterface dtdlInterface = new DtdlInterface
                        {
                            Id = Id,
                            Type = "Interface",
                            DisplayName = GetInterfaceDisplayName(owlClass),
                            Comment = GetInterfaceComment(owlClass),
                            Contents = new List<DtdlContents>()
                        };

                        // Use DTDL 'extends' for super classes
                        IEnumerable<OntologyClass> foundSuperClasses = owlClass.DirectSuperClasses;

                        if (foundSuperClasses.Any())
                        {

                            List<string> extendsList = new List<string>();

                            int extendsMax = 0;

                            foreach (var superClass in foundSuperClasses)
                            {

                                // DTDL v2 allows for a maximum of 2 extends. We ignore the other super classes
                                if(extendsMax < 2)
                                {

                                    string superClassId = GenerateDTMI(superClass);

                                    extendsList.Add(superClassId);

                                    extendsMax++;

                                }

                            }

                            dtdlInterface.Extends = extendsList;

                        }

                        List<OntologyProperty> properties;

                        // Get list of properties which have this class as a domain
                        properties = owlClass.IsDomainOf.ToList();

                        foreach (var property in properties)
                        {

                            if (property.Types.First().ToString() == "http://www.w3.org/2002/07/owl#ObjectProperty")
                            {

                                Console.WriteLine($"  Found relationship: {property}");

                                // If IRI, parse out relationship name from IRI
                                if (property.ToString().Contains("#"))
                                {

                                    int index = property.ToString().LastIndexOf("#");
                                    property.AddLabel(property.ToString().Substring(index + 1));

                                }
                                else if (property.ToString().Contains("/"))
                                {
                                    int index = property.ToString().LastIndexOf("/");
                                    property.AddLabel(property.ToString().Substring(index + 1));
                                }

                                // Create relationship
                                DtdlContents dtdlRelationship = new DtdlContents
                                {
                                    Name = property.ToString(),
                                    Type = "Relationship",
                                    DisplayName = GetRelationshipDisplayName(property),
                                    Comment = GetRelationshipComment(property)
                                };

                                // DTDL only supports a single target Id.
                                var range = property.Ranges.FirstOrDefault();

                                if (range == null)
                                {

                                    // If no range is found, we omit the DTDL target property.
                                    // This allows any Interface to be the target.
                                    Console.WriteLine("    No target found.");

                                }
                                else
                                {

                                    Console.WriteLine($"    Found target: {range}");

                                    // Convert range to DTMI and add to DTDL relationship target.
                                    string target = GenerateDTMI(range);
                                    dtdlRelationship.Target = target;

                                }

                                // Add relationship to the Interface
                                dtdlInterface.Contents.Add(dtdlRelationship);

                            }

                            if (property.Types.First().ToString() == "http://www.w3.org/2002/07/owl#DatatypeProperty")
                            {

                                Console.WriteLine($"  Found property: {property}");

                                // Create property
                                DtdlContents dtdlProperty = new DtdlContents
                                {
                                    Name = property.ToString(),
                                    Type = "Property",
                                    Schema = _map[property.Ranges.FirstOrDefault().ToString()]
                                };

                                // Add the Property to the Interface
                                dtdlInterface.Contents.Add(dtdlProperty);

                            }

                        }

                        // Add the DTDL context to the Interface
                        dtdlInterface.Context = _context;

                        // Add interface to the list of interfaces
                        _interfaceList.Add(dtdlInterface);

                    }

                }

                // Serialize to JSON
                var json = JsonConvert.SerializeObject(_interfaceList);

                // Save to file
                System.IO.File.WriteAllText(dtdlFile.ToString(), json);
                Console.WriteLine($"DTDL written to: {dtdlFile}");

                // Run DTDL validation
                Console.WriteLine("Validating DTDL...");

                ModelParser modelParser = new ModelParser();

                List<string> modelJson = new List<string>();

                modelJson.Add(json);

                IReadOnlyDictionary<Dtmi, DTEntityInfo> parseTask = modelParser.ParseAsync(modelJson).GetAwaiter().GetResult();

            }
            catch (ParsingException pe)
            {

                Console.WriteLine($"*** Error parsing models");

                int errCount = 1;

                foreach (ParsingError err in pe.Errors)
                {

                    Console.WriteLine($"Error {errCount}:");
                    Console.WriteLine($"{err.Message}");
                    Console.WriteLine($"Primary ID: {err.PrimaryID}");
                    Console.WriteLine($"Secondary ID: {err.SecondaryID}");
                    Console.WriteLine($"Property: {err.Property}\n");
                    errCount++;

                }

            }
            catch (Exception e)
            {

                Console.WriteLine($"{e.Message}");

            }

            Console.WriteLine($"Validation complete!");

        }

        /// <summary>
        /// Generate a DTMI
        /// </summary>
        /// <param name="resource"></param>
        /// <returns>Returns a DTMI Id</returns>
        private static string GenerateDTMI(OntologyResource resource)
        {

            string id = null;

            try
            {

                string nodeType = resource.Resource.NodeType.ToString();

                if (nodeType == "Uri")
                {
                    //Get the IRI
                    id = resource.Resource.ToString();

                    // Find the suffix after the # in the IRI when pattern is https://brickschema.org/schema/1.1/Brick#Equipment
                    if (id.Contains("#"))
                    {

                        int index = id.LastIndexOf("#");
                        id = id.Substring(index + 1);

                    }
                    else  // Or find the suffix after the last / in the IRI when pattern is http://webprotege.stanford.edu/Pizza
                    {

                        int index = id.LastIndexOf("/");
                        id = id.Substring(index + 1);

                    }

                    id = $"dtmi:{_dtmiPrefix}:{id};{_modelVersion}";

                    // Check to ensure id is <= 128 characters
                    if (id.Count() > 128)
                    {

                        throw new System.Exception($"{id} is > 128 characters.");

                    }

                }
                else
                {

                    // Console.WriteLine($"Not a URI -> {resource.ToString()}");

                }

            }
            catch (Exception e)
            {

                Console.WriteLine($"Could not generate DTMI for {resource.ToString()}");
                Console.WriteLine($"{e.Message}");
                id = null;

            }

            return id;

        }

        /// <summary>
        /// Get an display name for an Interface
        /// </summary>
        /// <param name="resource"></param>
        /// <returns>Displayname string</returns>
        private static string GetInterfaceDisplayName(OntologyResource resource)
        {

            string displayName;

            try
            {

                // Use the first rdfs:label on the owl:Class for the DTDL Interface displayName
                displayName = resource.Label.First().ToString();

                // DTDL displayName limited to 64 characters
                if (displayName.Length > 64)
                {

                    displayName = displayName.Substring(0, 64);

                }

            }
            catch (Exception e)
            {

                //Console.WriteLine($"Missing rdfs:label on {resource.ToString()}");
                //Console.WriteLine($"{e.Message}");
                displayName = null;

            }

            return displayName;

        }

        /// <summary>
        /// Get a comment for an Interface
        /// </summary>
        /// <param name="resource"></param>
        /// <returns>Comment string</returns>
        private static string GetInterfaceComment(OntologyResource resource)
        {

            string comment;

            try
            {

                // Use the first rdfs:comment on the owl:Class for the DTDL Interface comment
                comment = resource.Comment.First().ToString();

            }
            catch (Exception e)
            {

                //Console.WriteLine($"Missing rdfs:comment on {resource.ToString()}");
                //Console.WriteLine($"{e.Message}");
                comment = null;

            }

            return comment;

        }

        /// <summary>
        /// Get a display name for a Relationship
        /// </summary>
        /// <param name="relationship"></param>
        /// <returns>Display name string</returns>
        private static string GetRelationshipDisplayName(OntologyProperty relationship)
        {

            string displayName;

            try
            {

                // Use the first rdfs:label on the ObjectProperty for the DTDL relationship displayName
                displayName = relationship.Label.First().ToString();

            }
            catch (Exception e)
            {

                //Console.WriteLine($"Missing rdfs:label on {relationship.ToString()}");
                //Console.WriteLine($"{e.Message}");
                displayName = null;

            }

            return displayName;

        }

        /// <summary>
        /// Get a comment for a Relationship
        /// </summary>
        /// <param name="relationship"></param>
        /// <returns>Comment string</returns>
        private static string GetRelationshipComment(OntologyProperty relationship)
        {

            string comment;

            try
            {

                // Use the first rdfs:comment on the ObjectProperty for the DTDL relationship comment
                comment = relationship.Comment.First().ToString();

            }
            catch (Exception e)
            {
                //Console.WriteLine($"Missing rdfs:comment on {relationship.ToString()}");
                //Console.WriteLine($"{e.Message}");
                comment = null;
            }

            return comment;

        }

        /// <summary>
        /// Create a dictionary to map XSD to DTDL primitives
        /// </summary>
        private static void CreateSchemaMap()
        {

            // Customize the schema mappings as needed
            // https://www.w3.org/2011/rdf-wg/wiki/XSD_Datatypes 
            // -> https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/dtdlv2.md#primitive-schemas

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

        }

    }

}
