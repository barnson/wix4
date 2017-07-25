// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Rows;
    using WixToolset.Extensibility;

    /// <summary>
    /// The compiler for the WiX Toolset Software Id Tag Extension.
    /// </summary>
    public sealed class TagCompiler : CompilerExtension
    {
#if false // TODO: Fix tag extension for wix4
        private static readonly string TagFolderId = "WixTagFolder";
#endif
        private const int MsidbComponentAttributes64bit = 256;

        /// <summary>
        /// Instantiate a new GamingCompiler.
        /// </summary>
        public TagCompiler()
        {
            this.Namespace = "http://wixtoolset.org/schemas/v4/wxs/tag";
        }

        /// <summary>
        /// Processes an element for the Compiler.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="element">Element to process.</param>
        /// <param name="contextValues">Extra information about the context in which this element is being parsed.</param>
        public override void ParseElement(XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            switch (parentElement.Name.LocalName)
            {
                case "Bundle":
                    switch (element.Name.LocalName)
                    {
                        case "Tag":
                            this.ParseBundleTagElement(element);
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "Product":
                    switch (element.Name.LocalName)
                    {
                        case "Tag":
                            this.ParseProductTagElement(element);
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "PatchFamily":
                    switch (element.Name.LocalName)
                    {
                        case "TagRef":
                            this.ParseTagRefElement(element);
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                default:
                    this.Core.UnexpectedElement(parentElement, element);
                    break;
            }
        }

        /// <summary>
        /// Parses a Tag element for Software Id Tag registration under a Bundle element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseBundleTagElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string name = null;
            string regid = null;
            string installPath = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Name":
                            name = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "Regid":
                            regid = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "InstallDirectory":
                        case "Win64":
                            this.Core.OnMessage(WixErrors.ExpectedParentWithAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Product"));
                            break;
                        case "InstallPath":
                            installPath = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Licensed":
                            this.Core.OnMessage(WixWarnings.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            break;
                        case "Type":
                            this.Core.OnMessage(WixWarnings.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            this.Core.ParseForExtensionElements(node);

            if (String.IsNullOrEmpty(name))
            {
                XAttribute productNameAttribute = node.Parent.Attribute("Name");
                if (null != productNameAttribute)
                {
                    name = productNameAttribute.Value;
                }
                else
                {
                    this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
                }
            }

            if (!String.IsNullOrEmpty(name) && !this.Core.IsValidLongFilename(name, false))
            {
                this.Core.OnMessage(TagErrors.IllegalName(sourceLineNumbers, node.Parent.Name.LocalName, name));
            }

            if (String.IsNullOrEmpty(regid))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Regid"));
            }
            else if (regid.StartsWith("regid."))
            {
                this.Core.OnMessage(TagWarnings.DeprecatedRegidFormat(sourceLineNumbers, regid));
                return;
            }
            else if (regid.Equals("example.com", StringComparison.OrdinalIgnoreCase))
            {
                this.Core.OnMessage(TagErrors.ExampleRegid(sourceLineNumbers, regid));
                return;
            }

            if (String.IsNullOrEmpty(installPath))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "InstallPath"));
            }
            if (!this.Core.EncounteredError)
            {
                string fileName = String.Concat(name, ".swidtag");

                Row tagRow = this.Core.CreateRow(sourceLineNumbers, "WixBundleTag");
                tagRow[0] = fileName;
                tagRow[1] = regid;
                tagRow[2] = name;
                tagRow[3] = installPath;
            }
        }

        /// <summary>
        /// Parses a Tag element for Software Id Tag registration under a Product element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseProductTagElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string name = null;
            string regid = null;
            string feature = "WixSwidTag";
            string installDirectory = null;
            bool win64 = (Platform.IA64 == this.Core.CurrentPlatform || Platform.X64 == this.Core.CurrentPlatform);

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Name":
                            name = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "Regid":
                            regid = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Feature":
                            feature = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "InstallDirectory":
                            installDirectory = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "InstallPath":
                            this.Core.OnMessage(WixErrors.ExpectedParentWithAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Bundle"));
                            break;
                        case "Licensed":
                            this.Core.OnMessage(WixWarnings.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            break;
                        case "Type":
                            this.Core.OnMessage(WixWarnings.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            break;
                        case "Win64":
                            win64 = (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib));
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            this.Core.ParseForExtensionElements(node);

            if (String.IsNullOrEmpty(name))
            {
                XAttribute productNameAttribute = node.Parent.Attribute("Name");
                if (null != productNameAttribute)
                {
                    name = productNameAttribute.Value;
                }
                else
                {
                    this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
                }
            }

            if (!String.IsNullOrEmpty(name) && !this.Core.IsValidLongFilename(name, false))
            {
                this.Core.OnMessage(TagErrors.IllegalName(sourceLineNumbers, node.Parent.Name.LocalName, name));
            }

            if (String.IsNullOrEmpty(regid))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Regid"));
            }
            else if (regid.StartsWith("regid."))
            {
                this.Core.OnMessage(TagWarnings.DeprecatedRegidFormat(sourceLineNumbers, regid));
                return;
            }
            else if (regid.Equals("example.com", StringComparison.OrdinalIgnoreCase))
            {
                this.Core.OnMessage(TagErrors.ExampleRegid(sourceLineNumbers, regid));
                return;
            }

            if (String.IsNullOrEmpty(installDirectory))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "InstallDirectory"));
            }

#if false // TODO: Fix tag extension for wix4
            if (!this.Core.EncounteredError)
            {
                Identifier fileId = this.Core.CreateIdentifier("tag", regid, ".product.tag");
                string fileName = String.Concat(name, ".swidtag");
                string shortName = this.Core.CreateShortName(fileName, false, false);

                Row directoryRow = this.Core.CreateRow(sourceLineNumbers, "Directory");
                directoryRow[0] = "WixTagInstallFolder";
                directoryRow[1] = installDirectory;
                directoryRow[2] = ".";

                this.Core.CreateSimpleReference(sourceLineNumbers, "Directory", installDirectory);

                ComponentRow componentRow = (ComponentRow)this.Core.CreateRow(sourceLineNumbers, "Component", fileId);
                componentRow.Guid = "*";
                componentRow[3] = (win64 ? TagCompiler.MsidbComponentAttributes64bit : 0);
                componentRow.Directory = TagCompiler.TagFolderId;
                componentRow.IsLocalOnly = true;
                componentRow.KeyPath = fileId.Id;

                this.Core.CreateSimpleReference(sourceLineNumbers, "Directory", TagCompiler.TagFolderId);
                this.Core.CreateSimpleReference(sourceLineNumbers, "Feature", feature);
                this.Core.CreateComplexReference(sourceLineNumbers, ComplexReferenceParentType.Feature, feature, null, ComplexReferenceChildType.Component, fileId.Id, true);

                FileRow fileRow = (FileRow)this.Core.CreateRow(sourceLineNumbers, "File", fileId);
                fileRow.Component = fileId.Id;
                fileRow.FileName = String.Concat(shortName, "|", fileName);

                WixFileRow wixFileRow = (WixFileRow)this.Core.CreateRow(sourceLineNumbers, "WixFile");
                wixFileRow.Directory = TagCompiler.TagFolderId;
                wixFileRow.File = fileId.Id;
                wixFileRow.DiskId = 1;
                wixFileRow.Attributes = 1;
                wixFileRow.Source = fileName;

                this.Core.EnsureTable(sourceLineNumbers, "SoftwareIdentificationTag");
                Row row = this.Core.CreateRow(sourceLineNumbers, "WixProductTag");
                row[0] = fileId.Id;
                row[1] = regid;
                row[2] = name;

                this.Core.CreateSimpleReference(sourceLineNumbers, "File", fileId.Id);
            }
#endif
        }

        /// <summary>
        /// Parses a TagRef element for Software Id Tag registration under a PatchFamily element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseTagRefElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string regid = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Regid":
                            regid = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            this.Core.ParseForExtensionElements(node);

            if (String.IsNullOrEmpty(regid))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Regid"));
            }
            else if (regid.StartsWith("regid."))
            {
                this.Core.OnMessage(TagWarnings.DeprecatedRegidFormat(sourceLineNumbers, regid));
                return;
            }
            else if (regid.Equals("example.com", StringComparison.OrdinalIgnoreCase))
            {
                this.Core.OnMessage(TagErrors.ExampleRegid(sourceLineNumbers, regid));
                return;
            }

            if (!this.Core.EncounteredError)
            {
                Identifier id = this.Core.CreateIdentifier("tag", regid, ".product.tag");
                this.Core.CreatePatchFamilyChildReference(sourceLineNumbers, "Component", id.Id);
            }
        }
    }
}
