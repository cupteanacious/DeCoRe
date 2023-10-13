// Copyright (c) Faizan Yousaf, Tobias Bonnesen.  All Rights Reserved.

namespace DCR2Jolie

open System.Xml.Linq

module PomGeneration =
    let writePomFile (artifactId: string) (groupId: string) (version: string) (finalName: string) (directory: string) =
        let ns = XNamespace.Get("http://maven.apache.org/POM/4.0.0")
        let xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance")
        let pom = 
            new XElement(ns + "project",
                new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                new XAttribute(xsi + "schemaLocation", "http://maven.apache.org/POM/4.0.0 http://maven.apache.org/xsd/maven-4.0.0.xsd"),
                new XElement(ns + "modelVersion", "4.0.0"),
                new XElement(ns + "groupId", groupId),
                new XElement(ns + "artifactId", artifactId),
                new XElement(ns + "version", version),
                new XElement(ns + "packaging", "jar"),
                new XElement(ns + "name", artifactId),
                new XElement(ns + "url", "http://maven.apache.org"),
                new XElement(ns + "properties",
                    new XElement(ns + "project.build.sourceEncoding", "UTF-8"),
                    new XElement(ns + "maven.compiler.source", "1.8"),
                    new XElement(ns + "maven.compiler.target", "1.8")),
                new XElement(ns + "dependencies",
                    new XElement(ns + "dependency",
                        new XElement(ns + "groupId", "org.jolie-lang"),
                        new XElement(ns + "artifactId", "jolie"),
                        new XElement(ns + "version", "1.11.0"))),
                new XElement(ns + "build",
                    new XElement(ns + "finalName", finalName),
                    new XElement(ns + "sourceDirectory", "java"),
                    new XElement(ns + "plugins",
                        new XElement(ns + "plugin",
                            new XElement(ns + "groupId", "org.apache.maven.plugins"),
                            new XElement(ns + "artifactId", "maven-compiler-plugin"),
                            new XElement(ns + "version", "3.7.0"),
                            new XElement(ns + "configuration",
                                new XElement(ns + "source", "1.8"),
                                new XElement(ns + "target", "1.8"))),
                        new XElement(ns + "plugin",
                            new XElement(ns + "groupId", "org.apache.maven.plugins"),
                            new XElement(ns + "artifactId", "maven-jar-plugin"),
                            new XElement(ns + "version", "3.0.2"),
                            new XElement(ns + "configuration",
                                new XElement(ns + "outputDirectory", "${project.basedir}/jolie/lib"),
                                new XElement(ns + "archive",
                                    new XElement(ns + "manifest",
                                        new XElement(ns + "addClasspath", "true"),
                                        new XElement(ns + "classpathPrefix", "lib/"))))),
                        new XElement(ns + "plugin",
                            new XElement(ns + "groupId", "org.apache.maven.plugins"),
                            new XElement(ns + "artifactId", "maven-clean-plugin"),
                            new XElement(ns + "version", "3.1.0"),
                            new XElement(ns + "executions",
                                new XElement(ns + "execution",
                                    new XElement(ns + "id", "auto-clean"),
                                    new XElement(ns + "phase", "package"),
                                    new XElement(ns + "goals",
                                        new XElement(ns + "goal", "clean"))))))))
        let doc = new XDocument(pom)
        let path = System.IO.Path.Combine(directory, "pom.xml")
        
        // creating directory if doesn't exist
        System.IO.Directory.CreateDirectory(directory) |> ignore
        doc.Save(path)

    let writePomFileForRole (role: string) (directory: string) =
        let lowRole: string = role.ToLower()
        writePomFile lowRole (sprintf "%s_service" lowRole) "1.0-SNAPSHOT" lowRole directory
